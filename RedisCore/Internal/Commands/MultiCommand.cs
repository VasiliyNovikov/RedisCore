namespace RedisCore.Internal.Commands;

internal class MultiCommand : VoidCommand
{
    public MultiCommand()
        : base(CommandNames.Multi)
    {
    }
}