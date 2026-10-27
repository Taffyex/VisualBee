using System;

namespace MusicBeePlugin;

internal sealed class DsdConverter
{
	private sealed class Channel
	{
		public byte[] Bytes;

		public int BytePos;

		public int ByteCount;

		public float[] H2;

		public int Pos2;

		public int Count2;

		public float[] H3;

		public int Pos3;

		public int Count3;
	}

	public const int OutRate = 44100;

	private const int MidRate = 352800;

	private const double Gain = 2.0;

	private const double Beta = 12.0;

	public readonly int Channels;

	public readonly int Factor;

	public readonly int Delay;

	private readonly int _bytesPer1;

	private readonly int _tables;

	private readonly float[] _lut;

	private readonly float[] _taps2;

	private readonly float[] _taps3;

	private readonly Channel[] _state;

	public static int FactorFor(int dopRate)
	{
		switch (dopRate)
		{
		case 176400:
		case 352800:
		case 705600:
		case 1411200:
			return dopRate / 44100;
		default:
			return 0;
		}
	}

	public static bool IsDop(float[] data, int frames, int channels)
	{
		int num = Math.Min(frames, 2048);
		if (num < 64)
		{
			return false;
		}
		int num2 = 0;
		int num3 = -1;
		for (int i = 0; i < num; i++)
		{
			int num4 = ((int)Math.Round((double)data[i * channels] * 8388608.0) >> 16) & 0xFF;
			if ((num4 == 5 || num4 == 250) && num4 != num3)
			{
				num2++;
			}
			num3 = num4;
		}
		return (double)num2 >= (double)num * 0.98;
	}

	public DsdConverter(int dopRate, int channels)
	{
		Factor = FactorFor(dopRate);
		if (Factor == 0 || channels <= 0)
		{
			throw new ArgumentException("Not a DoP rate: " + dopRate + " Hz.");
		}
		Channels = channels;
		int num = dopRate * 16;
		int num2 = num / 352800;
		_bytesPer1 = num2 / 8;
		int num3 = 9 * num2;
		double[] array = Design(num3, num, 20000.0, 328800.0);
		_tables = num3 / 8;
		_lut = new float[_tables * 256];
		for (int i = 0; i < _tables; i++)
		{
			for (int j = 0; j < 256; j++)
			{
				double num4 = 0.0;
				for (int k = 0; k < 8; k++)
				{
					num4 += array[i * 8 + k] * ((((j >> 7 - k) & 1) != 0) ? 1.0 : (-1.0));
				}
				_lut[i * 256 + j] = (float)(num4 * 2.0);
			}
		}
		_taps2 = ToFloat(Design(61, 352800.0, 20000.0, 66150.0));
		_taps3 = ToFloat(Design(337, 88200.0, 20000.0, 22050.0));
		double num5 = (double)(num3 - 1) / 2.0 / (double)num + (double)(_taps2.Length - 1) / 2.0 / 352800.0 + (double)(_taps3.Length - 1) / 2.0 / 88200.0;
		Delay = (int)Math.Round(num5 * 44100.0);
		_state = new Channel[channels];
		for (int l = 0; l < channels; l++)
		{
			_state[l] = new Channel
			{
				Bytes = new byte[_tables * 2],
				H2 = new float[_taps2.Length * 2],
				H3 = new float[_taps3.Length * 2]
			};
		}
		Reset();
	}

	public void Reset()
	{
		Channel[] state = _state;
		foreach (Channel channel in state)
		{
			for (int j = 0; j < channel.Bytes.Length; j++)
			{
				channel.Bytes[j] = 105;
			}
			Array.Clear(channel.H2, 0, channel.H2.Length);
			Array.Clear(channel.H3, 0, channel.H3.Length);
			channel.BytePos = (channel.Pos2 = (channel.Pos3 = 0));
			channel.ByteCount = (channel.Count2 = (channel.Count3 = 0));
		}
	}

	public int MaxOutput(int frames)
	{
		return frames / Factor + 1;
	}

	public unsafe int Process(float[] src, int srcFrame, int frames, float[] dst, int dstFrame)
	{
		if (frames <= 0)
		{
			return 0;
		}
		int channels = Channels;
		int result = 0;
		fixed (float* ptr = &src[0])
		{
			fixed (float* ptr2 = &dst[0])
			{
				for (int i = 0; i < channels; i++)
				{
					Channel s = _state[i];
					float* ptr3 = ptr2 + dstFrame * channels + i;
					float* ptr4 = ptr + srcFrame * channels + i;
					int num = 0;
					int num2 = 0;
					while (num2 < frames)
					{
						int num3 = (int)(*ptr4 * 8388608f);
						if (Push(s, (byte)(num3 >> 8), ptr3 + num * channels))
						{
							num++;
						}
						if (Push(s, (byte)num3, ptr3 + num * channels))
						{
							num++;
						}
						num2++;
						ptr4 += channels;
					}
					result = num;
				}
			}
		}
		return result;
	}

