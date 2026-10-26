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
        Solid = 2,
        GlassPillar = 3
    }

    public enum GlassFillMode
    {
        Mixed = 0,
        SegmentedRungs = 1,
        SolidGlass = 2
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

        // General / Mode
        public VisualizerMode Mode = VisualizerMode.SpectrumBars;
        public StereoMode StereoMode = StereoMode.Mono;
        public AudioSourceMode AudioSource = AudioSourceMode.Auto;
        public BarVisualEffect BarStyle = BarVisualEffect.GlassPillar;
        public BarMirror Mirror = BarMirror.None;

        // Bars Geometry
        public float BarGap = 0.26f;
        public int BarCount = 32;
        public float BarRoundness = 0.60f;
        public float TrebleTaper = 0.05f;
        public float LedSegmentHeight = 5.0f;
        public float LedSegmentGap = 2.0f;
        public bool HighlightBarTips = true;

        // Glass Pillars & Rungs (Studio VFD)
        public GlassFillMode GlassFill = GlassFillMode.Mixed;
        public bool GlassTubeOverlay = true;
        public float GlassOpacity = 0.45f;
        public float RungGhostOpacity = 0.28f;
        public float RungGhostRange = 0.35f;

        // Center-Axis Symmetry & Horizontal Laser Line
        public bool CenterLineGlow = true;
        public float CenterLineGlowIntensity = 0.60f;
        public bool SymmetricCenterColor = true;

        // Colours
        public string Palette = "vfd_glass";
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
        public int SmoothingMs => DecayMs;
        public float MinFreqHz = 50.0f;
        public float MaxFreqHz = 20000.0f;
        public float DbRange = 60.0f;
        public float TrebleTiltDb = 13.0f;
        public int SyncOffsetMs = 0;

        // Ribbon / Waveform
        public float RibbonThickness = 2.5f;
        public bool RibbonFill = true;
        public bool RibbonFloorReflection = true;

        // Lighting & Environment
        public float BloomIntensity = 0.95f;
        public BackgroundSource BgSource = BackgroundSource.Palette;
        public float BgBrightness = 1.0f;
        public float Vignette = 0.35f;
        public float FloorReflection = 0.25f;
        public bool ContactGlow = true;
        public bool DecibelGrid = true;
        public float DecibelGridOpacity = 0.12f;
        public bool HdrIncandescence = true;

        // Atmosphere & Particles
        public bool ParticlesEnabled = true;
        public float ParticleIntensity = 0.45f;
        public float ParticleParallax = 0.60f;
        public bool CyberGridEnabled = false;
        public float CyberGridIntensity = 0.35f;
        public float ScanlineShimmer = 0.15f;

        // Performance & Telemetry
        public int FrameCap = 0;
        public int AnalysisHz = 120;
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
            if (string.IsNullOrEmpty(sec) || sec == "general" || sec == "studio")
            {
                Mode = VisualizerMode.SpectrumBars;
                StereoMode = StereoMode.Mono;
                AudioSource = AudioSourceMode.Auto;
                Mirror = BarMirror.None;
                BarStyle = BarVisualEffect.GlassPillar;
                BarGap = 0.26f;
                BarCount = 32;
                BarRoundness = 0.60f;
                TrebleTaper = 0.05f;
                LedSegmentHeight = 5.0f;
                LedSegmentGap = 2.0f;
                HighlightBarTips = true;
                GlassFill = GlassFillMode.Mixed;
                GlassTubeOverlay = true;
                GlassOpacity = 0.45f;
                RungGhostOpacity = 0.28f;
                RungGhostRange = 0.35f;
                CenterLineGlow = true;
                CenterLineGlowIntensity = 0.60f;
                SymmetricCenterColor = true;
                RibbonThickness = 2.5f;
                RibbonFill = true;
                RibbonFloorReflection = true;
                PeakHoldEnabled = true;
                PeakHoldMs = 350f;
                PeakGravity = 2.8f;
                PeakCapThickness = 2.0f;
                PeakCapColorMode = PeakColorMode.PaletteTip;
                PeakCustomColor = Color.FromArgb(255, 255, 255);
            }
            if (string.IsNullOrEmpty(sec) || sec == "theme" || sec == "colours")
            {
                Palette = "vfd_glass";
                ColorMode = BarColorMode.VerticalGradient;
                HueSpeed = 0.25f;
                HueMotion = HueCycleMotion.Sideways;
                HueSource = HueCycleSource.Palette;
                BloomIntensity = 0.95f;
                BgSource = BackgroundSource.Palette;
                BgBrightness = 1.0f;
                Vignette = 0.35f;
                FloorReflection = 0.25f;
                ContactGlow = true;
                DecibelGrid = true;
                DecibelGridOpacity = 0.12f;
                HdrIncandescence = true;
                ParticlesEnabled = true;
                ParticleIntensity = 0.45f;
                ParticleParallax = 0.60f;
                CyberGridEnabled = false;
                CyberGridIntensity = 0.35f;
                ScanlineShimmer = 0.15f;
            }
            if (string.IsNullOrEmpty(sec) || sec == "audio" || sec == "analyzer")
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
            if (string.IsNullOrEmpty(sec) || sec == "system" || sec == "performance")
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
