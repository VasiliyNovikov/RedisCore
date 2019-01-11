using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RedisCore.Utils;

namespace RedisCore.Tests
{
    [TestClass]
    public class RedisClientTests : RedisClientTestsBase
    {
        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), typeof(RedisClientTestsBase), DynamicDataSourceType.Method)]
        public async Task RedisClient_Ping_Test(RedisClientConfig config)
        {
            using (var client = new RedisClient(config))
            {
                for (var i = 0; i < 8; ++i)
                {
                    var start = MonotonicTime.Now;
                    var rtt = await client.Ping();
                    var testRtt = MonotonicTime.Now - start;
                    Assert.IsTrue(rtt > TimeSpan.Zero);
                    Assert.IsTrue(testRtt > rtt);
                }
            }
        }
        
        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), typeof(RedisClientTestsBase), DynamicDataSourceType.Method)]
        public async Task RedisClient_Get_Set_Delete_Test(RedisClientConfig config)
        {
            using (var client = new RedisClient(config))
            {
                for (var i = 0; i < 8; ++i)
                {
                    var testKey = UniqueString();
                    var testValue = UniqueString();

                    Assert.IsNull(await client.GetOrDefault<string>(testKey));

                    await client.Set(testKey, testValue);
                    Assert.AreEqual(testValue, await client.Get<string>(testKey));

                    await client.Delete(testKey);
                    Assert.IsNull(await client.GetOrDefault<string>(testKey));
                }
            }
        }
        
        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), typeof(RedisClientTestsBase), DynamicDataSourceType.Method)]
        public async Task RedisClient_Set_With_Concurrency_Test(RedisClientConfig config)
        {
            using (var client = new RedisClient(config))
            {
                for (var i = 0; i < 8; ++i)
                {
                    var testKey = UniqueString();
                    var testValue = UniqueString();

                    Assert.IsFalse(await client.Set(testKey, testValue, OptimisticConcurrency.IfExists));
                    Assert.IsNull(await client.GetOrDefault<string>(testKey));

                    Assert.IsTrue(await client.Set(testKey, testValue, OptimisticConcurrency.IfNotExists));
                    Assert.AreEqual(testValue, await client.Get<string>(testKey));

                    var testValue2 = UniqueString();
                    Assert.IsFalse(await client.Set(testKey, testValue2, OptimisticConcurrency.IfNotExists));
                    Assert.AreEqual(testValue, await client.Get<string>(testKey));

                    Assert.IsTrue(await client.Set(testKey, testValue2, OptimisticConcurrency.IfExists));
                    Assert.AreEqual(testValue2, await client.Get<string>(testKey));

                    await client.Delete(testKey);
                }
            }
        }
        
        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), typeof(RedisClientTestsBase), DynamicDataSourceType.Method)]
        public async Task RedisClient_Get_Set_Delete_Large_Test(RedisClientConfig config)
        {
            const int valueSize = 0x100000;
            var rnd = new Random();
            using (var client = new RedisClient(config))
            {
                for (var i = 0; i < 8; ++i)
                {
                    var testKey = UniqueString();
                    var testValueBuilder = new StringBuilder(valueSize);
                    const string letters = "0123456789ABCDEF";
                    for (var j = 0; j < valueSize; ++j)
                        testValueBuilder.Append(letters[rnd.Next(letters.Length)]);
                    var testValue = testValueBuilder.ToString();

                    Assert.IsNull(await client.GetOrDefault<string>(testKey));

                    await client.Set(testKey, testValue);
                    Assert.AreEqual(testValue, await client.Get<string>(testKey));

                    await client.Delete(testKey);
                    Assert.IsNull(await client.GetOrDefault<string>(testKey));
                }
            }
        }

        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), typeof(RedisClientTestsBase), DynamicDataSourceType.Method)]
        public async Task RedisClient_Set_Expire_Test(RedisClientConfig config)
        {
            var expireTime = TimeSpan.FromSeconds(0.5);
            using (var client = new RedisClient(config))
            {
                var testKey = UniqueString();
                var testValue = UniqueString();

                Assert.IsFalse(await client.Expire(testKey, expireTime));

                Assert.IsTrue(await client.Set(testKey, testValue));
                Assert.IsTrue(await client.Expire(testKey, expireTime));
                Assert.AreEqual(testValue, await client.Get<string>(testKey));

                await Task.Delay(expireTime * 1.5);

                Assert.IsNull(await client.GetOrDefault<string>(testKey));
            }
        }
        
        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), typeof(RedisClientTestsBase), DynamicDataSourceType.Method)]
        public async Task RedisClient_Set_Expire_Test_2(RedisClientConfig config)
        {
            var expireTime = TimeSpan.FromSeconds(0.5);
            using (var client = new RedisClient(config))
            {
                var testKey = UniqueString();
                var testValue = UniqueString();

                Assert.IsTrue(await client.Set(testKey, testValue, expireTime));
                Assert.AreEqual(testValue, await client.Get<string>(testKey));

                await Task.Delay(expireTime * 1.5);

                Assert.IsNull(await client.GetOrDefault<string>(testKey));
            }
        }

        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), typeof(RedisClientTestsBase), DynamicDataSourceType.Method)]
        public async Task RedisClient_Integer_Get_Set_Delete_Test(RedisClientConfig config)
        {
            using (var client = new RedisClient(config))
            {
                for (var i = 0; i < 4; ++i)
                {
                    var testKey = UniqueString();
                    const int testValue = 42;

                    Assert.IsNull(await client.GetOrDefault<int?>(testKey));

                    await client.Set(testKey, testValue);
                    Assert.AreEqual(testValue, await client.Get<int>(testKey));

                    await client.Delete(testKey);
                    Assert.IsNull(await client.GetOrDefault<int?>(testKey));
                }
            }
        }

        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), typeof(RedisClientTestsBase), DynamicDataSourceType.Method)]
        public async Task RedisClient_Transaction_Get_Set_Test(RedisClientConfig config)
        {
            using (var client = new RedisClient(config))
            {
                for (var i = 0; i < 8; ++i)
                {
                    var testKey = UniqueString();
                    var testValue = UniqueString();

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

        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), typeof(RedisClientTestsBase), DynamicDataSourceType.Method)]
        public async Task RedisClient_Transaction_Get_Set_Discard_Test(RedisClientConfig config)
        {
            using (var client = new RedisClient(config))
            {
                for (var i = 0; i < 8; ++i)
                {
                    var testKey = UniqueString();
                    var testValue = UniqueString();

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

        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), typeof(RedisClientTestsBase), DynamicDataSourceType.Method)]
        public async Task RedisClient_PubSub_Send_Receive_Test(RedisClientConfig config)
        {
            using (var client = new RedisClient(config))
            {
                for (var i = 0; i < 4; ++i)
                {
                    var testChannel = UniqueString();
                    var testMsgBase = UniqueString();
                    using (var subscription = await client.Subscribe(testChannel))
                    {
                        for (var j = 0; j < 8; ++j)
                        {
                            var testMsg = testMsgBase + j;
                            await client.Publish(testChannel, testMsg);
                            var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(0.5));
                            var actualMsg = await subscription.GetMessage<string>(cancellationSource.Token);
                            Assert.AreEqual(testMsg, actualMsg);
                        }

                        await subscription.Unsubscribe();
                    }
                }
            }
        }

        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), typeof(RedisClientTestsBase), DynamicDataSourceType.Method)]
        public async Task RedisClient_PubSub_Batch_Send_Receive_Test(RedisClientConfig config)
        {
            using (var client = new RedisClient(config))
            {
                for (var i = 0; i < 4; ++i)
                {
                    var testChannel = UniqueString();
                    var testMsgBase = UniqueString();
                    var messages = Enumerable.Range(0, 8).Select(j => testMsgBase + j).ToList();
                    using (var subscription = await client.Subscribe(testChannel))
                    {
                        foreach (var testMsg in messages)
                        {
                            await client.Publish(testChannel, testMsg);
                        }

                        var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                        foreach (var testMsg in messages)
                        {
                            var actualMsg = await subscription.GetMessage<string>(cancellationSource.Token);
                            Assert.AreEqual(testMsg, actualMsg);
                        }

                        await subscription.Unsubscribe();
                    }
                }
            }
        }

        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), typeof(RedisClientTestsBase), DynamicDataSourceType.Method)]
        public async Task RedisClient_PubSub_Receive_Cancel_Send_Receive_Test(RedisClientConfig config)
        {
            using (var client = new RedisClient(config))
            {
                for (var i = 0; i < 3; ++i)
                {
                    var testChannel = UniqueString();
                    using (var subscription = await client.Subscribe(testChannel))
                    {
                        for (var j = 0; j < 2; ++j)
                        {
                            var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(0.2));
                            try
                            {
                                await subscription.GetMessage<string>(cancellationSource.Token);
                                Assert.Fail("Expected cancellation exception");
                            }
                            catch (OperationCanceledException)
                            {
                            }
                        }

                        var testMsg = UniqueString();
                        await client.Publish(testChannel, testMsg);
                        var cancellationSource2 = new CancellationTokenSource(TimeSpan.FromSeconds(0.5));
                        var actualMsg = await subscription.GetMessage<string>(cancellationSource2.Token);
                        Assert.AreEqual(testMsg, actualMsg);

                        await subscription.Unsubscribe();
                    }
                }
            }
        }
    }
}