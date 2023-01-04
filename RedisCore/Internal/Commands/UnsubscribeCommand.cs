namespace RedisCore.Internal.Commands;

internal sealed class UnsubscribeCommand : VoidCommand
{
    public UnsubscribeCommand()
        : base(CommandNames.Unsubscribe)
    {
    }
}