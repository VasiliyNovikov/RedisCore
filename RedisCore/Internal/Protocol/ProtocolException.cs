using System;
using System.Diagnostics.CodeAnalysis;

namespace RedisCore.Internal.Protocol;

[SuppressMessage("Microsoft.Design", "CA1032: Implement standard exception constructors",
                 Justification = "This exception is not supposed to be instantiated outside of RedisClient API")]
public class ProtocolException : Exception
{
    public ProtocolException(string message)
        : base(message)
    {
    }
}