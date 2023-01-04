using System;
using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class HashSetCommand<T> : Command<bool>
{
    public HashSetCommand(string key, string field, T value, OptimisticConcurrency concurrency = OptimisticConcurrency.None) 
        : base(concurrency == OptimisticConcurrency.None ? CommandNames.HSet : CommandNames.HSetNX, key.ToValue(), field.ToValue(), value.ToValue())
    {
        if (concurrency == OptimisticConcurrency.IfExists)
            throw new NotSupportedException();
    }
}