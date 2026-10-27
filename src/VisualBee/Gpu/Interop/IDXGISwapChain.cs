using System;
using System.Runtime.InteropServices;

namespace MusicBeePlugin;

[ComImport]
[Guid("310d36a0-d2e7-4c0a-aa04-6a9d23b8886a")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IDXGISwapChain
{
	[PreserveSig]
	int SetPrivateData();

	[PreserveSig]
	int SetPrivateDataInterface();

	[PreserveSig]
	int GetPrivateData();

	[PreserveSig]
	int GetParent(ref Guid riid, out IntPtr parent);

	[PreserveSig]
	int GetDevice();

	[PreserveSig]
	int Present(int syncInterval, int flags);

	[PreserveSig]
	int GetBuffer(int buffer, ref Guid riid, out IntPtr surface);

	[PreserveSig]
	int SetFullscreenState(int fullscreen, IntPtr target);

	[PreserveSig]
	int GetFullscreenState();

	[PreserveSig]
	int GetDesc(out SwapChainDesc desc);

	[PreserveSig]
	int ResizeBuffers(int count, int width, int height, int format, int flags);
}
