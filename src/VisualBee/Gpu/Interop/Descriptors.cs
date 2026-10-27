using System;
using System.Runtime.InteropServices;

namespace MusicBeePlugin;

internal struct BlendDesc
{
	public int AlphaToCoverageEnable;

	public int IndependentBlendEnable;

	public TargetBlendDesc Target0;

	public TargetBlendDesc Target1;

	public TargetBlendDesc Target2;

	public TargetBlendDesc Target3;

	public TargetBlendDesc Target4;

	public TargetBlendDesc Target5;

	public TargetBlendDesc Target6;

	public TargetBlendDesc Target7;
}

internal struct BufferDesc
{
	public int ByteWidth;

	public int Usage;

	public int BindFlags;

	public int CpuAccessFlags;

	public int MiscFlags;

	public int StructureByteStride;
}

internal struct BufferViewDesc
{
	public int Format;

	public int ViewDimension;

	public int FirstElement;

	public int NumElements;

	public int Pad0;

	public int Pad1;
}

internal struct InputElementDesc
{
	public IntPtr SemanticName;

	public int SemanticIndex;

	public int Format;

	public int InputSlot;

	public int AlignedByteOffset;

	public int InputSlotClass;

	public int InstanceDataStepRate;
}

internal struct MappedSubresource
{
	public IntPtr Data;

	public int RowPitch;

	public int DepthPitch;
}

internal struct RasterizerDesc
{
	public int FillMode;

	public int CullMode;

	public int FrontCounterClockwise;

	public int DepthBias;

	public float DepthBiasClamp;

	public float SlopeScaledDepthBias;

	public int DepthClipEnable;

	public int ScissorEnable;

	public int MultisampleEnable;

	public int AntialiasedLineEnable;
}

internal struct SamplerDesc
{
	public int Filter;

	public int AddressU;

	public int AddressV;

	public int AddressW;

	public float MipLodBias;

	public int MaxAnisotropy;

	public int ComparisonFunc;

	public float Border0;

	public float Border1;

	public float Border2;

	public float Border3;

	public float MinLod;

	public float MaxLod;
}

internal struct SubresourceData
{
	public IntPtr SysMem;

	public int SysMemPitch;

	public int SysMemSlicePitch;
}

internal struct SwapChainDesc
{
	public int Width;

	public int Height;

	public int RefreshNumerator;

	public int RefreshDenominator;

	public int Format;

	public int ScanlineOrdering;

	public int Scaling;

	public int SampleCount;

	public int SampleQuality;

	public int BufferUsage;

	public int BufferCount;

	public IntPtr OutputWindow;

	public int Windowed;

	public int SwapEffect;

	public int Flags;
}

internal struct TargetBlendDesc
{
	public int BlendEnable;

	public int SrcBlend;

	public int DestBlend;

	public int BlendOp;

	public int SrcBlendAlpha;

	public int DestBlendAlpha;

	public int BlendOpAlpha;

	public int WriteMask;
}

internal struct Texture2DDesc
{
	public int Width;

	public int Height;

	public int MipLevels;

	public int ArraySize;

	public int Format;

	public int SampleCount;

	public int SampleQuality;

	public int Usage;

	public int BindFlags;

	public int CpuAccessFlags;

	public int MiscFlags;
}

internal struct Viewport
{
	public float X;

	public float Y;

	public float Width;

	public float Height;

	public float MinDepth;

	public float MaxDepth;
}

internal struct PassData
{
	public Vec4 P0;

	public Vec4 P1;

	public Vec4 P2;

	public Vec4 P3;

	public Vec4 P4;

	public Vec4 P5;

	public Vec4 P6;

	public Vec4 P7;
}
