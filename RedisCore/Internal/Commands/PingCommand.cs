using System;
using RedisCore.Internal.Protocol;
using RedisCore.Utils;

namespace RedisCore.Internal.Commands;

internal class PingCommand : Command<TimeSpan>
{
    private readonly TimeSpan _startTime;

    public PingCommand() : base(CommandNames.Ping) => _startTime = MonotonicTime.Now;

    public override TimeSpan GetResult(RedisObject resultObject) => MonotonicTime.Now - _startTime;
}