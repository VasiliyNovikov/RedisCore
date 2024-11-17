using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class ExistsCommand : Command<bool>
{
    public ExistsCommand(string key)
        : base(CommandNames.Exists, key.ToValue())
    {
    }
}