using System;
using System.Threading.Tasks;

namespace RedisCore
{
    public interface IRedisTransaction : IRedisCommands, IDisposable
    {
        void Watch(string key);
        ValueTask<bool> Complete();
    }
}