using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RedisCore.Internal
{
    internal class ConnectionPool : IDisposable
    {
        private readonly RedisClientConfig _config;
        private bool _disposed;
        private readonly ConcurrentBag<Connection> _connections = new ConcurrentBag<Connection>();
        private readonly AutoResetEvent _event = new AutoResetEvent(false);
        private readonly Task _maintainTask;

        public ConnectionPool(RedisClientConfig config)
        {
            _config = config;
            _maintainTask = Task.Factory.StartNew(MaintainPool, TaskCreationOptions.LongRunning);
        }

        private void MaintainPool()
        {
            while (!_disposed)
            {
                while (_connections.Count > _config.MaxFreeConnections && _connections.TryTake(out var connection))
                    connection.Dispose();
                _event.WaitOne();
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
                if (_config.UseSsl)
                {
                    stream = new NetworkStream(socket, false);
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
                connection.Dispose();
            }

            while (true)
            {
                connection = await Create();
                if (connection.Connected)
                    return connection;
                connection.Dispose();
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
            _event.Set();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _event.Set();
            _maintainTask.Wait();
            foreach (var connection in _connections)
                connection.Dispose();
            _connections.Clear();
            _event.Dispose();
        }
    }
}