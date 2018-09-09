using System.Net;
using System.Net.Sockets;
using StackExchange.Redis;

namespace RedisCore.Benchmarks
{
    public class RedisBenchmarks
    {
        protected IDatabase TcpOfficialClient { get; }

        protected IDatabase UdpOfficialClient { get; }

        protected RedisClient TcpClient { get; }

        protected RedisClient UnixClient { get; }

        public RedisBenchmarks()
        {
            var tcpEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6379);
            //var tcpEndPoint = new IPEndPoint(IPAddress.Parse("192.168.0.64"), 6379);
            var unixEndPoint = new UnixDomainSocketEndPoint("/var/run/redis/redis.sock");
            
            TcpOfficialClient = ConnectionMultiplexer.Connect(new ConfigurationOptions {EndPoints = {tcpEndPoint}}).GetDatabase();
            UdpOfficialClient = ConnectionMultiplexer.Connect(new ConfigurationOptions {EndPoints = {unixEndPoint}}).GetDatabase();
            TcpClient = new RedisClient(tcpEndPoint);
            UnixClient = new RedisClient(unixEndPoint);
        }
    }
}