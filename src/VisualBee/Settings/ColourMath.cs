using System;
using System.Collections.Generic;
using System.Globalization;

namespace MusicBeePlugin;

internal static class ColourMath
{
	public struct Lab
	{
		public double L;

		public double A;

		public double B;

		public double Alpha;
	}

	public static string ToHex(int argb, bool alpha)
	{
		uint num = (uint)argb;
		if (!alpha || num >> 24 == 255)
		{
			return "#" + (num & 0xFFFFFF).ToString("X6", CultureInfo.InvariantCulture);
		}
		return "#" + num.ToString("X8", CultureInfo.InvariantCulture);
	}

	public static bool TryParseHex(string text, out int argb)
	{
		argb = 0;
		text = (text ?? "").Trim().TrimStart('#');
		if (text.Length == 3)
		{
			text = new string(new char[6]
			{
				text[0],
				text[0],
				text[1],
				text[1],
				text[2],
				text[2]
			});
		}
		if ((text.Length != 6 && text.Length != 8) || !uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result))
		{
			return false;
		}
		argb = ((text.Length == 6) ? ((int)result | -16777216) : ((int)result));
		return true;
	}

	public static int Argb(int a, int r, int g, int b)
	{
		return (Clamp255(a) << 24) | (Clamp255(r) << 16) | (Clamp255(g) << 8) | Clamp255(b);
	}

	private static int Clamp255(int v)
	{
		if (v >= 0)
		{
			if (v <= 255)
			{
				return v;
			}
			return 255;
		}
		return 0;
	}

	public static int WithAlpha(int argb, int alpha)
	{
		return (argb & 0xFFFFFF) | (Clamp255(alpha) << 24);
	}

	public static int FromHsv(double h, double s, double v, int alpha = 255)
	{
		h = (h % 360.0 + 360.0) % 360.0 / 60.0;
		int num = (int)Math.Floor(h);
		double num2 = h - (double)num;
		double num3 = v * (1.0 - s);
		double num4 = v * (1.0 - s * num2);
		double num5 = v * (1.0 - s * (1.0 - num2));
		double num6;
		double num7;
		double num8;
		switch (num)
		{
		case 0:
			num6 = v;
			num7 = num5;
			num8 = num3;
			break;
		case 1:
			num6 = num4;
			num7 = v;
			num8 = num3;
			break;
		case 2:
			num6 = num3;
			num7 = v;
			num8 = num5;
			break;
		case 3:
			num6 = num3;
			num7 = num4;
			num8 = v;
			break;
		case 4:
			num6 = num5;
			num7 = num3;
			num8 = v;
			break;
		default:
			num6 = v;
			num7 = num3;
			num8 = num4;
			break;
		}
		return Argb(alpha, (int)Math.Round(num6 * 255.0), (int)Math.Round(num7 * 255.0), (int)Math.Round(num8 * 255.0));
	}

	public static void ToHsv(int argb, out double h, out double s, out double v)
	{
		double num = (double)((argb >> 16) & 0xFF) / 255.0;
		double num2 = (double)((argb >> 8) & 0xFF) / 255.0;
		double num3 = (double)(argb & 0xFF) / 255.0;
		double num4 = Math.Max(num, Math.Max(num2, num3));
		double num5 = Math.Min(num, Math.Min(num2, num3));
		double num6 = num4 - num5;
		v = num4;
		s = ((num4 > 0.0) ? (num6 / num4) : 0.0);
		if (num6 <= 0.0)
		{
			h = 0.0;
		}
		else if (num4 == num)
		{
			h = 60.0 * ((num2 - num3) / num6 % 6.0);
		}
		else if (num4 == num2)
		{
			h = 60.0 * ((num3 - num) / num6 + 2.0);
		}
		else
		{
			h = 60.0 * ((num - num2) / num6 + 4.0);
		}
		if (h < 0.0)
		{
			h += 360.0;
		}
	}

	private static double ToLinear(double c)
	{
		if (!(c <= 0.04045))
		{
			return Math.Pow((c + 0.055) / 1.055, 2.4);
		}
		return c / 12.92;
	}

	private static double ToGamma(double c)
	{
		c = ((c < 0.0) ? 0.0 : ((c > 1.0) ? 1.0 : c));
		if (!(c <= 0.0031308))
		{
			return 1.055 * Math.Pow(c, 5.0 / 12.0) - 0.055;
		}
		return 12.92 * c;
	}

	public static Lab ToOklab(Vec4 c)
	{
		double num = ToLinear(c.X);
		double num2 = ToLinear(c.Y);
		double num3 = ToLinear(c.Z);
		double num4 = Math.Pow(0.4122214708 * num + 0.5363325363 * num2 + 0.0514459929 * num3, 1.0 / 3.0);
		double num5 = Math.Pow(0.2119034982 * num + 0.6806995451 * num2 + 0.1073969566 * num3, 1.0 / 3.0);
		double num6 = Math.Pow(0.0883024619 * num + 0.2817188376 * num2 + 0.6299787005 * num3, 1.0 / 3.0);
		return new Lab
		{
			L = 0.2104542553 * num4 + 0.793617785 * num5 - 0.0040720468 * num6,
			A = 1.9779984951 * num4 - 2.428592205 * num5 + 0.4505937099 * num6,
			B = 0.0259040371 * num4 + 0.7827717662 * num5 - 0.808675766 * num6,
			Alpha = c.W
		};
	}

	public static Vec4 FromOklab(Lab c)
	{
		double num = c.L + 0.3963377774 * c.A + 0.2158037573 * c.B;
		double num2 = c.L - 0.1055613458 * c.A - 0.0638541728 * c.B;
		double num3 = c.L - 0.0894841775 * c.A - 1.291485548 * c.B;
		num = num * num * num;
		num2 = num2 * num2 * num2;
		num3 = num3 * num3 * num3;
		double c2 = 4.0767416621 * num - 3.3077115913 * num2 + 0.2309699292 * num3;
		double c3 = -1.2684380046 * num + 2.6097574011 * num2 - 0.3413193965 * num3;
		double c4 = -0.0041960863 * num - 0.7034186147 * num2 + 1.707614701 * num3;
		return new Vec4((float)ToGamma(c2), (float)ToGamma(c3), (float)ToGamma(c4), (float)c.Alpha);
	}

	public static Vec4 Mix(Vec4 from, Vec4 to, float t)
	{
		if (t >= 1f)
		{
			return to;
		}
		Lab lab = ToOklab(from);
		Lab lab2 = ToOklab(to);
		return FromOklab(new Lab
		{
			L = lab.L + (lab2.L - lab.L) * (double)t,
			A = lab.A + (lab2.A - lab.A) * (double)t,
			B = lab.B + (lab2.B - lab.B) * (double)t,
			Alpha = lab.Alpha + (lab2.Alpha - lab.Alpha) * (double)t
		});
	}

	public static Vec4 RotateHue(Vec4 c, double degrees)
	{
		Lab c2 = ToOklab(c);
		double num = degrees * Math.PI / 180.0;
		double num2 = Math.Cos(num);
		double num3 = Math.Sin(num);
		double a = c2.A * num2 - c2.B * num3;
		double b = c2.A * num3 + c2.B * num2;
		c2.A = a;
		c2.B = b;
		return FromOklab(c2);
	}

	public static int ToArgb(Vec4 c)
	{
		return Argb((int)Math.Round(c.W * 255f), (int)Math.Round(c.X * 255f), (int)Math.Round(c.Y * 255f), (int)Math.Round(c.Z * 255f));
	}

	public static double Lightness(int argb)
	{
		return ToOklab(Vec4.Colour(argb)).L;
	}

	public static int Blend(int a, int b, double t)
	{
		return ToArgb(Vec4.Lerp(Vec4.Colour(a), Vec4.Colour(b), (float)t));
	}

	public static List<int> Palette(int[] pixels, int count)
	{
		double[] array = new double[97];
		double[] array2 = new double[array.Length];
		double[] array3 = new double[array.Length];
		double[] array4 = new double[array.Length];
		for (int i = 0; i < count; i += 2)
		{
			int num = pixels[i];
			ToHsv(num, out var h, out var s, out var v);
			int num2 = ((s < 0.18 || v < 0.12) ? 96 : ((int)(h / 360.0 * 24.0) % 24 * 4 + Math.Min(3, (int)(v * 4.0))));
			double num3 = 1.0 + 3.0 * s * v;
			array[num2] += (double)((num >> 16) & 0xFF) * num3;
			array2[num2] += (double)((num >> 8) & 0xFF) * num3;
			array3[num2] += (double)(num & 0xFF) * num3;
			array4[num2] += num3;
		}
		List<KeyValuePair<double, int>> list = new List<KeyValuePair<double, int>>();
		for (int j = 0; j < array4.Length; j++)
		{
			if (!(array4[j] <= 0.0))
			{
				int num4 = Argb(255, (int)(array[j] / array4[j]), (int)(array2[j] / array4[j]), (int)(array3[j] / array4[j]));
				ToHsv(num4, out var _, out var s2, out var _);
				list.Add(new KeyValuePair<double, int>(array4[j] * (0.35 + s2), num4));
			}
		}
		list.Sort((KeyValuePair<double, int> x, KeyValuePair<double, int> y) => y.Key.CompareTo(x.Key));
		List<int> list2 = new List<int>();
		foreach (KeyValuePair<double, int> item in list)
		{
			bool flag = true;
			foreach (int item2 in list2)
			{
				Lab lab = ToOklab(Vec4.Colour(item2));
				Lab lab2 = ToOklab(Vec4.Colour(item.Value));
				if (Math.Sqrt((lab.L - lab2.L) * (lab.L - lab2.L) + (lab.A - lab2.A) * (lab.A - lab2.A) + (lab.B - lab2.B) * (lab.B - lab2.B)) < 0.12)
				{
					flag = false;
				}
			}
			if (flag)
			{
				list2.Add(item.Value);
			}
			if (list2.Count == 4)
			{
				break;
			}
		}
		return list2;
	}

	public static Theme ThemeFrom(List<int> palette)
	{
		if (palette == null || palette.Count == 0)
		{
			return null;
		}
		List<int> list = new List<int>(palette);
		list.Sort((int a, int b) => Chroma(b).CompareTo(Chroma(a)));
		int argb = list[0];
		int argb2 = ((list.Count > 1) ? list[1] : RotateArgb(argb, 40.0));
		int argb3 = ((list.Count > 2) ? list[2] : RotateArgb(argb, -40.0));
		int argb4 = palette[0];
		int backTop = Shade(argb4, 0.13, 0.35);
		int backBottom = Shade(argb4, 0.07, 0.3);
		int low = Shade(argb2, 0.45, 0.9);
		int num = Shade(argb, 0.7, 1.0);
		int high = Shade(argb3, 0.88, 0.75);
		int peak = Shade(argb, 0.95, 0.35);
		return new Theme("Album art", backTop, backBottom, Shade(argb4, 0.1, 0.4), low, num, high, peak, num, Shade(argb4, 0.28, 0.25), Shade(argb4, 0.75, 0.2));
	}

	private static double Chroma(int argb)
	{
		Lab lab = ToOklab(Vec4.Colour(argb));
		return Math.Sqrt(lab.A * lab.A + lab.B * lab.B);
	}

	private static int RotateArgb(int argb, double degrees)
	{
		return ToArgb(RotateHue(Vec4.Colour(argb), degrees));
	}

	private static int Shade(int argb, double lightness, double chroma)
	{
		Lab c = ToOklab(Vec4.Colour(argb));
		double num = Math.Sqrt(c.A * c.A + c.B * c.B);
		double num2 = Math.Min(0.25, Math.Max(num * chroma, 0.02 * chroma));
		double num3 = ((num > 1E-05) ? (num2 / num) : 0.0);
		c.L = lightness;
		c.A *= num3;
		c.B *= num3;
		c.Alpha = 1.0;
		return ToArgb(FromOklab(c));
	}
}
