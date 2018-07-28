namespace RedisCore
{
    public struct Optional<T>
    {
        public bool HasValue { get; }

        public T Value { get; }

        public Optional(T value)
        {
            HasValue = true;
            Value = value;
        }

        public override string ToString() => HasValue ? Value?.ToString() ?? "<null>" : "<unspecified>";

        public static implicit operator Optional<T>(T value) => new Optional<T>(value);

        public static readonly Optional<T> Unspecified = default;
    }
}