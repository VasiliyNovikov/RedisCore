using System;
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

public sealed class RedisClient : RedisCommandsBase, IDisposable, IAsyncDisposable
{
    private readonly RedisClientConfig _config;
    private readonly ConnectionPool _connectionPool;
    private bool _disposed;

    private protected override ScriptCache? Scripts { get; }

    public RedisClient(RedisClientConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _connectionPool = new ConnectionPool(_config);
        if (_config.UseScriptCache)
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
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;
        await _connectionPool.DisposeAsync().ConfigureAwait(false);
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
#if NETCOREAPP3_1_OR_GREATER
                redisException = null;
#else
                redisException = null!;
#endif
                return false;
        }
    }

    private async ValueTask<Connection> AcquireConnection()
    {
        CheckDisposed();
        try
        {
            var connection = await _connectionPool.Acquire().ConfigureAwait(false);
            if (_config.Password != null && !connection.Authenticated)
            {
                await Execute(connection, new AuthCommand(_config.Password)).ConfigureAwait(false);
                connection.MarkAsAuthenticated();
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
                    await connection.Output.FlushAsync().ConfigureAwait(false);
                    result = await ProtocolHandler.Read(connection.Input, bufferPool).ConfigureAwait(false);
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
                if (e.ErrorType == KnownRedisErrors.NoScript)
                {
                    await Scripts!.ReUploadAll().ConfigureAwait(false);
                    continue;
                }

                // Right after start Redis server accepts connections immediately
                // but still not operation by some time while loading database
                // So it always returns error in response until fully loaded 
                if (e.ErrorType != KnownRedisErrors.Loading || MonotonicTime.Now > retryTimeoutTime)
                    throw;

                await Task.Delay(retryDelay).ConfigureAwait(false);
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
        var result = await Execute(connection, command.Data, bufferPool).ConfigureAwait(false);
        return command.GetResult(result);
    }

    private async ValueTask<Memory<byte>?> Execute<TCommand>(Connection connection, TCommand command, IBufferPool<byte> bufferPool)
        where TCommand : Command<Optional<byte[]>>
    {
        var result = await Execute(connection, command.Data, bufferPool).ConfigureAwait(false);
        return result.To(bufferPool);
    }

    internal override async ValueTask<T> Execute<T>(Command<T> command)
    {
        CheckDisposed();
        var connection = await AcquireConnection().ConfigureAwait(false);
        try
        {
            return await Execute(connection, command).ConfigureAwait(false);
        }
        finally
        {
            ReleaseConnection(connection);
        }
    }

    private protected override async ValueTask<Memory<byte>?> Execute<TCommand>(TCommand command, IBufferPool<byte> bufferPool)
    {
        CheckDisposed();
        var connection = await AcquireConnection().ConfigureAwait(false);
        try
        {
            return await Execute(connection, command, bufferPool).ConfigureAwait(false);
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

    private class Transaction : RedisCommandsBase, IRedisTransaction
    {
        private readonly RedisClient _client;
        private readonly IBufferPool<byte> _bufferPool;
        private bool _disposed;
        private readonly List<string> _watchedKeys = new();
        private readonly List<QueuedCommand> _queuedCommands = new();

        private protected override ScriptCache? Scripts => _client.Scripts;

        public Transaction(RedisClient client)
        {
            _client = client;
            _bufferPool = client.CreateBufferPool();
        }

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
            var connection = await _client.AcquireConnection().ConfigureAwait(false);
            try
            {
                foreach (var key in _watchedKeys)
                    await _client.Execute(connection, new WatchCommand(key)).ConfigureAwait(false);

                await _client.Execute(connection, new MultiCommand()).ConfigureAwait(false);
                try
                {
                    foreach (var command in _queuedCommands)
                        await _client.Execute(connection, command.Data, _bufferPool).ConfigureAwait(false);
                    var result = await _client.Execute(connection, new ExecCommand()).ConfigureAwait(false);
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
                    await _client.Execute(connection, new DiscardCommand()).ConfigureAwait(false);
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
                _client.ReleaseConnection(connection);
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
            return await command.Task.ConfigureAwait(false);
        }

        internal override async ValueTask<T> Execute<T>(Command<T> command)
        {
            CheckDisposed();
            return await Queue(new QueuedValueCommand<T>(command)).ConfigureAwait(false);
        }

        private protected override async ValueTask<Memory<byte>?> Execute<TCommand>(TCommand command, IBufferPool<byte> bufferPool)
        {
            CheckDisposed();
            return await Queue(new QueuedBufferCommand<TCommand>(command, bufferPool)).ConfigureAwait(false);
        }

        private abstract class QueuedCommand
        {
            public abstract RedisArray Data { get; }
            public abstract void SetResult(RedisObject protocolResult);
            public abstract void SetCancelled();
        }

        private abstract class QueuedCommand<T> : QueuedCommand
        {
            private readonly TaskCompletionSource<T> _taskSource = new();

            public Task<T> Task => _taskSource.Task;

            protected abstract T ExtractResult(RedisObject protocolResult);

            [SuppressMessage("Microsoft.Design", "CA1031: Do not catch general exception types",
                             Justification = "False positive. Exception is asynchronously rethrown")]
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

        private class QueuedValueCommand<T> : QueuedCommand<T>
        {
            private readonly Command<T> _command;

            public override RedisArray Data => _command.Data;

            public QueuedValueCommand(Command<T> command)
            {
                _command = command;
            }

            protected override T ExtractResult(RedisObject protocolResult) => _command.GetResult(protocolResult);
        }

        private class QueuedBufferCommand<TCommand> : QueuedCommand<Memory<byte>?>
            where TCommand : Command<Optional<byte[]>>
        {
            private readonly TCommand _command;
            private readonly IBufferPool<byte> _bufferPool;

            public override RedisArray Data => _command.Data;

            public QueuedBufferCommand(TCommand command, IBufferPool<byte> bufferPool)
            {
                _command = command;
                _bufferPool = bufferPool;
            }

            protected override Memory<byte>? ExtractResult(RedisObject protocolResult) => protocolResult.To(_bufferPool);
        }
    }

    #endregion

    public async ValueTask<ISubscription> Subscribe(string channel)
    {
        CheckDisposed();
        var connection = await AcquireConnection().ConfigureAwait(false);
        try
        {
            var subscription = new Subscription(this, connection, channel);
            await subscription.Subscribe().ConfigureAwait(false);
            return subscription;
        }
        catch
        {
            ReleaseConnection(connection);
            throw;
        }
    }

    #region Subscription

    private class Subscription : ISubscription
    {
        private readonly RedisClient _client;
        private readonly Connection _connection;
        private readonly string _channel;
        private bool _unsubscribed;
        private bool _disposed;

        public Subscription(RedisClient client, Connection connection, string channel)
        {
            _client = client;
            _connection = connection;
            _channel = channel;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (_unsubscribed)
                _client.ReleaseConnection(_connection);
            else
                _connection.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            if (!_unsubscribed)
                try
                {
                    await Unsubscribe().ConfigureAwait(false);
                }
                catch (RedisConnectionException)
                {
                    await _connection.DisposeAsync().ConfigureAwait(false);
                    _disposed = true;
                    return;
                }

            _disposed = true;
            _client.ReleaseConnection(_connection);
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
                ProtocolHandler.Write(_connection.Output, cmd.Data);
                await _connection.Output.FlushAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if (WrapException(_connection, e, out var redisException))
                    throw redisException;
                throw;
            }
        }

        internal async ValueTask Subscribe()
        {
            await SendCommand(new SubscribeCommand(_channel)).ConfigureAwait(false);
            await GetMessage<int>("subscribe").ConfigureAwait(false);
        }

        public async ValueTask Unsubscribe()
        {
            CheckDisposed();
            await SendCommand(new UnsubscribeCommand()).ConfigureAwait(false);
            await GetMessage<int>("unsubscribe").ConfigureAwait(false);
            _unsubscribed = true;
        }

        private async ValueTask<T> GetMessage<T, TExtractResult>(string name, TExtractResult extractResult, CancellationToken cancellationToken = default)
            where TExtractResult : struct, IExtractResult<T>
        {
            CheckDisposed();
            using var bufferPool = _client.CreateBufferPool();
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                RedisObject result;
                try
                {
                    result = await ProtocolHandler.Read(_connection.Input, bufferPool, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    if (WrapException(_connection, e, out var redisException))
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
            return await GetMessage<T, ExtractValueResult<T>>(name, new ExtractValueResult<T>(), cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<T> GetMessage<T>(CancellationToken cancellationToken = default)
        {
            return await GetMessage<T>("message", cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<Memory<byte>> GetMessage(IBufferPool<byte> bufferPool, CancellationToken cancellationToken = default)
        {
            return await GetMessage<Memory<byte>, ExtractBufferResult>("message", new ExtractBufferResult(bufferPool), cancellationToken).ConfigureAwait(false);
        }

        private interface IExtractResult<out T>
        {
            T GetResult(RedisValueObject value);
        }

        private struct ExtractValueResult<T> : IExtractResult<T>
        {
            public T GetResult(RedisValueObject value) => value.To<T>();
        }

        private readonly struct ExtractBufferResult : IExtractResult<Memory<byte>>
        {
            private readonly IBufferPool<byte> _bufferPool;
            public ExtractBufferResult(IBufferPool<byte> bufferPool) => _bufferPool = bufferPool;
            // ReSharper disable PossibleInvalidOperationException
            public Memory<byte> GetResult(RedisValueObject value) => value.To(_bufferPool)!.Value;
            // ReSharper restore PossibleInvalidOperationException
        }
    }

    #endregion
}