using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands
{
    internal class ListLenCommand : Command<int>
    {
        public ListLenCommand(string key) 
            : base(CommandNames.LLen, key.ToValue())
        {
        }
    }
}