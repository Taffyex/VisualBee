using System;
using System.Collections.Generic;

namespace MusicBeePlugin;

internal sealed partial class GaussianAnalyzer
{
	private float[] Twiddles(int n, int sampleRate, double frequency)
	{
		double num = (double)n * frequency / (double)sampleRate;
		if (num > (double)(n >> 1))
		{
			return null;
		}
		if (sampleRate != _dftRate || n != _dftSize)
		{
			_twiddles.Clear();
			_dftRate = sampleRate;
			_dftSize = n;
		}
		if (!_twiddles.TryGetValue(frequency, out var value))
		{
			value = new float[n * 2];
			double num2 = num * (Math.PI * -2.0) / (double)n;
			for (int i = 0; i < n; i++)
			{
				double num3 = (double)i * num2;
				value[2 * i] = (float)Math.Cos(num3);
				value[2 * i + 1] = (float)Math.Sin(num3);
			}
			_twiddles[frequency] = value;
		}
		return value;
	}

	private void NarrowBands(float[] mono, int n, int count, double[] dbOut)
	{
		if (_dftWindow == null || _dftWindow.Length != n)
		{
			_dftWindow = new double[n];
			_windowed = new double[n];
			double num = 0.0;
			double num2 = 6.666666666666667 / (double)(n - 1);
			double num3 = (double)(n - 1) * 0.5;
			for (int i = 0; i < n; i++)
			{
				double num4 = ((double)i - num3) * num2;
				double num5 = Math.Exp(num4 * num4 * -0.5);
				_dftWindow[i] = num5;
				num += num5;
			}
			_dftNorm = ((num > 0.0) ? (1.0 / num + 1.0 / num) : 0.0);
		}
		double[] windowed = _windowed;
		unsafe
		{
			fixed (float* pMono = mono)
			fixed (double* pDftWin = _dftWindow)
			fixed (double* pWin = windowed)
			{
				for (int j = 0; j < n; j++) pWin[j] = (double)pMono[j] * pDftWin[j];
				int k;
				for (k = 0; k + 4 <= count; k += 4)
				{
					fixed (float* p1 = _narrowTw[k], p2 = _narrowTw[k + 1], p3 = _narrowTw[k + 2], p4 = _narrowTw[k + 3])
					{
						double num6 = 0.0, num7 = 0.0, num8 = 0.0, num9 = 0.0;
						double num10 = 0.0, num11 = 0.0, num12 = 0.0, num13 = 0.0;
						for (int l = 0; l < n; l++)
						{
							double num14 = pWin[l];
							int num15 = 2 * l;
							num6 += (double)p1[num15] * num14;
							num7 += (double)p1[num15 + 1] * num14;
							num8 += (double)p2[num15] * num14;
							num9 += (double)p2[num15 + 1] * num14;
							num10 += (double)p3[num15] * num14;
							num11 += (double)p3[num15 + 1] * num14;
							num12 += (double)p4[num15] * num14;
							num13 += (double)p4[num15 + 1] * num14;
						}
						dbOut[_narrowBand[k]] = ToDb(Math.Sqrt(num7 * num7 + num6 * num6) * _dftNorm);
						dbOut[_narrowBand[k + 1]] = ToDb(Math.Sqrt(num9 * num9 + num8 * num8) * _dftNorm);
						dbOut[_narrowBand[k + 2]] = ToDb(Math.Sqrt(num11 * num11 + num10 * num10) * _dftNorm);
						dbOut[_narrowBand[k + 3]] = ToDb(Math.Sqrt(num13 * num13 + num12 * num12) * _dftNorm);
					}
				}
				for (; k < count; k++)
				{
					fixed (float* p5 = _narrowTw[k])
					{
						double num16 = 0.0, num17 = 0.0;
						for (int m = 0; m < n; m++)
						{
							double num18 = pWin[m];
							num16 += (double)p5[2 * m] * num18;
							num17 += (double)p5[2 * m + 1] * num18;
						}
						dbOut[_narrowBand[k]] = ToDb(Math.Sqrt(num17 * num17 + num16 * num16) * _dftNorm);
					}
				}
			}
		}
	}

	private void EnsureFft(int n)
	{
		if (_fftSize == n)
		{
			return;
		}
		_fftSize = n;
		_re = new float[n];
		_im = new float[n];
		_mag = new float[n / 2];
		_window = new float[n];
		double num = 0.0;
		double num2 = (double)(n - 1) * 0.5;
		double num3 = (double)(n - 1) * 0.15;
		for (int i = 0; i < n; i++)
		{
			double num4 = ((double)i - num2) / num3;
			double num5 = Math.Exp(num4 * num4 * -0.5);
			num += num5;
			_window[i] = (float)num5;
		}
		_windowNorm = ((num > 0.0) ? ((float)(1.0 / num + 1.0 / num)) : 0f);
		int num6 = n / 2;
		_cos = new float[num6];
		_sin = new float[num6];
		for (int j = 0; j < num6; j++)
		{
			double num7 = (double)j * (Math.PI * 2.0) / (double)n;
			_cos[j] = (float)Math.Cos(num7);
			_sin[j] = (float)(0.0 - Math.Sin(num7));
		}
		List<int> list = new List<int>();
		List<int> list2 = new List<int>();
		int k;
		for (k = 0; 1 << k < n; k++)
		{
		}
		for (int l = 0; l < n; l++)
		{
			int num8 = 0;
			int num9 = l;
			for (int m = 0; m < k; m++)
			{
				num8 = (num8 << 1) | (num9 & 1);
				num9 >>= 1;
			}
			if (l < num8)
			{
				list.Add(l);
				list2.Add(num8);
			}
		}
		_swapA = list.ToArray();
		_swapB = list2.ToArray();
	}

	private unsafe void Fft()
	{
		int fftSize = _fftSize;
		fixed (float* pRe = _re)
		fixed (float* pIm = _im)
		fixed (int* pSwapA = _swapA)
		fixed (int* pSwapB = _swapB)
		fixed (float* pCos = _cos)
		fixed (float* pSin = _sin)
		{
			int swapLen = _swapA.Length;
			for (int i = 0; i < swapLen; i++)
			{
				int num = pSwapA[i];
				int num2 = pSwapB[i];
				float num3 = pRe[num];
				pRe[num] = pRe[num2];
				pRe[num2] = num3;
				num3 = pIm[num];
				pIm[num] = pIm[num2];
				pIm[num2] = num3;
			}
			for (int num4 = 2; num4 <= fftSize; num4 <<= 1)
			{
				int num5 = num4 >> 1;
				int num6 = fftSize / num4;
				for (int j = 0; j < fftSize; j += num4)
				{
					int num7 = 0;
					for (int k = j; k < j + num5; k++)
					{
						float num8 = pCos[num7];
						float num9 = pSin[num7];
					int num10 = k + num5;
						float num11 = pRe[num10] * num8 - pIm[num10] * num9;
						float num12 = pRe[num10] * num9 + pIm[num10] * num8;
						pRe[num10] = pRe[k] - num11;
						pIm[num10] = pIm[k] - num12;
						pRe[k] += num11;
						pIm[k] += num12;
						num7 += num6;
					}
				}
			}
		}
	}
}
