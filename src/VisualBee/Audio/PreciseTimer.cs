using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace MusicBeePlugin
{
    internal sealed class PreciseTimer : IDisposable
    {
        private const uint TIMER_ALL_ACCESS = 0x1F0003;
        private const uint CREATE_WAITABLE_TIMER_HIGH_RESOLUTION = 0x00000002;
        private const uint INFINITE = 0xFFFFFFFF;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWaitableTimerExW(
            IntPtr lpTimerAttributes,
            string lpTimerName,
            uint dwFlags,
            uint dwDesiredAccess);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWaitableTimer(
            IntPtr hTimer,
            [In] ref long lpDueTime,
            int lPeriod,
            IntPtr pfnCompletionRoutine,
            IntPtr lpArgToCompletionRoutine,
            [MarshalAs(UnmanagedType.Bool)] bool fResume);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("winmm.dll")]
        private static extern uint timeBeginPeriod(uint uPeriod);

        [DllImport("winmm.dll")]
        private static extern uint timeEndPeriod(uint uPeriod);

        private readonly IntPtr _timerHandle;
        private readonly bool _hasHighResTimer;
        private bool _disposed;

        static PreciseTimer()
        {
            try { timeBeginPeriod(1); } catch { }
        }

        public PreciseTimer()
        {
            try
            {
                _timerHandle = CreateWaitableTimerExW(IntPtr.Zero, null, CREATE_WAITABLE_TIMER_HIGH_RESOLUTION, TIMER_ALL_ACCESS);
                _hasHighResTimer = _timerHandle != IntPtr.Zero;
            }
            catch
            {
                _timerHandle = IntPtr.Zero;
                _hasHighResTimer = false;
            }
        }

        public void WaitUntil(long targetTicks)
        {
            long now = Stopwatch.GetTimestamp();
            if (now >= targetTicks) return;

            long ticksLeft = targetTicks - now;
            long freq = Stopwatch.Frequency;

            if (_hasHighResTimer && !_disposed)
            {
                // Convert to 100ns units (relative negative due time). Back off by 0.5 ms to spin the tail.
                double remainingSeconds = (double)ticksLeft / freq;
                double waitSeconds = remainingSeconds - 0.0005;

                if (waitSeconds > 0.0005)
                {
                    long dueTime = -((long)(waitSeconds * 10000000.0));
                    if (dueTime < 0 && SetWaitableTimer(_timerHandle, ref dueTime, 0, IntPtr.Zero, IntPtr.Zero, false))
                    {
                        WaitForSingleObject(_timerHandle, (uint)Math.Ceiling(waitSeconds * 1000.0 + 2.0));
                    }
                }
            }
            else
            {
                // Fallback: sleep if more than 2 ms remaining
                double remainingMs = (double)ticksLeft * 1000.0 / freq;
                if (remainingMs > 2.0)
                {
                    int sleepMs = (int)(remainingMs - 1.5);
                    if (sleepMs > 0) Thread.Sleep(sleepMs);
                }
            }

            // High-precision spin for the last sub-millisecond tail
            var spinner = new SpinWait();
            while (Stopwatch.GetTimestamp() < targetTicks)
            {
                spinner.SpinOnce();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_timerHandle != IntPtr.Zero)
            {
                try { CloseHandle(_timerHandle); } catch { }
            }
        }
    }
}
