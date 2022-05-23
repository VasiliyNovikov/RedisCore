namespace RedisCore.Internal.Commands;

internal class LeftPopCommand<T> : GetValueByKeyCommand<T>
{
    public LeftPopCommand(string key)
        : base(CommandNames.LPop, key)
    {
    }
}