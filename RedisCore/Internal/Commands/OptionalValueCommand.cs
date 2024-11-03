using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal abstract class OptionalValueCommand<T> : Command<Optional<T>>
{
    protected OptionalValueCommand(RedisString name, params RedisObject[] args)
        : base(name, args)
    {
    }
}