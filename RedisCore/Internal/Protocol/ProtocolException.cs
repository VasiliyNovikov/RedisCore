using System;

namespace RedisCore.Internal.Protocol;

public class ProtocolException : Exception
{
    public ProtocolException(string message)
        : base(message)
    {
    }
}