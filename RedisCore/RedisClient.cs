﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using RedisCore.Internal;
using RedisCore.Internal.Commands;
using RedisCore.Internal.Protocol;
using RedisCore.Utils;

namespace RedisCore;

public class RedisClient : RedisCommandsBase, IDisposable, IAsyncDisposable
{
    private readonly RedisClientConfig _config;
    private readonly ConnectionPool _connectionPool;
    private bool _disposed;

    private protected override ScriptCache? Scripts { get; }

    public RedisClient(RedisClientConfig config)
    {
        _config = config.Clone();
        _connectionPool = new ConnectionPool(_config);
        if (config.UseScriptCache)
            Scripts = new ScriptCache(this);
    }

    public RedisClient(Uri uri)
        : this(new RedisClientConfig(uri))
    {
    }

    public RedisClient(string uri)
        : this(new RedisClientConfig(uri))
    {
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _connectionPool.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;
        await _connectionPool.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    #region Private members

    private void CheckDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException("Redis client");
    }

    private static TObject CheckError<TObject>(TObject @object)
        where TObject : RedisObject
    {
        return @object is RedisError error
            ? throw new RedisClientException(error.Type, error.Message)
            : @object;
    }

    private static bool WrapException(Connection connection, Exception e, [MaybeNullWhen(false)] out RedisConnectionException redisException)
    {
        switch (e)
        {
            case SocketException:
            case IOException { InnerException: SocketException }:
                redisException = new RedisConnectionException(e.Message, e);
                return true;
            case EndOfStreamException:
                connection.MarkAsDisconnected();
                redisException = new RedisConnectionException(e.Message, e);
                return true;
            default:
                redisException = null;
                return false;
        }
    }

    private async ValueTask<Connection> AcquireConnection()
    {
        CheckDisposed();
        try
        {
            var connection = await _connectionPool.Acquire();
            if (_config.Password != null && !connection.Authenticated)
            {
                await Execute(connection, new AuthCommand(_config.Password));
                connection.MarkAsAuthenticated();
            }

            if (connection.Database != _config.Database)
            {
                await Execute(connection, new SelectCommand(_config.Database));
                connection.SetSelectedDatabase(_config.Database);
            }

            return connection;
        }
        catch (SocketException e)
        {
            throw new RedisConnectionException(e.Message, e);
        }
    }

    private void ReleaseConnection(Connection connection)
    {
        CheckDisposed();
        _connectionPool.Release(connection);
    }

    private IBufferPool<byte> CreateBufferPool()
    {
        return _config.UseBufferPool ? BufferPool.Create<byte>() : BufferPool.Empty<byte>();
    }

    private async ValueTask<RedisObject> Execute(Connection connection, RedisArray commandData, IBufferPool<byte> bufferPool)
    {
        var retryDelay = _config.LoadingRetryDelayMin;
        var retryTimeoutTime = MonotonicTime.Now + _config.LoadingRetryTimeout;
        while (true)
        {
            try
            {
                RedisObject result;
                try
                {
                    ProtocolHandler.Write(connection.Output, commandData);
                    await connection.Output.FlushAsync();
                    result = await ProtocolHandler.Read(connection.Input, bufferPool);
                }
                catch (Exception e)
                {
                    if (WrapException(connection, e, out var redisException))
                        throw redisException;
                    throw;
                }

                return CheckError(result);
            }
            catch (RedisClientException e)
            {
                if (e.Type == KnownRedisErrors.NoScript)
                {
                    await Scripts!.ReUploadAll();
                    continue;
                }

                // Right after start Redis server accepts connections immediately
                // but still not operation by some time while loading database
                // So it always returns error in response until fully loaded 
                if (e.Type != KnownRedisErrors.Loading || MonotonicTime.Now > retryTimeoutTime)
                    throw;

                await Task.Delay(retryDelay);
                retryDelay = retryDelay.Multiply(2);
                if (retryDelay > _config.LoadingRetryDelayMax)
#pragma warning disable IDE0059 // Remove unnecessary value assignment - false positive
                    retryDelay = _config.LoadingRetryDelayMax;
#pragma warning restore IDE0059
            }
        }
    }

    private async ValueTask<T> Execute<T>(Connection connection, Command<T> command)
    {
        using var bufferPool = CreateBufferPool();
        var result = await Execute(connection, command.Data, bufferPool);
        return command.GetResult(result);
    }

