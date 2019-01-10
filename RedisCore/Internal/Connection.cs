using System;
using System.IO;
using System.IO.Pipelines;
using System.Net.Sockets;
using RedisCore.Pipelines;

namespace RedisCore.Internal
{
    internal class Connection : IDisposable
    {
        private readonly Socket _socket;
        private readonly Stream _stream;
        private readonly DuplexPipe _pipe;
        private bool _connected = true;

        public bool Connected => _connected && _socket.Connected;
        public PipeReader Input => _pipe.Input;
        public PipeWriter Output => _pipe.Output;

        public Connection(Socket socket, Stream stream, int bufferSize)
        {
            _socket = socket;
            _stream = stream;
            if (stream == null)
                _pipe = new SocketPipe(socket, bufferSize / 2, bufferSize, bufferSize / 2);
            else
                _pipe = new StreamPipe(stream, bufferSize / 2, bufferSize, bufferSize / 2);
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _socket.Dispose();
        }

        public void MarkAsDisconnected() => _connected = false;
    }
}