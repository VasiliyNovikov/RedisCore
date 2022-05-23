using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal abstract class GetValueByKeyCommand<T> : OptionalValueCommand<T>
{
    protected GetValueByKeyCommand(RedisString name, string key) 
        : base(name, key.ToValue())
    {
    }
}