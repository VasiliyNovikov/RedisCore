using System;
using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands;

internal sealed class ExpireCommand(string key, TimeSpan time)
    : Command<bool>(CommandNames.PExpire, key.ToValue(), ((long)time.TotalMilliseconds).ToValue());