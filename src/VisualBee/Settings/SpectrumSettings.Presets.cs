using System.Globalization;
using System;
using System.Collections.Generic;

namespace MusicBeePlugin;

internal static partial class SpectrumSettings
{
	private static void AddLabels(Schema s)
	{
		s.Page("Atmosphere & FX").Section("Scale & Grid");
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

	private static string Hex(int argb)
	{
		return ColourMath.ToHex(argb, alpha: true);
	}

	public static Preset Foobar(int background, int text, int highlight, int bands, bool fill)
	{
		return new Preset("foobar2000 classic", "The spectrum as foobar2000 draws it, exactly: its analysis, band layout, colours and peaks.", "engine", "foobar", "bands", bands.ToString(CultureInfo.InvariantCulture), "fill", fill ? "1" : "0", "tilt", "0", "floorDb", "-60", "ceilingDb", "6", "attackMs", "0", "releaseMs", "0", "layout", "bars", "layout.margin", "0", "reflection.opacity", "0", "bars.style", "solid", "bars.width", "100", "bars.gap", "1", "bars.radius", "0", "bars.snap", "1", "colours.mode", "plot", "colours.useMiddle", "0", "colour.low", Hex(text), "colour.high", Hex(highlight), "colour.peak", Hex(ColourMath.Blend(background, text, 0.4)), "colour.labels", Hex(text), "colour.grid", Hex(ColourMath.Blend(background, text, 0.2)), "peaks", "1", "peaks.style", "line", "peaks.size", "1", "peaks.holdMs", "333", "peaks.fall", "constant", "peaks.speed", "30", "grid.db", "1", "labels.db", "1", "labels.freq", "1", "labels.size", "100", "bg.mode", "solid", "bg.colour", Hex(background), "fx.bloom", "0", "fx.vignette", "0", "fx.dither", "0");
	}

	private static List<Preset> Presets()
	{
		List<Preset> list = new List<Preset>();
		// 1. Signature VFD Studio Glass (Default)
		list.Add(new Preset("VFD Studio Glass", "Borosilicate glass capsules, ghosted cyan VFD filaments, HDR core burn, and wet lacquer reflection.",
			"bars.style", "glass", "glass.fill", "mixed", "glass.tube", "full", "glass.refraction", "75", "glass.wall", "18", "glass.tint", "20",
			"glass.specular", "90", "glass.caustics", "75", "led.ghostRange", "35",
			"bars.width", "76", "bars.radius", "4", "colours.mode", "bar",
			"colour.low", "#0078F0", "colour.mid", "#00F0DC", "colour.high", "#FFFFFF", "colour.peak", "#D2FFF5",
			"colours.useMiddle", "1", "colours.middleAt", "55", "bg.mode", "solid", "bg.colour", "#040810",
			"colour.grid", "#2400F0DC", "grid.db", "1", "reflection.opacity", "32", "reflection.size", "24",
			"reflection.horizon", "1", "fx.bloom", "1", "fx.bloomStrength", "38", "fx.bloomThreshold", "50",
			"bars.hdr", "1", "glass.mesh", "1", "peaks", "1", "peaks.style", "line", "peaks.size", "2.5", "peaks.barColour", "1"));

		// 2. VFD Nixie Rungs
		list.Add(new Preset("VFD Nixie Rungs", "Classic warm amber Nixie & VFD ladder rungs with unlit filaments, cathode mesh, and lacquer reflection.",
			"bars.style", "nixie", "bars.width", "78", "bars.radius", "1", "colours.mode", "bar",
			"colour.low", "#FF5000", "colour.mid", "#FF8800", "colour.high", "#FFCC33", "colour.peak", "#FFE8B0",
			"led.size", "3.5", "led.gap", "1.5", "led.ghost", "22", "led.ghostRange", "50",
			"bars.hdr", "1", "glass.mesh", "1", "bg.mode", "solid", "bg.colour", "#080605",
			"colour.grid", "#1A884400", "grid.db", "1", "reflection.opacity", "28", "reflection.horizon", "1",
			"fx.bloom", "1", "fx.bloomStrength", "50", "fx.bloomThreshold", "35",
			"peaks", "1", "peaks.style", "line", "peaks.size", "2.0", "peaks.barColour", "1"));

		// 3. Pure Crystal Glass
		list.Add(new Preset("Pure Crystal Glass", "Water-clear see-through glass bars with physical cylindrical lens refraction and edge fiber caustics.",
			"bars.style", "glass", "glass.fill", "crystal", "glass.tube", "off", "glass.refraction", "90", "glass.wall", "20", "glass.tint", "10",
			"glass.specular", "110", "glass.caustics", "85", "bars.width", "78", "bars.radius", "6", "colours.mode", "bar",
			"colour.low", "#00B4D8", "colour.mid", "#90E0EF", "colour.high", "#CAF0F8", "colour.peak", "#FFFFFF",
			"bg.mode", "solid", "bg.colour", "#05070B", "reflection.opacity", "30", "reflection.horizon", "1",
			"fx.bloom", "1", "fx.bloomStrength", "55", "fx.bloomThreshold", "30"));

		// 3. McIntosh Studio Glass
		list.Add(new Preset("McIntosh Studio Glass", "Deep oceanic blue and teal glass rack with warm lacquer reflections.",
			"bars.style", "glass", "glass.fill", "mixed", "glass.tube", "full", "glass.refraction", "85", "glass.wall", "22",
			"bars.width", "74", "bars.radius", "3.5", "colours.mode", "plot",
			"colour.low", "#006EE6", "colour.mid", "#00E1B4", "colour.high", "#D2FFF5", "colour.peak", "#FFFFFF",
			"bg.mode", "solid", "bg.colour", "#080C12", "colour.grid", "#1A00E1B4", "grid.db", "1",
			"reflection.opacity", "35", "reflection.size", "25", "reflection.horizon", "1",
			"fx.bloom", "1", "fx.bloomStrength", "60", "fx.bloomThreshold", "28"));

		// 4. Aurora (Teal)
		list.Add(new Preset("Aurora (Teal)", "Ethereal teal and mint filaments over slowly drifting curtains of light.",
			"bars.style", "glass", "layout", "wave", "colours.mode", "frequency",
			"colour.low", "#14C0A6", "colour.mid", "#8CFAE1", "colour.high", "#DCF5FF", "colour.curve", "#DCF5FF",
			"wave.width", "3.0", "wave.fill", "40", "bg.mode", "aurora", "bg.colour", "#060C0E",
			"aurora.a", "#021C20", "aurora.b", "#14C0A6", "aurora.c", "#8CFAE1", "aurora.speed", "35",
			"fx.bloom", "1", "fx.bloomStrength", "65", "fx.bloomThreshold", "25", "grid.db", "0"));

		// 5. Cyberpunk (Neon)
		list.Add(new Preset("Cyberpunk (Neon)", "Center-Out stereo wings in high-voltage magenta and cyan over a 3D rolling cyber-grid.",
			"layout", "centred", "bars.style", "glass", "glass.fill", "crystal", "glass.tube", "full",
			"bg.cyberGrid", "1", "bg.cyberGridIntensity", "60", "layout.laser", "1", "layout.laserIntensity", "85",
			"bars.width", "72", "bars.radius", "2", "colours.mode", "bar",
			"colour.low", "#8C19EB", "colour.mid", "#FF1494", "colour.high", "#19F2FF",
			"colour.peak", "#FFFFFF", "bg.mode", "linear", "bg.colour", "#0A0812", "bg.end", "#18082A",
			"colour.grid", "#25FF1494", "fx.bloomStrength", "35", "fx.bloomThreshold", "50",
			"fx.scanlines", "25", "fx.vignette", "35", "reflection.opacity", "0", "peaks.style", "glow", "peaks.size", "2.5"));

		// 6. OLED Midnight
		list.Add(new Preset("OLED Midnight", "Pure pitch black background with floating sapphire pills and gravity peaks.",
			"bars.style", "pills", "bars.width", "70", "bars.radius", "12", "colours.mode", "plot",
			"colour.low", "#468CF5", "colour.mid", "#AAD2FF", "colour.high", "#FFFFFF", "colour.peak", "#FFFFFF",
			"bg.mode", "solid", "bg.colour", "#000000", "reflection.opacity", "0", "grid.db", "0",
			"peaks.style", "block", "peaks.size", "3", "peaks.fall", "gravity", "fx.bloomStrength", "50"));

		// 7. Teenage Engineering
		list.Add(new Preset("Teenage Engineering", "Matte slate studio hardware with crisp industrial orange LED rungs.",
			"bars.style", "led", "led.size", "3.5", "led.gap", "1.5", "led.ghost", "12",
			"bars.width", "78", "bars.radius", "0.5", "colours.mode", "single",
			"colour.low", "#F55519", "colour.mid", "#EBE1D2", "colour.high", "#FFFFFF", "colour.peak", "#FFFFFF",
			"bg.mode", "solid", "bg.colour", "#121214", "colour.grid", "#16FFFFFF", "grid.db", "1",
			"reflection.opacity", "0", "fx.bloom", "0", "fx.vignette", "0", "peaks.style", "block", "peaks.size", "2"));

		// 8. Phosphor Green
		list.Add(new Preset("Phosphor Green", "Vintage CRT terminal with glowing green phosphor rungs and afterglow trails.",
			"bars.style", "led", "led.size", "4", "led.gap", "2", "led.ghost", "15",
			"bars.width", "78", "bars.radius", "0", "colours.mode", "plot",
			"colour.low", "#109136", "colour.mid", "#34E86C", "colour.high", "#D7FFE1", "colour.peak", "#D7FFE1",
			"bg.mode", "solid", "bg.colour", "#050C06", "colour.grid", "#1E1F6B36",
			"fx.scanlines", "40", "fx.trails", "1", "fx.trailMs", "220", "fx.bloomStrength", "60",
			"reflection.opacity", "0", "peaks.style", "block", "peaks.size", "3"));

		// 9. Sunset Ember
		list.Add(new Preset("Sunset Ember", "Fiery crimson and gold pillars with embers rising from the peaks.",
			"bars.style", "glass", "glass.fill", "mixed", "bars.width", "75", "bars.radius", "3", "colours.mode", "bar",
			"colour.low", "#D92652", "colour.mid", "#FF8C1E", "colour.high", "#FFEB80", "colour.peak", "#FFF8D0",
			"bg.mode", "radial", "bg.colour", "#1E0610", "bg.end", "#060204",
			"particles", "1", "particles.emit", "flow", "particles.amount", "35", "particles.gravity", "-30",
			"particles.speed", "30", "particles.life", "1600", "particles.size", "2.5",
			"fx.bloomStrength", "75", "fx.bloomThreshold", "22", "reflection.opacity", "20", "reflection.size", "20"));

		// 10. Monochrome Slate
		list.Add(new Preset("Monochrome Slate", "Clean mastering slate grey gradient with full architectural dB and frequency grid.",
			"bars.style", "glass", "glass.fill", "crystal", "bars.width", "70", "bars.radius", "2", "colours.mode", "plot",
			"colour.low", "#5A6673", "colour.mid", "#C0CCD9", "colour.high", "#FFFFFF", "colour.peak", "#FFFFFF",
			"bg.mode", "solid", "bg.colour", "#0E0F12", "colour.grid", "#24C0CCD9", "grid.db", "1", "labels.db", "1",
			"labels.freq", "1", "reflection.opacity", "25", "reflection.size", "20", "fx.bloomStrength", "40"));

		// 11. MusicBee Skin (Dynamic)
		list.Add(new Preset("MusicBee Skin (Dynamic)", "Dynamically tinted from your active MusicBee skin theme colours.",
			"bars.style", "glass", "anim.artColours", "1", "anim.artAmount", "100", "fx.bloomStrength", "55",
			"reflection.opacity", "28", "grid.db", "1"));

		// 12. VisualBee Scope
		list.Add(new Preset("VisualBee Scope", "Phase-locked hardware oscilloscope with auto-normalized Liquid Neon Ribbon.",
			"layout", "scope", "wave.style", "neon", "wave.normalize", "1", "wave.gain", "100", "wave.fill", "30",
			"wave.reflection", "1", "colours.mode", "single",
			"colour.low", "#00FF88", "colour.mid", "#00FFCC", "colour.high", "#88FFFF", "colour.curve", "#00FF66",
			"wave.width", "2.8", "peaks", "0", "particles", "0",
			"bg.mode", "solid", "bg.colour", "#040A06", "fx.bloom", "1", "fx.bloomStrength", "75",
			"fx.bloomThreshold", "25", "fx.trails", "1", "fx.trailMs", "260", "fx.scanlines", "35",
			"fx.vignette", "40", "grid.db", "0"));

		// 13. Solid Neo-Bars (Classic Blocky)
		list.Add(new Preset("Solid Neo-Bars (Classic)", "Clean, sharp, blocky solid spectrum bars with outline and no glass tube.",
			"bars.style", "solid", "bars.width", "82", "bars.radius", "0", "bars.outline", "0",
			"colours.mode", "plot", "colour.low", "#00B4D8", "colour.mid", "#00F0DC", "colour.high", "#FFFFFF", "colour.peak", "#FFFFFF",
			"bg.mode", "solid", "bg.colour", "#080C10", "colour.grid", "#1A00F0DC", "grid.db", "1",
			"reflection.opacity", "25", "reflection.horizon", "1", "peaks.style", "line", "peaks.size", "2.0"));

		return list;
	}

	public static void Migrate(LookStore store, string file)
	{
		Dictionary<string, string> dictionary = LookStore.ReadPairs(file);
		if (dictionary == null)
		{
			store.Apply(store.BuiltIn[0]);
			return;
		}
		int bands = LookStore.Int(dictionary, "bands", 160);
		bool fill = LookStore.Int(dictionary, "fill", 1) != 0;
		int background = LookStore.Colour(dictionary, "background", -15327191);
		int text = LookStore.Colour(dictionary, "text", -5197923);
		int highlight = LookStore.Colour(dictionary, "highlight", -10187829);
		Preset preset = Foobar(background, text, highlight, bands, fill);
		preset.Name = "My foobar2000 look";
		preset.Description = "Your spectrum as it was before the update: foobar2000's analysis and look with your colours.";
		store.SavePreset(preset);
		store.Apply(store.BuiltIn[0]);
		Setting s = store.Schema.Find("sync.delayMs");
		store.Set(s, (double)Math.Max(0, Math.Min(500, LookStore.Int(dictionary, "delayMs", 0))));
		store.PresetName = store.BuiltIn[0].Name;
		store.Modified = false;
	}
}