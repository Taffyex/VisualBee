using System;
using System.Runtime.InteropServices;

namespace MusicBeePlugin;

[ComImport]
[Guid("7b7166ec-21c7-44ae-b21a-c9ae321ae369")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IDXGIFactory
{
	[PreserveSig]
	int SetPrivateData();

	[PreserveSig]
	int SetPrivateDataInterface();

	[PreserveSig]
	int GetPrivateData();

	[PreserveSig]
	int GetParent();

	[PreserveSig]
	int EnumAdapters();

	[PreserveSig]
	int MakeWindowAssociation(IntPtr window, int flags);
}
