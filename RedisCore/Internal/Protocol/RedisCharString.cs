namespace RedisCore.Internal.Protocol;

internal sealed class RedisCharString(string value) : RedisString
{
    private int _byteLength = -1;
    public override int ByteLength
    {
        get
        {
            if (_byteLength < 0)
                _byteLength = ProtocolHandler.Encoding.GetByteCount(Value);
            return _byteLength;
        }
    }

    public string Value => value;

    public override string ToString() => Value;
}