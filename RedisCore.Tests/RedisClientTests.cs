using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RedisCore.Tests
{
    [TestClass]
    public class RedisClientTests
    {
        private static IEnumerable<RedisClientConfig>  LocalTestConfigs()
        {
            yield return new RedisClientConfig("127.0.0.1");
            //yield return new RedisClientConfig("192.168.0.64");
            yield return new RedisClientConfig(new UnixDomainSocketEndPoint("/var/run/redis/redis.sock"));

        }

        private static IEnumerable<RedisClientConfig> TestConfigs()
        {
            foreach (var config in LocalTestConfigs())
                yield return config;

            if (Environment.GetEnvironmentVariable("TF_BUILD") == null)
                yield break;

            var host = Environment.GetEnvironmentVariable("AZURE_REDIS_HOST");
            var password = Environment.GetEnvironmentVariable("AZURE_REDIS_PWD");

            Assert.IsNotNull(host, "Host is missing");
            Assert.IsNotNull(password, "Password is missing");

            yield return new RedisClientConfig(host, true) {Password = password};
        }
        
        private static IEnumerable<object[]> Local_Test_Endpoints_Data() => LocalTestConfigs().Select(cfg => new object[] {cfg});
        private static IEnumerable<object[]> Test_Endpoints_Data() => TestConfigs().Select(cfg => new object[] {cfg});

        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), DynamicDataSourceType.Method)]
        public async Task RedisClient_Ping_Test(RedisClientConfig config)
        {
            for (var i = 0; i < 4; ++i)
            {
                using (var client = new RedisClient(config))
                {
                    for (var j = 0; j < 8; ++j)
                    {
                        var start = DateTime.UtcNow;
                        var rtt = await client.Ping();
                        var testRtt = DateTime.UtcNow - start;
                        Assert.IsTrue(rtt > TimeSpan.Zero);
                        Assert.IsTrue(testRtt > rtt);
                    }
                }
            }
        }
        
        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), DynamicDataSourceType.Method)]
        public async Task RedisClient_Get_Set_Delete_Test(RedisClientConfig config)
        {
            for (var i = 0; i < 4; ++i)
            {
                using (var client = new RedisClient(config))
                {
                    for (var j = 0; j < 8; ++j)
                    {
                        var testKey = Guid.NewGuid().ToString();
                        var testValue = Guid.NewGuid().ToString();

                        Assert.IsNull(await client.GetOrDefault<string>(testKey));

                        await client.Set(testKey, testValue);
                        Assert.AreEqual(testValue, await client.Get<string>(testKey));

                        await client.Delete(testKey);
                        Assert.IsNull(await client.GetOrDefault<string>(testKey));
                    }
                }
            }
        }

        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), DynamicDataSourceType.Method)]
        public async Task RedisClient_Set_Expire_Test(RedisClientConfig config)
        {
            var expireTime = TimeSpan.FromSeconds(0.5);
            using (var client = new RedisClient(config))
            {
                var testKey = Guid.NewGuid().ToString();
                var testValue = Guid.NewGuid().ToString();

                Assert.IsFalse(await client.Expire(testKey, expireTime));

                Assert.IsTrue(await client.Set(testKey, testValue));
                Assert.IsTrue(await client.Expire(testKey, expireTime));
                Assert.AreEqual(testValue, await client.Get<string>(testKey));

                await Task.Delay(expireTime * 1.5);

                Assert.IsNull(await client.GetOrDefault<string>(testKey));
            }
        }

        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), DynamicDataSourceType.Method)]
        public async Task RedisClient_Integer_Get_Set_Delete_Test(RedisClientConfig config)
        {
            for (var i = 0; i < 4; ++i)
            {
                using (var client = new RedisClient(config))
                {
                    for (var j = 0; j < 8; ++j)
                    {
                        var testKey = Guid.NewGuid().ToString();
                        const int testValue = 42;

                        Assert.IsNull(await client.GetOrDefault<int?>(testKey));

                        await client.Set(testKey, testValue);
                        Assert.AreEqual(testValue, await client.Get<int>(testKey));

                        await client.Delete(testKey);
                        Assert.IsNull(await client.GetOrDefault<int?>(testKey));
                    }
                }
            }
        }
        
        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), DynamicDataSourceType.Method)]
        public async Task RedisClient_Transaction_Get_Set_Test(RedisClientConfig config)
        {
            for (var i = 0; i < 4; ++i)
            {
                using (var client = new RedisClient(config))
                {
                    for (var j = 0; j < 8; ++j)
                    {
                        var testKey = Guid.NewGuid().ToString();
                        var testValue = Guid.NewGuid().ToString();

                        using (var transaction = client.CreateTransaction())
                        {
                            var getEmptyTask = transaction.GetOrDefault<string>(testKey);
                            var setTask = transaction.Set(testKey, testValue);
                            var getTask = transaction.Get<string>(testKey);
                           
                            Assert.IsTrue(await transaction.Complete());
                            
                            Assert.IsNull(await getEmptyTask);
                            Assert.IsTrue(await setTask);
                            Assert.AreEqual(testValue, await getTask);
                        }

                        Assert.AreEqual(testValue, await client.Get<string>(testKey));

                        await client.Delete(testKey);
                        Assert.IsNull(await client.GetOrDefault<int?>(testKey));
                    }
                }
            }
        }
        
        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), DynamicDataSourceType.Method)]
        public async Task RedisClient_Transaction_Get_Set_Discard_Test(RedisClientConfig config)
        {
            for (var i = 0; i < 4; ++i)
            {
                using (var client = new RedisClient(config))
                {
                    for (var j = 0; j < 8; ++j)
                    {
                        var testKey = Guid.NewGuid().ToString();
                        var testValue = Guid.NewGuid().ToString();

                        ValueTask<string> getEmptyTask;
                        ValueTask<bool> setTask;
                        ValueTask<Optional<string>> getTask;
                        using (var transaction = client.CreateTransaction())
                        {
                            getEmptyTask = transaction.GetOrDefault<string>(testKey);
                            setTask = transaction.Set(testKey, testValue);
                            getTask = transaction.Get<string>(testKey);
                        }

                        await Assert.ThrowsExceptionAsync<TaskCanceledException>(getEmptyTask.AsTask);
                        await Assert.ThrowsExceptionAsync<TaskCanceledException>(setTask.AsTask);
                        await Assert.ThrowsExceptionAsync<TaskCanceledException>(getTask.AsTask);

                        Assert.IsNull(await client.GetOrDefault<int?>(testKey));
                    }
                }
            }
        }

        private static async Task StopRedis()
        {
            await ExecUtil.Command("redis-cli", "save");
            await ExecUtil.Command("sudo", "service redis-server stop");
        }
        
        private static async Task StartRedis()
        {
            await ExecUtil.Command("sudo", "service redis-server start");
        }

        [TestMethod]
        [DynamicData(nameof(Local_Test_Endpoints_Data), DynamicDataSourceType.Method)]
        public async Task RedisClient_Stopped_Ping_Test(RedisClientConfig config)
        {
            using (var client = new RedisClient(config))
            {
                await client.Ping();

                await StopRedis();
                try
                {
                    for (var i = 0; i < 4; ++i)
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
        }
    }
}