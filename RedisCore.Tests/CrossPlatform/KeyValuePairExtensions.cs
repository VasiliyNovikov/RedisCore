#if !NET6_0_OR_GREATER
using System.ComponentModel;

namespace System.Collections.Generic;

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