using System.Linq;
using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal abstract class Command
{
    public RedisArray Data { get; }

    protected Command(RedisString name, params RedisObject[] args)
    {
        Data = args.Length == 0
            ? new RedisArray(name)
            : new RedisArray(args.Prepend(name));
    }
}

internal abstract class Command<T> : Command
{
    protected Command(RedisString name, params RedisObject[] args)
        : base(name, args)
    {
    }

    public virtual T GetResult(RedisObject resultObject) => resultObject.To<T>();
}