using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace RedisCore.Utils;

public static class MonotonicTime
{
    private static readonly Implementation Impl = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
        ? new LinuxImplementation()
        : new DefaultImplementation();

    public static TimeSpan Now => Impl.GetTime();

    private abstract class Implementation
    {
        public abstract TimeSpan GetTime();
    }

    private class DefaultImplementation : Implementation
    {
        private readonly Stopwatch _timer = Stopwatch.StartNew();

        public override TimeSpan GetTime() => _timer.Elapsed;
    }

    // See http://man7.org/linux/man-pages/man2/clock_gettime.2.html
    private class LinuxImplementation : Implementation
    {
        private const long NanosecondsPerSecond = 1_000_000_000;
        private const long NanosecondsPerTick = NanosecondsPerSecond / TimeSpan.TicksPerSecond;

        public override TimeSpan GetTime()
        {
            return clock_gettime(clockid_t.CLOCK_MONOTONIC_RAW, out var time) == 0
                ? new TimeSpan(((time.tv_sec * NanosecondsPerSecond) + time.tv_nsec + (NanosecondsPerTick / 2)) / NanosecondsPerTick)
                : throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        // ReSharper disable InconsistentNaming
        // ReSharper disable IdentifierTypo
        [SuppressMessage("Microsoft.Style", "IDE1006: Naming rule violation", Justification = "Platform interop type in C-notation")]
        private enum clockid_t
        {
            CLOCK_MONOTONIC_RAW = 4
        }


        [SuppressMessage("Microsoft.Style", "IDE1006: Naming rule violation", Justification = "Platform interop type in C-notation")]
        [StructLayout(LayoutKind.Sequential)]
        private struct timespec
        {
            public readonly long tv_sec;
            public readonly long tv_nsec;
        }

        [SuppressMessage("Microsoft.Style", "IDE1006: Naming rule violation", Justification = "Platform interop type in C-notation")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("libc.so.6", SetLastError = true)]
        private static extern int clock_gettime(clockid_t clk_id, out timespec tp);
        // ReSharper restore IdentifierTypo
        // ReSharper restore InconsistentNaming
    }
}