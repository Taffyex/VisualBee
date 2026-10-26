using System;
using System.Diagnostics;

namespace MusicBeePlugin
{
    internal sealed class BarDynamics
    {
        public const int MaxBands = 160;
        private const float MicroFloatSeconds = 0.080f;
        private const float GravityPeak = 3.6f;

        private readonly BandFrame _prevFrame = new BandFrame();
        private readonly BandFrame _currFrame = new BandFrame();

        private readonly float[] _interpolatedBandsMono = new float[MaxBands];
        private readonly float[] _interpolatedBandsL = new float[MaxBands];
        private readonly float[] _interpolatedBandsR = new float[MaxBands];

        // Left / Mono channel state
        private readonly float[] _heightL = new float[MaxBands];
        private readonly float[] _velocityL = new float[MaxBands];
        private readonly float[] _peakL = new float[MaxBands];
        private readonly float[] _peakHoldTimerL = new float[MaxBands];
        private readonly float[] _peakVelocityL = new float[MaxBands];
        private readonly float[] _prevTargetL = new float[MaxBands];

        // Right channel state
        private readonly float[] _heightR = new float[MaxBands];
        private readonly float[] _velocityR = new float[MaxBands];
        private readonly float[] _peakR = new float[MaxBands];
        private readonly float[] _peakHoldTimerR = new float[MaxBands];
        private readonly float[] _peakVelocityR = new float[MaxBands];
        private readonly float[] _prevTargetR = new float[MaxBands];
        private readonly float[] _rawTargets = new float[MaxBands];

        private int _lastBarCount = -1;

        public void Push(BandFrame frame)
        {
            if (frame == null) return;
            if (frame.Sequence != _currFrame.Sequence)
            {
                _prevFrame.CopyFrom(_currFrame);
                _currFrame.CopyFrom(frame);
            }
        }

        public void Step(double nowSeconds, float dt, VisualBeeSettings settings, float[] outHeightPeak)
        {
            if (settings == null || outHeightPeak == null) return;

            int barCount = Math.Max(16, Math.Min(MaxBands, settings.BarCount));
            if (barCount != _lastBarCount)
            {
                ResetChannelState();
                _lastBarCount = barCount;
            }

            InterpolateAudio();

            bool isStereo = (settings.StereoMode != StereoMode.Mono);
            if (isStereo)
            {
                StepChannel(_interpolatedBandsL, _heightL, _velocityL, _peakL, _peakHoldTimerL, _peakVelocityL, _prevTargetL, barCount, dt, settings, outHeightPeak, 0);
                StepChannel(_interpolatedBandsR, _heightR, _velocityR, _peakR, _peakHoldTimerR, _peakVelocityR, _prevTargetR, barCount, dt, settings, outHeightPeak, MaxBands * 2);
            }
            else
            {
                StepChannel(_interpolatedBandsMono, _heightL, _velocityL, _peakL, _peakHoldTimerL, _peakVelocityL, _prevTargetL, barCount, dt, settings, outHeightPeak, 0);
                if (outHeightPeak.Length >= MaxBands * 4)
                    Array.Copy(outHeightPeak, 0, outHeightPeak, MaxBands * 2, MaxBands * 2);
            }
        }

