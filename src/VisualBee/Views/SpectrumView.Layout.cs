using System;
using System.Drawing;

namespace MusicBeePlugin;

internal sealed partial class SpectrumView
{
	private void EnsureGeometry(ref Frame f)
	{
		SpectrumLook spectrumLook = (SpectrumLook)f.Look;
		if (_geo == null || _geo.Width != f.Width || _geo.Height != f.Height || _geo.Bands != _bands || _geo.Rate != _rate || GeometryDiffers(_geo.Look, spectrumLook))
		{
			bool num = _geo == null || _geo.Look == null || LabelsDiffer(_geo.Look, spectrumLook) || _geo.Width != f.Width || _geo.Height != f.Height || _geo.Bands != _bands || _geo.Rate != _rate;
			_geo = Measure(spectrumLook, f.Width, f.Height, f.Scale);
			if (num)
			{
				_labelsBuilt = false;
			}
		}
		else
		{
			_geo.Look = spectrumLook;
		}
	}

	private static bool GeometryDiffers(SpectrumLook a, SpectrumLook b)
	{
		if (a == null || b == null) return true;
		if (a.Layout == b.Layout && a.Margin == b.Margin && a.Engine == b.Engine &&
		    a.FillWidth == b.FillWidth && a.Snap == b.Snap && a.DbLabels == b.DbLabels &&
		    a.FreqLabels == b.FreqLabels && a.LabelSize == b.LabelSize && a.BarWidth == b.BarWidth &&
		    a.BarGap == b.BarGap && a.TrebleTaper == b.TrebleTaper && a.RadialInner == b.RadialInner &&
		    a.RadialTurn == b.RadialTurn && a.Flip == b.Flip && a.ReflectionOpacity == b.ReflectionOpacity &&
		    a.ReflectionSize == b.ReflectionSize && a.WaveFloorReflection == b.WaveFloorReflection &&
		    a.FloorDb == b.FloorDb && a.CeilingDb == b.CeilingDb && a.MinHz == b.MinHz &&
		    a.MaxHz == b.MaxHz && a.Scale == b.Scale)
		{
			return false;
		}
		return true;
	}

	private static bool LabelsDiffer(SpectrumLook a, SpectrumLook b)
	{
		if (a.Layout == b.Layout && a.DbLabels == b.DbLabels && a.FreqLabels == b.FreqLabels && a.LabelSize == b.LabelSize && a.Engine == b.Engine && a.FloorDb == b.FloorDb && a.CeilingDb == b.CeilingDb && a.Margin == b.Margin && a.MinHz == b.MinHz && a.MaxHz == b.MaxHz && a.Scale == b.Scale && a.FillWidth == b.FillWidth && a.Flip == b.Flip && a.ReflectionOpacity == b.ReflectionOpacity && a.ReflectionSize == b.ReflectionSize && a.BarWidth == b.BarWidth && a.BarGap == b.BarGap)
		{
			return a.Snap != b.Snap;
		}
		return true;
	}

