using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using RedisCore.Utils;

namespace RedisCore.Internal.Protocol
{
    internal static class ProtocolHandler
    {
        internal static readonly Encoding Encoding = new UTF8Encoding(false);
        
        private static readonly ReadOnlyMemory<byte> NullData = Encoding.GetBytes("$-1");
        private static readonly ReadOnlyMemory<byte> NewLine = Encoding.GetBytes("\r\n");
        private static readonly ReadOnlyMemory<byte> IntPrefix = Encoding.GetBytes(":");
        private static readonly ReadOnlyMemory<byte> StrLenPrefix = Encoding.GetBytes("$");
        private static readonly ReadOnlyMemory<byte> ArrayLenPrefix = Encoding.GetBytes("*");

        private static async ValueTask WriteScalar<T>(Connection connection, T value) where T : struct
        {
            using (var buffer = new RentedBuffer<byte>(FormattedSize.Value<T>()))
            {
                Utf8Converter.TryFormat(value, buffer, out var bytesWritten);
                await connection.Write(buffer.Memory.Slice(0, bytesWritten));
            }
        }
        
        private static async ValueTask WriteString(Connection connection, RedisString value)
        {
            if (value is RedisByteString byteValue)
            {
                await connection.Write(byteValue.Value);
                return;
            }

            var charValue = (RedisCharString) value;
            using (var buffer = new RentedBuffer<byte>(charValue.ByteLength))
            {
                Encoding.GetBytes(charValue.Value, buffer);
                await connection.Write(buffer.Memory);
            }
        }

        public static async ValueTask Write(Connection connection, RedisObject @object)
        {
            switch (@object)
            {
                case RedisNull _:
                    await connection.Write(NullData);
                    await connection.Write(NewLine);
                    return;
                
                case RedisInteger intObject:
                    await connection.Write(IntPrefix);
                    await WriteScalar(connection, intObject.Value);
                    await connection.Write(NewLine);
                    return;
                
                case RedisString strObject:
                    await connection.Write(StrLenPrefix);
                    await WriteScalar(connection, strObject.ByteLength);
                    await connection.Write(NewLine);

                    await WriteString(connection, strObject);
                    await connection.Write(NewLine);
                    return;

                case RedisArray arrayObject:
                    await connection.Write(ArrayLenPrefix);
                    await WriteScalar(connection, arrayObject.Items.Count);
                    await connection.Write(NewLine);
                    
                    foreach (var item in arrayObject.Items)
                        await Write(connection, item);
                    return;

                default:
                    throw new ArgumentException($"Unsuppoted object {@object.GetType()}", nameof(@object));
            }
        }

        public static async ValueTask<RedisObject> Read(Connection connection)
        {
            var buffer = new RentedBuffer<byte>(64);
            try
            {
                var totalBytesRead = 0;
                var isCarriageReturn = false;
                while (true)
                {
                    if (totalBytesRead == buffer.Length)
                    {
                        buffer.Dispose();
                        buffer = new RentedBuffer<byte>(buffer.Length * 2);
                    }

                    if (await connection.Read(buffer.Memory.Slice(totalBytesRead, 1)) == 0)
                        throw new ProtocolException("Expected CRLF");

                    if (buffer.Span[totalBytesRead] == '\n' && isCarriageReturn)
                        break;

                    isCarriageReturn = buffer.Span[totalBytesRead] == '\r';
                    totalBytesRead += 1;
                }

                var lineType = (char)buffer.Span[0];
                var line = buffer.Memory.Slice(1, totalBytesRead - 2);
                
                switch (lineType)
                {
                    case '+':
                        return new RedisCharString(Encoding.GetString(line.Span));
                    case ':':
                    {
                        if (!Utf8Parser.TryParse(line.Span, out long intValue, out var bytesConsumed) || bytesConsumed != line.Length)
                            throw new ProtocolException("Expected integer value");
                        return new RedisInteger(intValue);
                    }
                    case '$':
                    {
                        if (!Utf8Parser.TryParse(line.Span, out int length, out var bytesConsumed) || bytesConsumed != line.Length)
                            throw new ProtocolException("Expected string length");
                        if (length < 0)
                            return RedisNull.Value;

                        Memory<byte> strBuffer = new byte[length + 2];
                        totalBytesRead = 0;
                        while (totalBytesRead < strBuffer.Length)
                        {
                            var bytesRead = await connection.Read(strBuffer.Slice(totalBytesRead));
                            if (bytesRead == 0)
                                throw new ProtocolException("Expected fixed size string ended with CRLF");
                            totalBytesRead += bytesRead;
                        }

                        if (!strBuffer.Span.Slice(length).SequenceEqual(NewLine.Span))
                            throw new ProtocolException("Expected CRLF");

                        return new RedisByteString(strBuffer.Slice(0, length));
                    }
                    case '*':
                    {
                        if (!Utf8Parser.TryParse(line.Span, out int length, out var bytesConsumed) || bytesConsumed != line.Length)
                            throw new ProtocolException("Expected array length");
                        if (length < 0)
                            return RedisNull.Value;
                        var items = new List<RedisObject>(length);
                        for (var i = 0; i < length; ++i)
                            items.Add(await Read(connection));
                        return new RedisArray(items);
                    }
                    case '-':
                        var errorMessagePos = line.Span.IndexOf((byte) ' ') + 1;
                        if (errorMessagePos <= 0)
                            return new RedisError(null, Encoding.GetString(line.Span));
                        return new RedisError(Encoding.GetString(line.Span.Slice(0, errorMessagePos - 1)), Encoding.GetString(line.Span.Slice(errorMessagePos)));
                    default:
                        throw new FormatException($"Invalid line returned from server: {lineType}{Encoding.GetString(line.Span)}");
                }
            }
            finally 
            {
                buffer.Dispose();
            }
        }
    }
}