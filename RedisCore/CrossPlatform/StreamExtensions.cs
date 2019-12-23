#if !NETSTANDARD21
using System;
using System.IO;
using System.Threading.Tasks;
#if NETSTANDARD20
using System.Threading;
using RedisCore.Utils;
#endif

namespace RedisCore
{
    public static class StreamExtensions
    {
#if NETSTANDARD20
        public static async ValueTask<int> ReadAsync(this Stream stream, Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            using var rentedBuffer = new RentedBuffer<byte>(buffer.Length);
            var segment = rentedBuffer.Segment;
            var result = await stream.ReadAsync(segment.Array, segment.Offset, segment.Count, cancellationToken);
            rentedBuffer.Memory.CopyTo(buffer);
            return result;
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
#endif
        public static ValueTask DisposeAsync(this Stream stream)
        {
            try
            {
                stream.Dispose();
                return new ValueTask(Task.CompletedTask);
            }
            catch (Exception e)
            {
                return new ValueTask(Task.FromException(e));
            }
        }
    }
}
#endif