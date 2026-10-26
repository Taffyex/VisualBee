using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using SharpDX.DXGI;

namespace MusicBeePlugin
{
    internal sealed class FramePacer : IDisposable
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        private readonly PreciseTimer _timer = new PreciseTimer();
        private IntPtr _waitableObject = IntPtr.Zero;
        private bool _tearingSupported;
        private bool _useTearing;
        private int _frameCap;
        private bool _isVsync;

        private long _nextDeadlineTicks;
        private long _idleSinceTicks;
        private bool _isThrottled;
        private int _effectiveTargetFps = 60;
        private string _presentMode = "VSync";

        public bool TearingSupported => _tearingSupported;
        public string PresentMode => _presentMode;
        public bool IsThrottled => _isThrottled;
        public int EffectiveTargetFps => _effectiveTargetFps;

        public static void QueryCapabilities(Factory factory, out bool tearingSupported)
        {
            tearingSupported = false;
            if (factory == null) return;
            try
            {
                using (var f5 = factory.QueryInterfaceOrNull<Factory5>())
                {
                    if (f5 != null)
                    {
                        int allowTearing = 0;
                        unsafe
                        {
                            f5.CheckFeatureSupport(Feature.PresentAllowTearing, new IntPtr(&allowTearing), sizeof(int));
                        }
                        tearingSupported = (allowTearing != 0);
                    }
                }
            }
            catch { }
        }

        public static void DetermineSwapchainParams(
            int frameCap,
            bool tearingSupported,
            out SwapChainFlags flags,
            out bool isVsync)
        {
            if (frameCap == 0)
            {
                // VSync mode: low-latency waitable flip
                isVsync = true;
                flags = SwapChainFlags.FrameLatencyWaitAbleObject;
            }
            else
            {
                // High-refresh or Uncapped: allow tearing if supported
                isVsync = false;
                flags = tearingSupported ? SwapChainFlags.AllowTearing : SwapChainFlags.None;
            }
        }

        public void DetachSwapChain()
        {
            if (_waitableObject != IntPtr.Zero)
            {
                try { CloseHandle(_waitableObject); } catch { }
                _waitableObject = IntPtr.Zero;
            }
        }

        public void AttachSwapChain(SwapChain sc, int frameCap, bool tearingSupported)
        {
            DetachSwapChain();
            _frameCap = frameCap;
            _tearingSupported = tearingSupported;

            DetermineSwapchainParams(frameCap, tearingSupported, out var flags, out _isVsync);
            _useTearing = ! _isVsync && tearingSupported;

            if (_isVsync)
            {
                _presentMode = "VSync (Waitable Flip)";
                _effectiveTargetFps = 120;
                try
                {
                    using (var sc2 = sc.QueryInterfaceOrNull<SwapChain2>())
                    {
                        if (sc2 != null)
                        {
                            sc2.MaximumFrameLatency = 1;
                            _waitableObject = sc2.FrameLatencyWaitableObject;
                        }
                    }
                }
                catch { }
            }
            else if (frameCap == -1)
            {
                _presentMode = _useTearing ? "Uncapped (Tearing Flip)" : "Uncapped (Fast Flip)";
                _effectiveTargetFps = 0;
            }
            else
            {
                _presentMode = _useTearing ? $"{frameCap} fps (Low-Latency Flip)" : $"{frameCap} fps (Flip)";
                _effectiveTargetFps = frameCap;
            }

            _nextDeadlineTicks = Stopwatch.GetTimestamp();
        }

        public void WaitForFrame(VisualBeeSettings settings, bool isSilent)
        {
            long now = Stopwatch.GetTimestamp();

            // Idle throttle: drop to 20 fps if silent for > 1 second
            if (isSilent && settings != null && settings.IdleThrottle)
            {
                if (_idleSinceTicks == 0) _idleSinceTicks = now;
                else if (now - _idleSinceTicks > Stopwatch.Frequency) _isThrottled = true;
            }
            else
            {
                _idleSinceTicks = 0;
                _isThrottled = false;
            }

            if (_isThrottled)
            {
                long idleInterval = Stopwatch.Frequency / 20;
                if (now < _nextDeadlineTicks)
                    _timer.WaitUntil(_nextDeadlineTicks);
                _nextDeadlineTicks = Stopwatch.GetTimestamp() + idleInterval;
                return;
            }

            if (_isVsync)
            {
                if (_waitableObject != IntPtr.Zero)
                {
                    WaitForSingleObject(_waitableObject, 100);
                }
                return;
            }

            if (_frameCap == -1)
            {
                // Uncapped
                Thread.Yield();
                return;
            }

            // Fixed frame rate cap (60..360)
            int targetFps = _frameCap > 0 ? _frameCap : 120;
            long intervalTicks = Math.Max(1, Stopwatch.Frequency / targetFps);

            if (now < _nextDeadlineTicks)
            {
                _timer.WaitUntil(_nextDeadlineTicks);
            }

            now = Stopwatch.GetTimestamp();
            _nextDeadlineTicks += intervalTicks;
            if (now - _nextDeadlineTicks > intervalTicks * 2)
            {
                _nextDeadlineTicks = now + intervalTicks;
            }
        }

        public void Present(SwapChain sc)
        {
            if (sc == null || sc.IsDisposed) return;

            if (_isThrottled)
            {
                sc.Present(1, PresentFlags.None);
                return;
            }

            if (_isVsync)
            {
                sc.Present(1, PresentFlags.None);
            }
            else
            {
                PresentFlags flags = _useTearing ? PresentFlags.AllowTearing : PresentFlags.None;
                sc.Present(0, flags);
            }
        }

        public void Dispose()
        {
            DetachSwapChain();
            _timer.Dispose();
        }
    }
}
