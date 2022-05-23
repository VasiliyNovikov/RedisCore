using System;
using System.Diagnostics.CodeAnalysis;

namespace RedisCore;

[SuppressMessage("Microsoft.Design", "CA1032: Implement standard exception constructors",
                 Justification = "This exception is not supposed to be instantiated outside of RedisClient API")]
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

[SuppressMessage("Microsoft.Design", "CA1032: Implement standard exception constructors",
                 Justification = "This exception is not supposed to be instantiated outside of RedisClient API")]
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

[SuppressMessage("Microsoft.Design", "CA1032: Implement standard exception constructors",
                 Justification = "This exception is not supposed to be instantiated outside of RedisClient API")]
public class RedisClientException : RedisException
{
    public string ErrorType { get; }

    public RedisClientException(string type, string message)
        : base($"{type}: {message}")
    {
        ErrorType = type;
    }
}

public static class KnownRedisErrors
{
    public const string Loading = "LOADING";
    public const string NoScript = "NOSCRIPT";
}