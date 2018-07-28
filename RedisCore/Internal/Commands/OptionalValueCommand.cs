using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands
{
    internal abstract class OptionalValueCommand<T> : Command<Optional<T>>
    {
        protected OptionalValueCommand(RedisString name, params RedisObject[] args) 
            : base(name, args)
        {
        }

        public override Optional<T> GetResult(RedisObject resultObject)
        {
            return resultObject == RedisNull.Value
                ? Optional<T>.Unspecified
                : ((RedisValueObject) resultObject).To<T>();
        }
    }
}