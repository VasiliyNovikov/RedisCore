#if NETSTANDARD20
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