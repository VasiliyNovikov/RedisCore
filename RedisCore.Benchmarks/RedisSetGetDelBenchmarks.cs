using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace RedisCore.Benchmarks
{
    public class RedisSetGetDelBenchmarks : RedisBenchmarks
    {
        [Benchmark]
        public async Task OfficialClient_Set_Get_Del()
        {
            var key = Guid.NewGuid().ToString();
            var value = Guid.NewGuid().ToString();

            await OfficialClient.StringSetAsync(key, value);
            await OfficialClient.StringGetAsync(key);
            await OfficialClient.KeyDeleteAsync(key);
        }
        
        [Benchmark]
        public Task Tcp_Client_Set_Get_Del()
        {
            return Client_Set_Get_Del(TcpClient); 
        }
        
        [Benchmark]
        public Task Unix_Client_Set_Get_Del()
        {
            return Client_Set_Get_Del(UnixClient); 
        }
        
        private static async Task Client_Set_Get_Del(RedisClient client)
        {
            var key = Guid.NewGuid().ToString();
            var value = Guid.NewGuid().ToString();

            await client.Set(key, value);
            await client.Get<string>(key);
            await client.Delete(key);
        }
    }
}