using System;

namespace MusicBeePlugin;

internal static partial class SpectrumSettings
{
	private static void AddPeaks(Schema s)
	{
		s.Page("Studio").Section("Peaks");
		s.Toggle("peaks", "Show peaks", "A marker above each bar that stays at the highest recent level, then falls.", (SpectrumLook l) => l.Peaks, (SpectrumLook l, bool v) =>
		{
			l.Peaks = v;
		}).When(Bars).Words("caps hold");
		s.Choice("peaks.style", "Style", "A thin line, a solid block, a dot, or a line with a soft glow around it.", SpectrumLook.PeakStyleNames, SpectrumLook.PeakStyleTokens, (SpectrumLook l) => l.PeakStyle, (SpectrumLook l, int v) =>
		{
			l.PeakStyle = v;
		}).When((Look l) => Bars(l) && ((SpectrumLook)l).Peaks);
		s.Number("peaks.size", "Thickness", "Height of the peak marker (the diameter of a dot).", 1.0, 16.0, 0.5, 1, "px", (SpectrumLook l) => l.PeakSize, (SpectrumLook l, double v) =>
		{
			l.PeakSize = v;
		}).When((Look l) => Bars(l) && ((SpectrumLook)l).Peaks);
		s.Toggle("peaks.barColour", "Same colour as the bar", "Colours each peak like the top of its bar instead of the peak colour.", (SpectrumLook l) => l.PeakBarColour, (SpectrumLook l, bool v) =>
		{
			l.PeakBarColour = v;
		}).When((Look l) => Bars(l) && ((SpectrumLook)l).Peaks);
		s.Number("peaks.opacity", "Opacity", "How solid the peaks are.", 5.0, 100.0, 1.0, 0, "%", (SpectrumLook l) => l.PeakOpacity, (SpectrumLook l, double v) =>
		{
			l.PeakOpacity = v;
		}).When((Look l) => Bars(l) && ((SpectrumLook)l).Peaks);
		s.Number("peaks.holdMs", "Hold time", "How long a peak stays at its highest point before it starts to fall.", 0.0, 3000.0, 10.0, 0, "ms", (SpectrumLook l) => l.PeakHoldMs, (SpectrumLook l, double v) =>
		{
			l.PeakHoldMs = v;
		}).When((Look l) => Bars(l) && ((SpectrumLook)l).Peaks);
		s.Choice("peaks.fall", "Falling", "Steady falls at a constant speed; Gravity starts slowly and speeds up; Fade out stays put and fades away.", SpectrumLook.FallNames, SpectrumLook.FallTokens, (SpectrumLook l) => l.PeakFall, (SpectrumLook l, int v) =>
		{
			l.PeakFall = v;
		}).When((Look l) => Bars(l) && ((SpectrumLook)l).Peaks);
		s.Number("peaks.speed", "Fall speed", "How fast peaks fall (Steady) or fade (Fade out), in dB per second.", 1.0, 300.0, 1.0, 0, "dB/s", (SpectrumLook l) => l.PeakSpeed, (SpectrumLook l, double v) =>
		{
			l.PeakSpeed = v;
		}).When((Look l) => Bars(l) && ((SpectrumLook)l).Peaks && ((SpectrumLook)l).PeakFall != 1).Log();
		s.Number("peaks.gravity", "Gravity", "How quickly falling peaks speed up, in dB per second per second.", 5.0, 2000.0, 5.0, 0, "dB/s²", (SpectrumLook l) => l.PeakGravity, (SpectrumLook l, double v) =>
		{
			l.PeakGravity = v;
		}).When((Look l) => Bars(l) && ((SpectrumLook)l).Peaks && ((SpectrumLook)l).PeakFall == 1).Log();
	}