    private async ValueTask<Memory<byte>?> Execute<TCommand>(Connection connection, TCommand command, IBufferPool<byte> bufferPool)
        where TCommand : Command<Optional<byte[]>>
    {
        var result = await Execute(connection, command.Data, bufferPool);
        return result.To(bufferPool);
    }

    internal override async ValueTask<T> Execute<T>(Command<T> command)
    {
        CheckDisposed();
        var connection = await AcquireConnection();
        try
        {
            return await Execute(connection, command);
        }
        finally
        {
            ReleaseConnection(connection);
        }
    }

    private protected override async ValueTask<Memory<byte>?> Execute<TCommand>(TCommand command, IBufferPool<byte> bufferPool)
    {
        CheckDisposed();
        var connection = await AcquireConnection();
        try
        {
            return await Execute(connection, command, bufferPool);
        }
        finally
        {
            ReleaseConnection(connection);
        }
    }

    #endregion

    public IRedisTransaction CreateTransaction()
    {
        return new Transaction(this);
    }

    #region Transaction

    private sealed class Transaction(RedisClient client) : RedisCommandsBase, IRedisTransaction
    {
        private readonly IBufferPool<byte> _bufferPool = client.CreateBufferPool();
        private bool _disposed;
        private readonly List<string> _watchedKeys = [];
        private readonly List<QueuedCommand> _queuedCommands = [];

        private protected override ScriptCache? Scripts => client.Scripts;

        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException("Redis transaction");
        }

        public void Watch(string key)
        {
            CheckDisposed();
            _watchedKeys.Add(key);
        }

        public async ValueTask<bool> Complete()
        {
            CheckDisposed();
            _disposed = true;
            var connection = await client.AcquireConnection();
            try
            {
                foreach (var key in _watchedKeys)
                    await client.Execute(connection, new WatchCommand(key));

                await client.Execute(connection, new MultiCommand());
                try
                {
                    foreach (var command in _queuedCommands)
                        await client.Execute(connection, command.Data, _bufferPool);
                    var result = await client.Execute(connection, new ExecCommand());
                    if (result == null)
                    {
                        foreach (var command in _queuedCommands)
                            command.SetCancelled();
                        return false;
                    }

                    for (var i = 0; i < result.Count; ++i)
                        _queuedCommands[i].SetResult(result[i]);
                    return true;
                }
                catch
                {
                    await client.Execute(connection, new DiscardCommand());
                    throw;
                }
            }
            catch
            {
                foreach (var command in _queuedCommands)
                    command.SetCancelled();
                throw;
            }
            finally
            {
                client.ReleaseConnection(connection);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            foreach (var command in _queuedCommands)
                command.SetCancelled();
            _bufferPool.Dispose();
        }

        private async ValueTask<T> Queue<T>(QueuedCommand<T> command)
        {
            _queuedCommands.Add(command);
            return await command.Task;
        }

        internal override async ValueTask<T> Execute<T>(Command<T> command)
        {
            CheckDisposed();
            return await Queue(new QueuedValueCommand<T>(command));
        }

        private protected override async ValueTask<Memory<byte>?> Execute<TCommand>(TCommand command, IBufferPool<byte> bufferPool)
        {
            CheckDisposed();
            return await Queue(new QueuedBufferCommand<TCommand>(command, bufferPool));
        }

        private abstract class QueuedCommand
        {
            public abstract RedisArray Data { get; }
            public abstract void SetResult(RedisObject protocolResult);
            public abstract void SetCancelled();
        }

        private abstract class QueuedCommand<T> : QueuedCommand
        {
            private readonly TaskCompletionSource<T> _taskSource;

            public Task<T> Task => _taskSource.Task;

            protected QueuedCommand()
            {
                _taskSource = new TaskCompletionSource<T>();
            }

            protected abstract T ExtractResult(RedisObject protocolResult);

            public override void SetResult(RedisObject protocolResult)
            {
                try
                {
                    CheckError(protocolResult);
                    var result = ExtractResult(protocolResult);
                    _taskSource.TrySetResult(result);
                }
                catch (Exception e)
                {
                    _taskSource.TrySetException(e);
                }
            }

            public override void SetCancelled()
            {
                _taskSource.TrySetCanceled();
            }
        }

        private sealed class QueuedValueCommand<T>(Command<T> command) : QueuedCommand<T>
        {
            public override RedisArray Data => command.Data;

            protected override T ExtractResult(RedisObject protocolResult) => command.GetResult(protocolResult);
        }

        private sealed class QueuedBufferCommand<TCommand>(TCommand command, IBufferPool<byte> bufferPool) : QueuedCommand<Memory<byte>?>
            where TCommand : Command<Optional<byte[]>>
        {
            public override RedisArray Data => command.Data;

            protected override Memory<byte>? ExtractResult(RedisObject protocolResult) => protocolResult.To(bufferPool);
        }
    }

