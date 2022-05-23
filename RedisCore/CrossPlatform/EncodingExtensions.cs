#if !NETCOREAPP3_1_OR_GREATER
using System;
using System.Text;

namespace RedisCore;

public static class EncodingExtensions
{
    public static unsafe int GetBytes(this Encoding encoding, string source, Span<byte> destination)
    {
        if (encoding == null)
            throw new ArgumentNullException(nameof(encoding));
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        fixed (char* sourcePtr = &source.AsSpan().GetPinnableReference())
        fixed (byte* destinationPtr = &destination.GetPinnableReference())
            return encoding.GetBytes(sourcePtr, source.Length, destinationPtr, destination.Length);
    }

    public static unsafe string GetString(this Encoding encoding, ReadOnlySpan<byte> source)
    {
        if (encoding == null)
            throw new ArgumentNullException(nameof(encoding));

        fixed (byte* sourcePtr = &source.GetPinnableReference())
            return encoding.GetString(sourcePtr, source.Length);
    }
}
#endif