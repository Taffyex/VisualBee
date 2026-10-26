namespace MusicBeePlugin
{
    internal static partial class D3DShaders
    {
        public const string Ribbon = @"
// -----------------------------------------------------------------------------
// Mode 1: Liquid Neon Oscilloscope Waveform (Centered, Continuous Polyline AA, Luminous Fill)
// -----------------------------------------------------------------------------
float4 PSRibbon(FSOut i) : SV_Target
{
    float2 uv = float2(i.UV.x, 1.0 - i.UV.y);
    int mirror = (int)BarParams.z;
    bool mirrorH = (mirror & 1) != 0;
    bool mirrorV = (mirror & 2) != 0;

    float yBaseline = 0.50;
    float maxAmp = 0.38 * (1.0 + AudioMetricsB.x * 0.15);

    // Dark vignette background with dynamic atmosphere
    float vign = 1.0 - length((uv - 0.5) * float2(1.2, 0.9));
    float3 bg = PaletteBg.rgb * saturate(vign * 0.7 + 0.3);
    float3 col = RenderAtmosphere(uv, VisualParams.x, bg);

    // Studio Decibel Reference Grid Lines (-24 dB .. 0 dB)
    if (StudioParams.x > 0.001)
    {
        static const float kGridMarks[5] = { 0.18, 0.36, 0.54, 0.72, 0.90 };
        float gridLine = 0.0;
        [unroll]
        for (int g = 0; g < 5; g++)
        {
            float markPx = kGridMarks[g] * Viewport.y;
            gridLine += saturate(1.0 - abs(uv.y * Viewport.y - markPx));
        }
        float3 gridCol = lerp(PaletteColorA.rgb, PaletteColorB.rgb, 0.35) * 0.6;
        col += gridCol * (gridLine * StudioParams.x);
    }

    // Main line & polyline parameters
    float pxX = uv.x * Viewport.x;
    float pxY = uv.y * Viewport.y;
    float dpi = max(1.0, RibbonParams.w);
    float targetThickPx = max(0.8, RibbonParams.x * dpi);
    float halfThick = targetThickPx * 0.5;

    // Centerline subtle baseline glow
    if (dot(PaletteBg.rgb, PaletteBg.rgb) > 0.0001)
    {
        float dCenterLine = abs(uv.y - yBaseline) * Viewport.y;
        float centerAlpha = exp(-dCenterLine * 0.08) * 0.12;
        col += PaletteColorB.rgb * centerAlpha;
    }

    // Audio-reactive translucent fill under/above the wave towards the baseline
    float sampleU = mirrorH ? abs(uv.x * 2.0 - 1.0) : uv.x;
    if (RibbonParams.y > 0.5)
    {
        float localW = g_Waveform.Sample(g_Linear, sampleU).r * maxAmp;
        float waveCurveY = yBaseline + localW;
        float minY = min(yBaseline, waveCurveY);
        float maxY = max(yBaseline, waveCurveY);

        if (uv.y >= minY && uv.y <= maxY)
        {
            float span = max(0.001, maxY - minY);
            float distFromBase = abs(uv.y - yBaseline) / span;
            float fillGlow = pow(distFromBase, 1.8) * 0.35 + pow(distFromBase, 4.0) * 0.30;
            float3 fillCol = lerp(PaletteColorB.rgb, PaletteColorC.rgb, uv.x) * fillGlow;
            col += fillCol;
        }
    }

    // Sample discrete polyline segments near pxX
    float uTex = sampleU * 511.0;
    int centerIdx = clamp((int)uTex, 0, 511);
    float cellWidthPx = Viewport.x / 511.0;
    int kSpan = clamp((int)ceil(halfThick / max(1.0, cellWidthPx)) + 2, 2, 6);

    float minDist = 9999.0;
    int startIdx = max(0, centerIdx - kSpan);
    int endIdx = min(510, centerIdx + kSpan);

    for (int j = startIdx; j <= endIdx; j++)
    {
        float x0 = (j / 511.0) * Viewport.x;
        float y0 = (yBaseline + g_Waveform.Load(int2(j, 0)).r * maxAmp) * Viewport.y;
        float x1 = ((j + 1) / 511.0) * Viewport.x;
        float y1Actual = (yBaseline + g_Waveform.Load(int2(j + 1, 0)).r * maxAmp) * Viewport.y;

        float2 p0 = float2(x0, y0);
        float2 p1 = float2(x1, y1Actual);
        float2 p = float2(pxX, pxY);

        float2 seg = p1 - p0;
        float segLenSq = max(0.001, dot(seg, seg));
        float t = clamp(dot(p - p0, seg) / segLenSq, 0.0, 1.0);
        float2 proj = p0 + seg * t;
        minDist = min(minDist, length(p - proj));
    }

    // Dual-tone neon gradient along the waveform
    float3 lineCol = lerp(PaletteColorB.rgb, PaletteColorC.rgb, uv.x);

    // Constant-thickness antialiased core & hot specular center
    float dLine = minDist - halfThick;
    float lineCov = saturate(0.5 - dLine);
    float halo = exp(-minDist * minDist / (halfThick * halfThick * 5.0)) * 0.65;
    float core = saturate(1.0 - minDist / max(0.5, halfThick * 0.50));

    lineCol += PaletteColorD.rgb * (core * 1.5);
    col = lerp(col, lineCol, lineCov);
    col += lineCol * halo;

    // Optional floor reflection
    if (RibbonParams.z > 0.5 && uv.y < 0.18)
    {
        float floorFade = (1.0 - uv.y / 0.18) * 0.35;
        float reflDist = (0.18 - uv.y) * Viewport.y;
        float reflAlpha = exp(-reflDist * 0.05) * floorFade;
        col += lerp(PaletteColorB.rgb, PaletteColorC.rgb, uv.x) * reflAlpha;
    }

    return float4(col, 1.0);
}
";
    }
}
