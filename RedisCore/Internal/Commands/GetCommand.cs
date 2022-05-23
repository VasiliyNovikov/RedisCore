namespace RedisCore.Internal.Commands;

internal class GetCommand<T> : GetValueByKeyCommand<T>
{
    public GetCommand(string key)
        : base(CommandNames.Get, key)
    {
    }
}