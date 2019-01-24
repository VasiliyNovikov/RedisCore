using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using StackExchange.Redis;

namespace RedisCore.Benchmarks
{
    public class RedisSetGetDelBenchmarks : RedisBenchmarks
    {
        private readonly byte[] _value;
        
        protected virtual int ValueLength => 512;

        public RedisSetGetDelBenchmarks()
        {
            _value = new byte[ValueLength];
            new Random().NextBytes(_value);
        }
        
        [Benchmark]
        public Task Tcp_OfficialClient_Set_Get_Del()
        {
            return Client_Set_Get_Del(TcpOfficialClient); 
        }

        [Benchmark]
        public Task Unix_OfficialClient_Set_Get_Del()
        {
            return Client_Set_Get_Del(UnixOfficialClient); 
        }

        [Benchmark]
        public Task Tcp_Client_Set_Get_Del()
        {
            return Client_Set_Get_Del(TcpClient); 
        }

        [Benchmark]
        public Task Tcp_Client_Streamed_Set_Get_Del()
        {
            return Client_Set_Get_Del(TcpClientStreamed); 
        }

        [Benchmark]
        public Task Tcp_Client_NoBufferPool_Set_Get_Del()
        {
            return Client_Set_Get_Del(TcpClientNoBufferPool); 
        }

        [Benchmark]
        public Task Unix_Client_Set_Get_Del()
        {
            return Client_Set_Get_Del(UnixClient); 
        }

        [Benchmark]
        public Task Unix_Client_Streamed_Set_Get_Del()
        {
            return Client_Set_Get_Del(UnixClientStreamed); 
        }

        [Benchmark]
        public Task Unix_Client_NoBufferPool_Set_Get_Del()
        {
            return Client_Set_Get_Del(UnixClientNoBufferPool); 
        }

        private async Task Client_Set_Get_Del(IDatabase client)
        {
            var key = Guid.NewGuid().ToString();

            await client.StringSetAsync(key, _value);
            await client.StringGetAsync(key);
            await client.KeyDeleteAsync(key);
        }

        private async Task Client_Set_Get_Del(RedisClient client)
        {
            var key = Guid.NewGuid().ToString();

            await client.Set(key, _value);
            await client.Get<byte[]>(key);
            await client.Delete(key);
        }
    }
}