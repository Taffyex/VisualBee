namespace MusicBeePlugin;

internal static partial class Shaders
{
	public const string HeaderAndRectSource = @"
cbuffer Frame : register(b0)
{
    float2 ViewSize;
    float2 InvViewSize;
    float Time;
    float3 FramePad;
};

cbuffer PassParams : register(b1)
{
    float4 P[8];
};

SamplerState LinearClamp : register(s0);
SamplerState PointClamp : register(s1);

float4 ToClip(float2 p)
{
    return float4(p.x * InvViewSize.x * 2.0 - 1.0, 1.0 - p.y * InvViewSize.y * 2.0, 0.0, 1.0);
}

float4 Premultiply(float4 c, float cover)
{
    float a = saturate(c.a * cover);
    return float4(c.rgb * a, a);
}

float Along(float from, float to, float x)
{
    float span = to - from;
    if (abs(span) < 1e-4)
        span = 1e-4;
    return saturate((x - from) / span);
}

float4 Stops(float4 c0, float4 c1, float4 c2, float middle, float t)
{
    if (middle < 0.0)
        return lerp(c0, c1, t);
    if (t < middle)
        return lerp(c0, c1, t / max(middle, 1e-5));
    return lerp(c1, c2, (t - middle) / max(1.0 - middle, 1e-5));
}

struct RectInst
{
    float4 Rect;
    float4 C0;
    float4 C1;
    float4 C2;
    float4 Shape;
    float4 Grad;
    float4 Seg;
};

StructuredBuffer<RectInst> Rects : register(t0);
Texture2D Image : register(t1);

struct RectOut
{
    float4 Pos : SV_Position;
    float2 P : TEXCOORD0;
    nointerpolation uint Id : TEXCOORD1;
};

RectOut RectVS(uint v : SV_VertexID, uint id : INDEX)
{
    RectInst r = Rects[id];
    float pad = r.Shape.y + 1.0;
    float2 c = float2(v & 1, v >> 1);
    float2 p = lerp(r.Rect.xy - pad, r.Rect.zw + pad, c);
    RectOut o;
    o.Pos = ToClip(p);
    o.P = p;
    o.Id = id;
    return o;
}

float RoundBox(float2 p, float2 halfSize, float radius)
{
    float2 q = abs(p) - halfSize + radius;
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
}

float4 RectPS(RectOut i) : SV_Target
{
    RectInst r = Rects[i.Id];
    float2 halfSize = max((r.Rect.zw - r.Rect.xy) * 0.5, 0.0);
    float2 centre = (r.Rect.xy + r.Rect.zw) * 0.5;
    uint flags = (uint)r.Shape.w;
    float2 rel = i.P - centre;
    float corner = r.Shape.x;
    if ((flags & 8) && rel.y > 0.0)
        corner = r.Grad.w;
    if ((flags & 16) && rel.x > 0.0)
        corner = r.Grad.w;
    float dist = RoundBox(rel, halfSize, corner);
    float soft = max(r.Shape.y, 1e-3);
    float cover = saturate(-dist / soft + 0.5);
    if (r.Shape.z > 0.0 && !(flags & 32))
    {
        float inner = dist + r.Shape.z;
        cover *= saturate(inner / soft + 0.5);
    }
    float axis = (flags & 1) ? i.P.x : i.P.y;
    float nx = halfSize.x > 0.5 ? clamp(rel.x / halfSize.x, -1.0, 1.0) : 0.0;
    float a = 0.0;
    if (r.Seg.x > 0.0 && !(flags & 32))
    {
        float s = (flags & 2) ? i.P.x : i.P.y;
        float local = (s - r.Seg.z) / r.Seg.x;
        float cell = floor(local);
        a = (local - cell) * r.Seg.x;
        cover *= saturate(a + 0.5) * saturate(r.Seg.y - a + 0.5);
        if (flags & 4)
            axis = r.Seg.z + cell * r.Seg.x + r.Seg.y * 0.5;
    }
    float4 c = Stops(r.C0, r.C1, r.C2, r.Grad.z, Along(r.Grad.x, r.Grad.y, axis));
    if (flags & 1024)
    {
        float nz = sqrt(max(0.0, 1.0 - nx * nx));
        c.rgb *= (0.75 + 0.35 * nz);
    }
    if (flags & 64)
    {
        float segH = max(r.Seg.y, 1.0);
        float uBevel = saturate(a / segH);
        float bevel = (0.78 + 0.32 * sin(uBevel * 3.14159)) * (0.72 + 0.28 * sqrt(max(0.0, 1.0 - nx * nx)));
        if (flags & 512)
        {
            float mesh = 1.0 - 0.12 * smoothstep(0.3, 0.85, sin(i.P.x * 1.5) * sin(i.P.y * 1.5));
            bevel *= mesh;
        }
        c.rgb *= bevel;
        float power = r.Seg.w;
        if ((flags & 256) && power > 0.55)
        {
            float overdrive = smoothstep(0.55, 0.88, power);
            float coreX = 1.0 - smoothstep(0.0, 0.72, abs(nx));
            c.rgb = lerp(c.rgb, float3(1.35, 1.40, 1.45), overdrive * coreX * 0.92);
        }
    }
    if (flags & 128)
    {
        float3 floorTint = float3(0.78, 0.88, 1.05);
        c.rgb = c.rgb * floorTint * 0.85;
    }
    if (flags & 32)
    {
        float wall = saturate((r.Seg.x > 1.0) ? (r.Seg.x * 0.01) : ((r.Seg.x > 0.0001) ? r.Seg.x : 0.18));
        float bodyTint = saturate((r.Seg.y > 1.0) ? (r.Seg.y * 0.01) : ((r.Seg.y > 0.0001) ? r.Seg.y : 0.22));
        float specFactor = (r.Seg.z > 1.5) ? (r.Seg.z * 0.01) : ((r.Seg.z > 0.0001) ? r.Seg.z : 1.0);
        float activePower = saturate(r.Seg.w);

        float domeTop = max(0.0, halfSize.y - halfSize.x);
        float ny = (rel.y < -domeTop && halfSize.x > 0.5) ? (rel.y + domeTop) / halfSize.x : 0.0;
        float rad = sqrt(nx * nx + ny * ny);
        float nz = sqrt(max(0.0, 1.0 - min(1.0, rad * rad)));
        float3 N = float3(nx, ny, nz);

        float refr = r.Shape.z > 0.0 ? r.Shape.z : 0.65;
        float2 uv = i.Pos.xy * InvViewSize;
        float2 dUv = -N.xy * (1.0 - nz) * halfSize.x * InvViewSize * (refr * 0.85);

        float3 refrDelta = float3(0.0, 0.0, 0.0);
        if (dot(dUv, dUv) > 1e-7)
        {
            float3 bgR = Image.Sample(LinearClamp, uv + dUv * 0.85).rgb;
            float3 bgG = Image.Sample(LinearClamp, uv + dUv * 1.00).rgb;
            float3 bgB = Image.Sample(LinearClamp, uv + dUv * 1.18).rgb;
            refrDelta = max(float3(0.0, 0.0, 0.0), (float3(bgR.r, bgG.g, bgB.b) - bgG) * (0.5 + 0.5 * refr));
        }

        float innerR = saturate(1.0 - wall);
        float wallEdge = smoothstep(0.0, 0.04, abs(rad - innerR));
        float wallFresnel = smoothstep(innerR, 1.0, rad);

        float onz = 1.0 - nz;
        float onz2 = onz * onz;
        float fresnel = 0.04 + 0.96 * (onz2 * onz2);
        float specAmp = specFactor * (0.15 + 0.35 * activePower);
        float spec1 = exp(-55.0 * (nx + 0.55) * (nx + 0.55)) * nz;
        float spec2 = 0.40 * exp(-85.0 * (nx - 0.62) * (nx - 0.62)) * nz;
        float domeSpec = (ny < -0.15) ? exp(-40.0 * (rad - 0.88) * (rad - 0.88)) * abs(ny) : 0.0;
        float specular = (spec1 + spec2 + domeSpec) * specAmp;

        float wallCaustic = activePower * (wallFresnel * sqrt(wallFresnel)) * 0.45;
        float domeCaustic = activePower * smoothstep(0.5, 1.0, rad) * (ny < -0.15 ? 0.35 : 0.0);

        float3 glassTint = lerp(r.C1.rgb, r.C2.rgb, 0.5);
        float3 glassCol = refrDelta + glassTint * (0.04 + bodyTint * (0.12 + 0.45 * wallFresnel));
        glassCol *= lerp(0.75, 1.0, wallEdge);
        glassCol += glassTint * (wallCaustic + domeCaustic + activePower * 0.45);
        glassCol += float3(specular, specular, specular);
        glassCol += glassTint * (fresnel * (0.08 + 0.25 * activePower));
        glassCol = clamp(glassCol, 0.0, 1.5);

        float glassAlpha = saturate(0.06 + 0.20 * bodyTint + 0.42 * wallFresnel + fresnel * 0.18 + activePower * 0.30 + specular * 0.35);
        c = float4(glassCol, glassAlpha);
    }
    return Premultiply(c, cover);
}
";
}
