using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RedisCore.Tests;

public class RedisClientTestsBase
{
    private static bool IsCIBuild => Environment.GetEnvironmentVariable("CI") == "true";

    protected static bool HasLocalRedis => !IsCIBuild || Environment.GetEnvironmentVariable("LOCAL_REDIS") == "true";

    private static string LocalRedisAddress => Environment.GetEnvironmentVariable("LOCAL_REDIS_ADDRESS") ?? "127.0.0.1";

    private static readonly int[] BufferSizes = {64, 256, 65536};

    private static IEnumerable<RedisClientConfig> LocalTestConfigs(bool addScriptCache = false)
    {
        foreach (var useScriptCache in addScriptCache ? new[] {false, true} : new [] {false})
        {
            foreach (var bufferSize in BufferSizes)
            {
                foreach (var forceUseNetworkStream in new[] {false, true})
                {
                    yield return new RedisClientConfig($"tcp://{LocalRedisAddress}")
                    {
                        BufferSize = bufferSize,
                        ForceUseNetworkStream = forceUseNetworkStream,
                        UseScriptCache = useScriptCache
                    };
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                        yield return new RedisClientConfig("unix:///var/run/redis/redis.sock")
                        {
                            BufferSize = bufferSize,
                            ForceUseNetworkStream = forceUseNetworkStream,
                            UseScriptCache = useScriptCache
                        };
                }
            }
        }
    }

    protected static IEnumerable<RedisClientConfig> TestConfigs(bool addScriptCache = false)
    {
        if (HasLocalRedis)
            foreach (var config in LocalTestConfigs(addScriptCache))
                yield return config;

        if (!IsCIBuild)
            yield break;

        var host = Environment.GetEnvironmentVariable("AZURE_REDIS_HOST");
        var password = Environment.GetEnvironmentVariable("AZURE_REDIS_PWD");

        Assert.IsNotNull(host, "Azure Redis host variable is missing");
        Assert.IsNotNull(password, "Azure Redis password variable is missing");

        foreach (var useScriptCache in addScriptCache ? new[] {false, true} : new [] {false})
        foreach (var bufferSize in BufferSizes)
            yield return new RedisClientConfig($"ssl://{host}") {Password = password, BufferSize = bufferSize, UseScriptCache = useScriptCache};
    }

    protected static IEnumerable<object[]> Local_Test_Endpoints_Data() => LocalTestConfigs().Select(cfg => new object[] {cfg});
    protected static IEnumerable<object[]> Test_Endpoints_Data() => TestConfigs().Select(cfg => new object[] {cfg});

    protected static string UniqueString() => Guid.NewGuid().ToString();

    protected static byte[] UniqueBinary(int length)
    {
        var result = new byte[length];
        new Random().NextBytes(result);
        return result;
    }
}