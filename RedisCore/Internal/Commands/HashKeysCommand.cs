using System.Collections.Generic;
using System.Linq;
using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands
{
    internal class HashKeysCommand : Command<IEnumerable<string>>
    {
        public HashKeysCommand(string key) 
            : base(CommandNames.HKeys, key.ToValue())
        {
        }

        public override IEnumerable<string> GetResult(RedisObject resultObject)
        {
            return ((RedisArray) resultObject).Items.Select(i => ((RedisValueObject) i).To<string>());
        }
    }
}