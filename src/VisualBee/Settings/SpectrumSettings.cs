using System;
using System.Collections.Generic;

namespace MusicBeePlugin;

internal static partial class SpectrumSettings
{
	public static LookStore Create()
	{
		Schema schema = new Schema();
		AddLayout(schema);
		AddBars(schema);
		AddWaveform(schema);
		AddPeaks(schema);
		CommonLook.AddBackground(schema);
		AddParticles(schema);
		CommonLook.AddEffects(schema);
		AddLabels(schema);
		AddColours(schema);
		AddAnalysis(schema);
		CommonLook.AddAdvanced(schema);
		return new LookStore("VisualBee Studio", "spectrum", schema, new SpectrumLook(), Presets(), Themes.All(), ApplyTheme)
		{
			SwatchOf = (Look l) => new int[5]
			{
				l.Colours[0],
				l.Colours[10],
				l.Colours[11],
				l.Colours[12],
				l.Colours[13]
			}
		};
	}

	private static bool Bars(Look l)
	{
		return ((SpectrumLook)l).Layout != 7 && ((SpectrumLook)l).Layout != 8;
	}

	private static bool CanReflect(Look l)
	{
		int layout = ((SpectrumLook)l).Layout;
		return (layout == 0 || layout == 2 || layout == 3 || layout == 5) && !((SpectrumLook)l).Flip;
	}

	private static void AddLayout(Schema s)
	{
		s.Page("Studio").Section("Arrangement");
		s.Choice("layout", "Layout", "How the bands are laid out. Centred grows the bars both ways from the middle; Mirrored repeats the spectrum on both halves; the Stereo layouts show the left and right channels separately; Radial arranges the bars round a circle; Wave draws one smooth curve; Oscilloscope shows the live waveform.", SpectrumLook.LayoutNames, SpectrumLook.LayoutTokens, (SpectrumLook l) => l.Layout, (SpectrumLook l, int v) =>
		{
			l.Layout = v;
		}).Words("centered mirror split stereo circle radial wave curve scope");
		s.Integer("bands", "Bar count", "How many bars across the spectrum. More bands show finer frequency detail.", 8, 512, 1, "", (SpectrumLook l) => l.Bands, (SpectrumLook l, int v) =>
		{
			l.Bands = v;
		}).When(Bars).Words("bars count resolution bands");
		s.Toggle("layout.flip", "Upside down", "Bars hang from the top instead of standing on the bottom.", (SpectrumLook l) => l.Flip, (SpectrumLook l, bool v) =>
		{
			l.Flip = v;
		}).When((Look l) => ((SpectrumLook)l).Layout == 0 || ((SpectrumLook)l).Layout == 7 || ((SpectrumLook)l).Layout == 2 || ((SpectrumLook)l).Layout == 3 || ((SpectrumLook)l).Layout == 5);
		s.Number("layout.margin", "Margin", "Empty space around the visualisation.", 0.0, 60.0, 1.0, 0, "px", (SpectrumLook l) => l.Margin, (SpectrumLook l, double v) =>
		{
			l.Margin = v;
		});
		s.Section("Radial");
		s.Number("radial.inner", "Inner circle", "Size of the empty circle the bars grow out from, as a share of the panel's smaller side.", 0.0, 80.0, 1.0, 0, "%", (SpectrumLook l) => l.RadialInner, (SpectrumLook l, double v) =>
		{
			l.RadialInner = v;
		}).When((Look l) => ((SpectrumLook)l).Layout == 6);
		s.Number("radial.turn", "Rotation", "Turns the circle; 0° puts the lowest notes at the top.", 0.0, 360.0, 5.0, 0, "°", (SpectrumLook l) => l.RadialTurn, (SpectrumLook l, double v) =>
		{
			l.RadialTurn = v;
		}).When((Look l) => ((SpectrumLook)l).Layout == 6);
		s.Toggle("radial.art", "Cover in the middle", "Shows the playing track's cover inside the circle, pulsing with the beat.", (SpectrumLook l) => l.RadialArt, (SpectrumLook l, bool v) =>
		{
			l.RadialArt = v;
		}).When((Look l) => ((SpectrumLook)l).Layout == 6 && ((SpectrumLook)l).RadialInner >= 10.0).Words("album artwork picture");
		s.Section("Symmetry & Horizon");
		s.Number("reflection.opacity", "Floor reflection", "A faded mirror image under the bars, as on a glossy floor. 0 turns it off.", 0.0, 100.0, 1.0, 0, "%", (SpectrumLook l) => l.ReflectionOpacity, (SpectrumLook l, double v) =>
		{
			l.ReflectionOpacity = v;
		}).When(CanReflect).Words("mirror floor gloss");
		s.Number("reflection.size", "Reflection depth", "How much of the panel's height the reflection takes.", 5.0, 50.0, 1.0, 0, "%", (SpectrumLook l) => l.ReflectionSize, (SpectrumLook l, double v) =>
		{
			l.ReflectionSize = v;
		}).When((Look l) => CanReflect(l) && ((SpectrumLook)l).ReflectionOpacity > 0.0);
		s.Toggle("reflection.horizon", "Wet lacquer horizon", "Sharp datum line and contact light pool where bars meet the wet lacquer floor.", (SpectrumLook l) => l.LacquerHorizon, (SpectrumLook l, bool v) =>
		{
			l.LacquerHorizon = v;
		}).When((Look l) => CanReflect(l) && ((SpectrumLook)l).ReflectionOpacity > 0.0);
		s.Toggle("layout.laser", "Center horizon laser", "Horizontal neon laser beam bisecting centered and stereo layouts.", (SpectrumLook l) => l.CenterLaser, (SpectrumLook l, bool v) =>
		{
			l.CenterLaser = v;
		}).When((Look l) => ((SpectrumLook)l).Layout == 1 || ((SpectrumLook)l).Layout == 4).Words("laser line center datum");
		s.Number("layout.laserIntensity", "Laser glow intensity", "Brightness and halo spread of the center laser line.", 0.0, 200.0, 5.0, 0, "%", (SpectrumLook l) => l.CenterLaserIntensity, (SpectrumLook l, double v) =>
		{
			l.CenterLaserIntensity = v;
		}).When((Look l) => (((SpectrumLook)l).Layout == 1 || ((SpectrumLook)l).Layout == 4) && ((SpectrumLook)l).CenterLaser);
	}

