using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RedisCore.Tests;

public class RedisClientTestsBase
{
    protected const string LocalRedisAddress = "127.0.0.1";
#if LINUX
    private const string LocalRedisSocket = "/var/run/redis/redis.sock";
#endif

    private static bool IsCIBuild => Environment.GetEnvironmentVariable("CI") == "true";

    protected static readonly int[] BufferSizes = [64, 256, 65536];

    protected static IEnumerable<RedisClientConfig> TestConfigs(bool addScriptCache = false)
    {
#if LINUX
        foreach (var useScriptCache in addScriptCache ? [false, true] : new[] { false })
            foreach (var bufferSize in BufferSizes)
                foreach (var forceUseNetworkStream in new[] { false, true })
                    foreach (var uri in new[] { $"tcp://{LocalRedisAddress}", $"unix://{LocalRedisSocket}" })
                        yield return new RedisClientConfig(uri)
                        {
                            BufferSize = bufferSize,
                            ForceUseNetworkStream = forceUseNetworkStream,
                            UseScriptCache = useScriptCache
                        };
#endif
        if (!IsCIBuild)
            yield break;

        var host = Environment.GetEnvironmentVariable("AZURE_REDIS_HOST");
        var password = Environment.GetEnvironmentVariable("AZURE_REDIS_PWD");

        Assert.IsNotNull(host, "Azure Redis host variable is missing");
        Assert.IsNotNull(password, "Azure Redis password variable is missing");

        foreach (var useScriptCache in addScriptCache ? [false, true] : new[] { false })
            foreach (var bufferSize in BufferSizes)
                yield return new RedisClientConfig($"ssl://{host}") { Password = password, BufferSize = bufferSize, UseScriptCache = useScriptCache };
    }

    protected static IEnumerable<object[]> Test_Endpoints_Data() => TestConfigs().Select(cfg => new object[] { cfg });

    protected static string UniqueString() => Guid.NewGuid().ToString();

    protected static byte[] UniqueBinary(int length)
    {
        var result = new byte[length];
        new Random().NextBytes(result);
        return result;
    }
}