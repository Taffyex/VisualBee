using System;
using System.Collections.Generic;

namespace MusicBeePlugin;

internal sealed partial class SpectrumAnalyzer
{
	public const int ScaleLog = 0;

	public const int ScaleMel = 1;

	public const int ScaleBark = 2;

	public const int ScaleLinear = 3;

	public static readonly string[] ScaleNames = new string[4] { "Logarithmic", "Mel", "Bark", "Linear" };

	public static readonly string[] ScaleTokens = new string[4] { "log", "mel", "bark", "linear" };

	public static readonly int[] FftSizes = new int[5] { 2048, 4096, 8192, 16384, 32768 };

	public static readonly string[] FftSizeNames = new string[5] { "2048 (46 ms)", "4096 (93 ms)", "8192 (186 ms)", "16384 (372 ms)", "32768 (743 ms)" };

	public static readonly string[] FftSizeTokens = new string[5] { "2048", "4096", "8192", "16384", "32768" };

	public const int WindowHann = 0;

	public const int WindowBlackmanHarris = 1;

	public const int WindowGaussian = 2;

	public static readonly string[] WindowNames = new string[3] { "Hann", "Blackman-Harris", "Gaussian (foobar2000)" };

	public static readonly string[] WindowTokens = new string[3] { "hann", "blackman-harris", "gaussian" };

	public const int ValuePeak = 0;

	public const int ValueAverage = 1;

	public static readonly string[] ValueNames = new string[2] { "Peak", "Average" };

	public static readonly string[] ValueTokens = new string[2] { "peak", "average" };

	public const int ChannelsMix = 0;

	public const int ChannelsLeft = 1;

	public const int ChannelsRight = 2;

	public const int ChannelsMid = 3;

	public const int ChannelsSide = 4;

	public static readonly string[] ChannelNames = new string[5] { "All mixed", "Left", "Right", "Mid", "Side" };

	public static readonly string[] ChannelTokens = new string[5] { "mix", "left", "right", "mid", "side" };

	private const double FloorValue = 5.960464477539063E-08;

	private int _n;

	private int _window = -1;

	private float[] _w;

	private float _norm;

	private float[] _re;

	private float[] _im;

	private float[] _cos;

	private float[] _sin;

	private float[] _mag;

	private int[] _swapA;

	private int[] _swapB;

	private int _tN;

	private int _tRate;

	private int _tBands;

	private int _tScale;

	private double _tMin;

	private double _tMax;

	private double[] _lo;

	private double[] _hi;

	private double[] _centre;

	public double[] Centres => _centre;

	public static int WindowSize(int choice, int rate)
	{
		double num = (double)FftSizes[Math.Max(0, Math.Min(FftSizes.Length - 1, choice))] * (double)rate / 44100.0;
		int num2 = 1024;
		while (num2 < 131072 && (double)num2 * 1.5 < num)
		{
			num2 *= 2;
		}
		return num2;
	}

	public void AnalyzeStereo(float[] left, float[] right, int n, int rate, int window, int value, int bands, double minHz, double maxHz, int scale, double[] dbLeft, double[] dbRight, SpectrumAnalyzer aux)
	{
		Prepare(n, window);
		for (int i = 0; i < n; i++)
		{
			float w = _w[i];
			_re[i] = left[i] * w;
			_im[i] = right[i] * w;
		}
		Fft();
		int num = n >> 1;
		if (aux._mag == null || aux._mag.Length != num) aux._mag = new float[num];
		float norm = _norm;
		int maxBin = Math.Min(num, (int)Math.Ceiling(maxHz * n / rate) + 4);
		for (int j = 0; j < maxBin; j++)
		{
			int k = (j == 0) ? 0 : (n - j);
			float rL = 0.5f * (_re[j] + _re[k]);
			float iL = 0.5f * (_im[j] - _im[k]);
			_mag[j] = (float)Math.Sqrt(rL * rL + iL * iL) * norm;
			float rR = 0.5f * (_im[j] + _im[k]);
			float iR = 0.5f * (_re[k] - _re[j]);
			aux._mag[j] = (float)Math.Sqrt(rR * rR + iR * iR) * norm;
		}
		if (maxBin < num)
		{
			Array.Clear(_mag, maxBin, num - maxBin);
			Array.Clear(aux._mag, maxBin, num - maxBin);
		}
		Bands(_mag, num, n, rate, value, bands, minHz, maxHz, scale, dbLeft);
		aux.Bands(aux._mag, num, n, rate, value, bands, minHz, maxHz, scale, dbRight);
	}


