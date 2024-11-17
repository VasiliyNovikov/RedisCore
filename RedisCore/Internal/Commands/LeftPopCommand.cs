namespace RedisCore.Internal.Commands;

internal sealed class LeftPopCommand<T> : GetValueByKeyCommand<T>
{
    public LeftPopCommand(string key)
        : base(CommandNames.LPop, key)
    {
    }
}