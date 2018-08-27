using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisCore
{
    public interface ISubscription : IDisposable
    {
        ValueTask Unsubscribe();
        ValueTask<T> GetMessage<T>(CancellationToken cancellationToken = default);
    }
}