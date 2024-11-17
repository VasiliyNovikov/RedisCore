namespace RedisCore.Internal.Commands;

internal sealed class RightPushCommand<T> : PushCommand<T>
{
    public RightPushCommand(string key, T value)
        : base(CommandNames.RPush, key, value)
    {
    }
}