    #endregion

    public async ValueTask<ISubscription> Subscribe(string channel)
    {
        CheckDisposed();
        var connection = await AcquireConnection();
        try
        {
            var subscription = new Subscription(this, connection, channel);
            await subscription.Subscribe();
            return subscription;
        }
        catch
        {
            ReleaseConnection(connection);
            throw;
        }
    }

    #region Subscription

    private sealed class Subscription(RedisClient client, Connection connection, string channel) : ISubscription
    {
        private bool _unsubscribed;
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (_unsubscribed)
                client.ReleaseConnection(connection);
            else
                connection.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            if (!_unsubscribed)
                try
                {
                    await Unsubscribe();
                }
                catch (RedisConnectionException)
                {
                    await connection.DisposeAsync();
                    _disposed = true;
                    return;
                }

            _disposed = true;
            client.ReleaseConnection(connection);
        }

        private void CheckDisposed()
        {
            if (_disposed || _unsubscribed)
                throw new ObjectDisposedException("Redis subscription");
        }

        private async ValueTask SendCommand<T>(Command<T> cmd)
        {
            try
            {
                ProtocolHandler.Write(connection.Output, cmd.Data);
                await connection.Output.FlushAsync();
            }
            catch (Exception e)
            {
                if (WrapException(connection, e, out var redisException))
                    throw redisException;
                throw;
            }
        }

        internal async ValueTask Subscribe()
        {
            await SendCommand(new SubscribeCommand(channel));
            await GetMessage<int>("subscribe");
        }

        public async ValueTask Unsubscribe()
        {
            CheckDisposed();
            await SendCommand(new UnsubscribeCommand());
            await GetMessage<int>("unsubscribe");
            _unsubscribed = true;
        }

        private async ValueTask<T> GetMessage<T, TExtractResult>(string name, TExtractResult extractResult, CancellationToken cancellationToken = default)
            where TExtractResult : struct, IExtractResult<T>
        {
            CheckDisposed();
            using var bufferPool = client.CreateBufferPool();
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                RedisObject result;
                try
                {
                    result = await ProtocolHandler.Read(connection.Input, bufferPool, cancellationToken);
                }
                catch (Exception e)
                {
                    if (WrapException(connection, e, out var redisException))
                        throw redisException;
                    throw;
                }

                var resultArray = ((RedisArray)CheckError(result)).Items;
                if (((RedisValueObject)resultArray[0]).To<string>() == name)
                    return extractResult.GetResult((RedisValueObject)resultArray[2]);
            }
        }

        private async ValueTask<T> GetMessage<T>(string name, CancellationToken cancellationToken = default)
        {
            return await GetMessage<T, ExtractValueResult<T>>(name, new ExtractValueResult<T>(), cancellationToken);
        }

        public async ValueTask<T> GetMessage<T>(CancellationToken cancellationToken = default)
        {
            return await GetMessage<T>("message", cancellationToken);
        }

        public async ValueTask<Memory<byte>> GetMessage(IBufferPool<byte> bufferPool, CancellationToken cancellationToken = default)
        {
            return await GetMessage<Memory<byte>, ExtractBufferResult>("message", new ExtractBufferResult(bufferPool), cancellationToken);
        }

        private interface IExtractResult<out T>
        {
            T GetResult(RedisValueObject value);
        }

        private struct ExtractValueResult<T> : IExtractResult<T>
        {
            public readonly T GetResult(RedisValueObject value) => value.To<T>();
        }

        private readonly struct ExtractBufferResult(IBufferPool<byte> bufferPool) : IExtractResult<Memory<byte>>
        {
            private readonly IBufferPool<byte> _bufferPool = bufferPool;

            public Memory<byte> GetResult(RedisValueObject value) => value.To(_bufferPool)!.Value;
        }
    }

    #endregion
}