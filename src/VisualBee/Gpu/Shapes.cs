using System;
using System.Runtime.InteropServices;

namespace MusicBeePlugin;

internal enum LineCap
{
	Butt,
	Round,
	Exact
}

internal struct LineShape
{
	public Vec4 P;

	public Vec4 C0;

	public Vec4 C1;

	public Vec4 Shape;

	public Vec4 Seg;

	public static LineShape Make(float x0, float y0, float x1, float y1, float width, Vec4 c0, Vec4 c1, LineCap cap)
	{
		return new LineShape
		{
			P = new Vec4(x0, y0, x1, y1),
			C0 = c0,
			C1 = c1,
			Shape = new Vec4(width * 0.5f, 1f, (float)cap, 0f)
		};
	}

	public LineShape Beam()
	{
		Shape.W = 1f;
		return this;
	}

	public LineShape Soft(float softness)
	{
		Shape.Y = softness;
		return this;
	}

	public LineShape Dashes(float period, float lit)
	{
		Seg = new Vec4(period, lit, 0f, 0f);
		return this;
	}
}

[Flags]
internal enum RectFlags
{
	None = 0,
	GradientAlongX = 1,
	SegmentsAlongX = 2,
	ColourPerSegment = 4,
	SplitTopBottom = 8,
	SplitLeftRight = 0x10,
	GlassTube = 0x20,
	VfdRung = 0x40,
	LacquerReflection = 0x80,
	HdrCore = 0x100,
	VfdMesh = 0x200,
	Pill3D = 0x400
}

internal struct RectShape
{
	public Vec4 Rect;

	public Vec4 C0;

	public Vec4 C1;

	public Vec4 C2;

	public Vec4 Shape;

	public Vec4 Grad;

	public Vec4 Seg;

	public static RectShape Solid(float left, float top, float right, float bottom, Vec4 colour)
	{
		return new RectShape
		{
			Rect = new Vec4(left, top, right, bottom),
			C0 = colour,
			C1 = colour,
			C2 = colour,
			Shape = new Vec4(0f, 1f, 0f, 0f),
			Grad = new Vec4(top, bottom, -1f, 0f)
		};
	}

	public RectShape Rounded(float radius, float softness = 1f)
	{
		Shape.X = radius;
		Shape.Y = softness;
		return this;
	}

	public RectShape Gradient(Vec4 c0, Vec4 c1, float from, float to)
	{
		C0 = c0;
		C1 = c1;
		C2 = c1;
		Grad = new Vec4(from, to, -1f, Grad.W);
		return this;
	}

	public RectShape Gradient(Vec4 c0, Vec4 middle, Vec4 c2, float at, float from, float to)
	{
		C0 = c0;
		C1 = middle;
		C2 = c2;
		Grad = new Vec4(from, to, at, Grad.W);
		return this;
	}

	public RectShape Segments(float period, float lit, float origin)
	{
		Seg = new Vec4(period, lit, origin, 0f);
		return this;
	}

	public RectShape Flags(RectFlags flags)
	{
		Shape.W = (int)Shape.W | (int)flags;
		return this;
	}

	public RectShape Corners(float first, float second, bool alongX = false)
	{
		Shape.X = first;
		Grad.W = second;
		return Flags(alongX ? RectFlags.SplitLeftRight : RectFlags.SplitTopBottom);
	}

	public RectShape Glass(float activePower, float refraction, float wall = 0.18f, float tint = 0.22f, float specular = 1.0f)
	{
		Seg = new Vec4(wall, tint, specular, activePower);
		Shape.Z = refraction;
		return Flags(RectFlags.GlassTube);
	}

	public RectShape Vfd(float period, float lit, float origin, float power, bool hdr, bool mesh)
	{
		Seg = new Vec4(period, lit, origin, power);
		RectFlags f = RectFlags.VfdRung | RectFlags.ColourPerSegment;
		if (hdr) f |= RectFlags.HdrCore;
		if (mesh) f |= RectFlags.VfdMesh;
		return Flags(f);
	}

	public RectShape Lacquer()
	{
		return Flags(RectFlags.LacquerReflection);
	}

	public RectShape Pill()
	{
		return Flags(RectFlags.Pill3D);
	}

	public RectShape Outline(float width)
	{
		Shape.Z = width;
		return this;
	}
}

internal enum SpriteKind
{
	Glow,
	Disc,
	Ring
}

internal struct SpriteShape
{
	public Vec4 Pos;

	public Vec4 Col;

	public Vec4 Motion;

	public static SpriteShape Make(float x, float y, float radius, SpriteKind kind, Vec4 colour, float streakX = 0f, float streakY = 0f)
	{
		return new SpriteShape
		{
			Pos = new Vec4(x, y, radius, (float)kind),
			Col = colour,
			Motion = new Vec4(streakX, streakY, 0f, 0f)
		};
	}
}

internal struct Frame
{
	public GpuDevice Gpu;

	public Canvas Canvas;

	public GpuTexture Background;

	public GpuTexture Content;

	public Look Look;

	public Vec4[] Colours;

	public int Width;

	public int Height;

	public float Dt;

	public float Time;

	public float Scale;

	public bool LookChanged;

	public bool SizeChanged;
}

internal struct Effects
{
	public bool Bloom;

	public float BloomStrength;

	public float BloomThreshold;

	public float BloomRadius;

	public Vec4 BloomTint;

	public bool Trails;

	public float TrailSeconds;

	public bool Shadow;

	public float ShadowOpacity;

	public float ShadowBlur;

	public float ShadowX;

	public float ShadowY;

	public Vec4 ShadowColour;

	public float Vignette;

	public float VignetteStart;

	public float Scanlines;

	public float Aberration;

	public float Grain;

	public bool Dither;

	public Vec4 Flash;
}
