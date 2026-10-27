using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MusicBeePlugin;

internal sealed class GpuDevice : IDisposable
{
	private static readonly int[] Levels = new int[2] { 45312, 45056 };

	public ID3D11Device Device;

	public ID3D11DeviceContext Context;

	private IDXGISwapChain _swap;

	private GpuTexture _offscreen;

	private IntPtr _backBuffer;

	private IntPtr _backView;

	public int Width;

	public int Height;

	public bool Warp;

	public bool FlipModel;

	public IntPtr BackBufferView
	{
		get
		{
			if (_offscreen == null)
			{
				return _backView;
			}
			return _offscreen.Rtv;
		}
	}

	public IntPtr BackBuffer
	{
		get
		{
			if (_offscreen == null)
			{
				return _backBuffer;
			}
			return _offscreen.Texture;
		}
	}

	public static GpuDevice ForWindow(IntPtr hwnd, int width, int height, out string error)
	{
		error = null;
		int num = 0;
		for (int i = 0; i < 4; i++)
		{
			bool flag = i >= 2;
			bool flag2 = i % 2 == 0;
			SwapChainDesc desc = new SwapChainDesc
			{
				Width = Math.Max(1, width),
				Height = Math.Max(1, height),
				Format = 87,
				SampleCount = 1,
				BufferUsage = 32,
				BufferCount = ((!flag2) ? 1 : 2),
				OutputWindow = hwnd,
				Windowed = 1,
				SwapEffect = (flag2 ? 4 : 0)
			};
			int num2 = D3D.D3D11CreateDeviceAndSwapChain(IntPtr.Zero, (!flag) ? 1 : 5, IntPtr.Zero, 32, Levels, Levels.Length, 7, ref desc, out var swapChain, out var device, out var _, out var context);
			if (num2 < 0)
			{
				num = num2;
				continue;
			}
			GpuDevice gpuDevice = new GpuDevice
			{
				Warp = flag,
				FlipModel = flag2
			};
			gpuDevice.Device = Take<ID3D11Device>(device);
			gpuDevice.Context = Take<ID3D11DeviceContext>(context);
			gpuDevice._swap = Take<IDXGISwapChain>(swapChain);
			gpuDevice.Width = desc.Width;
			gpuDevice.Height = desc.Height;
			gpuDevice.Associate(hwnd);
			if (gpuDevice.GetBackBuffer())
			{
				return gpuDevice;
			}
			gpuDevice.Dispose();
		}
		error = "Direct3D 11 is not available (0x" + num.ToString("X8") + ")";
		return null;
	}

	public static GpuDevice Headless(int width, int height, bool warp, out string error)
	{
		error = null;
		int num = D3D.D3D11CreateDevice(IntPtr.Zero, (!warp) ? 1 : 5, IntPtr.Zero, 32, Levels, Levels.Length, 7, out var device, out var _, out var context);
		if (num < 0)
		{
			error = "D3D11CreateDevice failed (0x" + num.ToString("X8") + ")";
			return null;
		}
		GpuDevice gpuDevice = new GpuDevice();
		gpuDevice.Warp = warp;
		gpuDevice.Device = Take<ID3D11Device>(device);
		gpuDevice.Context = Take<ID3D11DeviceContext>(context);
		gpuDevice.Resize(width, height);
		return gpuDevice;
	}

	private static T Take<T>(IntPtr p) where T : class
	{
		T result = D3D.Wrap<T>(p);
		Marshal.Release(p);
		return result;
	}

	private void Associate(IntPtr hwnd)
	{
		Guid riid = D3D.FactoryGuid;
		if (_swap.GetParent(ref riid, out var parent) < 0 || parent == IntPtr.Zero)
		{
			return;
		}
		IDXGIFactory rcw = Take<IDXGIFactory>(parent);
		try
		{
			rcw.MakeWindowAssociation(hwnd, 3);
		}
		finally
		{
			D3D.Release(ref rcw);
		}
	}

	private bool GetBackBuffer()
	{
		Guid riid = D3D.Texture2DGuid;
		if (_swap.GetBuffer(0, ref riid, out _backBuffer) < 0)
		{
			return false;
		}
		return Device.CreateRenderTargetView(_backBuffer, IntPtr.Zero, out _backView) >= 0;
	}

	public bool Resize(int width, int height)
	{
		width = Math.Max(1, width);
		height = Math.Max(1, height);
		if (_swap == null)
		{
			if (_offscreen != null && _offscreen.Width == width && _offscreen.Height == height)
			{
				return true;
			}
			if (_offscreen != null)
			{
				_offscreen.Dispose();
			}
			_offscreen = CreateTexture(width, height, 87, renderTarget: true, IntPtr.Zero, 0);
			Width = width;
			Height = height;
			return _offscreen != null;
		}
		if (width == Width && height == Height && _backView != IntPtr.Zero)
		{
			return true;
		}
		Context.OMSetRenderTargets(0, null, IntPtr.Zero);
		D3D.Release(ref _backView);
		D3D.Release(ref _backBuffer);
		if (_swap.ResizeBuffers(0, width, height, 0, 0) < 0)
		{
			return false;
		}
		Width = width;
		Height = height;
		return GetBackBuffer();
	}

