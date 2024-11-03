using System;
#if NETSTANDARD2_0
using System.Text;
#endif

namespace RedisCore.Internal.Protocol;

internal sealed class RedisByteString : RedisString
{
    public override int ByteLength => Value.Length;

    public ReadOnlyMemory<byte> Value { get; }

    public RedisByteString(ReadOnlyMemory<byte> value) => Value = value;

    public RedisByteString(string value)
    {
        var valueBytes = new byte[ProtocolHandler.Encoding.GetByteCount(value)];
        ProtocolHandler.Encoding.GetBytes(value, valueBytes);
        Value = valueBytes;
    }

    public override string ToString() => ProtocolHandler.Encoding.GetString(Value.Span);
}