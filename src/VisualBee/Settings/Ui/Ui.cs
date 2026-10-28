using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal static class Ui
{
	public static readonly Color Back = Color.FromArgb(9, 11, 16);

	public static readonly Color Side = Color.FromArgb(13, 16, 24);

	public static readonly Color Card = Color.FromArgb(18, 22, 32);

	public static readonly Color Hover = Color.FromArgb(26, 34, 48);

	public static readonly Color Raised = Color.FromArgb(30, 39, 56);

	public static readonly Color RaisedHover = Color.FromArgb(38, 50, 72);

	public static readonly Color Pressed = Color.FromArgb(46, 60, 86);

	public static readonly Color Border = Color.FromArgb(30, 37, 54);

	public static readonly Color Text = Color.FromArgb(235, 240, 248);

	public static readonly Color Dim = Color.FromArgb(150, 162, 182);

	public static readonly Color Faint = Color.FromArgb(92, 104, 124);

	public static readonly Color Accent = Color.FromArgb(0, 232, 214);

	public static readonly Color Amber = Color.FromArgb(255, 176, 32);

	public static readonly Color AccentDeep = Color.FromArgb(14, 68, 78);

	public static readonly Color Danger = Color.FromArgb(255, 122, 110);

	public static readonly Font Body = new Font("Segoe UI", 9f);

	public static readonly Font Bold = new Font("Segoe UI Semibold", 9f);

	public static readonly Font Small = new Font("Segoe UI", 8f);

	public static readonly Font Heading = new Font("Segoe UI Semibold", 13f);

	public static readonly Font Section = new Font("Segoe UI Semibold", 8.25f);

	public static readonly Font Icons = IconFont(10f);

	public static readonly Font IconsLarge = IconFont(12f);

	public const string GlyphReset = "\ue7a7";

	public const string GlyphInfo = "\ue946";

	public const string GlyphSearch = "\ue721";

	public const string GlyphChevron = "\ue70d";

	public const string GlyphCheck = "\ue73e";

	public const string GlyphSave = "\ue74e";

	public const string GlyphFolder = "\ue838";

	public const string GlyphImport = "\ue8b5";

	public const string GlyphDelete = "\ue74d";

	public const string GlyphClose = "\ue711";

	public const string GlyphPalette = "\ue790";

	public const string GlyphSparkle = "\ue945";

	public static int S(int px)
	{
		return PanelLayout.S(px);
	}

	public static float Sf(float px)
	{
		return px * PanelLayout.Scale;
	}

	private static Font IconFont(float size)
	{
		string[] array = new string[2] { "Segoe Fluent Icons", "Segoe MDL2 Assets" };
		foreach (string text in array)
		{
			using Font font = new Font(text, size);
			if (font.Name == text)
			{
				return new Font(text, size);
			}
		}
		return new Font("Segoe UI Symbol", size);
	}

	public static GraphicsPath Round(RectangleF r, float radius)
	{
		GraphicsPath graphicsPath = new GraphicsPath();
		float num = Math.Min(radius * 2f, Math.Min(r.Width, r.Height));
		if (num <= 0.5f)
		{
			graphicsPath.AddRectangle(r);
			return graphicsPath;
		}
		graphicsPath.AddArc(r.X, r.Y, num, num, 180f, 90f);
		graphicsPath.AddArc(r.Right - num, r.Y, num, num, 270f, 90f);
		graphicsPath.AddArc(r.Right - num, r.Bottom - num, num, num, 0f, 90f);
		graphicsPath.AddArc(r.X, r.Bottom - num, num, num, 90f, 90f);
		graphicsPath.CloseFigure();
		return graphicsPath;
	}

	public static void Fill(Graphics g, RectangleF r, float radius, Color c)
	{
		using GraphicsPath path = Round(r, radius);
		using SolidBrush brush = new SolidBrush(c);
		g.FillPath(brush, path);
	}

	public static void Stroke(Graphics g, RectangleF r, float radius, Color c, float width = 1f)
	{
		using GraphicsPath path = Round(r, radius);
		using Pen pen = new Pen(c, width);
		g.DrawPath(pen, path);
	}

	public static void DrawText(Graphics g, string text, Font font, Color colour, Rectangle r, TextFormatFlags flags)
	{
		TextRenderer.DrawText(g, text, font, r, colour, flags | TextFormatFlags.NoPrefix);
	}

	public static void Glyph(Graphics g, string glyph, Font font, Color colour, Rectangle r)
	{
		TextRenderer.DrawText(g, glyph, font, r, colour, TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
	}

	public static void Checker(Graphics g, Rectangle r)
	{
		int num = Math.Max(3, S(5));
		using SolidBrush solidBrush = new SolidBrush(Color.FromArgb(154, 154, 154));
		using SolidBrush solidBrush2 = new SolidBrush(Color.FromArgb(92, 92, 92));
		for (int i = r.Top; i < r.Bottom; i += num)
		{
			for (int j = r.Left; j < r.Right; j += num)
			{
				bool flag = ((j - r.Left) / num + (i - r.Top) / num) % 2 == 1;
				g.FillRectangle(flag ? solidBrush2 : solidBrush, j, i, Math.Min(num, r.Right - j), Math.Min(num, r.Bottom - i));
			}
		}
	}

	public static Color Mix(Color a, Color b, double t)
	{
		return Palette.Mix(a, b, t);
	}

	public static Color FromArgb(int argb)
	{
		return Color.FromArgb(argb);
	}

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wParam, string lParam);

	public static void CueBanner(TextBox box, string text)
	{
		box.HandleCreated += (object s, EventArgs a) =>
		{
			SendMessage(box.Handle, 5377, (IntPtr)1, text);
		};
		if (box.IsHandleCreated)
		{
			SendMessage(box.Handle, 5377, (IntPtr)1, text);
		}
	}

	public static void Smooth(Graphics g)
	{
		g.SmoothingMode = SmoothingMode.AntiAlias;
		g.PixelOffsetMode = PixelOffsetMode.HighQuality;
	}
}
