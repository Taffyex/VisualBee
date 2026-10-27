using System;
using System.Runtime.InteropServices;

namespace MusicBeePlugin;

internal struct Vec2(float x, float y)
{
	public float X = x;

	public float Y = y;
}

internal struct Vec4(float x, float y, float z, float w)
{
	public float X = x;

	public float Y = y;

	public float Z = z;

	public float W = w;

	public static readonly Vec4 Zero = new Vec4(0f, 0f, 0f, 0f);

	public static Vec4 Colour(int argb, float opacity = 1f)
	{
		return new Vec4((float)((argb >> 16) & 0xFF) / 255f, (float)((argb >> 8) & 0xFF) / 255f, (float)(argb & 0xFF) / 255f, (float)((argb >> 24) & 0xFF) / 255f * opacity);
	}

	public Vec4 WithAlpha(float a)
	{
		return new Vec4(X, Y, Z, a);
	}

	public Vec4 Faded(float opacity)
	{
		return new Vec4(X, Y, Z, W * opacity);
	}

	public static Vec4 Lerp(Vec4 a, Vec4 b, float t)
	{
		return new Vec4(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t, a.W + (b.W - a.W) * t);
	}

	public static Vec4 operator *(Vec4 a, float k)
	{
		return new Vec4(a.X * k, a.Y * k, a.Z * k, a.W * k);
	}
}

internal struct Vertex(float x, float y, Vec4 c)
{
	public float X = x;

	public float Y = y;

	public float Pad0 = 0f;

	public float Pad1 = 0f;

	public Vec4 C = c;
}

internal struct TexQuad
{
	public Vec4 Dst;

	public Vec4 Src;

	public Vec4 Tint;

	public Vec4 Mode;

	public static TexQuad Make(float left, float top, float right, float bottom, float u0, float v0, float u1, float v1, Vec4 tint, bool mask, float opacity = 1f)
	{
		return new TexQuad
		{
			Dst = new Vec4(left, top, right, bottom),
			Src = new Vec4(u0, v0, u1, v1),
			Tint = tint,
			Mode = new Vec4(mask ? 1f : 0f, opacity, 0f, 0f)
		};
	}

	public TexQuad Round()
	{
		TexQuad result = this;
		result.Mode.Z = 1f;
		return result;
	}
}

internal struct Stroke
{
	public float Width;

	public float Softness;

	public float Opacity;

	public StrokeColouring Colouring;

	public Vec4 C0;

	public Vec4 C1;

	public Vec4 C2;

	public float From;

	public float To;

	public float Middle;

	public float LaneCentre;

	public float LaneHalf;

	public float Baseline;

	public static Stroke Solid(float width, Vec4 colour)
	{
		return new Stroke
		{
			Width = width,
			Softness = 1f,
			Opacity = 1f,
			C0 = colour,
			C1 = colour,
			C2 = colour,
			Middle = -1f
		};
	}

	public PassData ToPass()
	{
		return new PassData
		{
			P0 = new Vec4(Width * 0.5f, (Softness <= 0f) ? 1f : Softness, (float)Colouring, Opacity),
			P1 = C0,
			P2 = C1,
			P3 = C2,
			P4 = new Vec4(From, To, Middle, 0f),
			P5 = new Vec4(LaneCentre, LaneHalf, Baseline, 0f)
		};
	}
}

internal enum StrokeColouring
{
	Solid,
	AlongY,
	FromLaneCentre,
	AlongX,
	TowardsCurve
}

internal enum BlendMode
{
	Over,
	Add,
	Max,
	Behind,
	Mix,
	Replace
}