	private Geometry Measure(SpectrumLook look, int width, int height, float scale)
	{
		Geometry geometry = new Geometry
		{
			Look = look,
			Width = width,
			Height = height,
			Bands = _bands,
			Rate = _rate
		};
		EnsureFont(look, scale);
		Gdi.SIZE sIZE = (_sizeDb.cy > 0) ? _sizeDb : TextSheet.Measure(_labelFont, "-60dB");
		Gdi.SIZE sIZE2 = (_sizeFreq.cy > 0) ? _sizeFreq : TextSheet.Measure(_labelFont, "5.5K");
		float num = (float)Math.Round(look.Margin * (double)scale);
		float num2 = num;
		float num3 = num;
		float num4 = (float)width - num;
		float num5 = (float)height - num;
		bool flag = look.Layout != 6;
		geometry.Shown = ((look.Engine == 0 && look.FillWidth) ? FoobarAnalyzer.UsableBands(_bands, _rate) : _bands);
		float num6 = (float)Math.Round(4f * scale);
		geometry.DbColumn = flag && look.DbLabels && (float)sIZE.cx + num6 + (float)(geometry.Shown * 2) <= num4 - num2;
		if (geometry.DbColumn)
		{
			geometry.DbRight = num4 - num6;
			geometry.DbLeft = geometry.DbRight - (float)sIZE.cx;
			num4 = geometry.DbLeft;
		}
		int num7 = ((look.Layout == 2 || look.Layout == 3 || look.Layout == 5) ? (geometry.Shown * 2) : geometry.Shown);
		if (look.Engine == 0)
		{
			geometry.FreqRow = flag && look.FreqLabels && num5 - num3 > (float)sIZE2.cy && num4 - num2 > (float)(num7 * sIZE2.cx);
		}
		else
		{
			geometry.FreqRow = flag && look.FreqLabels && num5 - num3 > (float)(sIZE2.cy * 3);
		}
		if (geometry.FreqRow)
		{
			geometry.FreqTop = num5 - (float)sIZE2.cy;
			num5 = geometry.FreqTop;
		}
		geometry.Left = num2;
		geometry.Top = num3;
		geometry.Right = Math.Max(num2 + 1f, num4);
		geometry.Bottom = Math.Max(num3 + 1f, num5);
		float num8 = geometry.Bottom - geometry.Top;
		switch (look.Layout)
		{
		case 1:
		case 4:
			geometry.Base = geometry.Top + num8 / 2f;
			geometry.Span = num8 / 2f;
			break;
		case 6:
			geometry.CentreX = (geometry.Left + geometry.Right) / 2f;
			geometry.CentreY = (geometry.Top + geometry.Bottom) / 2f;
			geometry.Outer = Math.Min(geometry.Right - geometry.Left, geometry.Bottom - geometry.Top) / 2f * 0.96f;
			geometry.Inner = (float)((double)geometry.Outer * look.RadialInner / 100.0);
			geometry.Span = geometry.Outer - geometry.Inner;
			break;
		default:
			if (look.Flip)
			{
				geometry.Base = geometry.Top;
				geometry.Span = num8;
			}
			else
			{
				bool canReflectBars = (look.Layout == 0 || look.Layout == 2 || look.Layout == 3 || look.Layout == 5) && look.ReflectionOpacity > 0.0;
				bool canReflectWave = (look.Layout == 7 || look.Layout == 8) && look.WaveFloorReflection;
				geometry.Reflection = ((canReflectBars || canReflectWave) ? ((float)((double)num8 * look.ReflectionSize / 100.0)) : 0f);
				geometry.Base = geometry.Bottom - geometry.Reflection;
				geometry.Span = geometry.Base - geometry.Top;
			}
			break;
		}
		Slots(geometry, look, scale);
		return geometry;
	}

	private void Slots(Geometry g, SpectrumLook look, float scale)
	{
		int shown = g.Shown;
		int layout = look.Layout;
		bool flag = layout == 2 || layout == 3 || layout == 5 || layout == 6;
		int num = (g.Slots = (flag ? (shown * 2) : shown));
		g.X0 = new float[num];
		g.X1 = new float[num];
		g.Band = new int[num];
		g.Channel = new int[num];
		for (int i = 0; i < num; i++)
		{
			int num2 = i;
			int num3 = 0;
			if (flag)
			{
				bool flag2 = i < shown;
				int num4 = (flag2 ? i : (i - shown));
				bool flag3 = layout != 3;
				if (layout == 6)
				{
					num2 = num4;
				}
				else if (flag2)
				{
					num2 = (flag3 ? (shown - 1 - num4) : num4);
				}
				else
				{
					num2 = (flag3 ? num4 : (shown - 1 - num4));
				}
				if (layout == 5)
				{
					num3 = ((!flag2) ? 1 : 0);
				}
			}
			g.Band[i] = num2;
			g.Channel[i] = num3;
		}
		if (layout == 6)
		{
			g.Angle = new float[num];
			for (int j = 0; j < num; j++)
			{
				double num5 = ((double)((j < shown) ? j : (j - shown)) + 0.5) / (double)shown * Math.PI;
				g.Angle[j] = (float)(look.RadialTurn * Math.PI / 180.0 + ((j < shown) ? num5 : (0.0 - num5)));
			}
			float num6 = (float)(Math.PI * ((double)g.Inner + 0.25 * (double)g.Span) / (double)shown);
			g.BarThickness = Math.Max(1f, Math.Min(num6 * (float)(look.BarWidth / 100.0), num6 - (float)(look.BarGap * (double)scale)));
			return;
		}
		float num7 = g.Right - g.Left;
		float num8 = (float)Math.Round(look.BarGap * (double)scale);
		if (look.Engine == 0 && !look.FillWidth && look.Snap && !flag)
		{
			float num9 = SnapPitch(Math.Max(num8 + num8, num7 / (float)num));
			for (int k = 0; k < num; k++)
			{
				g.X0[k] = Gdi.Round((float)k * num9 + g.Left);
				g.X1[k] = Math.Max(g.X0[k] + 1f, (float)Gdi.Round((float)(k + 1) * num9 + g.Left) - num8);
			}
			return;
		}
		float num10 = num7 / (float)num;
		float num11 = Math.Max(Math.Min(num10, 1f), Math.Min(num10 * (float)(look.BarWidth / 100.0), num10 - num8));
		for (int l = 0; l < num; l++)
		{
			float barW = num11;
			if (look.TrebleTaper > 0.0 && shown > 1)
			{
				float bandFrac = (float)g.Band[l] / (float)(shown - 1);
				float taper = 1f - (float)(look.TrebleTaper / 100.0) * bandFrac * 0.70f;
				barW = Math.Max(1f, num11 * Math.Max(0.20f, taper));
			}
			float num12;
			float num13;
			if (look.Snap && barW >= num10 - num8)
			{
				num12 = Gdi.Round((float)l * num10 + g.Left);
				num13 = Math.Max(num12 + 1f, (float)Gdi.Round((float)(l + 1) * num10 + g.Left) - num8);
			}
			else if (look.Snap)
			{
				num12 = Gdi.Round(g.Left + (float)l * num10 + (num10 - barW) / 2f);
				num13 = num12 + (float)Math.Max(1, Gdi.Round(barW));
			}
			else
			{
				num12 = g.Left + (float)l * num10 + (num10 - barW) / 2f;
				num13 = num12 + barW;
			}
			g.X0[l] = num12;
			g.X1[l] = num13;
		}
	}

