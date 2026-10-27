using System;
using System.Runtime.InteropServices;

namespace MusicBeePlugin;

internal static class VuFont
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct LOGFONT
	{
		public int lfHeight;

		public int lfWidth;

		public int lfEscapement;

		public int lfOrientation;

		public int lfWeight;

		public byte lfItalic;

		public byte lfUnderline;

		public byte lfStrikeOut;

		public byte lfCharSet;

		public byte lfOutPrecision;

		public byte lfClipPrecision;

		public byte lfQuality;

		public byte lfPitchAndFamily;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string lfFaceName;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct NONCLIENTMETRICS
	{
		public int cbSize;

		public int iBorderWidth;

		public int iScrollWidth;

		public int iScrollHeight;

		public int iCaptionWidth;

		public int iCaptionHeight;

		public LOGFONT lfCaptionFont;

		public int iSmCaptionWidth;

		public int iSmCaptionHeight;

		public LOGFONT lfSmCaptionFont;

		public int iMenuWidth;

		public int iMenuHeight;

		public LOGFONT lfMenuFont;

		public LOGFONT lfStatusFont;

		public LOGFONT lfMessageFont;

		public int iPaddedBorderWidth;
	}

	private const int TmtMenuFont = 803;

	public static IntPtr CreateDefault()
	{
		if (GetThemeSysFont(IntPtr.Zero, 803, out var font) < 0)
		{
			NONCLIENTMETRICS m = new NONCLIENTMETRICS
			{
				cbSize = Marshal.SizeOf(typeof(NONCLIENTMETRICS))
			};
			if (!SystemParametersInfo(41, m.cbSize, ref m, 0))
			{
				return IntPtr.Zero;
			}
			font = m.lfMenuFont;
		}
		LOGFONT font2 = new LOGFONT
		{
			lfHeight = font.lfHeight,
			lfWeight = font.lfWeight,
			lfItalic = font.lfItalic,
			lfCharSet = 1,
			lfOutPrecision = 3,
			lfClipPrecision = 2,
			lfQuality = 1,
			lfPitchAndFamily = 34,
			lfFaceName = font.lfFaceName
		};
		return CreateFontIndirect(ref font2);
	}

	public static IntPtr Scaled(IntPtr font)
	{
		if (font == IntPtr.Zero || GetObject(font, Marshal.SizeOf(typeof(LOGFONT)), out var font2) == 0)
		{
			return IntPtr.Zero;
		}
		int num = Gdi.Round((double)font2.lfHeight * 0.7);
		if (num == 0)
		{
			num = ((font2.lfHeight < 0) ? (-1) : ((font2.lfHeight > 0) ? 1 : 0));
		}
		font2.lfHeight = num;
		return CreateFontIndirect(ref font2);
	}

	public static IntPtr CreateLabel(double scale, bool bold)
	{
		IntPtr intPtr = CreateDefault();
		LOGFONT font = default;
		bool num = intPtr != IntPtr.Zero && GetObject(intPtr, Marshal.SizeOf(typeof(LOGFONT)), out font) != 0;
		if (intPtr != IntPtr.Zero)
		{
			Gdi.DeleteObject(intPtr);
		}
		if (!num)
		{
			return IntPtr.Zero;
		}
		int num2 = Gdi.Round((double)font.lfHeight * scale);
		if (num2 == 0)
		{
			num2 = ((font.lfHeight >= 0) ? 1 : (-1));
		}
		font.lfHeight = num2;
		font.lfQuality = 4;
		if (bold)
		{
			font.lfWeight = 700;
		}
		return CreateFontIndirect(ref font);
	}

	public static void Measure(IntPtr dc, IntPtr font, out Gdi.SIZE wtf, out Gdi.SIZE db60)
	{
		IntPtr obj = Gdi.SelectObject(dc, font);
		Gdi.GetTextExtentPoint32(dc, "WTF", 3, out wtf);
		Gdi.GetTextExtentPoint32(dc, "-60dB", 5, out db60);
		Gdi.SelectObject(dc, obj);
	}

	[DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
	private static extern int GetThemeSysFont(IntPtr theme, int fontId, out LOGFONT font);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "SystemParametersInfoW")]
	private static extern bool SystemParametersInfo(int action, int param, ref NONCLIENTMETRICS m, int ini);

	[DllImport("gdi32.dll", CharSet = CharSet.Unicode, EntryPoint = "CreateFontIndirectW")]
	private static extern IntPtr CreateFontIndirect(ref LOGFONT font);

	[DllImport("gdi32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetObjectW")]
	private static extern int GetObject(IntPtr h, int size, out LOGFONT font);
}
