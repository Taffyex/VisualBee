using System;

namespace MusicBeePlugin;

internal static class CommonLook
{
	public static readonly string[] FrameCapNames = new string[7] { "Every display refresh", "240 fps", "165 fps", "144 fps", "120 fps", "60 fps", "30 fps" };

	public static readonly string[] FrameCapTokens = new string[7] { "display", "240", "165", "144", "120", "60", "30" };

	public static readonly int[] FrameCapRates = new int[7] { 0, 240, 165, 144, 120, 60, 30 };

	public static void AddBackground(Schema s)
	{
		s.Page("Optics & FX").Section("Background");
		s.Choice("bg.mode", "Background", "What fills the panel behind the visualisation. Aurora drifts slowly and brightens with the music; Album art shows the playing track's cover, blurred and dimmed.", BackgroundModes.Names, BackgroundModes.Tokens, (Look l) => l.BackgroundMode, (Look l, int v) =>
		{
			l.BackgroundMode = v;
		});
		s.Colour("bg.colour", "Colour", "The background colour; where the panel has a gradient, the colour it starts from (the top of a linear gradient, the centre of a radial glow). Also shows through dim album art.", 0);
		s.Colour("bg.end", "Gradient end", "Where the gradient ends: the bottom of a linear gradient, the edges of a radial glow.", 1).When((Look l) => l.BackgroundMode == 1 || l.BackgroundMode == 2);
		s.Toggle("bg.useMiddle", "Middle colour", "Adds a third colour part way along the gradient.", (Look l) => l.BackgroundUseMiddle, (Look l, bool v) =>
		{
			l.BackgroundUseMiddle = v;
		}).When((Look l) => l.BackgroundMode == 1 || l.BackgroundMode == 2);
		s.Colour("bg.middle", "Middle", "The gradient's middle colour.", 2).When((Look l) => l.BackgroundUseMiddle && (l.BackgroundMode == 1 || l.BackgroundMode == 2));
		s.Number("bg.middleAt", "Middle position", "How far along the gradient the middle colour sits.", 5.0, 95.0, 1.0, 0, "%", (Look l) => l.BackgroundMiddleAt, (Look l, double v) =>
		{
			l.BackgroundMiddleAt = v;
		}).When((Look l) => l.BackgroundUseMiddle && (l.BackgroundMode == 1 || l.BackgroundMode == 2));
		s.Number("bg.angle", "Gradient angle", "Direction of a linear gradient: 0° runs from the top down, 90° from left to right, 180° from the bottom up.", 0.0, 360.0, 5.0, 0, "°", (Look l) => l.BackgroundAngle, (Look l, double v) =>
		{
			l.BackgroundAngle = v;
		}).When((Look l) => l.BackgroundMode == 1);
		s.Section("3D Cyber-Grid");
		s.Toggle("bg.cyberGrid", "3D Cyber-Grid horizon", "Perspective cyber ground grid that rolls forward and reacts to bass pulses.", (Look l) => l.CyberGrid, (Look l, bool v) =>
		{
			l.CyberGrid = v;
		}).Words("cyberpunk grid horizon retro");
		s.Number("bg.cyberGridIntensity", "Grid luminance", "How bright the cyber grid lines glow.", 0.0, 100.0, 1.0, 0, "%", (Look l) => l.CyberGridIntensity, (Look l, double v) =>
		{
			l.CyberGridIntensity = v;
		}).When((Look l) => l.CyberGrid).Words("brightness glow");
		s.Number("bg.cyberGridSpeed", "Roll speed", "How fast the grid horizon moves towards you.", 0.0, 200.0, 5.0, 0, "%", (Look l) => l.CyberGridSpeed, (Look l, double v) =>
		{
			l.CyberGridSpeed = v;
		}).When((Look l) => l.CyberGrid).Words("speed motion");
		s.Section("Aurora");
		s.Colour("aurora.a", "Colour 1", "One of the aurora's three colours.", 7).When((Look l) => l.BackgroundMode == 3);
		s.Colour("aurora.b", "Colour 2", "One of the aurora's three colours.", 8).When((Look l) => l.BackgroundMode == 3);
		s.Colour("aurora.c", "Colour 3", "One of the aurora's three colours.", 9).When((Look l) => l.BackgroundMode == 3);
		s.Number("aurora.speed", "Drift speed", "How fast the curtains of light move.", 0.0, 200.0, 1.0, 0, "%", (Look l) => l.AuroraSpeed, (Look l, double v) =>
		{
			l.AuroraSpeed = v;
		}).When((Look l) => l.BackgroundMode == 3);
		s.Number("aurora.intensity", "Intensity", "How bright the aurora gets; louder music adds up to half again.", 0.0, 150.0, 1.0, 0, "%", (Look l) => l.AuroraIntensity, (Look l, double v) =>
		{
			l.AuroraIntensity = v;
		}).When((Look l) => l.BackgroundMode == 3);
		s.Number("aurora.scale", "Scale", "Size of the aurora's shapes: lower is broader.", 30.0, 300.0, 5.0, 0, "%", (Look l) => l.AuroraScale, (Look l, double v) =>
		{
			l.AuroraScale = v;
		}).When((Look l) => l.BackgroundMode == 3);
		s.Section("Album art");
		s.Number("art.opacity", "Art opacity", "How strongly the cover shows over the background colour. Keep it low so the visualisation stays readable.", 0.0, 100.0, 1.0, 0, "%", (Look l) => l.ArtOpacity, (Look l, double v) =>
		{
			l.ArtOpacity = v;
		}).When((Look l) => l.BackgroundMode == 4);
		s.Number("art.blur", "Art blur", "Softens the cover so it reads as colour and light rather than a picture.", 0.0, 100.0, 1.0, 0, "%", (Look l) => l.ArtBlur, (Look l, double v) =>
		{
			l.ArtBlur = v;
		}).When((Look l) => l.BackgroundMode == 4);
	}