	public void Analyze(float[] samples, int n, int rate, int window, int value, int bands, double minHz, double maxHz, int scale, double[] dbOut)
	{
		Prepare(n, window);
		Array.Clear(_im, 0, n);
		for (int i = 0; i < n; i++)
		{
			_re[i] = samples[i] * _w[i];
		}
		Fft();
		int num = n >> 1;
		int maxBin = Math.Min(num, (int)Math.Ceiling(maxHz * n / rate) + 4);
		for (int j = 0; j < maxBin; j++)
		{
			_mag[j] = (float)Math.Sqrt(_re[j] * _re[j] + _im[j] * _im[j]) * _norm;
		}
		if (maxBin < num)
		{
			Array.Clear(_mag, maxBin, num - maxBin);
		}
		Bands(_mag, num, n, rate, value, bands, minHz, maxHz, scale, dbOut);
	}

	public void FromMagnitudes(float[] mags, int count, int rate, int value, int bands, double minHz, double maxHz, int scale, double[] dbOut)
	{
		Bands(mags, count, count * 2, rate, value, bands, minHz, maxHz, scale, dbOut);
	}

	private void Bands(float[] mag, int count, int n, int rate, int value, int bands, double minHz, double maxHz, int scale, double[] dbOut)
	{
		Table(n, rate, bands, minHz, maxHz, scale);
		for (int i = 0; i < bands; i++)
		{
			double num = _lo[i];
			double num2 = _hi[i];
			double num3 = _centre[i] * (double)n / (double)rate;
			double num4;
			if (num3 >= (double)(count - 1))
			{
				num4 = 0.0;
			}
			else if (num2 - num < 1.5)
			{
				num4 = Interpolate(mag, count, num3);
			}
			else
			{
				int num5 = Math.Max(0, (int)Math.Ceiling(num));
				int num6 = Math.Min(count - 1, (int)Math.Floor(num2));
				if (num6 < num5)
				{
					num4 = Interpolate(mag, count, num3);
				}
				else if (value == 1)
				{
					double num7 = 0.0;
					for (int j = num5; j <= num6; j++)
					{
						num7 += (double)mag[j] * (double)mag[j];
					}
					num4 = Math.Sqrt(num7 / (double)(num6 - num5 + 1));
				}
				else
				{
					float num8 = 0f;
					for (int k = num5; k <= num6; k++)
					{
						if (mag[k] > num8)
						{
							num8 = mag[k];
						}
					}
					num4 = num8;
				}
			}
			dbOut[i] = 20.0 * Math.Log10((num4 > 5.960464477539063E-08) ? num4 : 5.960464477539063E-08);
		}
	}

	private static double Interpolate(float[] mag, int count, double k)
	{
		int num = (int)Math.Floor(k);
		double num2 = k - (double)num;
		double num3 = mag[Math.Max(0, Math.Min(count - 1, num - 1))];
		double num4 = mag[Math.Max(0, Math.Min(count - 1, num))];
		double num5 = mag[Math.Max(0, Math.Min(count - 1, num + 1))];
		double num6 = mag[Math.Max(0, Math.Min(count - 1, num + 2))];
		double val = num4 + 0.5 * num2 * (num5 - num3 + num2 * (2.0 * num3 - 5.0 * num4 + 4.0 * num5 - num6 + num2 * (3.0 * (num4 - num5) + num6 - num3)));
		return Math.Max(0.0, val);
	}

