using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MusicBeePlugin;

internal sealed partial class Canvas
{
	public void Rect(RectShape r, GpuTexture backdrop = null)
	{
		Queue(Kind.Rect, _rects.Add(ref r), backdrop, -1);
	}

	public void Line(LineShape l)
	{
		Queue(Kind.Line, _lines.Add(ref l), null, -1);
	}

	public void Sprite(SpriteShape s)
	{
		Queue(Kind.Sprite, _sprites.Add(ref s), null, -1);
	}

	public void Quad(GpuTexture texture, TexQuad q)
	{
		if (texture != null)
		{
			Queue(Kind.Quad, _quads.Add(ref q), texture, -1);
		}
	}

	public void Triangles(Vertex[] vertices, int count)
	{
		if (count >= 3)
		{
			int num = _verts.Reserve(count);
			Array.Copy(vertices, 0, _verts.Items, num, count);
			Command item = new Command
			{
				Kind = Kind.Triangles,
				Blend = Blend,
				First = num,
				Count = count,
				Pass = -1
			};
			_commands.Add(item);
		}
	}

	public void Polyline(float[] xy, int count, Stroke stroke)
	{
		Points(Kind.Poly, xy, count, stroke);
	}

	public void Area(float[] xy, int count, Stroke fill)
	{
		Points(Kind.Area, xy, count, fill);
	}

	private unsafe void Points(Kind kind, float[] xy, int count, Stroke stroke)
	{
		if (count < 2)
		{
			return;
		}
		int num = _points.Reserve(count);
		fixed (float* source = xy)
		{
			fixed (Vec2* destination = &_points.Items[num])
			{
				Buffer.MemoryCopy(source, destination, count * 8, count * 8);
			}
		}
		_passes.Add(stroke.ToPass());
		Command item = new Command
		{
			Kind = kind,
			Blend = Blend,
			First = num,
			Count = count,
			Pass = _passes.Count - 1
		};
		_commands.Add(item);
	}

	private void Queue(Kind kind, int index, GpuTexture texture, int pass)
	{
		int num = _commands.Count - 1;
		if (num >= 0)
		{
			Command value = _commands[num];
			if (value.Kind == kind && value.Blend == Blend && value.Texture == texture && value.Pass < 0 && value.First + value.Count == index)
			{
				value.Count++;
				_commands[num] = value;
				return;
			}
		}
		_commands.Add(new Command
		{
			Kind = kind,
			Blend = Blend,
			First = index,
			Count = 1,
			Texture = texture,
			Pass = pass
		});
	}

	public void Flush()
	{
		if (_commands.Count == 0)
		{
			return;
		}
		bool ok = true;
		if (_rects.Count > 0) ok &= _rects.Upload(_gpu);
		if (_lines.Count > 0) ok &= _lines.Upload(_gpu);
		if (_sprites.Count > 0) ok &= _sprites.Upload(_gpu);
		if (_quads.Count > 0) ok &= _quads.Upload(_gpu);
		if (_verts.Count > 0) ok &= _verts.Upload(_gpu);
		if (_points.Count > 0) ok &= _points.Upload(_gpu);
		if (ok)
		{
			UploadFrame();
			foreach (Command command in _commands)
			{
				Draw(command);
			}
		}
		_commands.Clear();
		_passes.Clear();
		_rects.Count = (_lines.Count = (_sprites.Count = (_quads.Count = (_verts.Count = (_points.Count = 0)))));
	}

	private unsafe void Draw(Command c)
	{
		switch (c.Kind)
		{
		case Kind.Rect:
			Bind(_rectVs, _rectPs, _rects.View, ((c.Texture != null) ? c.Texture.Srv : ((SceneBackdrop != null) ? SceneBackdrop.Srv : IntPtr.Zero)), c.Blend);
			break;
		case Kind.Line:
			Bind(_lineVs, _linePs, _lines.View, IntPtr.Zero, c.Blend);
			break;
		case Kind.Sprite:
			Bind(_spriteVs, _spritePs, _sprites.View, IntPtr.Zero, c.Blend);
			break;
		case Kind.Quad:
			Bind(_texVs, _texPs, _quads.View, c.Texture.Srv, c.Blend);
			break;
		case Kind.Triangles:
			Bind(_triVs, _triPs, _verts.View, IntPtr.Zero, c.Blend);
			break;
		case Kind.Poly:
			Bind(_polyVs, _polyPs, _points.View, IntPtr.Zero, c.Blend);
			break;
		case Kind.Area:
			Bind(_areaVs, _areaPs, _points.View, IntPtr.Zero, c.Blend);
			break;
		}
		if (c.Pass >= 0)
		{
			PassData passData = _passes[c.Pass];
			_gpu.Upload(_passBuffer, &passData, sizeof(PassData));
		}
		if (c.Kind == Kind.Triangles)
		{
			SetLayout(_perVertex);
			SetTopology(4);
			_ctx.Draw(c.Count, c.First);
		}
		else
		{
			SetLayout(_perInstance);
			SetTopology(5);
			int instanceCount = ((c.Kind == Kind.Poly || c.Kind == Kind.Area) ? (c.Count - 1) : c.Count);
			_ctx.DrawInstanced(4, instanceCount, 0, c.First);
		}
	}

}
