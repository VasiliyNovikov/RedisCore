#if !NET6_0_OR_GREATER
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