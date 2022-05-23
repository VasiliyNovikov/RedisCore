namespace RedisCore.Benchmarks;

public class RedisSetGetDelLargeBenchmarks : RedisSetGetDelBenchmarks
{
    protected override int ValueLength => 65536;
}