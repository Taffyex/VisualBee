using System;

namespace MusicBeePlugin;

internal sealed partial class SpectrumView
{
	private static Vec4 Gradient(SpectrumLook look, Vec4[] colours, float t)
	{
		Vec4 vec = colours[10];
		if (look.Colouring == 4) return vec;
		if (!look.UseMiddle) return Vec4.Lerp(vec, colours[12], t);
		float num = (float)(look.MiddleAt / 100.0);
		return (t < num) ? Vec4.Lerp(vec, colours[11], t / num) : Vec4.Lerp(colours[11], colours[12], (t - num) / (1f - num));
	}

	private static Vec4 TipColour(SpectrumLook look, Vec4[] colours, Geometry g, int slot, int band, float v)
	{
		if (look.Colouring == 4)
		{
			return Vec4.Lerp(colours[10], colours[12], v);
		}
		if (look.Colouring == 2)
		{
			float amount = ((g.Shown > 1) ? ((float)band / (float)(g.Shown - 1)) : 0f);
			if (look.UseMiddle)
			{
				float num = (float)(look.MiddleAt / 100.0);
				return (amount < num) ? Vec4.Lerp(colours[10], colours[11], amount / Math.Max(0.001f, num)) : Vec4.Lerp(colours[11], colours[12], (amount - num) / Math.Max(0.001f, 1f - num));
			}
			return Vec4.Lerp(colours[10], colours[12], amount);
		}
		return colours[12];
	}

	private static void Stops(ref RectShape r, SpectrumLook look, Vec4[] colours, float from, float to)
	{
		if (look.Colouring != 4)
		{
			if (look.UseMiddle)
			{
				r = r.Gradient(colours[10], colours[11], colours[12], (float)(look.MiddleAt / 100.0), from, to);
			}
			else
			{
				r = r.Gradient(colours[10], colours[12], from, to);
			}
		}
	}

	private void Reflection(Canvas c, SpectrumLook look, Vec4[] colours, Geometry g, int slot, int band, float v, float x0, float x1, float opacity)
	{
		float num = Math.Min(v * g.Span, g.Reflection);
		if (!(num <= 0.5f))
		{
			float opacity2 = (float)(look.ReflectionOpacity / 100.0) * opacity;
			RectShape rectShape = Bar(look, colours, g, slot, band, v, x0, x1, g.Base, 1f, 1f);
			rectShape.Rect = new Vec4(x0, g.Base + Math.Max(1f, PanelLayout.Scale), x1, g.Base + num);
			float falloff = Math.Max(0.15f, 1f - num / Math.Max(1f, g.Reflection));
			rectShape.C0 = rectShape.C0.Faded(opacity2);
			rectShape.C1 = rectShape.C1.Faded(opacity2 * 0.70f);
			rectShape.C2 = rectShape.C2.Faded(opacity2 * falloff * 0.45f);
			if (look.LacquerHorizon) rectShape = rectShape.Lacquer();
			c.Rect(rectShape);
		}
	}

	private void DrawPeak(Canvas c, SpectrumLook look, Vec4[] colours, Geometry g, int slot, int band, int channel, float x0, float x1, float outward, ref Frame f)
	{
		double db = _peak[channel][band];
		float num = (float)Norm(look, db);
		if (num > 0f)
		{
			float opacity = _peakAlpha[channel][band] * (float)(look.PeakOpacity / 100.0);
			Vec4 colour = (look.PeakBarColour ? TipColour(look, colours, g, slot, band, num) : colours[13]).Faded(opacity);
			float num2 = (float)(look.PeakSize * (double)f.Scale);
			float num3 = g.Base + outward * num * g.Span;
			if (look.Snap)
			{
				num2 = Math.Max(1f, (float)Math.Round(num2));
				num3 = (float)Math.Round(num3);
			}
			float num4 = (look.Snap ? num3 : (num3 - num2 / 2f));
			switch (look.PeakStyle)
			{
			case 2: c.Sprite(SpriteShape.Make((x0 + x1) / 2f, num3, num2 / 2f, SpriteKind.Disc, colour)); break;
			case 1: c.Rect(RectShape.Solid(x0, (outward < 0f) ? (num3 - num2) : num3, x1, (outward < 0f) ? num3 : (num3 + num2), colour).Rounded(Math.Min((float)(look.Radius * (double)f.Scale), num2 / 2f), look.Snap ? 0.5f : 1f)); break;
			case 3:
				c.Rect(RectShape.Solid(x0, num3 - num2 / 2f, x1, num3 + num2 / 2f, colour.Faded(0.35f)).Rounded(num2, num2 * 2.5f));
				c.Rect(RectShape.Solid(x0, num3 - num2 / 2f, x1, num3 + num2 / 2f, colour).Rounded(num2 / 2f));
				break;
			default: c.Rect(RectShape.Solid(x0, num4, x1, num4 + num2, colour).Rounded(0f, look.Snap ? 0.5f : 1f)); break;
			}
		}
	}
}
