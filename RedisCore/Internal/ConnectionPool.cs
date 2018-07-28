using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RedisCore.Internal
{
    internal class ConnectionPool : IDisposable
    {
        private bool _disposed;
        private readonly EndPoint _endPoint;
        private readonly int _bufferSize;
        private readonly int _maxFreeConnections;
        private readonly ConcurrentBag<Connection> _connections = new ConcurrentBag<Connection>();
        private readonly AutoResetEvent _event = new AutoResetEvent(false);
        private readonly Task _maintainTask;

        public ConnectionPool(EndPoint endPoint, int bufferSize, int maxFreeConnections = 2)
        {
            _endPoint = endPoint;
            _bufferSize = bufferSize;
            _maxFreeConnections = maxFreeConnections;
            _maintainTask = Task.Factory.StartNew(MaintainPool, TaskCreationOptions.LongRunning);
        }

        private void MaintainPool()
        {
            while (!_disposed)
            {
                while (_connections.Count > _maxFreeConnections && _connections.TryTake(out var connection))
                    connection.Dispose();
                _event.WaitOne();
            }
        }

        private async ValueTask<Connection> Create()
        {
            var isUnixEndpoint = _endPoint is UnixDomainSocketEndPoint;
            var socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, isUnixEndpoint ? ProtocolType.Unspecified : ProtocolType.Tcp);
            try
            {
                if (!isUnixEndpoint)
                    socket.NoDelay = true;
                await socket.ConnectAsync(_endPoint);
                return new Connection(socket, _bufferSize);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        public async ValueTask<Connection> Aquire()
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