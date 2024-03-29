using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class ListIndexCommand<T> : OptionalValueCommand<T>
{
    public ListIndexCommand(string key, int index) 
        : base(CommandNames.LIndex, key.ToValue(), index.ToValue())
    {
    }
}