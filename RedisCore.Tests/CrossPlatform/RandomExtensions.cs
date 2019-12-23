#if NETSTANDARD20
using System;

namespace RedisCore.Tests
{
    public static class RandomExtensions
    {
        public static void NextBytes(this Random random, Span<byte> buffer)
        {
            for (var i = 0; i < buffer.Length; ++i)
                buffer[i] = (byte)random.Next();
        }
    }
}
#endif