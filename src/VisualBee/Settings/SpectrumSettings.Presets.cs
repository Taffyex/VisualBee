using System.Globalization;
using System;
using System.Collections.Generic;

namespace MusicBeePlugin;

internal static partial class SpectrumSettings
{
	private static void AddLabels(Schema s)
	{
		s.Page("Optics & FX").Section("Scale & Graticule");
		s.Toggle("grid.db", "Level lines", "Faint lines every 10 dB.", (SpectrumLook l) => l.DbGrid, (SpectrumLook l, bool v) =>
		{
			l.DbGrid = v;
		}).Words("grid");
		s.Toggle("labels.db", "Level labels", "dB values beside the level lines, on the right.", (SpectrumLook l) => l.DbLabels, (SpectrumLook l, bool v) =>
		{
			l.DbLabels = v;
		}).Words("scale");
		s.Toggle("labels.freq", "Frequency labels", "Frequencies under the bars where there is room.", (SpectrumLook l) => l.FreqLabels, (SpectrumLook l, bool v) =>
		{
			l.FreqLabels = v;
		}).Words("hz scale");
		s.Number("labels.size", "Label size", "Size of the labels' text; 100 % is the standard metering size.", 50.0, 250.0, 5.0, 0, "%", (SpectrumLook l) => l.LabelSize, (SpectrumLook l, double v) =>
		{
			l.LabelSize = v;
		}).When((Look l) => ((SpectrumLook)l).DbLabels || ((SpectrumLook)l).FreqLabels).Words("font text");
	}

	private static void ApplyTheme(Look look, Theme t)
	{
		CommonLook.ApplyTheme(look, t);
		look.Colours[10] = t.Low;
		look.Colours[11] = t.Mid;
		look.Colours[12] = t.High;
		look.Colours[13] = t.Peak;
		look.Colours[14] = t.Accent;
		look.Colours[15] = t.High;
	}

