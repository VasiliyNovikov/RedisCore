#if !NET6_0_OR_GREATER
namespace System;

public static class RandomExtensions
{
    public static void NextBytes(this Random random, Span<byte> buffer)
    {
        for (var i = 0; i < buffer.Length; ++i)
            buffer[i] = (byte)random.Next();
    }
}
#endif