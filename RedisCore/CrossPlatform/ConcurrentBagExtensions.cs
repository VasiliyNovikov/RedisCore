#if !NETCOREAPP3_1_OR_GREATER
using System.Collections.Concurrent;

namespace RedisCore
{
    public static class ConcurrentBagExtensions
    {
        public static void Clear<T>(this ConcurrentBag<T> collection)
        {
            while (collection.TryTake(out _))
            {
            }
        }
    }
}
#endif