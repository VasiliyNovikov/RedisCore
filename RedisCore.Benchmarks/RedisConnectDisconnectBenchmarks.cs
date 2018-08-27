using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using StackExchange.Redis;

namespace RedisCore.Benchmarks
{
    public class RedisConnectDisconnectBenchmarks
    {
        [Benchmark]
        public async Task Tcp_OfficialClient_Connect_Ping_Disconnect()
        {
            var tcpEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6379);
            using (var client = ConnectionMultiplexer.Connect(new ConfigurationOptions {EndPoints = {tcpEndPoint}}))
                await client.GetDatabase().PingAsync();
        }

        [Benchmark]
        public async Task Unix_OfficialClient_Connect_Ping_Disconnect()
        {
            var unixEndPoint = new UnixDomainSocketEndPoint("/var/run/redis/redis.sock");
            using (var client = ConnectionMultiplexer.Connect(new ConfigurationOptions {EndPoints = {unixEndPoint}}))
                await client.GetDatabase().PingAsync();
        }

        [Benchmark]
        public async Task Tcp_Client_Connect_Ping_Disconnect()
        {
            var tcpEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6379);
            using (var client = new RedisClient(tcpEndPoint))
                await client.Ping();
        }
        
        [Benchmark]
        public async Task Unix_Client_Connect_Ping_Disconnect()
        {
            var unixEndPoint = new UnixDomainSocketEndPoint("/var/run/redis/redis.sock");
            using (var client = new RedisClient(unixEndPoint))
                await client.Ping();
        }
    }
}