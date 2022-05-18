using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RedisCore.Utils
{
    public struct RentedBuffer<T> : IEnumerable<T>, IDisposable
        where T : struct
    {
        private T[] _buffer;

        public int Length { get; }

        public Memory<T> Memory => new Memory<T>(_buffer, 0, Length);

        public Span<T> Span => new Span<T>(_buffer, 0, Length);
        
        public ArraySegment<T> Segment => new(_buffer, 0, Length);

        public RentedBuffer(int length)
        {
            Length = length;
            _buffer = ArrayPool<T>.Shared.Rent(length);
        }

        public static implicit operator Memory<T>(in RentedBuffer<T> buffer) => buffer.Memory;

        public static implicit operator Span<T>(in RentedBuffer<T> buffer) => buffer.Span;

        public static explicit operator ArraySegment<T>(in RentedBuffer<T> buffer) => buffer.Segment;

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
}