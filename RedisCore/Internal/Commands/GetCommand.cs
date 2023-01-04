namespace RedisCore.Internal.Commands;

internal sealed class GetCommand<T> : GetValueByKeyCommand<T>
{
    public GetCommand(string key) 
        : base(CommandNames.Get, key)
    {
    }
}