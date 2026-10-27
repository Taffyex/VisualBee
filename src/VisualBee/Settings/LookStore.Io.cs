using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace MusicBeePlugin;

internal sealed partial class LookStore
{
	public bool Load(string file, string presetFolder)
	{
		_file = file;
		_presetFolder = presetFolder;
		Dictionary<string, string> dictionary = ReadPairs(file);
		if (dictionary == null || !dictionary.TryGetValue("version", out var value) || value != 2.ToString(CultureInfo.InvariantCulture))
		{
			return false;
		}
		Look look = Defaults.Clone();
		foreach (Setting item in Schema.All)
		{
			if (dictionary.TryGetValue(item.Key, out var value2))
			{
				object obj = item.Parse(value2);
				if (obj != null)
				{
					item.Set(look, obj);
				}
			}
		}
		PresetName = (dictionary.TryGetValue("preset", out var value3) ? value3 : "");
		Modified = dictionary.TryGetValue("modified", out var value4) && value4 == "1";
		_current = look;
		return true;
	}

	public static Dictionary<string, string> ReadPairs(string file)
	{
		if (string.IsNullOrEmpty(file) || !File.Exists(file))
		{
			return null;
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			string[] array = File.ReadAllLines(file);
			foreach (string text in array)
			{
				if (!text.StartsWith("#", StringComparison.Ordinal))
				{
					int num = text.IndexOf('=');
					if (num > 0)
					{
						dictionary[text.Substring(0, num).Trim()] = text.Substring(num + 1).Trim();
					}
				}
			}
			return dictionary;
		}
		catch (IOException)
		{
			return null;
		}
		catch (UnauthorizedAccessException)
		{
			return null;
		}
	}

	private void ScheduleSave()
	{
		if (_file == null)
		{
			return;
		}
		if (_saveTimer == null)
		{
			_saveTimer = new Timer((object s) =>
			{
				Save();
			}, null, -1, -1);
		}
		_saveTimer.Change(400, -1);
	}

	public void Save()
	{
		if (_file == null)
		{
			return;
		}
		Look current = _current;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("# " + Title + " settings");
		stringBuilder.AppendLine("version=" + 2.ToString(CultureInfo.InvariantCulture));
		stringBuilder.AppendLine("preset=" + PresetName);
		stringBuilder.AppendLine("modified=" + (Modified ? "1" : "0"));
		foreach (Setting item in Schema.All)
		{
			if (!item.IsDefault(current))
			{
				stringBuilder.AppendLine(item.Key + "=" + item.Format(item.Get(current)));
			}
		}
		WriteText(_file, stringBuilder.ToString());
	}

	private void WriteText(string file, string text)
	{
		lock (_fileLock)
		{
			try
			{
				string directoryName = Path.GetDirectoryName(file);
				if (!string.IsNullOrEmpty(directoryName))
				{
					Directory.CreateDirectory(directoryName);
				}
				File.WriteAllText(file, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}
	}

	public List<Preset> UserPresets()
	{
		List<Preset> list = new List<Preset>();
		if (string.IsNullOrEmpty(_presetFolder) || !Directory.Exists(_presetFolder))
		{
			return list;
		}
		string[] files;
		try
		{
			files = Directory.GetFiles(_presetFolder, "*.ini");
		}
		catch (Exception)
		{
			return list;
		}
		Array.Sort(files, StringComparer.CurrentCultureIgnoreCase);
		string[] array = files;
		for (int i = 0; i < array.Length; i++)
		{
			Preset preset = ReadPreset(array[i]);
			if (preset != null)
			{
				list.Add(preset);
			}
		}
		return list;
	}

	public static Preset ReadPreset(string file)
	{
		Dictionary<string, string> dictionary = ReadPairs(file);
		if (dictionary == null)
		{
			return null;
		}
		if (!dictionary.TryGetValue("name", out var value) || value.Length == 0)
		{
			value = Path.GetFileNameWithoutExtension(file);
		}
		dictionary.TryGetValue("description", out var value2);
		Preset preset = new Preset(value, value2);
		foreach (KeyValuePair<string, string> item in dictionary)
		{
			if (item.Key != "name" && item.Key != "description")
			{
				preset.Values[item.Key] = item.Value;
			}
		}
		preset.File = file;
		return preset;
	}

	public string SavePreset(Preset p)
	{
		if (string.IsNullOrEmpty(_presetFolder))
		{
			return null;
		}
		string text = p.File ?? Path.Combine(_presetFolder, SafeName(p.Name) + ".ini");
		WritePreset(p, text);
		p.File = text;
		return text;
	}

	public void WritePreset(Preset p, string file)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("# " + Title + " preset");
		stringBuilder.AppendLine("name=" + p.Name);
		if (p.Description.Length > 0)
		{
			stringBuilder.AppendLine("description=" + p.Description.Replace("\r", " ").Replace("\n", " "));
		}
		foreach (KeyValuePair<string, string> value in p.Values)
		{
			stringBuilder.AppendLine(value.Key + "=" + value.Value);
		}
		WriteText(file, stringBuilder.ToString());
	}

}