	private static List<Preset> Presets()
	{
		List<Preset> list = new List<Preset>();

		// 1. Borosilicate VFD (Default)
		list.Add(new Preset("Borosilicate VFD", "Vacuum tube glass pillars with Snell lens refraction, Cauchy prism dispersion, and cyan filaments.",
			"bars.style", "glass", "glass.fill", "crystal", "glass.tube", "full", "glass.refraction", "75", "glass.wall", "18", "glass.tint", "20",
			"glass.specular", "90", "glass.caustics", "75", "led.ghostRange", "35",
			"bars.width", "76", "bars.radius", "4", "colours.mode", "bar",
			"colour.low", "#0078F0", "colour.mid", "#00F0DC", "colour.high", "#FFFFFF", "colour.peak", "#D2FFF5",
			"colours.useMiddle", "1", "colours.middleAt", "55", "bg.mode", "solid", "bg.colour", "#040810",
			"colour.grid", "#2400F0DC", "grid.db", "1", "reflection.opacity", "32", "reflection.size", "24",
			"reflection.horizon", "1", "fx.bloom", "1", "fx.bloomStrength", "38", "fx.bloomThreshold", "50",
			"bars.hdr", "1", "glass.mesh", "1", "peaks", "1", "peaks.style", "line", "peaks.size", "2.5", "peaks.barColour", "1"));

		// 2. Nixie Amber Tube
		list.Add(new Preset("Nixie Amber Tube", "Warm gas-discharge amber ladder rungs with twin metallic anode guide wires, cathode mesh, and lacquer reflection.",
			"bars.style", "nixie", "bars.width", "78", "bars.radius", "1", "colours.mode", "bar",
			"colour.low", "#FF5000", "colour.mid", "#FF8800", "colour.high", "#FFCC33", "colour.peak", "#FFE8B0",
			"led.size", "3.5", "led.gap", "1.5", "led.ghost", "22", "led.ghostRange", "50",
			"bars.hdr", "1", "glass.mesh", "1", "bg.mode", "solid", "bg.colour", "#080605",
			"colour.grid", "#1A884400", "grid.db", "1", "reflection.opacity", "28", "reflection.horizon", "1",
			"fx.bloom", "1", "fx.bloomStrength", "50", "fx.bloomThreshold", "35",
			"peaks", "1", "peaks.style", "line", "peaks.size", "2.0", "peaks.barColour", "1"));

		// 3. Quartz Prism
		list.Add(new Preset("Quartz Prism", "Water-clear cylindrical crystal lenses with high Snell refraction, prism caustics, and twin studio softbox speculars.",
			"bars.style", "glass", "glass.fill", "crystal", "glass.tube", "off", "glass.refraction", "90", "glass.wall", "20", "glass.tint", "10",
			"glass.specular", "110", "glass.caustics", "85", "bars.width", "78", "bars.radius", "6", "colours.mode", "bar",
			"colour.low", "#00B4D8", "colour.mid", "#90E0EF", "colour.high", "#CAF0F8", "colour.peak", "#FFFFFF",
			"bg.mode", "solid", "bg.colour", "#05070B", "reflection.opacity", "30", "reflection.horizon", "1",
			"fx.bloom", "1", "fx.bloomStrength", "55", "fx.bloomThreshold", "30"));

		// 4. Plasma Core Reactor
		list.Add(new Preset("Plasma Core Reactor", "Floating liquid plasma columns suspended inside borosilicate tubes with deep violet/cyan bloom.",
			"bars.style", "glass", "glass.fill", "plasma", "glass.tube", "full", "glass.refraction", "65", "glass.wall", "20",
			"bars.width", "72", "bars.radius", "5", "colours.mode", "bar",
			"colour.low", "#7928CA", "colour.mid", "#0070F3", "colour.high", "#00DFD8", "colour.peak", "#FFFFFF",
			"colours.useMiddle", "1", "colours.middleAt", "50", "bg.mode", "solid", "bg.colour", "#07040D",
			"reflection.opacity", "35", "reflection.horizon", "1", "fx.bloom", "1", "fx.bloomStrength", "60", "fx.bloomThreshold", "25"));

		// 5. Blue Meter Reference
		list.Add(new Preset("Blue Meter Reference", "Classic studio power-amp sapphire & teal solid rack with precision dB graticule.",
			"bars.style", "solid", "bars.width", "82", "bars.radius", "2", "colours.mode", "plot",
			"colour.low", "#0055D4", "colour.mid", "#00A8E8", "colour.high", "#00E8D6", "colour.peak", "#D2FFF5",
			"bg.mode", "solid", "bg.colour", "#060A10", "colour.grid", "#2400A8E8", "grid.db", "1", "labels.db", "1",
			"reflection.opacity", "25", "reflection.horizon", "1", "peaks", "1", "peaks.style", "line", "peaks.size", "2.0"));

		// 6. Outrun Horizon
		list.Add(new Preset("Outrun Horizon", "Center-out stereo wings over a bass-reactive 3D perspective grid and neon horizon laser.",
			"layout", "stereo-sides", "bars.style", "glass", "glass.fill", "crystal", "glass.tube", "off",
			"bars.width", "70", "bars.radius", "4", "colours.mode", "frequency",
			"colour.low", "#FF007F", "colour.mid", "#9B00E8", "colour.high", "#00F0FF", "colour.peak", "#FFFFFF",
			"layout.laser", "1", "layout.laserIntensity", "120", "bg.mode", "solid", "bg.colour", "#0A0314",
			"reflection.opacity", "30", "reflection.horizon", "1", "fx.bloom", "1", "fx.bloomStrength", "65", "fx.bloomThreshold", "30"));

		// 7. Obsidian Capsules
		list.Add(new Preset("Obsidian Capsules", "Pitch-black OLED backdrop with floating indigo-violet pills and gravity peak blocks.",
			"bars.style", "pills", "bars.width", "68", "colours.mode", "bar",
			"colour.low", "#3A0CA3", "colour.mid", "#7209B7", "colour.high", "#F72585", "colour.peak", "#4CC9F0",
			"bg.mode", "solid", "bg.colour", "#000000", "peaks", "1", "peaks.style", "gravity", "peaks.size", "4.0",
			"reflection.opacity", "0", "fx.bloom", "1", "fx.bloomStrength", "45", "fx.bloomThreshold", "40"));

		// 8. Dieter Studio LED
		list.Add(new Preset("Dieter Studio LED", "Matte industrial graphite hardware rack with crisp orange and warm-white segmented LEDs.",
			"bars.style", "led", "bars.width", "80", "bars.radius", "1", "led.size", "3.0", "led.gap", "1.5",
			"led.ghost", "15", "led.ghostRange", "100", "colours.mode", "bar",
			"colour.low", "#D05A00", "colour.mid", "#FF8800", "colour.high", "#FFE0A0", "colour.peak", "#FFFFFF",
			"bg.mode", "solid", "bg.colour", "#121214", "grid.db", "1", "labels.db", "1",
			"reflection.opacity", "10", "reflection.horizon", "0", "peaks", "1", "peaks.style", "line", "peaks.size", "2.0"));

		// 9. Tektronix P31 Scope
		list.Add(new Preset("Tektronix P31 Scope", "Phase-locked CRT electron beam oscilloscope with inverse slew-rate brightness and 10% graticule.",
			"layout", "scope", "wave.style", "crt", "wave.width", "2.0", "wave.graticule", "1", "wave.normalize", "1",
			"colour.low", "#00E040", "colour.mid", "#33FF66", "colour.high", "#88FFAA", "colour.curve", "#88FFAA",
			"bg.mode", "solid", "bg.colour", "#020803", "colour.grid", "#1A00E040",
			"fx.bloom", "1", "fx.bloomStrength", "50", "fx.bloomThreshold", "25", "grid.db", "0"));

		// 10. Liquid Neon Ribbon
		list.Add(new Preset("Liquid Neon Ribbon", "Auto-normalized smooth spline waveform with luminous baseline fill and mirror floor reflection.",
			"layout", "wave", "wave.style", "neon", "wave.width", "3.0", "wave.fill", "45", "wave.normalize", "1", "wave.reflection", "1",
			"colour.low", "#00E8D6", "colour.mid", "#00B4D8", "colour.high", "#90E0EF", "colour.curve", "#00E8D6",
			"bg.mode", "solid", "bg.colour", "#030A10", "fx.bloom", "1", "fx.bloomStrength", "60", "fx.bloomThreshold", "30"));

		// 11. Mastering Console
		list.Add(new Preset("Mastering Console", "128-band precision dB and frequency analyzer with neutral titanium solid neo-bars and peak hold lines.",
			"bands", "128", "bars.style", "solid", "bars.width", "85", "bars.radius", "1", "bars.snap", "1",
			"colours.mode", "level", "colour.low", "#304050", "colour.mid", "#507090", "colour.high", "#80B0D0", "colour.peak", "#FFE060",
			"grid.db", "1", "labels.db", "1", "labels.freq", "1", "bg.mode", "solid", "bg.colour", "#0A0D12",
			"peaks", "1", "peaks.style", "line", "peaks.size", "2.0", "peaks.holdMs", "600", "reflection.opacity", "0"));

		// 12. Cover Art Chameleon
		list.Add(new Preset("Cover Art Chameleon", "Adaptive album-artwork colour extraction with blurred cover backdrop and glass pillars.",
			"bars.style", "glass", "glass.fill", "crystal", "glass.tube", "full", "glass.refraction", "70", "glass.wall", "18",
			"bars.width", "75", "bars.radius", "4", "colours.mode", "bar", "colours.art", "1",
			"bg.mode", "art", "bg.artBlur", "60", "bg.artDim", "70",
			"reflection.opacity", "30", "reflection.horizon", "1", "fx.bloom", "1", "fx.bloomStrength", "40", "fx.bloomThreshold", "45"));

		return list;
	}

	public static void Migrate(LookStore store, string file)
	{
		store.Apply(store.BuiltIn[0]);
		store.PresetName = store.BuiltIn[0].Name;
		store.Modified = false;
	}
}
