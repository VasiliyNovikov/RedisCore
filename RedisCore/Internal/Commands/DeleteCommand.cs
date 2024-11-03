using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class DeleteCommand : Command<bool>
{
    public DeleteCommand(string key)
        : base(CommandNames.Del, key.ToValue())
    {
    }
}