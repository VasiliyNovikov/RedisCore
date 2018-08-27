using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands
{
    internal class SubscribeCommand : VoidCommand
    {
        public SubscribeCommand(string channel)
            : base(CommandNames.Subscribe, channel.ToValue())
        {
        }
    }
}