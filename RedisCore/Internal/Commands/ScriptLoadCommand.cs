using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class ScriptLoadCommand(string script) : Command<string>(CommandNames.Script, CommandNames.Load, script.ToValue());