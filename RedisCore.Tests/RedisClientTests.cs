using System;
using System.Collections.Generic;
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
        public async Task RedisClient_Get_Set_Delete_Exists_Test(RedisClientConfig config)
        {
            using (var client = new RedisClient(config))
            {
                for (var i = 0; i < 8; ++i)
                {
                    var testKey = UniqueString();
                    var testValue = UniqueString();

                    Assert.IsFalse(await client.Exists(testKey));
                    Assert.IsNull(await client.GetOrDefault<string>(testKey));

                    await client.Set(testKey, testValue);
                    Assert.IsTrue(await client.Exists(testKey));
                    Assert.AreEqual(testValue, await client.Get<string>(testKey));

                    await client.Delete(testKey);
                    Assert.IsNull(await client.GetOrDefault<string>(testKey));
                    Assert.IsFalse(await client.Exists(testKey));
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
        public async Task RedisClient_Get_Set_Delete_Buffered_Test(RedisClientConfig config)
        {
            var rnd = new Random();
            using (var bufferPool = BufferPool.Create<byte>())
            using (var client = new RedisClient(config))
            {
                for (var i = 0; i < 8; ++i)
                {
                    var testKey = UniqueString();
                    var testValue = bufferPool.RentMemory(16384);
                    rnd.NextBytes(testValue.Span);

                    Assert.AreEqual(null, await client.Get(testKey, bufferPool));

                    await client.Set(testKey, testValue);

                    CollectionAssert.AreEqual(testValue.ToArray(), (await client.Get(testKey, bufferPool)).Value.ToArray());
                    
                    await client.Delete(testKey);
                    Assert.AreEqual(null, await client.Get(testKey, bufferPool));
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

                await Task.Delay(expireTime.Multiply(1.5));

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

                await Task.Delay(expireTime.Multiply(1.5));

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
        public async Task RedisClient_Hash_Get_Keys_Values_All_Test(RedisClientConfig config)
        {
            using (var client = new RedisClient(config))
            {
                var testHash = UniqueString();
                var testData = Enumerable.Range(0, 5).ToDictionary(_ => UniqueString(), _ => UniqueString());

                try
                {
                    foreach (var (key, value) in testData)
                        Assert.IsTrue(await client.HashSet(testHash, key, value));
                    
                    CollectionAssert.AreEquivalent(testData.Keys, (await client.HashKeys(testHash)).ToArray());
                    CollectionAssert.AreEquivalent(testData.Values, await client.HashValues<string>(testHash));
                    CollectionAssert.AreEquivalent(testData, await client.HashItems<string>(testHash));
                }
                finally
                {
                    await client.Delete(testHash);    
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
                            using (var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(0.5)))
                            {
                                var actualMsg = await subscription.GetMessage<string>(cancellationSource.Token);
                                Assert.AreEqual(testMsg, actualMsg);
                            }
                        }

                        await subscription.Unsubscribe();
                    }
                }
            }
        }
        
        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), typeof(RedisClientTestsBase), DynamicDataSourceType.Method)]
        public async Task RedisClient_PubSub_Send_Receive_Buffered_Test(RedisClientConfig config)
        {
            var rnd = new Random();
            using (var bufferPool = BufferPool.Create<byte>())
            using (var client = new RedisClient(config))
            {
                for (var i = 0; i < 4; ++i)
                {
                    var testChannel = UniqueString();
                    using (var subscription = await client.Subscribe(testChannel))
                    {
                        for (var j = 0; j < 8; ++j)
                        {
                            var testMsg = bufferPool.RentMemory(16384);
                            rnd.NextBytes(testMsg.Span);
                            
                            await client.Publish(testChannel, testMsg);
                            using (var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(0.5)))
                                CollectionAssert.AreEqual(testMsg.ToArray(), (await subscription.GetMessage(bufferPool, cancellationSource.Token)).ToArray());
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

                        using (var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(1)))
                        {
                            foreach (var testMsg in messages)
                            {
                                var actualMsg = await subscription.GetMessage<string>(cancellationSource.Token);
                                Assert.AreEqual(testMsg, actualMsg);
                            }
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
                            using (var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(0.2)))
                            {
                                try
                                {
                                    await subscription.GetMessage<string>(cancellationSource.Token);
                                    Assert.Fail("Expected cancellation exception");
                                }
                                catch (OperationCanceledException)
                                {
                                }
                            }
                        }

                        var testMsg = UniqueString();
                        await client.Publish(testChannel, testMsg);
                        using (var cancellationSource2 = new CancellationTokenSource(TimeSpan.FromSeconds(0.5)))
                        {
                            var actualMsg = await subscription.GetMessage<string>(cancellationSource2.Token);
                            Assert.AreEqual(testMsg, actualMsg);
                        }

                        await subscription.Unsubscribe();
                    }
                }
            }
        }

        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), typeof(RedisClientTestsBase), DynamicDataSourceType.Method)]
        public async Task RedisClient_PubSub_Subscribe_Publish_Unsubscribe_Ping_Test(RedisClientConfig config)
        {
            using (var client = new RedisClient(config))
            {
                for (var i = 0; i < 3; ++i)
                {
                    var testChannel = UniqueString();
                    using (var subscription = await client.Subscribe(testChannel))
                    {
                        await client.Publish(testChannel, "Whatever...");
                        await subscription.Unsubscribe();
                    }

                    await client.Ping();
                }
            }
        }
        
        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), typeof(RedisClientTestsBase), DynamicDataSourceType.Method)]
        public async Task RedisClient_Eval_Echo_Test(RedisClientConfig config)
        {
            using (var client = new RedisClient(config))
            {
                var testValue = UniqueString();
                var value = await client.Eval<string, string>("return ARGV[1]", testValue);
                Assert.AreEqual(testValue, value);
            }
        }

        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), typeof(RedisClientTestsBase), DynamicDataSourceType.Method)]
        public async Task RedisClient_Eval_Echo_Int_Test(RedisClientConfig config)
        {
            using (var client = new RedisClient(config))
            {
                const int testValue = 42;
                var value = await client.Eval<int?, int?>("return ARGV[1]", testValue);
                Assert.AreEqual(testValue, value);
            }
        }

        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), typeof(RedisClientTestsBase), DynamicDataSourceType.Method)]
        public async Task RedisClient_Eval_Echo_Buffered_Test(RedisClientConfig config)
        {
            using (var bufferPool = BufferPool.Create<byte>())
            using (var client = new RedisClient(config))
            {
                var testValue = UniqueBinary(1024);
                var value = await client.Eval<Memory<byte>>(bufferPool, "return ARGV[1]", testValue);
                CollectionAssert.AreEqual(testValue, value.Value.ToArray());
            }
        }

        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), typeof(RedisClientTestsBase), DynamicDataSourceType.Method)]
        public async Task RedisClient_Set_Eval_Get_Test(RedisClientConfig config)
        {
            using (var client = new RedisClient(config))
            {
                var testKey = UniqueString();
                var testValue = UniqueString();

                await client.Set(testKey, testValue);
                try
                {
                    var value = await client.Eval<string>("return redis.call('GET', KEYS[1])", testKey);
                    Assert.AreEqual(testValue, value);
                }
                finally
                {
                    await client.Delete(testKey);
                }
            }
        }
        
        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), typeof(RedisClientTestsBase), DynamicDataSourceType.Method)]
        public async Task RedisClient_Set_2_Eval_Get_2_Test(RedisClientConfig config)
        {
            using (var client = new RedisClient(config))
            {
                var testKey1 = UniqueString();
                var testKey2 = UniqueString();
                var testValue1 = UniqueString();
                var testValue2 = UniqueString();

                const string script = 
@"local data1 = redis.call('GET', KEYS[1])
local data2 = redis.call('GET', KEYS[2])
return {data1, data2}";

                await client.Set(testKey1, testValue1);
                await client.Set(testKey2, testValue2);
                try
                {
                    var values = await client.Eval<IReadOnlyList<string>>(script, testKey1, testKey2);
                    Assert.AreEqual(testValue1, values[0]);
                    Assert.AreEqual(testValue2, values[1]);
                }
                finally
                {
                    await client.Delete(testKey1);
                    await client.Delete(testKey2);
                }
            }
        }
        
        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), typeof(RedisClientTestsBase), DynamicDataSourceType.Method)]
        public async Task RedisClient_Eval_Get_Optional_Test(RedisClientConfig config)
        {
            using (var client = new RedisClient(config))
            {
                const string testValue = "123";
                Assert.AreEqual(testValue, await client.Eval<Optional<string>>($"return '{testValue}'"));
                Assert.AreEqual(Optional<string>.Unspecified, await client.Eval<Optional<string>>("return false"));
            }
        }
        
        [TestMethod]
        [DynamicData(nameof(Test_Endpoints_Data), typeof(RedisClientTestsBase), DynamicDataSourceType.Method)]
        public async Task RedisClient_Eval_Move_From_Key_To_List_Test(RedisClientConfig config)
        {
            using (var client = new RedisClient(config))
            {
                var testKey = UniqueString();
                var testList = UniqueString();
                var testValue = UniqueBinary(65536);

                const string script = 
@"local data = redis.call('GET', KEYS[1])
if data then
    redis.call('LPUSH', KEYS[2], data)
end
return 0";

                try
                {
                    await client.Eval<bool>(script, testKey, testList);
                    Assert.AreEqual(0, await client.ListLength(testList));

                    await client.Set(testKey, testValue);
                    await client.Eval<bool>(script, testKey, testList);
                    Assert.AreEqual(1, await client.ListLength(testList));
                    CollectionAssert.AreEqual(testValue, (await client.LeftPop<byte[]>(testList)).Value);
                }
                finally
                {
                    await client.Delete(testKey);
                    await client.Delete(testList);
                }
            }
        }
    }
}