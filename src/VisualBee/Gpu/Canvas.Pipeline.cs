using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MusicBeePlugin;

internal sealed partial class Canvas
{
	private unsafe string Build()
	{
		string error = null;
		if (!Vs("RectVS", out _rectVs, ref error) || !Ps("RectPS", out _rectPs, ref error) || !Vs("LineVS", out _lineVs, ref error) || !Ps("LinePS", out _linePs, ref error) || !Vs("PolyVS", out _polyVs, ref error) || !Ps("PolyPS", out _polyPs, ref error) || !Vs("AreaVS", out _areaVs, ref error) || !Ps("AreaPS", out _areaPs, ref error) || !Vs("SpriteVS", out _spriteVs, ref error) || !Ps("SpritePS", out _spritePs, ref error) || !Vs("TriVS", out _triVs, ref error) || !Ps("TriPS", out _triPs, ref error) || !Vs("TexVS", out _texVs, ref error) || !Ps("TexPS", out _texPs, ref error) || !Vs("FullVS", out FullVs, ref error) || !Ps("ZeroPS", out ZeroPs, ref error) || !Ps("DownPS", out DownPs, ref error) || !Ps("UpPS", out UpPs, ref error) || !Ps("TrailPS", out TrailPs, ref error) || !Ps("PhosphorPS", out PhosphorPs, ref error) || !Ps("BackgroundPS", out BackgroundPs, ref error) || !Ps("AuroraPS", out AuroraPs, ref error) || !Ps("CompositePS", out CompositePs, ref error))
		{
			return error;
		}
		_over = BlendState(enable: true, 2, 6, 1);
		_add = BlendState(enable: true, 2, 2, 1);
		_max = BlendState(enable: true, 2, 2, 5);
		_behind = BlendState(enable: true, 8, 2, 1);
		_mix = BlendState(enable: true, 14, 15, 1);
		_replace = BlendState(enable: false, 2, 1, 1);
		_decay = BlendState(enable: true, 1, 14, 1);
		RasterizerDesc desc = new RasterizerDesc
		{
			FillMode = 3,
			CullMode = 1,
			DepthClipEnable = 1
		};
		_gpu.Device.CreateRasterizerState(ref desc, out _raster);
		_linear = Sampler(21);
		_point = Sampler(0);
		_frameBuffer = _gpu.CreateConstants(32);
		_passBuffer = _gpu.CreateConstants(sizeof(PassData));
		string text = Layouts();
		if (text != null)
		{
			return text;
		}
		if (_over == IntPtr.Zero || _add == IntPtr.Zero || _max == IntPtr.Zero || _behind == IntPtr.Zero || _mix == IntPtr.Zero || _replace == IntPtr.Zero || _decay == IntPtr.Zero || _raster == IntPtr.Zero || _linear == IntPtr.Zero || _point == IntPtr.Zero || _frameBuffer == IntPtr.Zero || _passBuffer == IntPtr.Zero)
		{
			return "creating pipeline state failed";
		}
		return null;
	}

	private unsafe string Layouts()
	{
		int[] array = new int[65536];
		for (int i = 0; i < 65536; i++)
		{
			array[i] = i;
		}
		BufferDesc desc = new BufferDesc
		{
			ByteWidth = 262144,
			Usage = 1,
			BindFlags = 1
		};
		fixed (int* ptr = array)
		{
			SubresourceData subresourceData = new SubresourceData
			{
				SysMem = (IntPtr)ptr
			};
			if (_gpu.Device.CreateBuffer(ref desc, (IntPtr)(&subresourceData), out _indices) < 0)
			{
				return "creating the index buffer failed";
			}
		}
		IntPtr intPtr = Marshal.StringToHGlobalAnsi("INDEX");
		try
		{
			byte[] array2 = Shaders.Get("RectVS", "vs_5_0", out var error);
			byte[] array3 = Shaders.Get("TriVS", "vs_5_0", out error);
			InputElementDesc[] elements = new InputElementDesc[1]
			{
				new InputElementDesc
				{
					SemanticName = intPtr,
					Format = 42,
					InputSlotClass = 1,
					InstanceDataStepRate = 1
				}
			};
			InputElementDesc[] elements2 = new InputElementDesc[1]
			{
				new InputElementDesc
				{
					SemanticName = intPtr,
					Format = 42,
					InputSlotClass = 0
				}
			};
			if (array2 == null || _gpu.Device.CreateInputLayout(elements, 1, array2, (IntPtr)array2.Length, out _perInstance) < 0 || array3 == null || _gpu.Device.CreateInputLayout(elements2, 1, array3, (IntPtr)array3.Length, out _perVertex) < 0)
			{
				return "creating the input layouts failed";
			}
		}
		finally
		{
			Marshal.FreeHGlobal(intPtr);
		}
		return null;
	}

