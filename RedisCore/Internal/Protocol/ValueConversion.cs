using System;
using RedisCore.Utils;

namespace RedisCore.Internal.Protocol
{
    internal static class ValueConversion
    {
        public static T To<T>(this RedisByteString value)
        {
            return Converter<RedisByteString, T>.Invoke(value);
        }

        public static T To<T>(this RedisCharString value)
        {
            return Converter<RedisCharString, T>.Invoke(value);
        }

        public static T To<T>(this RedisNull value)
        {
            return Converter<RedisNull, T>.Invoke(value);
        }

        public static T To<T>(this RedisInteger value)
        {
            return Converter<RedisInteger, T>.Invoke(value);
        }
        
        public static T To<T>(this RedisValueObject value)
        {
            switch (value)
            {
                case RedisInteger intValue:
                    return intValue.To<T>();
                case RedisNull nullValue:
                    return nullValue.To<T>();
                case RedisByteString byteValue:
                    return byteValue.To<T>();
                case RedisCharString charValue:
                    return charValue.To<T>();
                default:
                    throw new NotImplementedException();
            }
        }

        public static T To<T>(this RedisString value)
        {
            switch (value)
            {
                case RedisByteString byteValue:
                    return byteValue.To<T>();
                case RedisCharString charValue:
                    return charValue.To<T>();
                default:
                    throw new NotImplementedException();
            }
        }

        public static RedisValueObject ToValue<T>(this T value)
        {
            return Creator<T>.Invoke(value);
        }
        
        static ValueConversion()
        {
            Creator<string>.Implement(v => v == null ? (RedisValueObject)RedisNull.Value : new RedisCharString(v));
            Creator<byte[]>.Implement(v => v == null ? (RedisValueObject)RedisNull.Value : new RedisByteString(v));
            void CreateStringWithUtf8<T>() where T : struct
            {
                Creator<T>.Implement(v =>
                {
                    Memory<byte> buffer = new byte[FormattedSize.Value<T>()];
                    Utf8Converter.TryFormat(v, buffer.Span, out var bytesWritten);
                    return new RedisByteString(buffer.Slice(0, bytesWritten));
                });
            }
            CreateStringWithUtf8<int>();
            CreateStringWithUtf8<long>();
            CreateStringWithUtf8<double>();
            CreateStringWithUtf8<Guid>();
            Creator<int?>.Implement(v => v == null ? RedisNull.Value : v.Value.ToValue());
            Creator<long?>.Implement(v => v == null ? RedisNull.Value : v.Value.ToValue());
            Creator<double?>.Implement(v => v == null ? RedisNull.Value : v.Value.ToValue());
            Creator<Guid?>.Implement(v => v == null ? RedisNull.Value : v.Value.ToValue());
            Creator<bool>.Implement(v => (v ? 1 : 0).ToValue());
            Creator<bool?>.Implement(v => v == null ? RedisNull.Value : v.Value.ToValue());
            
            
            Converter<RedisNull, string>.Implement(v => null);
            Converter<RedisNull, byte[]>.Implement(v => null);
            Converter<RedisNull, int?>.Implement(v => null);
            Converter<RedisNull, long?>.Implement(v => null);
            Converter<RedisNull, double?>.Implement(v => null);
            Converter<RedisNull, bool?>.Implement(v => null);

            Converter<RedisByteString, string>.Implement(v => v.ToString());
            Converter<RedisByteString, byte[]>.Implement(v => v.Value.ToArray());
            void ConvertStringWithUtf8<T>()
            {
                Converter<RedisByteString, T>.Implement(v => Utf8Converter.TryParse(v.Value.Span, out T result, out var bytesRead) && bytesRead == v.Value.Length
                    ? result
                    : throw new FormatException($"String {v} is not valid {typeof(int)}"));
            }
            ConvertStringWithUtf8<int>();
            ConvertStringWithUtf8<long>();
            ConvertStringWithUtf8<double>();
            ConvertStringWithUtf8<Guid>();
            Converter<RedisByteString, int?>.Implement(v => v.To<int>());
            Converter<RedisByteString, long?>.Implement(v => v.To<long>());
            Converter<RedisByteString, double?>.Implement(v => v.To<double>());
            Converter<RedisByteString, Guid?>.Implement(v => v.To<Guid>());
            Converter<RedisByteString, bool>.Implement(v => v.To<int>() != 0);
            Converter<RedisByteString, bool?>.Implement(v => v.To<bool>());
            
            Converter<RedisCharString, string>.Implement(v => v.Value);
            Converter<RedisCharString, byte[]>.Implement(v => ProtocolHandler.Encoding.GetBytes(v.Value));
            Converter<RedisCharString, int>.Implement(v => Int32.Parse(v.Value));
            Converter<RedisCharString, int?>.Implement(v => v.To<int>());
            Converter<RedisCharString, long>.Implement(v => Int64.Parse(v.Value));
            Converter<RedisCharString, long?>.Implement(v => v.To<long>());
            Converter<RedisCharString, double>.Implement(v => Double.Parse(v.Value));
            Converter<RedisCharString, double?>.Implement(v => v.To<double>());
            Converter<RedisCharString, Guid>.Implement(v => Guid.Parse(v.Value));
            Converter<RedisCharString, Guid?>.Implement(v => v.To<Guid>());
            Converter<RedisCharString, bool>.Implement(v => v.To<int>() != 0);
            Converter<RedisCharString, bool?>.Implement(v => v.To<bool>());

            Converter<RedisInteger, string>.Implement(v => v.ToString());
            Converter<RedisInteger, int>.Implement(v => (int)v.Value);
            Converter<RedisInteger, int?>.Implement(v => (int)v.Value);
            Converter<RedisInteger, long>.Implement(v => v.Value);
            Converter<RedisInteger, long?>.Implement(v => v.Value);
            Converter<RedisInteger, double>.Implement(v => v.Value);
            Converter<RedisInteger, double?>.Implement(v => v.Value);
            Converter<RedisInteger, bool>.Implement(v => v.Value != 0);
            Converter<RedisInteger, bool?>.Implement(v => v.Value != 0);
        }
        
        private class Creator<T> : Functionality<T>
        {
            private readonly Func<T, RedisValueObject> _create;

            private Creator(Func<T, RedisValueObject> create) => _create = create;

            public static void Implement(Func<T, RedisValueObject> create)
            {
                Implementation<Creator<T>>.Instance = new Creator<T>(create);
            }

            public static RedisValueObject Invoke(T value)
            {
                return Implementation<Creator<T>>.Instance._create(value);
            }
        }
        
        private class Converter<TValue, T> : Functionality<T>
            where TValue : RedisValueObject
        {
            private readonly Func<TValue, T> _convert;

            private Converter(Func<TValue, T> convert) => _convert = convert;

            public static void Implement(Func<TValue, T> convert)
            {
                Implementation<Converter<TValue, T>>.Instance = new Converter<TValue, T>(convert);
            }

            public static T Invoke(TValue value)
            {
                return Implementation<Converter<TValue, T>>.Instance._convert(value);
            }
        }
    }
}