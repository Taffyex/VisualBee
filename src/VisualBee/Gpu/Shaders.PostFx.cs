namespace MusicBeePlugin;

internal static partial class Shaders
{
	public const string PostFxSource = @"
Texture2D Src0 : register(t0);
Texture2D Src1 : register(t1);
Texture2D Src2 : register(t2);
Texture2D Src3 : register(t3);

struct FullOut
{
    float4 Pos : SV_Position;
    float2 Uv : TEXCOORD0;
};

FullOut FullVS(uint v : SV_VertexID)
{
    float2 uv = float2((v << 1) & 2, v & 2);
    FullOut o;
    o.Pos = float4(uv.x * 2.0 - 1.0, 1.0 - uv.y * 2.0, 0.0, 1.0);
    o.Uv = uv;
    return o;
}

float4 ZeroPS(FullOut i) : SV_Target
{
    return float4(0.0, 0.0, 0.0, 0.0);
}

float4 DownPS(FullOut i) : SV_Target
{
    float2 t = P[0].xy;
    float2 uv = i.Uv;
    float4 a = Src0.Sample(LinearClamp, uv + t * float2(-2.0, -2.0));
    float4 b = Src0.Sample(LinearClamp, uv + t * float2(0.0, -2.0));
    float4 c = Src0.Sample(LinearClamp, uv + t * float2(2.0, -2.0));
    float4 d = Src0.Sample(LinearClamp, uv + t * float2(-1.0, -1.0));
    float4 e = Src0.Sample(LinearClamp, uv + t * float2(1.0, -1.0));
    float4 f = Src0.Sample(LinearClamp, uv + t * float2(-2.0, 0.0));
    float4 g = Src0.Sample(LinearClamp, uv);
    float4 h = Src0.Sample(LinearClamp, uv + t * float2(2.0, 0.0));
    float4 j = Src0.Sample(LinearClamp, uv + t * float2(-1.0, 1.0));
    float4 k = Src0.Sample(LinearClamp, uv + t * float2(1.0, 1.0));
    float4 l = Src0.Sample(LinearClamp, uv + t * float2(-2.0, 2.0));
    float4 m = Src0.Sample(LinearClamp, uv + t * float2(0.0, 2.0));
    float4 n = Src0.Sample(LinearClamp, uv + t * float2(2.0, 2.0));
    float4 s = (d + e + j + k) * 0.125 + (a + c + l + n) * 0.03125 + (b + f + h + m) * 0.0625 + g * 0.125;
    if (P[1].x > 0.5)
    {
        float bright = max(s.r, max(s.g, s.b));
        float knee = max(P[0].w, 1e-4);
        float soft = clamp(bright - P[0].z + knee, 0.0, 2.0 * knee);
        soft = soft * soft / (4.0 * knee);
        s *= max(soft, bright - P[0].z) / max(bright, 1e-4);
    }
    return s;
}

float4 UpPS(FullOut i) : SV_Target
{
    float2 t = P[0].xy * P[0].z;
    float2 uv = i.Uv;
    float4 s = Src0.Sample(LinearClamp, uv + t * float2(-1.0, -1.0));
    s += Src0.Sample(LinearClamp, uv + t * float2(0.0, -1.0)) * 2.0;
    s += Src0.Sample(LinearClamp, uv + t * float2(1.0, -1.0));
    s += Src0.Sample(LinearClamp, uv + t * float2(-1.0, 0.0)) * 2.0;
    s += Src0.Sample(LinearClamp, uv) * 4.0;
    s += Src0.Sample(LinearClamp, uv + t * float2(1.0, 0.0)) * 2.0;
    s += Src0.Sample(LinearClamp, uv + t * float2(-1.0, 1.0));
    s += Src0.Sample(LinearClamp, uv + t * float2(0.0, 1.0)) * 2.0;
    s += Src0.Sample(LinearClamp, uv + t * float2(1.0, 1.0));
    return s * (P[0].w / 16.0);
}

float4 TrailPS(FullOut i) : SV_Target
{
    int3 px = int3(i.Pos.xy, 0);
    float4 old = Src0.Load(px) * P[0].x;
    float4 now = Src1.Load(px);
    return max(old, now);
}

float4 PhosphorPS(FullOut i) : SV_Target
{
    float4 e = Src0.Load(int3(i.Pos.xy, 0));
    float t = sqrt(saturate(e.a * P[0].x));
    float3 trace = P[0].w > 0.5 && e.a > 1e-7 ? saturate(e.rgb / e.a) : P[1].rgb;
    float start = max(P[0].y, 1e-3);
    if (t <= start)
    {
        float a = t / start;
        return float4(trace * a, a);
    }
    float3 c = lerp(trace, P[2].rgb, (t - start) / max(1.0 - start, 1e-3) * P[0].z);
    return float4(c, 1.0);
}

float4 BackgroundPS(FullOut i) : SV_Target
{
    uint mode = (uint)P[3].x;
    float t = 0.0;
    if (mode == 1)
    {
        float2 dir = float2(sin(P[3].y), cos(P[3].y));
        float reach = 0.5 * (abs(dir.x) + abs(dir.y));
        t = dot(i.Uv - 0.5, dir) / max(reach, 1e-4) * 0.5 + 0.5;
    }
    else if (mode == 2)
    {
        float2 d = (i.Uv - P[3].zw) * float2(ViewSize.x / max(ViewSize.y, 1.0), 1.0);
        t = length(d) / max(P[4].x, 1e-3);
    }
    float4 c = mode == 0 ? P[0] : Stops(P[0], P[4].y < 0.0 ? P[1] : P[2], P[1], P[4].y, saturate(t));

    if (P[5].x > 0.5)
    {
        float horizon = (P[6].x > 0.01) ? P[6].x : 0.60;
        if (i.Uv.y > horizon)
        {
            float depth = (i.Uv.y - horizon) / max(1.0 - horizon, 0.01);
            float z = 1.0 / max(depth, 0.005);
            float aspect = ViewSize.x / max(ViewSize.y, 1.0);
            float x = (i.Uv.x - 0.5) * aspect * z;
            float speed = P[5].y * 2.5;
            float scroll = Time * speed;
            float density = (P[5].z > 0.05 ? P[5].z : 0.50) * 12.0;
            float2 gridCoord = float2(x * density * 0.15, (z + scroll) * density * 0.35);
            float2 gridFract = abs(frac(gridCoord - 0.5) - 0.5);
            float2 gridDeriv = fwidth(gridCoord);
            float2 gridLine = smoothstep(gridDeriv * 1.5, float2(0.0, 0.0), gridFract);
            float gridIntensity = max(gridLine.x, gridLine.y);
            float fog = smoothstep(0.0, 0.25, depth);
            float pulse = 1.0 + P[5].w * 1.6;
            float3 gridCol = P[7].rgb * (gridIntensity * fog * pulse * P[5].z);
            gridCol += P[7].rgb * (depth * 0.15 * pulse * P[5].z);
            c.rgb += gridCol;
        }
        else
        {
            float distToHorizon = abs(i.Uv.y - horizon);
            float horizonGlow = exp(-distToHorizon * 220.0) * (0.3 + 0.7 * P[5].w) * P[5].z;
            c.rgb += P[7].rgb * horizonGlow;
        }
    }
    return float4(c.rgb, 1.0);
}

float Hash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float ValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float a = Hash21(i);
    float b = Hash21(i + float2(1.0, 0.0));
    float c = Hash21(i + float2(0.0, 1.0));
    float d = Hash21(i + float2(1.0, 1.0));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float Fbm(float2 p)
{
    float s = 0.0;
    float w = 0.5;
    for (int k = 0; k < 3; k++)
    {
        s += ValueNoise(p) * w;
        p = p * 2.03 + float2(17.1, 9.2);
        w *= 0.5;
    }
    return s;
}

float4 AuroraPS(FullOut i) : SV_Target
{
    float2 uv = i.Uv;
    float aspect = ViewSize.x / max(ViewSize.y, 1.0);
    float2 p = float2(uv.x * aspect, uv.y) * P[3].z;
    float t = Time * P[3].x;
    float n1 = Fbm(p * float2(0.7, 1.6) + float2(t * 0.11, -t * 0.04));
    float n2 = Fbm(p * float2(1.3, 0.9) + float2(-t * 0.07, t * 0.09) + n1 * 1.7);
    float band = smoothstep(0.25, 0.85, n2) * lerp(1.0, 0.35, uv.y);
    float3 col = lerp(P[0].rgb, P[1].rgb, saturate(n1 * 1.6 - 0.3));
    col = lerp(col, P[2].rgb, saturate((n2 - 0.45) * 2.2));
    float amount = band * P[3].y * (0.55 + 0.45 * P[3].w);
    return float4(P[4].rgb + col * amount, 1.0);
}

float Ign(float2 p)
{
    return frac(52.9829189 * frac(dot(p, float2(0.06711056, 0.00583715))));
}

float3 AcesFilmic(float3 x)
{
    float a = 2.51;
    float b = 0.03;
    float c = 2.43;
    float d = 0.59;
    float e = 0.14;
    return saturate((x * (a * x + b)) / (x * (c * x + d) + e));
}

float4 CompositePS(FullOut i) : SV_Target
{
    int3 px = int3(i.Pos.xy, 0);
    float2 uv = i.Uv;
    uint flags = (uint)P[3].w;
    float3 bg = Src0.Load(px).rgb;
    if (flags & 2)
    {
        float shadow = Src3.Sample(LinearClamp, uv - P[0].zw * InvViewSize).a * P[0].y;
        bg = lerp(bg, P[2].rgb, saturate(shadow));
    }
    float4 fg;
    if (P[3].x > 0.0)
    {
        float2 shift = (uv - 0.5) * 2.0 * P[3].x * InvViewSize;
        float4 r = Src1.Sample(LinearClamp, uv + shift);
        float4 g = Src1.Load(px);
        float4 b = Src1.Sample(LinearClamp, uv - shift);
        fg = float4(r.r, g.g, b.b, max(g.a, max(r.a, b.a)));
    }
    else
        fg = Src1.Load(px);
    float3 col = bg * (1.0 - saturate(fg.a)) + fg.rgb;
    if (flags & 1)
        col += Src2.Sample(LinearClamp, uv).rgb * P[0].x * P[1].rgb;
    col += P[4].rgb * P[4].a;
    if (P[1].w > 0.0)
    {
        float r = length(uv - 0.5) * 1.41421356;
        col *= 1.0 - P[1].w * smoothstep(P[3].z, 1.05, r);
    }
    if (P[2].w > 0.0)
        col *= 1.0 - P[2].w * fmod(floor(i.Pos.y), 2.0);
    if (P[3].y > 0.0)
        col += (Ign(i.Pos.xy + frac(Time * 7.31) * 413.0) - 0.5) * P[3].y;
    col = AcesFilmic(col);
    if (flags & 4)
        col += (Ign(i.Pos.xy) - 0.5) * (0.98 / 255.0);
    return float4(saturate(col), 1.0);
}
";

	public static readonly string Source = HeaderAndRectSource + PrimitiveSource + PostFxSource;
}
