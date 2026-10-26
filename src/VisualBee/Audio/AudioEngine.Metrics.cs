using System;

namespace MusicBeePlugin
{
    public sealed partial class AudioEngine
    {
        private float Group(int lo, int hi)
        {
            if (hi < lo) return 0f;
            float sum = 0f;
            int count = 0;
            for (int i = lo; i <= hi && i < BandCount; i++)
            {
                sum += _levels[i];
                count++;
            }
            return count > 0 ? sum / count : 0f;
        }

        private static float Adaptive(ref float rollingMax, float value)
        {
            rollingMax = Math.Max(value, rollingMax * 0.995f);
            return value / Math.Max(0.20f, rollingMax * 1.05f);
        }

        private static float Boost(float value)
        {
            if (value <= 0f) return 0f;
            return (float)Math.Pow(Math.Min(1f, value), 0.62);
        }

        private float Peak()
        {
            float peak = 0f;
            for (int i = 0; i < BandCount; i++)
                if (_levels[i] > peak) peak = _levels[i];
            return peak;
        }

        private float Flux(int lo, int hi)
        {
            if (hi < lo) return 0f;
            float sum = 0f;
            int count = 0;
            for (int i = lo; i <= hi && i < BandCount; i++)
            {
                float diff = _levels[i] - _prevLevels[i];
                if (diff > 0f) sum += diff;
                count++;
            }
            return count > 0 ? sum / count : 0f;
        }

        private void UpdateBeat(float flux, float dt)
        {
            float alpha = 1f - (float)Math.Exp(-dt / 1.0f);
            _fluxAvg += (flux - _fluxAvg) * alpha;
            float dev = flux - _fluxAvg;
            _fluxVar += (dev * dev - _fluxVar) * alpha;

            float threshold = _fluxAvg + (1.8f - _settings.BeatSensitivity * 1.0f) * (float)Math.Sqrt(Math.Max(0f, _fluxVar)) + 0.008f;
            long now = DateTime.UtcNow.Ticks;
            long cooldown = TimeSpan.TicksPerMillisecond * 180;
            if (flux > threshold && now - _lastBeatTicks > cooldown)
            {
                _lastBeatTicks = now;
                _envBeat = 1f;
            }
        }

        private float ComputeRms()
        {
            if (_mono == null || _mono.Length == 0) return 0f;
            double sum = 0.0;
            for (int i = 0; i < _mono.Length; i++) sum += _mono[i] * _mono[i];
            return (float)Math.Sqrt(sum / _mono.Length) * 2.5f;
        }

        private float ComputeCentroid(int rate)
        {
            if (rate <= 0) rate = 44100;
            double weighted = 0.0;
            double total = 0.0;
            double minF = _settings?.MinFreqHz ?? 50.0;
            double maxF = _settings?.MaxFreqHz ?? 20000.0;
            for (int i = 0; i < BandCount; i++)
            {
                double hz = FoobarAnalyzer.BandFrequency(i, BandCount, minF, maxF);
                weighted += hz * _levels[i];
                total += _levels[i];
            }
            if (total <= 0.0) return 0f;
            double centroid = weighted / total;
            double norm = Math.Log(Math.Max(minF, centroid) / minF) / Math.Max(0.001, Math.Log(maxF / minF));
            return (float)Math.Max(0.0, Math.Min(1.0, norm));
        }

        private static float Follow(float env, float target, float dt, float attack, float release)
        {
            float tc = target > env ? attack : release;
            float k = 1f - (float)Math.Exp(-dt / Math.Max(0.0001f, tc));
            return env + (target - env) * k;
        }

        private static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }
    }
}