	private static void AddParticles(Schema s)
	{
		s.Page("Optics & FX").Section("Particles & Embers");
		s.Toggle("particles", "Particles", "Sparks of light that fly off the bars and glow in the bloom pass.", (SpectrumLook l) => l.Particles, (SpectrumLook l, bool v) =>
		{
			l.Particles = v;
		}).Words("sparks embers fireworks");
		s.Choice("particles.emit", "Released", "When particles appear: on rising peaks, on beats, or streaming continuously off loud bars.", SpectrumLook.EmitNames, SpectrumLook.EmitTokens, (SpectrumLook l) => l.Emit, (SpectrumLook l, int v) =>
		{
			l.Emit = v;
		}).When((Look l) => ((SpectrumLook)l).Particles);
		s.Number("particles.amount", "Amount", "How many particles are released.", 1.0, 100.0, 1.0, 0, "%", (SpectrumLook l) => l.ParticleAmount, (SpectrumLook l, double v) =>
		{
			l.ParticleAmount = v;
		}).When((Look l) => ((SpectrumLook)l).Particles);
		s.Number("particles.size", "Size", "Particle radius.", 0.5, 12.0, 0.5, 1, "px", (SpectrumLook l) => l.ParticleSize, (SpectrumLook l, double v) =>
		{
			l.ParticleSize = v;
		}).When((Look l) => ((SpectrumLook)l).Particles);
		s.Number("particles.speed", "Speed", "How fast particles shoot out.", 5.0, 200.0, 1.0, 0, "%", (SpectrumLook l) => l.ParticleSpeed, (SpectrumLook l, double v) =>
		{
			l.ParticleSpeed = v;
		}).When((Look l) => ((SpectrumLook)l).Particles);
		s.Number("particles.life", "Lifetime", "How long a particle lasts before it fades out.", 100.0, 4000.0, 50.0, 0, "ms", (SpectrumLook l) => l.ParticleLife, (SpectrumLook l, double v) =>
		{
			l.ParticleLife = v;
		}).When((Look l) => ((SpectrumLook)l).Particles).Log();
		s.Number("particles.gravity", "Gravity", "Pulls particles back down; negative values make them float up like embers.", -100.0, 100.0, 1.0, 0, "%", (SpectrumLook l) => l.ParticleGravity, (SpectrumLook l, double v) =>
		{
			l.ParticleGravity = v;
		}).When((Look l) => ((SpectrumLook)l).Particles);
		s.Toggle("particles.sparks", "Streaks", "Draws each particle stretched along its motion vector like a spark.", (SpectrumLook l) => l.ParticleSparks, (SpectrumLook l, bool v) =>
		{
			l.ParticleSparks = v;
		}).When((Look l) => ((SpectrumLook)l).Particles);
		s.Toggle("particles.barColour", "Bar colours", "Particles take the colour of the bar they came from; otherwise the particle colour.", (SpectrumLook l) => l.ParticleBarColour, (SpectrumLook l, bool v) =>
		{
			l.ParticleBarColour = v;
		}).When((Look l) => ((SpectrumLook)l).Particles);
	}

