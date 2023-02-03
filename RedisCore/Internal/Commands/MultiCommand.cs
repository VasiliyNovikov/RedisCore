namespace RedisCore.Internal.Commands;

internal sealed class MultiCommand : VoidCommand
{
    public MultiCommand() 
        : base(CommandNames.Multi)
    {
    }
}