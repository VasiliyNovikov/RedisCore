using System;
using System.Threading;
using System.Threading.Tasks;
using RedisCore.Utils;

namespace RedisCore
{
    public interface ISubscription : IDisposable
    {
        ValueTask Unsubscribe();
        ValueTask<T> GetMessage<T>(CancellationToken cancellationToken = default);
        ValueTask<Memory<byte>> GetMessage(IBufferPool<byte> bufferPool, CancellationToken cancellationToken = default);
    }
}