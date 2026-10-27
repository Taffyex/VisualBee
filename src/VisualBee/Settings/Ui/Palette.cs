using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed class Palette
{
	public Color Back;

	public Color Fore;

	public Color Side;

	public Color ListBack;

	public Color ListAlt;

	public Color ListFore;

	public Color Dim;

	public Color Faint;

	public Color Line;

	public Color Selection;

	public Color Button;

	public Color ButtonHot;

	public Color Bad;

	public Color Good;

	public Color Accent;

	public bool Dark => Back.GetBrightness() < 0.5f;

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

	public static void DarkTitle(Form form, Palette p)
	{
		if (!p.Dark)
		{
			return;
		}
		int value = 1;
		try
		{
			if (DwmSetWindowAttribute(form.Handle, 20, ref value, 4) != 0)
			{
				DwmSetWindowAttribute(form.Handle, 19, ref value, 4);
			}
		}
		catch (Exception)
		{
		}
	}

	public static Palette From(Color back, Color fore, Color listBack, Color listFore)
	{
		Palette palette = new Palette
		{
			Back = back,
			Fore = fore,
			ListBack = listBack,
			ListFore = listFore
		};
		bool flag = back.GetBrightness() < 0.5f;
		palette.Side = Mix(back, fore, 0.04);
		palette.ListAlt = Mix(listBack, listFore, 0.035);
		palette.Dim = Mix(fore, back, 0.35);
		palette.Faint = Mix(fore, back, 0.55);
		palette.Line = Mix(back, fore, 0.14);
		palette.Selection = Mix(listBack, Color.FromArgb(51, 153, 255), flag ? 0.35 : 0.22);
		palette.Button = Mix(back, fore, 0.08);
		palette.ButtonHot = Mix(back, fore, 0.16);
		palette.Bad = (flag ? Color.FromArgb(255, 122, 110) : Color.FromArgb(198, 40, 40));
		palette.Good = (flag ? Color.FromArgb(76, 175, 80) : Color.FromArgb(46, 125, 50));
		palette.Accent = (flag ? Color.FromArgb(61, 155, 245) : Color.FromArgb(25, 118, 210));
		return palette;
	}

	public static Color Mix(Color a, Color b, double t)
	{
		return Color.FromArgb((int)Math.Round((double)(int)a.R + (double)(b.R - a.R) * t), (int)Math.Round((double)(int)a.G + (double)(b.G - a.G) * t), (int)Math.Round((double)(int)a.B + (double)(b.B - a.B) * t));
	}
}