		private static bool HasSegments(Look l)
	{
		if (!Bars(l)) return false;
		SpectrumLook sl = (SpectrumLook)l;
		return sl.BarStyle == SpectrumLook.StyleLed || sl.BarStyle == SpectrumLook.StyleNixie || (sl.BarStyle == SpectrumLook.StyleGlass && sl.GlassFill != 0 && sl.GlassFill != 3);
	}

	private static bool HasMesh(Look l)
	{
		if (!Bars(l)) return false;
		SpectrumLook sl = (SpectrumLook)l;
		return sl.BarStyle == SpectrumLook.StyleNixie || (sl.BarStyle == SpectrumLook.StyleGlass && sl.GlassFill != 0);
	}

	private static void AddBars(Schema s)
	{
		s.Page("Studio").Section("Bar Shape");
		s.Choice("bars.style", "Style", "Glass Pillars & Tubes, Smooth Pills, LED-style segments, Solid Neo-Bars, or Peaks only.", SpectrumLook.StyleNames, SpectrumLook.StyleTokens, (SpectrumLook l) => l.BarStyle, (SpectrumLook l, int v) =>
		{
			l.BarStyle = v;
		}).When(Bars);
		s.Number("bars.width", "Bar width", "How much of each band's slot the bar fills; the rest is the gap between bars.", 10.0, 100.0, 1.0, 0, "%", (SpectrumLook l) => l.BarWidth, (SpectrumLook l, double v) =>
		{
			l.BarWidth = v;
		}).When(Bars).Words("gap spacing thickness");
		s.Number("bars.gap", "Smallest gap", "Bars always keep at least this much space between them.", 0.0, 12.0, 1.0, 0, "px", (SpectrumLook l) => l.BarGap, (SpectrumLook l, double v) =>
		{
			l.BarGap = v;
		}).When(Bars).Words("spacing");
		s.Number("bars.radius", "Corner rounding", "Rounds the bars' corners; large values make pill shapes.", 0.0, 24.0, 0.5, 1, "px", (SpectrumLook l) => l.Radius, (SpectrumLook l, double v) =>
		{
			l.Radius = v;
		}).When(Bars).Words("round radius");
		s.Number("bars.taper", "Treble taper", "Makes high-frequency bars progressively narrower for studio rack aesthetics.", 0.0, 80.0, 5.0, 0, "%", (SpectrumLook l) => l.TrebleTaper, (SpectrumLook l, double v) =>
		{
			l.TrebleTaper = v;
		}).When(Bars).Words("taper narrow treble");
		s.Toggle("bars.tipHighlight", "Highlight bar tips", "Adds a high-intensity accent sheen to the top edge of each bar.", (SpectrumLook l) => l.TipHighlight, (SpectrumLook l, bool v) =>
		{
			l.TipHighlight = v;
		}).When(Bars).Words("tip sheen accent");
		s.Number("bars.opacity", "Opacity", "How solid the bars are.", 5.0, 100.0, 1.0, 0, "%", (SpectrumLook l) => l.BarOpacity, (SpectrumLook l, double v) =>
		{
			l.BarOpacity = v;
		}).Words("transparency alpha");
		s.Toggle("bars.hdr", "HDR incandescence", "Overdrives loud bars (> 55%) into white-hot incandescence at their core.", (SpectrumLook l) => l.HdrIncandescence, (SpectrumLook l, bool v) =>
		{
			l.HdrIncandescence = v;
		}).When(Bars).Words("hdr heat glow white");
		s.Toggle("bars.snap", "Pixel-sharp bars", "Puts every bar edge on whole pixels: crisp edges without subpixel interpolation.", (SpectrumLook l) => l.Snap, (SpectrumLook l, bool v) =>
		{
			l.Snap = v;
		}).When(Bars);

		s.Section("Glass & Vacuum Tube Optics");
		s.Choice("glass.fill", "Interior fill", "Pure Crystal Glass (transparent rising bar), Mixed Plasma & VFD Rungs, Segmented Rungs, or Solid Plasma Core.", SpectrumLook.GlassFillNames, SpectrumLook.GlassFillTokens, (SpectrumLook l) => l.GlassFill, (SpectrumLook l, int v) =>
		{
			l.GlassFill = v;
		}).When((Look l) => Bars(l) && ((SpectrumLook)l).BarStyle == 0);
		s.Choice("glass.tube", "Outer glass tube", "Full-height stationary vacuum tube, dynamic capsule following bar height, or interior only.", SpectrumLook.TubeModeNames, SpectrumLook.TubeModeTokens, (SpectrumLook l) => l.GlassTubeMode, (SpectrumLook l, int v) =>
		{
			l.GlassTubeMode = v;
		}).When((Look l) => Bars(l) && ((SpectrumLook)l).BarStyle == 0);
		s.Number("glass.refraction", "Lens refraction & prism", "Snell's Law lens distortion and Cauchy RGB chromatic dispersion through the curved glass.", 0.0, 150.0, 5.0, 0, "%", (SpectrumLook l) => l.GlassRefraction, (SpectrumLook l, double v) =>
		{
			l.GlassRefraction = v;
		}).When((Look l) => Bars(l) && ((SpectrumLook)l).BarStyle == 0);
		s.Number("glass.wall", "Glass wall thickness", "Thickness of the outer borosilicate cylinder wall and dome meniscus.", 5.0, 50.0, 1.0, 0, "%", (SpectrumLook l) => l.GlassWall, (SpectrumLook l, double v) =>
		{
			l.GlassWall = v;
		}).When((Look l) => Bars(l) && ((SpectrumLook)l).BarStyle == 0);
		s.Number("glass.tint", "Body clarity & frost tint", "0% is water-clear crystal see-through glass; higher values add body tint or frosting.", 0.0, 100.0, 5.0, 0, "%", (SpectrumLook l) => l.GlassTint, (SpectrumLook l, double v) =>
		{
			l.GlassTint = v;
		}).When((Look l) => Bars(l) && ((SpectrumLook)l).BarStyle == 0);
		s.Number("glass.specular", "Specular sheen", "Brightness of the twin studio softbox vertical highlights and dome reflection.", 0.0, 150.0, 5.0, 0, "%", (SpectrumLook l) => l.GlassSpecular, (SpectrumLook l, double v) =>
		{
			l.GlassSpecular = v;
		}).When((Look l) => Bars(l) && ((SpectrumLook)l).BarStyle == 0);
		s.Number("glass.caustics", "Fiber-optic caustics", "Internal audio-reactive light piping up the cylinder walls into the top dome.", 0.0, 150.0, 5.0, 0, "%", (SpectrumLook l) => l.GlassCaustics, (SpectrumLook l, double v) =>
		{
			l.GlassCaustics = v;
		}).When((Look l) => Bars(l) && ((SpectrumLook)l).BarStyle == 0);
		s.Number("glass.opacity", "Outer tube opacity", "Opacity of the outer vacuum tube shell and dome outline.", 0.0, 100.0, 5.0, 0, "%", (SpectrumLook l) => l.GlassOpacity, (SpectrumLook l, double v) =>
		{
			l.GlassOpacity = v;
		}).When((Look l) => Bars(l) && ((SpectrumLook)l).BarStyle == 0 && ((SpectrumLook)l).GlassTubeMode != 2);
		s.Toggle("glass.mesh", "Cathode wire mesh", "Fine hexagonal cathode wire mesh over the VFD phosphor rungs.", (SpectrumLook l) => l.VfdMesh, (SpectrumLook l, bool v) =>
		{
			l.VfdMesh = v;
		}).When(HasMesh);

		s.Section("LED / VFD segments");
		s.Number("led.size", "Segment height", "Height of each lit segment.", 1.0, 40.0, 0.5, 1, "px", (SpectrumLook l) => l.LedSize, (SpectrumLook l, double v) =>
		{
			l.LedSize = v;
		}).When(HasSegments);
		s.Number("led.gap", "Segment gap", "Dark space between segments.", 0.5, 20.0, 0.5, 1, "px", (SpectrumLook l) => l.LedGap, (SpectrumLook l, double v) =>
		{
			l.LedGap = v;
		}).When(HasSegments);
		s.Number("led.ghost", "Unlit segments", "Visibility of unlit segments above the active bar, like inactive VFD filaments.", 0.0, 60.0, 1.0, 0, "%", (SpectrumLook l) => l.LedGhost, (SpectrumLook l, double v) =>
		{
			l.LedGhost = v;
		}).When(HasSegments);
		s.Number("led.ghostRange", "Ghost trail height", "How far unlit segments extend above the bar; 100% fills to the ceiling.", 0.0, 100.0, 5.0, 0, "%", (SpectrumLook l) => l.GhostRange, (SpectrumLook l, double v) =>
		{
			l.GhostRange = v;
		}).When(HasSegments);
	}

