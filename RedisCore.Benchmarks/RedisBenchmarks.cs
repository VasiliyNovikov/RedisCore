using System.Net;
using System.Net.Sockets;
using StackExchange.Redis;

namespace RedisCore.Benchmarks;

public class RedisBenchmarks
{
    protected IDatabase TcpOfficialClient { get; }

    protected IDatabase UnixOfficialClient { get; }

    protected RedisClient TcpClient { get; }

    protected RedisClient TcpClientScriptCache { get; }

    protected RedisClient TcpClientStreamed { get; }

    protected RedisClient TcpClientNoBufferPool { get; }

    protected RedisClient UnixClient { get; }

    protected RedisClient UnixClientScriptCache { get; }

    protected RedisClient UnixClientStreamed { get; }

    protected RedisClient UnixClientNoBufferPool { get; }

    protected RedisBenchmarks()
    {
        const string tcpAddress = "127.0.0.1";
        const string unixAddress = "/var/run/redis/redis.sock";
        const string tcpUri = $"tcp://{tcpAddress}";
        const string unixUri = $"unix://{unixAddress}";

        TcpOfficialClient = ConnectionMultiplexer.Connect(new ConfigurationOptions { EndPoints = { new IPEndPoint(IPAddress.Parse(tcpAddress), 6379) } }).GetDatabase();
        UnixOfficialClient = ConnectionMultiplexer.Connect(new ConfigurationOptions { EndPoints = { new UnixDomainSocketEndPoint(unixAddress) } }).GetDatabase();
        TcpClient = new RedisClient(tcpUri);
        UnixClient = new RedisClient(unixUri);
        TcpClientScriptCache = new RedisClient(new RedisClientConfig(tcpUri) { UseScriptCache = true });
        UnixClientScriptCache = new RedisClient(new RedisClientConfig(unixUri) { UseScriptCache = true });
        TcpClientStreamed = new RedisClient(new RedisClientConfig(tcpUri) { ForceUseNetworkStream = true });
        UnixClientStreamed = new RedisClient(new RedisClientConfig(unixUri) { ForceUseNetworkStream = true });
        TcpClientNoBufferPool = new RedisClient(new RedisClientConfig(tcpUri) { UseBufferPool = false });
        UnixClientNoBufferPool = new RedisClient(new RedisClientConfig(unixUri) { UseBufferPool = false });
    }
}