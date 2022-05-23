using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace RedisCore.Utils;

[SuppressMessage("Microsoft.Performance", "CA1815: Override equals and operator equals on value types", Justification = "This type has reference type semantics")]
public struct RentedBuffer<T> : IEnumerable<T>, IDisposable
    where T : struct
{
    private T[] _buffer;

    public int Length { get; }

    public Memory<T> Memory => new(_buffer, 0, Length);

    public Span<T> Span => new(_buffer, 0, Length);

    public ArraySegment<T> Segment => new(_buffer, 0, Length);

    public RentedBuffer(int length)
    {
        Length = length;
        _buffer = ArrayPool<T>.Shared.Rent(length);
    }

    public void Dispose()
    {
        ArrayPool<T>.Shared.Return(_buffer);
        _buffer = null!;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _buffer.Take(Length).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Sort()
    {
        Array.Sort(_buffer, 0, Length);
    }
}