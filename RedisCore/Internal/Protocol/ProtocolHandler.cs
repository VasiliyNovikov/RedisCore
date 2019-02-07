using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
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

        private static void Write(PipeWriter writer, ReadOnlyMemory<byte> value)
        {
            var buffer = writer.GetMemory(value.Length);
            value.CopyTo(buffer);
            writer.Advance(value.Length);
        }

        private static void WriteScalar<T>(PipeWriter writer, T value) where T : struct
        {
            var buffer = writer.GetMemory(FormattedSize.Value<T>());
            Utf8Converter.TryFormat(value, buffer.Span, out var bytesWritten);
            writer.Advance(bytesWritten);
        }
        
        private static void WriteString(PipeWriter writer, RedisString value)
        {
            var buffer = writer.GetMemory(value.ByteLength);
            if (value is RedisByteString byteValue)
                byteValue.Value.CopyTo(buffer);
            else
            {
                var charValue = (RedisCharString) value;
                Encoding.GetBytes(charValue.Value, buffer.Span);
            }

            writer.Advance(value.ByteLength);
        }

        public static void Write(PipeWriter writer, RedisObject @object)
        {
            switch (@object)
            {
                case RedisNull _:
                    Write(writer, NullData);
                    Write(writer, NewLine);
                    return;
                
                case RedisInteger intObject:
                    Write(writer, IntPrefix);
                    WriteScalar(writer, intObject.Value);
                    Write(writer, NewLine);
                    return;
                
                case RedisString strObject:
                    Write(writer, StrLenPrefix);
                    WriteScalar(writer, strObject.ByteLength);
                    Write(writer, NewLine);

                    WriteString(writer, strObject);
                    Write(writer, NewLine);
                    return;

                case RedisArray arrayObject:
                    Write(writer, ArrayLenPrefix);
                    WriteScalar(writer, arrayObject.Items.Count);
                    Write(writer, NewLine);
                    
                    foreach (var item in arrayObject.Items)
                        Write(writer, item);
                    return;

                default:
                    throw new ArgumentException($"Unsupported object {@object.GetType()}", nameof(@object));
            }
        }

        private static void CheckCompletedUnexpectedly(PipeReader reader, ReadResult readResult)
        {
            if (!readResult.IsCompleted)
                return;

            reader.AdvanceTo(readResult.Buffer.End);
            throw new EndOfStreamException();
        }

        private static SequencePosition? FindNewLine(ReadOnlySequence<byte> buffer)
        {
            var crPosition = buffer.PositionOf((byte)'\r');
            if (crPosition == null)
                return null;
            
            var lfPosition = buffer.GetPosition(1, crPosition.Value);
            if (lfPosition.Equals(buffer.End))
                return null;
            
            if (buffer.Slice(lfPosition).First.Span[0] != (byte) '\n')
                return null;
            
            return crPosition;
        }

        private static string GetString(ReadOnlySequence<byte> buffer)
        {
            if (buffer.IsSingleSegment)
                return Encoding.GetString(buffer.First.Span);
            
            using (var localBuffer = new RentedBuffer<byte>((int)buffer.Length))
            {
                buffer.CopyTo(localBuffer);
                return Encoding.GetString(localBuffer.Span);
            }
        }

        public static async ValueTask<RedisObject> Read(PipeReader reader, IBufferPool<byte> bufferPool, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                var readResult = await reader.ReadAsync(cancellationToken);
                var buffer = readResult.Buffer;
                var newLinePosition = FindNewLine(buffer);
                if (newLinePosition == null)
                {
                    CheckCompletedUnexpectedly(reader, readResult);
                    reader.AdvanceTo(buffer.Start, buffer.End);
                    continue;
                }

                var line = buffer.Slice(1, newLinePosition.Value);
                var lineType = (char) buffer.First.Span[0];
                
                var nextPosition = buffer.GetPosition(2, newLinePosition.Value);

                switch (lineType)
                {
                    case '+':
                    {
                        var str = GetString(line);
                        reader.AdvanceTo(nextPosition);
                        return new RedisCharString(str);
                    }
                    case ':':
                    {
                        if (!Utf8Converter.TryParse(line, out long intValue, out var bytesConsumed) ||
                            bytesConsumed != line.Length)
                            throw new ProtocolException("Expected integer value");
                        reader.AdvanceTo(nextPosition);
                        return new RedisInteger(intValue);
                    }
                    case '$':
                    {
                        if (!Utf8Converter.TryParse(line, out int length, out var bytesConsumed) ||
                            bytesConsumed != line.Length)
                            throw new ProtocolException("Expected string length");

                        reader.AdvanceTo(nextPosition);

                        if (length < 0)
                            return RedisNull.Value;

                        var strBuffer = bufferPool.RentMemory(length + 2);
                        var totalBytesRead = 0;
                        while (totalBytesRead < strBuffer.Length)
                        {
                            readResult = await reader.ReadAsync();
                            buffer = readResult.Buffer;
                            var bytesRead = (int)buffer.Length;
                            if (bytesRead == 0)
                                throw new ProtocolException("Expected fixed size string ended with CRLF");
                            if (totalBytesRead + bytesRead > strBuffer.Length)
                            {
                                bytesRead = strBuffer.Length - totalBytesRead;
                                buffer = buffer.Slice(buffer.Start, bytesRead);
                            }

                            buffer.CopyTo(strBuffer.Span.Slice(totalBytesRead));
                            reader.AdvanceTo(buffer.End);
                            totalBytesRead += bytesRead;
                            
                            if (totalBytesRead < strBuffer.Length)
                                CheckCompletedUnexpectedly(reader, readResult);
                        }

                        if (!strBuffer.Span.Slice(length).SequenceEqual(NewLine.Span))
                            throw new ProtocolException("Expected CRLF");

                        return new RedisByteString(strBuffer.Slice(0, length));
                    }
                    case '*':
                    {
                        if (!Utf8Converter.TryParse(line, out int length, out var bytesConsumed) || bytesConsumed != line.Length)
                            throw new ProtocolException("Expected array length");

                        reader.AdvanceTo(nextPosition);
                        
                        if (length < 0)
                            return RedisNull.Value;
                        var items = new List<RedisObject>(length);
                        for (var i = 0; i < length; ++i)
                            items.Add(await Read(reader, bufferPool));
                        return new RedisArray(items);
                    }
                    case '-':
                        var errorMessagePos = line.PositionOf((byte) ' ');
                        string type;
                        string message;
                        if (errorMessagePos == null)
                        {
                            type = null;
                            message = GetString(line);
                        }
                        else
                        {
                            type = GetString(line.Slice(0, errorMessagePos.Value));
                            message = GetString(line.Slice(line.GetPosition(1, errorMessagePos.Value)));
                        }
                        
                        reader.AdvanceTo(nextPosition);

                        return new RedisError(type, message);
                    default:
                        throw new FormatException($"Invalid line returned from server: {lineType}{GetString(line)}");
                }
            }
        }
    }
}