	public static void AddColours(Schema s, bool labels, bool grid)
	{
		if (labels | grid)
		{
			s.Section("Text and grid");
		}
		if (labels)
		{
			s.Colour("colour.labels", "Labels", "Colour of the scale and channel labels.", 3);
		}
		if (grid)
		{
			s.Colour("colour.grid", "Grid lines", "Colour of the grid and scale lines; its alpha sets how faint they are.", 4, alpha: true);
		}
		s.Section("Animation");
		s.Toggle("anim.hue", "Cycle colours", "Turns every colour slowly around the colour wheel, keeping its brightness. Combine with the frequency colours of a rainbow preset for a moving rainbow.", (Look l) => l.HueCycle, (Look l, bool v) =>
		{
			l.HueCycle = v;
		}).Words("hue rotate rainbow cycle");
		s.Number("anim.hueSpeed", "Cycle speed", "Degrees around the colour wheel per second (360 is one full turn).", 1.0, 180.0, 1.0, 0, "°/s", (Look l) => l.HueSpeed, (Look l, double v) =>
		{
			l.HueSpeed = v;
		}).When((Look l) => l.HueCycle).Log();
		s.Toggle("anim.hueBackground", "Cycle the background too", "Also turns the background colours.", (Look l) => l.HueCycleBackground, (Look l, bool v) =>
		{
			l.HueCycleBackground = v;
		}).When((Look l) => l.HueCycle);
		s.Toggle("anim.artColours", "Colours from album art", "Takes the colours from the playing track's cover and glides to new ones at each track change.", (Look l) => l.ArtColours, (Look l, bool v) =>
		{
			l.ArtColours = v;
		}).Words("adaptive cover dominant");
		s.Number("anim.artAmount", "Album art influence", "How far the colours move from your own towards the cover's.", 0.0, 100.0, 1.0, 0, "%", (Look l) => l.ArtColoursAmount, (Look l, double v) =>
		{
			l.ArtColoursAmount = v;
		}).When((Look l) => l.ArtColours);
		s.Number("anim.transition", "Colour transitions", "How long colours take to glide to new ones when you pick a preset, a theme or a colour, or the album art changes. 0 switches at once.", 0.0, 3000.0, 50.0, 0, "ms", (Look l) => l.TransitionMs, (Look l, double v) =>
		{
			l.TransitionMs = v;
		}).Words("fade smooth switch");
	}

