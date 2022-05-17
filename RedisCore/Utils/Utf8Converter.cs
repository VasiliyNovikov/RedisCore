using System;
using System.Buffers;
using System.Buffers.Text;
using RedisCore.Internal.Protocol;

namespace RedisCore.Utils
{
    public static class Utf8Converter
    {
        public static bool TryParse<T>(ReadOnlySpan<byte> source, out T value, out int bytesConsumed, char standardFormat = '\0')
        {
            return ParseFunctionality<T>.Invoke(source, out value, out bytesConsumed, standardFormat);
        }

        public static bool TryParse<T>(ReadOnlySequence<byte> source, out T value, out int bytesConsumed, char standardFormat = '\0')
        {
            var maxSize = FormattedSize.Value<T>();
            if (source.IsSingleSegment || source.First.Length >= maxSize)
                return TryParse(source.First.Span, out value, out bytesConsumed, standardFormat);

            if (source.Length < maxSize)
                maxSize = (int)source.Length;

            using var buffer = new RentedBuffer<byte>(maxSize);
            source.CopyTo(buffer);
            return TryParse(buffer.Span, out value, out bytesConsumed, standardFormat);
        }

        public static bool TryFormat<T>(T value, Span<byte> destination, out int bytesWritten, StandardFormat format = default)
        {
            return FormatFunctionality<T>.Invoke(value, destination, out bytesWritten, format);
        }
        
        static Utf8Converter()
        {
            ParseFunctionality<int>.Implement(Utf8Parser.TryParse);
            ParseFunctionality<uint>.Implement(Utf8Parser.TryParse);
            ParseFunctionality<long>.Implement(Utf8Parser.TryParse);
            ParseFunctionality<ulong>.Implement(Utf8Parser.TryParse);
            ParseFunctionality<short>.Implement(Utf8Parser.TryParse);
            ParseFunctionality<ushort>.Implement(Utf8Parser.TryParse);
            ParseFunctionality<byte>.Implement(Utf8Parser.TryParse);
            ParseFunctionality<sbyte>.Implement(Utf8Parser.TryParse);
            ParseFunctionality<bool>.Implement(Utf8Parser.TryParse);
            ParseFunctionality<double>.Implement(Utf8Parser.TryParse);
            ParseFunctionality<float>.Implement(Utf8Parser.TryParse);
            ParseFunctionality<Guid>.Implement(Utf8Parser.TryParse);
            ParseFunctionality<DateTime>.Implement(Utf8Parser.TryParse);
            ParseFunctionality<DateTimeOffset>.Implement(Utf8Parser.TryParse);
            ParseFunctionality<TimeSpan>.Implement(Utf8Parser.TryParse);
            
            FormatFunctionality<int>.Implement(Utf8Formatter.TryFormat);
            FormatFunctionality<uint>.Implement(Utf8Formatter.TryFormat);
            FormatFunctionality<long>.Implement(Utf8Formatter.TryFormat);
            FormatFunctionality<ulong>.Implement(Utf8Formatter.TryFormat);
            FormatFunctionality<short>.Implement(Utf8Formatter.TryFormat);
            FormatFunctionality<ushort>.Implement(Utf8Formatter.TryFormat);
            FormatFunctionality<byte>.Implement(Utf8Formatter.TryFormat);
            FormatFunctionality<sbyte>.Implement(Utf8Formatter.TryFormat);
            FormatFunctionality<bool>.Implement(Utf8Formatter.TryFormat);
            FormatFunctionality<double>.Implement(Utf8Formatter.TryFormat);
            FormatFunctionality<float>.Implement(Utf8Formatter.TryFormat);
            FormatFunctionality<Guid>.Implement(Utf8Formatter.TryFormat);
            FormatFunctionality<DateTime>.Implement(Utf8Formatter.TryFormat);
            FormatFunctionality<DateTimeOffset>.Implement(Utf8Formatter.TryFormat);
            FormatFunctionality<TimeSpan>.Implement(Utf8Formatter.TryFormat);
        }
        
        private delegate bool TryParseDelegate<T>(ReadOnlySpan<byte> source, out T value, out int bytesConsumed, char standardFormat);
        private delegate bool TryFormatDelegate<in T>(T value, Span<byte> destination, out int bytesWritten, StandardFormat format);
        
        private class ParseFunctionality<T> : Functionality<ParseFunctionality<T>, T>
        {
            private readonly TryParseDelegate<T> _parser;

            private ParseFunctionality(TryParseDelegate<T> parser)
            {
                _parser = parser;
            }

            public static bool Invoke(ReadOnlySpan<byte> source, out T value, out int bytesConsumed, char standardFormat)
            {
                return Instance._parser(source, out value, out bytesConsumed, standardFormat);
            }

            public static void Implement(TryParseDelegate<T> parser)
            {
                Instance = new ParseFunctionality<T>(parser);
            }
        }
        
        private class FormatFunctionality<T> : Functionality<FormatFunctionality<T>, T>
        {
            private readonly TryFormatDelegate<T> _formatter;

            private FormatFunctionality(TryFormatDelegate<T> formatter)
            {
                _formatter = formatter;
            }

            public static bool Invoke(T value, Span<byte> destination, out int bytesWritten, StandardFormat format)
            {
                return Instance._formatter(value, destination, out bytesWritten, format);
            }

            public static void Implement(TryFormatDelegate<T> formatter)
            {
                Instance = new FormatFunctionality<T>(formatter);
            }
        }
    }
}