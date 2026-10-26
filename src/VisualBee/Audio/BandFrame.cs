using System;

namespace MusicBeePlugin
{
    public sealed class BandFrame
    {
        public const int BandCount = 160;
        public const int WaveformCount = 256;

        public readonly float[] Levels = new float[BandCount];
        public readonly float[] LevelsL = new float[BandCount];
        public readonly float[] LevelsR = new float[BandCount];
        public readonly float[] Waveform = new float[WaveformCount];
        public readonly float[] WaveformL = new float[WaveformCount];
        public readonly float[] WaveformR = new float[WaveformCount];
        public AudioSnapshot Metrics;
        public long TimestampTicks;
        public ulong Sequence;

        public void CopyFrom(BandFrame source)
        {
            if (source == null) return;
            Array.Copy(source.Levels, Levels, BandCount);
            Array.Copy(source.LevelsL, LevelsL, BandCount);
            Array.Copy(source.LevelsR, LevelsR, BandCount);
            Array.Copy(source.Waveform, Waveform, WaveformCount);
            Array.Copy(source.WaveformL, WaveformL, WaveformCount);
            Array.Copy(source.WaveformR, WaveformR, WaveformCount);
            Metrics = source.Metrics;
            TimestampTicks = source.TimestampTicks;
            Sequence = source.Sequence;
        }

        public void Clear()
        {
            Array.Clear(Levels, 0, BandCount);
            Array.Clear(LevelsL, 0, BandCount);
            Array.Clear(LevelsR, 0, BandCount);
            Array.Clear(Waveform, 0, WaveformCount);
            Array.Clear(WaveformL, 0, WaveformCount);
            Array.Clear(WaveformR, 0, WaveformCount);
            Metrics = default(AudioSnapshot);
            Metrics.Source = AudioSourceKind.Silent;
            TimestampTicks = 0;
            Sequence = 0;
        }
    }
}