	private void SetLayout(IntPtr layout)
	{
		if (!(layout == _boundLayout))
		{
			_ctx.IASetInputLayout(layout);
			_boundLayout = layout;
		}
	}

	private bool Vs(string entry, out IntPtr shader, ref string error)
	{
		shader = IntPtr.Zero;
		byte[] array = Shaders.Get(entry, "vs_5_0", out error);
		if (array == null)
		{
			return false;
		}
		int num = _gpu.Device.CreateVertexShader(array, (IntPtr)array.Length, IntPtr.Zero, out shader);
		if (num < 0)
		{
			error = entry + ": CreateVertexShader failed (0x" + num.ToString("X8") + ")";
		}
		return num >= 0;
	}

	private bool Ps(string entry, out IntPtr shader, ref string error)
	{
		shader = IntPtr.Zero;
		byte[] array = Shaders.Get(entry, "ps_5_0", out error);
		if (array == null)
		{
			return false;
		}
		int num = _gpu.Device.CreatePixelShader(array, (IntPtr)array.Length, IntPtr.Zero, out shader);
		if (num < 0)
		{
			error = entry + ": CreatePixelShader failed (0x" + num.ToString("X8") + ")";
		}
		return num >= 0;
	}

	private IntPtr BlendState(bool enable, int src, int dest, int op)
	{
		TargetBlendDesc target = new TargetBlendDesc
		{
			BlendEnable = (enable ? 1 : 0),
			SrcBlend = src,
			DestBlend = dest,
			BlendOp = op,
			SrcBlendAlpha = src,
			DestBlendAlpha = dest,
			BlendOpAlpha = op,
			WriteMask = 15
		};
		BlendDesc desc = new BlendDesc
		{
			Target0 = target
		};
		if (_gpu.Device.CreateBlendState(ref desc, out var state) < 0)
		{
			return IntPtr.Zero;
		}
		return state;
	}

	private IntPtr Sampler(int filter)
	{
		SamplerDesc desc = new SamplerDesc
		{
			Filter = filter,
			AddressU = 3,
			AddressV = 3,
			AddressW = 3,
			MaxAnisotropy = 1,
			ComparisonFunc = 1,
			MaxLod = float.MaxValue
		};
		if (_gpu.Device.CreateSamplerState(ref desc, out var state) < 0)
		{
			return IntPtr.Zero;
		}
		return state;
	}

	public void Begin(IntPtr rtv, int width, int height, float time)
	{
		Flush();
		if (!_stateSet)
		{
			_ctx.RSSetState(_raster);
			_two[0] = _linear;
			_two[1] = _point;
			_ctx.PSSetSamplers(0, 2, _two);
			_constants[0] = _frameBuffer;
			_constants[1] = _passBuffer;
			_ctx.VSSetConstantBuffers(0, 2, _constants);
			_ctx.PSSetConstantBuffers(0, 2, _constants);
			_one[0] = _indices;
			_ctx.IASetVertexBuffers(0, 1, _one, VertexStrides, VertexOffsets);
			_stateSet = true;
		}
		if (rtv != _target)
		{
			UnbindViews();
		}
		_one[0] = rtv;
		_ctx.OMSetRenderTargets(1, _one, IntPtr.Zero);
		_target = rtv;
		if (width != _targetW || height != _targetH)
		{
			_viewport[0] = new Viewport
			{
				Width = width,
				Height = height,
				MaxDepth = 1f
			};
			_ctx.RSSetViewports(1, _viewport);
			_targetW = width;
			_targetH = height;
			_frameDirty = true;
		}
		if (time != _time)
		{
			_time = time;
			_frameDirty = true;
		}
	}

	private unsafe void UploadFrame()
	{
		if (_frameDirty)
		{
			float* ptr = stackalloc float[8];
			*ptr = _targetW;
			ptr[1] = _targetH;
			ptr[2] = 1f / (float)Math.Max(1, _targetW);
			ptr[3] = 1f / (float)Math.Max(1, _targetH);
			ptr[4] = _time;
			_gpu.Upload(_frameBuffer, ptr, 32);
			_frameDirty = false;
		}
	}

	public void ReleaseTarget()
	{
		Flush();
		_ctx.OMSetRenderTargets(0, null, IntPtr.Zero);
		_target = IntPtr.Zero;
	}

	public void Begin(GpuTexture target, float time)
	{
		Begin(target.Rtv, target.Width, target.Height, time);
	}

