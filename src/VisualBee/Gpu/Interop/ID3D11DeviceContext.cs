using System;
using System.Runtime.InteropServices;

namespace MusicBeePlugin;

[ComImport]
[Guid("c0bfa96c-e089-44fb-8eaf-26f8796190da")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ID3D11DeviceContext
{
	[PreserveSig]
	void GetDevice(out IntPtr device);

	[PreserveSig]
	int GetPrivateData();

	[PreserveSig]
	int SetPrivateData();

	[PreserveSig]
	int SetPrivateDataInterface();

	[PreserveSig]
	void VSSetConstantBuffers(int start, int count, [MarshalAs(UnmanagedType.LPArray)] IntPtr[] buffers);

	[PreserveSig]
	void PSSetShaderResources(int start, int count, [MarshalAs(UnmanagedType.LPArray)] IntPtr[] views);

	[PreserveSig]
	void PSSetShader(IntPtr shader, IntPtr classInstances, int count);

	[PreserveSig]
	void PSSetSamplers(int start, int count, [MarshalAs(UnmanagedType.LPArray)] IntPtr[] samplers);

	[PreserveSig]
	void VSSetShader(IntPtr shader, IntPtr classInstances, int count);

	[PreserveSig]
	void DrawIndexed(int indexCount, int startIndex, int baseVertex);

	[PreserveSig]
	void Draw(int vertexCount, int startVertex);

	[PreserveSig]
	int Map(IntPtr resource, int subresource, int mapType, int flags, out MappedSubresource mapped);

	[PreserveSig]
	void Unmap(IntPtr resource, int subresource);

	[PreserveSig]
	void PSSetConstantBuffers(int start, int count, [MarshalAs(UnmanagedType.LPArray)] IntPtr[] buffers);

	[PreserveSig]
	void IASetInputLayout(IntPtr layout);

	[PreserveSig]
	void IASetVertexBuffers(int start, int count, [MarshalAs(UnmanagedType.LPArray)] IntPtr[] buffers, [MarshalAs(UnmanagedType.LPArray)] int[] strides, [MarshalAs(UnmanagedType.LPArray)] int[] offsets);

	[PreserveSig]
	void IASetIndexBuffer();

	[PreserveSig]
	void DrawIndexedInstanced();

	[PreserveSig]
	void DrawInstanced(int vertexCountPerInstance, int instanceCount, int startVertex, int startInstance);

	[PreserveSig]
	void GSSetConstantBuffers();

	[PreserveSig]
	void GSSetShader(IntPtr shader, IntPtr classInstances, int count);

	[PreserveSig]
	void IASetPrimitiveTopology(int topology);

	[PreserveSig]
	void VSSetShaderResources(int start, int count, [MarshalAs(UnmanagedType.LPArray)] IntPtr[] views);

	[PreserveSig]
	void VSSetSamplers();

	[PreserveSig]
	void Begin();

	[PreserveSig]
	void End();

	[PreserveSig]
	int GetData();

	[PreserveSig]
	void SetPredication();

	[PreserveSig]
	void GSSetShaderResources();

	[PreserveSig]
	void GSSetSamplers();

	[PreserveSig]
	void OMSetRenderTargets(int count, [MarshalAs(UnmanagedType.LPArray)] IntPtr[] views, IntPtr depthStencil);

	[PreserveSig]
	void OMSetRenderTargetsAndUnorderedAccessViews();

	[PreserveSig]
	void OMSetBlendState(IntPtr state, [MarshalAs(UnmanagedType.LPArray)] float[] factor, int sampleMask);

	[PreserveSig]
	void OMSetDepthStencilState(IntPtr state, int stencilRef);

	[PreserveSig]
	void SOSetTargets();

	[PreserveSig]
	void DrawAuto();

	[PreserveSig]
	void DrawIndexedInstancedIndirect();

	[PreserveSig]
	void DrawInstancedIndirect();

	[PreserveSig]
	void Dispatch();

	[PreserveSig]
	void DispatchIndirect();

	[PreserveSig]
	void RSSetState(IntPtr state);

	[PreserveSig]
	void RSSetViewports(int count, [MarshalAs(UnmanagedType.LPArray)] Viewport[] viewports);

	[PreserveSig]
	void RSSetScissorRects(int count, [MarshalAs(UnmanagedType.LPArray)] Gdi.RECT[] rects);

	[PreserveSig]
	void CopySubresourceRegion();

	[PreserveSig]
	void CopyResource(IntPtr destination, IntPtr source);

	[PreserveSig]
	void UpdateSubresource(IntPtr destination, int subresource, IntPtr box, IntPtr data, int rowPitch, int depthPitch);

	[PreserveSig]
	void CopyStructureCount();

	[PreserveSig]
	void ClearRenderTargetView(IntPtr view, [MarshalAs(UnmanagedType.LPArray)] float[] colour);
}
