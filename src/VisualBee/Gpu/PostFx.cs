using System;

namespace MusicBeePlugin;

internal sealed class PostFx : IDisposable
{
	public GpuTexture Background;

	public GpuTexture Content;

	private GpuTexture _trail;

	private GpuTexture _trailNext;

	private bool _trailFresh;

	private readonly BlurChain _bloom = new BlurChain();

	private readonly BlurChain _shadow = new BlurChain();

	private int _w;

	private int _h;

	public bool BackgroundValid;

	public GpuTexture LastBloom;

	public int Width => _w;

	public int Height => _h;

	public bool Ensure(GpuDevice gpu, int width, int height)
	{
		if (Background != null && _w == width && _h == height)
		{
			return true;
		}
		Free();
		_w = width;
		_h = height;
		Background = gpu.CreateTexture(width, height, 87, renderTarget: true, IntPtr.Zero, 0);
		Content = gpu.CreateTexture(width, height, 10, renderTarget: true, IntPtr.Zero, 0);
		BackgroundValid = false;
		if (Background != null)
		{
			return Content != null;
		}
		return false;
	}

	public void ResetTrails()
	{
		_trailFresh = true;
	}

	public void Present(GpuDevice gpu, Canvas canvas, Effects fx, float dt, float time, IntPtr output, int width, int height)
	{
		GpuTexture gpuTexture = Content;
		if (fx.Trails && fx.TrailSeconds > 0f)
		{
			if (_trail == null)
			{
				_trail = gpu.CreateTexture(_w, _h, 87, renderTarget: true, IntPtr.Zero, 0);
				_trailNext = gpu.CreateTexture(_w, _h, 87, renderTarget: true, IntPtr.Zero, 0);
				_trailFresh = true;
			}
			if (_trail != null && _trailNext != null)
			{
				if (_trailFresh)
				{
					Canvas.Clear(gpu, _trail, Vec4.Zero);
					_trailFresh = false;
				}
				float x = (float)Math.Exp((double)(0f - Math.Max(dt, 0f)) * 2.302585 / (double)fx.TrailSeconds);
				PassData pass = new PassData
				{
					P0 = new Vec4(x, 0f, 0f, 0f)
				};
				canvas.Begin(_trailNext, time);
				canvas.Pass(canvas.TrailPs, pass, BlendMode.Replace, _trail, Content);
				GpuTexture trail = _trail;
				_trail = _trailNext;
				_trailNext = trail;
				gpuTexture = _trail;
			}
		}
		else
		{
			FreeTrails();
		}
		GpuTexture gpuTexture2 = null;
		int levels = BloomLevels(fx.BloomRadius);
		if (fx.Bloom && fx.BloomStrength > 0f)
		{
			gpuTexture2 = _bloom.Run(gpu, canvas, gpuTexture, levels, bloom: true, fx.BloomThreshold, time);
		}
		LastBloom = gpuTexture2;
		GpuTexture gpuTexture3 = null;
		if (fx.Shadow && fx.ShadowOpacity > 0f)
		{
			gpuTexture3 = _shadow.Run(gpu, canvas, gpuTexture, ShadowLevels(fx.ShadowBlur), bloom: false, 0f, time);
		}
		int num = (int)(((gpuTexture2 != null) ? 1u : 0u) | (uint)((gpuTexture3 != null) ? 2 : 0)) | (fx.Dither ? 4 : 0);
		PassData pass2 = new PassData
		{
			P0 = new Vec4((gpuTexture2 != null) ? fx.BloomStrength : 0f, fx.ShadowOpacity, fx.ShadowX, fx.ShadowY),
			P1 = new Vec4(fx.BloomTint.X, fx.BloomTint.Y, fx.BloomTint.Z, fx.Vignette),
			P2 = new Vec4(fx.ShadowColour.X, fx.ShadowColour.Y, fx.ShadowColour.Z, fx.Scanlines),
			P3 = new Vec4(fx.Aberration, fx.Grain * 0.08f, fx.VignetteStart, num),
			P4 = fx.Flash
		};
		canvas.Begin(output, width, height, time);
		canvas.Pass(canvas.CompositePs, pass2, BlendMode.Replace, Background, gpuTexture, gpuTexture2, gpuTexture3);
	}

	private static int BloomLevels(float radius)
	{
		return 1 + (int)Math.Round(Clamp01(radius) * 4f);
	}

	private static int ShadowLevels(float blur)
	{
		int i;
		for (i = 1; i < 7 && (float)(1 << i + 1) <= Math.Max(2f, blur); i++)
		{
		}
		return Math.Min(3, i);
	}

	private static float Clamp01(float v)
	{
		if (!(v < 0f))
		{
			if (!(v > 1f))
			{
				return v;
			}
			return 1f;
		}
		return 0f;
	}

	private void FreeTrails()
	{
		if (_trail != null)
		{
			_trail.Dispose();
		}
		if (_trailNext != null)
		{
			_trailNext.Dispose();
		}
		_trail = null;
		_trailNext = null;
	}

	private void Free()
	{
		if (Background != null)
		{
			Background.Dispose();
		}
		if (Content != null)
		{
			Content.Dispose();
		}
		Background = null;
		Content = null;
		FreeTrails();
		_bloom.Dispose();
		_shadow.Dispose();
	}

	public void Dispose()
	{
		Free();
	}
}
