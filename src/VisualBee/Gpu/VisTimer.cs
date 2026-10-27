using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace MusicBeePlugin;

internal sealed class VisTimer
{
	private sealed class TimerHandle : WaitHandle
	{
		public TimerHandle(IntPtr handle)
		{
			SafeWaitHandle = new SafeWaitHandle(handle, ownsHandle: true);
		}
	}

	private const uint HighResolution = 2u;

	private const uint TimerAllAccess = 2031619u;

	private const int VRefresh = 116;

	private const double MinimumTickMs = 2.0;

	private readonly int _intervalMs;

	private readonly string _name;

	private readonly Func<bool> _tick;

	private readonly Action _exit;

	private readonly ManualResetEvent _stop = new ManualResetEvent(initialState: false);

	private Thread _thread;

	public VisTimer(int intervalMs, string name, Func<bool> tick, Action exit = null)
	{
		_intervalMs = intervalMs;
		_name = name;
		_tick = tick;
		_exit = exit;
	}

	public void Start()
	{
		if (_thread == null)
		{
			_stop.Reset();
			_thread = new Thread(Run)
			{
				IsBackground = true,
				Priority = ThreadPriority.Normal,
				Name = _name
			};
			_thread.Start();
		}
	}

	public void Stop()
	{
		Thread thread = _thread;
		if (thread != null)
		{
			_thread = null;
			_stop.Set();
			if (thread != Thread.CurrentThread)
			{
				thread.Join(1000);
			}
		}
	}

	private void Run()
	{
		IntPtr intPtr = CreateWaitableTimerExW(IntPtr.Zero, IntPtr.Zero, 2u, 2031619u);
		bool flag = intPtr == IntPtr.Zero;
		if (flag)
		{
			Gdi.timeBeginPeriod(1u);
			intPtr = CreateWaitableTimerExW(IntPtr.Zero, IntPtr.Zero, 0u, 2031619u);
		}
		try
		{
			if (intPtr == IntPtr.Zero)
			{
				return;
			}
			using TimerHandle timerHandle = new TimerHandle(intPtr);
			WaitHandle[] waitHandles = new WaitHandle[2] { _stop, timerHandle };
			double refreshMs = RefreshMs();
			long frameTicks = Math.Max(1L, (long)(refreshMs * (double)Stopwatch.Frequency / 1000.0));
			long nextFrame = Stopwatch.GetTimestamp();
			while (!_stop.WaitOne(0))
			{
				try
				{
					_tick();
				}
				catch (Exception ex)
				{
					Console.WriteLine("VisTimer Exception: " + ex);
				}
				nextFrame += frameTicks;
				long now = Stopwatch.GetTimestamp();
				long remainingTicks = nextFrame - now;
				if (remainingTicks > 0)
				{
					long dueTime = -(long)((double)remainingTicks * 10000000.0 / (double)Stopwatch.Frequency);
					if (!SetWaitableTimer(intPtr, ref dueTime, 0, IntPtr.Zero, IntPtr.Zero, resume: false) || WaitHandle.WaitAny(waitHandles) == 0)
					{
						break;
					}
				}
				else
				{
					if (-remainingTicks > frameTicks)
					{
						nextFrame = now;
					}
					Thread.Yield();
				}
			}
		}
		finally
		{
			if (flag)
			{
				Gdi.timeEndPeriod(1u);
			}
			if (_exit != null)
			{
				try
				{
					_exit();
				}
				catch (Exception ex)
				{
					Console.WriteLine("VisTimer Exception: " + ex);
				}
			}
		}
	}

	internal double RefreshMs()
	{
		IntPtr dC = Gdi.GetDC(IntPtr.Zero);
		int deviceCaps = Gdi.GetDeviceCaps(dC, 116);
		Gdi.ReleaseDC(IntPtr.Zero, dC);
		if (deviceCaps <= 1)
		{
			return _intervalMs;
		}
		return 1000.0 / (double)deviceCaps;
	}

	[DllImport("kernel32.dll")]
	private static extern IntPtr CreateWaitableTimerExW(IntPtr attributes, IntPtr name, uint flags, uint access);




	[DllImport("kernel32.dll")]
	private static extern bool SetWaitableTimer(IntPtr timer, ref long dueTime, int periodMs, IntPtr completion, IntPtr arg, bool resume);
}
