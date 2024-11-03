#if LINUX
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RedisCore.Tests;

[TestClass]
public class RedisClientStoppedTests : RedisClientTestsBase
{
    private const int Port = 7379;
    private const string UnixSocketPath = "/tmp/redis-stopped.sock";
    private const string UnixSocketPermission = "777";

    [TestMethod]
    [DynamicData(nameof(Stopped_Test_Endpoints_Data), DynamicDataSourceType.Method)]
    public async Task RedisClient_Stopped_Ping_Test(RedisClientConfig config)
    {
        await using var client = new RedisClient(config);

        using (new RedisScope())
            await client.Ping();

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

        using (new RedisScope())
            await client.Ping();
    }

    [TestMethod]
    [DynamicData(nameof(Stopped_Test_Endpoints_Data), DynamicDataSourceType.Method)]
    public async Task RedisClient_Stopped_PubSub_Receive_Test(RedisClientConfig config)
    {
        await using var client = new RedisClient(config);
        for (var i = 0; i < 2; i++)
        {
            var testChannel = UniqueString();

            ISubscription subscription;
            using (new RedisScope())
                subscription = await client.Subscribe(testChannel);
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
                using (new RedisScope())
                    await subscription.DisposeAsync();
            }
        }
    }

    private static IEnumerable<RedisClientConfig> StoppedTestConfigs()
    {
        foreach (var bufferSize in BufferSizes)
            foreach (var forceUseNetworkStream in new[] { false, true })
                foreach (var uri in new[] { $"unix://{UnixSocketPath}", $"tcp://{LocalRedisAddress}:{Port}" })
                    yield return new RedisClientConfig(uri)
                    {
                        BufferSize = bufferSize,
                        ForceUseNetworkStream = forceUseNetworkStream,
                    };
    }

    protected static IEnumerable<object[]> Stopped_Test_Endpoints_Data() => StoppedTestConfigs().Select(cfg => new object[] { cfg });

    private sealed class RedisScope : IDisposable
    {
        private const int RedisStartDelayMilliseconds = 100;
        private readonly Process _process;

        public RedisScope()
        {
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "redis-server",
                    Arguments = $"--bind {LocalRedisAddress} --port {Port} --unixsocket {UnixSocketPath} --unixsocketperm {UnixSocketPermission} --maxclients 256 --logfile /dev/null"
                }
            };
            _process.Start();
            Thread.Sleep(RedisStartDelayMilliseconds);
            if (_process.HasExited)
                throw new AssertFailedException("Redis server failed to start");
        }

        public void Dispose()
        {
            if (!_process.HasExited)
            {
                _process.Kill();
                _process.WaitForExit();
            }
            _process.Dispose();
        }
    }
}
#endif