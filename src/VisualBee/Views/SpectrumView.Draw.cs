using System;

namespace MusicBeePlugin;

internal sealed partial class SpectrumView
{
	private float[] _radialRing = new float[194];

	private float[] _smoothRefl = new float[0];

	protected override void DrawContent(ref Frame f)
	{
		SpectrumLook spectrumLook = (SpectrumLook)f.Look;
		Geometry geo = _geo;
		if (geo != null)
		{
			Canvas canvas = f.Canvas;
			if (spectrumLook.Layout == 7)
			{
				DrawWave(canvas, spectrumLook, geo, ref f);
			}
			else if (spectrumLook.Layout == 8)
			{
				DrawOscilloscope(canvas, spectrumLook, geo, ref f);
			}
			else if (spectrumLook.Layout == 6)
			{
				DrawRadial(canvas, spectrumLook, geo, ref f);
			}
			else
			{
				DrawBars(canvas, spectrumLook, geo, ref f);
			}
			DrawParticles(canvas, spectrumLook, ref f);
		}
	}

	private void DrawRadial(Canvas c, SpectrumLook look, Geometry g, ref Frame f)
	{
		float num = (float)(look.BarOpacity / 100.0);
		if (g.Inner > 4f)
		{
			float num2 = g.Inner - 3f * f.Scale;
			for (int i = 0; i <= 96; i++)
			{
				double num3 = (double)(i * 2) * Math.PI / 96.0;
				_radialRing[2 * i] = g.CentreX + (float)Math.Sin(num3) * num2;
				_radialRing[2 * i + 1] = g.CentreY - (float)Math.Cos(num3) * num2;
			}
			Vec4 colour = Gradient(look, f.Colours, 0f).Faded(num * (0.35f + 0.65f * _beat.Pulse));
			c.Polyline(_radialRing, 97, Stroke.Solid(2f * f.Scale, colour));
		}
		bool flag = look.BarStyle != SpectrumLook.StyleHidden;
		float barThickness = g.BarThickness;
		LineCap cap = ((look.Radius > 0.5) ? LineCap.Round : LineCap.Butt);
		for (int j = 0; j < g.Slots; j++)
		{
			int num4 = g.Band[j];
			if (num4 >= _bands)
			{
				continue;
			}
			float num5 = (float)Norm(look, _level[0][num4]);
			float num6 = g.Angle[j];
			float num7 = (float)Math.Sin(num6);
			float num8 = 0f - (float)Math.Cos(num6);
			float inner = g.Inner;
			float num9 = g.Inner + num5 * g.Span;
			if (flag && num5 > 0f)
			{
				Vec4 vec;
				Vec4 vec2;
				switch (look.Colouring)
				{
				case 0:
					vec = Gradient(look, f.Colours, 0f);
					vec2 = Gradient(look, f.Colours, num5);
					break;
				case 1:
					vec = Gradient(look, f.Colours, 0f);
					vec2 = Gradient(look, f.Colours, 1f);
					break;
				default:
					vec = (vec2 = TipColour(look, f.Colours, g, j, num4, num5));
					break;
				}
				LineShape l = LineShape.Make(g.CentreX + num7 * inner, g.CentreY + num8 * inner, g.CentreX + num7 * num9, g.CentreY + num8 * num9, barThickness, vec.Faded(num), vec2.Faded(num), cap);
				if (look.BarStyle == SpectrumLook.StyleLed || look.BarStyle == SpectrumLook.StyleNixie)
				{
					float num10 = (float)(look.LedSize * (double)f.Scale);
					float num11 = (float)(look.LedGap * (double)f.Scale);
					l = l.Dashes(num10 + num11, num10);
				}
				c.Line(l);
			}
			if (!look.Peaks)
			{
				continue;
			}
			float num12 = (float)Norm(look, _peak[0][num4]);
			if (num12 > 0f)
			{
				float num13 = g.Inner + num12 * g.Span;
				Vec4 vec3 = (look.PeakBarColour ? TipColour(look, f.Colours, g, j, num4, num12) : f.Colours[13]).Faded(_peakAlpha[0][num4] * (float)(look.PeakOpacity / 100.0));
				float num14 = (float)(look.PeakSize * (double)f.Scale);
				if (look.PeakStyle == 2)
				{
					c.Sprite(SpriteShape.Make(g.CentreX + num7 * num13, g.CentreY + num8 * num13, num14 / 2f, SpriteKind.Disc, vec3));
				}
				else
				{
					c.Line(LineShape.Make(g.CentreX + num7 * num13, g.CentreY + num8 * num13, g.CentreX + num7 * (num13 + num14), g.CentreY + num8 * (num13 + num14), barThickness, vec3, vec3, LineCap.Butt));
				}
			}
		}
	}