	public void Clear(Vec4 colour)
	{
		Flush();
		_clear[0] = colour.X * colour.W;
		_clear[1] = colour.Y * colour.W;
		_clear[2] = colour.Z * colour.W;
		_clear[3] = colour.W;
		_ctx.ClearRenderTargetView(_target, _clear);
	}

	public static void Clear(GpuDevice gpu, GpuTexture target, Vec4 colour)
	{
		gpu.Context.ClearRenderTargetView(target.Rtv, new float[4]
		{
			colour.X * colour.W,
			colour.Y * colour.W,
			colour.Z * colour.W,
			colour.W
		});
	}

	private void Bind(IntPtr vs, IntPtr ps, IntPtr view, IntPtr texture, BlendMode blend)
	{
		if (vs != _boundVs)
		{
			_ctx.VSSetShader(vs, IntPtr.Zero, 0);
			_boundVs = vs;
		}
		if (ps != _boundPs)
		{
			_ctx.PSSetShader(ps, IntPtr.Zero, 0);
			_boundPs = ps;
		}
		if (view != _boundView || texture != _boundTexture)
		{
			_two[0] = view;
			_two[1] = texture;
			_ctx.VSSetShaderResources(0, 1, _two);
			_ctx.PSSetShaderResources(0, 2, _two);
			_boundView = view;
			_boundTexture = texture;
		}
		SetBlend(blend, 1f);
	}

	private void SetBlend(BlendMode blend, float factor)
	{
		IntPtr intPtr = blend switch
		{
			BlendMode.Mix => _mix, 
			BlendMode.Behind => _behind, 
			BlendMode.Replace => _replace, 
			BlendMode.Max => _max, 
			BlendMode.Add => _add, 
			_ => _over, 
		};
		if (!(intPtr == _boundBlend) || factor != _boundFactor)
		{
			_factor[0] = (_factor[1] = (_factor[2] = (_factor[3] = factor)));
			_ctx.OMSetBlendState(intPtr, _factor, -1);
			_boundBlend = intPtr;
			_boundFactor = factor;
		}
	}

	public unsafe void Pass(IntPtr ps, PassData pass, BlendMode blend, GpuTexture t0, GpuTexture t1 = null, GpuTexture t2 = null, GpuTexture t3 = null, float mix = 1f)
	{
		Flush();
		if (FullVs != _boundVs)
		{
			_ctx.VSSetShader(FullVs, IntPtr.Zero, 0);
			_boundVs = FullVs;
		}
		if (ps != _boundPs)
		{
			_ctx.PSSetShader(ps, IntPtr.Zero, 0);
			_boundPs = ps;
		}
		_four[0] = t0?.Srv ?? IntPtr.Zero;
		_four[1] = t1?.Srv ?? IntPtr.Zero;
		_four[2] = t2?.Srv ?? IntPtr.Zero;
		_four[3] = t3?.Srv ?? IntPtr.Zero;
		_ctx.PSSetShaderResources(0, 4, _four);
		SetLayout(IntPtr.Zero);
		if (ps == CompositePs || ps == BackgroundPs || ps == AuroraPs)
		{
			UploadFrame();
		}
		_gpu.Upload(_passBuffer, &pass, sizeof(PassData));
		SetBlend(blend, mix);
		SetTopology(4);
		_ctx.Draw(3, 0);
		_ctx.PSSetShaderResources(0, 4, _noViews);
		_boundView = (_boundTexture = IntPtr.Zero);
	}

	public void Fade(float factor)
	{
		Flush();
		_ctx.VSSetShader(FullVs, IntPtr.Zero, 0);
		_ctx.PSSetShader(ZeroPs, IntPtr.Zero, 0);
		SetLayout(IntPtr.Zero);
		_factor[0] = (_factor[1] = (_factor[2] = (_factor[3] = factor)));
		_ctx.OMSetBlendState(_decay, _factor, -1);
		SetTopology(4);
		_ctx.Draw(3, 0);
		_boundVs = (_boundPs = (_boundBlend = IntPtr.Zero));
		_boundFactor = -1f;
	}

	private static readonly int[] VertexStrides = new int[1] { 4 };
	private static readonly int[] VertexOffsets = new int[1];

	private void SetTopology(int topology)
	{
		if (topology != _topology)
		{
			_ctx.IASetPrimitiveTopology(topology);
			_topology = topology;
		}
	}

	private void UnbindViews()
	{
		_ctx.PSSetShaderResources(0, 4, _noViews);
		_ctx.VSSetShaderResources(0, 1, _noViews);
		_boundView = (_boundTexture = IntPtr.Zero);
		_boundVs = (_boundPs = (_boundBlend = IntPtr.Zero));
		_boundFactor = -1f;
	}

}
