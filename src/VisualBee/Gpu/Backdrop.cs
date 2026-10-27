using System;
using System.Runtime.InteropServices;

namespace MusicBeePlugin;

internal sealed class Backdrop : IDisposable
{
	private GpuTexture _art;

	private int _artId;

	private readonly BlurChain _blur = new BlurChain();

	private GpuTexture _blurred;

	private int _blurLevels = -1;

	private GpuTexture _aurora;

	public bool Animated(Look l)
	{
		if (l.BackgroundMode == 3)
		{
			return l.AuroraSpeed > 0.0;
		}
		if (l.CyberGrid)
		{
			return l.CyberGridSpeed > 0.0;
		}
		return false;
	}

	public void Draw(GpuDevice gpu, Canvas c, GpuTexture target, Look l, Vec4[] colours, float time, float energy, ArtImage art)
	{
		c.Begin(target, time);
		if (l.BackgroundMode == 3)
		{
			int aw = Math.Max(16, target.Width / 2);
			int ah = Math.Max(16, target.Height / 2);
			if (_aurora == null || _aurora.Width != aw || _aurora.Height != ah)
			{
				_aurora?.Dispose();
				_aurora = gpu.CreateTexture(aw, ah, 87, renderTarget: true, IntPtr.Zero, 0);
			}
			if (_aurora != null)
			{
				c.Begin(_aurora, time);
				PassData pass = new PassData
				{
					P0 = colours[7],
					P1 = colours[8],
					P2 = colours[9],
					P3 = new Vec4((float)(l.AuroraSpeed / 100.0), (float)(l.AuroraIntensity / 100.0), (float)(1.6 * l.AuroraScale / 100.0), Math.Max(0f, Math.Min(1f, energy))),
					P4 = colours[0]
				};
				c.Pass(c.AuroraPs, pass, BlendMode.Replace, null);
				c.Begin(target, time);
				c.Blend = BlendMode.Replace;
				c.Quad(_aurora, TexQuad.Make(0f, 0f, target.Width, target.Height, 0f, 0f, 1f, 1f, new Vec4(1f, 1f, 1f, 1f), mask: false));
				c.Flush();
				return;
			}
		}
		int num = ((l.BackgroundMode == 1) ? 1 : ((l.BackgroundMode == 2) ? 2 : 0));
		float num2 = (float)target.Width / (float)Math.Max(1, target.Height);
		PassData pass2 = new PassData
		{
			P0 = colours[0],
			P1 = colours[1],
			P2 = colours[2],
			P3 = new Vec4(num, (float)(l.BackgroundAngle * Math.PI / 180.0), 0.5f, 0.5f),
			P4 = new Vec4((float)Math.Sqrt(0.25 * (double)num2 * (double)num2 + 0.25), l.BackgroundUseMiddle ? ((float)(l.BackgroundMiddleAt / 100.0)) : (-1f), 0f, 0f),
			P5 = new Vec4(l.CyberGrid ? 1f : 0f, (float)(l.CyberGridSpeed / 100.0), (float)(l.CyberGridIntensity / 100.0), Math.Max(0f, Math.Min(1f, energy))),
			P6 = new Vec4(0.60f, 0f, 0f, 0f),
			P7 = colours[4]
		};
		c.Pass(c.BackgroundPs, pass2, BlendMode.Replace, null);
		if (l.BackgroundMode != 4 || art == null || l.ArtOpacity <= 0.0)
		{
			return;
		}
		GpuTexture gpuTexture = ArtTexture(gpu, c, art, l, time);
		if (gpuTexture != null)
		{
			float num3 = (float)art.Width / (float)art.Height;
			float u = 0f;
			float v = 0f;
			float u2 = 1f;
			float v2 = 1f;
			if (num2 > num3)
			{
				float num4 = num3 / num2;
				v = 0.5f - num4 / 2f;
				v2 = 0.5f + num4 / 2f;
			}
			else
			{
				float num5 = num2 / num3;
				u = 0.5f - num5 / 2f;
				u2 = 0.5f + num5 / 2f;
			}
			c.Begin(target, time);
			c.Blend = BlendMode.Over;
			c.Quad(gpuTexture, TexQuad.Make(0f, 0f, target.Width, target.Height, u, v, u2, v2, new Vec4(1f, 1f, 1f, 1f), mask: false, (float)(l.ArtOpacity / 100.0)));
			c.Flush();
		}
	}

	public GpuTexture Cover(GpuDevice gpu, ArtImage art)
	{
		if (art == null)
		{
			return null;
		}
		if (_art == null || _artId != art.Id)
		{
			if (_art != null)
			{
				_art.Dispose();
			}
			GCHandle gCHandle = GCHandle.Alloc(art.Pixels, GCHandleType.Pinned);
			try
			{
				_art = gpu.CreateTexture(art.Width, art.Height, 87, renderTarget: true, gCHandle.AddrOfPinnedObject(), art.Width * 4);
			}
			finally
			{
				gCHandle.Free();
			}
			_artId = art.Id;
			_blurLevels = -1;
		}
		return _art;
	}

	private GpuTexture ArtTexture(GpuDevice gpu, Canvas c, ArtImage art, Look l, float time)
	{
		if (Cover(gpu, art) == null)
		{
			return null;
		}
		int num = (int)Math.Round(l.ArtBlur / 100.0 * 6.0);
		if (num == 0)
		{
			return _art;
		}
		if (num != _blurLevels || _blurred == null)
		{
			_blurred = _blur.Run(gpu, c, _art, num, bloom: false, 0f, time);
			_blurLevels = num;
		}
		return _blurred ?? _art;
	}

	public void Dispose()
	{
		if (_art != null)
		{
			_art.Dispose();
		}
		_art = null;
		if (_aurora != null)
		{
			_aurora.Dispose();
		}
		_aurora = null;
		_blur.Dispose();
		_blurred = null;
		_blurLevels = -1;
	}
}
