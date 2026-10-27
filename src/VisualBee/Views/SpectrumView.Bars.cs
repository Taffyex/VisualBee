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

	private static Vec4 TipColour(SpectrumLook look, Vec4[] colours, Geometry g, int slot, int band, float v) => look.Colouring switch
	{
		1 => Gradient(look, colours, 1f),
		2 => Gradient(look, colours, (g.Bands > 1) ? ((float)band / (float)(g.Bands - 1)) : 0f),
		0 or 3 => Gradient(look, colours, v),
		_ => colours[10]
	};

	private RectShape Bar(SpectrumLook look, Vec4[] colours, Geometry g, int slot, int band, float v, float x0, float x1, float baseY, float outward, float opacity)
	{
		float num = v * g.Span;
		float num2 = baseY + outward * num;
		if (look.Snap)
		{
			num2 = (float)Math.Round(num2);
		}
		float top = Math.Min(baseY, num2);
		float bottom = Math.Max(baseY, num2);
		RectShape r = RectShape.Solid(x0, top, x1, bottom, colours[10]);
		float to = baseY + outward * g.Span;
		switch (look.Colouring)
		{
		case 0: Stops(ref r, look, colours, baseY, to); break;
			
		case 1: Stops(ref r, look, colours, baseY, num2); break;
		case 2:
		{
			Vec4 vec2 = Gradient(look, colours, (g.Bands > 1) ? ((float)band / (float)(g.Bands - 1)) : 0f);
			r = r.Gradient(vec2, vec2, baseY, to);
			break;
		}
		case 3:
		{
			Vec4 vec = Gradient(look, colours, v);
			r = r.Gradient(vec, vec, baseY, to);
			break;
		}
		}
		r.C0 = r.C0.Faded(opacity);
		r.C1 = r.C1.Faded(opacity);
		r.C2 = r.C2.Faded(opacity);

		float halfWidth = (x1 - x0) / 2f;
		float cornerRound = Math.Min((float)(look.Radius * (double)PanelLayout.Scale), halfWidth);

		switch (look.BarStyle)
		{
		case SpectrumLook.StyleGlass:
			r = r.Rounded(halfWidth, look.Snap ? 0.5f : 1f).Corners((outward < 0f) ? halfWidth : 0f, (outward < 0f) ? 0f : halfWidth);
			break;
		case SpectrumLook.StylePills:
			r = r.Rounded(halfWidth, look.Snap ? 0.5f : 1f).Corners((outward < 0f) ? halfWidth : 0f, (outward < 0f) ? 0f : halfWidth).Pill();
			break;
		case SpectrumLook.StyleNixie:
			float nH = Math.Max(2f, (float)(look.LedSize * (double)PanelLayout.Scale));
			float nGap = Math.Max(1f, (float)(look.LedGap * (double)PanelLayout.Scale));
			r = r.Vfd(nH + nGap, nH, ((outward < 0f) ? (baseY + nGap) : baseY), v, look.HdrIncandescence, look.VfdMesh);
			break;
		case SpectrumLook.StyleLed:
			float num4 = (float)(look.LedSize * (double)PanelLayout.Scale);
			float num5 = (float)(look.LedGap * (double)PanelLayout.Scale);
			r = r.Segments(num4 + num5, num4, ((outward < 0f) ? (baseY + num5) : baseY)).Flags(RectFlags.ColourPerSegment);
			if (look.VfdMesh) r = r.Flags(RectFlags.VfdMesh);
			break;
		case SpectrumLook.StyleSolid:
		default:
			r = r.Rounded(cornerRound, look.Snap ? 0.5f : 1f).Corners((outward < 0f) ? cornerRound : 0f, (outward < 0f) ? 0f : cornerRound);
			if (look.OutlineWidth > 0) r = r.Outline((float)(look.OutlineWidth * (double)PanelLayout.Scale));
			break;
		}
		return r;
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

	private void DrawBars(Canvas c, SpectrumLook look, Geometry g, ref Frame f)
	{
		float num = (float)(look.BarOpacity / 100.0);
		bool flag = look.Layout == 4;
		bool flag2 = look.Layout == 1;
		float num2 = ((look.Flip && !flag2 && !flag) ? 1f : (-1f));
		bool flag3 = look.BarStyle != SpectrumLook.StyleHidden;

		if (look.CenterLaser && (flag2 || flag))
		{
			Vec4 laserCol = f.Colours[12].Faded(0.85f);
			c.Rect(RectShape.Solid(g.Left, g.Base - 1f, g.Right, g.Base + 1f, laserCol));
			c.Rect(RectShape.Solid(g.Left, g.Base - 3.5f, g.Right, g.Base + 3.5f, laserCol.Faded(0.25f)));
		}

		if (look.LacquerHorizon && g.Reflection > 0f && !look.Flip)
		{
			c.Rect(RectShape.Solid(0f, g.Base - 0.75f, g.Width, g.Base + 0.75f, new Vec4(0.2f, 0.45f, 0.75f, 0.25f)));
		}

		bool isGlassStyle = look.BarStyle == SpectrumLook.StyleGlass;
		bool isNixieStyle = look.BarStyle == SpectrumLook.StyleNixie;
		bool showTube = isGlassStyle && look.GlassTubeMode != 2;

		for (int i = 0; i < g.Slots; i++)
		{
			int num3 = g.Band[i];
			if (num3 >= _bands)
			{
				continue;
			}
			float x = g.X0[i];
			float x2 = g.X1[i];
			float halfBarWidth = (x2 - x) / 2f;
			int num4 = ((!flag) ? g.Channel[i] : 0);
			int num5 = (flag ? 1 : g.Channel[i]);
			for (int j = num4; j <= num5; j++)
			{
				float num6 = (float)Norm(look, _level[j][num3]);

				float num7 = (flag ? ((j == 0) ? (-1f) : 1f) : num2);

				float pTop = Math.Min(g.Base, g.Base + num7 * g.Span);
				float pBottom = Math.Max(g.Base, g.Base + num7 * g.Span);

				float segH = (float)(look.LedSize * (double)PanelLayout.Scale);
				if (segH < 2f) segH = Math.Max(2f, (x2 - x) * 0.35f);
				float segGap = (float)(look.LedGap * (double)PanelLayout.Scale);
				if (segGap < 1f) segGap = Math.Max(1f, segH * 0.35f);
				float origin = ((num7 < 0f) ? (g.Base + segGap) : g.Base);

				if (flag3)
				{
					// 1. Ghost Rungs (only draw beyond lit height)
					float ghostAlpha = (float)(look.LedGhost / 100.0) * num;
					if (ghostAlpha > 0f && (isGlassStyle || isNixieStyle || look.BarStyle == SpectrumLook.StyleLed))
					{
						float ghostReach = (look.GhostRange > 0.0) ? Math.Min(1.0f, num6 + (float)(look.GhostRange / 100.0)) : 1.0f;
						if (ghostReach > num6 + 0.005f)
						{
							float gAct = g.Base + num7 * num6 * g.Span;
							float gEnd = g.Base + num7 * ghostReach * g.Span;
							Vec4 ghostCol = f.Colours[10].Faded(ghostAlpha * 0.25f);
							c.Rect(RectShape.Solid(x, Math.Min(gAct, gEnd), x2, Math.Max(gAct, gEnd), ghostCol).Vfd(segH + segGap, segH, origin, 0f, false, look.VfdMesh));
							if (flag2)
							{
								float mgAct = g.Base - num7 * num6 * g.Span;
								float mgEnd = g.Base - num7 * ghostReach * g.Span;
								c.Rect(RectShape.Solid(x, Math.Min(mgAct, mgEnd), x2, Math.Max(mgAct, mgEnd), ghostCol).Vfd(segH + segGap, segH, origin, 0f, false, look.VfdMesh));
							}
						}
					}

					float wall = (float)(look.GlassWall / 100.0);
					float tint = (float)(look.GlassTint / 100.0);
					float spec = (float)(look.GlassSpecular / 100.0);
					float caust = (float)(look.GlassCaustics / 100.0);
					float refr = (float)(look.GlassRefraction / 100.0);

					// 2. Active Interior Fill (drawn BEFORE foreground glass envelope)
					if (num6 > 0f)
					{
						if (isGlassStyle)
						{
							if (look.GlassFill == 0)
							{
								RectShape innerC = Bar(look, f.Colours, g, i, num3, num6, x, x2, g.Base, num7, num);
								if (!showTube) innerC = innerC.Glass(num6 * caust, refr, wall, tint, spec);
								c.Rect(innerC);
								if (flag2)
								{
									RectShape mInnerC = Bar(look, f.Colours, g, i, num3, num6, x, x2, g.Base, 0f - num7, num);
									if (!showTube) mInnerC = mInnerC.Glass(num6 * caust, refr, wall, tint, spec);
									c.Rect(mInnerC);
								}
							}
							else if (look.GlassFill == 1)
							{
								RectShape lit = Bar(look, f.Colours, g, i, num3, num6, x, x2, g.Base, num7, num)
									.Vfd(segH + segGap, segH, origin, num6, look.HdrIncandescence, look.VfdMesh);
								c.Rect(lit);
								if (flag2) c.Rect(Bar(look, f.Colours, g, i, num3, num6, x, x2, g.Base, 0f - num7, num).Vfd(segH + segGap, segH, origin, num6, look.HdrIncandescence, look.VfdMesh));
							}
							else if (look.GlassFill == 2)
							{
								float inset = (x2 - x) * 0.12f;
								c.Rect(Bar(look, f.Colours, g, i, num3, num6, x + inset, x2 - inset, g.Base, num7, num * 0.95f));
								if (flag2) c.Rect(Bar(look, f.Colours, g, i, num3, num6, x + inset, x2 - inset, g.Base, 0f - num7, num * 0.95f));
							}
							else if (look.GlassFill == 3)
							{
								float filamentW = Math.Max(1.5f, (x2 - x) * 0.22f);
								float mid = (x + x2) * 0.5f;
								float leftFil = mid - filamentW * 1.1f;
								float rightFil = mid + filamentW * 0.1f;
								c.Rect(Bar(look, f.Colours, g, i, num3, num6, leftFil, leftFil + filamentW, g.Base, num7, num));
								c.Rect(Bar(look, f.Colours, g, i, num3, num6, rightFil, rightFil + filamentW, g.Base, num7, num));
								if (flag2)
								{
									c.Rect(Bar(look, f.Colours, g, i, num3, num6, leftFil, leftFil + filamentW, g.Base, 0f - num7, num));
									c.Rect(Bar(look, f.Colours, g, i, num3, num6, rightFil, rightFil + filamentW, g.Base, 0f - num7, num));
								}
							}
						}
						else if (isNixieStyle)
						{
							c.Rect(Bar(look, f.Colours, g, i, num3, num6, x, x2, g.Base, num7, num));
							if (flag2) c.Rect(Bar(look, f.Colours, g, i, num3, num6, x, x2, g.Base, 0f - num7, num));
						}
						else
						{
							c.Rect(Bar(look, f.Colours, g, i, num3, num6, x, x2, g.Base, num7, num));
							if (flag2) c.Rect(Bar(look, f.Colours, g, i, num3, num6, x, x2, g.Base, 0f - num7, num));
						}

						// Tip Highlight Apex Cap
						if (look.TipHighlight)
						{
							float tipY = g.Base + num7 * num6 * g.Span;
							float tipH = Math.Max(2f, Math.Min(5f, halfBarWidth * 0.35f));
							float t0 = (num7 < 0f) ? tipY : tipY - tipH;
							float t1 = (num7 < 0f) ? tipY + tipH : tipY;
							Vec4 tipCol = Vec4.Lerp(TipColour(look, f.Colours, g, i, num3, num6), new Vec4(1f, 1f, 1f, 1f), 0.65f);
							c.Rect(RectShape.Solid(x, t0, x2, t1, tipCol).Rounded(halfBarWidth * 0.5f, 0.5f));
							if (flag2)
							{
								float mtY = g.Base - num7 * num6 * g.Span;
								float mt0 = (num7 > 0f) ? mtY : mtY - tipH;
								float mt1 = (num7 > 0f) ? mtY + tipH : mtY;
								c.Rect(RectShape.Solid(x, mt0, x2, mt1, tipCol).Rounded(halfBarWidth * 0.5f, 0.5f));
							}
						}
					}

					// 3. Foreground Glass Capsule / Tube Envelope (drawn OVER interior)
					if (showTube)
					{
						bool dynamicCap = look.GlassTubeMode == 1;
						if (!dynamicCap || num6 > 0.005f)
						{
							float spanH = dynamicCap ? Math.Max(halfBarWidth * 2f, num6 * g.Span) : g.Span;
							float capTop = Math.Min(g.Base, g.Base + num7 * spanH);
							float capBottom = Math.Max(g.Base, g.Base + num7 * spanH);
							float glassOp = (float)(look.GlassOpacity / 100.0) * num;
							Vec4 glassTint = f.Colours[11].Faded(glassOp);
							RectShape pillar = RectShape.Solid(x, capTop, x2, capBottom, glassTint)
								.Rounded(halfBarWidth, look.Snap ? 0.5f : 1f)
								.Corners((num7 < 0f) ? halfBarWidth : 0f, (num7 < 0f) ? 0f : halfBarWidth)
								.Glass(num6 * caust, refr, wall, tint, spec)
								.Flags(RectFlags.Pill3D);
							c.Rect(pillar);
							if (flag2)
							{
								float mcapTop = Math.Min(g.Base, g.Base - num7 * spanH);
								float mcapBottom = Math.Max(g.Base, g.Base - num7 * spanH);
								RectShape mPillar = RectShape.Solid(x, mcapTop, x2, mcapBottom, glassTint)
									.Rounded(halfBarWidth, look.Snap ? 0.5f : 1f)
									.Corners((num7 > 0f) ? halfBarWidth : 0f, (num7 > 0f) ? 0f : halfBarWidth)
									.Glass(num6 * caust, refr, wall, tint, spec)
									.Flags(RectFlags.Pill3D);
								c.Rect(mPillar);
							}
						}
					}
				}

				if (g.Reflection > 0f && !look.Flip && num6 > 0f)
				{
					Reflection(c, look, f.Colours, g, i, num3, num6, x, x2, num);
				}

				if (look.Peaks && look.PeakStyle != 2)
				{
					DrawPeak(c, look, f.Colours, g, i, num3, j, x, x2, num7, ref f);
					if (flag2)
					{
						DrawPeak(c, look, f.Colours, g, i, num3, j, x, x2, 0f - num7, ref f);
					}
				}
			}
		}

		bool drawPools = look.LacquerHorizon && g.Reflection > 0f && !look.Flip;
		bool drawDiscPeaks = look.Peaks && look.PeakStyle == 2;
		if (drawPools || drawDiscPeaks)
		{
			for (int k = 0; k < g.Slots; k++)
			{
				int b = g.Band[k];
				if (b >= _bands) continue;
				float x0 = g.X0[k], x1 = g.X1[k];
				int ch0 = (!flag) ? g.Channel[k] : 0;
				int ch1 = flag ? 1 : g.Channel[k];
				for (int ch = ch0; ch <= ch1; ch++)
				{
					float v = (float)Norm(look, _level[ch][b]);
					if (drawPools && v > 0f)
					{
						float poolR = (x1 - x0) * 0.75f;
						Vec4 poolCol = TipColour(look, f.Colours, g, k, b, v).Faded(num * 0.22f * v);
						c.Sprite(SpriteShape.Make((x0 + x1) * 0.5f, g.Base + 1.5f, poolR, SpriteKind.Glow, poolCol));
					}
					if (drawDiscPeaks)
					{
						float dir = flag ? ((ch == 0) ? (-1f) : 1f) : num2;
						DrawPeak(c, look, f.Colours, g, k, b, ch, x0, x1, dir, ref f);
						if (flag2) DrawPeak(c, look, f.Colours, g, k, b, ch, x0, x1, 0f - dir, ref f);
					}
				}
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
			if (look.LacquerHorizon)
			{
				rectShape = rectShape.Lacquer();
			}
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
