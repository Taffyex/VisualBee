using System;
using System.Collections.Generic;

namespace MusicBeePlugin;

internal sealed class TextSheet : IDisposable
{
	private struct Item
	{
		public string Text;

		public Gdi.RECT Rect;

		public uint Format;
	}

	private readonly List<Item> _items = new List<Item>();

	private GpuTexture _texture;

	private int _left;

	private int _top;

	public int Count => _items.Count;

	public void Clear()
	{
		_items.Clear();
		Free();
	}

	public void Add(string text, Gdi.RECT rect, uint format)
	{
		if (!string.IsNullOrEmpty(text) && rect.right > rect.left && rect.bottom > rect.top)
		{
			_items.Add(new Item
			{
				Text = text,
				Rect = rect,
				Format = format
			});
		}
	}

	public static Gdi.SIZE Measure(IntPtr font, string text)
	{
		IntPtr dC = Gdi.GetDC(IntPtr.Zero);
		IntPtr hdc = Gdi.CreateCompatibleDC(dC);
		Gdi.ReleaseDC(IntPtr.Zero, dC);
		IntPtr obj = Gdi.SelectObject(hdc, font);
		Gdi.GetTextExtentPoint32(hdc, text, text.Length, out var size);
		Gdi.SelectObject(hdc, obj);
		Gdi.DeleteDC(hdc);
		return size;
	}

	public bool Build(GpuDevice gpu, IntPtr font)
	{
		if (_items.Count == 0)
		{
			Free();
			return true;
		}
		int num = int.MaxValue;
		int num2 = int.MaxValue;
		int num3 = int.MinValue;
		int num4 = int.MinValue;
		foreach (Item item in _items)
		{
			num = Math.Min(num, item.Rect.left);
			num2 = Math.Min(num2, item.Rect.top);
			num3 = Math.Max(num3, item.Rect.right);
			num4 = Math.Max(num4, item.Rect.bottom);
		}
		int num5 = num3 - num;
		int num6 = num4 - num2;
		if (num5 <= 0 || num6 <= 0)
		{
			Free();
			return true;
		}
		byte[] array = new byte[40];
		BitConverter.GetBytes(40).CopyTo(array, 0);
		BitConverter.GetBytes(num5).CopyTo(array, 4);
		BitConverter.GetBytes(-num6).CopyTo(array, 8);
		BitConverter.GetBytes((short)1).CopyTo(array, 12);
		BitConverter.GetBytes((short)32).CopyTo(array, 14);
		IntPtr dC = Gdi.GetDC(IntPtr.Zero);
		IntPtr intPtr = Gdi.CreateDIBSection(dC, array, 0u, out var bits, IntPtr.Zero, 0u);
		IntPtr intPtr2 = Gdi.CreateCompatibleDC(dC);
		Gdi.ReleaseDC(IntPtr.Zero, dC);
		if (intPtr == IntPtr.Zero || intPtr2 == IntPtr.Zero)
		{
			if (intPtr != IntPtr.Zero)
			{
				Gdi.DeleteObject(intPtr);
			}
			if (intPtr2 != IntPtr.Zero)
			{
				Gdi.DeleteDC(intPtr2);
			}
			return false;
		}
		IntPtr obj = Gdi.SelectObject(intPtr2, intPtr);
		IntPtr obj2 = Gdi.SelectObject(intPtr2, font);
		Gdi.SetTextColor(intPtr2, 16777215);
		Gdi.SetBkMode(intPtr2, 1);
		foreach (Item item2 in _items)
		{
			Gdi.RECT rect = new Gdi.RECT(item2.Rect.left - num, item2.Rect.top - num2, item2.Rect.right - num, item2.Rect.bottom - num2);
			Gdi.DrawText(intPtr2, item2.Text, -1, ref rect, item2.Format);
		}
		Gdi.GdiFlush();
		if (_texture != null && _texture.Width == num5 && _texture.Height == num6)
		{
			gpu.Context.UpdateSubresource(_texture.Texture, 0, IntPtr.Zero, bits, num5 * 4, 0);
		}
		else
		{
			Free();
			_texture = gpu.CreateTexture(num5, num6, 87, renderTarget: false, bits, num5 * 4);
		}
		Gdi.SelectObject(intPtr2, obj2);
		Gdi.SelectObject(intPtr2, obj);
		Gdi.DeleteDC(intPtr2);
		Gdi.DeleteObject(intPtr);
		_left = num;
		_top = num2;
		return _texture != null;
	}

	public void Draw(Canvas canvas, Vec4 colour)
	{
		if (_texture != null)
		{
			canvas.Quad(_texture, TexQuad.Make(_left, _top, _left + _texture.Width, _top + _texture.Height, 0f, 0f, 1f, 1f, colour, mask: true));
		}
	}

	private void Free()
	{
		if (_texture != null)
		{
			_texture.Dispose();
		}
		_texture = null;
	}

	public void Dispose()
	{
		_items.Clear();
		Free();
	}
}
