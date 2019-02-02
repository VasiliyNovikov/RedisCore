﻿using System;
using System.Collections.Generic;

namespace RedisCore
{
    public struct Optional<T> : IEquatable<Optional<T>>
    {
        private static readonly IEqualityComparer<T> ValueComparer = EqualityComparer<T>.Default;
        
        public static readonly Optional<T> Unspecified = default;

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

        public override bool Equals(object obj)
        {
            return !ReferenceEquals(null, obj) && obj is Optional<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HasValue ? 0 : ValueComparer.GetHashCode(_value);
        }

        public static implicit operator Optional<T>(T value) => new Optional<T>(value);
        
        public static explicit operator T(Optional<T> value) => value.Value;

        public static bool operator ==(Optional<T> a, Optional<T> b) => a.Equals(b);

        public static bool operator !=(Optional<T> a, Optional<T> b) => !a.Equals(b);
    }
}