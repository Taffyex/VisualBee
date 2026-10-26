using System;
using System.Drawing;

namespace MusicBeePlugin
{
    public struct PaletteColors
    {
        public Color ColorA;
        public Color ColorB;
        public Color ColorC;
        public Color ColorD;
        public Color Background;

        public PaletteColors(Color a, Color b, Color c, Color d, Color bg)
        {
            ColorA = a;
            ColorB = b;
            ColorC = c;
            ColorD = d;
            Background = bg;
        }

        public Color Evaluate(float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            if (t <= 0.33f)
            {
                float k = t / 0.33f;
                return Lerp(ColorA, ColorB, k);
            }
            if (t <= 0.66f)
            {
                float k = (t - 0.33f) / 0.33f;
                return Lerp(ColorB, ColorC, k);
            }
            {
                float k = (t - 0.66f) / 0.34f;
                return Lerp(ColorC, ColorD, k);
            }
        }

        private static Color Lerp(Color c1, Color c2, float k)
        {
            int r = (int)(c1.R + (c2.R - c1.R) * k);
            int g = (int)(c1.G + (c2.G - c1.G) * k);
            int b = (int)(c1.B + (c2.B - c1.B) * k);
            return Color.FromArgb(
                Math.Max(0, Math.Min(255, r)),
                Math.Max(0, Math.Min(255, g)),
                Math.Max(0, Math.Min(255, b)));
        }
    }

    public static class PaletteManager
    {
        public static readonly PaletteColors Aurora = new PaletteColors(
            Color.FromArgb(2, 28, 32),
            Color.FromArgb(20, 192, 166),
            Color.FromArgb(140, 250, 225),
            Color.FromArgb(220, 245, 255),
            Color.FromArgb(6, 12, 14));

        public static readonly PaletteColors Cyberpunk = new PaletteColors(
            Color.FromArgb(15, 8, 30),
            Color.FromArgb(140, 25, 235),
            Color.FromArgb(255, 20, 148),
            Color.FromArgb(25, 242, 255),
            Color.FromArgb(10, 8, 18));

        public static readonly PaletteColors Sunset = new PaletteColors(
            Color.FromArgb(28, 8, 32),
            Color.FromArgb(217, 38, 82),
            Color.FromArgb(255, 140, 30),
            Color.FromArgb(255, 235, 128),
            Color.FromArgb(14, 6, 16));

        public static readonly PaletteColors ElectricLavender = new PaletteColors(
            Color.FromArgb(18, 12, 36),
            Color.FromArgb(98, 70, 234),
            Color.FromArgb(170, 125, 255),
            Color.FromArgb(240, 220, 255),
            Color.FromArgb(12, 10, 20));

        public static readonly PaletteColors PhosphorGreen = new PaletteColors(
            Color.FromArgb(4, 20, 10),
            Color.FromArgb(16, 145, 54),
            Color.FromArgb(52, 232, 108),
            Color.FromArgb(215, 255, 225),
            Color.FromArgb(5, 12, 6));

        public static readonly PaletteColors Monochrome = new PaletteColors(
            Color.FromArgb(20, 22, 25),
            Color.FromArgb(90, 102, 115),
            Color.FromArgb(192, 204, 217),
            Color.FromArgb(255, 255, 255),
            Color.FromArgb(14, 15, 18));

        public static readonly PaletteColors McIntoshGlass = new PaletteColors(
            Color.FromArgb(5, 20, 35),
            Color.FromArgb(0, 110, 230),
            Color.FromArgb(0, 225, 180),
            Color.FromArgb(210, 255, 245),
            Color.FromArgb(8, 12, 18));

        public static readonly PaletteColors VfdStudioGlass = new PaletteColors(
            Color.FromArgb(4, 16, 28),
            Color.FromArgb(0, 120, 240),
            Color.FromArgb(0, 240, 220),
            Color.FromArgb(255, 255, 255),
            Color.FromArgb(4, 8, 16));

        public static readonly PaletteColors TeenageEngineering = new PaletteColors(
            Color.FromArgb(30, 30, 32),
            Color.FromArgb(245, 85, 25),
            Color.FromArgb(235, 225, 210),
            Color.FromArgb(255, 255, 255),
            Color.FromArgb(18, 18, 20));

        public static readonly PaletteColors OledMidnight = new PaletteColors(
            Color.FromArgb(10, 12, 16),
            Color.FromArgb(70, 140, 245),
            Color.FromArgb(170, 210, 255),
            Color.FromArgb(255, 255, 255),
            Color.FromArgb(0, 0, 0));

        public static PaletteColors FromSkin(MusicBeeApiInterface api)
        {
            Color accent = Color.FromArgb(20, 220, 190);
            Color bg = Color.FromArgb(18, 20, 24);

            if (api.Setting_GetSkinElementColour != null)
            {
                try
                {
                    int rawAccent = api.Setting_GetSkinElementColour(
                        SkinElement.SkinTrackAndArtistPanel,
                        ElementState.ElementStateDefault,
                        ElementComponent.ComponentForeground);
                    if (rawAccent != 0)
                        accent = Color.FromArgb((rawAccent >> 16) & 0xFF, (rawAccent >> 8) & 0xFF, rawAccent & 0xFF);

                    int rawBg = api.Setting_GetSkinElementColour(
                        SkinElement.SkinTrackAndArtistPanel,
                        ElementState.ElementStateDefault,
                        ElementComponent.ComponentBackground);
                    if (rawBg != 0)
                        bg = Color.FromArgb((rawBg >> 16) & 0xFF, (rawBg >> 8) & 0xFF, rawBg & 0xFF);
                }
                catch { }
            }

            int ar = accent.R, ag = accent.G, ab = accent.B;
            Color dark = Color.FromArgb(Math.Max(5, (int)(ar * 0.15f)), Math.Max(5, (int)(ag * 0.15f)), Math.Max(5, (int)(ab * 0.15f)));
            Color mid = accent;
            Color bright = Color.FromArgb(
                Math.Min(255, (int)(ar * 1.25f + 40)),
                Math.Min(255, (int)(ag * 1.25f + 40)),
                Math.Min(255, (int)(ab * 1.25f + 40)));
            Color peak = Color.FromArgb(
                Math.Min(255, (int)(ar * 0.5f + 130)),
                Math.Min(255, (int)(ag * 0.5f + 130)),
                Math.Min(255, (int)(ab * 0.5f + 130)));

            return new PaletteColors(dark, mid, bright, peak, bg);
        }

        public static PaletteColors Resolve(string name, MusicBeeApiInterface api)
        {
            return PresetManager.Resolve(name, api);
        }
    }
}