	public static void AddEffects(Schema s)
	{
		s.Page("Optics & FX").Section("Glow");
		s.Toggle("fx.bloom", "Glow", "Bright parts bleed light into their surroundings, as on a screen photographed at night. Costs almost nothing: it runs on the graphics card.", (Look l) => l.Bloom, (Look l, bool v) =>
		{
			l.Bloom = v;
		}).Words("bloom");
		s.Number("fx.bloomStrength", "Glow strength", "How much light the glow adds.", 0.0, 200.0, 1.0, 0, "%", (Look l) => l.BloomStrength, (Look l, double v) =>
		{
			l.BloomStrength = v;
		}).When((Look l) => l.Bloom).Words("bloom");
		s.Number("fx.bloomThreshold", "Glow threshold", "How bright something must be to glow. 0 makes everything glow; higher values keep the glow to the brightest highlights.", 0.0, 100.0, 1.0, 0, "%", (Look l) => l.BloomThreshold, (Look l, double v) =>
		{
			l.BloomThreshold = v;
		}).When((Look l) => l.Bloom).Words("bloom");
		s.Number("fx.bloomRadius", "Glow radius", "How far the glow spreads.", 0.0, 100.0, 1.0, 0, "%", (Look l) => l.BloomRadius, (Look l, double v) =>
		{
			l.BloomRadius = v;
		}).When((Look l) => l.Bloom).Words("bloom");
		s.Colour("fx.glowTint", "Glow tint", "Colours the glow; white keeps each element's own colour.", 5).When((Look l) => l.Bloom);
		s.Section("Afterglow");
		s.Toggle("fx.trails", "Afterglow", "Moving shapes leave a fading trail, like the phosphor of an old screen. Makes fast, thin movement easier to follow at high refresh rates; on something that slides sideways (a scrolling oscilloscope) a long afterglow smears it into a band.", (Look l) => l.Trails, (Look l, bool v) =>
		{
			l.Trails = v;
		}).Words("trails persistence motion blur phosphor");
		s.Number("fx.trailMs", "Afterglow length", "Time for a trail to fade to a tenth.", 20.0, 2000.0, 10.0, 0, "ms", (Look l) => l.TrailMs, (Look l, double v) =>
		{
			l.TrailMs = v;
		}).When((Look l) => l.Trails).Log()
			.Words("trails persistence");
		s.Section("Shadow");
		s.Toggle("fx.shadow", "Drop shadow", "A soft shadow under everything the visualisation draws, lifting it off the background.", (Look l) => l.Shadow, (Look l, bool v) =>
		{
			l.Shadow = v;
		});
		s.Number("fx.shadowOpacity", "Shadow opacity", "How dark the shadow is.", 0.0, 100.0, 1.0, 0, "%", (Look l) => l.ShadowOpacity, (Look l, double v) =>
		{
			l.ShadowOpacity = v;
		}).When((Look l) => l.Shadow);
		s.Number("fx.shadowBlur", "Shadow softness", "How far the shadow's edge is blurred.", 1.0, 64.0, 1.0, 0, "px", (Look l) => l.ShadowBlur, (Look l, double v) =>
		{
			l.ShadowBlur = v;
		}).When((Look l) => l.Shadow).Log();
		s.Number("fx.shadowDistance", "Shadow distance", "How far the shadow falls from what casts it.", 0.0, 40.0, 1.0, 0, "px", (Look l) => l.ShadowDistance, (Look l, double v) =>
		{
			l.ShadowDistance = v;
		}).When((Look l) => l.Shadow);
		s.Number("fx.shadowAngle", "Shadow direction", "Which way the shadow falls: 90° is straight down.", 0.0, 360.0, 5.0, 0, "°", (Look l) => l.ShadowAngle, (Look l, double v) =>
		{
			l.ShadowAngle = v;
		}).When((Look l) => l.Shadow);
		s.Colour("fx.shadowColour", "Shadow colour", "Usually black; a deep colour gives a coloured glow underneath instead.", 6).When((Look l) => l.Shadow);
		s.Section("Lens and screen");
		s.Number("fx.vignette", "Vignette", "Darkens the corners, drawing the eye to the middle, for a moodier, gloomier picture.", 0.0, 100.0, 1.0, 0, "%", (Look l) => l.Vignette, (Look l, double v) =>
		{
			l.Vignette = v;
		}).Words("gloom dark corners");
		s.Number("fx.scanlines", "Scanlines", "Darkens every other pixel row like an old CRT monitor.", 0.0, 100.0, 1.0, 0, "%", (Look l) => l.Scanlines, (Look l, double v) =>
		{
			l.Scanlines = v;
		}).Words("crt retro");
		s.Number("fx.aberration", "Colour fringing", "Splits red and blue apart towards the edges, like a cheap lens.", 0.0, 12.0, 0.5, 1, "px", (Look l) => l.Aberration, (Look l, double v) =>
		{
			l.Aberration = v;
		}).Words("chromatic aberration");
		s.Number("fx.grain", "Film grain", "Fine moving noise over the picture.", 0.0, 100.0, 1.0, 0, "%", (Look l) => l.Grain, (Look l, double v) =>
		{
			l.Grain = v;
		}).Words("noise");
		s.Number("fx.beatFlash", "Beat flash", "Lights up the whole panel briefly on each kick or snare, in the glow tint colour.", 0.0, 100.0, 1.0, 0, "%", (Look l) => l.BeatFlash, (Look l, double v) =>
		{
			l.BeatFlash = v;
		}).Words("pulse bass kick");
	}

