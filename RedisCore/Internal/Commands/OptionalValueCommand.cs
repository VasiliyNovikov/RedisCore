using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal abstract class OptionalValueCommand<T>(RedisString name, params RedisObject[] args) : Command<Optional<T>>(name, args);