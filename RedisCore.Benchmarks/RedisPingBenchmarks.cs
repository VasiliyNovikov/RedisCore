using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace RedisCore.Benchmarks
{
    public class RedisPingBenchmarks : RedisBenchmarks
    {
        [Benchmark]
        public async Task Tcp_OfficialClient_Ping()
        {
            await TcpOfficialClient.PingAsync();
        }

        [Benchmark]
        public async Task Udp_OfficialClient_Ping()
        {
            await UdpOfficialClient.PingAsync();
        }

        [Benchmark]
        public async Task Tcp_Client_Ping()
        {
            await TcpClient.Ping();
        }
        
        [Benchmark]
        public async Task Unix_Client_Ping()
        {
            await UnixClient.Ping();
        }
    }
}