        private void StepChannel(
            float[] inputBands,
            float[] height,
            float[] velocity,
            float[] peak,
            float[] peakHoldTimer,
            float[] peakVelocity,
            float[] prevTarget,
            int barCount,
            float dt,
            VisualBeeSettings settings,
            float[] outHeightPeak,
            int outOffset)
        {
            float attackSec = Math.Max(0.002f, Math.Min(0.100f, settings.AttackMs / 1000f));
            float decaySec = Math.Max(0.010f, Math.Min(0.500f, settings.DecayMs / 1000f));
            float alphaAttack = 1.0f - (float)Math.Exp(-dt / attackSec);
            float alphaDecay = 1.0f - (float)Math.Exp(-dt / decaySec);
            float holdDuration = Math.Max(0.05f, settings.PeakHoldMs / 1000f);
            float gravity = Math.Max(0.5f, settings.PeakGravity);

            for (int b = 0; b < barCount; b++)
            {
                int lo = (int)Math.Floor((double)b * MaxBands / barCount);
                int hi = (int)Math.Floor((double)(b + 1) * MaxBands / barCount) - 1;
                if (hi < lo) hi = lo;
                if (hi >= MaxBands) hi = MaxBands - 1;

                float maxVal = 0f;
                for (int k = lo; k <= hi; k++)
                {
                    if (inputBands[k] > maxVal)
                        maxVal = inputBands[k];
                }

                _rawTargets[b] = Math.Max(0f, Math.Min(1.0f, maxVal * settings.Sensitivity));
            }

            for (int b = 0; b < barCount; b++)
            {
                float left = (b > 0) ? _rawTargets[b - 1] : _rawTargets[b];
                float center = _rawTargets[b];
                float right = (b < barCount - 1) ? _rawTargets[b + 1] : _rawTargets[b];
                float target = left * 0.18f + center * 0.64f + right * 0.18f;

                if (target >= height[b])
                    height[b] += (target - height[b]) * alphaAttack;
                else
                    height[b] += (target - height[b]) * alphaDecay;

                if (height[b] >= peak[b])
                {
                    peak[b] = height[b];
                    peakHoldTimer[b] = holdDuration;
                    peakVelocity[b] = 0f;
                }
                else
                {
                    if (peakHoldTimer[b] > 0f)
                    {
                        peakHoldTimer[b] -= dt;
                        peakVelocity[b] = 0f;
                    }
                    else
                    {
                        peakVelocity[b] += gravity * dt;
                        peak[b] -= peakVelocity[b] * dt;
                        if (peak[b] < height[b])
                        {
                            peak[b] = height[b];
                            peakVelocity[b] = 0f;
                        }
                    }
                }

                height[b] = Math.Max(0f, Math.Min(1.0f, height[b]));
                peak[b] = Math.Max(height[b], Math.Min(1.0f, peak[b]));

                int outIdx = outOffset + b * 2;
                if (outIdx + 1 < outHeightPeak.Length)
                {
                    outHeightPeak[outIdx] = height[b];
                    outHeightPeak[outIdx + 1] = settings.PeakHoldEnabled ? peak[b] : 0f;
                }
            }

            for (int b = barCount; b < MaxBands; b++)
            {
                int outIdx = outOffset + b * 2;
                if (outIdx + 1 < outHeightPeak.Length)
                {
                    outHeightPeak[outIdx] = 0f;
                    outHeightPeak[outIdx + 1] = 0f;
                }
            }
        }

        private void ResetChannelState()
        {
            Array.Clear(_heightL, 0, MaxBands);
            Array.Clear(_velocityL, 0, MaxBands);
            Array.Clear(_peakL, 0, MaxBands);
            Array.Clear(_peakHoldTimerL, 0, MaxBands);
            Array.Clear(_peakVelocityL, 0, MaxBands);
            Array.Clear(_prevTargetL, 0, MaxBands);

            Array.Clear(_heightR, 0, MaxBands);
            Array.Clear(_velocityR, 0, MaxBands);
            Array.Clear(_peakR, 0, MaxBands);
            Array.Clear(_peakHoldTimerR, 0, MaxBands);
            Array.Clear(_peakVelocityR, 0, MaxBands);
            Array.Clear(_prevTargetR, 0, MaxBands);
        }

        private void InterpolateAudio()
        {
            if (_currFrame.Metrics.Source == AudioSourceKind.Silent)
            {
                Array.Clear(_interpolatedBandsMono, 0, MaxBands);
                Array.Clear(_interpolatedBandsL, 0, MaxBands);
                Array.Clear(_interpolatedBandsR, 0, MaxBands);
                return;
            }

            long dtTicks = _currFrame.TimestampTicks - _prevFrame.TimestampTicks;
            if (dtTicks <= 0 || _prevFrame.Sequence == 0)
            {
                Array.Copy(_currFrame.Levels, _interpolatedBandsMono, MaxBands);
                Array.Copy(_currFrame.LevelsL, _interpolatedBandsL, MaxBands);
                Array.Copy(_currFrame.LevelsR, _interpolatedBandsR, MaxBands);
                return;
            }

            long nowTicks = Stopwatch.GetTimestamp();
            long elapsedTicks = nowTicks - _currFrame.TimestampTicks;
            float t = (float)elapsedTicks / dtTicks;
            t = Math.Max(0f, Math.Min(1.0f, t));

            for (int i = 0; i < MaxBands; i++)
            {
                _interpolatedBandsMono[i] = _prevFrame.Levels[i] + (_currFrame.Levels[i] - _prevFrame.Levels[i]) * t;
                _interpolatedBandsL[i] = _prevFrame.LevelsL[i] + (_currFrame.LevelsL[i] - _prevFrame.LevelsL[i]) * t;
                _interpolatedBandsR[i] = _prevFrame.LevelsR[i] + (_currFrame.LevelsR[i] - _prevFrame.LevelsR[i]) * t;
            }
        }
    }
}
