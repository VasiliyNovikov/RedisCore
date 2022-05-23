using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace RedisCore.Pipelines;

public class StreamPipe : DuplexPipe
{
    private readonly Stream _stream;

    public StreamPipe(Stream stream, int bufferSegmentSize = 512, int pauseWriterThreshold = 8192, int resumeWriterThreshold = 4096)
        : base(bufferSegmentSize, pauseWriterThreshold, resumeWriterThreshold)
    {
        _stream = stream;
    }

    protected override async Task PopulateReader(PipeWriter readerBackend)
    {
        if (readerBackend == null)
            throw new ArgumentNullException(nameof(readerBackend));

        while (true)
        {
            var memory = readerBackend.GetMemory(BufferSegmentSize);
            var bytesRead = await _stream.ReadAsync(memory).ConfigureAwait(false);
            if (bytesRead == 0)
                break;

            readerBackend.Advance(bytesRead);

            var result = await readerBackend.FlushAsync().ConfigureAwait(false);
            if (result.IsCompleted)
                break;
        }
    }

    protected override async Task PopulateWriter(PipeReader writerBackend)
    {
        if (writerBackend == null)
            throw new ArgumentNullException(nameof(writerBackend));

        while (true)
        {
            var result = await writerBackend.ReadAsync().ConfigureAwait(false);
            var buffer = result.Buffer;
            while (buffer.Length > 0)
            {
                await _stream.WriteAsync(buffer.First).ConfigureAwait(false);
                buffer = buffer.Slice(buffer.First.Length);
            }

            await _stream.FlushAsync().ConfigureAwait(false);
            writerBackend.AdvanceTo(result.Buffer.End);

            if (result.IsCompleted)
                break;
        }
    }
}