using System.Linq;
using RedisCore.Internal.Protocol;

namespace RedisCore.Internal.Commands
{
    internal abstract class Command<T>
    {
        public RedisArray Data { get; }

        protected Command(RedisString name, params RedisObject[] args)
        {
            Data = args.Length == 0
                ? new RedisArray(name)
                : new RedisArray(args.Prepend(name));
        }

        public abstract T GetResult(RedisObject resultObject);
    }
}