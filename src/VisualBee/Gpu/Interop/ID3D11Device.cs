using System;
using System.Runtime.InteropServices;

namespace MusicBeePlugin;

[ComImport]
[Guid("db6f6ddb-ac77-4e88-8253-819df9bbf140")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ID3D11Device
{
	[PreserveSig]
	int CreateBuffer(ref BufferDesc desc, IntPtr initialData, out IntPtr buffer);

	[PreserveSig]
	int CreateTexture1D();

	[PreserveSig]
	int CreateTexture2D(ref Texture2DDesc desc, IntPtr initialData, out IntPtr texture);

	[PreserveSig]
	int CreateTexture3D();

	[PreserveSig]
	int CreateShaderResourceView(IntPtr resource, IntPtr desc, out IntPtr view);

	[PreserveSig]
	int CreateUnorderedAccessView();

	[PreserveSig]
	int CreateRenderTargetView(IntPtr resource, IntPtr desc, out IntPtr view);

	[PreserveSig]
	int CreateDepthStencilView();

	[PreserveSig]
	int CreateInputLayout([MarshalAs(UnmanagedType.LPArray)] InputElementDesc[] elements, int count, [MarshalAs(UnmanagedType.LPArray)] byte[] code, IntPtr length, out IntPtr layout);

	[PreserveSig]
	int CreateVertexShader([MarshalAs(UnmanagedType.LPArray)] byte[] code, IntPtr length, IntPtr linkage, out IntPtr shader);

	[PreserveSig]
	int CreateGeometryShader();

	[PreserveSig]
	int CreateGeometryShaderWithStreamOutput();

	[PreserveSig]
	int CreatePixelShader([MarshalAs(UnmanagedType.LPArray)] byte[] code, IntPtr length, IntPtr linkage, out IntPtr shader);

	[PreserveSig]
	int CreateHullShader();

	[PreserveSig]
	int CreateDomainShader();

	[PreserveSig]
	int CreateComputeShader();

	[PreserveSig]
	int CreateClassLinkage();

	[PreserveSig]
	int CreateBlendState(ref BlendDesc desc, out IntPtr state);

	[PreserveSig]
	int CreateDepthStencilState();

	[PreserveSig]
	int CreateRasterizerState(ref RasterizerDesc desc, out IntPtr state);

	[PreserveSig]
	int CreateSamplerState(ref SamplerDesc desc, out IntPtr state);

	[PreserveSig]
	int CreateQuery();

	[PreserveSig]
	int CreatePredicate();

	[PreserveSig]
	int CreateCounter();

	[PreserveSig]
	int CreateDeferredContext();

	[PreserveSig]
	int OpenSharedResource();

	[PreserveSig]
	int CheckFormatSupport();

	[PreserveSig]
	int CheckMultisampleQualityLevels();

	[PreserveSig]
	void CheckCounterInfo();

	[PreserveSig]
	int CheckCounter();

	[PreserveSig]
	int CheckFeatureSupport();

	[PreserveSig]
	int GetPrivateData();

	[PreserveSig]
	int SetPrivateData();

	[PreserveSig]
	int SetPrivateDataInterface();

	[PreserveSig]
	int GetFeatureLevel();

	[PreserveSig]
	int GetCreationFlags();

	[PreserveSig]
	int GetDeviceRemovedReason();
}
