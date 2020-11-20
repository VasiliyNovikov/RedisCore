using BenchmarkDotNet.Running;

namespace RedisCore.Benchmarks
{
    internal static class Program
    {
        private static void Main()
        {
            //BenchmarkRunner.Run<RedisPingBenchmarks>();
            //BenchmarkRunner.Run<RedisSetGetDelBenchmarks>();
            //BenchmarkRunner.Run<RedisSetGetDelLargeBenchmarks>();
            //BenchmarkRunner.Run<RedisConnectDisconnectBenchmarks>();
            BenchmarkRunner.Run<RedisReliableEnqueueBenchmarks>();
        }
    }
}