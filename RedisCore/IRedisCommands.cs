using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using RedisCore.Utils;

namespace RedisCore;

[SuppressMessage("Microsoft.Naming", "CA1716: Identifiers should not match keywords", Justification = "Applicable to VB.NET only")]
public interface IRedisCommands
{
    ValueTask<TimeSpan> Ping();
    ValueTask<Optional<T>> Get<T>(string key);
    ValueTask<bool> Set<T>(string key, T value, TimeSpan? expiration = null, OptimisticConcurrency concurrency = OptimisticConcurrency.None);
    ValueTask<bool> Delete(string key);
    ValueTask<bool> Expire(string key, TimeSpan time);
    ValueTask<bool> Exists(string key);

    ValueTask<int> LeftPush<T>(string key, T value);
    ValueTask<int> RightPush<T>(string key, T value);
    ValueTask<Optional<T>> LeftPop<T>(string key);
    ValueTask<Optional<T>> RightPop<T>(string key);
    ValueTask<Optional<T>> RightPopLeftPush<T>(string source, string destination);
    ValueTask<Optional<T>> BlockingRightPopLeftPush<T>(string source, string destination, TimeSpan timeout);
    ValueTask<Optional<T>> ListIndex<T>(string key, int index);
    ValueTask<int> ListLength(string key);

    ValueTask<Optional<T>> HashGet<T>(string key, string field);
    ValueTask<bool> HashSet<T>(string key, string field, T value, OptimisticConcurrency concurrency = OptimisticConcurrency.None);
    ValueTask<bool> HashDelete(string key, string field);
    ValueTask<bool> HashExists(string key, string field);
    ValueTask<int> HashLength(string key);
    ValueTask<HashSet<string>> HashKeys(string key);
    ValueTask<T[]> HashValues<T>(string key);
    ValueTask<Dictionary<string, T>> HashItems<T>(string key);

    ValueTask<int> Publish<T>(string channel, T message);

    ValueTask<TResult> Eval<TResult>(string script, params string[] keys);
    ValueTask<TResult> Eval<T, TResult>(string script, T arg, params string[] keys);
    ValueTask<TResult> Eval<T1, T2, TResult>(string script, T1 arg1, T2 arg2, params string[] keys);
    ValueTask<TResult> Eval<T1, T2, T3, TResult>(string script, T1 arg1, T2 arg2, T3 arg3, params string[] keys);
}

[SuppressMessage("Microsoft.Naming", "CA1716: Identifiers should not match keywords", Justification = "Applicable to VB.NET only")]
public interface IRedisBufferCommands
{
    ValueTask<Memory<byte>?> Get(string key, IBufferPool<byte> bufferPool);
    ValueTask<Memory<byte>?> LeftPop(string key, IBufferPool<byte> bufferPool);
    ValueTask<Memory<byte>?> RightPop(string key, IBufferPool<byte> bufferPool);
    ValueTask<Memory<byte>?> RightPopLeftPush(string source, string destination, IBufferPool<byte> bufferPool);
    ValueTask<Memory<byte>?> BlockingRightPopLeftPush(string source, string destination, TimeSpan timeout, IBufferPool<byte> bufferPool);
    ValueTask<Memory<byte>?> ListIndex(string key, int index, IBufferPool<byte> bufferPool);
    ValueTask<Memory<byte>?> HashGet(string key, string field, IBufferPool<byte> bufferPool);

    ValueTask<Memory<byte>?> Eval(IBufferPool<byte> bufferPool, string script, params string[] keys);
    ValueTask<Memory<byte>?> Eval<T>(IBufferPool<byte> bufferPool, string script, T arg, params string[] keys);
    ValueTask<Memory<byte>?> Eval<T1, T2>(IBufferPool<byte> bufferPool, string script, T1 arg1, T2 arg2, params string[] keys);
    ValueTask<Memory<byte>?> Eval<T1, T2, T3>(IBufferPool<byte> bufferPool, string script, T1 arg1, T2 arg2, T3 arg3, params string[] keys);
}

public static class RedisCommandsExtensions
{
    public static async ValueTask<T> GetOrDefault<T>(this IRedisCommands redis, string key, T defaultValue)
    {
        if (redis == null)
            throw new ArgumentNullException(nameof(redis));

        var result = await redis.Get<T>(key).ConfigureAwait(false);
        return result.HasValue ? result.Value : defaultValue;
    }

    public static ValueTask<T?> GetOrDefault<T>(this IRedisCommands redis, string key) => GetOrDefault<T?>(redis, key, default);

    public static ValueTask<bool> Set<T>(this IRedisCommands redis, string key, T value, OptimisticConcurrency concurrency)
    {
#pragma warning disable IDE0046 // Use conditional expression for return
        if (redis == null)
            throw new ArgumentNullException(nameof(redis));

        return redis.Set(key, value, null, concurrency);
#pragma warning restore IDE0046
    }
}