using System;
using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class BlockingRightPopLeftPushCommand<T>(string source, string destination, TimeSpan timeout)
    : OptionalValueCommand<T>(CommandNames.BRPopLPush, source.ToValue(), destination.ToValue(), ((int)timeout.TotalSeconds).ToValue());