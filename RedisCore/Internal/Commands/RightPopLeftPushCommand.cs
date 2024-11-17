using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class RightPopLeftPushCommand<T>(string source, string destination) : OptionalValueCommand<T>(CommandNames.RPopLPush, source.ToValue(), destination.ToValue());