	private static void AddColours(Schema s)
	{
		s.Page("Colours").Section("Bars & Waves");
		s.Choice("colours.mode", "Colouring", "Over the height: gradient spans the whole panel; On each bar: each bar shows the full gradient; Across frequencies: lows take low colour, highs high colour; By level: bar colour shows loudness.", SpectrumLook.ColouringNames, SpectrumLook.ColouringTokens, (SpectrumLook l) => l.Colouring, (SpectrumLook l, int v) =>
		{
			l.Colouring = v;
		}).Words("gradient rainbow");
		s.Colour("colour.low", "Low", "The gradient's first colour: bottom of bars, or lowest frequencies.", 10);
		s.Toggle("colours.useMiddle", "Middle colour", "Adds a third colour to the gradient.", (SpectrumLook l) => l.UseMiddle, (SpectrumLook l, bool v) =>
		{
			l.UseMiddle = v;
		}).When((Look l) => ((SpectrumLook)l).Colouring != 4);
		s.Colour("colour.mid", "Middle", "The gradient's middle colour.", 11).When((Look l) => ((SpectrumLook)l).UseMiddle && ((SpectrumLook)l).Colouring != 4);
		s.Number("colours.middleAt", "Middle position", "Where along the gradient the middle colour sits.", 5.0, 95.0, 1.0, 0, "%", (SpectrumLook l) => l.MiddleAt, (SpectrumLook l, double v) =>
		{
			l.MiddleAt = v;
		}).When((Look l) => ((SpectrumLook)l).UseMiddle && ((SpectrumLook)l).Colouring != 4);
		s.Colour("colour.high", "High", "The gradient's last colour: tops of loud bars, or highest frequencies.", 12).When((Look l) => ((SpectrumLook)l).Colouring != 4);
		s.Colour("colour.curve", "Wave line", "Colour of the Wave layout's curve.", 15).When((Look l) => ((SpectrumLook)l).Layout == 7 || ((SpectrumLook)l).Layout == 8);
		s.Section("Peaks and particles");
		s.Colour("colour.peak", "Peaks", "Colour of the peak markers.", 13);
		s.Colour("colour.particle", "Particles", "Colour of particles that don't take their bar's colour.", 14);
		CommonLook.AddColours(s, labels: true, grid: true);
	}

