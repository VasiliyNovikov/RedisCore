using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal abstract class PushCommand<T>(RedisString name, string key, T value) : Command<int>(name, key.ToValue(), value.ToValue());