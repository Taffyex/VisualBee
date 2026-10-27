namespace MusicBeePlugin;

internal abstract class Look
{
	public const int BackgroundTop = 0;

	public const int BackgroundBottom = 1;

	public const int BackgroundMiddle = 2;

	public const int LabelColour = 3;

	public const int GridColour = 4;

	public const int GlowColour = 5;

	public const int ShadowColour = 6;

	public const int AuroraA = 7;

	public const int AuroraB = 8;

	public const int AuroraC = 9;

	public const int CommonColours = 10;

	public int[] Colours;

	public int BackgroundMode;

	public double BackgroundAngle;

	public double BackgroundMiddleAt;

	public double ArtOpacity;

	public double ArtBlur;

	public bool CyberGrid;

	public double CyberGridIntensity = 45.0;

	public double CyberGridSpeed = 50.0;

	public double AuroraSpeed;

	public double AuroraIntensity;

	public double AuroraScale;

	public bool Bloom;

	public double BloomStrength;

	public double BloomThreshold;

	public double BloomRadius;

	public bool Trails;

	public double TrailMs;

	public bool Shadow;

	public double ShadowOpacity;

	public double ShadowBlur;

	public double ShadowDistance;

	public double ShadowAngle;

	public double Vignette;

	public double Scanlines;

	public double Aberration;

	public double Grain;

	public double BeatFlash;

	public bool HueCycle;

	public double HueSpeed;

	public bool HueCycleBackground;

	public bool ArtColours;

	public double ArtColoursAmount;

	public double TransitionMs;

	public int DelayMs;

	public int FrameCap;

	public bool ShowFps;

	public bool Dither;

	public bool BackgroundUseMiddle;

	public virtual bool WantsArt
	{
		get
		{
			if (!ArtColours)
			{
				return BackgroundMode == 4;
			}
			return true;
		}
	}

	protected Look(int colourSlots)
	{
		Colours = new int[colourSlots];
		Colours[0] = -15722458;
		Colours[1] = -16381681;
		Colours[2] = -15326664;
		Colours[3] = -8549466;
		Colours[4] = 817154288;
		Colours[5] = -1;
		Colours[6] = -16777216;
		Colours[7] = -14691931;
		Colours[8] = -12944427;
		Colours[9] = -5215504;
		BackgroundMode = 1;
		BackgroundAngle = 0.0;
		BackgroundMiddleAt = 50.0;
		ArtOpacity = 45.0;
		ArtBlur = 60.0;
		AuroraSpeed = 30.0;
		AuroraIntensity = 55.0;
		AuroraScale = 100.0;
		Bloom = true;
		BloomStrength = 45.0;
		BloomThreshold = 35.0;
		BloomRadius = 55.0;
		TrailMs = 180.0;
		ShadowOpacity = 55.0;
		ShadowBlur = 8.0;
		ShadowDistance = 3.0;
		ShadowAngle = 90.0;
		Vignette = 25.0;
		HueSpeed = 12.0;
		ArtColoursAmount = 100.0;
		TransitionMs = 700.0;
		Dither = true;
	}

	public Look Clone()
	{
		Look look = (Look)MemberwiseClone();
		look.Colours = (int[])Colours.Clone();
		return look;
	}
}