	public static void AddAdvanced(Schema s)
	{
		s.Page("Audio").Section("Timing & Display");
		s.Integer("sync.delayMs", "Sync delay", "Shows the audio this much later, to line the picture up with what you hear (for example through Bluetooth headphones). Ctrl+scroll over the panel changes it too; add Shift for 1 ms steps.", 0, 500, 5, "ms", (Look l) => l.DelayMs, (Look l, int v) =>
		{
			l.DelayMs = v;
		}).Mine().Words("latency offset");
		s.Choice("perf.frameCap", "Frame rate", "At most this many frames a second. 'Every display refresh' is smoothest; a lower rate uses less power.", FrameCapNames, FrameCapTokens, (Look l) => l.FrameCap, (Look l, int v) =>
		{
			l.FrameCap = v;
		}).Mine().Words("fps refresh power");
		s.Toggle("perf.showFps", "Show frame rate", "Prints frames per second and the time each frame took in the corner.", (Look l) => l.ShowFps, (Look l, bool v) =>
		{
			l.ShowFps = v;
		}).Mine().Words("fps diagnostics");
		s.Section("Quality");
		s.Toggle("fx.dither", "Smooth gradients", "Adds invisible noise that hides banding in dark gradients and glows.", (Look l) => l.Dither, (Look l, bool v) =>
		{
			l.Dither = v;
		}).Words("dither banding");
	}

	public static void ApplyTheme(Look l, Theme t)
	{
		l.Colours[0] = t.BackTop;
		l.Colours[1] = t.BackBottom;
		l.Colours[2] = t.BackMiddle;
		l.Colours[3] = t.Text;
		l.Colours[4] = (t.Grid & 0xFFFFFF) | (l.Colours[4] & -16777216);
		l.Colours[7] = t.Low;
		l.Colours[8] = t.Mid;
		l.Colours[9] = t.High;
	}

	public static Effects Effects(Look l, Vec4[] colours, float beat)
	{
		Vec4 bloomTint = colours[5];
		double num = l.ShadowAngle * Math.PI / 180.0;
		float scale = PanelLayout.Scale;
		return new Effects
		{
			Bloom = l.Bloom,
			BloomStrength = (float)(l.BloomStrength / 100.0 * 2.0),
			BloomThreshold = (float)(l.BloomThreshold / 100.0),
			BloomRadius = (float)(l.BloomRadius / 100.0),
			BloomTint = bloomTint,
			Trails = l.Trails,
			TrailSeconds = (float)(l.TrailMs / 1000.0),
			Shadow = l.Shadow,
			ShadowOpacity = (float)(l.ShadowOpacity / 100.0),
			ShadowBlur = (float)(l.ShadowBlur * (double)scale),
			ShadowX = (float)(Math.Cos(num) * l.ShadowDistance * (double)scale),
			ShadowY = (float)(Math.Sin(num) * l.ShadowDistance * (double)scale),
			ShadowColour = colours[6],
			Vignette = (float)(l.Vignette / 100.0 * 0.85),
			VignetteStart = 0.3f,
			Scanlines = (float)(l.Scanlines / 100.0 * 0.6),
			Aberration = (float)(l.Aberration * (double)scale),
			Grain = (float)(l.Grain / 100.0),
			Dither = l.Dither,
			Flash = new Vec4(bloomTint.X, bloomTint.Y, bloomTint.Z, (float)(l.BeatFlash / 100.0 * 0.22 * (double)beat))
		};
	}
}