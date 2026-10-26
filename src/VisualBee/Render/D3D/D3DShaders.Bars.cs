namespace MusicBeePlugin
{
    internal static partial class D3DShaders
    {
        public const string Bars = @"
// -----------------------------------------------------------------------------
// Mode 0: Spectrum Bars (Grounded, Pixel-Accurate AA, H/V Mirror, Dynamic Styles)
// -----------------------------------------------------------------------------

float3 GetBarColor(float relY, float barFrac, float time)
{
    int mode = (int)ColorParams.x;
    if (mode == 1) // Across frequencies (A -> B -> C -> D)
    {
        float3 c;
        if (barFrac < 0.33) c = lerp(PaletteColorA.rgb, PaletteColorB.rgb, barFrac / 0.33);
        else if (barFrac < 0.66) c = lerp(PaletteColorB.rgb, PaletteColorC.rgb, (barFrac - 0.33) / 0.33);
        else c = lerp(PaletteColorC.rgb, PaletteColorD.rgb, (barFrac - 0.66) / 0.34);
        c = lerp(c, PaletteColorD.rgb, smoothstep(0.85, 1.0, relY) * 0.45);
        return c;
    }
    else if (mode == 2) // Solid tip
    {
        return lerp(PaletteColorC.rgb * 0.9, PaletteColorD.rgb, smoothstep(0.80, 1.0, relY));
    }
    else if (mode == 3) // Hue cycle
    {
        float hue = frac(time * ColorParams.y * 0.12 + barFrac * 0.5 + relY * 0.15);
        float3 rgb = saturate(abs(frac(hue + float3(0.0, 2.0/3.0, 1.0/3.0)) * 6.0 - 3.0) - 1.0);
        return rgb;
    }
    else // Mode 0: Vertical gradient (Default)
    {
        float3 c = lerp(PaletteColorB.rgb, PaletteColorC.rgb, relY);
        c = lerp(c, PaletteColorD.rgb, smoothstep(0.85, 1.0, relY) * 0.85);
        return c;
    }
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

    if (stereoMode == 1) // Center-Out Wings (Opt-in)
    {
        if (x < 0.5)
        {
            float u = (0.5 - x) * 2.0;
            float cell = u * N;
            bar = clamp((int)cell, 0, (int)N - 1);
            lx = 1.0 - frac(cell);
            channelOffset = 0;
            barFrac = (float)bar / max(1.0, N - 1.0);
        }
        else
        {
            float u = (x - 0.5) * 2.0;
            float cell = u * N;
            bar = clamp((int)cell, 0, (int)N - 1);
            lx = frac(cell);
            channelOffset = 160;
            barFrac = (float)bar / max(1.0, N - 1.0);
        }
    }
    else if (stereoMode == 2) // Split L/R (Opt-in)
    {
        if (y >= 0.5)
        {
            y = (y - 0.5) * 2.0;
            channelOffset = 0;
        }
        else
        {
            y = (0.5 - y) * 2.0;
            channelOffset = 160;
        }
        if (mirrorH) x = abs(x * 2.0 - 1.0);
        float cell = x * N;
        bar = clamp((int)cell, 0, (int)N - 1);
        lx = frac(cell);
        barFrac = (float)bar / max(1.0, N - 1.0);
    }
    else // Mono / Standard (Default)
    {
        if (mirrorH) x = abs(x * 2.0 - 1.0);
        float cell = x * N;
        bar = clamp((int)cell, 0, (int)N - 1);
        lx = frac(cell);
        channelOffset = 0;
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
    bg += PaletteColorB.rgb * (0.015 * AudioMetricsA.x * (1.0 - uv.y));
    float3 col = bg;

    // Floor reflection support
    float floorReflect = (!mirrorV && stereoMode != 2) ? PeakParams.z : 0.0;
    float groundBase = (floorReflect > 0.01) ? 0.16 : 0.0;
    bool isFloorMirror = (floorReflect > 0.01 && y < groundBase);

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

    float cellPx = (stereoMode == 1 || mirrorH) ? ((Viewport.x * 0.5) / N) : (Viewport.x / N);
    float barWidthPx = cellPx * barW;
    float halfBarPx = max(1.0, barWidthPx * 0.5);
    float pxX = abs(lx - 0.5) * cellPx;
    float dX = pxX - halfBarPx;
    float covX = saturate(0.5 - dX);

    // 1. Contact Light Pool on the ground
    if (PeakParams.w > 0.5 && !isFloorMirror)
    {
        if (yPx < 10.0)
        {
            float pool = exp(-yPx * 0.35) * rawH * 0.30;
            col += PaletteColorB.rgb * pool;
        }
        if (yPx < 1.0)
        {
            col = lerp(col, PaletteColorB.rgb * 0.35, 0.5);
        }
    }

    int style = (int)VisualParams.y; // 0 = Pills, 1 = Segmented, 2 = Solid
    float roundness = saturate(BarShape.x);

    // Style 0: Smooth Pills (Top-rounded, flat grounded bottom)
    if (style == 0)
    {
        float radiusPx = halfBarPx * roundness;
        float dY = yPx - (hPx - radiusPx);
        float dCorner = (dY > 0.0 && radiusPx > 0.5)
            ? (length(float2(max(0.0, pxX - (halfBarPx - radiusPx)), dY)) - radiusPx)
            : dX;

        float cov = saturate(0.5 - dCorner) * saturate(hPx - yPx + 0.5);
        if (cov > 0.0)
        {
            float relY = yPx / max(1.0, hPx);
            float3 barCol = GetBarColor(relY, barFrac, VisualParams.x);

            float rim = 1.0 - saturate(pxX / halfBarPx);
            barCol *= (0.80 + 0.35 * rim);

            if (isFloorMirror)
            {
                float fade = (1.0 - y / groundBase) * floorReflect * 0.45;
                col = lerp(col, barCol * fade, cov);
            }
            else
            {
                col = lerp(col, barCol, cov);
            }
        }
    }
    // Style 1: Segmented LEDs (Customizable segment height & gap)
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
            float3 ledCol = GetBarColor(relY, barFrac, VisualParams.x);

            float uBevel = segY / segH;
            float bevel = sin(uBevel * 3.14159) * 0.35 + 0.75;

            if (isLit)
            {
                if (isFloorMirror)
                {
                    float fade = (1.0 - y / groundBase) * floorReflect * 0.45;
                    col = lerp(col, ledCol * bevel * fade, covX);
                }
                else
                {
                    col = lerp(col, ledCol * bevel, covX);
                }
            }
            else if (!isFloorMirror)
            {
                // Dim unlit LED
                float3 offCol = PaletteColorA.rgb * 0.35 * bevel;
                col = lerp(col, offCol, covX * 0.85);
            }
        }
    }
    // Style 2: Solid Neo-Bars
    else
    {
        float covY = saturate(hPx - yPx + 0.5);
        float cov = covX * covY;
        if (cov > 0.0)
        {
            float relY = yPx / max(1.0, hPx);
            float3 barCol = GetBarColor(relY, barFrac, VisualParams.x);

            if (abs(yPx - hPx) < 1.5)
                barCol += PaletteColorD.rgb * 0.6;

            float chamfer = 1.0 - smoothstep(halfBarPx * (0.5 + 0.5 * roundness), halfBarPx, pxX) * 0.35;
            if (isFloorMirror)
            {
                float fade = (1.0 - y / groundBase) * floorReflect * 0.45;
                col = lerp(col, barCol * chamfer * fade, cov);
            }
            else
            {
                col = lerp(col, barCol * chamfer, cov);
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
            if (capMode == 1) capCol = GetBarColor(1.0, barFrac, VisualParams.x) * 1.25;
            else if (capMode == 2) capCol = PeakColor.rgb * 1.25;
            col = lerp(col, capCol, covPeak);
        }
    }

    return float4(col, 1.0);
}
";
    }
}
