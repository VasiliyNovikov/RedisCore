using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands
{
    internal class HashSetCommand<T> : IntAsBoolCommand
    {
        public HashSetCommand(string key, string field, T value) 
            : base(CommandNames.HSet, key.ToValue(), field.ToValue(), value.ToValue())
        {
        }
    }
}