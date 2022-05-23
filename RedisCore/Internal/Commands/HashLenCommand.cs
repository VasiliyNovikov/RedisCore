using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal class HashLenCommand : Command<int>
{
    public HashLenCommand(string key)
        : base(CommandNames.HLen, key.ToValue())
    {
    }
}