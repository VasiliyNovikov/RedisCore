namespace RedisCore.Internal.Protocol
{
    internal sealed class RedisCharString : RedisString
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

        public string Value { get; }

        public RedisCharString(string value) => Value = value;

        public override string ToString() => Value;
    }
}