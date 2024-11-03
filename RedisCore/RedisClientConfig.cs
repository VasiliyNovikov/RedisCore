using System;
using System.Text;

namespace RedisCore;

public class RedisClientConfig
{
    private const int DefaultBufferSize = 2048;
    private const int DefaultMaxFreeConnections = 3;
    private static readonly TimeSpan DefaultConnectionPoolMaintenanceInterval = TimeSpan.FromSeconds(30);

    public Uri Uri { get; }

    public string? Password { get; set; }

    public int Database { get; set; }

    public int BufferSize { get; set; } = DefaultBufferSize;

    public int MaxFreeConnections { get; set; } = DefaultMaxFreeConnections;

    public TimeSpan ConnectionPoolMaintenanceInterval { get; set; } = DefaultConnectionPoolMaintenanceInterval;

    public bool ForceUseNetworkStream { get; set; }

    public bool UseBufferPool { get; set; } = true;

    public bool UseScriptCache { get; set; }

    public TimeSpan LoadingRetryDelayMin { get; set; } = TimeSpan.FromMilliseconds(20);

    public TimeSpan LoadingRetryDelayMax { get; set; } = TimeSpan.FromMilliseconds(200);

    public TimeSpan LoadingRetryTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <param name="uri">Connection URI like:
    /// tcp://127.0.0.1 or
    /// ssl://some-azure-redis.redis.cache.windows.net or
    /// unix:///var/run/redis/redis.sock
    /// </param>
    public RedisClientConfig(Uri uri)
    {
        static void ThrowMalformedUri() => throw new ArgumentException("Malformed redis connection uri", nameof(uri));

        if (uri.Query != "")
            ThrowMalformedUri();

        switch (uri.Scheme)
        {
            case RedisUriSchema.Tcp:
            case RedisUriSchema.Ssl:
                if (uri.AbsolutePath != "/")
                    ThrowMalformedUri();
                if (uri.Scheme == RedisUriSchema.Ssl && uri.HostNameType != UriHostNameType.Dns)
                    throw new ArgumentException("DNS hostname is required for SSL connection", nameof(uri));
                break;
            case RedisUriSchema.Unix:
                if (uri.Host != "" || uri.AbsolutePath == "/" || !uri.IsDefaultPort)
                    ThrowMalformedUri();
                break;
            default:
                ThrowMalformedUri();
                break;
        }
        Uri = uri;
    }

    /// <param name="uri">Connection URI like:
    /// tcp://127.0.0.1 or
    /// ssl://some-azure-redis.redis.cache.windows.net or
    /// unix:///var/run/redis/redis.sock
    /// </param>
    public RedisClientConfig(string uri)
        : this(new Uri(uri))
    {
    }

    internal RedisClientConfig Clone() => (RedisClientConfig)MemberwiseClone();

    public override string ToString()
    {
        var featureBuilder = new StringBuilder();
        if (BufferSize != DefaultBufferSize)
            featureBuilder.Append(FormattableString.Invariant($"bufferSize={BufferSize}"));
        if (MaxFreeConnections != DefaultMaxFreeConnections)
        {
            if (featureBuilder.Length > 0)
                featureBuilder.Append(", ");
            featureBuilder.Append(FormattableString.Invariant($"maxFreeConnections={MaxFreeConnections}"));
        }
        if (ForceUseNetworkStream)
        {
            if (featureBuilder.Length > 0)
                featureBuilder.Append(", ");
            featureBuilder.Append("forceUseNetworkStream");
        }
        if (UseScriptCache)
        {
            if (featureBuilder.Length > 0)
                featureBuilder.Append(", ");
            featureBuilder.Append("useScriptCache");
        }

        return $"{Uri} {{{featureBuilder}}})";
    }
}