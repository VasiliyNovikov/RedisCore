using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace RedisCore.Benchmarks;

public class RedisReliableEnqueueBenchmarks : RedisBenchmarks
{
    private static readonly TimeSpan BufferExpiration = TimeSpan.FromSeconds(4);

    private readonly byte[] _value;

    protected virtual int ValueLength => 512;

    private RedisClient _client = null!;
    private string _buffer = null!;
    private string _queue = null!;

    public RedisReliableEnqueueBenchmarks()
    {
        _value = new byte[ValueLength];
        new Random().NextBytes(_value);
    }

    [Benchmark]
    public Task Tcp_Client_Reliable_Enqueue_Script()
    {
        _client = TcpClient;
        return Reliable_Enqueue_Script();
    }

    [Benchmark]
    public Task Unix_Client_Reliable_Enqueue_Script()
    {
        _client = UnixClient;
        return Reliable_Enqueue_Script();
    }

    [Benchmark]
    public Task Tcp_Client_Reliable_Enqueue_Script_Cache()
    {
        _client = TcpClientScriptCache;
        return Reliable_Enqueue_Script();
    }

    [Benchmark]
    public Task Unix_Client_Reliable_Enqueue_Script_Cache()
    {
        _client = UnixClientScriptCache;
        return Reliable_Enqueue_Script();
    }

    [Benchmark]
    public Task Tcp_Client_Reliable_Enqueue_Tran()
    {
        _client = TcpClient;
        return Reliable_Enqueue_Tran();
    }

    [Benchmark]
    public Task Unix_Client_Reliable_Enqueue_Tran()
    {
        _client = UnixClient;
        return Reliable_Enqueue_Tran();
    }

    private async Task Reliable_Enqueue_Script()
    {
        const string moveScript =
            """
            local data = redis.call('GET', KEYS[1])
            if data then
                redis.call('LPUSH', KEYS[2], data)
                redis.call('DEL', KEYS[1])
            end
            return 0
            """;
        _buffer = Guid.NewGuid().ToString();
        _queue = Guid.NewGuid().ToString();
        try
        {
            for (var i = 0; i < 10; ++i)
            {
                await _client.Set(_buffer, _value, BufferExpiration);
                await _client.Eval<bool>(moveScript, _buffer, _queue);
            }
        }
        finally
        {
            await _client.Delete(_buffer);
            await _client.Delete(_queue);
        }
    }

    private async Task Reliable_Enqueue_Tran()
    {
        _buffer = Guid.NewGuid().ToString();
        _queue = Guid.NewGuid().ToString();
        try
        {
            for (var i = 0; i < 10; ++i)
            {
                using (var tran = _client.CreateTransaction())
                {
                    var t1 = tran.Delete(_buffer);
                    var t2 = tran.LeftPush(_buffer, _value);
                    var t3 = tran.Expire(_buffer, BufferExpiration);

                    await tran.Complete();

                    await t1;
                    await t2;
                    await t3;
                }

                await _client.RightPopLeftPush<byte[]>(_buffer, _queue);
            }
        }
        finally
        {
            await _client.Delete(_buffer);
            await _client.Delete(_queue);
        }
    }
}