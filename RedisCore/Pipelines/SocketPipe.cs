using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RedisCore.Pipelines;

public class SocketPipe : DuplexPipe
{
    private readonly Socket _socket;

    public SocketPipe(Socket socket, int bufferSegmentSize = 512, int pauseWriterThreshold = 8192, int resumeWriterThreshold = 4096)
        : base(bufferSegmentSize, pauseWriterThreshold, resumeWriterThreshold)
    {
        _socket = socket;
    }
        
    protected override async Task PopulateReader(PipeWriter readerBackend)
    {
        while (true)
        {
            var memory = readerBackend.GetMemory(BufferSegmentSize);
            var bytesRead = await _socket.ReceiveAsync(memory, SocketFlags.None);
            if (bytesRead == 0)
                break;

            readerBackend.Advance(bytesRead);

            var result = await readerBackend.FlushAsync();
            if (result.IsCompleted)
                break;
        }
    }

    protected override async Task PopulateWriter(PipeReader writerBackend)
    {
        while (true)
        {
            var result = await writerBackend.ReadAsync();
            var buffer = result.Buffer;
            while (buffer.Length > 0)
            {
                await _socket.SendAsync(buffer.First, SocketFlags.None);
                buffer = buffer.Slice(buffer.First.Length);
            }

            writerBackend.AdvanceTo(result.Buffer.End);
               
            if (result.IsCompleted)
                break;
        }
    }
}