using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RedisCore.Internal.Commands;
using RedisCore.Utils;

namespace RedisCore.Internal
{
    public abstract class RedisCommandsBase : IRedisCommands, IRedisBufferCommands
    {
        private protected abstract ValueTask<T> Execute<T>(Command<T> command);
        
        private protected abstract ValueTask<Memory<byte>?> Execute<TCommand>(TCommand command, IBufferPool<byte> bufferPool) where TCommand : Command<Optional<byte[]>>;
        
        #region IRedisCommands
        
        public async ValueTask<TimeSpan> Ping()
        {
            return await Execute(new PingCommand());
        }

        public async ValueTask<Optional<T>> Get<T>(string key)
        {
            return await Execute(new GetCommand<T>(key));
        }

        public async ValueTask<bool> Set<T>(string key, T value, TimeSpan? expiration = null, OptimisticConcurrency concurrency = OptimisticConcurrency.None)
        {
            return await Execute(new SetCommand<T>(key, value, expiration, concurrency));
        }

        public async ValueTask<bool> Delete(string key)
        {
            return await Execute(new DeleteCommand(key));
        }

        public async ValueTask<bool> Expire(string key, TimeSpan time)
        {
            return await Execute(new ExpireCommand(key, time));
        }

        public async ValueTask<bool> Exists(string key)
        {
            return await Execute(new ExistsCommand(key));
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

        public async ValueTask<int> ListLength(string key)
        {
            return await Execute(new ListLenCommand(key));
        }

        public async ValueTask<Optional<T>> HashGet<T>(string key, string field)
        {
            return await Execute(new HashGetCommand<T>(key, field));
        }

        public async ValueTask<bool> HashSet<T>(string key, string field, T value, OptimisticConcurrency concurrency = OptimisticConcurrency.None)
        {
            return await Execute(new HashSetCommand<T>(key, field, value, concurrency));
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

        public async ValueTask<HashSet<string>> HashKeys(string key)
        {
            return await Execute(new HashKeysCommand(key));
        }

        public async ValueTask<T[]> HashValues<T>(string key)
        {
            return await Execute(new HashValuesCommand<T>(key));
        }

        public async ValueTask<Dictionary<string, T>> HashItems<T>(string key)
        {
            return await Execute(new HashGetAllCommand<T>(key));
        }

        public async ValueTask<int> Publish<T>(string channel, T message)
        {
            return await Execute(new PublishCommand<T>(channel, message));
        }

        public async ValueTask<TResult> Eval<TResult>(string script, params string[] keys)
        {
            return await Execute(EvalCommand<TResult>.Create(script, keys));
        }

        public async ValueTask<TResult> Eval<T, TResult>(string script, T arg, params string[] keys)
        {
            return await Execute(EvalCommand<TResult>.Create(script, arg, keys));
        }

        public async ValueTask<TResult> Eval<T1, T2, TResult>(string script, T1 arg1, T2 arg2, params string[] keys)
        {
            return await Execute(EvalCommand<TResult>.Create(script, arg1, arg2, keys));
        }

        public async ValueTask<TResult> Eval<T1, T2, T3, TResult>(string script, T1 arg1, T2 arg2, T3 arg3, params string[] keys)
        {
            return await Execute(EvalCommand<TResult>.Create(script, arg1, arg2, arg3, keys));
        }

        #endregion IRedisCommands

        #region IRedisBufferCommands
        
        public async ValueTask<Memory<byte>?> Get(string key, IBufferPool<byte> bufferPool)
        {
            return await Execute(new GetCommand<byte[]>(key), bufferPool);
        }

        public async ValueTask<Memory<byte>?> LeftPop<T>(string key, IBufferPool<byte> bufferPool)
        {
            return await Execute(new LeftPopCommand<byte[]>(key), bufferPool);
        }

        public async ValueTask<Memory<byte>?> RightPop<T>(string key, IBufferPool<byte> bufferPool)
        {
            return await Execute(new RightPopCommand<byte[]>(key), bufferPool);
        }

        public async ValueTask<Memory<byte>?> RightPopLeftPush<T>(string source, string destination, IBufferPool<byte> bufferPool)
        {
            return await Execute(new RightPopLeftPushCommand<byte[]>(source, destination), bufferPool);
        }

        public async ValueTask<Memory<byte>?> BlockingRightPopLeftPush<T>(string source, string destination, TimeSpan timeout, IBufferPool<byte> bufferPool)
        {
            return await Execute(new BlockingRightPopLeftPushCommand<byte[]>(source, destination, timeout), bufferPool);
        }

        public async ValueTask<Memory<byte>?> ListIndex<T>(string key, int index, IBufferPool<byte> bufferPool)
        {
            return await Execute(new ListIndexCommand<byte[]>(key, index), bufferPool);
        }

        public async ValueTask<Memory<byte>?> Eval(IBufferPool<byte> bufferPool, string script, params string[] keys)
        {
            return await Execute(EvalCommand<Optional<byte[]>>.Create(script, keys), bufferPool);
        }

        public async ValueTask<Memory<byte>?> Eval<T>(IBufferPool<byte> bufferPool, string script, T arg, params string[] keys)
        {
            return await Execute(EvalCommand<Optional<byte[]>>.Create(script, arg, keys), bufferPool);
        }

        public async ValueTask<Memory<byte>?> Eval<T1, T2>(IBufferPool<byte> bufferPool, string script, T1 arg1, T2 arg2, params string[] keys)
        {
            return await Execute(EvalCommand<Optional<byte[]>>.Create(script, arg1, arg2, keys), bufferPool);
        }

        public async ValueTask<Memory<byte>?> Eval<T1, T2, T3>(IBufferPool<byte> bufferPool, string script, T1 arg1, T2 arg2, T3 arg3, params string[] keys)
        {
            return await Execute(EvalCommand<Optional<byte[]>>.Create(script, arg1, arg2, arg3, keys), bufferPool);
        }

        #endregion IRedisBufferCommands
    }
}