namespace RedisCore.Internal.Commands;

internal sealed class ScriptFlushCommand : Command<string>
{
    public ScriptFlushCommand()
        : base(CommandNames.Script, CommandNames.Flush)
    {
    }
}