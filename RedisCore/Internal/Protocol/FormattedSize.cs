using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace RedisCore.Internal.Protocol;

public static class FormattedSize
{
    public static int Value<T>()
    {
        return Container<T>.Value;
    }

    [SuppressMessage("Microsoft.Performance", "CA1810: Initialize reference type static fields inline", Justification = "It is intentional")]
    static FormattedSize()
    {
        Container<SByte>.Value = ProtocolHandler.Encoding.GetByteCount(SByte.MinValue.ToString(CultureInfo.InvariantCulture)) + 4;
        Container<Byte>.Value = ProtocolHandler.Encoding.GetByteCount(Byte.MaxValue.ToString(CultureInfo.InvariantCulture)) + 4;
        Container<Int16>.Value = ProtocolHandler.Encoding.GetByteCount(Int16.MinValue.ToString(CultureInfo.InvariantCulture)) + 4;
        Container<UInt16>.Value = ProtocolHandler.Encoding.GetByteCount(UInt16.MaxValue.ToString(CultureInfo.InvariantCulture)) + 4;
        Container<Int32>.Value = ProtocolHandler.Encoding.GetByteCount(Int32.MinValue.ToString(CultureInfo.InvariantCulture)) + 4;
        Container<UInt32>.Value = ProtocolHandler.Encoding.GetByteCount(UInt32.MaxValue.ToString(CultureInfo.InvariantCulture)) + 4;
        Container<Int64>.Value = ProtocolHandler.Encoding.GetByteCount(Int64.MinValue.ToString(CultureInfo.InvariantCulture)) + 4;
        Container<UInt64>.Value = ProtocolHandler.Encoding.GetByteCount(UInt64.MaxValue.ToString(CultureInfo.InvariantCulture)) + 4;
        Container<Guid>.Value = ProtocolHandler.Encoding.GetByteCount(Guid.Empty.ToString()) + 2;
        Container<Double>.Value = 32;
        Container<Single>.Value = 32;
        Container<Boolean>.Value = 5;
        Container<DateTime>.Value = 128;
        Container<DateTimeOffset>.Value = 128;
        Container<TimeSpan>.Value = 128;
    }

    private static class Container<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static int Value = -1;
    }
}