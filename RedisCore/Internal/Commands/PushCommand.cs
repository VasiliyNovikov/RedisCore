using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal abstract class PushCommand<T> : Command<int>
{
    protected PushCommand(RedisString name, string key, T value)
        : base(name, key.ToValue(), value.ToValue())
    {
    }
}