#if NETSTANDARD
using System;
using System.Text;

namespace RedisCore.CrossPlatform
{
    public static class EncodingExtensions
    {
        public static unsafe int GetBytes(this Encoding encoding, string source, Span<byte> destination)
        {
            fixed (char* sourcePtr = &source.AsSpan().GetPinnableReference())
            fixed (byte* destinationPtr = &destination.GetPinnableReference())
                return encoding.GetBytes(sourcePtr, source.Length, destinationPtr, destination.Length);
        }

        public static unsafe string GetString(this Encoding encoding, ReadOnlySpan<byte> source)
        {
            fixed (byte* sourcePtr = &source.GetPinnableReference())
                return encoding.GetString(sourcePtr, source.Length);
        }
    }
}
#endif