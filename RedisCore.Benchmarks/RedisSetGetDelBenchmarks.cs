﻿using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using StackExchange.Redis;

namespace RedisCore.Benchmarks
{
    public class RedisSetGetDelBenchmarks : RedisBenchmarks
    {
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
        public Task Unix_Client_Set_Get_Del()
        {
            return Client_Set_Get_Del(UnixClient); 
        }

        [Benchmark]
        public Task Unix_Client_Streamed_Set_Get_Del()
        {
            return Client_Set_Get_Del(UnixClientStreamed); 
        }

        private static async Task Client_Set_Get_Del(IDatabase client)
        {
            var key = Guid.NewGuid().ToString();
            var value = Guid.NewGuid().ToString();

            await client.StringSetAsync(key, value);
            await client.StringGetAsync(key);
            await client.KeyDeleteAsync(key);
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