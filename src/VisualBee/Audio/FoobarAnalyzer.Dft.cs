using System;
using System.Collections.Generic;

namespace MusicBeePlugin
{
    internal sealed partial class FoobarAnalyzer
    {
        private int _dftRate;
        private int _dftSize;
        private double[] _dftWindow;
        private double _dftNorm;
        private readonly Dictionary<double, float[]> _twiddles = new Dictionary<double, float[]>();

        private double NarrowBand(float[] mono, int n, int sampleRate, double frequency)
        {
            double position = (double)n * frequency / sampleRate;
            if (position > (n >> 1)) return 0.0;

            if (sampleRate != _dftRate || n != _dftSize)
            {
                _twiddles.Clear();
                _dftRate = sampleRate;
                _dftSize = n;
            }

            float[] twiddle;
            if (!_twiddles.TryGetValue(frequency, out twiddle))
            {
                twiddle = new float[n * 2];
                double step = position * (Math.PI * -2.0) / n;
                for (int i = 0; i < n; i++)
                {
                    double angle = i * step;
                    twiddle[2 * i] = (float)Math.Cos(angle);
                    twiddle[2 * i + 1] = (float)Math.Sin(angle);
                }
                _twiddles[frequency] = twiddle;
            }

            if (_dftWindow == null || _dftWindow.Length != n)
            {
                _dftWindow = new double[n];
                double sum = 0.0;
                double step = 6.666666666666667 / (n - 1);
                double mid = (n - 1) * 0.5;
                for (int i = 0; i < n; i++)
                {
                    double x = (i - mid) * step;
                    double w = Math.Exp(x * x * -0.5);
                    _dftWindow[i] = w;
                    sum += w;
                }
                _dftNorm = sum > 0.0 ? (1.0 / sum + 1.0 / sum) : 0.0;
            }

            double re = 0.0;
            double im = 0.0;
            for (int i = 0; i < n; i++)
            {
                double sample = mono[i] * _dftWindow[i];
                re += twiddle[2 * i] * sample;
                im += twiddle[2 * i + 1] * sample;
            }
            return Math.Sqrt(im * im + re * re) * _dftNorm;
        }
    }
}
