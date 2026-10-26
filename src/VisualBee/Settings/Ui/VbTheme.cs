using System;
using System.Drawing;

namespace MusicBeePlugin.SettingsUi
{
    internal static class VbTheme
    {
        public static readonly Color BgMain = Color.FromArgb(18, 20, 26);
        public static readonly Color BgNav = Color.FromArgb(14, 16, 21);
        public static readonly Color BgCard = Color.FromArgb(26, 30, 40);
        public static readonly Color BgHover = Color.FromArgb(36, 42, 56);
        public static readonly Color BgActive = Color.FromArgb(46, 54, 72);

        public static readonly Color Accent = Color.FromArgb(20, 192, 166);
        public static readonly Color AccentHover = Color.FromArgb(32, 215, 188);

        public static readonly Color TextPrimary = Color.FromArgb(235, 240, 250);
        public static readonly Color TextSecondary = Color.FromArgb(145, 155, 175);
        public static readonly Color TextMuted = Color.FromArgb(100, 110, 130);

        public static readonly Color Border = Color.FromArgb(42, 48, 64);
        public static readonly Color BorderLight = Color.FromArgb(58, 66, 88);

        public static readonly Font FontTitle = new Font("Segoe UI", 12f, FontStyle.Bold);
        public static readonly Font FontSection = new Font("Segoe UI", 10f, FontStyle.Bold);
        public static readonly Font FontRegular = new Font("Segoe UI", 9f, FontStyle.Regular);
        public static readonly Font FontBold = new Font("Segoe UI", 9f, FontStyle.Bold);
        public static readonly Font FontSmall = new Font("Segoe UI", 8f, FontStyle.Regular);
    }
}
