using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands
{
    internal class ExistsCommand : Command<bool>
    {
        public ExistsCommand(string key) 
            : base(CommandNames.Exists, key.ToValue())
        {
        }
    }
}