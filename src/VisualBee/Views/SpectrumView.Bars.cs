using System;

namespace MusicBeePlugin;

internal sealed partial class SpectrumView
{
	private static RectShape Bar(SpectrumLook look, Vec4[] colours, Geometry g, int slot, int band, float v, float x0, float x1, float baseY, float outward, float opacity)
	{
		float to = baseY + outward * v * g.Span;
		RectShape r = RectShape.Solid(x0, Math.Min(baseY, to), x1, Math.Max(baseY, to), colours[10]);
		switch (look.Colouring)
		{
		case 0:
			Stops(ref r, look, colours, g.Base, g.Base + outward * g.Span);
			break;
		case 1:
			Stops(ref r, look, colours, baseY, to);
			break;
		case 2:
		{
			Vec4 c = TipColour(look, colours, g, slot, band, v);
			r = r.Gradient(c, c, baseY, to);
			break;
		}
		case 3:
		{
			Vec4 c = Gradient(look, colours, v);
			r = r.Gradient(c, c, baseY, to);
			break;
		}
		case 4:
			r = r.Gradient(colours[10], colours[10], baseY, to);
			break;
		}
		r.C0 = r.C0.Faded(opacity);
		r.C1 = r.C1.Faded(opacity);
		r.C2 = r.C2.Faded(opacity);

		float halfWidth = (x1 - x0) / 2f;
		float cornerRound = Math.Min((float)(look.Radius * (double)PanelLayout.Scale), halfWidth);
		float hdrDrive = look.HdrIncandescence ? (float)(1.0 + look.HdrDrive / 100.0) : 0f;

		switch (look.BarStyle)
		{
		case SpectrumLook.StyleGlass:
			r = r.Rounded(cornerRound, look.Snap ? 0.5f : 1f).Corners((outward < 0f) ? cornerRound : 0f, (outward < 0f) ? 0f : cornerRound);
			break;
		case SpectrumLook.StylePills:
			r = r.Rounded(halfWidth, look.Snap ? 0.5f : 1f).Corners((outward < 0f) ? halfWidth : 0f, (outward < 0f) ? 0f : halfWidth).Pill();
			if (look.OutlineWidth > 0.0) r = r.Outline((float)(look.OutlineWidth * (double)PanelLayout.Scale));
			if (look.HdrIncandescence) r = r.Hdr(v * hdrDrive);
			break;
		case SpectrumLook.StyleNixie:
			float nH = Math.Max(2f, (float)(look.LedSize * (double)PanelLayout.Scale));
			float nGap = Math.Max(1f, (float)(look.LedGap * (double)PanelLayout.Scale));
			r = r.Vfd(nH + nGap, nH, ((outward < 0f) ? (baseY + nGap) : baseY), v * Math.Max(1f, hdrDrive), look.HdrIncandescence, look.VfdMesh);
			break;
		case SpectrumLook.StyleLed:
			float num4 = (float)(look.LedSize * (double)PanelLayout.Scale);
			float num5 = (float)(look.LedGap * (double)PanelLayout.Scale);
			r = r.Segments(num4 + num5, num4, ((outward < 0f) ? (baseY + num5) : baseY)).Flags(RectFlags.ColourPerSegment);
			if (look.VfdMesh) r = r.Flags(RectFlags.VfdMesh);
			if (look.HdrIncandescence) r = r.Hdr(v * hdrDrive);
			break;
		case SpectrumLook.StyleSolid:
		default:
			r = r.Rounded(cornerRound, look.Snap ? 0.5f : 1f).Corners((outward < 0f) ? cornerRound : 0f, (outward < 0f) ? 0f : cornerRound);
			if (look.OutlineWidth > 0.0) r = r.Outline((float)(look.OutlineWidth * (double)PanelLayout.Scale));
			if (look.HdrIncandescence) r = r.Hdr(v * hdrDrive);
			break;
		}
		return r;
	}

