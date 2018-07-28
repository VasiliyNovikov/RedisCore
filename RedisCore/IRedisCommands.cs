using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RedisCore.Utils;

namespace RedisCore
{
    public interface IRedisCommands
    {
        ValueTask<TimeSpan> Ping();
        ValueTask<Optional<T>> Get<T>(string key);
        ValueTask<T> GetOrDefault<T>(string key, T defaultValue = default);
        ValueTask<bool> Set<T>(string key, T value);
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
        ValueTask<bool> HashSet<T>(string key, string field, T value);
        ValueTask<bool> HashDelete(string key, string field);
        ValueTask<bool> HashExists(string key, string field);
        ValueTask<int> HashLength(string key);
        ValueTask<IEnumerable<string>> HashKeys(string key);
        ValueTask<IEnumerable<T>> HashValues<T>(string key);
        ValueTask<IEnumerable<KeyValuePair<string, T>>> HashItems<T>(string key);
        
        ValueTask<int> Publish<T>(string channel, T message);
    }
}