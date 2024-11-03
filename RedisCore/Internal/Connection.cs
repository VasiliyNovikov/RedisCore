using System;
using System.IO;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;
using RedisCore.Pipelines;

namespace RedisCore.Internal;

internal sealed class Connection : IDisposable, IAsyncDisposable
{
    private readonly Socket _socket;
    private readonly Stream? _stream;
    private readonly DuplexPipe _pipe;

    public bool Connected
    {
        get => field && _socket.Connected;
        private set;
    }

    public bool Authenticated { get; private set; }
    public int Database { get; private set; }

    public PipeReader Input => _pipe.Input;
    public PipeWriter Output => _pipe.Output;

    public Connection(Socket socket, Stream? stream, int bufferSize)
    {
        _socket = socket;
        _stream = stream;
        _pipe = stream is null
            ? new SocketPipe(socket, bufferSize / 2, bufferSize, bufferSize / 2)
            : new StreamPipe(stream, bufferSize / 2, bufferSize, bufferSize / 2);
        Connected = true;
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _socket.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_stream != null)
            await _stream.DisposeAsync();
        _socket.Dispose();
    }

    public void MarkAsDisconnected() => Connected = false;

    public void MarkAsAuthenticated() => Authenticated = true;

    public void SetSelectedDatabase(int database) => Database = database;
}