	public int Present(int syncInterval = 1)
	{
		if (_swap == null)
		{
			return 0;
		}
		return _swap.Present(syncInterval, 0);
	}

	public static bool Lost(int hr)
	{
		return hr < 0 && hr != D3D.StatusOccluded;
	}

	public GpuTexture CreateTexture(int width, int height, int format, bool renderTarget, IntPtr pixels, int pitch)
	{
		Texture2DDesc desc = new Texture2DDesc
		{
			Width = width,
			Height = height,
			MipLevels = 1,
			ArraySize = 1,
			Format = format,
			SampleCount = 1,
			Usage = 0,
			BindFlags = (8 | (renderTarget ? 32 : 0))
		};
		GpuTexture gpuTexture = new GpuTexture
		{
			Width = width,
			Height = height,
			Format = format
		};
		int num;
		if (pixels != IntPtr.Zero)
		{
			SubresourceData structure = new SubresourceData
			{
				SysMem = pixels,
				SysMemPitch = pitch
			};
			IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SubresourceData)));
			try
			{
				Marshal.StructureToPtr(structure, intPtr, fDeleteOld: false);
				num = Device.CreateTexture2D(ref desc, intPtr, out gpuTexture.Texture);
			}
			finally
			{
				Marshal.FreeHGlobal(intPtr);
			}
		}
		else
		{
			num = Device.CreateTexture2D(ref desc, IntPtr.Zero, out gpuTexture.Texture);
		}
		if (num < 0 || Device.CreateShaderResourceView(gpuTexture.Texture, IntPtr.Zero, out gpuTexture.Srv) < 0 || (renderTarget && Device.CreateRenderTargetView(gpuTexture.Texture, IntPtr.Zero, out gpuTexture.Rtv) < 0))
		{
			gpuTexture.Dispose();
			return null;
		}
		return gpuTexture;
	}

	public bool CreateStructured(int stride, int count, out IntPtr buffer, out IntPtr view)
	{
		view = IntPtr.Zero;
		BufferDesc desc = new BufferDesc
		{
			ByteWidth = stride * count,
			Usage = 2,
			BindFlags = 8,
			CpuAccessFlags = 65536,
			MiscFlags = 64,
			StructureByteStride = stride
		};
		if (Device.CreateBuffer(ref desc, IntPtr.Zero, out buffer) < 0)
		{
			return false;
		}
		BufferViewDesc structure = new BufferViewDesc
		{
			Format = 0,
			ViewDimension = 1,
			NumElements = count
		};
		IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(BufferViewDesc)));
		try
		{
			Marshal.StructureToPtr(structure, intPtr, fDeleteOld: false);
			if (Device.CreateShaderResourceView(buffer, intPtr, out view) >= 0)
			{
				return true;
			}
		}
		finally
		{
			Marshal.FreeHGlobal(intPtr);
		}
		D3D.Release(ref buffer);
		return false;
	}

	public IntPtr CreateConstants(int bytes)
	{
		BufferDesc desc = new BufferDesc
		{
			ByteWidth = ((bytes + 15) & -16),
			Usage = 2,
			BindFlags = 4,
			CpuAccessFlags = 65536
		};
		if (Device.CreateBuffer(ref desc, IntPtr.Zero, out var buffer) < 0)
		{
			return IntPtr.Zero;
		}
		return buffer;
	}

	public unsafe bool Upload(IntPtr resource, void* data, int bytes)
	{
		if (Context.Map(resource, 0, 4, 0, out var mapped) < 0)
		{
			return false;
		}
		Buffer.MemoryCopy(data, (void*)mapped.Data, bytes, bytes);
		Context.Unmap(resource, 0);
		return true;
	}

	public unsafe Bitmap ReadBack(IntPtr texture, int width, int height)
	{
		Texture2DDesc desc = new Texture2DDesc
		{
			Width = width,
			Height = height,
			MipLevels = 1,
			ArraySize = 1,
			Format = 87,
			SampleCount = 1,
			Usage = 3,
			CpuAccessFlags = 131072
		};
		if (Device.CreateTexture2D(ref desc, IntPtr.Zero, out var texture2) < 0)
		{
			return null;
		}
		try
		{
			Context.CopyResource(texture2, texture);
			if (Context.Map(texture2, 0, 1, 0, out var mapped) < 0)
			{
				return null;
			}
			Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppRgb);
			BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
			for (int i = 0; i < height; i++)
			{
				Buffer.MemoryCopy((byte*)(void*)mapped.Data + i * mapped.RowPitch, (byte*)(void*)bitmapData.Scan0 + i * bitmapData.Stride, width * 4, width * 4);
			}
			bitmap.UnlockBits(bitmapData);
			Context.Unmap(texture2, 0);
			return bitmap;
		}
		finally
		{
			D3D.Release(ref texture2);
		}
	}

	public void Dispose()
	{
		if (_offscreen != null)
		{
			_offscreen.Dispose();
			_offscreen = null;
		}
		D3D.Release(ref _backView);
		D3D.Release(ref _backBuffer);
		D3D.Release(ref _swap);
		D3D.Release(ref Context);
		D3D.Release(ref Device);
	}
}
