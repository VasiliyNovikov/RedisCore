using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class ExistsCommand(string key) : Command<bool>(CommandNames.Exists, key.ToValue());