	private static void AddWaveform(Schema s)
	{
		s.Page("Studio").Section("Waveform & Oscilloscope");
		s.Choice("wave.style", "Wave style", "Liquid Neon Ribbon (gradient ribbon with luminous core) or CRT Electron Beam (phosphor graticule).", SpectrumLook.ScopeStyleNames, SpectrumLook.ScopeStyleTokens, (SpectrumLook l) => l.ScopeStyle, (SpectrumLook l, int v) =>
		{
			l.ScopeStyle = v;
		}).When((Look l) => ((SpectrumLook)l).Layout == 7 || ((SpectrumLook)l).Layout == 8);
		s.Toggle("wave.normalize", "Auto-normalize wave (AGC)", "Automatically normalizes quiet and loud tracks to fill the target scope height cleanly.", (SpectrumLook l) => l.WaveNormalize, (SpectrumLook l, bool v) =>
		{
			l.WaveNormalize = v;
		}).When((Look l) => ((SpectrumLook)l).Layout == 7 || ((SpectrumLook)l).Layout == 8).Words("agc auto gain normalize");
		s.Number("wave.gain", "Wave gain", "Amplitude multiplier for the waveform.", 20.0, 400.0, 5.0, 0, "%", (SpectrumLook l) => l.WaveGain, (SpectrumLook l, double v) =>
		{
			l.WaveGain = v;
		}).When((Look l) => ((SpectrumLook)l).Layout == 7 || ((SpectrumLook)l).Layout == 8).Words("gain sensitivity");
		s.Number("wave.width", "Line width", "Thickness of the waveform line.", 0.5, 12.0, 0.5, 1, "px", (SpectrumLook l) => l.CurveWidth, (SpectrumLook l, double v) =>
		{
			l.CurveWidth = v;
		}).When((Look l) => ((SpectrumLook)l).Layout == 7 || ((SpectrumLook)l).Layout == 8);
		s.Number("wave.fill", "Luminous baseline fill", "Audio-reactive glow fill between the waveform and the center baseline.", 0.0, 100.0, 1.0, 0, "%", (SpectrumLook l) => l.CurveFill, (SpectrumLook l, double v) =>
		{
			l.CurveFill = v;
		}).When((Look l) => ((SpectrumLook)l).Layout == 7 || ((SpectrumLook)l).Layout == 8);
		s.Toggle("wave.reflection", "Wave floor reflection", "Soft mirror reflection of the waveform at the bottom of the panel.", (SpectrumLook l) => l.WaveFloorReflection, (SpectrumLook l, bool v) =>
		{
			l.WaveFloorReflection = v;
		}).When((Look l) => ((SpectrumLook)l).Layout == 7 || ((SpectrumLook)l).Layout == 8);
		s.Toggle("wave.graticule", "CRT graticule grid", "Hardware-style 10% step oscilloscope grid lines.", (SpectrumLook l) => l.ScopeGraticule, (SpectrumLook l, bool v) =>
		{
			l.ScopeGraticule = v;
		}).When((Look l) => ((SpectrumLook)l).Layout == 8);
	}
}