using System.Globalization;
using System;
using System.Drawing;

namespace MusicBeePlugin;

internal sealed partial class SpectrumView
{
	private void EnsureFont(SpectrumLook look, float scale)
	{
		double num = look.LabelSize / 100.0 * 0.7;
		if (!(_labelFont != IntPtr.Zero) || num != _labelFontSize)
		{
			if (_labelFont != IntPtr.Zero)
			{
				Gdi.DeleteObject(_labelFont);
			}
			_labelFont = VuFont.CreateLabel(num, bold: false);
			_labelFontSize = num;
			_labelsBuilt = false;
			_sizeDb = TextSheet.Measure(_labelFont, "-60dB");
			_sizeFreq = TextSheet.Measure(_labelFont, "5.5K");
		}
	}

	private void BuildLabels(GpuDevice gpu, SpectrumLook look, Geometry g)
	{
		_labelsBuilt = true;
		_labels.Clear();
		Gdi.SIZE sIZE = (_sizeDb.cy > 0) ? _sizeDb : TextSheet.Measure(_labelFont, "-60dB");
		if (g.DbColumn && g.Span > 0f)
		{
			double num = (double)g.Span / Math.Max(1.0, look.CeilingDb - look.FloorDb);
			int num2 = 10;
			while (num2 < 60 && (double)num2 * num < (double)sIZE.cy)
			{
				num2 = num2 switch
				{
					20 => 30, 
					10 => 20, 
					_ => 60, 
				};
			}
			int num3 = (int)Math.Floor(look.CeilingDb / 10.0) * 10;
			while ((double)num3 >= look.FloorDb)
			{
				if (num3 % num2 == 0)
				{
					float v = (float)Norm(look, num3);
					int num4 = GridRows(g, look, v, out var y, out var y2);
					for (int i = 0; i < num4; i++)
					{
						float val = ((i == 0) ? y : y2);
						float num5 = Math.Max(g.Top + (float)sIZE.cy / 2f, Math.Min(g.Bottom - (float)sIZE.cy / 2f, val));
						Gdi.RECT rect = new Gdi.RECT((int)g.DbLeft, (int)Math.Round(num5 - (float)sIZE.cy / 2f), (int)Math.Round(g.DbRight), (int)Math.Round(num5 + (float)sIZE.cy / 2f + 1f));
						_labels.Add(num3.ToString(CultureInfo.InvariantCulture) + "dB", rect, 2085u);
					}
				}
				num3 -= 10;
			}
		}
		if (g.FreqRow)
		{
			Gdi.SIZE sIZE2 = (_sizeFreq.cy > 0) ? _sizeFreq : TextSheet.Measure(_labelFont, "5.5K");
			int num6 = (int)Math.Round(g.FreqTop);
			int num7 = num6 + sIZE2.cy;
			if (look.Engine == 0)
			{
				for (int j = 0; j < g.Slots; j++)
				{
					_labels.Add(BandLabel(g.Band[j], _bands), new Gdi.RECT((int)g.X0[j], num6, (int)Math.Max(g.X0[j] + 1f, g.X1[j]), num7), 2085u);
				}
			}
			else
			{
				FrequencyLabels(look, g, num6, num7);
			}
		}
		_labels.Build(gpu, _labelFont);
	}

	private void FrequencyLabels(SpectrumLook look, Geometry g, int top, int bottom)
	{
		double num = TopHz(look);
		bool flag = g.Slots == g.Shown * 2;
		float num2 = (g.Right - g.Left) / (float)((!flag) ? 1 : 2);
		float num3 = float.NegativeInfinity;
		double[] labelFrequencies = LabelFrequencies;
		for (int i = 0; i < labelFrequencies.Length; i++)
		{
			double num4 = labelFrequencies[i];
			if (num4 < look.MinHz || num4 > num)
			{
				continue;
			}
			float num5 = (float)SpectrumAnalyzer.ToPosition(num4, look.MinHz, num, look.Scale);
			string text = ((num4 >= 1000.0) ? ((num4 / 1000.0).ToString("0.#", CultureInfo.InvariantCulture) + "k") : num4.ToString("0", CultureInfo.InvariantCulture));
			Gdi.SIZE sIZE = TextSheet.Measure(_labelFont, text);
			float[] array;
			if (flag)
			{
				array = ((look.Layout != 3) ? new float[2]
				{
					g.Left + num2 - num5 * num2,
					g.Left + num2 + num5 * num2
				} : new float[2]
				{
					g.Left + num5 * num2,
					g.Right - num5 * num2
				});
			}
			else
			{
				array = new float[1] { g.Left + num5 * num2 };
			}
			if (!(array[0] - (float)sIZE.cx / 2f < num3 + 6f))
			{
				float[] array2 = array;
				foreach (float num6 in array2)
				{
					float num7 = Math.Max(g.Left, Math.Min(g.Right - (float)sIZE.cx, num6 - (float)sIZE.cx / 2f));
					_labels.Add(text, new Gdi.RECT((int)num7, top, (int)num7 + sIZE.cx + 1, bottom), 2085u);
				}
				num3 = array[0] + (float)sIZE.cx / 2f;
			}
		}
	}

	private static string BandLabel(int band, int bands)
	{
		double num = FoobarAnalyzer.BandFrequency(band, bands);
		long num2 = (long)Math.Round(num * 0.001, MidpointRounding.AwayFromZero);
		if (num2 > 9)
		{
			return num2 + "K";
		}
		long num3 = (long)Math.Round(num * 0.01, MidpointRounding.AwayFromZero);
		if (num3 > 9)
		{
			return num3 / 10 + "." + num3 % 10 + "K";
		}
		return ((long)Math.Round(num, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture);
	}

}
