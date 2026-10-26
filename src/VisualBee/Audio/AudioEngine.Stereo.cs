using System;

namespace MusicBeePlugin
{
    public sealed partial class AudioEngine
    {
        private void ExtractStereoWaveforms(float[] left, float[] right, int size, float dt)
        {
            if (left == null || right == null || size < 256) return;
            for (int i = 0; i < 256; i++)
            {
                float rawL = i < size ? left[i] : 0f;
                float rawR = i < size ? right[i] : 0f;
                float prevL = _waveformL[i];
                float prevR = _waveformR[i];
                float tau = 0.018f;
                float coeff = 1.0f - (float)Math.Exp(-dt / tau);
                _waveformL[i] = prevL + (rawL - prevL) * coeff;
                _waveformR[i] = prevR + (rawR - prevR) * coeff;
            }
        }

        private void ComputeStereoMetrics(float[] left, float[] right, int size, out float width, out float correlation)
        {
            width = 0f;
            correlation = 1.0f;
            if (left == null || right == null || size <= 0) return;

            double sumLR = 0.0, sumLL = 0.0, sumRR = 0.0;
            int n = Math.Min(size, 1024);
            for (int i = 0; i < n; i++)
            {
                float l = left[i];
                float r = right[i];
                sumLR += l * r;
                sumLL += l * l;
                sumRR += r * r;
            }

            double denom = Math.Sqrt(sumLL * sumRR);
            if (denom > 1e-6)
            {
                correlation = (float)(sumLR / denom);
                width = (float)Math.Max(0.0, Math.Min(1.0, 1.0 - correlation));
            }
        }
    }
}
