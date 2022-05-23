using System.Collections.Generic;
using System.Linq;

namespace RedisCore.Internal.Protocol;

internal sealed class RedisArray : RedisObject
{
    public IReadOnlyList<RedisObject> Items { get; }

    public RedisArray(IEnumerable<RedisObject> items) => Items = items.ToList().AsReadOnly();

    public RedisArray(params RedisObject[] items) 
        : this((IEnumerable<RedisObject>) items)
    {
    }
}