using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class DeleteCommand(string key) : Command<bool>(CommandNames.Del, key.ToValue());