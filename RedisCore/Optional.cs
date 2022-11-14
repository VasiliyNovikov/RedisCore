using System;
using System.Collections.Generic;

namespace RedisCore;

public readonly struct Optional<T> : IEquatable<Optional<T>>
{
    private static readonly IEqualityComparer<T> ValueComparer = EqualityComparer<T>.Default;

    public static readonly Optional<T> Unspecified;

    public bool HasValue { get; }

    private readonly T _value;
    public T Value => HasValue ? _value : throw new InvalidOperationException("There is no value");

    private Optional(T value)
    {
        HasValue = true;
        _value = value;
    }

    public override string ToString() => HasValue ? Value?.ToString() ?? "<null>" : "<unspecified>";

    public bool Equals(Optional<T> other)
    {
        return HasValue == other.HasValue && (!HasValue || ValueComparer.Equals(_value, other._value));
    }

    public override bool Equals(object? obj) => obj is Optional<T> other && Equals(other);

    public override int GetHashCode() => HasValue ? _value?.GetHashCode() ?? 0 : 0;

    public static implicit operator Optional<T>(T value) => new(value);

    public static explicit operator T(Optional<T> value) => value.Value;

    public static bool operator ==(Optional<T> a, Optional<T> b) => a.Equals(b);

    public static bool operator !=(Optional<T> a, Optional<T> b) => !a.Equals(b);
}