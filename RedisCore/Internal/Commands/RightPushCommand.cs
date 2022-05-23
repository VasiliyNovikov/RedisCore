namespace RedisCore.Internal.Commands;

internal class RightPushCommand<T> : PushCommand<T>
{
    public RightPushCommand(string key, T value)
        : base(CommandNames.RPush, key, value)
    {
    }
}