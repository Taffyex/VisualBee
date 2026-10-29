namespace MusicBeePlugin;

internal sealed class SpectrumLook : Look
{
	public const int BarLow = 10;

	public const int BarMid = 11;

	public const int BarHigh = 12;

	public const int PeakColour = 13;

	public const int ParticleColour = 14;

	public const int CurveColour = 15;

	public const int Slots = 16;

	public const int EngineGaussian = 0;

	public const int EngineFoobar = 0;

	public const int EngineModern = 1;

	public const int LayoutBars = 0;

	public const int LayoutCentred = 1;

	public const int LayoutMirror = 2;

	public const int LayoutMirrorOut = 3;

	public const int LayoutStereo = 4;

	public const int LayoutStereoSides = 5;

	public const int LayoutRadial = 6;

	public const int LayoutWave = 7;

	public const int LayoutOscilloscope = 8;

	public const int StyleGlass = 0;

	public const int StylePills = 1;

	public const int StyleLed = 2;

	public const int StyleSolid = 3;

	public const int StyleNixie = 4;

	public const int StyleHidden = 5;







	public const int ColourPlot = 0;

	public const int ColourBar = 1;

	public const int ColourFrequency = 2;

	public const int ColourLevel = 3;

	public const int ColourSingle = 4;

	public const int PeakLine = 0;

	public const int PeakBlock = 1;

	public const int PeakDot = 2;

	public const int PeakGlow = 3;

	public const int FallConstant = 0;

	public const int FallGravity = 1;

	public const int FallFade = 2;

	public const int EmitPeaks = 0;

	public const int EmitBeats = 1;

	public const int EmitFlow = 2;

	public const int AudioSourceAuto = 0;

	public const int AudioSourceAlwaysApi = 1;

	public const int AudioSourceBassOnly = 2;

	public const int AudioSourceWasapiLoopback = 3;

	public const int AudioSourcePlayerOnly = 4;

	public int AudioSource = AudioSourceAuto;

	public bool NeedsLoopback => AudioSource == AudioSourceWasapiLoopback || AudioSource == AudioSourceAuto;

	public int Engine = 1;

	public int Bands = 96;

	public bool FillWidth = true;

	public double MinHz = 30.0;

	public double MaxHz = 18000.0;

	public int Scale;

	public int FftSize = 2;

	public int Window;

	public int Value;

	public int Channels;

	public double Tilt = 3.0;

	public double FloorDb = -66.0;

	public double CeilingDb;

	public double AttackMs = 25.0;

	public double ReleaseMs = 280.0;

	public int Layout;

	public bool Flip;

	public double Margin = 4.0;

	public double RadialInner = 32.0;

	public double RadialTurn;

	public bool RadialArt;

	public double ReflectionOpacity = 24.0;

	public double ReflectionSize = 30.0;

	public int BarStyle = StyleGlass;

	public int GlassFill = 0;

	public int GlassTubeMode = 0;

	public double GlassWall = 18.0;

	public double GlassTint = 22.0;

	public double GlassSpecular = 85.0;

	public double GlassCaustics = 70.0;

	public double GhostRange = 35.0;

	public double TrebleTaper = 0.0;

	public bool TipHighlight = true;

	public bool CenterLaser = true;

	public double CenterLaserIntensity = 65.0;

	public int ScopeStyle = 0;

	public bool WaveNormalize = true;

	public double WaveGain = 100.0;

	public bool WaveFloorReflection = true;

	public bool ScopeGraticule = true;

	public int AnalysisHz = 0;

	public double Sensitivity = 100.0;

	public bool HdrIncandescence = true;

	public double GlassRefraction = 65.0;

	public double GlassOpacity = 45.0;

	public bool VfdMesh = true;

	public bool LacquerHorizon = true;

	public double BarWidth = 72.0;

	public double BarGap = 1.0;

	public double Radius = 3.0;

	public double LedSize = 4.0;

	public double LedGap = 2.0;

	public double LedGhost = 10.0;

	public double OutlineWidth = 1.5;

	public int Colouring;

	public bool UseMiddle = true;

	public double MiddleAt = 55.0;

	public double BarOpacity = 100.0;

	public double CurveWidth = 2.5;

	public double CurveFill = 55.0;

	public bool Snap;

	public bool Peaks = true;

	public int PeakStyle;

	public double PeakSize = 3.0;

	public double PeakHoldMs = 400.0;

	public int PeakFall = 1;

	public double PeakSpeed = 30.0;

	public double PeakGravity = 160.0;

	public bool PeakBarColour;

	public double PeakOpacity = 100.0;

	public bool Particles;

	public int Emit;

	public double ParticleAmount = 40.0;

	public double ParticleSize = 3.0;

