using System.Collections.Generic;
using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands
{
    internal class ExecCommand : Command<IReadOnlyList<RedisObject>?>
    {
        public ExecCommand()
            : base(CommandNames.Exec)
        {
        }

        public override IReadOnlyList<RedisObject>? GetResult(RedisObject resultObject)
        {
            return resultObject == RedisNull.Value 
                ? null 
                : ((RedisArray) resultObject).Items;
        }
    }
}