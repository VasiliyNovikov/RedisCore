using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal abstract class GetValueByKeyCommand<T>(RedisString name, string key) : OptionalValueCommand<T>(name, key.ToValue());