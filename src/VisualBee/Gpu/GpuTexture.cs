using System;

namespace MusicBeePlugin;

internal sealed class GpuTexture : IDisposable
{
	public IntPtr Texture;

	public IntPtr Srv;

	public IntPtr Rtv;

	public int Width;

	public int Height;

	public int Format;

	public void Dispose()
	{
		D3D.Release(ref Rtv);
		D3D.Release(ref Srv);
		D3D.Release(ref Texture);
	}
}
