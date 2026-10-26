namespace MusicBeePlugin
{
    internal static partial class D3DShaders
    {
        public const string Scope = @"
// -----------------------------------------------------------------------------
// Mode 2: Lissajous Goniometer Phase Scope (Stereo Vector Field & Phosphor Beam)
// -----------------------------------------------------------------------------
float4 PSScope(FSOut i) : SV_Target
{
    float2 uv = float2(i.UV.x, 1.0 - i.UV.y);
    float2 center = float2(0.5, 0.5);

    // Dark analog display vignette
    float dCenter = length(uv - center);
    float vign = saturate(1.0 - dCenter * 0.95);
    float3 bg = PaletteBg.rgb * (vign * 0.7 + 0.3);
    float3 col = bg;

    // Studio Goniometer Reticle & Polar Grid (45-deg phase axes + concentric rings)
    if (StyleExtra.x > 0.5)
    {
        float ring1 = abs(dCenter - 0.20) * Viewport.y;
        float ring2 = abs(dCenter - 0.38) * Viewport.y;
        float ringCov = saturate(1.0 - ring1) * 0.12 + saturate(1.0 - ring2) * 0.10;

        // Diagonal phase boundary lines (M/S axes)
        float2 diag1 = abs((uv.x - 0.5) - (uv.y - 0.5)) * Viewport.y;
        float2 diag2 = abs((uv.x - 0.5) + (uv.y - 0.5)) * Viewport.y;
        float axisCov = saturate(1.0 - diag1.x) * 0.08 + saturate(1.0 - diag2.x) * 0.08;

        // Vertical mono axis
        float vAxis = abs(uv.x - 0.5) * Viewport.x;
        axisCov += saturate(1.0 - vAxis) * 0.14;

        col += float3(0.45, 0.60, 0.75) * (ringCov + axisCov);
    }

    float pxX = uv.x * Viewport.x;
    float pxY = uv.y * Viewport.y;
    float2 p = float2(pxX, pxY);

    float dpi = max(1.0, RibbonParams.w);
    float halfThick = max(1.2, RibbonParams.x * dpi * 0.45);
    float maxAmp = 0.36;

    float minDist = 9999.0;
    float minSegIdx = 0.0;

    // Trace 128 discrete vector segments across the phase scope
    [loop]
    for (int j = 0; j < 127; j++)
    {
        float2 lr0 = g_Waveform.Load(int2(j * 4, 0));
        float2 lr1 = g_Waveform.Load(int2((j + 1) * 4, 0));

        // Rotate 45 deg: X = Side (L - R), Y = Mid (L + R)
        float2 v0 = float2((lr0.x - lr0.y) * 0.7071 * maxAmp + 0.5, (lr0.x + lr0.y) * 0.7071 * maxAmp + 0.5);
        float2 v1 = float2((lr1.x - lr1.y) * 0.7071 * maxAmp + 0.5, (lr1.x + lr1.y) * 0.7071 * maxAmp + 0.5);

        float2 p0 = v0 * Viewport.xy;
        float2 p1 = v1 * Viewport.xy;

        float2 seg = p1 - p0;
        float segLenSq = max(0.001, dot(seg, seg));
        float t = clamp(dot(p - p0, seg) / segLenSq, 0.0, 1.0);
        float2 proj = p0 + seg * t;
        float d = length(p - proj);
        if (d < minDist)
        {
            minDist = d;
            minSegIdx = (float)j / 127.0;
        }
    }

    // Phosphor core & glowing beam
    float dLine = minDist - halfThick;
    float lineCov = saturate(0.5 - dLine);
    float halo = exp(-minDist * minDist / (halfThick * halfThick * 6.0)) * 0.75;
    float core = saturate(1.0 - minDist / max(0.5, halfThick * 0.45));

    // Dynamic color: Mid (Cyan / PaletteB) vs Side (Magenta / PaletteC)
    float3 beamCol = lerp(PaletteColorB.rgb, PaletteColorC.rgb, minSegIdx);
    beamCol += PaletteColorD.rgb * (core * 1.6);

    col = lerp(col, beamCol, lineCov);
    col += beamCol * halo;

    return float4(col, 1.0);
}
";
    }
}
