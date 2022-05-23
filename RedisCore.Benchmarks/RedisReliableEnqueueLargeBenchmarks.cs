namespace RedisCore.Benchmarks;

public class RedisReliableEnqueueLargeBenchmarks : RedisReliableEnqueueBenchmarks
{
    protected override int ValueLength => 128 * 1024;
}