using System.Collections.Generic;
using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands
{
    internal class HashGetAllCommand<T> : Command<IEnumerable<KeyValuePair<string, T>>>
    {
        public HashGetAllCommand(string key) 
            : base(CommandNames.HGetAll, key.ToValue())
        {
        }

        public override IEnumerable<KeyValuePair<string, T>> GetResult(RedisObject resultObject)
        {
            var arrayItems = ((RedisArray) resultObject).Items;
            for (var i = 0; i < arrayItems.Count; i += 2)
            {
                var key = ((RedisValueObject) arrayItems[i]).To<string>();
                var value = ((RedisValueObject) arrayItems[i + 1]).To<T>();
                yield return new KeyValuePair<string, T>(key, value);
            }
        }
    }
}