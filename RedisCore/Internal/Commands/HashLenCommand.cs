using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class HashLenCommand(string key) : Command<int>(CommandNames.HLen, key.ToValue());