	private void DrawWave(Canvas c, SpectrumLook look, Geometry g, ref Frame f)
	{
		int shown = g.Shown;
		if (shown < 2)
		{
			return;
		}
		if (_curve.Length < shown * 2)
		{
			_curve = new float[shown * 2];
		}
		int count = Smoothed(look, g, _level[0], shown);
		float num = (look.Flip ? (g.Base + g.Span) : (g.Base - g.Span));
		float num2 = (float)(look.BarOpacity / 100.0);
		if (look.CurveFill > 0.0)
		{
			Stroke fill = new Stroke
			{
				Width = 1f,
				Softness = 1f,
				Opacity = (float)(look.CurveFill / 100.0) * num2,
				Colouring = ((look.Colouring != 2) ? StrokeColouring.AlongY : StrokeColouring.AlongX),
				C0 = f.Colours[10],
				C1 = (look.UseMiddle ? f.Colours[11] : f.Colours[12]),
				C2 = f.Colours[12],
				Middle = (look.UseMiddle ? ((float)(look.MiddleAt / 100.0)) : (-1f)),
				From = ((look.Colouring == 2) ? g.Left : g.Base),
				To = ((look.Colouring == 2) ? g.Right : num),
				Baseline = g.Base
			};
			if (look.Colouring == 4)
			{
				fill.Colouring = StrokeColouring.Solid;
				fill.C0 = f.Colours[10];
			}
			c.Area(_smooth, count, fill);
		}
		c.Blend = BlendMode.Max;
		Stroke stroke = Stroke.Solid((float)(look.CurveWidth * (double)f.Scale), f.Colours[15].Faded(num2));
		c.Polyline(_smooth, count, stroke);
		c.Blend = BlendMode.Over;

		if (look.WaveFloorReflection && g.Reflection > 0f)
		{
			if (_smoothRefl.Length < count * 2)
			{
				_smoothRefl = new float[count * 2];
			}
			for (int k = 0; k < count; k++)
			{
				_smoothRefl[2 * k] = _smooth[2 * k];
				float dist = _smooth[2 * k + 1] - g.Base;
				_smoothRefl[2 * k + 1] = g.Base - dist * 0.40f;
			}
			Stroke reflStroke = Stroke.Solid(1.5f * f.Scale, f.Colours[15].Faded(num2 * 0.28f));
			c.Polyline(_smoothRefl, count, reflStroke);
		}

		if (look.Peaks)
		{
			count = Smoothed(look, g, _peak[0], shown);
			Stroke stroke2 = Stroke.Solid((float)(Math.Max(1.0, look.PeakSize * 0.5) * (double)f.Scale), f.Colours[13].Faded((float)(look.PeakOpacity / 100.0) * 0.6f));
			c.Polyline(_smooth, count, stroke2);
		}
	}

	private int Smoothed(SpectrumLook look, Geometry g, double[] levels, int n)
	{
		float num = (look.Flip ? 1f : (-1f));
		for (int i = 0; i < n; i++)
		{
			_curve[2 * i] = (g.X0[i] + g.X1[i]) / 2f;
			_curve[2 * i + 1] = g.Base + num * (float)Norm(look, levels[i]) * g.Span;
		}
		int num2 = (n - 1) * 4 + 1;
		if (_smooth.Length < num2 * 2)
		{
			_smooth = new float[num2 * 2];
		}
		int num3 = 0;
		for (int j = 0; j < n - 1; j++)
		{
			int num4 = Math.Max(0, j - 1);
			int num5 = j;
			int num6 = j + 1;
			int num7 = Math.Min(n - 1, j + 2);
			for (int k = 0; k < 4; k++)
			{
				float t = (float)k / 4f;
				_smooth[num3++] = CatmullRom(_curve[2 * num4], _curve[2 * num5], _curve[2 * num6], _curve[2 * num7], t);
				float val = CatmullRom(_curve[2 * num4 + 1], _curve[2 * num5 + 1], _curve[2 * num6 + 1], _curve[2 * num7 + 1], t);
				_smooth[num3++] = (look.Flip ? Math.Max(g.Base, Math.Min(g.Base + g.Span, val)) : Math.Min(g.Base, Math.Max(g.Base - g.Span, val)));
			}
		}
		_smooth[num3++] = _curve[2 * (n - 1)];
		_smooth[num3++] = _curve[2 * (n - 1) + 1];
		return num2;
	}

	private static float CatmullRom(float p0, float p1, float p2, float p3, float t)
	{
		return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (3f * (p1 - p2) + p3 - p0) * t * t * t);
	}
}
