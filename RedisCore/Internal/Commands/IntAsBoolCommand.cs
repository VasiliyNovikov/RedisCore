using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands
{
    internal abstract class IntAsBoolCommand : Command<bool>
    {
        protected IntAsBoolCommand(RedisString name, params RedisObject[] args) 
            : base(name, args)
        {
        }

        public override bool GetResult(RedisObject resultObject) => ((RedisInteger) resultObject).To<bool>();
    }
}