using System;
using System.Collections.Generic;

namespace MusicBeePlugin
{
    /// <summary>
    /// Gaussian-windowed FFT and foobar2000-style logarithmic band analyzer.
    /// </summary>
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

        public static int WindowSize(int sampleRate)
        {
            long target = (long)Math.Round(0.2 * sampleRate, MidpointRounding.AwayFromZero);
            int n = 2;
            while ((long)n * 2 <= target) n *= 2;
            return n;
        }

        public static double BandFrequency(double x, int bands, double minFreq = 50.0, double maxFreq = 20000.0)
        {
            if (minFreq <= 0.0) minFreq = 20.0;
            if (maxFreq <= minFreq) maxFreq = minFreq + 1000.0;
            return minFreq * Math.Pow(maxFreq / minFreq, x / bands);
        }

        public void Analyze(float[] mono, int n, int sampleRate, int bands, double[] dbOut, double minFreq = 50.0, double maxFreq = 20000.0)
        {
            EnsureFft(n);
            for (int i = 0; i < n; i++)
            {
                _re[i] = _window[i] * mono[i];
                _im[i] = 0f;
            }
            Fft();

            int half = n >> 1;
            for (int i = 0; i < half; i++)
                _mag[i] = (float)Math.Sqrt(_re[i] * _re[i] + _im[i] * _im[i]) * _windowNorm;

            double scale = (double)half / (sampleRate * 0.5);
            for (int b = 0; b < bands; b++)
            {
                int lo = BinIndex(BandFrequency((double)b - 0.5, bands, minFreq, maxFreq) * scale, half);
                int hi = BinIndex(BandFrequency((double)b + 0.5, bands, minFreq, maxFreq) * scale, half);
                double value;
                if (lo + 1 < hi)
                {
                    float peak = 0f;
                    for (int i = lo; i < hi; i++)
                    {
                        float m = Math.Abs(_mag[i]);
                        if (m > peak) peak = m;
                    }
                    value = peak;
                }
                else
                {
                    value = NarrowBand(mono, n, sampleRate, BandFrequency(b, bands, minFreq, maxFreq));
                }
                dbOut[b] = ToDb(value);
            }
        }

        public static void BandsFromMagnitudes(float[] mag, int count, int sampleRate, int bands, double[] dbOut, double minFreq = 50.0, double maxFreq = 20000.0)
        {
            if (count <= 0)
            {
                for (int i = 0; i < bands; i++) dbOut[i] = FloorDb;
                return;
            }
            double scale = (double)count / (sampleRate * 0.5);
            for (int b = 0; b < bands; b++)
            {
                int lo = BinIndex(BandFrequency((double)b - 0.5, bands, minFreq, maxFreq) * scale, count);
                int hi = BinIndex(BandFrequency((double)b + 0.5, bands, minFreq, maxFreq) * scale, count);
                double value;
                if (lo + 1 < hi)
                {
                    float peak = 0f;
                    for (int i = lo; i < hi; i++)
                    {
                        float m = Math.Abs(mag[i]);
                        if (m > peak) peak = m;
                    }
                    value = peak;
                }
                else
                {
                    double pos = BandFrequency(b, bands, minFreq, maxFreq) * scale;
                    if (pos > count) value = 0.0;
                    else
                    {
                        int i0 = (int)Math.Floor(pos);
                        double frac = pos - i0;
                        double a = i0 < count ? Math.Abs(mag[i0]) : 0f;
                        double c = i0 + 1 < count ? Math.Abs(mag[i0 + 1]) : 0f;
                        value = a + (c - a) * frac;
                    }
                }
                dbOut[b] = ToDb(value);
            }
        }

        public static double ToDb(double value)
        {
            if (!(value > FloorValue)) value = FloorValue;
            return 20.0 * Math.Log10(value);
        }

        private static int BinIndex(double position, int count)
        {
            double r = Math.Round(position, MidpointRounding.AwayFromZero);
            if (!(r < count)) return count;
            if (r < 0.0) return 0;
            return (int)r;
        }

        private void EnsureFft(int n)
        {
            if (_fftSize == n) return;
            _fftSize = n;
            _re = new float[n];
            _im = new float[n];
            _mag = new float[n / 2];
            _window = new float[n];

            double sum = 0.0;
            double mid = (n - 1) * 0.5;
            double sigma = (n - 1) * 0.15;
            for (int i = 0; i < n; i++)
            {
                double x = (i - mid) / sigma;
                double w = Math.Exp(x * x * -0.5);
                sum += w;
                _window[i] = (float)w;
            }
            _windowNorm = sum > 0.0 ? (float)(1.0 / sum + 1.0 / sum) : 0f;

            int half = n / 2;
            _cos = new float[half];
            _sin = new float[half];
            for (int i = 0; i < half; i++)
            {
                double angle = i * (Math.PI * 2.0) / n;
                _cos[i] = (float)Math.Cos(angle);
                _sin[i] = (float)(0.0 - Math.Sin(angle));
            }

            var a = new List<int>();
            var b = new List<int>();
            int bits = 0;
            while (1 << bits < n) bits++;
            for (int i = 0; i < n; i++)
            {
                int rev = 0;
                int v = i;
                for (int j = 0; j < bits; j++)
                {
                    rev = (rev << 1) | (v & 1);
                    v >>= 1;
                }
                if (i < rev)
                {
                    a.Add(i);
                    b.Add(rev);
                }
            }
            _swapA = a.ToArray();
            _swapB = b.ToArray();
        }

        private void Fft()
        {
            int n = _fftSize;
            float[] re = _re;
            float[] im = _im;
            for (int i = 0; i < _swapA.Length; i++)
            {
                int x = _swapA[i];
                int y = _swapB[i];
                float t = re[x]; re[x] = re[y]; re[y] = t;
                t = im[x]; im[x] = im[y]; im[y] = t;
            }

            for (int size = 2; size <= n; size <<= 1)
            {
                int half = size >> 1;
                int step = n / size;
                for (int start = 0; start < n; start += size)
                {
                    int k = 0;
                    for (int i = start; i < start + half; i++)
                    {
                        float c = _cos[k];
                        float s = _sin[k];
                        int j = i + half;
                        float tr = re[j] * c - im[j] * s;
                        float ti = re[j] * s + im[j] * c;
                        re[j] = re[i] - tr;
                        im[j] = im[i] - ti;
                        re[i] += tr;
                        im[i] += ti;
                        k += step;
                    }
                }
            }
        }
    }
}
