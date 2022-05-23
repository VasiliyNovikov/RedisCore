using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal class HashExistsCommand : Command<bool>
{
    public HashExistsCommand(string key, string field) 
        : base(CommandNames.HExists, key.ToValue(), field.ToValue())
    {
    }
}