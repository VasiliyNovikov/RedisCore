using System;
using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands
{
    internal class PingCommand : Command<TimeSpan>
    {
        private readonly DateTime _startTime;

        public PingCommand() : base(CommandNames.Ping) => _startTime = DateTime.UtcNow;

        public override TimeSpan GetResult(RedisObject resultObject) => DateTime.UtcNow - _startTime;
    }
}