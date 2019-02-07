using System.Collections.Generic;
using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands
{
    internal class HashGetAllCommand<T> : Command<Dictionary<string, T>>
    {
        public HashGetAllCommand(string key) 
            : base(CommandNames.HGetAll, key.ToValue())
        {
        }

        public override Dictionary<string, T> GetResult(RedisObject resultObject)
        {
            var arrayItems = ((RedisArray) resultObject).Items;
            var result = new Dictionary<string, T>(arrayItems.Count / 2);
            for (var i = 0; i < arrayItems.Count; i += 2)
            {
                var key = ((RedisValueObject) arrayItems[i]).To<string>();
                var value = ((RedisValueObject) arrayItems[i + 1]).To<T>();
                result.Add(key, value);
            }

            return result;
        }
    }
}