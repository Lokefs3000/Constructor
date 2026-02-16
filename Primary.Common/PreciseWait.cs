using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Primary.Common
{
    public static unsafe class PreciseWait
    {
        //https://blog.bearcats.nl/perfect-sleep-function/
        public static void Wait(double seconds)
        {
            if (OperatingSystem.IsWindows())
                PreciseWait_Win32Impl.Wait(seconds);
            else
                throw new PlatformNotSupportedException();
        }
    }

    internal unsafe static partial class PreciseWait_Win32Impl
    {
        static PreciseWait_Win32Impl()
        {
            timeBeginPeriod(PERIOD);
        }

        internal static void Wait(double seconds)
        {
            nint timerHandle = s_win32Timer.Value;

            long t = Stopwatch.GetTimestamp();
            long target = t + (long)(seconds * Stopwatch.Frequency);

            const long maxTicks = 1 * 9500;
            do
            {
                long remaining = target - t;
                long ticks = (remaining - TOLERANCE) / 100;
                if (ticks <= 0)
                    break;
                if (ticks > maxTicks)
                    ticks = maxTicks;

                LARGE_INTEGER due = default;
                due.QuadPart = (ulong)-ticks;
                SetWaitableTimerEx(timerHandle, ref due, 0, nint.Zero, nint.Zero, nint.Zero, 0);
                WaitForSingleObject(timerHandle, INFINITE);

                t = Stopwatch.GetTimestamp();
            }
            while (true);

            while (Stopwatch.GetTimestamp() < target)
                Thread.Yield();
        }

        [ThreadStatic]
        private static Lazy<nint> s_win32Timer = new Lazy<nint>(() =>
        {
            nint ret = CreateWaitableTimerExW(nint.Zero, null, CREATE_WAITABLE_TIMER_HIGH_RESOLUTION, TIMER_ALL_ACCESS);
            if (ret == nint.Zero)
            {
                throw new Exception($"Failed to create win32 timer: {GetLastError():x8}");
            }

            return ret;
        });

        [LibraryImport("winmm.dll")]
        public static partial uint timeBeginPeriod(uint uPeriod);

        [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
        public static partial nint CreateWaitableTimerExW(nint lpTimerAttributes, string? lpTimerName, uint dwFlags, uint dwDesiredAccess);

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SetWaitableTimerEx(nint hTimer, ref LARGE_INTEGER lpDueTime, long lPeriod, nint pfnCompletionRoutine, nint lpArgToCompletionRoutine, nint WakeContext, ulong TolerableDelay);

        [LibraryImport("kernel32.dll")]
        public static partial uint WaitForSingleObject(nint hHandle, ulong dwMilliseconds);

        [LibraryImport("kernel32.dll")]
        public static partial int GetLastError();

        public record struct LARGE_INTEGER
        {
            public ulong QuadPart;
        }

        private const uint CREATE_WAITABLE_TIMER_HIGH_RESOLUTION = 0x00000002;
        private const uint TIMER_ALL_ACCESS = 0x1f0003;

        private const uint PERIOD = 1;
        private const long TOLERANCE = 1020000;

        private const uint INFINITE = 0xffffffff;
    }
}
