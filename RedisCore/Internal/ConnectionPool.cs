using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RedisCore.Internal
{
    internal class ConnectionPool : IDisposable, IAsyncDisposable
    {
        private readonly RedisClientConfig _config;
        private bool _disposed;
        private readonly ConcurrentBag<Connection> _connections = new ConcurrentBag<Connection>();
        private readonly Task _maintainTask;
        private readonly CancellationTokenSource _maintainTaskCancellation = new CancellationTokenSource();

        public ConnectionPool(RedisClientConfig config)
        {
            _config = config;
            _maintainTask = MaintainPool();
        }

        private async Task MaintainPool()
        {
            try
            {
                while (!_disposed)
                {
                    await Task.Delay(_config.ConnectionPoolMaintenanceInterval, _maintainTaskCancellation.Token);
                    while (_connections.Count > _config.MaxFreeConnections && _connections.TryTake(out var connection))
                        await connection.DisposeAsync();
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async ValueTask<Connection> Create()
        {
            var endPoint = _config.EndPoint;
            var isUnixEndpoint = endPoint is UnixDomainSocketEndPoint;
            var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, isUnixEndpoint ? ProtocolType.Unspecified : ProtocolType.Tcp);
            try
            {
                if (!isUnixEndpoint)
                    socket.NoDelay = true;
                await socket.ConnectAsync(endPoint);

                Stream stream = null;
                if (_config.UseSsl || _config.ForceUseNetworkStream)
                {
                    stream = new NetworkStream(socket, false);
                    if (_config.UseSsl)
                    {
                        try
                        {
                            var sslStream = new SslStream(new NetworkStream(socket, false));
                            await sslStream.AuthenticateAsClientAsync(_config.HostName);
                            stream = sslStream;
                        }
                        catch
                        {
                            stream.Dispose();
                            throw;
                        }
                    }
                }

                return new Connection(socket, stream, _config.BufferSize);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        public async ValueTask<Connection> Acquire()
        {
            Connection connection;
            while (_connections.TryTake(out connection))
            {
                if (connection.Connected)
                    return connection;
                await connection.DisposeAsync();
            }

            while (true)
            {
                connection = await Create();
                if (connection.Connected)
                    return connection;
                await connection.DisposeAsync();
            }
        }

        public void Release(Connection connection)
        {
            if (!connection.Connected)
            {
                connection.Dispose();
                return;
            }
            
            _connections.Add(connection);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _maintainTaskCancellation.Cancel();
            _maintainTask.Wait();
            foreach (var connection in _connections)
                connection.Dispose();
            _connections.Clear();
            _maintainTaskCancellation.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;
            _maintainTaskCancellation.Cancel();
            await _maintainTask;
            foreach (var connection in _connections)
                await connection.DisposeAsync();
            _connections.Clear();
            _maintainTaskCancellation.Dispose();
        }
    }
}