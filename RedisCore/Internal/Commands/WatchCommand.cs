using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class WatchCommand : VoidCommand
{
    public WatchCommand(string key)
        : base(CommandNames.Watch, key.ToValue())
    {
    }
}