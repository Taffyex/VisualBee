using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal abstract partial class GpuView : Control
{
	private const int WM_CONTEXTMENU = 123;

	protected readonly VisualiserHost Host;

	protected readonly bool FullScreen;

	private readonly VisTimer _timer;

	private readonly ContextMenu _menu = new ContextMenu();

	private readonly object _sync = new object();

	private IntPtr _hwnd;

	private bool _closed;

	private volatile bool _dirty = true;

	private volatile bool _resetTrails;

	private volatile bool _hasGpu;

	private GpuDevice _gpu;

	private IntPtr _gpuHwnd;

	private Canvas _canvas;

	private PostFx _post;

	private Backdrop _backdrop;

	private TextSheet _overlay;

	private IntPtr _overlayFont;

	private string _overlayText;

	private readonly ColourAnimator _animator = new ColourAnimator();

	private readonly Stopwatch _clock = Stopwatch.StartNew();

	private double _lastTick;

	private double _lastFrame;

	private double _retryAt;

	private int _failures;

	private double _hiddenSince = -1.0;

	private double _settleUntil;

	private Look _drawnLook;

	private int _drawnW;

	private int _drawnH;

	private Look _artLook;

	private Look _artLookBase;

	private int _artLookId;

	private int _fpsFrames;

	private double _fpsStart;

	private double _fpsCpu;

	private double _lastPlayStateCheck;

	private PlayState _cachedPlayState = PlayState.Playing;

	private string _fpsText = "";

	private volatile string _gpuStatus = "starting";

	private double _hudUntil;

	private string _hud = "";

	private volatile Action<Bitmap> _capture;

	private volatile Action<Bitmap[]> _captureLayers;

	internal volatile Action<double> Presented;

	public string GpuStatus => _gpuStatus;

	protected virtual float Energy => 0f;

	protected virtual float Beat => 0f;

	protected GpuView(VisualiserHost host, bool fullScreen, string threadName)
	{
		Host = host;
		FullScreen = fullScreen;
		SetStyle(ControlStyles.UserPaint | ControlStyles.Opaque | ControlStyles.Selectable | ControlStyles.AllPaintingInWmPaint, value: true);
		SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer, value: false);
		TabStop = false;
		_timer = new VisTimer(16, threadName, OnTick, ThreadEnding);
	}

	public void Start()
	{
		_timer.Start();
	}

	public void Redraw()
	{
		_dirty = true;
	}

	protected void ResetTrails()
	{
		_resetTrails = true;
	}

	protected override void Dispose(bool disposing)
	{
		_timer.Stop();
		lock (_sync)
		{
			_closed = true;
		}
		if (disposing)
		{
			_menu.Dispose();
		}
		base.Dispose(disposing);
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		lock (_sync)
		{
			_hwnd = Handle;
		}
	}

	protected override void OnHandleDestroyed(EventArgs e)
	{
		lock (_sync)
		{
			_hwnd = IntPtr.Zero;
		}
		base.OnHandleDestroyed(e);
	}

	protected abstract bool Advance(ref Frame f, PlayState state);

	private bool OnTick()
	{
		lock (_sync)
		{
			if (_closed)
			{
				return false;
			}
			double totalSeconds = _clock.Elapsed.TotalSeconds;
			IntPtr hwnd = _hwnd;
			if (_gpu != null && _gpuHwnd != hwnd)
			{
				ReleaseGpu();
			}
			if (hwnd == IntPtr.Zero || !Gdi.IsWindowVisible(hwnd))
			{
				if (_hiddenSince < 0.0)
				{
					_hiddenSince = totalSeconds;
				}
				else if (totalSeconds - _hiddenSince > 30.0 && _gpu != null)
				{
					ReleaseGpu();
				}
				return false;
			}
			_hiddenSince = -1.0;
			if (!Gdi.GetClientRect(hwnd, out var rect) || rect.right <= 0 || rect.bottom <= 0)
			{
				return false;
			}
			Look current = Host.Store.Current;
			int num = CommonLook.FrameCapRates[Math.Max(0, Math.Min(CommonLook.FrameCapRates.Length - 1, current.FrameCap))];
			if (num > 0 && totalSeconds - _lastFrame < 1.0 / (double)num - 0.0015)
			{
				return false;
			}
			double rawDt = Math.Min(0.1, Math.Max(0.0005, totalSeconds - _lastTick));
			double expDt = _timer.RefreshMs() * 0.001;
			float dt = (expDt > 0.002 && Math.Abs(rawDt - expDt) < expDt * 0.35) ? (float)expDt : (float)rawDt;
			_lastTick = totalSeconds;
			if (!EnsureGpu(hwnd, rect.right, rect.bottom, totalSeconds))
			{
				return false;
			}
			long timestamp = Stopwatch.GetTimestamp();
			Frame f = new Frame
			{
				Gpu = _gpu,
				Canvas = _canvas,
				Look = current,
				Width = rect.right,
				Height = rect.bottom,
				Dt = dt,
				Time = (float)totalSeconds,
				Scale = PanelLayout.Scale,
				LookChanged = (current != _drawnLook),
				SizeChanged = (rect.right != _drawnW || rect.bottom != _drawnH)
			};
			_animator.Update(current, ArtLook(current), dt, totalSeconds);
			f.Colours = _animator.Shown;
			if (totalSeconds - _lastPlayStateCheck > 0.500)
			{
				_cachedPlayState = Host.CachedPlayState;
				_lastPlayStateCheck = totalSeconds;
			}
			if (Advance(ref f, _cachedPlayState) || _animator.Moving || _backdrop.Animated(current))
			{
				_settleUntil = totalSeconds + (current.Trails ? (current.TrailMs / 1000.0 * 1.5) : 0.0) + 0.05;
			}
			bool flag = current.ShowFps || totalSeconds < _hudUntil + 0.1;
			if (!_dirty && !f.LookChanged && !f.SizeChanged && totalSeconds > _settleUntil && !flag)
			{
				return false;
			}
			_dirty = false;
			_drawnLook = current;
			_drawnW = f.Width;
			_drawnH = f.Height;
			Render(ref f, totalSeconds);
			Action<Bitmap> capture = _capture;
			if (capture != null)
			{
				_capture = null;
				capture(_gpu.ReadBack(_gpu.BackBuffer, _gpu.Width, _gpu.Height));
			}
			Action<Bitmap[]> captureLayers = _captureLayers;
			if (captureLayers != null)
			{
				_captureLayers = null;
				captureLayers(new Bitmap[3]
				{
					Layer(_post.Background, f.Width, f.Height),
					Layer(_post.Content, f.Width, f.Height),
					Layer(_post.LastBloom, f.Width, f.Height)
				});
			}
			int sync = 1;
			if (num > 0)
			{
				double refMs = _timer.RefreshMs();
				int devHz = (refMs > 0.0) ? (int)Math.Round(1000.0 / refMs) : 60;
				if (devHz > 0)
				{
					int div = (int)Math.Round((double)devHz / num);
					if (div >= 1 && div <= 4 && Math.Abs((double)devHz / div - num) < 2.0)
					{
						sync = div;
					}
				}
			}
			int hr = _gpu.Present(sync);
			if (GpuDevice.Lost(hr))
			{
				_gpuStatus = "the graphics device was reset (0x" + hr.ToString("X8") + "); starting again";
				ReleaseGpu();
				_dirty = true;
				return false;
			}
			_lastFrame = _clock.Elapsed.TotalSeconds;
			double num2 = (double)(Stopwatch.GetTimestamp() - timestamp) * 1000.0 / (double)Stopwatch.Frequency;
			CountFrame(totalSeconds, num2);
			Presented?.Invoke(num2);
			return true;
		}
	}

	private Look ArtLook(Look look)
	{
		ArtImage art = Host.Art;
		if (!look.ArtColours || art == null || art.Theme == null)
		{
			return null;
		}
		if (_artLook == null || _artLookId != art.Id || _artLookBase != look)
		{
			_artLook = Host.Store.Themed(look, art.Theme);
			_artLookId = art.Id;
			_artLookBase = look;
		}
		return _artLook;
	}

	private void Render(ref Frame f, double now)
	{
		Canvas canvas = _canvas;
		if (_post.Ensure(_gpu, f.Width, f.Height))
		{
			if (_resetTrails)
			{
				_resetTrails = false;
				_post.ResetTrails();
			}
			Look look = f.Look;
			if (!_post.BackgroundValid || f.LookChanged || f.SizeChanged || _animator.Moving || _backdrop.Animated(look) || BackgroundNeedsRedraw(ref f))
			{
				_backdrop.Draw(_gpu, canvas, _post.Background, look, f.Colours, f.Time, Energy, Host.Art);
				canvas.Begin(_post.Background, f.Time);
				canvas.Blend = BlendMode.Over;
				DrawBackground(ref f);
				canvas.Flush();
				_post.BackgroundValid = true;
			}
			canvas.SceneBackdrop = _post.Background;
			f.Background = _post.Background;
			canvas.Begin(_post.Content, f.Time);
			canvas.Clear(Vec4.Zero);
			canvas.Blend = BlendMode.Over;
			f.Content = _post.Content;
			DrawContent(ref f);
			canvas.Flush();
			_post.Present(_gpu, canvas, CommonLook.Effects(look, f.Colours, Beat), f.Dt, f.Time, _gpu.BackBufferView, f.Width, f.Height);
			DrawOverlays(ref f, now);
			canvas.Flush();
		}
	}

	private bool EnsureGpu(IntPtr hwnd, int width, int height, double now)
	{
		if (_gpu != null)
		{
			if (_gpu.Width != width || _gpu.Height != height)
			{
				_canvas.ReleaseTarget();
				if (!_gpu.Resize(width, height))
				{
					ReleaseGpu();
					return false;
				}
			}
			return true;
		}
		if (now < _retryAt)
		{
			return false;
		}
		_gpu = GpuDevice.ForWindow(hwnd, width, height, out var error);
		if (_gpu != null)
		{
			_canvas = Canvas.Create(_gpu, out error);
		}
		if (_gpu == null || _canvas == null)
		{
			ReleaseGpu();
			_failures++;
			_retryAt = now + Math.Min(30.0, 0.5 * Math.Pow(2.0, _failures));
			_gpuStatus = error ?? "Direct3D 11 is not available";
			return false;
		}
		_gpuHwnd = hwnd;
		_post = new PostFx();
		_backdrop = new Backdrop();
		_failures = 0;
		_dirty = true;
		_gpuStatus = (_gpu.Warp ? "drawn by Windows' software renderer (no usable graphics card)" : "drawn on the graphics card");
		_hasGpu = true;
		return true;
	}

	protected virtual bool BackgroundNeedsRedraw(ref Frame f) => false;
}
