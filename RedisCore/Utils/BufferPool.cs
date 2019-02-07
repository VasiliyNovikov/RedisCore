using System;
using System.Buffers;

namespace RedisCore.Utils
{
    public interface IBufferPool<out T> : IDisposable
    {
        T[] Rent(int minimumLength);
        void Clear();
    }

    public static class BufferPool
    {
        public static IBufferPool<T> Empty<T>() => EmptyImplementation<T>.Instance;

        public static IBufferPool<T> Create<T>() => new Implementation<T>();

        public static Memory<T> RentMemory<T>(this IBufferPool<T> bufferPool, int length) => bufferPool.Rent(length).AsMemory(0, length);

        private class Implementation<T> : IBufferPool<T>
        {
            private T[][] _buffers;
            private int _length;

            public T[] Rent(int minimumLength)
            {
                if (_buffers == null)
                    _buffers = ArrayPool<T[]>.Shared.Rent(1);
                else if (_length == _buffers.Length)
                {
                    var newBuffers = ArrayPool<T[]>.Shared.Rent(_buffers.Length * 2);
                    _buffers.CopyTo(newBuffers, 0);
                    ArrayPool<T[]>.Shared.Return(_buffers);
                    _buffers = newBuffers;
                }

                return _buffers[_length++] = ArrayPool<T>.Shared.Rent(minimumLength);
            }

            ~Implementation()
            {
                Clear();
            }

            public void Dispose()
            {
                Clear();
                GC.SuppressFinalize(this);
            }

            public void Clear()
            {
                if (_buffers == null)
                    return;

                for (var i = 0; i < _length; ++i)
                    ArrayPool<T>.Shared.Return(_buffers[i]);
                ArrayPool<T[]>.Shared.Return(_buffers);

                _buffers = null;
                _length = 0;
            }
        }

        private class EmptyImplementation<T> : IBufferPool<T>
        {
            public static readonly EmptyImplementation<T> Instance = new EmptyImplementation<T>();

            public T[] Rent(int minimumLength) => new T[minimumLength];

            public void Dispose()
            {
            }

            public void Clear()
            {
            }
        }
    }
}