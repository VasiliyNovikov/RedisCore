using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RedisCore.Utils;

public readonly struct RentedBuffer<T>(int length) : IEnumerable<T>, IDisposable
    where T : struct
{
    private readonly T[] _buffer = ArrayPool<T>.Shared.Rent(length);

    public int Length => length;

    public Memory<T> Memory => new(_buffer, 0, length);

    public Span<T> Span => new(_buffer, 0, length);

    public ArraySegment<T> Segment => new(_buffer, 0, length);

    public static implicit operator Memory<T>(in RentedBuffer<T> buffer) => buffer.Memory;

    public static implicit operator Span<T>(in RentedBuffer<T> buffer) => buffer.Span;

    public static explicit operator ArraySegment<T>(in RentedBuffer<T> buffer) => buffer.Segment;

    public void Dispose() => ArrayPool<T>.Shared.Return(_buffer);

    public IEnumerator<T> GetEnumerator() => _buffer.Take(length).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}