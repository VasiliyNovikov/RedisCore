using RedisCore.Internal.Protocol;
using System;

namespace RedisCore.Internal.Commands;

internal abstract class OptionalValueCommand<T>(RedisString name, params ReadOnlySpan<RedisObject> args) : Command<Optional<T>>(name, args);