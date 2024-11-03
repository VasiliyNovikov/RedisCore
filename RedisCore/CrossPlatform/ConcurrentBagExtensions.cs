#if NETSTANDARD2_0
namespace System.Collections.Concurrent;

public static class ConcurrentBagExtensions
{
    public static void Clear<T>(this ConcurrentBag<T> collection)
    {
        while (collection.TryTake(out _))
        {
        }
    }
}
#endif