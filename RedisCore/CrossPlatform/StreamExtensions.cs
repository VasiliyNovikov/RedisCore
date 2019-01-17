#if NETSTANDARD2_0
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RedisCore.Utils;

namespace RedisCore.CrossPlatform
{
    public static class StreamExtensions
    {
        public static async ValueTask<int> ReadAsync(this Stream stream, Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            using (var rentedBuffer = new RentedBuffer<byte>(buffer.Length))
            {
                var segment = rentedBuffer.Segment;
                var result = await stream.ReadAsync(segment.Array, segment.Offset, segment.Count, cancellationToken);
                rentedBuffer.Memory.CopyTo(buffer);
                return result;
            }
        }

        public static async ValueTask WriteAsync(this Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            using (var rentedBuffer = new RentedBuffer<byte>(buffer.Length))
            {
                buffer.CopyTo(rentedBuffer.Memory);
                var segment = rentedBuffer.Segment;
                await stream.WriteAsync(segment.Array, segment.Offset, segment.Count, cancellationToken);
            }
        }
    }
}
#endif