using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands
{
    internal class HashExistsCommand : IntAsBoolCommand
    {
        public HashExistsCommand(string key, string field) 
            : base(CommandNames.HExists, key.ToValue(), field.ToValue())
        {
        }
    }
}