using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal class WatchCommand : VoidCommand
{
    public WatchCommand(string key) 
        : base(CommandNames.Watch, key.ToValue())
    {
    }
}