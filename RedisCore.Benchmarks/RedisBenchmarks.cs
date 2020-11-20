using System.Net;
using System.Net.Sockets;
using StackExchange.Redis;

namespace RedisCore.Benchmarks
{
    public class RedisBenchmarks
    {
        protected IDatabase TcpOfficialClient { get; }

        protected IDatabase UnixOfficialClient { get; }

        protected RedisClient TcpClient { get; }

        protected RedisClient TcpClientScriptCache { get; }

        protected RedisClient TcpClientStreamed { get; }
        
        protected RedisClient TcpClientNoBufferPool { get; }

        protected RedisClient UnixClient { get; }

        protected RedisClient UnixClientScriptCache { get; }

        protected RedisClient UnixClientStreamed { get; }
        
        protected RedisClient UnixClientNoBufferPool { get; }

        protected RedisBenchmarks()
        {
            var tcpEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6379);
            var unixEndPoint = new UnixDomainSocketEndPoint("/var/run/redis/redis.sock");
            
            TcpOfficialClient = ConnectionMultiplexer.Connect(new ConfigurationOptions {EndPoints = {tcpEndPoint}}).GetDatabase();
            UnixOfficialClient = ConnectionMultiplexer.Connect(new ConfigurationOptions {EndPoints = {unixEndPoint}}).GetDatabase();
            TcpClient = new RedisClient(tcpEndPoint);
            UnixClient = new RedisClient(unixEndPoint);
            TcpClientScriptCache = new RedisClient(new RedisClientConfig(tcpEndPoint){UseScriptCache = true});
            UnixClientScriptCache = new RedisClient(new RedisClientConfig(unixEndPoint){UseScriptCache = true});
            TcpClientStreamed = new RedisClient(new RedisClientConfig(tcpEndPoint){ForceUseNetworkStream = true});
            UnixClientStreamed = new RedisClient(new RedisClientConfig(unixEndPoint){ForceUseNetworkStream = true});
            TcpClientNoBufferPool = new RedisClient(new RedisClientConfig(tcpEndPoint){UseBufferPool = false});
            UnixClientNoBufferPool = new RedisClient(new RedisClientConfig(unixEndPoint){UseBufferPool = false});
        }
    }
}