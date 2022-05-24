#if !NETCOREAPP3_1_OR_GREATER
using System;
using System.Diagnostics.CodeAnalysis;

namespace RedisCore.Tests;

[SuppressMessage("Microsoft.Security", "CA5394: Do not use insecure randomness",
                 Justification = "This is extension method to the Random class used only in tests")]
public static class RandomExtensions
{
    public static void NextBytes(this Random random, Span<byte> buffer)
    {
        if (random == null)
            throw new ArgumentNullException(nameof(random));

        for (var i = 0; i < buffer.Length; ++i)
            buffer[i] = (byte)random.Next();
    }
}
#endif