	private static void AddAnalysis(Schema s)
	{
		s.Page("Audio").Section("Audio Engine");
		s.Choice("audio.source", "Audio capture hook", "Capture method. Auto uses direct BASS when MusicBee plays and falls back to WASAPI Loopback for Browser2 (YouTube Music) or system audio.", SpectrumLook.AudioSourceNames, SpectrumLook.AudioSourceTokens, (SpectrumLook l) => l.AudioSource, (SpectrumLook l, int v) =>
		{
			l.AudioSource = v;
		}).Words("audio source direct bass wasapi loopback browser youtube asio capture fallback");
		s.Choice("audio.hz", "Audio refresh rate", "Rate of FFT audio analysis. Sync matches the display refresh; 60–500 Hz sets an explicit cadence.", SpectrumLook.AnalysisHzNames, SpectrumLook.AnalysisHzTokens, (SpectrumLook l) => l.AnalysisHz, (SpectrumLook l, int v) =>
		{
			l.AnalysisHz = v;
		}).Words("audio hz refresh rate analysis");
		s.Number("audio.sensitivity", "Input sensitivity", "Global gain multiplier for audio reactivity; boost for quiet tracks.", 20.0, 300.0, 5.0, 0, "%", (SpectrumLook l) => l.Sensitivity, (SpectrumLook l, double v) =>
		{
			l.Sensitivity = v;
		}).Words("sensitivity gain volume");
		s.Choice("engine", "Analyser", "Gaussian (Smooth) provides analog-smooth Gaussian windowed analysis; Modern lets you choose frequency range, scale, window and resolution.", SpectrumLook.EngineNames, SpectrumLook.EngineTokens, (SpectrumLook l) => l.Engine, (SpectrumLook l, int v) =>
		{
			l.Engine = v;
		});
		s.Toggle("fill", "Fill the width", "Spreads usable bands over the whole width when using the Gaussian engine.", (SpectrumLook l) => l.FillWidth, (SpectrumLook l, bool v) =>
		{
			l.FillWidth = v;
		}).When((Look l) => ((SpectrumLook)l).Engine == 0);
		s.Section("Frequencies");
		s.Number("minHz", "Lowest frequency", "The first band starts here.", 10.0, 2000.0, 1.0, 0, "Hz", (SpectrumLook l) => l.MinHz, (SpectrumLook l, double v) =>
		{
			l.MinHz = v;
		}).When((Look l) => ((SpectrumLook)l).Engine == 1).Log();
		s.Number("maxHz", "Highest frequency", "The last band ends here.", 1000.0, 24000.0, 50.0, 0, "Hz", (SpectrumLook l) => l.MaxHz, (SpectrumLook l, double v) =>
		{
			l.MaxHz = v;
		}).When((Look l) => ((SpectrumLook)l).Engine == 1).Log();
		s.Choice("scale", "Frequency scale", "Logarithmic (octaves equal), Mel, Bark, or Linear.", SpectrumAnalyzer.ScaleNames, SpectrumAnalyzer.ScaleTokens, (SpectrumLook l) => l.Scale, (SpectrumLook l, int v) =>
		{
			l.Scale = v;
		}).When((Look l) => ((SpectrumLook)l).Engine == 1);
		s.Section("Resolution");
		s.Choice("fft", "Window length", "Number of audio samples per analysis window.", SpectrumAnalyzer.FftSizeNames, SpectrumAnalyzer.FftSizeTokens, (SpectrumLook l) => l.FftSize, (SpectrumLook l, int v) =>
		{
			l.FftSize = v;
		}).When((Look l) => ((SpectrumLook)l).Engine == 1).Words("fft size latency");
		s.Choice("window", "Window shape", "Sample window taper: Hann, Blackman-Harris, or Gaussian.", SpectrumAnalyzer.WindowNames, SpectrumAnalyzer.WindowTokens, (SpectrumLook l) => l.Window, (SpectrumLook l, int v) =>
		{
			l.Window = v;
		}).When((Look l) => ((SpectrumLook)l).Engine == 1);
		s.Choice("value", "Band level", "Peak (sharpest) or Average (energy-based).", SpectrumAnalyzer.ValueNames, SpectrumAnalyzer.ValueTokens, (SpectrumLook l) => l.Value, (SpectrumLook l, int v) =>
		{
			l.Value = v;
		}).When((Look l) => ((SpectrumLook)l).Engine == 1);
		s.Choice("channels", "Channels", "All channels mixed, Left only, Right only, Mid or Side.", SpectrumAnalyzer.ChannelNames, SpectrumAnalyzer.ChannelTokens, (SpectrumLook l) => l.Channels, (SpectrumLook l, int v) =>
		{
			l.Channels = v;
		}).When((Look l) => !((SpectrumLook)l).Stereo);
		s.Section("Levels & Ballistics");
		s.Number("floorDb", "Bottom of the scale", "Levels at or below this are an empty bar.", -140.0, -20.0, 1.0, 0, "dB", (SpectrumLook l) => l.FloorDb, (SpectrumLook l, double v) =>
		{
			l.FloorDb = v;
		}).Words("range sensitivity");
		s.Number("ceilingDb", "Top of the scale", "Levels at or above this fill the bar.", -40.0, 24.0, 1.0, 0, "dB", (SpectrumLook l) => l.CeilingDb, (SpectrumLook l, double v) =>
		{
			l.CeilingDb = v;
		}).Words("range gain sensitivity");
		s.Number("tilt", "Treble boost", "Raises highs per octave above 1 kHz. +3 dB makes typical music look balanced.", -6.0, 9.0, 0.5, 1, "dB/oct", (SpectrumLook l) => l.Tilt, (SpectrumLook l, double v) =>
		{
			l.Tilt = v;
		}).Words("slope tilt pink");
		s.Number("attackMs", "Rise time", "How quickly bars jump up. 0 is instant.", 0.0, 300.0, 1.0, 0, "ms", (SpectrumLook l) => l.AttackMs, (SpectrumLook l, double v) =>
		{
			l.AttackMs = v;
		}).Words("attack smoothing");
		s.Number("releaseMs", "Fall time", "How slowly bars sink back. 0 drops at once.", 0.0, 2000.0, 5.0, 0, "ms", (SpectrumLook l) => l.ReleaseMs, (SpectrumLook l, double v) =>
		{
			l.ReleaseMs = v;
		}).Words("decay release smoothing");
	}
}