namespace RedisCore.Internal.Commands;

internal sealed class LeftPushCommand<T> : PushCommand<T>
{
    public LeftPushCommand(string key, T value)
        : base(CommandNames.LPush, key, value)
    {
    }
}