	private void DrawBars(Canvas c, SpectrumLook look, Geometry g, ref Frame f)
	{
		float num = (float)(look.BarOpacity / 100.0);
		bool flag = look.Layout == 4;
		bool flag2 = look.Layout == 1;
		float num2 = ((look.Flip && !flag2 && !flag) ? 1f : (-1f));
		bool flag3 = look.BarStyle != SpectrumLook.StyleHidden;

		if (look.CenterLaser && (flag2 || flag) && look.CenterLaserIntensity > 0.0)
		{
			float li = (float)(look.CenterLaserIntensity / 100.0) * (0.85f + 0.35f * _beat.Pulse);
			float hdrScale = look.HdrIncandescence ? (1f + (float)(look.HdrDrive / 100.0) * 0.45f) : 1f;
			Vec4 coreCol = f.Colours[12] * (Math.Min(2.4f, 0.7f + 0.9f * li) * hdrScale);
			coreCol.W = Math.Min(1f, 0.85f * li);
			float coreH = Math.Max(0.75f, (0.8f + 0.6f * li) * f.Scale);
			float haloH = (2.5f + 5.0f * li) * f.Scale;
			c.Rect(RectShape.Solid(g.Left, g.Base - haloH, g.Right, g.Base + haloH, f.Colours[12].Faded(Math.Min(0.65f, 0.22f * li))).Rounded(haloH, haloH));
			c.Rect(RectShape.Solid(g.Left, g.Base - coreH, g.Right, g.Base + coreH, coreCol));
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
			if (num3 >= _bands) continue;
			float x = g.X0[i];
			float x2 = g.X1[i];
			float halfBarWidth = (x2 - x) / 2f;
			int num4 = ((!flag) ? g.Channel[i] : 0);
			int num5 = (flag ? 1 : g.Channel[i]);
			for (int j = num4; j <= num5; j++)
			{
				float num6 = (float)Norm(look, _level[j][num3]);
				float num7 = (flag ? ((j == 0) ? (-1f) : 1f) : num2);

				float segH = (float)(look.LedSize * (double)PanelLayout.Scale);
				if (segH < 2f) segH = Math.Max(2f, (x2 - x) * 0.35f);
				float segGap = (float)(look.LedGap * (double)PanelLayout.Scale);
				if (segGap < 1f) segGap = Math.Max(1f, segH * 0.35f);
				float origin = ((num7 < 0f) ? (g.Base + segGap) : g.Base);

				if (flag3)
				{
					// 1. Ghost Rungs (only for segmented / ladder displays)
					float ghostAlpha = (float)(look.LedGhost / 100.0) * num;
					if (ghostAlpha > 0f && (isNixieStyle || look.BarStyle == SpectrumLook.StyleLed || (isGlassStyle && look.GlassFill == 3)))
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

					// 2. Twin Anode Guide Wires for Vintage Nixie Tubes
					if (isNixieStyle)
					{
						float filW = Math.Max(1.0f, (x2 - x) * 0.08f);
						float leftRail = x + (x2 - x) * 0.22f;
						float rightRail = x2 - (x2 - x) * 0.22f - filW;
						float rTop = Math.Min(g.Base, g.Base + num7 * g.Span);
						float rBottom = Math.Max(g.Base, g.Base + num7 * g.Span);
						Vec4 railCol = f.Colours[10].Faded(num * 0.18f);
						c.Rect(RectShape.Solid(leftRail, rTop, leftRail + filW, rBottom, railCol));
						c.Rect(RectShape.Solid(rightRail, rTop, rightRail + filW, rBottom, railCol));
						if (flag2)
						{
							float mrTop = Math.Min(g.Base, g.Base - num7 * g.Span);
							float mrBottom = Math.Max(g.Base, g.Base - num7 * g.Span);
							c.Rect(RectShape.Solid(leftRail, mrTop, leftRail + filW, mrBottom, railCol));
							c.Rect(RectShape.Solid(rightRail, mrTop, rightRail + filW, mrBottom, railCol));
						}
					}

					float wall = (float)(look.GlassWall / 100.0);
					float tint = (float)(look.GlassTint / 100.0);
					float spec = (float)(look.GlassSpecular / 100.0);
					float caust = (float)(look.GlassCaustics / 100.0);
					float refr = (float)(look.GlassRefraction / 100.0);
					float hdrDrive = look.HdrIncandescence ? (float)(1.0 + look.HdrDrive / 100.0) : 0f;
					float glassOp = (float)(look.GlassOpacity / 100.0) * num;
					float tubeRound = Math.Min((float)(look.Radius * (double)PanelLayout.Scale), halfBarWidth);

					if (isGlassStyle && look.GlassFill == 0 && showTube && look.GlassTubeMode == 0 && glassOp > 0.005f)
					{
						EmitOuterTube(c, look, g, f.Colours[11].Faded(glassOp * 0.55f), x, x2, g.Base, num7, flag2, g.Span, tubeRound, 0.06f * caust * num6, refr, wall, tint * 0.35f, spec * 0.7f);
					}

					// 3. Active Interior Fill
					if (num6 > 0f)
					{
						if (isGlassStyle)
						{
							if (look.GlassFill == 0) // Pure Crystal Glass
							{
								float activePow = num6 * (0.35f + caust) * Math.Max(1f, hdrDrive * 0.7f);
								RectShape innerC = Bar(look, f.Colours, g, i, num3, num6, x, x2, g.Base, num7, num)
									.Glass(activePow, refr, wall, tint, spec);
								if (look.HdrIncandescence) innerC = innerC.Flags(RectFlags.HdrCore);
								c.Rect(innerC);
								if (flag2)
								{
									RectShape mInner = Bar(look, f.Colours, g, i, num3, num6, x, x2, g.Base, 0f - num7, num)
										.Glass(activePow, refr, wall, tint, spec);
									if (look.HdrIncandescence) mInner = mInner.Flags(RectFlags.HdrCore);
									c.Rect(mInner);
								}
							}
							else if (look.GlassFill == 1) // Liquid Plasma Column
							{
								float inset = (x2 - x) * (0.10f + 0.14f * wall);
								float plasmaOp = Math.Min(1f, num * (0.85f + caust * 0.25f));
								RectShape pBar = Bar(look, f.Colours, g, i, num3, num6, x + inset, x2 - inset, g.Base, num7, plasmaOp);
								if (look.HdrIncandescence) pBar = pBar.Hdr(num6 * Math.Max(1f, hdrDrive));
								c.Rect(pBar);
								if (flag2)
								{
									RectShape mpBar = Bar(look, f.Colours, g, i, num3, num6, x + inset, x2 - inset, g.Base, 0f - num7, plasmaOp);
									if (look.HdrIncandescence) mpBar = mpBar.Hdr(num6 * Math.Max(1f, hdrDrive));
									c.Rect(mpBar);
								}
							}
							else if (look.GlassFill == 2) // Twin Filament Anodes
							{
								float filW = Math.Max(1.5f, (x2 - x) * 0.18f);
								float mid = (x + x2) * 0.5f;
								float leftFil = mid - filW * 1.3f;
								float rightFil = mid + filW * 0.3f;
								RectShape fL = Bar(look, f.Colours, g, i, num3, num6, leftFil, leftFil + filW, g.Base, num7, num);
								RectShape fR = Bar(look, f.Colours, g, i, num3, num6, rightFil, rightFil + filW, g.Base, num7, num);
								if (look.HdrIncandescence) { fL = fL.Hdr(num6 * hdrDrive); fR = fR.Hdr(num6 * hdrDrive); }
								c.Rect(fL);
								c.Rect(fR);
								float tipY = g.Base + num7 * num6 * g.Span;
								float tipH = Math.Max(2f, filW);
								Vec4 bridgeCol = f.Colours[12] * (1f + caust * 0.4f);
								bridgeCol.W = f.Colours[12].W * num;
								c.Rect(RectShape.Solid(leftFil, (num7 < 0f) ? tipY : tipY - tipH, rightFil + filW, (num7 < 0f) ? tipY + tipH : tipY, bridgeCol));
								if (flag2)
								{
									RectShape mfL = Bar(look, f.Colours, g, i, num3, num6, leftFil, leftFil + filW, g.Base, 0f - num7, num);
									RectShape mfR = Bar(look, f.Colours, g, i, num3, num6, rightFil, rightFil + filW, g.Base, 0f - num7, num);
									if (look.HdrIncandescence) { mfL = mfL.Hdr(num6 * hdrDrive); mfR = mfR.Hdr(num6 * hdrDrive); }
									c.Rect(mfL);
									c.Rect(mfR);
								}
							}
							else if (look.GlassFill == 3) // Hybrid Glass & VFD Rungs
							{
								RectShape lit = Bar(look, f.Colours, g, i, num3, num6, x, x2, g.Base, num7, num)
									.Vfd(segH + segGap, segH, origin, num6 * Math.Max(1f, hdrDrive), look.HdrIncandescence, look.VfdMesh);
								c.Rect(lit);
								if (flag2) c.Rect(Bar(look, f.Colours, g, i, num3, num6, x, x2, g.Base, 0f - num7, num).Vfd(segH + segGap, segH, origin, num6 * Math.Max(1f, hdrDrive), look.HdrIncandescence, look.VfdMesh));
							}
						}
						else
						{
							c.Rect(Bar(look, f.Colours, g, i, num3, num6, x, x2, g.Base, num7, num));
							if (flag2) c.Rect(Bar(look, f.Colours, g, i, num3, num6, x, x2, g.Base, 0f - num7, num));
						}

						if (look.TipHighlight)
						{
							float tipY = g.Base + num7 * num6 * g.Span;
							float tipH = Math.Max(2f, Math.Min(5f, halfBarWidth * 0.35f));
							float t0 = (num7 < 0f) ? tipY : tipY - tipH;
							float t1 = (num7 < 0f) ? tipY + tipH : tipY;
							Vec4 tipCol = Vec4.Lerp(TipColour(look, f.Colours, g, i, num3, num6), new Vec4(1f, 1f, 1f, 1f), 0.65f);
							if (look.HdrIncandescence) tipCol = new Vec4(tipCol.X * (1f + hdrDrive * 0.35f * num6), tipCol.Y * (1f + hdrDrive * 0.35f * num6), tipCol.Z * (1f + hdrDrive * 0.35f * num6), tipCol.W * num);
							else tipCol = tipCol.Faded(num);
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

					// 4. Outer Glass Enclosures (skipped for GlassFill == 0 dynamic/full so bar gradient stays crisp)
					if (showTube && look.GlassFill != 0 && glassOp > 0.005f)
					{
						bool dynamicCap = look.GlassTubeMode == 1;
						if (!dynamicCap || num6 > 0.005f)
						{
							float spanH = dynamicCap ? Math.Max(halfBarWidth * 2f, num6 * g.Span) : g.Span;
							EmitOuterTube(c, look, g, f.Colours[11].Faded(glassOp), x, x2, g.Base, num7, flag2, spanH, tubeRound, num6 * caust, refr, wall, tint * 0.6f, spec);
						}
					}
					else if (isNixieStyle)
					{
						float capTop = Math.Min(g.Base, g.Base + num7 * g.Span);
						float capBottom = Math.Max(g.Base, g.Base + num7 * g.Span);
						Vec4 nixieTube = new Vec4(0.35f, 0.20f, 0.10f, num * 0.18f);
						c.Rect(RectShape.Solid(x, capTop, x2, capBottom, nixieTube).Rounded(halfBarWidth * 0.75f, 0.5f).Outline(1.0f));
						if (flag2)
						{
							float mcapTop = Math.Min(g.Base, g.Base - num7 * g.Span);
							float mcapBottom = Math.Max(g.Base, g.Base - num7 * g.Span);
							c.Rect(RectShape.Solid(x, mcapTop, x2, mcapBottom, nixieTube).Rounded(halfBarWidth * 0.75f, 0.5f).Outline(1.0f));
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
					if (flag2) DrawPeak(c, look, f.Colours, g, i, num3, j, x, x2, 0f - num7, ref f);
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

	private static void EmitOuterTube(Canvas c, SpectrumLook look, Geometry g, Vec4 glassTint, float x0, float x1, float baseY, float dir, bool mirror, float spanH, float corner, float activePow, float refr, float wall, float tint, float spec)
	{
		float soft = look.Snap ? 0.5f : 1f;
		float top = Math.Min(baseY, baseY + dir * spanH);
		float bot = Math.Max(baseY, baseY + dir * spanH);
		c.Rect(RectShape.Solid(x0, top, x1, bot, glassTint).Rounded(corner, soft).Corners((dir < 0f) ? corner : 0f, (dir < 0f) ? 0f : corner).Glass(activePow, refr, wall, tint, spec).Flags(RectFlags.Pill3D));
		if (mirror)
		{
			float mTop = Math.Min(baseY, baseY - dir * spanH);
			float mBot = Math.Max(baseY, baseY - dir * spanH);
			c.Rect(RectShape.Solid(x0, mTop, x1, mBot, glassTint).Rounded(corner, soft).Corners((dir > 0f) ? corner : 0f, (dir > 0f) ? 0f : corner).Glass(activePow, refr, wall, tint, spec).Flags(RectFlags.Pill3D));
		}
	}
}