	public double ParticleSpeed = 50.0;

	public double ParticleLife = 900.0;

	public double ParticleGravity = 35.0;

	public bool ParticleSparks;

	public bool ParticleBarColour = true;

	public bool DbGrid = true;

	public bool DbLabels;

	public bool FreqLabels;

	public double LabelSize = 100.0;

	public static readonly string[] AudioSourceNames = new string[5]
	{
		"Auto (Direct BASS + API + Loopback Fallback)",
		"Always MusicBee API (Universal Safe)",
		"Direct BASS Stream Only",
		"WASAPI Loopback (Browser & System Audio)",
		"MusicBee Player Only (Auto BASS + API)"
	};

	public static readonly string[] AudioSourceTokens = new string[5]
	{
		"auto",
		"api",
		"bass",
		"loopback",
		"player"
	};

	public static readonly string[] EngineNames = new string[2] { "Gaussian (Smooth)", "Modern FFT" };

	public static readonly string[] EngineTokens = new string[2] { "gaussian", "modern" };

	public static readonly string[] LayoutNames = new string[9] { "Bars", "Centred", "Mirrored (lows in the middle)", "Mirrored (lows at the sides)", "Stereo (left up, right down)", "Stereo (left and right halves)", "Radial", "Wave", "Oscilloscope (Waveform)" };

	public static readonly string[] LayoutTokens = new string[9] { "bars", "centred", "mirror", "mirror-out", "stereo", "stereo-sides", "radial", "wave", "scope" };

	public static readonly string[] StyleNames = new string[6] { "Glass Pillars (VFD)", "Smooth Pills", "LED segments", "Solid Neo-Bars", "Nixie / VFD Rungs", "Peaks only" };

	public static readonly string[] StyleTokens = new string[6] { "glass", "pills", "led", "solid", "nixie", "hidden" };

	public static readonly string[] GlassFillNames = new string[4] { "Pure Crystal Glass", "Liquid Plasma Column", "Twin Filament Anodes", "Hybrid Glass & VFD Rungs" };

	public static readonly string[] GlassFillTokens = new string[4] { "crystal", "plasma", "filaments", "rungs" };

	public static readonly string[] TubeModeNames = new string[3] { "Full-Height Vacuum Tube", "Dynamic Capsule (Bar Height)", "Off (Interior Only)" };

	public static readonly string[] TubeModeTokens = new string[3] { "full", "dynamic", "off" };

	public static readonly string[] ScopeStyleNames = new string[2] { "Liquid Neon Ribbon", "CRT Electron Beam" };

	public static readonly string[] ScopeStyleTokens = new string[2] { "neon", "crt" };

	public static readonly string[] AnalysisHzNames = new string[5] { "Sync to display refresh", "60 Hz", "120 Hz", "240 Hz", "500 Hz" };

	public static readonly string[] AnalysisHzTokens = new string[5] { "sync", "60", "120", "240", "500" };

	public static readonly int[] AnalysisHzRates = new int[5] { 0, 60, 120, 240, 500 };

	public static readonly string[] ColouringNames = new string[5] { "Gradient over the height", "Gradient on each bar", "Across the frequencies", "By level", "One colour" };

	public static readonly string[] ColouringTokens = new string[5] { "plot", "bar", "frequency", "level", "single" };

	public static readonly string[] PeakStyleNames = new string[4] { "Line", "Block", "Dot", "Glowing line" };

	public static readonly string[] PeakStyleTokens = new string[4] { "line", "block", "dot", "glow" };

	public static readonly string[] FallNames = new string[3] { "Steady", "Gravity", "Fade out" };

	public static readonly string[] FallTokens = new string[3] { "constant", "gravity", "fade" };

	public static readonly string[] EmitNames = new string[3] { "From rising peaks", "On beats", "Streaming off the bars" };

	public static readonly string[] EmitTokens = new string[3] { "peaks", "beats", "flow" };

	public override bool WantsArt
	{
		get
		{
			if (!base.WantsArt)
			{
				if (Layout == 6)
				{
					return RadialArt;
				}
				return false;
			}
			return true;
		}
	}

	public bool Stereo
	{
		get
		{
			if (Layout != 4)
			{
				return Layout == 5;
			}
			return true;
		}
	}

	public SpectrumLook()
		: base(16)
	{
		Colours[10] = -14795662;
		Colours[11] = -13984268;
		Colours[12] = -5835009;
		Colours[13] = -1507841;
		Colours[14] = -11349505;
		Colours[15] = -5835009;
		Colours[0] = -15854042;
		Colours[1] = -16447474;
		Colours[4] = 513586912;
		BloomStrength = 60.0;
		BloomThreshold = 30.0;
	}
}
