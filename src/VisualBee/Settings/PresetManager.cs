using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;

namespace MusicBeePlugin
{
    public sealed class ThemePreset
    {
        public string Key { get; set; }
        public string DisplayName { get; set; }
        public bool IsBuiltIn { get; set; }
        public PaletteColors Colors { get; set; }

        public ThemePreset(string key, string displayName, bool isBuiltIn, PaletteColors colors)
        {
            Key = key ?? string.Empty;
            DisplayName = displayName ?? key;
            IsBuiltIn = isBuiltIn;
            Colors = colors;
        }

        public static string ColorToHex(Color c)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
        }

        public static Color HexToColor(string hex, Color defaultColor)
        {
            if (string.IsNullOrEmpty(hex)) return defaultColor;
            hex = hex.Trim().TrimStart('#');
            if (hex.Length == 6 && int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
            {
                return Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
            }
            return defaultColor;
        }
    }

    public static class PresetManager
    {
        private static readonly object _sync = new object();
        private static string _storagePath;
        private static readonly List<ThemePreset> _builtInPresets = new List<ThemePreset>();
        private static readonly Dictionary<string, ThemePreset> _customPresets = new Dictionary<string, ThemePreset>(StringComparer.OrdinalIgnoreCase);

        static PresetManager()
        {
            RegisterBuiltIns();
        }

        private static void RegisterBuiltIns()
        {
            _builtInPresets.Clear();
            _builtInPresets.Add(new ThemePreset("skin", "MusicBee Skin", true, default(PaletteColors)));
            _builtInPresets.Add(new ThemePreset("aurora", "Aurora (Teal)", true, PaletteManager.Aurora));
            _builtInPresets.Add(new ThemePreset("cyberpunk", "Cyberpunk (Neon)", true, PaletteManager.Cyberpunk));
            _builtInPresets.Add(new ThemePreset("sunset", "Sunset Ember", true, PaletteManager.Sunset));
            _builtInPresets.Add(new ThemePreset("lavender", "Electric Lavender", true, PaletteManager.ElectricLavender));
            _builtInPresets.Add(new ThemePreset("phosphor", "Phosphor Green", true, PaletteManager.PhosphorGreen));
            _builtInPresets.Add(new ThemePreset("monochrome", "Monochrome Slate", true, PaletteManager.Monochrome));
            _builtInPresets.Add(new ThemePreset("mcintosh", "McIntosh Studio Glass", true, PaletteManager.McIntoshGlass));
            _builtInPresets.Add(new ThemePreset("teenage", "Teenage Engineering", true, PaletteManager.TeenageEngineering));
            _builtInPresets.Add(new ThemePreset("oled", "OLED Midnight", true, PaletteManager.OledMidnight));
        }

        public static void Initialize(MusicBeeApiInterface api)
        {
            lock (_sync)
            {
                try
                {
                    if (api.Setting_GetPersistentStoragePath != null)
                    {
                        string folder = api.Setting_GetPersistentStoragePath();
                        if (!string.IsNullOrEmpty(folder))
                        {
                            string dir = Path.Combine(folder, "VisualBee");
                            Directory.CreateDirectory(dir);
                            _storagePath = Path.Combine(dir, "presets.ini");
                        }
                    }
                }
                catch { }

                LoadCustomPresets();
            }
        }

        public static List<ThemePreset> GetAllPresets(MusicBeeApiInterface api)
        {
            lock (_sync)
            {
                var list = new List<ThemePreset>();
                foreach (var b in _builtInPresets)
                {
                    if (b.Key.Equals("skin", StringComparison.OrdinalIgnoreCase))
                        list.Add(new ThemePreset("skin", "MusicBee Skin", true, PaletteManager.FromSkin(api)));
                    else
                        list.Add(b);
                }
                foreach (var c in _customPresets.Values)
                {
                    list.Add(c);
                }
                return list;
            }
        }

