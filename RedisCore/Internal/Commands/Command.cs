using System.Linq;
using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal abstract class Command(RedisString name, params RedisObject[] args)
{
    public RedisArray Data { get; } = args.Length == 0 ? new RedisArray(name) : new RedisArray(args.Prepend(name));
}

internal abstract class Command<T>(RedisString name, params RedisObject[] args) : Command(name, args)
{
    public virtual T GetResult(RedisObject resultObject) => resultObject.To<T>();
}