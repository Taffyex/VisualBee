using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace MusicBeePlugin;

internal sealed class ArtImage
{
	private const int MaxSide = 512;

	private static int _lastId;

	public readonly int Id;

	public readonly int Width;

	public readonly int Height;

	public readonly byte[] Pixels;

	public readonly Theme Theme;

	private ArtImage(int width, int height, byte[] pixels, Theme theme)
	{
		Id = Interlocked.Increment(ref _lastId);
		Width = width;
		Height = height;
		Pixels = pixels;
		Theme = theme;
	}

	public static ArtImage Decode(byte[] data)
	{
		if (data == null || data.Length == 0)
		{
			return null;
		}
		try
		{
			using MemoryStream stream = new MemoryStream(data);
			using Image image = Image.FromStream(stream, useEmbeddedColorManagement: false, validateImageData: false);
			double num = Math.Min(1.0, 512.0 / (double)Math.Max(image.Width, image.Height));
			int num2 = Math.Max(1, (int)Math.Round((double)image.Width * num));
			int num3 = Math.Max(1, (int)Math.Round((double)image.Height * num));
			using Bitmap bitmap = new Bitmap(num2, num3, PixelFormat.Format32bppArgb);
			using (Graphics graphics = Graphics.FromImage(bitmap))
			{
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
				graphics.DrawImage(image, new Rectangle(0, 0, num2, num3));
			}
			BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, num2, num3), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
			byte[] array = new byte[num2 * num3 * 4];
			for (int i = 0; i < num3; i++)
			{
				Marshal.Copy(bitmapData.Scan0 + i * bitmapData.Stride, array, i * num2 * 4, num2 * 4);
			}
			bitmap.UnlockBits(bitmapData);
			int[] array2 = new int[num2 * num3];
			Buffer.BlockCopy(array, 0, array2, 0, array.Length);
			List<int> palette = ColourMath.Palette(array2, array2.Length);
			return new ArtImage(num2, num3, array, ColourMath.ThemeFrom(palette));
		}
		catch (Exception)
		{
			return null;
		}
	}
}
