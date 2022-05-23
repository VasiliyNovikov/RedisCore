namespace RedisCore.Internal.Commands;

internal class DiscardCommand : VoidCommand
{
    public DiscardCommand()
        : base(CommandNames.Discard)
    {
    }
}