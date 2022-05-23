using System;

namespace RedisCore;

public abstract class RedisException : Exception
{
    protected RedisException(string message) 
        : base(message)
    {
    }

    protected RedisException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public class RedisConnectionException : RedisException
{
    public RedisConnectionException(string message) 
        : base(message)
    {
    }

    public RedisConnectionException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}

public class RedisClientException : RedisException
{
    public string Type { get; }

    public RedisClientException(string type, string message)
        : base($"{type}: {message}")
    {
        Type = type;
    }
}

public static class KnownRedisErrors
{
    public const string Loading = "LOADING";
    public const string NoScript = "NOSCRIPT";
}