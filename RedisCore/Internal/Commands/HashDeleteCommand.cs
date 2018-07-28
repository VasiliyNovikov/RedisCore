using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands
{
    internal class HashDeleteCommand : IntAsBoolCommand
    {
        public HashDeleteCommand(string key, string field) 
            : base(CommandNames.HDel, key.ToValue(), field.ToValue())
        {
        }
    }
}