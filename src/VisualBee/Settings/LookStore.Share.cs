using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace MusicBeePlugin;

internal sealed partial class LookStore
{
	public const string SharePrefix = "VB2:";

	private const int MaxShareDecompressedBytes = 65536;

	public string FormatPresetText(Preset p)
	{
		StringBuilder sb = new StringBuilder();
		sb.Append("# ").Append(Title).Append(" preset").Append(Environment.NewLine);
		AppendPresetBody(sb, p, "Preset", Environment.NewLine);
		return sb.ToString();
	}

	public string EncodeShareCode(Preset p)
	{
		StringBuilder sb = new StringBuilder();
		AppendPresetBody(sb, p, "Custom Look", "\n");

		byte[] raw = Encoding.UTF8.GetBytes(sb.ToString());
		byte[] compressed;
		using (MemoryStream ms = new MemoryStream())
		{
			using (DeflateStream deflate = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true))
			{
				deflate.Write(raw, 0, raw.Length);
			}
			compressed = ms.ToArray();
		}
		string b64 = Convert.ToBase64String(compressed)
			.Replace('+', '-')
			.Replace('/', '_')
			.TrimEnd('=');
		return SharePrefix + b64;
	}

	private void AppendPresetBody(StringBuilder sb, Preset p, string fallbackName, string newline)
	{
		string name = (p?.Name ?? fallbackName).Replace("\r", " ").Replace("\n", " ");
		sb.Append("name=").Append(name).Append(newline);
		if (!string.IsNullOrEmpty(p?.Description))
		{
			sb.Append("description=").Append(p.Description.Replace("\r", " ").Replace("\n", " ")).Append(newline);
		}
		if (p == null) return;
		List<string> keys = new List<string>(p.Values.Keys);
		keys.Sort(StringComparer.OrdinalIgnoreCase);
		foreach (string key in keys)
		{
			Setting s = Schema.Find(key);
			if (s != null && s.Personal) continue;
			sb.Append(key).Append('=').Append(p.Values[key]).Append(newline);
		}
	}

	public string EncodeCurrentShareCode()
	{
		string name = !string.IsNullOrWhiteSpace(PresetName) ? PresetName : "Custom Look";
		return EncodeShareCode(Capture(name, ""));
	}

	public static Preset TryParseShareText(string text, Schema schema = null)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		Preset fromCode = TryDecodeShareCode(text, schema);
		if (fromCode != null)
		{
			return fromCode;
		}
		string[] lines = text.Split(new string[3] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
		return ParsePresetLines(lines, "Shared Look", schema, requireKnownKey: true);
	}

	private static Preset TryDecodeShareCode(string text, Schema schema)
	{
		int idx = text.IndexOf(SharePrefix, StringComparison.OrdinalIgnoreCase);
		if (idx < 0)
		{
			return null;
		}
		int start = idx + SharePrefix.Length;
		StringBuilder token = new StringBuilder();
		for (int i = start; i < text.Length; i++)
		{
			char ch = text[i];
			bool isB64 = (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9')
				|| ch == '-' || ch == '_' || ch == '+' || ch == '/' || ch == '=';
			if (isB64)
			{
				token.Append(ch);
			}
			else if (token.Length > 0)
			{
				break;
			}
		}
		if (token.Length < 4)
		{
			return null;
		}
		string b64 = token.ToString().Replace('-', '+').Replace('_', '/').TrimEnd('=');
		int mod = b64.Length % 4;
		if (mod == 2) b64 += "==";
		else if (mod == 3) b64 += "=";
		else if (mod == 1) return null;

		try
		{
			byte[] compressed = Convert.FromBase64String(b64);
			using MemoryStream input = new MemoryStream(compressed);
			using DeflateStream deflate = new DeflateStream(input, CompressionMode.Decompress);
			using MemoryStream output = new MemoryStream();
			byte[] buffer = new byte[4096];
			int read;
			int total = 0;
			while ((read = deflate.Read(buffer, 0, buffer.Length)) > 0)
			{
				total += read;
				if (total > MaxShareDecompressedBytes)
				{
					return null;
				}
				output.Write(buffer, 0, read);
			}
			string decoded = Encoding.UTF8.GetString(output.ToArray());
			string[] lines = decoded.Split(new string[3] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
			return ParsePresetLines(lines, "Shared Look", schema, requireKnownKey: schema != null);
		}
		catch
		{
			return null;
		}
	}

	internal static Preset ParsePresetLines(string[] lines, string fallbackName, Schema schema, bool requireKnownKey)
	{
		if (lines == null || lines.Length == 0)
		{
			return null;
		}
		Dictionary<string, string> pairs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (string rawLine in lines)
		{
			string line = rawLine?.Trim() ?? "";
			if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith(";", StringComparison.Ordinal))
			{
				continue;
			}
			int eq = line.IndexOf('=');
			if (eq > 0)
			{
				pairs[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
			}
		}
		if (pairs.Count == 0)
		{
			return null;
		}
		string name = (pairs.TryGetValue("name", out string n) && n.Length > 0) ? n : fallbackName;
		pairs.TryGetValue("description", out string desc);
		Preset preset = new Preset(name, desc);
		int knownCount = 0;
		foreach (KeyValuePair<string, string> kv in pairs)
		{
			if (string.Equals(kv.Key, "name", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(kv.Key, "description", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			if (schema != null)
			{
				Setting s = schema.Find(kv.Key);
				if (s != null)
				{
					if (s.Personal) continue;
					if (s.Parse(kv.Value) != null) knownCount++;
				}
			}
			preset.Values[kv.Key] = kv.Value;
		}
		if (preset.Values.Count == 0 || (requireKnownKey && knownCount == 0))
		{
			return null;
		}
		return preset;
	}
}
