using System;
using System.Collections.Generic;

namespace MusicBeePlugin;

internal sealed class Preset
{
	public string Name;

	public string Description = "";

	public bool BuiltIn;

	public string File;

	public readonly Dictionary<string, string> Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	public Preset(string name, string description, params string[] pairs)
	{
		Name = name;
		Description = description ?? "";
		for (int i = 0; i + 1 < pairs.Length; i += 2)
		{
			Values[pairs[i]] = pairs[i + 1];
		}
	}
}
