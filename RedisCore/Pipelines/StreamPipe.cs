using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
#if NETSTANDARD
using RedisCore.CrossPlatform;
#endif

namespace RedisCore.Pipelines
{
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
            while (true)
            {
                var memory = readerBackend.GetMemory(BufferSegmentSize);
                var bytesRead = await _stream.ReadAsync(memory);
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
                    await _stream.WriteAsync(buffer.First);
                    buffer = buffer.Slice(buffer.First.Length);
                }

                await _stream.FlushAsync();
                writerBackend.AdvanceTo(result.Buffer.End);
               
                if (result.IsCompleted)
                    break;
            }
        }
    }
}