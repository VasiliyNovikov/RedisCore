using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands
{
    internal class SetCommand<T> : Command<bool>
    {
        public SetCommand(string key, T value) 
            : base(CommandNames.Set, key.ToValue(), value.ToValue())
        {
        }

        public override bool GetResult(RedisObject resultObject) => resultObject != RedisNull.Value;
    }
}