using System;
using System.Drawing;
using System.Globalization;
using System.IO;

namespace MusicBeePlugin
{
    public sealed partial class VisualBeeSettings
    {
        public static VisualBeeSettings Load(MusicBeeApiInterface api)
        {
            string path = null;
            try
            {
                if (api.Setting_GetPersistentStoragePath != null)
                {
                    string folder = api.Setting_GetPersistentStoragePath();
                    if (!string.IsNullOrEmpty(folder))
                    {
                        Directory.CreateDirectory(folder);
                        path = Path.Combine(folder, "mb_VisualBee.ini");
                    }
                }
            }
            catch { }

            var settings = new VisualBeeSettings(path);
            if (path == null || !File.Exists(path))
                return settings;

            try
            {
                bool hasExplicitDecay = false;
                int legacySmoothing = -1;

                foreach (string rawLine in File.ReadAllLines(path))
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith(";"))
                        continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1).Trim();

                    switch (key)
                    {
                        case "Mode":
                            if (int.TryParse(val, out int mode))
                                settings.Mode = (mode == 1) ? VisualizerMode.Waveform : VisualizerMode.SpectrumBars;
                            break;
                        case "StereoMode":
                            if (int.TryParse(val, out int sm) && Enum.IsDefined(typeof(StereoMode), sm))
                                settings.StereoMode = (StereoMode)sm;
                            break;
                        case "AudioSource":
                            if (int.TryParse(val, out int src) && Enum.IsDefined(typeof(AudioSourceMode), src))
                                settings.AudioSource = (AudioSourceMode)src;
                            break;
                        case "BarStyle":
                            if (int.TryParse(val, out int bStyle) && Enum.IsDefined(typeof(BarVisualEffect), bStyle))
                                settings.BarStyle = (BarVisualEffect)bStyle;
                            break;
                        case "Mirror":
                            if (int.TryParse(val, out int mir))
                                settings.Mirror = (BarMirror)(mir & 3);
                            break;
                        case "BarGap":
                            if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float gap))
                                settings.BarGap = Math.Max(0.0f, Math.Min(0.6f, gap));
                            break;
                        case "BarCount":
                            if (int.TryParse(val, out int bars) && bars >= 16 && bars <= 160)
                                settings.BarCount = bars;
                            break;
                        case "BarRoundness":
                            if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float roundness))
                                settings.BarRoundness = Math.Max(0.0f, Math.Min(1.0f, roundness));
                            break;
                        case "TrebleTaper":
                            if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float taper))
                                settings.TrebleTaper = Math.Max(0.0f, Math.Min(0.80f, taper));
                            break;
                        case "LedSegmentHeight":
                            if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float lsh))
                                settings.LedSegmentHeight = Math.Max(2.0f, Math.Min(24.0f, lsh));
                            break;
                        case "LedSegmentGap":
                            if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float lsg))
                                settings.LedSegmentGap = Math.Max(1.0f, Math.Min(8.0f, lsg));
                            break;
                        case "HighlightBarTips":
                            if (bool.TryParse(val, out bool hbt))
                                settings.HighlightBarTips = hbt;
                            break;
                        case "ColorMode":
                            if (int.TryParse(val, out int cMode) && Enum.IsDefined(typeof(BarColorMode), cMode))
                                settings.ColorMode = (BarColorMode)cMode;
                            break;
                        case "HueSpeed":
                            if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float hSpd))
                                settings.HueSpeed = Math.Max(0.01f, Math.Min(2.0f, hSpd));
                            break;
                        case "HueMotion":
                            if (int.TryParse(val, out int hMot) && Enum.IsDefined(typeof(HueCycleMotion), hMot))
                                settings.HueMotion = (HueCycleMotion)hMot;
                            break;
                        case "HueSource":
                            if (int.TryParse(val, out int hSrc) && Enum.IsDefined(typeof(HueCycleSource), hSrc))
                                settings.HueSource = (HueCycleSource)hSrc;
                            break;
                        case "PeakHoldEnabled":
                            if (bool.TryParse(val, out bool peak))
                                settings.PeakHoldEnabled = peak;
                            break;
                        case "PeakHoldMs":
                            if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float phm))
                                settings.PeakHoldMs = Math.Max(50f, Math.Min(2000f, phm));
                            break;
                        case "PeakGravity":
                            if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float pgrav))
                                settings.PeakGravity = Math.Max(0.5f, Math.Min(10.0f, pgrav));
                            break;
                        case "PeakCapThickness":
                            if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float pct))
                                settings.PeakCapThickness = Math.Max(1.0f, Math.Min(8.0f, pct));
                            break;
                        case "PeakCapColorMode":
                            if (int.TryParse(val, out int pccm) && Enum.IsDefined(typeof(PeakColorMode), pccm))
                                settings.PeakCapColorMode = (PeakColorMode)pccm;
                            break;
                        case "PeakCustomColor":
                            if (!string.IsNullOrEmpty(val))
                            {
                                try
                                {
                                    if (val.StartsWith("#") && val.Length == 7)
                                    {
                                        int r = Convert.ToInt32(val.Substring(1, 2), 16);
                                        int g = Convert.ToInt32(val.Substring(3, 2), 16);
                                        int b = Convert.ToInt32(val.Substring(5, 2), 16);
                                        settings.PeakCustomColor = Color.FromArgb(255, r, g, b);
                                    }
                                }
                                catch { }
                            }
                            break;
                        case "Sensitivity":
                            if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float sens))
                                settings.Sensitivity = Math.Max(0.1f, Math.Min(4.0f, sens));
                            break;
                        case "BeatSensitivity":
                            if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float beatSens))
                                settings.BeatSensitivity = Math.Max(0.1f, Math.Min(2.0f, beatSens));
                            break;
                        case "AttackMs":
                            if (int.TryParse(val, out int att))
                                settings.AttackMs = Math.Max(2, Math.Min(50, att));
                            break;
                        case "DecayMs":
                            if (int.TryParse(val, out int dec))
                            {
                                settings.DecayMs = Math.Max(10, Math.Min(300, dec));
                                hasExplicitDecay = true;
                            }
                            break;
                        case "SmoothingMs":
                            if (int.TryParse(val, out int smooth))
                                legacySmoothing = Math.Max(10, Math.Min(300, smooth));
                            break;
                        case "MinFreqHz":
                            if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float minF))
                                settings.MinFreqHz = Math.Max(20.0f, Math.Min(200.0f, minF));
                            break;
                        case "MaxFreqHz":
                            if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float maxF))
                                settings.MaxFreqHz = Math.Max(5000.0f, Math.Min(24000.0f, maxF));
                            break;
                        case "DbRange":
                            if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float dbR))
                                settings.DbRange = Math.Max(30.0f, Math.Min(90.0f, dbR));
                            break;
                        case "TrebleTiltDb":
                            if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float tilt))
                                settings.TrebleTiltDb = Math.Max(0.0f, Math.Min(24.0f, tilt));
                            break;
                        case "SyncOffsetMs":
                            if (int.TryParse(val, out int sync))
                                settings.SyncOffsetMs = Math.Max(-300, Math.Min(300, sync));
                            break;
                        case "RibbonThickness":
                            if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float th))
                                settings.RibbonThickness = Math.Max(0.5f, Math.Min(12.0f, th));
                            break;
                        case "RibbonFill":
                            if (bool.TryParse(val, out bool rf))
                                settings.RibbonFill = rf;
                            break;
                        case "RibbonFloorReflection":
                            if (bool.TryParse(val, out bool floor))
                                settings.RibbonFloorReflection = floor;
                            break;
                        case "BloomIntensity":
                            if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float bloom))
                                settings.BloomIntensity = Math.Max(0.0f, Math.Min(3.0f, bloom));
                            break;
                        case "BgSource":
                            if (int.TryParse(val, out int bgs) && Enum.IsDefined(typeof(BackgroundSource), bgs))
                                settings.BgSource = (BackgroundSource)bgs;
                            break;
                        case "BgBrightness":
                            if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float bgb))
                                settings.BgBrightness = Math.Max(0.0f, Math.Min(2.0f, bgb));
                            break;
                        case "Vignette":
                            if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float vig))
                                settings.Vignette = Math.Max(0.0f, Math.Min(1.0f, vig));
                            break;
                        case "FloorReflection":
                            if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float fr))
                                settings.FloorReflection = Math.Max(0.0f, Math.Min(1.0f, fr));
                            break;
                        case "ContactGlow":
                            if (bool.TryParse(val, out bool cg))
                                settings.ContactGlow = cg;
                            break;
                        case "FrameCap":
                            if (int.TryParse(val, out int cap))
                                settings.FrameCap = cap;
                            break;
                        case "AnalysisHz":
                            if (int.TryParse(val, out int aHz))
                                settings.AnalysisHz = (aHz == 0 || aHz == 60 || aHz == 120 || aHz == 240 || aHz == 500 || aHz == 1000) ? aHz : 120;
                            break;
                        case "IdleThrottle":
                            if (bool.TryParse(val, out bool idle))
                                settings.IdleThrottle = idle;
                            break;
                        case "Palette":
                            if (!string.IsNullOrEmpty(val))
                                settings.Palette = val;
                            break;
                        case "ShowDiagnostics":
                            if (bool.TryParse(val, out bool diag))
                                settings.ShowDiagnostics = diag;
                            break;
                    }
                }

                if (!hasExplicitDecay && legacySmoothing > 0)
                {
                    settings.DecayMs = legacySmoothing;
                    settings.AttackMs = Math.Max(2, (int)(legacySmoothing * 0.12f));
                }
            }
            catch { }
            return settings;
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(_path)) return;
            try
            {
                using (var writer = new StreamWriter(_path, false))
                {
                    writer.WriteLine("# VisualBee v1.0 configuration");
                    writer.WriteLine("Mode=" + (int)Mode);
                    writer.WriteLine("StereoMode=" + (int)StereoMode);
                    writer.WriteLine("AudioSource=" + (int)AudioSource);
                    writer.WriteLine("BarStyle=" + (int)BarStyle);
                    writer.WriteLine("Mirror=" + (int)Mirror);

                    writer.WriteLine("BarGap=" + BarGap.ToString("0.00", CultureInfo.InvariantCulture));
                    writer.WriteLine("BarCount=" + BarCount);
                    writer.WriteLine("BarRoundness=" + BarRoundness.ToString("0.00", CultureInfo.InvariantCulture));
                    writer.WriteLine("TrebleTaper=" + TrebleTaper.ToString("0.00", CultureInfo.InvariantCulture));
                    writer.WriteLine("LedSegmentHeight=" + LedSegmentHeight.ToString("0.0", CultureInfo.InvariantCulture));
                    writer.WriteLine("LedSegmentGap=" + LedSegmentGap.ToString("0.0", CultureInfo.InvariantCulture));
                    writer.WriteLine("HighlightBarTips=" + HighlightBarTips);

                    writer.WriteLine("Palette=" + (Palette ?? "skin"));
                    writer.WriteLine("ColorMode=" + (int)ColorMode);
                    writer.WriteLine("HueSpeed=" + HueSpeed.ToString("0.00", CultureInfo.InvariantCulture));
                    writer.WriteLine("HueMotion=" + (int)HueMotion);
                    writer.WriteLine("HueSource=" + (int)HueSource);

                    writer.WriteLine("PeakHoldEnabled=" + PeakHoldEnabled);
                    writer.WriteLine("PeakHoldMs=" + PeakHoldMs.ToString("0", CultureInfo.InvariantCulture));
                    writer.WriteLine("PeakGravity=" + PeakGravity.ToString("0.00", CultureInfo.InvariantCulture));
                    writer.WriteLine("PeakCapThickness=" + PeakCapThickness.ToString("0.0", CultureInfo.InvariantCulture));
                    writer.WriteLine("PeakCapColorMode=" + (int)PeakCapColorMode);
                    writer.WriteLine("PeakCustomColor=" + string.Format("#{0:X2}{1:X2}{2:X2}", PeakCustomColor.R, PeakCustomColor.G, PeakCustomColor.B));

                    writer.WriteLine("Sensitivity=" + Sensitivity.ToString("0.00", CultureInfo.InvariantCulture));
                    writer.WriteLine("BeatSensitivity=" + BeatSensitivity.ToString("0.00", CultureInfo.InvariantCulture));
                    writer.WriteLine("AttackMs=" + AttackMs);
                    writer.WriteLine("DecayMs=" + DecayMs);
                    writer.WriteLine("MinFreqHz=" + MinFreqHz.ToString("0.0", CultureInfo.InvariantCulture));
                    writer.WriteLine("MaxFreqHz=" + MaxFreqHz.ToString("0.0", CultureInfo.InvariantCulture));
                    writer.WriteLine("DbRange=" + DbRange.ToString("0.0", CultureInfo.InvariantCulture));
                    writer.WriteLine("TrebleTiltDb=" + TrebleTiltDb.ToString("0.0", CultureInfo.InvariantCulture));
                    writer.WriteLine("SyncOffsetMs=" + SyncOffsetMs);

                    writer.WriteLine("RibbonThickness=" + RibbonThickness.ToString("0.00", CultureInfo.InvariantCulture));
                    writer.WriteLine("RibbonFill=" + RibbonFill);
                    writer.WriteLine("RibbonFloorReflection=" + RibbonFloorReflection);

                    writer.WriteLine("BloomIntensity=" + BloomIntensity.ToString("0.00", CultureInfo.InvariantCulture));
                    writer.WriteLine("BgSource=" + (int)BgSource);
                    writer.WriteLine("BgBrightness=" + BgBrightness.ToString("0.00", CultureInfo.InvariantCulture));
                    writer.WriteLine("Vignette=" + Vignette.ToString("0.00", CultureInfo.InvariantCulture));
                    writer.WriteLine("FloorReflection=" + FloorReflection.ToString("0.00", CultureInfo.InvariantCulture));
                    writer.WriteLine("ContactGlow=" + ContactGlow);

                    writer.WriteLine("FrameCap=" + FrameCap);
                    writer.WriteLine("AnalysisHz=" + AnalysisHz);
                    writer.WriteLine("IdleThrottle=" + IdleThrottle);
                    writer.WriteLine("ShowDiagnostics=" + ShowDiagnostics);
                }
            }
            catch { }
        }
    }
}
