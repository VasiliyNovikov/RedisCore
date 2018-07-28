using System;
using System.Net;

namespace RedisCore
{
    public class RedisClientConfig
    {
        public EndPoint EndPoint { get; }

        public int BufferSize { get; set; } = 1024;

        public TimeSpan LoadingRetryDelayMin { get; set; } = TimeSpan.FromMilliseconds(20);
        
        public TimeSpan LoadingRetryDelayMax { get; set; } = TimeSpan.FromMilliseconds(200);
        
        public TimeSpan LoadingRetryTimeout { get; set; } = TimeSpan.FromSeconds(30);

        public RedisClientConfig(EndPoint endPoint)
        {
            EndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
        }
    }
}