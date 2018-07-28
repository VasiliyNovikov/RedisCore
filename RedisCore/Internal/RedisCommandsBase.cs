using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RedisCore.Internal.Commands;

namespace RedisCore.Internal
{
    public abstract class RedisCommandsBase : IRedisCommands
    {
        private protected abstract ValueTask<T> Execute<T>(Command<T> command);
        
        public async ValueTask<TimeSpan> Ping()
        {
            return await Execute(new PingCommand());
        }

        public async ValueTask<Optional<T>> Get<T>(string key)
        {
            return await Execute(new GetCommand<T>(key));
        }

        public async ValueTask<T> GetOrDefault<T>(string key, T defaultValue = default)
        {
            var result = await Get<T>(key);
            return result.HasValue ? result.Value : defaultValue;
        }

        public async ValueTask<bool> Set<T>(string key, T value)
        {
            return await Execute(new SetCommand<T>(key, value));
        }

        public async ValueTask<bool> Delete(string key)
        {
            return await Execute(new DeleteCommand(key));
        }

        public async ValueTask<bool> Expire(string key, TimeSpan time)
        {
            return await Execute(new ExpireCommand(key, time));
        }

        public async ValueTask<int> LeftPush<T>(string key, T value)
        {
            return await Execute(new LeftPushCommand<T>(key, value));
        }

        public async ValueTask<int> RightPush<T>(string key, T value)
        {
            return await Execute(new RightPushCommand<T>(key, value));
        }

        public async ValueTask<Optional<T>> LeftPop<T>(string key)
        {
            return await Execute(new LeftPopCommand<T>(key));
        }

        public async ValueTask<Optional<T>> RightPop<T>(string key)
        {
            return await Execute(new RightPopCommand<T>(key));
        }

        public async ValueTask<Optional<T>> RightPopLeftPush<T>(string source, string destination)
        {
            return await Execute(new RightPopLeftPushCommand<T>(source, destination));
        }

        public async ValueTask<Optional<T>> BlockingRightPopLeftPush<T>(string source, string destination, TimeSpan timeout)
        {
            return await Execute(new BlockingRightPopLeftPushCommand<T>(source, destination, timeout));
        }

        public async ValueTask<Optional<T>> ListIndex<T>(string key, int index)
        {
            return await Execute(new ListIndexCommand<T>(key, index));
        }

        public async ValueTask<Optional<T>> HashGet<T>(string key, string field)
        {
            return await Execute(new HashGetCommand<T>(key, field));
        }

        public async ValueTask<bool> HashSet<T>(string key, string field, T value)
        {
            return await Execute(new HashSetCommand<T>(key, field, value));
        }

        public async ValueTask<bool> HashDelete(string key, string field)
        {
            return await Execute(new HashDeleteCommand(key, field));
        }

        public async ValueTask<bool> HashExists(string key, string field)
        {
            return await Execute(new HashExistsCommand(key, field));
        }

        public async ValueTask<int> HashLength(string key)
        {
            return await Execute(new HashLenCommand(key));
        }

        public async ValueTask<IEnumerable<string>> HashKeys(string key)
        {
            return await Execute(new HashKeysCommand(key));
        }

        public async ValueTask<IEnumerable<T>> HashValues<T>(string key)
        {
            return await Execute(new HashValuesCommand<T>(key));
        }

        public async ValueTask<IEnumerable<KeyValuePair<string, T>>> HashItems<T>(string key)
        {
            return await Execute(new HashGetAllCommand<T>(key));
        }

        public async ValueTask<int> Publish<T>(string channel, T message)
        {
            return await Execute(new PublishCommand<T>(channel, message));
        }        
    }
}