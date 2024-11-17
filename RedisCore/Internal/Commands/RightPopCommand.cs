namespace RedisCore.Internal.Commands;

internal sealed class RightPopCommand<T> : GetValueByKeyCommand<T>
{
    public RightPopCommand(string key)
        : base(CommandNames.RPop, key)
    {
    }
}