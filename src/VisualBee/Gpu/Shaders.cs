using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MusicBeePlugin;

internal static partial class Shaders
{
	private const int CompileFlags = 34816;

	private static readonly Dictionary<string, byte[]> Compiled = new Dictionary<string, byte[]>();

	internal static string CacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MusicBee", "mb_Visuals", "shaders");

	public static byte[] Get(string entry, string profile, out string error)
	{
		error = null;
		string text = entry + "_" + profile;
		lock (Compiled)
		{
			if (Compiled.TryGetValue(text, out var value))
			{
				return value;
			}
			string text2 = null;
			if (CacheFolder != null)
			{
				text2 = Path.Combine(CacheFolder, text + "_" + Hash(Source + "|" + text) + ".cso");
				value = ReadCache(text2);
			}
			if (value == null)
			{
				value = Compile(entry, profile, out error);
				if (value == null)
				{
					return null;
				}
				if (text2 != null)
				{
					WriteCache(text2, text, value);
				}
			}
			Compiled[text] = value;
			return value;
		}
	}

	private static byte[] Compile(string entry, string profile, out string error)
	{
		error = null;
		byte[] bytes = Encoding.ASCII.GetBytes(Source);
		int num = D3D.D3DCompile(bytes, (IntPtr)bytes.Length, "visuals.hlsl", IntPtr.Zero, IntPtr.Zero, entry, profile, CompileFlags, 0, out var code, out var errors);
		byte[] array = D3D.BlobBytes(errors);
		if (num < 0 || code == IntPtr.Zero)
		{
			D3D.BlobBytes(code);
			error = entry + ": " + ((array != null) ? Encoding.ASCII.GetString(array).TrimEnd('\0', '\r', '\n') : ("HRESULT 0x" + num.ToString("X8")));
			return null;
		}
		return D3D.BlobBytes(code);
	}

	private static byte[] ReadCache(string file)
	{
		try
		{
			return File.Exists(file) ? File.ReadAllBytes(file) : null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static void WriteCache(string file, string key, byte[] code)
	{
		try
		{
			Directory.CreateDirectory(CacheFolder);
			string[] files = Directory.GetFiles(CacheFolder, key + "_*.cso");
			foreach (string text in files)
			{
				if (!string.Equals(text, file, StringComparison.OrdinalIgnoreCase))
				{
					File.Delete(text);
				}
			}
			string text2 = file + "." + Process.GetCurrentProcess().Id + ".tmp";
			File.WriteAllBytes(text2, code);
			if (!File.Exists(file))
			{
				File.Move(text2, file);
			}
			if (File.Exists(text2))
			{
				File.Delete(text2);
			}
		}
		catch (Exception)
		{
		}
	}

	private static string Hash(string text)
	{
		using SHA256 sHA = SHA256.Create();
		byte[] array = sHA.ComputeHash(Encoding.UTF8.GetBytes(text));
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < 8; i++)
		{
			stringBuilder.Append(array[i].ToString("x2"));
		}
		return stringBuilder.ToString();
	}
}
