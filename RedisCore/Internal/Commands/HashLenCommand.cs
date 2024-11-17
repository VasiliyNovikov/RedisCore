using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class HashLenCommand : Command<int>
{
    public HashLenCommand(string key)
        : base(CommandNames.HLen, key.ToValue())
    {
    }
}