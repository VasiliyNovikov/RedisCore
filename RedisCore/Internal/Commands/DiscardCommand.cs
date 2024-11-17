namespace RedisCore.Internal.Commands;

internal sealed class DiscardCommand : VoidCommand
{
    public DiscardCommand()
        : base(CommandNames.Discard)
    {
    }
}