using System;
using System.Collections.Generic;
using System.Reflection;
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

        public static T To<T>(this RedisArray value)
        {
            return CollectionConverter<T>.Invoke(value);
        }

        public static T To<T>(this RedisObject value)
        {
            switch (value)
            {
                case RedisValueObject valueValue:
                    return valueValue.To<T>();
                case RedisArray arrayValue:
                    return arrayValue.To<T>();
                default:
                    throw new NotImplementedException();
            }
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

        public static Memory<byte>? To(this RedisObject value, IBufferPool<byte> bufferPool)
        {
            switch (value)
            {
                case RedisByteString byteValue:
                {
                    var result = bufferPool.RentMemory(byteValue.ByteLength);
                    byteValue.Value.CopyTo(result);
                    return result;
                }
                case RedisCharString charValue:
                {
                    var result = bufferPool.RentMemory(charValue.ByteLength);
                    ProtocolHandler.Encoding.GetBytes(charValue.Value, result.Span);
                    return result;
                }
                case RedisNull _:
                    return null;
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
            Creator<string?>.Implement(v => v == null ? RedisNull.Value : new RedisCharString(v));
            Creator<byte[]?>.Implement(v => v == null ? RedisNull.Value : new RedisByteString(v));
            Creator<ReadOnlyMemory<byte>>.Implement(v => new RedisByteString(v));
            Creator<Memory<byte>>.Implement(v => new RedisByteString(v));
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
            
            Converter<RedisNull, string?>.Implement(_ => null);
            Converter<RedisNull, byte[]?>.Implement(_ => null);

            void ConvertNullable<T>() where T : struct
            {
                Converter<RedisNull, T>.Implement(_ => throw new ArgumentNullException(null, "Value is null"));
            }
            
            ConvertNullable<int>();
            ConvertNullable<long>();
            ConvertNullable<double>();
            ConvertNullable<Guid>();
            ConvertNullable<bool>();

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
            Converter<RedisByteString, bool>.Implement(v => v.To<int>() != 0);
            
            Converter<RedisCharString, string>.Implement(v => v.Value);
            Converter<RedisCharString, byte[]>.Implement(v => ProtocolHandler.Encoding.GetBytes(v.Value));
            Converter<RedisCharString, int>.Implement(v => Int32.Parse(v.Value));
            Converter<RedisCharString, long>.Implement(v => Int64.Parse(v.Value));
            Converter<RedisCharString, double>.Implement(v => Double.Parse(v.Value));
            Converter<RedisCharString, Guid>.Implement(v => Guid.Parse(v.Value));
            Converter<RedisCharString, bool>.Implement(v => v.To<int>() != 0);

            Converter<RedisInteger, string>.Implement(v => v.ToString());
            Converter<RedisInteger, int>.Implement(v => (int)v.Value);
            Converter<RedisInteger, long>.Implement(v => v.Value);
            Converter<RedisInteger, double>.Implement(v => v.Value);
            Converter<RedisInteger, bool>.Implement(v => v.Value != 0);
        }
        
        private class Creator<T> : Functionality<Creator<T>, T>
        {
            private readonly Func<T, RedisValueObject> _create;

            private Creator(Func<T, RedisValueObject> create) => _create = create;

            public static void Implement(Func<T, RedisValueObject> create)
            {
                Instance = new Creator<T>(create);
                Creator<Optional<T>>.Instance = new Creator<Optional<T>>(value => value.HasValue ? create(value.Value) : RedisNull.Value);
                if (typeof(T).IsValueType)
                {
                    var implementNullableMethod = typeof(Creator<T>).GetMethod(nameof(ImplementNullable), BindingFlags.Static | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(T));
                    implementNullableMethod.Invoke(null, new object[] {create});
                }
            }

            private static void ImplementNullable<TN>(Func<TN, RedisValueObject> create)
                where TN : struct
            {
                Creator<TN?>.Instance = new Creator<TN?>(value => value.HasValue ? create(value.Value) : RedisNull.Value);
            }

            public static RedisValueObject Invoke(T value)
            {
                return Instance._create(value);
            }
        }
        
        private class Converter<TValue, T> : Functionality<Converter<TValue, T>, T>
            where TValue : RedisObject
        {
            private readonly Func<TValue, T> _convert;

            internal Converter(Func<TValue, T> convert) => _convert = convert;

            public T Convert(TValue value) => _convert(value);

            public static void Implement(Func<TValue, T> convert)
            {
                Instance = new Converter<TValue, T>(convert);
                Converter<TValue, Optional<T>>.Instance = typeof(TValue) == typeof(RedisNull)
                    ? new Converter<TValue, Optional<T>>(_ => Optional<T>.Unspecified)
                    : new Converter<TValue, Optional<T>>(value => convert(value));
                if (typeof(T).IsValueType)
                {
                    var implementNullableMethod = typeof(Converter<TValue, T>).GetMethod(nameof(ImplementNullable), BindingFlags.Static | BindingFlags.NonPublic)!
                                                                              .MakeGenericMethod(typeof(T));
                    implementNullableMethod.Invoke(null, new object[] {convert});
                }
            }

            private static void ImplementNullable<TN>(Func<TValue, TN> convert)
                where TN : struct
            {
                if (typeof(TValue) == typeof(RedisNull))
                {
                    Converter<TValue, TN?>.Instance = new Converter<TValue, TN?>(_ => null);
                    Converter<TValue, Optional<TN?>>.Instance = new Converter<TValue, Optional<TN?>>(_ => Optional<TN?>.Unspecified);
                }
                else
                {
                    Converter<TValue, TN?>.Instance = new Converter<TValue, TN?>(value => convert(value));
                    Converter<TValue, Optional<TN?>>.Instance = new Converter<TValue, Optional<TN?>>(value => convert(value));
                }
            }

            public static T Invoke(TValue value)
            {
                return Instance.Convert(value);
            }
        }

        private class CollectionConverter<T> : DynamicFunctionality<CollectionConverter<T>, Converter<RedisArray, T>, T>
        {
            protected override Converter<RedisArray, T> CreateInstance()
            {
                var type = typeof(T);
                Type elementType;
                string methodName;
                if (type.IsArray)
                {
                    elementType = type.GetElementType()!;
                    methodName = nameof(CreateArray);
                }
                else if (type.IsGenericType)
                {
                    elementType = type.GetGenericArguments()[0];
                    if (type.IsAssignableFrom(elementType.MakeArrayType()))
                        methodName = nameof(CreateArray);
                    else if (type.IsAssignableFrom(typeof(List<>).MakeGenericType(elementType)))
                        methodName = nameof(CreateList);
                    else if (type.IsAssignableFrom(typeof(HashSet<>).MakeGenericType(elementType)))
                        methodName = nameof(CreateSet);
                    else
                        throw new NotSupportedException($"{type} is not supported collection type");
                }
                else
                    throw new NotSupportedException($"{type} is not supported collection type");
                
                var method = typeof(CollectionConverter<T>).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)!
                                                           .MakeGenericMethod(elementType);
                return new Converter<RedisArray, T>((Func<RedisArray, T>) Delegate.CreateDelegate(typeof(Func<RedisArray, T>), method));
            }

            private static TItem[] CreateArray<TItem>(RedisArray value)
            {
                var items = value.Items;
                var result = new TItem[items.Count];
                for (var i = 0; i < items.Count; ++i) 
                    result[i] = items[i].To<TItem>();
                return result;
            }

            private static List<TItem> CreateList<TItem>(RedisArray value)
            {
                var items = value.Items;
                var result = new List<TItem>(items.Count);
                foreach (var item in items)
                    result.Add(item.To<TItem>());
                return result;
            }
            
            private static HashSet<TItem> CreateSet<TItem>(RedisArray value)
            {
                var items = value.Items;
#if NETCOREAPP3_1_OR_GREATER
                var result = new HashSet<TItem>(items.Count);
#else
                var result = new HashSet<TItem>();
#endif
                foreach (var item in items)
                    result.Add(item.To<TItem>());
                return result;
            }

            public static T Invoke(RedisArray value)
            {
                return Instance.Convert(value);
            }
        }
    }
}