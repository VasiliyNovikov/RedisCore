namespace RedisCore.Internal.Protocol
{
    internal abstract class RedisString : RedisValueObject
    {
        public abstract int ByteLength { get; }
    }
}