namespace RedisCore.Internal.Protocol
{
    internal sealed class RedisInteger : RedisValueObject
    {
        public long Value { get; }

        public RedisInteger(long value) => Value = value;

        public override string ToString() => Value.ToString();
    }
}