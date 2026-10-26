using System;
using System.Drawing;
using System.Threading;

namespace MusicBeePlugin
{
    public enum VisualizerMode
    {
        SpectrumBars = 0,
        Waveform = 1
    }

    public enum HueCycleMotion
    {
        Sideways = 0,
        Spin = 1
    }

    public enum HueCycleSource
    {
        Palette = 0,
        Rainbow = 1
    }

    public enum StereoMode
    {
        Mono = 0,
        CenterOut = 1,
        SplitLR = 2
    }

    public enum AudioSourceMode
    {
        Auto = 0,
        DirectPcm = 1,
        ApiFft = 2
    }

    public enum BarVisualEffect
    {
        Pills = 0,
        Segmented = 1,
        Solid = 2
    }

    [Flags]
    public enum BarMirror
    {
        None = 0,
        Horizontal = 1,
        Vertical = 2
    }

    public enum BarColorMode
    {
        VerticalGradient = 0,
        AcrossFrequencies = 1,
        SolidTip = 2,
        HueCycle = 3
    }

    public enum PeakColorMode
    {
        PaletteTip = 0,
        BarColor = 1,
        Custom = 2
    }

    public enum BackgroundSource
    {
        Palette = 0,
        SkinPanel = 1,
        PureBlack = 2
    }

    public sealed partial class VisualBeeSettings
    {
        public event Action SettingsChanged;

        // General
        public VisualizerMode Mode = VisualizerMode.SpectrumBars;
        public StereoMode StereoMode = StereoMode.Mono;
        public AudioSourceMode AudioSource = AudioSourceMode.Auto;
        public BarVisualEffect BarStyle = BarVisualEffect.Pills;
        public BarMirror Mirror = BarMirror.None;

        // Bars
        public float BarGap = 0.22f;
        public int BarCount = 64;
        public float BarRoundness = 0.60f;
        public float TrebleTaper = 0.10f;
        public float LedSegmentHeight = 6.0f;
        public float LedSegmentGap = 2.0f;
        public bool HighlightBarTips = true;

        // Colours
        public string Palette = "skin";
        public BarColorMode ColorMode = BarColorMode.VerticalGradient;
        public float HueSpeed = 0.25f;
        public HueCycleMotion HueMotion = HueCycleMotion.Sideways;
        public HueCycleSource HueSource = HueCycleSource.Palette;

        // Peaks
        public bool PeakHoldEnabled = true;
        public float PeakHoldMs = 350f;
        public float PeakGravity = 2.8f;
        public float PeakCapThickness = 2.0f;
        public PeakColorMode PeakCapColorMode = PeakColorMode.PaletteTip;
        public Color PeakCustomColor = Color.FromArgb(255, 255, 255);

        // Analyzer
        public float Sensitivity = 1.0f;
        public float BeatSensitivity = 0.6f;
        public int AttackMs = 8;
        public int DecayMs = 65;
        public int SmoothingMs => DecayMs; // backward compatibility
        public float MinFreqHz = 50.0f;
        public float MaxFreqHz = 20000.0f;
        public float DbRange = 60.0f;
        public float TrebleTiltDb = 13.0f;
        public int SyncOffsetMs = 0;

        // Ribbon
        public float RibbonThickness = 2.5f;
        public bool RibbonFill = true;
        public bool RibbonFloorReflection = true;

        // Effects
        public float BloomIntensity = 0.85f;
        public BackgroundSource BgSource = BackgroundSource.Palette;
        public float BgBrightness = 1.0f;
        public float Vignette = 0.35f;
        public float FloorReflection = 0.0f;
        public bool ContactGlow = true;

        // Performance
        public int FrameCap = 0; // 0 = VSync, -1 = Uncapped, else 60/120/144/240/360
        public int AnalysisHz = 120; // 60, 120, 240
        public bool IdleThrottle = true;
        public bool ShowDiagnostics = false;

        private readonly string _path;
        private readonly object _saveLock = new object();
        private Timer _debounceTimer;

        public VisualBeeSettings(string path)
        {
            _path = path;
        }

        public void NotifyChanged()
        {
            try { SettingsChanged?.Invoke(); } catch { }
        }

        public void RequestSave()
        {
            lock (_saveLock)
            {
                if (_debounceTimer == null)
                {
                    _debounceTimer = new Timer(_ => Save(), null, 400, Timeout.Infinite);
                }
                else
                {
                    _debounceTimer.Change(400, Timeout.Infinite);
                }
            }
        }

        public void ResetToDefaults(string section = null)
        {
            string sec = (section ?? "").ToLowerInvariant();
            if (string.IsNullOrEmpty(sec) || sec == "general")
            {
                Mode = VisualizerMode.SpectrumBars;
                StereoMode = StereoMode.Mono;
                AudioSource = AudioSourceMode.Auto;
                Mirror = BarMirror.None;
            }
            if (string.IsNullOrEmpty(sec) || sec == "bars")
            {
                BarStyle = BarVisualEffect.Pills;
                BarGap = 0.22f;
                BarCount = 64;
                BarRoundness = 0.60f;
                TrebleTaper = 0.10f;
                LedSegmentHeight = 6.0f;
                LedSegmentGap = 2.0f;
                HighlightBarTips = true;
                StereoMode = StereoMode.Mono;
                PeakHoldEnabled = true;
                PeakHoldMs = 350f;
                PeakGravity = 2.8f;
                PeakCapThickness = 2.0f;
                PeakCapColorMode = PeakColorMode.PaletteTip;
                PeakCustomColor = Color.FromArgb(255, 255, 255);
            }
            if (string.IsNullOrEmpty(sec) || sec == "waveform")
            {
                RibbonThickness = 2.5f;
                RibbonFill = true;
                RibbonFloorReflection = true;
            }
            if (string.IsNullOrEmpty(sec) || sec == "colours" || sec == "colors")
            {
                Palette = "skin";
                ColorMode = BarColorMode.VerticalGradient;
                HueSpeed = 0.25f;
                HueMotion = HueCycleMotion.Sideways;
                HueSource = HueCycleSource.Palette;
            }
            if (string.IsNullOrEmpty(sec) || sec == "peaks")
            {
                PeakHoldEnabled = true;
                PeakHoldMs = 350f;
                PeakGravity = 2.8f;
                PeakCapThickness = 2.0f;
                PeakCapColorMode = PeakColorMode.PaletteTip;
                PeakCustomColor = Color.FromArgb(255, 255, 255);
            }
            if (string.IsNullOrEmpty(sec) || sec == "analyzer")
            {
                Sensitivity = 1.0f;
                BeatSensitivity = 0.6f;
                AttackMs = 8;
                DecayMs = 65;
                MinFreqHz = 50.0f;
                MaxFreqHz = 20000.0f;
                DbRange = 60.0f;
                TrebleTiltDb = 13.0f;
                SyncOffsetMs = 0;
            }
            if (string.IsNullOrEmpty(sec) || sec == "effects")
            {
                BloomIntensity = 0.85f;
                BgSource = BackgroundSource.Palette;
                BgBrightness = 1.0f;
                Vignette = 0.35f;
                FloorReflection = 0.0f;
                ContactGlow = true;
            }
            if (string.IsNullOrEmpty(sec) || sec == "performance")
            {
                FrameCap = 0;
                AnalysisHz = 120;
                IdleThrottle = true;
                ShowDiagnostics = false;
            }

            NotifyChanged();
            RequestSave();
        }
    }
}
