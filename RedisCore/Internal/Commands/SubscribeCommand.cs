using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class SubscribeCommand : VoidCommand
{
    public SubscribeCommand(string channel)
        : base(CommandNames.Subscribe, channel.ToValue())
    {
    }
}