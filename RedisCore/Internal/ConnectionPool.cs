using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RedisCore.Internal
{
    internal class ConnectionPool : IDisposable, IAsyncDisposable
    {
        private const int DefaultTcpPort = 6379;
        private const int DefaultSslPort = 6380;

        private readonly RedisClientConfig _config;
        private bool _disposed;
        private readonly ConcurrentBag<Connection> _connections = new();
        private readonly Task _maintainTask;
        private readonly CancellationTokenSource _maintainTaskCancellation = new();

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
            var uri = _config.Uri;
            EndPoint endPoint;
            bool isUnixEndpoint;
            if (_config.Uri.Scheme == RedisUriSchema.Unix)
            {
#if NETCOREAPP3_1_OR_GREATER
                endPoint = new UnixDomainSocketEndPoint(uri.AbsolutePath);
                isUnixEndpoint = true;
#else
                throw new PlatformNotSupportedException("Unix domain sockets are not supported by the runtime");
#endif
            }
            else
            {
                var address = uri.HostNameType == UriHostNameType.Dns 
                    ? (await Dns.GetHostEntryAsync(uri.DnsSafeHost)).AddressList[0]
                    : IPAddress.Parse(uri.DnsSafeHost);
                var port = uri.IsDefaultPort
                    ? uri.Scheme == RedisUriSchema.Tcp ? DefaultTcpPort : DefaultSslPort
                    : uri.Port;
                endPoint = new IPEndPoint(address, port);
                isUnixEndpoint = false;
            }

            var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, isUnixEndpoint ? ProtocolType.Unspecified : ProtocolType.Tcp);
            try
            {
                if (!isUnixEndpoint)
                    socket.NoDelay = true;
                await socket.ConnectAsync(endPoint);

                Stream? stream = null;
                if (uri.Scheme == RedisUriSchema.Ssl || _config.ForceUseNetworkStream)
                {
                    stream = new NetworkStream(socket);
                    if (uri.Scheme == RedisUriSchema.Ssl)
                    {
                        try
                        {
                            var sslStream = new SslStream(stream);
                            await sslStream.AuthenticateAsClientAsync(uri.DnsSafeHost);
                            stream = sslStream;
                        }
                        catch
                        {
                            await stream.DisposeAsync();
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
            Connection? connection;
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