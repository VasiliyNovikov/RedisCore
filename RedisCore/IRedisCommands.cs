﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RedisCore
{
    public interface IRedisCommands
    {
        ValueTask<TimeSpan> Ping();
        ValueTask<Optional<T>> Get<T>(string key);
        ValueTask<bool> Set<T>(string key, T value, TimeSpan? expiration = null, OptimisticConcurrency concurrency = OptimisticConcurrency.None);
        ValueTask<bool> Delete(string key);
        ValueTask<bool> Expire(string key, TimeSpan time);
        
        ValueTask<int> LeftPush<T>(string key, T value);
        ValueTask<int> RightPush<T>(string key, T value);
        ValueTask<Optional<T>> LeftPop<T>(string key);
        ValueTask<Optional<T>> RightPop<T>(string key);
        ValueTask<Optional<T>> RightPopLeftPush<T>(string source, string destination);
        ValueTask<Optional<T>> BlockingRightPopLeftPush<T>(string source, string destination, TimeSpan timeout);
        ValueTask<Optional<T>> ListIndex<T>(string key, int index);
        
        ValueTask<Optional<T>> HashGet<T>(string key, string field);
        ValueTask<bool> HashSet<T>(string key, string field, T value, OptimisticConcurrency concurrency = OptimisticConcurrency.None);
        ValueTask<bool> HashDelete(string key, string field);
        ValueTask<bool> HashExists(string key, string field);
        ValueTask<int> HashLength(string key);
        ValueTask<IEnumerable<string>> HashKeys(string key);
        ValueTask<IEnumerable<T>> HashValues<T>(string key);
        ValueTask<IEnumerable<KeyValuePair<string, T>>> HashItems<T>(string key);
        
        ValueTask<int> Publish<T>(string channel, T message);
    }

    public static class RedisCommandsExtensions
    {
        public static async ValueTask<T> GetOrDefault<T>(this IRedisCommands redis, string key, T defaultValue = default)
        {
            var result = await redis.Get<T>(key);
            return result.HasValue ? result.Value : defaultValue;
        }

        public static ValueTask<bool> Set<T>(this IRedisCommands redis, string key, T value, OptimisticConcurrency concurrency)
        {
            return redis.Set(key, value, null, concurrency);
        }
    }
}