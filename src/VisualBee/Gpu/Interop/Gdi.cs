using System;
using System.Runtime.InteropServices;

namespace MusicBeePlugin;

internal static class Gdi
{
	public struct RECT(int l, int t, int r, int b)
	{
		public int left = l;

		public int top = t;

		public int right = r;

		public int bottom = b;
	}

	public struct SIZE
	{
		public int cx;

		public int cy;
	}

	public struct TRIVERTEX
	{
		public int x;

		public int y;

		public ushort Red;

		public ushort Green;

		public ushort Blue;

		public ushort Alpha;
	}

	public struct GRADIENT_RECT
	{
		public uint UpperLeft;

		public uint LowerRight;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct MARGINS(int l, int r, int t, int b)
	{
		public int cxLeftWidth = l;
		public int cxRightWidth = r;
		public int cyTopHeight = t;
		public int cyBottomHeight = b;
	}

	public const int GWL_EXSTYLE = -20;
	public const int WS_EX_TOOLWINDOW = 0x00000080;
	public const int WS_EX_LAYERED = 0x00080000;
	public const int WS_EX_TRANSPARENT = 0x00000020;
	public const int WS_EX_NOACTIVATE = 0x08000000;
	public const int WS_EX_TOPMOST = 0x00000008;

	public const int WM_MOUSEACTIVATE = 0x0021;
	public const int MA_NOACTIVATE = 3;
	public const int WM_NCHITTEST = 0x0084;
	public const int WM_NCLBUTTONDOWN = 0x00A1;
	public const int HTCAPTION = 2;
	public const int HTLEFT = 10;
	public const int HTRIGHT = 11;
	public const int HTTOP = 12;
	public const int HTTOPLEFT = 13;
	public const int HTTOPRIGHT = 14;
	public const int HTBOTTOM = 15;
	public const int HTBOTTOMLEFT = 16;
	public const int HTBOTTOMRIGHT = 17;
	public const int WM_HOTKEY = 0x0312;

	public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
	public const int DWMWCP_ROUND = 2;

	public const int SRCCOPY = 13369376;

	public const int ETO_OPAQUE = 2;

	public const int TRANSPARENT = 1;

	public const int DC_PEN = 19;

	public const int LOGPIXELSX = 88;

	public const int LOGPIXELSY = 90;

	public const int GRADIENT_FILL_RECT_V = 1;

	public const uint DT_CENTER = 1u;

	public const uint DT_VCENTER = 4u;

	public const uint DT_SINGLELINE = 32u;

	public const uint DT_NOPREFIX = 2048u;

	public const uint LabelFormat = 2085u;

	[DllImport("gdi32.dll")]
	public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

	[DllImport("gdi32.dll")]
	public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int w, int h);

