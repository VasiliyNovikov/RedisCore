#if !NETCOREAPP3_1_OR_GREATER
using System;

namespace RedisCore;

public static class TimeSpanExtensions
{
    public static TimeSpan Multiply(this TimeSpan timeSpan, double factor)
    {
#pragma warning disable IDE0046 // Use conditional expression for return
        if (Double.IsNaN(factor))
            throw new ArgumentException("Factor can't be NaN", nameof(factor));
        var num = Math.Round(timeSpan.Ticks * factor);
        if (num > Int64.MaxValue | num < Int64.MinValue)
            throw new OverflowException("Result TimeSpan is too long");
        return TimeSpan.FromTicks((long)num);
#pragma warning restore IDE0046
    }
}
#endif