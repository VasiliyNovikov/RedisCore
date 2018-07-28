using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RedisCore.Internal
{
    public class Connection : IDisposable
    {
        private readonly Socket _socket;
        private readonly Stream _stream;

        public bool Connected => _socket.Connected;

        public Connection(Socket socket, int bufferSize)
        {
            _socket = socket;
            Stream stream = new NetworkStream(socket, false);
            _stream = bufferSize > 0 ? new BufferedStream(stream, bufferSize) : stream;
        }

        public void Dispose()
        {
            if (_socket.Connected)
                _stream.Dispose();
            else if (_stream is BufferedStream buffStream)
                buffStream.UnderlyingStream.Dispose();
            else
                _stream.Dispose();
            _socket.Dispose();
        }

        public async ValueTask<int> Read(Memory<byte> buffer)
        {
            var result = await _stream.ReadAsync(buffer);
            if (result == 0)
                throw new SocketException((int)SocketError.Interrupted);
            return result;
        }
        
        public async ValueTask Write(ReadOnlyMemory<byte> buffer)
        {
            await _stream.WriteAsync(buffer);
        }

        public async ValueTask Flush()
        {
            await _stream.FlushAsync();
        }
    }
}