	[DllImport("gdi32.dll")]
	public static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);

	[DllImport("gdi32.dll")]
	public static extern bool DeleteObject(IntPtr obj);

	[DllImport("gdi32.dll")]
	public static extern bool DeleteDC(IntPtr hdc);

	[DllImport("gdi32.dll")]
	public static extern bool BitBlt(IntPtr dst, int x, int y, int w, int h, IntPtr src, int sx, int sy, int rop);

	[DllImport("gdi32.dll")]
	public static extern int SetBkColor(IntPtr hdc, int color);

	[DllImport("gdi32.dll")]
	public static extern int SetTextColor(IntPtr hdc, int color);

	[DllImport("gdi32.dll")]
	public static extern int SetBkMode(IntPtr hdc, int mode);

	[DllImport("gdi32.dll")]
	public static extern IntPtr GetStockObject(int obj);

	[DllImport("gdi32.dll")]
	public static extern int SetDCPenColor(IntPtr hdc, int color);

	[DllImport("gdi32.dll")]
	public static extern bool MoveToEx(IntPtr hdc, int x, int y, IntPtr old);

	[DllImport("gdi32.dll")]
	public static extern bool LineTo(IntPtr hdc, int x, int y);

	[DllImport("gdi32.dll")]
	public static extern int GetDeviceCaps(IntPtr hdc, int index);

	[DllImport("gdi32.dll", CharSet = CharSet.Unicode, EntryPoint = "ExtTextOutW")]
	public static extern bool ExtTextOut(IntPtr hdc, int x, int y, int options, ref RECT rect, string text, int count, IntPtr dx);

	[DllImport("gdi32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetTextExtentPoint32W")]
	public static extern bool GetTextExtentPoint32(IntPtr hdc, string text, int count, out SIZE size);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "DrawTextW")]
	public static extern int DrawText(IntPtr hdc, string text, int count, ref RECT rect, uint format);

	[DllImport("user32.dll")]
	public static extern IntPtr GetDC(IntPtr hwnd);

	[DllImport("user32.dll")]
	public static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

	[DllImport("user32.dll")]
	public static extern bool IsWindowVisible(IntPtr hwnd);

	[DllImport("user32.dll")]
	public static extern bool GetClientRect(IntPtr hwnd, out RECT rect);

	[DllImport("dwmapi.dll")]
	public static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

	[DllImport("dwmapi.dll")]
	public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int size);

	[DllImport("user32.dll", EntryPoint = "GetWindowLong")]
	public static extern int GetWindowLong(IntPtr hwnd, int index);

	[DllImport("user32.dll", EntryPoint = "SetWindowLong")]
	public static extern int SetWindowLong(IntPtr hwnd, int index, int val);

	[DllImport("user32.dll")]
	public static extern bool RegisterHotKey(IntPtr hwnd, int id, uint fsModifiers, uint vk);

	[DllImport("user32.dll")]
	public static extern bool UnregisterHotKey(IntPtr hwnd, int id);

	[DllImport("user32.dll")]
	public static extern bool ReleaseCapture();

	[DllImport("user32.dll")]
	public static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);

	[DllImport("gdi32.dll")]
	public static extern bool GdiFlush();

	[DllImport("gdi32.dll")]
	public static extern int SaveDC(IntPtr hdc);

	[DllImport("gdi32.dll")]
	public static extern bool RestoreDC(IntPtr hdc, int saved);

	[DllImport("gdi32.dll")]
	public static extern int IntersectClipRect(IntPtr hdc, int left, int top, int right, int bottom);

	[DllImport("gdi32.dll")]
	public static extern IntPtr CreateRectRgn(int left, int top, int right, int bottom);

	[DllImport("gdi32.dll")]
	public static extern int GetClipRgn(IntPtr hdc, IntPtr rgn);

	[DllImport("gdi32.dll")]
	public static extern int SelectClipRgn(IntPtr hdc, IntPtr rgn);

	[DllImport("gdi32.dll")]
	public static extern IntPtr CreateDIBSection(IntPtr hdc, byte[] info, uint usage, out IntPtr bits, IntPtr section, uint offset);

	[DllImport("gdi32.dll")]
	public static extern uint SetDIBColorTable(IntPtr hdc, uint start, uint count, int[] colours);

	[DllImport("gdi32.dll")]
	public static extern bool Polyline(IntPtr hdc, int[] points, int count);

	[DllImport("msimg32.dll")]
	public static extern bool GradientFill(IntPtr hdc, TRIVERTEX[] vertices, uint count, GRADIENT_RECT[] mesh, uint meshCount, uint mode);

	[DllImport("winmm.dll")]
	public static extern uint timeBeginPeriod(uint period);

	[DllImport("winmm.dll")]
	public static extern uint timeEndPeriod(uint period);

	public static int ColorRef(int argb)
	{
		int num = (argb >> 16) & 0xFF;
		int num2 = (argb >> 8) & 0xFF;
		int num3 = argb & 0xFF;
		return num | (num2 << 8) | (num3 << 16);
	}

	public static int Blend(int bgArgb, int otherArgb, double amount)
	{
		int num = 0;
		for (int i = 0; i < 24; i += 8)
		{
			int num2 = (bgArgb >> i) & 0xFF;
			int num3 = (otherArgb >> i) & 0xFF;
			int num4 = num2 + (int)Math.Round((double)(num3 - num2) * amount, MidpointRounding.AwayFromZero);
			num |= (num4 & 0xFF) << i;
		}
		return num | -16777216;
	}

	public static int Round(double v)
	{
		return (int)Math.Round(v, MidpointRounding.AwayFromZero);
	}

	public static RECT ToRect(float x, float y, float w, float h)
	{
		return new RECT(Round(x), Round(y), Round(x + w), Round(y + h));
	}
}
