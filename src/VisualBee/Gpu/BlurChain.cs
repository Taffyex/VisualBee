using System;

namespace MusicBeePlugin;

internal sealed class BlurChain : IDisposable
{
	public const int MaxLevels = 5;

	private readonly GpuTexture[] _levels = new GpuTexture[5];

	public GpuTexture Run(GpuDevice gpu, Canvas canvas, GpuTexture source, int levels, bool bloom, float threshold, float time, int format = 87)
	{
		int num = source.Width;
		int num2 = source.Height;
		int num3 = 0;
		levels = Math.Max(1, Math.Min(5, levels));
		for (int i = 0; i < levels; i++)
		{
			num = Math.Max(1, (num + 1) / 2);
			num2 = Math.Max(1, (num2 + 1) / 2);
			if (_levels[i] != null && (_levels[i].Width != num || _levels[i].Height != num2 || _levels[i].Format != format))
			{
				_levels[i].Dispose();
				_levels[i] = null;
			}
			if (_levels[i] == null)
			{
				_levels[i] = gpu.CreateTexture(num, num2, format, renderTarget: true, IntPtr.Zero, 0);
			}
			if (_levels[i] == null)
			{
				break;
			}
			num3++;
			if (num <= 16 || num2 <= 16)
			{
				break;
			}
		}
		if (num3 == 0)
		{
			return null;
		}
		for (int j = 0; j < num3; j++)
		{
			GpuTexture gpuTexture = ((j == 0) ? source : _levels[j - 1]);
			PassData pass = new PassData
			{
				P0 = new Vec4(1f / (float)gpuTexture.Width, 1f / (float)gpuTexture.Height, threshold, Math.Max(0.05f, threshold * 0.5f)),
				P1 = new Vec4((bloom && j == 0 && threshold > 0f) ? 1f : 0f, 0f, 0f, 0f)
			};
			canvas.Begin(_levels[j], time);
			canvas.Pass(canvas.DownPs, pass, BlendMode.Replace, gpuTexture);
		}
		for (int num4 = num3 - 2; num4 >= 0; num4--)
		{
			GpuTexture gpuTexture2 = _levels[num4 + 1];
			PassData pass2 = new PassData
			{
				P0 = new Vec4(1f / (float)gpuTexture2.Width, 1f / (float)gpuTexture2.Height, 1f, 1f)
			};
			canvas.Begin(_levels[num4], time);
			canvas.Pass(canvas.UpPs, pass2, bloom ? BlendMode.Mix : BlendMode.Replace, gpuTexture2, null, null, null, 0.6f);
		}
		return _levels[0];
	}

	public void Dispose()
	{
		for (int i = 0; i < 5; i++)
		{
			if (_levels[i] != null)
			{
				_levels[i].Dispose();
			}
			_levels[i] = null;
		}
	}
}