	private static float SnapPitch(float x)
	{
		double num = Math.Floor(Math.Log(x * 0.5f) / Math.Log(2.0));
		float num2 = (float)Math.Pow(2.0, num);
		if (!(num2 > 1f))
		{
			num2 = 1f;
		}
		return (float)(Math.Floor(num2 * x) / (double)num2);
	}

	protected override void DrawBackground(ref Frame f)
	{
		SpectrumLook spectrumLook = (SpectrumLook)f.Look;
		Geometry geo = _geo;
		if (geo == null)
		{
			return;
		}
		Canvas canvas = f.Canvas;
		Vec4 colour = f.Colours[4];
		if (spectrumLook.DbGrid && spectrumLook.Layout != 6)
		{
			float num = Math.Max(1f, (float)Math.Round(f.Scale));
			int num2 = (int)Math.Floor(spectrumLook.CeilingDb / 10.0) * 10;
			while ((double)num2 >= spectrumLook.FloorDb)
			{
				float v = (float)Norm(spectrumLook, num2);
				int num3 = GridRows(geo, spectrumLook, v, out var y, out var y2);
				for (int i = 0; i < num3; i++)
				{
					float num4 = (float)Math.Round((i == 0) ? y : y2);
					canvas.Rect(RectShape.Solid(geo.Left, num4, geo.Right, num4 + num, colour));
				}
				num2 -= 10;
			}
		}
		if (!_labelsBuilt)
		{
			BuildLabels(f.Gpu, spectrumLook, geo);
		}
		_labels.Draw(canvas, f.Colours[3]);
		if (spectrumLook.Layout == 6 && spectrumLook.RadialArt)
		{
			DrawCover(canvas, geo, ref f);
		}
	}

	private void DrawCover(Canvas c, Geometry g, ref Frame f)
	{
		GpuTexture gpuTexture = Cover(ref f);
		float num = g.Inner - 7f * f.Scale;
		if (gpuTexture != null && !(num < 8f))
		{
			num += Math.Min(0.04f * num, 4f * f.Scale) * _beat.Pulse;
			float num2 = 0f;
			float num3 = 0f;
			float u = 1f;
			float v = 1f;
			if (gpuTexture.Width > gpuTexture.Height)
			{
				num2 = (1f - (float)gpuTexture.Height / (float)gpuTexture.Width) / 2f;
				u = 1f - num2;
			}
			else if (gpuTexture.Height > gpuTexture.Width)
			{
				num3 = (1f - (float)gpuTexture.Width / (float)gpuTexture.Height) / 2f;
				v = 1f - num3;
			}
			c.Quad(gpuTexture, TexQuad.Make(g.CentreX - num, g.CentreY - num, g.CentreX + num, g.CentreY + num, num2, num3, u, v, new Vec4(1f, 1f, 1f, 1f), mask: false, (float)(((SpectrumLook)f.Look).BarOpacity / 100.0)).Round());
		}
	}

	private static int GridRows(Geometry g, SpectrumLook look, float v, out float y0, out float y1)
	{
		if (look.Layout == 1 || look.Layout == 4)
		{
			y0 = g.Base - v * g.Span;
			y1 = g.Base + v * g.Span;
			if (!(v > 0f))
			{
				return 1;
			}
			return 2;
		}
		y0 = (look.Flip ? (g.Base + v * g.Span) : (g.Base - v * g.Span));
		y1 = y0;
		return 1;
	}

}
