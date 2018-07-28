using System.Collections.Generic;
using System.Linq;
using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands
{
    internal class HashValuesCommand<T> : Command<IEnumerable<T>>
    {
        public HashValuesCommand(string key) 
            : base(CommandNames.HVals, key.ToValue())
        {
        }

        public override IEnumerable<T> GetResult(RedisObject resultObject)
        {
            return ((RedisArray) resultObject).Items.Select(i => ((RedisValueObject) i).To<T>());
        }
    }
}