	public unsafe int Flush(float[] dst, int dstFrame)
	{
		int channels = Channels;
		int result = 0;
		fixed (float* ptr = &dst[0])
		{
			for (int i = 0; i < channels; i++)
			{
				Channel s = _state[i];
				float* ptr2 = ptr + dstFrame * channels + i;
				int num = 0;
				while (num < Delay)
				{
					if (Push(s, 105, ptr2 + num * channels))
					{
						num++;
					}
				}
				result = num;
			}
		}
		return result;
	}

	private unsafe bool Push(Channel s, byte b, float* to)
	{
		int tables = _tables;
		int bytePos = s.BytePos;
		s.Bytes[bytePos] = b;
		s.Bytes[bytePos + tables] = b;
		if (++bytePos == tables)
		{
			bytePos = 0;
		}
		s.BytePos = bytePos;
		if (++s.ByteCount < _bytesPer1)
		{
			return false;
		}
		s.ByteCount = 0;
		float num = 0f;
		fixed (byte* ptr = &s.Bytes[bytePos])
		{
			fixed (float* ptr2 = &_lut[0])
			{
				float* ptr3 = ptr2;
				int num2 = 0;
				while (num2 < tables)
				{
					num += ptr3[(int)ptr[num2]];
					num2++;
					ptr3 += 256;
				}
			}
		}
		if (!Stage(s.H2, ref s.Pos2, ref s.Count2, 4, num, _taps2, out var y))
		{
			return false;
		}
		if (!Stage(s.H3, ref s.Pos3, ref s.Count3, 2, y, _taps3, out var y2))
		{
			return false;
		}
		*to = y2;
		return true;
	}

	private unsafe static bool Stage(float[] h, ref int pos, ref int count, int factor, float x, float[] taps, out float y)
	{
		int num = taps.Length;
		h[pos] = x;
		h[pos + num] = x;
		if (++pos == num)
		{
			pos = 0;
		}
		y = 0f;
		if (++count < factor)
		{
			return false;
		}
		count = 0;
		int num2 = num / 2;
		fixed (float* ptr = &h[pos])
		{
			fixed (float* ptr2 = &taps[0])
			{
				float* ptr3 = ptr;
				float* ptr4 = ptr + num - 1;
				float num3 = ptr2[num2] * ptr[num2];
				float num4 = 0f;
				int num5 = 0;
				while (num5 + 1 < num2)
				{
					num3 += ptr2[num5] * (*ptr3 + *ptr4);
					num4 += ptr2[num5 + 1] * (ptr3[1] + ptr4[-1]);
					num5 += 2;
					ptr3 += 2;
					ptr4 -= 2;
				}
				while (num5 < num2)
				{
					num3 += ptr2[num5] * (*ptr3 + *ptr4);
					num5++;
					ptr3++;
					ptr4--;
				}
				y = num3 + num4;
			}
		}
		return true;
	}

	private static double[] Design(int n, double rate, double pass, double stop)
	{
		double num = (pass + stop) * 0.5 / rate;
		double[] array = new double[n];
		double num2 = (double)(n - 1) * 0.5;
		double num3 = BesselI0(12.0);
		double num4 = 0.0;
		for (int i = 0; i < n; i++)
		{
			double num5 = (double)i - num2;
			double num6 = ((num5 == 0.0) ? (2.0 * num) : (Math.Sin(Math.PI * 2.0 * num * num5) / (Math.PI * num5)));
			double num7 = ((num2 > 0.0) ? (num5 / num2) : 0.0);
			double num8 = BesselI0(12.0 * Math.Sqrt(Math.Max(0.0, 1.0 - num7 * num7))) / num3;
			array[i] = num6 * num8;
			num4 += array[i];
		}
		for (int j = 0; j < n; j++)
		{
			array[j] /= num4;
		}
		return array;
	}

	private static double BesselI0(double x)
	{
		double num = 1.0;
		double num2 = 1.0;
		double num3 = x * x / 4.0;
		for (int i = 1; i < 64; i++)
		{
			num2 *= num3 / ((double)i * (double)i);
			num += num2;
			if (num2 < num * 1E-17)
			{
				break;
			}
		}
		return num;
	}

	private static float[] ToFloat(double[] h)
	{
		float[] array = new float[h.Length];
		for (int i = 0; i < h.Length; i++)
		{
			array[i] = (float)h[i];
		}
		return array;
	}
}
