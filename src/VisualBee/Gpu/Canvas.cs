using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MusicBeePlugin;

internal sealed partial class Canvas : IDisposable
{
	private enum Kind
	{
		Rect,
		Line,
		Sprite,
		Quad,
		Triangles,
		Poly,
		Area
	}

	private struct Command
	{
		public Kind Kind;

		public BlendMode Blend;

	public GpuTexture SceneBackdrop;

		public int First;

		public int Count;

		public GpuTexture Texture;

		public int Pass;
	}

	private sealed class Stream<T> where T : unmanaged
	{
		public T[] Items = new T[64];

		public int Count;

		public IntPtr Buffer;

		public IntPtr View;

		private int _capacity;

		public int Add(ref T item)
		{
			if (Count == Items.Length)
			{
				Array.Resize(ref Items, Items.Length * 2);
			}
			Items[Count] = item;
			return Count++;
		}

		public int Reserve(int n)
		{
			if (Count + n > Items.Length)
			{
				Array.Resize(ref Items, Math.Max(Items.Length * 2, Count + n));
			}
			int count = Count;
			Count += n;
			return count;
		}

		public unsafe bool Upload(GpuDevice gpu)
		{
			if (Count == 0)
			{
				return true;
			}
			if (Count > _capacity)
			{
				Free();
				int num = Math.Max(Count, Math.Max(256, _capacity * 2));
				if (!gpu.CreateStructured(sizeof(T), num, out Buffer, out View))
				{
					return false;
				}
				_capacity = num;
			}
			fixed (T* items = Items)
			{
				return gpu.Upload(Buffer, items, Count * sizeof(T));
			}
		}

		public void Free()
		{
			D3D.Release(ref View);
			D3D.Release(ref Buffer);
			_capacity = 0;
		}
	}

	private readonly GpuDevice _gpu;

	private readonly ID3D11DeviceContext _ctx;

	private readonly Stream<RectShape> _rects = new Stream<RectShape>();

	private readonly Stream<LineShape> _lines = new Stream<LineShape>();

	private readonly Stream<SpriteShape> _sprites = new Stream<SpriteShape>();

	private readonly Stream<TexQuad> _quads = new Stream<TexQuad>();

	private readonly Stream<Vertex> _verts = new Stream<Vertex>();

	private readonly Stream<Vec2> _points = new Stream<Vec2>();

	private readonly List<Command> _commands = new List<Command>();

	private readonly List<PassData> _passes = new List<PassData>();

	private IntPtr _rectVs;

	private IntPtr _rectPs;

	private IntPtr _lineVs;

	private IntPtr _linePs;

	private IntPtr _polyVs;

	private IntPtr _polyPs;

	private IntPtr _areaVs;

	private IntPtr _areaPs;

	private IntPtr _spriteVs;

	private IntPtr _spritePs;

	private IntPtr _triVs;

	private IntPtr _triPs;

	private IntPtr _texVs;

	private IntPtr _texPs;

	public IntPtr FullVs;

	public IntPtr ZeroPs;

	public IntPtr DownPs;

	public IntPtr UpPs;

	public IntPtr TrailPs;

	public IntPtr PhosphorPs;

	public IntPtr BackgroundPs;

	public IntPtr AuroraPs;

	public IntPtr CompositePs;

	private IntPtr _over;

	private IntPtr _add;

	private IntPtr _max;

	private IntPtr _behind;

	private IntPtr _mix;

	private IntPtr _replace;

	private IntPtr _decay;

	private IntPtr _raster;

	private IntPtr _linear;

	private IntPtr _point;

	private IntPtr _frameBuffer;

	private IntPtr _passBuffer;

	private IntPtr _indices;

	private IntPtr _perInstance;

	private IntPtr _perVertex;

	private IntPtr _boundLayout;

	private int _topology = -1;

	private bool _stateSet;

	private bool _frameDirty = true;

	private IntPtr _target;

	private int _targetW;

	private int _targetH;

	private float _time;

	private readonly IntPtr[] _one = new IntPtr[1];

	private readonly IntPtr[] _two = new IntPtr[2];

	private readonly IntPtr[] _four = new IntPtr[4];

	private readonly IntPtr[] _noViews = new IntPtr[4];

	private readonly IntPtr[] _constants = new IntPtr[2];

	private readonly Viewport[] _viewport = new Viewport[1];

	private readonly float[] _clear = new float[4];

	private readonly float[] _factor = new float[4];

	private IntPtr _boundVs;

	private IntPtr _boundPs;

	private IntPtr _boundBlend;

	private IntPtr _boundView;

	private IntPtr _boundTexture;

	private float _boundFactor = -1f;

	public BlendMode Blend;

	public GpuTexture SceneBackdrop;

	public int TargetWidth => _targetW;

	public int TargetHeight => _targetH;

	private Canvas(GpuDevice gpu)
	{
		_gpu = gpu;
		_ctx = gpu.Context;
	}

	public static Canvas Create(GpuDevice gpu, out string error)
	{
		Canvas canvas = new Canvas(gpu);
		error = canvas.Build();
		if (error == null)
		{
			return canvas;
		}
		canvas.Dispose();
		return null;
	}


	public void Dispose()
	{
		_rects.Free();
		_lines.Free();
		_sprites.Free();
		_quads.Free();
		_verts.Free();
		_points.Free();
		IntPtr[] array = new IntPtr[38]
		{
			_rectVs, _rectPs, _lineVs, _linePs, _polyVs, _polyPs, _areaVs, _areaPs, _spriteVs, _spritePs,
			_triVs, _triPs, _texVs, _texPs, FullVs, ZeroPs, DownPs, UpPs, TrailPs, PhosphorPs,
			BackgroundPs, AuroraPs, CompositePs, _over, _add, _max, _behind, _mix, _replace, _decay,
			_raster, _linear, _point, _frameBuffer, _passBuffer, _indices, _perInstance, _perVertex
		};
		for (int i = 0; i < array.Length; i++)
		{
			IntPtr p = array[i];
			D3D.Release(ref p);
		}
		_rectVs = (_rectPs = (_lineVs = (_linePs = (_polyVs = (_polyPs = (_areaVs = (_areaPs = IntPtr.Zero)))))));
		_spriteVs = (_spritePs = (_triVs = (_triPs = (_texVs = (_texPs = IntPtr.Zero)))));
		FullVs = (ZeroPs = (DownPs = (UpPs = (TrailPs = (PhosphorPs = (BackgroundPs = (AuroraPs = (CompositePs = IntPtr.Zero))))))));
		_over = (_add = (_max = (_behind = (_mix = (_replace = (_decay = (_raster = (_linear = (_point = IntPtr.Zero)))))))));
		_frameBuffer = (_passBuffer = (_indices = (_perInstance = (_perVertex = (_boundLayout = IntPtr.Zero)))));
	}
}
