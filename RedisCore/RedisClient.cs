using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using RedisCore.Internal;
using RedisCore.Internal.Commands;
using RedisCore.Internal.Protocol;
using RedisCore.Utils;

namespace RedisCore
{
    public class RedisClient : RedisCommandsBase, IDisposable
    {
        private readonly RedisClientConfig _config;
        private readonly ConnectionPool _connectionPool;
        private bool _disposed;
        
        public RedisClient(RedisClientConfig config)
        {
            _config = config;
            _connectionPool = new ConnectionPool(_config);
        }

        public RedisClient(EndPoint endPoint)
            : this(new RedisClientConfig(endPoint))
        {
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _connectionPool.Dispose();
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

        private static bool WrapException(Connection connection, Exception e, out RedisConnectionException redisException)
        {
            switch (e)
            {
                case SocketException _:
                case IOException io when io.InnerException is SocketException:
                    redisException = new RedisConnectionException(e.Message, e);
                    return true;
                case EndOfStreamException _:
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
                if (_config.Password != null)
                    await Execute(connection, new AuthCommand(_config.Password));
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

        private async ValueTask<RedisObject> Execute(Connection connection, RedisArray commandData)
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
                        result = await ProtocolHandler.Read(connection.Input);
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
                    // Right after start Redis server accepts connections immediately
                    // but still not operation by some time while loading database
                    // So it always returns error in response until fully loaded 
                    if (e.Type != "LOADING" || MonotonicTime.Now > retryTimeoutTime)
                        throw;
                    
                    await Task.Delay(retryDelay);
                    retryDelay *= 2;
                    if (retryDelay > _config.LoadingRetryDelayMax)
                        retryDelay = _config.LoadingRetryDelayMax;
                }
            }
        }

        private async ValueTask<T> Execute<T>(Connection connection, Command<T> command)
        {
            var result = await Execute(connection, command.Data);
            return command.GetResult(result);
        }

        private protected override async ValueTask<T> Execute<T>(Command<T> command)
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

        #endregion

        public IRedisTransaction CreateTransaction()
        {
            return new Transaction(this);
        }

        #region Transaction

        private class Transaction : RedisCommandsBase, IRedisTransaction
        {
            private readonly RedisClient _client;
            private bool _disposed;
            private readonly List<string> _watchedKeys = new List<string>();
            private readonly List<QueuedCommand> _queuedCommands = new List<QueuedCommand>();
            
            public Transaction(RedisClient client)
            {
                _client = client;
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

            public async ValueTask<bool>  Complete()
            {
                CheckDisposed();
                _disposed = true;
                var connection = await _client.AcquireConnection();
                try
                {
                    foreach (var key in _watchedKeys)
                        await _client.Execute(connection, new WatchCommand(key));

                    await _client.Execute(connection, new MultiCommand());
                    try
                    {
                        foreach (var command in _queuedCommands)
                            await _client.Execute(connection, command.Data);
                        var result = await _client.Execute(connection, new ExecCommand());
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
                        await _client.Execute(connection, new DiscardCommand());
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
            }

            private async ValueTask<T> Queue<T>(QueuedCommand<T> command)
            {
                _queuedCommands.Add(command);
                return await command.Task;
            }

            private protected override async ValueTask<T> Execute<T>(Command<T> command)
            {
                CheckDisposed();
                return await Queue(new QueuedCommand<T>(command));
            }

            private abstract class QueuedCommand
            {
                public abstract RedisArray Data { get; }
                public abstract void SetResult(RedisObject protocolResult);
                public abstract void SetCancelled();
            }
            
            private class QueuedCommand<T> : QueuedCommand
            {
                private readonly Command<T> _command;
                private readonly TaskCompletionSource<T> _taskSource;

                public Task<T> Task => _taskSource.Task;

                public override RedisArray Data => _command.Data;

                public QueuedCommand(Command<T> command) 
                {
                    _command = command;
                    _taskSource = new TaskCompletionSource<T>();
                }

                public override void SetResult(RedisObject protocolResult)
                {
                    try
                    {
                        CheckError(protocolResult);
                        var result = _command.GetResult(protocolResult);
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
        }
        
        #endregion
        
        public async ValueTask<ISubscription> Subscribe(string channel)
        {
            CheckDisposed();
            var connection = await AcquireConnection();
            try
            {
                await Execute(connection, new SubscribeCommand(channel));
                return new Subscription(this, connection);
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
            private bool _unsubscribed;
            private bool _disposed;

            public Subscription(RedisClient client, Connection connection)
            {
                _client = client;
                _connection = connection;
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

            private void CheckDisposed()
            {
                if (_disposed || _unsubscribed)
                    throw new ObjectDisposedException("Redis subscription");
            }

            public async ValueTask Unsubscribe()
            {
                CheckDisposed();
                while (true)
                {
                    RedisObject result;
                    try
                    {
                        result = await _client.Execute(_connection, new RedisArray(CommandNames.Unsubscribe));
                    }
                    catch (Exception e)
                    {
                        if (WrapException(_connection, e, out var redisException))
                            throw redisException;
                        throw;
                    }
    
                    var resultArray = ((RedisArray)CheckError(result)).Items;
                    if (((RedisValueObject) resultArray[0]).To<string>() != "unsubscribe") 
                        continue;
                    
                    _unsubscribed = true;
                    return;
                }
            }

            public async ValueTask<T> GetMessage<T>(CancellationToken cancellationToken = default)
            {
                CheckDisposed();
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    RedisObject result;
                    try
                    {
                        result = await ProtocolHandler.Read(_connection.Input, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        if (WrapException(_connection, e, out var redisException))
                            throw redisException;
                        throw;
                    }
    
                    var resultArray = ((RedisArray)CheckError(result)).Items;
                    if (((RedisValueObject) resultArray[0]).To<string>() == "message")
                        return ((RedisValueObject) resultArray[2]).To<T>();
                }
            }
        }

        #endregion
    }
}