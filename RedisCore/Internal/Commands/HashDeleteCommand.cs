using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class HashDeleteCommand(string key, string field) : Command<bool>(CommandNames.HDel, key.ToValue(), field.ToValue());