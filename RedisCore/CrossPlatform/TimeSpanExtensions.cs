#if NETSTANDARD20
using System;

namespace RedisCore
{
    public static class TimeSpanExtensions
    {
        public static TimeSpan Multiply(this TimeSpan timeSpan, double factor)
        {
            if (Double.IsNaN(factor))
                throw new ArgumentException("Factor can't be NaN", nameof (factor));
            var num = Math.Round(timeSpan.Ticks * factor);
            if (num > Int64.MaxValue | num < Int64.MinValue)
                throw new OverflowException("Result TimeSpan is too long");
            return TimeSpan.FromTicks((long) num);
        }
    }
}
#endif