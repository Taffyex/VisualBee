using System;

namespace MusicBeePlugin
{
    public enum AudioSourceKind
    {
        Silent = 0,
        DirectPcm = 1,
        ApiFft = 2
    }

    public struct AudioSnapshot
    {
        // Frequency region envelopes (0.0 to 1.0)
        public float Sub;
        public float Bass;
        public float LowMid;
        public float Mid;
        public float HighMid;
        public float High;
        public float Air;

        // Dynamics & Perceptual metrics
        public float Rms;
        public float Peak;
        public float Transient;
        public float Beat;
        public float Centroid;
        public float LowPulse;
        public float StereoWidth;
        public float PhaseCorrelation;

        // Waveform for oscilloscope/ribbon (normalized -1.0 to 1.0)
        public float[] Waveform;
        public float[] WaveformL;
        public float[] WaveformR;

        // Technical metadata
        public int SampleRate;
        public int WindowSize;
        public int ValidBands;
        public AudioSourceKind Source;
    }
}
