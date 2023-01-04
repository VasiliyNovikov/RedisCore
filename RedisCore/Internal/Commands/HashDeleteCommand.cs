using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class HashDeleteCommand : Command<bool>
{
    public HashDeleteCommand(string key, string field) 
        : base(CommandNames.HDel, key.ToValue(), field.ToValue())
    {
    }
}