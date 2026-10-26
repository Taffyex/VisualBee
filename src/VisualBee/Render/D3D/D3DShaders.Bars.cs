namespace MusicBeePlugin
{
    internal static partial class D3DShaders
    {
        public const string Bars = @"
// -----------------------------------------------------------------------------
// Mode 0: Spectrum Bars (Glass Pillars, Studio VFD Rack, Decibel Grid & Lacquer)
// -----------------------------------------------------------------------------

float3 CyclePalette(float phase)
{
    float p = frac(phase) * 3.0;
    int seg = (int)p;
    float f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    if (seg == 0) return lerp(PaletteColorB.rgb, PaletteColorC.rgb, f);
    if (seg == 1) return lerp(PaletteColorC.rgb, PaletteColorD.rgb, f);
    return lerp(PaletteColorD.rgb, PaletteColorB.rgb, f);
}

float3 GetBarColor(float relY, float barFrac, float time, float screenY, float rawH, float coreDist)
{
    int mode = (int)ColorParams.x;
    bool highlightTip = StyleExtra.z > 0.5;
    float3 c;

    if (mode == 1) // Across frequencies (B -> C -> D)
    {
        c = (barFrac < 0.5)
            ? lerp(PaletteColorB.rgb, PaletteColorC.rgb, barFrac * 2.0)
            : lerp(PaletteColorC.rgb, PaletteColorD.rgb, (barFrac - 0.5) * 2.0);
        if (highlightTip)
            c = lerp(c, PaletteColorD.rgb, smoothstep(0.85, 1.0, relY) * 0.45);
    }
    else if (mode == 2) // Solid tip
    {
        c = highlightTip ? lerp(PaletteColorC.rgb * 0.9, PaletteColorD.rgb, smoothstep(0.80, 1.0, relY)) : PaletteColorC.rgb;
    }
    else if (mode == 3) // Hue cycle
    {
        int hueMotion = (int)StyleExtra.x;
        int hueSource = (int)StyleExtra.w;
        float speed = ColorParams.y * 0.15;
        float offset = (hueMotion == 1) ? (barFrac * 0.5 + screenY * 0.25) : barFrac;
        float phase = frac(time * speed + offset);
        c = (hueSource == 0)
            ? CyclePalette(phase)
            : saturate(abs(frac(phase + float3(0.0, 2.0/3.0, 1.0/3.0)) * 6.0 - 3.0) - 1.0);
        if (highlightTip)
            c = lerp(c, PaletteColorD.rgb, smoothstep(0.85, 1.0, relY) * 0.40);
    }
    else // Mode 0: Vertical gradient (Default)
    {
        c = lerp(PaletteColorB.rgb, PaletteColorC.rgb, relY);
        if (highlightTip)
            c = lerp(c, PaletteColorD.rgb, smoothstep(0.85, 1.0, relY) * 0.85);
    }

    // Power-driven HDR incandescence (White-hot overdrive)
    if (StudioParams.y > 0.5 && rawH > 0.45)
    {
        float overdrive = smoothstep(0.45, 0.95, rawH);
        float coreX = 1.0 - smoothstep(0.0, 0.85, coreDist);
        float heat = overdrive * (coreX * 0.75 + smoothstep(0.3, 1.0, relY) * 0.35);
        c = lerp(c, float3(1.65, 1.65, 1.65), heat * 0.90);
    }

    return c;
}

float4 PSBars(FSOut i) : SV_Target
{
    float2 uv = float2(i.UV.x, 1.0 - i.UV.y);
    int mirror = (int)BarParams.z;
    bool mirrorH = (mirror & 1) != 0;
    bool mirrorV = (mirror & 2) != 0;
    int stereoMode = (int)StyleExtra.y; // 0 = Mono/Standard, 1 = CenterOut, 2 = SplitLR

    float x = uv.x;
    float y = uv.y;
    float maxH = 0.95;
    if (mirrorV)
    {
        y = abs(y * 2.0 - 1.0);
        maxH = 0.92;
    }

    float N = clamp(BarParams.x, 16.0, 160.0);
    int bar = 0;
    float lx = 0.0;
    int channelOffset = 0;
    float barFrac = 0.0;

    if (stereoMode == 1) // Center-Out Wings
    {
        float u = (x < 0.5) ? (0.5 - x) * 2.0 : (x - 0.5) * 2.0;
        float cell = u * N;
        bar = clamp((int)cell, 0, (int)N - 1);
        lx = (x < 0.5) ? (1.0 - frac(cell)) : frac(cell);
        channelOffset = (x < 0.5) ? 0 : 160;
        barFrac = (float)bar / max(1.0, N - 1.0);
    }
    else if (stereoMode == 2) // Split L/R
    {
        channelOffset = (y >= 0.5) ? 0 : 160;
        y = (y >= 0.5) ? (y - 0.5) * 2.0 : (0.5 - y) * 2.0;
        if (mirrorH) x = abs(x * 2.0 - 1.0);
        float cell = x * N;
        bar = clamp((int)cell, 0, (int)N - 1);
        lx = frac(cell);
        barFrac = (float)bar / max(1.0, N - 1.0);
    }
    else // Mono / Standard
    {
        if (mirrorH) x = abs(x * 2.0 - 1.0);
        float cell = x * N;
        bar = clamp((int)cell, 0, (int)N - 1);
        lx = frac(cell);
        barFrac = (float)bar / max(1.0, N - 1.0);
    }

    // Treble taper
    float taper = saturate(BarShape.w);
    float taperFactor = lerp(1.0, max(0.2, 1.0 - taper), barFrac);
    float gap = clamp(BarParams.y, 0.0, 0.6);
    float barW = (1.0 - gap) * taperFactor;

    // Load discrete (height, peak) from BarDynamics
    float2 hp = g_Bands.Load(int2(channelOffset + bar, 0));
    float rawH = hp.x;
    float rawP = hp.y;
    float h = rawH * maxH;
    float peak = rawP * maxH;

    // Background: true dark PaletteBg with customizable vignette & bass breathing
    float3 bg = PaletteBg.rgb * lerp(0.85, 0.45, uv.y);
    float dCenter = length(uv - float2(0.5, 0.5));
    bg *= saturate(1.0 - dCenter * ColorParams.w);
    if (dot(PaletteBg.rgb, PaletteBg.rgb) > 0.0001)
    {
        bg += PaletteColorB.rgb * (0.015 * AudioMetricsA.x * (1.0 - uv.y));
    }
    float3 col = bg;

    // Floor reflection support
    float floorReflect = (!mirrorV && stereoMode != 2) ? PeakParams.z : 0.0;
    float groundBase = (floorReflect > 0.01) ? 0.16 : 0.0;
    bool isFloorMirror = (floorReflect > 0.01 && y < groundBase);

    // Wet Lacquer ground horizon datum line & ambient glow
    if (floorReflect > 0.01 && !mirrorV)
    {
        float dHorizon = abs(y - groundBase) * Viewport.y;
        float horizonLine = saturate(1.2 - dHorizon);
        col += lerp(PaletteColorB.rgb, PaletteColorC.rgb, 0.5) * (horizonLine * 0.40 * floorReflect);
        if (y < groundBase)
        {
            col += PaletteColorB.rgb * (exp(-dHorizon * 0.35) * 0.12 * floorReflect);
        }
    }

    float effectiveY = y;
    if (isFloorMirror)
    {
        effectiveY = (groundBase - y) / (1.0 - groundBase);
    }
    else if (groundBase > 0.0)
    {
        effectiveY = (y - groundBase) / (1.0 - groundBase);
    }

    // Pixel metrics
    float totalHPx = (mirrorV || stereoMode == 2) ? (Viewport.y * 0.5) : Viewport.y;
    float yPx = effectiveY * totalHPx;
    float hPx = h * totalHPx;
    float peakPx = peak * totalHPx;

    // Studio Decibel Reference Grid Lines (-24 dB .. 0 dB)
    if (StudioParams.x > 0.001 && !isFloorMirror)
    {
        static const float kGridMarks[5] = { 0.18, 0.36, 0.54, 0.72, 0.90 };
        float gridLine = 0.0;
        [unroll]
        for (int g = 0; g < 5; g++)
        {
            float markPx = kGridMarks[g] * totalHPx;
            gridLine += saturate(1.0 - abs(yPx - markPx));
        }
        float3 gridCol = lerp(PaletteColorA.rgb, PaletteColorB.rgb, 0.4) * 0.7;
        col += gridCol * (gridLine * StudioParams.x);
    }

    float cellPx = (stereoMode == 1 || mirrorH) ? ((Viewport.x * 0.5) / N) : (Viewport.x / N);
    float barWidthPx = cellPx * barW;
    float halfBarPx = max(1.0, barWidthPx * 0.5);
    float pxX = abs(lx - 0.5) * cellPx;
    float dX = pxX - halfBarPx;
    float covX = saturate(0.5 - dX);

    // Contact Light Pool on the ground
    if (PeakParams.w > 0.5 && !isFloorMirror && !mirrorV)
    {
        if (yPx < 10.0)
            col += PaletteColorB.rgb * (exp(-yPx * 0.35) * rawH * 0.30);
        if (yPx < 1.0)
            col = lerp(col, PaletteColorB.rgb * 0.35, 0.5);
    }

    int style = (int)VisualParams.y; // 0 = Pills, 1 = Segmented, 2 = Solid, 3 = Glass Pillars
    float roundness = saturate(BarShape.x);
    float distBelow = groundBase - y;
    float lacquerFalloff = exp(-distBelow * 11.0) * (1.0 - distBelow / max(0.001, groundBase)) * floorReflect;
    float3 navyLacquer = float3(0.08, 0.20, 0.42);

    // Style 0: Smooth Pills
    if (style == 0)
    {
        float radiusPx = min(halfBarPx * roundness, hPx * 0.5);
        float dY = yPx - (hPx - radiusPx);
        float dCorner = (dY > 0.0 && radiusPx > 0.5)
            ? (length(float2(max(0.0, pxX - (halfBarPx - radiusPx)), dY)) - radiusPx)
            : dX;

        float cov = saturate(0.5 - dCorner) * saturate(hPx - yPx + 0.5);
        if (cov > 0.0)
        {
            float relY = yPx / max(1.0, hPx);
            float coreDist = saturate(pxX / halfBarPx);
            float3 barCol = GetBarColor(relY, barFrac, VisualParams.x, uv.y, rawH, coreDist);
            barCol *= (0.80 + 0.35 * (1.0 - coreDist));

            if (isFloorMirror)
                col = lerp(col, barCol * navyLacquer * 1.6 + barCol * 0.12, cov * lacquerFalloff);
            else
                col = lerp(col, barCol, cov);
        }
    }
    // Style 1: Segmented LEDs
    else if (style == 1)
    {
        float segH = max(2.0, BarShape.y);
        float segGap = max(1.0, BarShape.z);
        float segPeriod = segH + segGap;

        int segIdx = (int)(yPx / segPeriod);
        float segY = yPx - segIdx * segPeriod;
        float segTopPx = (segIdx + 1) * segPeriod - segGap;
        bool isLit = segTopPx <= (hPx + segH * 0.5);
        bool inGap = segY >= segH;

        if (!inGap && covX > 0.0)
        {
            float relY = min(1.0, (segIdx * segPeriod) / max(1.0, totalHPx * 0.9));
            float coreDist = saturate(pxX / halfBarPx);
            float3 ledCol = GetBarColor(relY, barFrac, VisualParams.x, uv.y, rawH, coreDist);

            float uBevel = segY / segH;
            float bevel = sin(uBevel * 3.14159) * 0.35 + 0.75;
            ledCol *= bevel;

            if (isLit)
            {
                if (isFloorMirror)
                    col = lerp(col, ledCol * navyLacquer * 1.6 + ledCol * 0.12, covX * lacquerFalloff);
                else
                    col = lerp(col, ledCol, covX);
            }
            else if (!isFloorMirror)
            {
                col = lerp(col, PaletteColorA.rgb * 0.35 * bevel, covX * 0.85);
            }
        }
    }
    // Style 2: Solid Neo-Bars
    else if (style == 2)
    {
        float covY = saturate(hPx - yPx + 0.5);
        float cov = covX * covY;
        if (cov > 0.0)
        {
            float relY = yPx / max(1.0, hPx);
            float coreDist = saturate(pxX / halfBarPx);
            float3 barCol = GetBarColor(relY, barFrac, VisualParams.x, uv.y, rawH, coreDist);

            if (StyleExtra.z > 0.5 && abs(yPx - hPx) < 1.5)
                barCol += PaletteColorD.rgb * 0.6;

            float chamfer = 1.0 - smoothstep(halfBarPx * (0.5 + 0.5 * roundness), halfBarPx, pxX) * 0.35;
            if (isFloorMirror)
                col = lerp(col, barCol * chamfer * navyLacquer * 1.6 + barCol * 0.12, cov * lacquerFalloff);
            else
                col = lerp(col, barCol * chamfer, cov);
        }
    }
    // Style 3: Glass Pillars & VFD Rack (Enclosure, Ghosted Rungs, Architectural Ladder)
    else
    {
        float pillarTopPx = max(peakPx, hPx + 4.0);
        float radiusPx = min(halfBarPx * roundness, pillarTopPx * 0.5);
        float dYTop = yPx - (pillarTopPx - radiusPx);
        float dPillar = (dYTop > 0.0 && radiusPx > 0.5)
            ? (length(float2(max(0.0, pxX - (halfBarPx - radiusPx)), dYTop)) - radiusPx)
            : dX;

        float pillarCov = saturate(0.5 - dPillar) * saturate(pillarTopPx - yPx + 0.5);

        // 1. Translucent glass capsule boundary with subtle glowing rounded-pill border
        if (!isFloorMirror && pillarCov > 0.0)
        {
            float glassBorder = saturate(1.0 - abs(dPillar)) * 0.38;
            float3 glassCol = lerp(PaletteColorB.rgb, PaletteColorC.rgb, 0.4);
            col += glassCol * (glassBorder + pillarCov * 0.035);
        }

        // 2. Architectural Ladder Geometry (Global Y grid alignment)
        float segH = max(2.0, BarShape.y);
        float segGap = max(1.0, BarShape.z);
        float segPeriod = segH + segGap;

        int segIdx = (int)(yPx / segPeriod);
        float segY = yPx - segIdx * segPeriod;
        float covRungY = saturate(0.5 - (abs(segY - segH * 0.5) - (segH * 0.5 - 0.5)));
        float rungCov = covX * covRungY;

        if (rungCov > 0.0)
        {
            float relY = yPx / max(1.0, hPx);
            float coreDist = saturate(pxX / halfBarPx);
            float3 rungCol = GetBarColor(relY, barFrac, VisualParams.x, uv.y, rawH, coreDist);

            float uBevel = segY / segH;
            float bevel = sin(uBevel * 3.14159) * 0.35 + 0.80;
            rungCol *= bevel;

            bool isLit = yPx <= (hPx + 0.5);
            if (isLit)
            {
                if (isFloorMirror)
                    col = lerp(col, rungCol * navyLacquer * 1.6 + rungCol * 0.12, rungCov * lacquerFalloff);
                else
                    col = lerp(col, rungCol, rungCov);
            }
            else if (!isFloorMirror && yPx <= pillarTopPx)
            {
                // Ghosted unlit ladder rung (VFD unlit filament phosphor)
                float3 unlitCol = lerp(PaletteColorA.rgb, PaletteColorB.rgb, 0.25) * 0.18 * bevel;
                col = lerp(col, col + unlitCol, rungCov * 0.85);
            }
        }

        // 3. Floating illuminated cap at the top of active volume
        if (!isFloorMirror && abs(yPx - hPx) < (segH * 0.6) && hPx > 3.0)
        {
            float dCapY = abs(yPx - hPx);
            float capCov = covX * saturate(segH * 0.6 - dCapY);
            if (capCov > 0.0)
            {
                float3 capCol = lerp(PaletteColorC.rgb, float3(1.6, 1.6, 1.6), 0.65);
                col = lerp(col, capCol, capCov * 0.90);
            }
        }
    }

    // Peak Caps (BarParams.w: 1 = enabled)
    if (!isFloorMirror && BarParams.w > 0.5 && peakPx > (hPx + 2.0) && peakPx < totalHPx)
    {
        float thick = max(1.0, PeakParams.x);
        float dPeakY = abs(yPx - peakPx);
        float covPeak = covX * saturate((thick * 0.5 + 0.5) - dPeakY);
        if (covPeak > 0.0)
        {
            float3 capCol = PaletteColorD.rgb * 1.35;
            int capMode = (int)PeakParams.y;
            if (capMode == 1) capCol = GetBarColor(1.0, barFrac, VisualParams.x, uv.y, rawH, 0.0) * 1.25;
            else if (capMode == 2) capCol = PeakColor.rgb * 1.25;
            col = lerp(col, capCol, covPeak);
        }
    }

    return float4(col, 1.0);
}
";
    }
}
