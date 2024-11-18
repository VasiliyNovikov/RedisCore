using System;

namespace RedisCore.Internal.Protocol;

internal sealed class RedisArray : RedisObject
{
    public RedisObject[] Items { get; }

    public RedisArray(params ReadOnlySpan<RedisObject> items) => Items = items.ToArray();
}