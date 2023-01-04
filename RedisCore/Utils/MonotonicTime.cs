using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RedisCore.Utils;

public static class MonotonicTime
{
    private static readonly Implementation Impl;

    static MonotonicTime()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Impl = new LinuxImplementation();
        else
            Impl = new DefaultImplementation();
            
    }
        
    public static TimeSpan Now => Impl.GetTime();

    private abstract class Implementation
    {
        public abstract TimeSpan GetTime();
    }

    private sealed class DefaultImplementation : Implementation
    {
        private readonly Stopwatch _timer = Stopwatch.StartNew();

        public override TimeSpan GetTime() => _timer.Elapsed;
    }

    // See http://man7.org/linux/man-pages/man2/clock_gettime.2.html
    private sealed class LinuxImplementation : Implementation
    {
        private const long NanosecondsPerSecond = 1_000_000_000;
        private const long NanosecondsPerTick = NanosecondsPerSecond / TimeSpan.TicksPerSecond;

        public override TimeSpan GetTime()
        {
            var result = clock_gettime(clockid_t.CLOCK_MONOTONIC_RAW, out var time);
            if (result != 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return new TimeSpan((time.tv_sec * NanosecondsPerSecond + time.tv_nsec + NanosecondsPerTick / 2) / NanosecondsPerTick);
        }

#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters
        private enum clockid_t
        {
            CLOCK_MONOTONIC_RAW = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct timespec
        {
            public readonly long tv_sec;
            public readonly long tv_nsec;
        }
#pragma warning restore CS8981

        [DllImport("libc.so.6", SetLastError = true)]
        private static extern int clock_gettime(clockid_t clk_id, out timespec tp);
    }
}