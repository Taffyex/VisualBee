namespace MusicBeePlugin;

internal static partial class Shaders
{
	public const string PrimitiveSource = @"
struct LineInst
{
    float4 P;
    float4 C0;
    float4 C1;
    float4 Shape;
    float4 Seg;
};

StructuredBuffer<LineInst> Lines : register(t0);

struct LineOut
{
    float4 Pos : SV_Position;
    float2 Local : TEXCOORD0;
    nointerpolation uint Id : TEXCOORD1;
};

LineOut Segment(float2 a, float2 b, float reach, float ends, uint v, uint id)
{
    float2 d = b - a;
    float len = length(d);
    float2 dir = len > 1e-5 ? d / len : float2(1.0, 0.0);
    float2 nrm = float2(-dir.y, dir.x);
    float2 c = float2(v & 1, v >> 1);
    float along = lerp(-ends, len + ends, c.x);
    float across = lerp(-reach, reach, c.y);
    LineOut o;
    o.Pos = ToClip(a + dir * along + nrm * across);
    o.Local = float2(along, across);
    o.Id = id;
    return o;
}

float LineCover(float halfWidth, float soft, float cap, float beam, float along, float across, float len)
{
    float dist = cap > 0.5 && cap < 1.5 ? length(float2(along - clamp(along, 0.0, len), across)) : abs(across);
    if (beam > 0.5)
    {
        float sigma = max(halfWidth * 0.5, 0.35);
        return exp(-0.5 * dist * dist / (sigma * sigma));
    }
    float hw = max(halfWidth, 0.5);
    float cover = saturate((hw - dist) / soft + 0.5) * (halfWidth / hw);
    if (cap < 0.5)
        cover *= saturate(along / soft + 0.5) * saturate((len - along) / soft + 0.5);
    return cover;
}

LineOut LineVS(uint v : SV_VertexID, uint id : INDEX)
{
    LineInst l = Lines[id];
    float hw = max(l.Shape.x, 0.5);
    float reach = (l.Shape.w > 0.5 ? hw * 1.5 : hw) + l.Shape.y + 1.0;
    return Segment(l.P.xy, l.P.zw, reach, l.Shape.z > 1.5 ? 0.0 : reach, v, id);
}

float4 LinePS(LineOut i) : SV_Target
{
    LineInst l = Lines[i.Id];
    float len = length(l.P.zw - l.P.xy);
    float cover = LineCover(l.Shape.x, max(l.Shape.y, 1e-3), l.Shape.z, l.Shape.w, i.Local.x, i.Local.y, len);
    if (l.Seg.x > 0.0)
    {
        float a = i.Local.x - floor(i.Local.x / l.Seg.x) * l.Seg.x;
        cover *= saturate(a + 0.5) * saturate(l.Seg.y - a + 0.5);
    }
    float4 c = lerp(l.C0, l.C1, len > 1e-5 ? saturate(i.Local.x / len) : 0.0);
    return Premultiply(c, cover);
}

StructuredBuffer<float2> Points : register(t0);

float4 PolyColour(float2 pos)
{
    uint mode = (uint)P[0].z;
    float t = 0.0;
    if (mode == 1)
        t = Along(P[4].x, P[4].y, pos.y);
    else if (mode == 2)
        t = saturate(abs(pos.y - P[5].x) / max(P[5].y, 1e-3));
    else if (mode == 3)
        t = Along(P[4].x, P[4].y, pos.x);
    return Stops(P[1], P[2], P[3], P[4].z, t);
}

LineOut PolyVS(uint v : SV_VertexID, uint id : INDEX)
{
    float hw = max(P[0].x, 0.5);
    float reach = hw + P[0].y + 1.0;
    return Segment(Points[id], Points[id + 1], reach, reach, v, id);
}

float4 PolyPS(LineOut i) : SV_Target
{
    float len = length(Points[i.Id + 1] - Points[i.Id]);
    float cover = LineCover(P[0].x, max(P[0].y, 1e-3), 1.0, 0.0, i.Local.x, i.Local.y, len) * P[0].w;
    return Premultiply(PolyColour(i.Pos.xy), cover);
}

struct AreaOut
{
    float4 Pos : SV_Position;
    nointerpolation float4 Ends : TEXCOORD0;
};

AreaOut AreaVS(uint v : SV_VertexID, uint id : INDEX)
{
    float2 a = Points[id];
    float2 b = Points[id + 1];
    float baseline = P[5].z;
    float top = min(min(a.y, b.y), baseline) - 1.0;
    float bottom = max(max(a.y, b.y), baseline) + 1.0;
    float2 c = float2(v & 1, v >> 1);
    AreaOut o;
    o.Pos = ToClip(float2(lerp(a.x, b.x, c.x), lerp(top, bottom, c.y)));
    o.Ends = float4(a, b);
    return o;
}

float4 AreaPS(AreaOut i) : SV_Target
{
    float2 a = i.Ends.xy;
    float2 b = i.Ends.zw;
    float span = b.x - a.x;
    float curve = lerp(a.y, b.y, saturate((i.Pos.x - a.x) / (abs(span) < 1e-4 ? 1e-4 : span)));
    float baseline = P[5].z;
    float cover = baseline >= curve ? saturate(i.Pos.y - curve + 0.5) * saturate(baseline - i.Pos.y + 0.5)
                                    : saturate(curve - i.Pos.y + 0.5) * saturate(i.Pos.y - baseline + 0.5);
    float4 c = (uint)P[0].z == 4 ? Stops(P[1], P[2], P[3], P[4].z, saturate(abs(i.Pos.y - baseline) / max(abs(curve - baseline), 1.0)))
                                 : PolyColour(i.Pos.xy);
    return Premultiply(c, cover * P[0].w);
}

struct SpriteInst
{
    float4 Pos;
    float4 Col;
    float4 Motion;
};

StructuredBuffer<SpriteInst> Sprites : register(t0);

LineOut SpriteVS(uint v : SV_VertexID, uint id : INDEX)
{
    SpriteInst s = Sprites[id];
    float reach = s.Pos.z + 1.0;
    return Segment(s.Pos.xy - s.Motion.xy, s.Pos.xy, reach, reach, v, id);
}

float4 SpritePS(LineOut i) : SV_Target
{
    SpriteInst s = Sprites[i.Id];
    float len = length(s.Motion.xy);
    float along = i.Local.x;
    float dist = length(float2(along - clamp(along, 0.0, len), i.Local.y));
    float r = max(s.Pos.z, 0.25);
    uint shape = (uint)s.Pos.w;
    float cover;
    if (shape == 0)
    {
        float sigma = r * 0.45;
        cover = exp(-0.5 * dist * dist / (sigma * sigma));
    }
    else if (shape == 1)
        cover = saturate(r - dist + 0.5);
    else
        cover = saturate(r - dist + 0.5) * saturate(dist - r * 0.55 + 0.5);
    if (len > 1e-3)
        cover *= lerp(0.1, 1.0, saturate(along / len));
    return Premultiply(s.Col, cover);
}

struct Vert
{
    float2 Pos;
    float2 Pad;
    float4 Col;
};

StructuredBuffer<Vert> Verts : register(t0);

struct TriOut
{
    float4 Pos : SV_Position;
    float4 Col : COLOR0;
};

TriOut TriVS(uint v : INDEX)
{
    Vert x = Verts[v];
    TriOut o;
    o.Pos = ToClip(x.Pos);
    o.Col = x.Col;
    return o;
}

float4 TriPS(TriOut i) : SV_Target
{
    return Premultiply(i.Col, 1.0);
}

struct TexInst
{
    float4 Dst;
    float4 Src;
    float4 Tint;
    float4 Mode;
};

StructuredBuffer<TexInst> Quads : register(t0);

struct TexOut
{
    float4 Pos : SV_Position;
    float2 Uv : TEXCOORD0;
    nointerpolation uint Id : TEXCOORD1;
};

TexOut TexVS(uint v : SV_VertexID, uint id : INDEX)
{
    TexInst q = Quads[id];
    float2 c = float2(v & 1, v >> 1);
    TexOut o;
    o.Pos = ToClip(lerp(q.Dst.xy, q.Dst.zw, c));
    o.Uv = lerp(q.Src.xy, q.Src.zw, c);
    o.Id = id;
    return o;
}

float4 TexPS(TexOut i) : SV_Target
{
    TexInst q = Quads[i.Id];
    float4 s;
    if (q.Mode.z > 0.5)
    {
        float2 dx = ddx(i.Uv) * 0.25, dy = ddy(i.Uv) * 0.25;
        s = (Image.Sample(LinearClamp, i.Uv + dx + dy) + Image.Sample(LinearClamp, i.Uv + dx - dy)
           + Image.Sample(LinearClamp, i.Uv - dx + dy) + Image.Sample(LinearClamp, i.Uv - dx - dy)) * 0.25;
    }
    else
        s = Image.Sample(LinearClamp, i.Uv);
    float4 c = (uint)q.Mode.x == 1 ? float4(q.Tint.rgb, q.Tint.a * s.g) : s * q.Tint;
    if (q.Mode.z > 0.5)
    {
        float2 size = q.Dst.zw - q.Dst.xy;
        c.a *= saturate(min(size.x, size.y) * 0.5 - length(i.Pos.xy - (q.Dst.xy + q.Dst.zw) * 0.5) + 0.5);
    }
    return Premultiply(c, q.Mode.y);
}
";
}
