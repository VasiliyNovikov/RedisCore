using System;
using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands
{
    internal class ExpireCommand : Command<bool>
    {
        public ExpireCommand(string key, TimeSpan time) 
            : base(CommandNames.PExpire, key.ToValue(), ((long)time.TotalMilliseconds).ToValue())
        {
        }
    }
}