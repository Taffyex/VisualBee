using System;
using System.Collections.Generic;

namespace MusicBeePlugin;

internal sealed partial class FoobarAnalyzer
{
	public static readonly double FloorValue = Math.Pow(2.0, -24.0);

	public static readonly double FloorDb = 20.0 * Math.Log10(FloorValue);

	private float[] _window;

	private float _windowNorm;

	private float[] _re;

	private float[] _im;

	private float[] _cos;

	private float[] _sin;

	private int[] _swapA;

	private int[] _swapB;

	private float[] _mag;

	private int _fftSize;

	private int _dftRate;

	private int _dftSize;

	private double[] _dftWindow;

	private double[] _windowed;

	private double _dftNorm;

	private readonly Dictionary<double, float[]> _twiddles = new Dictionary<double, float[]>();

	private int[] _narrowBand = new int[0];

	private float[][] _narrowTw = new float[0][];

	private double[] _last;

	private int _lastSegment;

	private long _lastStart;

	private long _lastAudioStart;

	private int _lastSize;

	private int _lastRate;

	public static int WindowSize(int sampleRate)
	{
		long num = (long)Math.Round(0.2 * (double)sampleRate, MidpointRounding.AwayFromZero);
		int num2 = 2;
		while ((long)num2 * 2L <= num)
		{
			num2 *= 2;
		}
		return num2;
	}

	public static double BandFrequency(double x, int bands)
	{
		return Math.Pow(1.37, 20.0 / (double)bands * x) * 50.0;
	}

	public static int UsableBands(int bands, int sampleRate)
	{
		int num = WindowSize(sampleRate);
		int num2 = num >> 1;
		double num3 = (double)num2 / ((double)sampleRate * 0.5);
		for (int num4 = bands - 1; num4 > 0; num4--)
		{
			int num5 = BinIndex(BandFrequency((double)num4 - 0.5, bands) * num3, num2);
			int num6 = BinIndex(BandFrequency((double)num4 + 0.5, bands) * num3, num2);
			if (num5 + 1 < num6 || (double)num * BandFrequency(num4, bands) / (double)sampleRate <= (double)num2)
			{
				return num4 + 1;
			}
		}
		return 1;
	}

	public void Analyze(float[] mono, int n, int sampleRate, int bands, double[] dbOut)
	{
		EnsureFft(n);
		for (int i = 0; i < n; i++)
		{
			_re[i] = _window[i] * mono[i];
			_im[i] = 0f;
		}
		Fft();
		int num = n >> 1;
		for (int j = 0; j < num; j++)
		{
			_mag[j] = (float)Math.Sqrt(_re[j] * _re[j] + _im[j] * _im[j]) * _windowNorm;
		}
		double num2 = (double)num / ((double)sampleRate * 0.5);
		int num3 = 0;
		if (_narrowBand.Length < bands)
		{
			_narrowBand = new int[bands];
			_narrowTw = new float[bands][];
		}
		for (int k = 0; k < bands; k++)
		{
			int num4 = BinIndex(BandFrequency((double)k - 0.5, bands) * num2, num);
			int num5 = BinIndex(BandFrequency((double)k + 0.5, bands) * num2, num);
			if (num4 + 1 < num5)
			{
				float num6 = 0f;
				for (int l = num4; l < num5; l++)
				{
					float num7 = Math.Abs(_mag[l]);
					if (num7 > num6)
					{
						num6 = num7;
					}
				}
				dbOut[k] = ToDb(num6);
			}
			else
			{
				float[] array = Twiddles(n, sampleRate, BandFrequency(k, bands));
				if (array == null)
				{
					dbOut[k] = ToDb(0.0);
					continue;
				}
				_narrowBand[num3] = k;
				_narrowTw[num3] = array;
				num3++;
			}
		}
		if (num3 > 0)
		{
			NarrowBands(mono, n, num3, dbOut);
		}
	}

	public bool Reuse(int segment, long windowStart, long audioStart, int n, int sampleRate, double[] dbOut)
	{
		if (_last == null || _last.Length != dbOut.Length || segment != _lastSegment || windowStart != _lastStart || audioStart != _lastAudioStart || n != _lastSize || sampleRate != _lastRate)
		{
			return false;
		}
		Array.Copy(_last, dbOut, dbOut.Length);
		return true;
	}

	public void Remember(int segment, long windowStart, long audioStart, int n, int sampleRate, double[] db)
	{
		if (_last == null || _last.Length != db.Length)
		{
			_last = new double[db.Length];
		}
		Array.Copy(db, _last, db.Length);
		_lastSegment = segment;
		_lastStart = windowStart;
		_lastAudioStart = audioStart;
		_lastSize = n;
		_lastRate = sampleRate;
	}

	public static void BandsFromMagnitudes(float[] mag, int count, int sampleRate, int bands, double[] dbOut)
	{
		double num = (double)count / ((double)sampleRate * 0.5);
		for (int i = 0; i < bands; i++)
		{
			int num2 = BinIndex(BandFrequency((double)i - 0.5, bands) * num, count);
			int num3 = BinIndex(BandFrequency((double)i + 0.5, bands) * num, count);
			double value;
			if (num2 + 1 < num3)
			{
				float num4 = 0f;
				for (int j = num2; j < num3; j++)
				{
					float num5 = Math.Abs(mag[j]);
					if (num5 > num4)
					{
						num4 = num5;
					}
				}
				value = num4;
			}
			else
			{
				double num6 = BandFrequency(i, bands) * num;
				if (num6 > (double)count)
				{
					value = 0.0;
				}
				else
				{
					int num7 = (int)Math.Floor(num6);
					double num8 = num6 - (double)num7;
					double num9 = ((num7 < count) ? Math.Abs(mag[num7]) : 0f);
					double num10 = ((num7 + 1 < count) ? Math.Abs(mag[num7 + 1]) : 0f);
					value = num9 + (num10 - num9) * num8;
				}
			}
			dbOut[i] = ToDb(value);
		}
	}

	public static double ToDb(double value)
	{
		if (!(value > FloorValue))
		{
			value = FloorValue;
		}
		return 20.0 * Math.Log10(value);
	}

	private static int BinIndex(double position, int count)
	{
		double num = Math.Round(position, MidpointRounding.AwayFromZero);
		if (!(num < (double)count))
		{
			return count;
		}
		if (num < 0.0)
		{
			return 0;
		}
		return (int)num;
	}

}