        public static PaletteColors Resolve(string key, MusicBeeApiInterface api)
        {
            if (string.IsNullOrEmpty(key) || key.Equals("skin", StringComparison.OrdinalIgnoreCase))
                return PaletteManager.FromSkin(api);

            lock (_sync)
            {
                if (_customPresets.TryGetValue(key, out var custom))
                    return custom.Colors;

                foreach (var b in _builtInPresets)
                {
                    if (b.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                        return b.Colors;
                }
            }

            return PaletteManager.FromSkin(api);
        }

        public static bool SaveCustomPreset(string name, PaletteColors colors)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string key = name.Trim().ToLowerInvariant().Replace(' ', '_');

            lock (_sync)
            {
                var preset = new ThemePreset(key, name.Trim(), false, colors);
                _customPresets[key] = preset;
                return PersistCustomPresets();
            }
        }

        public static bool DeleteCustomPreset(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            lock (_sync)
            {
                if (_customPresets.Remove(key))
                {
                    return PersistCustomPresets();
                }
            }
            return false;
        }

        private static void LoadCustomPresets()
        {
            _customPresets.Clear();
            if (string.IsNullOrEmpty(_storagePath) || !File.Exists(_storagePath)) return;

            try
            {
                string[] lines = File.ReadAllLines(_storagePath);
                string curKey = null;
                string curName = null;
                Color a = Color.Empty, b = Color.Empty, c = Color.Empty, d = Color.Empty, bg = Color.Empty;

                foreach (string rawLine in lines)
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith(";"))
                        continue;

                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        if (curKey != null)
                        {
                            _customPresets[curKey] = new ThemePreset(curKey, curName ?? curKey, false, new PaletteColors(a, b, c, d, bg));
                        }
                        curKey = line.Substring(1, line.Length - 2).Trim();
                        curName = curKey;
                        a = Color.FromArgb(20, 30, 40);
                        b = Color.FromArgb(20, 192, 166);
                        c = Color.FromArgb(50, 220, 200);
                        d = Color.FromArgb(220, 250, 255);
                        bg = Color.FromArgb(10, 12, 16);
                        continue;
                    }

                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string k = line.Substring(0, eq).Trim();
                    string v = line.Substring(eq + 1).Trim();

                    switch (k.ToLowerInvariant())
                    {
                        case "name": curName = v; break;
                        case "colora": a = ThemePreset.HexToColor(v, a); break;
                        case "colorb": b = ThemePreset.HexToColor(v, b); break;
                        case "colorc": c = ThemePreset.HexToColor(v, c); break;
                        case "colord": d = ThemePreset.HexToColor(v, d); break;
                        case "background": bg = ThemePreset.HexToColor(v, bg); break;
                    }
                }

                if (curKey != null)
                {
                    _customPresets[curKey] = new ThemePreset(curKey, curName ?? curKey, false, new PaletteColors(a, b, c, d, bg));
                }
            }
            catch { }
        }

        private static bool PersistCustomPresets()
        {
            if (string.IsNullOrEmpty(_storagePath)) return false;
            try
            {
                using (var w = new StreamWriter(_storagePath, false))
                {
                    w.WriteLine("# VisualBee Custom Theme Presets");
                    foreach (var p in _customPresets.Values)
                    {
                        w.WriteLine("[" + p.Key + "]");
                        w.WriteLine("Name=" + p.DisplayName);
                        w.WriteLine("ColorA=" + ThemePreset.ColorToHex(p.Colors.ColorA));
                        w.WriteLine("ColorB=" + ThemePreset.ColorToHex(p.Colors.ColorB));
                        w.WriteLine("ColorC=" + ThemePreset.ColorToHex(p.Colors.ColorC));
                        w.WriteLine("ColorD=" + ThemePreset.ColorToHex(p.Colors.ColorD));
                        w.WriteLine("Background=" + ThemePreset.ColorToHex(p.Colors.Background));
                        w.WriteLine();
                    }
                }
                return true;
            }
            catch { return false; }
        }
    }
}