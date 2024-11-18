using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class HashExistsCommand(string key, string field) : Command<bool>(CommandNames.HExists, key.ToValue(), field.ToValue());