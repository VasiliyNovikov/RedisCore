using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RedisCore.Tests
{
    public class RedisClientTestsBase
    {
        private static bool IsVstsBuild => Environment.GetEnvironmentVariable("TF_BUILD") != null;

        protected static bool HasLocalRedis => !IsVstsBuild || Environment.GetEnvironmentVariable("LOCAL_REDIS") == "true";

        private static string LocalRedisAddress => Environment.GetEnvironmentVariable("LOCAL_REDIS_ADDRESS") ?? "127.0.0.1";

        private static readonly int[] BufferSizes = {64, 256, 65536};

        private static IEnumerable<RedisClientConfig>  LocalTestConfigs()
        {
            foreach (var bufferSize in BufferSizes)
            {
                foreach (var forceUseNetworkStream in new[] {false, true})
                {
                    yield return new RedisClientConfig(LocalRedisAddress)
                    {
                        BufferSize = bufferSize,
                        ForceUseNetworkStream = forceUseNetworkStream
                    };
#if NETSTANDARD
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                        yield return new RedisClientConfig(new UnixDomainSocketEndPoint("/var/run/redis/redis.sock"))
                        {
                            BufferSize = bufferSize,
                            ForceUseNetworkStream = forceUseNetworkStream
                        };
#endif
                }
            }
        }

        private static IEnumerable<RedisClientConfig> TestConfigs()
        {
            if (HasLocalRedis)
                foreach (var config in LocalTestConfigs())
                    yield return config;

            if (!IsVstsBuild)
                yield break;

            var host = Environment.GetEnvironmentVariable("AZURE_REDIS_HOST");
            var password = Environment.GetEnvironmentVariable("AZURE_REDIS_PWD");

            Assert.IsNotNull(host, "Host is missing");
            Assert.IsNotNull(password, "Password is missing");

            foreach (var bufferSize in BufferSizes)
                yield return new RedisClientConfig(host, true) {Password = password, BufferSize = bufferSize};
        }
        
        protected static IEnumerable<object[]> Local_Test_Endpoints_Data() => LocalTestConfigs().Select(cfg => new object[] {cfg});
        protected static IEnumerable<object[]> Test_Endpoints_Data() => TestConfigs().Select(cfg => new object[] {cfg});

        protected static string UniqueString() => Guid.NewGuid().ToString();
    }
}