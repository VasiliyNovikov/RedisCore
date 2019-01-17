using System;

namespace RedisCore
{
    public struct Optional<T>
    {
        public bool HasValue { get; }

        private readonly T _value;
        public T Value => HasValue ? _value : throw new InvalidOperationException("There is no value");

        private Optional(T value)
        {
            HasValue = true;
            _value = value;
        }

        public override string ToString() => HasValue ? Value?.ToString() ?? "<null>" : "<unspecified>";

        public static implicit operator Optional<T>(T value) => new Optional<T>(value);
        
        public static explicit operator T(Optional<T> value) => value.Value;

        public static readonly Optional<T> Unspecified = default;
    }
}