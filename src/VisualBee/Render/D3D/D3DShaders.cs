namespace MusicBeePlugin
{
    internal static partial class D3DShaders
    {
        public const string Common = @"
cbuffer VisualBeeConstants : register(b0)
{
    float4 Viewport;        // x: width, y: height, z: 1/width, w: 1/height
    float4 AudioMetricsA;   // x: lowPulse, y: bass, z: mid, w: high
    float4 AudioMetricsB;   // x: beat, y: rms, z: centroid, w: transient
    float4 VisualParams;    // x: time (s), y: barStyle, z: bloomIntensity, w: visualizerMode
    float4 PaletteColorA;   // Primary / Accent (linear)
    float4 PaletteColorB;   // Secondary / Warm Glow (linear)
    float4 PaletteColorC;   // Highlight / Peak (linear)
    float4 PaletteColorD;   // Accent Tip / Highlight (linear)
    float4 PaletteBg;       // True Dark Background (linear)
    float4 BarParams;       // x: barCount, y: barGap, z: mirrorFlags, w: peakHoldEnabled
    float4 RibbonParams;    // x: thicknessPx, y: fillToFloor, z: floorReflection, w: dpiScale
    float4 StyleExtra;      // x: hueMotion, y: stereoMode, z: highlightBarTips, w: hueSource
    float4 BarShape;        // x: roundness 0..1, y: segHeightPx, z: segGapPx, w: trebleTaper 0..0.8
    float4 ColorParams;     // x: colorMode 0..3, y: hueSpeed, z: bgBrightness, w: vignette 0..1
    float4 PeakParams;      // x: capThicknessPx, y: capColorMode 0..2, z: reflection 0..1, w: contactGlow 0/1
    float4 PeakColor;       // linear custom cap colour (rgb)
    float4 StudioParams;    // x: decibelGrid, y: incandescence, z: glassOpacity, w: horizonGlow
    float4 GlassParams;     // x: glassFillMode, y: glassTubeOverlay, z: rungGhostOpacity, w: rungGhostRange
    float4 AtmosphereParams;// x: particleIntensity, y: particleParallax, z: cyberGridIntensity, w: scanlineShimmer
    float4 SymmetryParams;  // x: centerLineGlow, y: symmetricCenterColor, z: centerLineIntensity, w: reserved
};

cbuffer BlurConstants : register(b1)
{
    float4 BlurDir;         // x, y step, z: 0, w: 0
};

Texture1D<float2> g_Waveform : register(t0);
Texture1D<float2> g_Bands    : register(t1);
Texture2D<float4> g_Scene    : register(t2);
Texture2D<float4> g_Bloom    : register(t3);

SamplerState g_Linear : register(s0);
SamplerState g_Point  : register(s1);

struct FSOut
{
    float4 Pos : SV_Position;
    float2 UV  : TEXCOORD0;
};

FSOut VSFullscreen(uint vid : SV_VertexID)
{
    FSOut o;
    o.UV  = float2((vid << 1) & 2, vid & 2);
    o.Pos = float4(o.UV.x * 2.0 - 1.0, 1.0 - o.UV.y * 2.0, 0.0, 1.0);
    return o;
}

// -----------------------------------------------------------------------------
// Procedural Background Atmosphere & Particles (Embers, Cyber-Grid & Shimmer)
// -----------------------------------------------------------------------------
float Hash21(float2 p)
{
    p = frac(p * float2(234.34, 435.345));
    p += dot(p, p + 34.23);
    return frac(p.x * p.y);
}

float3 RenderAtmosphere(float2 uv, float time, float3 bgBase)
{
    float3 col = bgBase;

    // 1. Perspective 3D Cyber-Grid & Horizon
    float gridInt = AtmosphereParams.z;
    if (gridInt > 0.01)
    {
        float2 pGrid = uv - float2(0.5, 0.5);
        if (pGrid.y < -0.01)
        {
            float depth = -0.22 / (pGrid.y - 0.02);
            float xPersp = pGrid.x * depth;
            float gridZ = frac(depth * 0.85 - time * 0.45);
            float gridX = frac(xPersp * 1.6);
            float lineZ = smoothstep(0.06, 0.0, abs(gridZ - 0.5) * 0.5);
            float lineX = smoothstep(0.06, 0.0, abs(gridX - 0.5) * 0.5);
            float gridMask = max(lineZ, lineX) * saturate(-pGrid.y * 3.5);
            float3 gridCol = lerp(PaletteColorB.rgb, PaletteColorC.rgb, saturate(depth * 0.15));
            col += gridCol * (gridMask * gridInt * 0.45);
        }
    }

    // 2. 3D Parallax Bokeh & Floating Embers
    float partInt = AtmosphereParams.x;
    if (partInt > 0.01)
    {
        float parallax = AtmosphereParams.y;
        float beat = AudioMetricsB.x;
        [unroll]
        for (int layer = 1; layer <= 3; layer++)
        {
            float fLayer = (float)layer;
            float speed = 0.08 * fLayer;
            float2 pUv = uv * (3.0 + fLayer * 2.5);
            pUv.y += time * speed;
            pUv.x += sin(time * 0.35 + fLayer) * (0.15 * parallax);

            float2 id = floor(pUv);
            float2 gv = frac(pUv) - 0.5;

            float n = Hash21(id + fLayer * 17.13);
            float size = lerp(0.08, 0.22, n);
            float d = length(gv - (float2(Hash21(id * 1.5), Hash21(id * 2.7)) - 0.5) * 0.65);
            float spark = smoothstep(size, 0.0, d);
            spark *= (0.4 + 0.6 * sin(time * 2.5 + n * 6.28));
            spark *= (1.0 + beat * 0.7);

            float3 partCol = lerp(PaletteColorB.rgb, PaletteColorD.rgb, n);
            col += partCol * (spark * partInt * (0.35 / fLayer));
        }
    }

    return col;
}
";

        public const string Bloom = @"
float4 PSBloomBright(FSOut i) : SV_Target
{
    float2 d = Viewport.zw * 0.75;
    float3 s0 = g_Scene.Sample(g_Linear, i.UV + float2(-d.x, -d.y)).rgb;
    float3 s1 = g_Scene.Sample(g_Linear, i.UV + float2( d.x, -d.y)).rgb;
    float3 s2 = g_Scene.Sample(g_Linear, i.UV + float2(-d.x,  d.y)).rgb;
    float3 s3 = g_Scene.Sample(g_Linear, i.UV + float2( d.x,  d.y)).rgb;
    float3 c = (s0 + s1 + s2 + s3) * 0.25;

    float lum = dot(c, float3(0.2126, 0.7152, 0.0722));
    float factor = smoothstep(0.40, 1.05, lum);
    return float4(c * factor, 1.0);
}

float4 PSBloomBlur(FSOut i) : SV_Target
{
    float2 step = BlurDir.xy * Viewport.zw;
    float3 col = g_Scene.Sample(g_Linear, i.UV).rgb * 0.227027;
    col += g_Scene.Sample(g_Linear, i.UV + step * 1.384615).rgb * 0.316216;
    col += g_Scene.Sample(g_Linear, i.UV - step * 1.384615).rgb * 0.316216;
    col += g_Scene.Sample(g_Linear, i.UV + step * 3.230769).rgb * 0.070270;
    col += g_Scene.Sample(g_Linear, i.UV - step * 3.230769).rgb * 0.070270;
    return float4(col, 1.0);
}

float4 PSComposite(FSOut i) : SV_Target
{
    float3 scene = g_Scene.Sample(g_Linear, i.UV).rgb;
    float bloomWeight = max(0.0, VisualParams.z);
    float3 col = scene;
    if (bloomWeight > 0.001)
    {
        float3 bloom = g_Bloom.Sample(g_Linear, i.UV).rgb;
        col += bloom * bloomWeight;
    }

    // Subtle VFD / CRT micro-scanline and glass shimmer
    float shimmer = AtmosphereParams.w;
    if (shimmer > 0.001)
    {
        float scanline = sin(i.UV.y * Viewport.y * 3.14159) * 0.5 + 0.5;
        col *= (1.0 - (1.0 - scanline) * (shimmer * 0.18));
    }

    // ACES Tone Mapping
    float a = 2.51f;
    float b = 0.03f;
    float c = 2.43f;
    float d = 0.59f;
    float e = 0.14f;
    col = saturate((col * (a * col + b)) / (col * (c * col + d) + e));

    // Gamma correction
    col = pow(col, 1.0 / 2.2);

    // Dither to prevent 8-bit banding on dark gradients
    float2 screenPos = i.UV * Viewport.xy;
    float noise = (frac(sin(dot(screenPos, float2(12.9898, 78.233))) * 43758.5453) - 0.5) * (1.5 / 255.0);
    return float4(saturate(col + noise), 1.0);
}
";
    }
}
