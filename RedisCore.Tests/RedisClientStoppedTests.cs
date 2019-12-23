using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RedisCore.Tests
{
    [TestClass]
    public class RedisClientStoppedTests : RedisClientTestsBase
    {
        private static async Task StopRedis()
        {
            await ExecUtil.Command("redis-cli", "save");
            await ExecUtil.Command("sudo", "service redis-server stop");
        }
        
        private static async Task StartRedis()
        {
            await Task.Delay(TimeSpan.FromSeconds(2)); // Delay is needed otherwise Redis sometimes doesn't want to start and says "redis-server.service: Start request repeated too quickly."
            await ExecUtil.Command("sudo", "service redis-server start");
        }
        
        [TestMethod]
        [DynamicData(nameof(Local_Test_Endpoints_Data), typeof(RedisClientTestsBase), DynamicDataSourceType.Method)]
        public async Task RedisClient_Stopped_Ping_Test(RedisClientConfig config)
        {
            if (!HasLocalRedis) // Workaround. Need to figure out proper way to execute it conditionally
                return;

            using var client = new RedisClient(config);
            await client.Ping();

            await StopRedis();
            try
            {
                for (var i = 0; i < 3; ++i)
                {
                    try
                    {
                        await client.Ping();
                        Assert.Fail($"{typeof(RedisConnectionException)} expected");
                    }
                    catch (RedisConnectionException)
                    {
                    }
                }
            }
            finally
            {
                await StartRedis();
            }

            await client.Ping();
        }
        
        [TestMethod]
        [DynamicData(nameof(Local_Test_Endpoints_Data), typeof(RedisClientTestsBase), DynamicDataSourceType.Method)]
        public async Task RedisClient_Stopped_PubSub_Receive_Test(RedisClientConfig config)
        {
            if (!HasLocalRedis) // Workaround. Need to figure out proper way to execute it conditionally
                return;

            await using (var client = new RedisClient(config))
            {
                for (var i = 0; i < 2; i++)
                {
                    var testChannel = UniqueString();
                    await using var subscription = await client.Subscribe(testChannel);
                    await StopRedis();
                    try
                    {
                        for (var j = 0; j < 2; ++j)
                        {
                            try
                            {
                                await subscription.GetMessage<string>();
                                Assert.Fail($"{typeof(RedisConnectionException)} expected");
                            }
                            catch (RedisConnectionException)
                            {
                            }
                        }
                    }
                    finally
                    {
                        await StartRedis();
                    }
                }
            }
        }
    }
}