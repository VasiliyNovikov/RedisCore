using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal class RightPopLeftPushCommand<T> : OptionalValueCommand<T>
{
    public RightPopLeftPushCommand(string source, string destination)
        : base(CommandNames.RPopLPush, source.ToValue(), destination.ToValue())
    {
    }
}