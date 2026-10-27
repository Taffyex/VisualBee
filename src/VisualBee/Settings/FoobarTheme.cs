using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace MusicBeePlugin;

internal static class FoobarTheme
{
	public static readonly Guid TextGuid = new Guid("5dd38be7-ff8a-416f-882d-a48e318785b2");

	public static readonly Guid BackgroundGuid = new Guid("16fc40c1-1cba-4385-933b-e9327f6e351f");

	public static readonly Guid HighlightGuid = new Guid("d2f98042-3e6a-423a-b866-6501fea97593");

	public static string Find()
	{
		List<string> list = new List<string>();
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
		list.Add(Path.Combine(folderPath2, "foobar2000", "profile", "theme.fth"));
		list.Add(Path.Combine(folderPath, "foobar2000", "theme.fth"));
		list.Add(Path.Combine(folderPath, "foobar2000-v2", "theme.fth"));
		foreach (string item in list)
		{
			if (File.Exists(item))
			{
				return item;
			}
		}
		return null;
	}

	public static byte[] Read(string path)
	{
		try
		{
			return File.ReadAllBytes(path);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static bool FindColour(byte[] data, Guid guid, out int argb)
	{
		argb = 0;
		byte[] key = guid.ToByteArray();
		for (int num = data.Length - 20; num >= 0; num--)
		{
			if (Matches(data, num, key))
			{
				if (data[num + 19] != 0)
				{
					return false;
				}
				int num2 = data[num + 16];
				int num3 = data[num + 17];
				int num4 = data[num + 18];
				argb = -16777216 | (num2 << 16) | (num3 << 8) | num4;
				return true;
			}
		}
		return false;
	}

	public static int SystemArgb(Color c)
	{
		return c.ToArgb() | -16777216;
	}

	public static byte[] FindElementConfig(byte[] data, Guid element, int size)
	{
		byte[] key = element.ToByteArray();
		for (int i = 0; i + 24 <= data.Length; i++)
		{
			if (!Matches(data, i, key))
			{
				continue;
			}
			int num = i + 16;
			int num2 = BitConverter.ToInt32(data, num);
			if (num2 >= 0 && num2 <= 256 && num + 4 + num2 + 4 + size <= data.Length)
			{
				num += 4 + num2;
				if (BitConverter.ToInt32(data, num) == size)
				{
					byte[] array = new byte[size];
					Array.Copy(data, num + 4, array, 0, size);
					return array;
				}
			}
		}
		return null;
	}

	private static bool Matches(byte[] data, int at, byte[] key)
	{
		for (int i = 0; i < key.Length; i++)
		{
			if (data[at + i] != key[i])
			{
				return false;
			}
		}
		return true;
	}
}
