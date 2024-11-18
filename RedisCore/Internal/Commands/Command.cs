using RedisCore.Internal.Protocol;
using RedisCore.Utils;
using System;

namespace RedisCore.Internal.Commands;

internal abstract class Command
{
    public RedisArray Data { get; }

    public Command(RedisString name, params ReadOnlySpan<RedisObject> args)
    {
        if (args.Length == 0)
            Data = new RedisArray(name);
        else
        {
            using var itemsBuffer = new RentedBuffer<RedisObject>(args.Length + 1);
            var items = itemsBuffer.Span;
            items[0] = name;
            args.CopyTo(items[1..]);
            Data = new RedisArray(items);
        }
    }
}

internal abstract class Command<T>(RedisString name, params ReadOnlySpan<RedisObject> args) : Command(name, args)
{
    public virtual T GetResult(RedisObject resultObject) => resultObject.To<T>();
}