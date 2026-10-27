using System;
using System.Runtime.InteropServices;

namespace MusicBeePlugin;

internal static class D3D
{
	public const int SdkVersion = 7;

	public const int DriverHardware = 1;

	public const int DriverWarp = 5;

	public const int FeatureLevel11_0 = 45056;

	public const int FeatureLevel11_1 = 45312;

	public const int CreateBgraSupport = 32;

	public const int FormatUnknown = 0;

	public const int FormatRgba16Float = 10;

	public const int FormatRgba8 = 28;

	public const int FormatR8 = 61;

	public const int FormatBgra8 = 87;

	public const int FormatR32Uint = 42;

	public const int UsageDefault = 0;

	public const int UsageImmutable = 1;

	public const int UsageDynamic = 2;

	public const int UsageStaging = 3;

	public const int BindVertexBuffer = 1;

	public const int BindConstantBuffer = 4;

	public const int BindShaderResource = 8;

	public const int BindRenderTarget = 32;

	public const int CpuWrite = 65536;

	public const int CpuRead = 131072;

	public const int MiscBufferStructured = 64;

	public const int MapRead = 1;

	public const int MapWriteDiscard = 4;

	public const int TopologyTriangleList = 4;

	public const int TopologyTriangleStrip = 5;

	public const int BlendZero = 1;

	public const int BlendOne = 2;

	public const int BlendInvSrcAlpha = 6;

	public const int BlendInvDestAlpha = 8;

	public const int BlendFactor = 14;

	public const int BlendInvFactor = 15;

	public const int BlendOpAdd = 1;

	public const int BlendOpMax = 5;

	public const int FilterPoint = 0;

	public const int FilterLinear = 21;

	public const int AddressClamp = 3;

	public const int SrvDimensionBuffer = 1;

	public const int PerVertexData = 0;

	public const int PerInstanceData = 1;

	public const int UsageRenderTargetOutput = 32;

	public const int SwapEffectDiscard = 0;

	public const int SwapEffectFlipDiscard = 4;

	public const int SwapChainFlagFrameLatencyWaitableObject = 64;

	public const int SwapChainFlagAllowTearing = 2048;

	public const int WindowAssociationNoChanges = 3;

	public const int StatusOccluded = 142213121;

	public const int ErrorDeviceRemoved = -2005270523;

	public const int ErrorDeviceReset = -2005270521;

	public static readonly Guid Texture2DGuid = new Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

	public static readonly Guid FactoryGuid = new Guid("7b7166ec-21c7-44ae-b21a-c9ae321ae369");

	[DllImport("d3d11.dll")]
	public static extern int D3D11CreateDevice(IntPtr adapter, int driverType, IntPtr software, int flags, int[] featureLevels, int levels, int sdkVersion, out IntPtr device, out int level, out IntPtr context);

	[DllImport("d3d11.dll")]
	public static extern int D3D11CreateDeviceAndSwapChain(IntPtr adapter, int driverType, IntPtr software, int flags, int[] featureLevels, int levels, int sdkVersion, ref SwapChainDesc desc, out IntPtr swapChain, out IntPtr device, out int level, out IntPtr context);

	[DllImport("d3dcompiler_47.dll", CharSet = CharSet.Ansi)]
	public static extern int D3DCompile([MarshalAs(UnmanagedType.LPArray)] byte[] source, IntPtr size, string name, IntPtr defines, IntPtr include, string entry, string target, int flags1, int flags2, out IntPtr code, out IntPtr errors);

	public static T Wrap<T>(IntPtr p) where T : class
	{
		return (T)Marshal.GetObjectForIUnknown(p);
	}

	public static void Release(ref IntPtr p)
	{
		if (!(p == IntPtr.Zero))
		{
			Marshal.Release(p);
			p = IntPtr.Zero;
		}
	}

	public static void Release<T>(ref T rcw) where T : class
	{
		if (rcw != null)
		{
			Marshal.FinalReleaseComObject(rcw);
			rcw = null;
		}
	}

	public static byte[] BlobBytes(IntPtr blob)
	{
		if (blob == IntPtr.Zero)
		{
			return null;
		}
		ID3DBlob iD3DBlob = Wrap<ID3DBlob>(blob);
		try
		{
			int num = iD3DBlob.GetBufferSize().ToInt32();
			byte[] array = new byte[num];
			Marshal.Copy(iD3DBlob.GetBufferPointer(), array, 0, num);
			return array;
		}
		finally
		{
			Marshal.FinalReleaseComObject(iD3DBlob);
			Marshal.Release(blob);
		}
	}
}