	private void Table(int n, int rate, int bands, double minHz, double maxHz, int scale)
	{
		if (_lo == null || n != _tN || rate != _tRate || bands != _tBands || minHz != _tMin || maxHz != _tMax || scale != _tScale)
		{
			_tN = n;
			_tRate = rate;
			_tBands = bands;
			_tMin = minHz;
			_tMax = maxHz;
			_tScale = scale;
			_lo = new double[bands];
			_hi = new double[bands];
			_centre = new double[bands];
			double num = (double)n / (double)rate;
			for (int i = 0; i < bands; i++)
			{
				_lo[i] = ToHz((double)i / (double)bands, minHz, maxHz, scale) * num;
				_hi[i] = ToHz((double)(i + 1) / (double)bands, minHz, maxHz, scale) * num;
				_centre[i] = ToHz(((double)i + 0.5) / (double)bands, minHz, maxHz, scale);
			}
		}
	}

	private void Prepare(int n, int window)
	{
		if (n == _n && window == _window)
		{
			return;
		}
		if (n != _n)
		{
			_n = n;
			_re = new float[n];
			_im = new float[n];
			_mag = new float[n / 2];
			int num = n / 2;
			_cos = new float[num];
			_sin = new float[num];
			for (int i = 0; i < num; i++)
			{
				double num2 = (double)(i * 2) * Math.PI / (double)n;
				_cos[i] = (float)Math.Cos(num2);
				_sin[i] = (float)(0.0 - Math.Sin(num2));
			}
			List<int> list = new List<int>();
			List<int> list2 = new List<int>();
			int j;
			for (j = 0; 1 << j < n; j++)
			{
			}
			for (int k = 0; k < n; k++)
			{
				int num3 = 0;
				int num4 = k;
				for (int l = 0; l < j; l++)
				{
					num3 = (num3 << 1) | (num4 & 1);
					num4 >>= 1;
				}
				if (k < num3)
				{
					list.Add(k);
					list2.Add(num3);
				}
			}
			_swapA = list.ToArray();
			_swapB = list2.ToArray();
		}
		_window = window;
		_w = new float[n];
		double num5 = 0.0;
		for (int m = 0; m < n; m++)
		{
			double num6 = (double)m / (double)(n - 1);
			double num7;
			switch (window)
			{
			case 1:
				num7 = 287.0 / 800.0 - 0.48829 * Math.Cos(Math.PI * 2.0 * num6) + 0.14128 * Math.Cos(Math.PI * 4.0 * num6) - 0.01168 * Math.Cos(Math.PI * 6.0 * num6);
				break;
			case 2:
			{
				double num8 = ((double)m - (double)(n - 1) * 0.5) / ((double)(n - 1) * 0.15);
				num7 = Math.Exp(-0.5 * num8 * num8);
				break;
			}
			default:
				num7 = 0.5 - 0.5 * Math.Cos(Math.PI * 2.0 * num6);
				break;
			}
			_w[m] = (float)num7;
			num5 += num7;
		}
		_norm = (float)(2.0 / num5);
	}

	private unsafe void Fft()
	{
		int n = _n;
		fixed (float* re = _re, im = _im, cos = _cos, sin = _sin)
		{
			for (int i = 0; i < _swapA.Length; i++)
			{
				int num = _swapA[i];
				int num2 = _swapB[i];
				float num3 = re[num];
				re[num] = re[num2];
				re[num2] = num3;
				num3 = im[num];
				im[num] = im[num2];
				im[num2] = num3;
			}
			for (int num4 = 2; num4 <= n; num4 <<= 1)
			{
				int num5 = num4 >> 1;
				int num6 = n / num4;
				for (int j = 0; j < n; j += num4)
				{
					int num7 = 0;
					int num8 = j;
					while (num8 < j + num5)
					{
						float num9 = cos[num7];
						float num10 = sin[num7];
						int num11 = num8 + num5;
						float num12 = re[num11] * num9 - im[num11] * num10;
						float num13 = re[num11] * num10 + im[num11] * num9;
						re[num11] = re[num8] - num12;
						im[num11] = im[num8] - num13;
						re[num8] += num12;
						im[num8] += num13;
						num8++;
						num7 += num6;
					}
				}
			}
		}
	}
}
