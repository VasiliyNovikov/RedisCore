using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands
{
    internal class RightPopLeftPushCommand<T> : OptionalValueCommand<T>
    {
        public RightPopLeftPushCommand(string source, string destination) 
            : base(CommandNames.RPopLPush, source.ToValue(), destination.ToValue())
        {
        }
    }
    
    internal class ListIndexCommand<T> : OptionalValueCommand<T>
    {
        public ListIndexCommand(string key, int index) 
            : base(CommandNames.LIndex, key.ToValue(), index.ToValue())
        {
        }
    }
}