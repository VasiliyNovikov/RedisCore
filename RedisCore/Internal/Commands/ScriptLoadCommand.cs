using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands
{
    internal class ScriptLoadCommand : Command<string>
    {
        public ScriptLoadCommand(string script) 
            : base(CommandNames.Script, CommandNames.Load, script.ToValue())
        {
        }
    }
}