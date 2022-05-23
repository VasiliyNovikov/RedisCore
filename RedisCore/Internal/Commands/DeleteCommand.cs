using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal class DeleteCommand : Command<bool>
{
    public DeleteCommand(string key) 
        : base(CommandNames.Del, key.ToValue())
    {
    }
}