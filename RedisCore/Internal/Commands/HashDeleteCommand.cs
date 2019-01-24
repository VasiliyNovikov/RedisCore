using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands
{
    internal class HashDeleteCommand : Command<bool>
    {
        public HashDeleteCommand(string key, string field) 
            : base(CommandNames.HDel, key.ToValue(), field.ToValue())
        {
        }
    }
}