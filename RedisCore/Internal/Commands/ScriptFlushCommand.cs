namespace RedisCore.Internal.Commands;

internal class ScriptFlushCommand : Command<string>
{
    public ScriptFlushCommand() 
        : base(CommandNames.Script, CommandNames.Flush)
    {
    }
}