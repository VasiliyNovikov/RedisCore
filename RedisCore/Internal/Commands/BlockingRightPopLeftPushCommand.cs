using System;
using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands
{
    internal class BlockingRightPopLeftPushCommand<T> : OptionalValueCommand<T>
    {
        public BlockingRightPopLeftPushCommand(string source, string destination, TimeSpan timeout) 
            : base(CommandNames.BRPopLPush, source.ToValue(), destination.ToValue(), ((int)timeout.TotalSeconds).ToValue())
        {
        }
    }
}