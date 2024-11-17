using System.Collections.Generic;
using System.Linq;

namespace RedisCore.Internal.Protocol;

internal sealed class RedisArray(IEnumerable<RedisObject> items) : RedisObject
{
    public IReadOnlyList<RedisObject> Items { get; } = items.ToList().AsReadOnly();

    public RedisArray(params RedisObject[] items)
        : this((IEnumerable<RedisObject>)items)
    {
    }
}