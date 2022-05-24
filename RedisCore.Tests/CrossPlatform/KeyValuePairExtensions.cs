#if !NETCOREAPP3_1_OR_GREATER
using System.Collections.Generic;
using System.ComponentModel;

namespace RedisCore.Tests;

public static class KeyValuePairExtensions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Deconstruct<TKey, TValue>(this in KeyValuePair<TKey, TValue> keyValuePair, out TKey key, out TValue value)
    {
        key = keyValuePair.Key;
        value = keyValuePair.Value;
    }
}
#endif