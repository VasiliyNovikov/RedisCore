using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class ListLenCommand(string key) : Command<int>(CommandNames.LLen, key.ToValue());