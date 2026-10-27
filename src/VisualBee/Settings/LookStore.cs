using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace MusicBeePlugin;

internal sealed partial class LookStore
{
	public const int FileVersion = 2;

	public readonly string Title;

	public readonly string Key;

	public readonly Schema Schema;

	public readonly Look Defaults;

	public readonly List<Preset> BuiltIn;

	public readonly List<Theme> Themes;

	private readonly Action<Look, Theme> _applyTheme;

	private volatile Look _current;

	private string _file;

	private string _presetFolder;

	private Timer _saveTimer;

	private readonly object _fileLock = new object();

	public string PresetName = "";

	public bool Modified;

	public Func<Look, int[]> SwatchOf = (Look l) => new int[2]
	{
		l.Colours[0],
		l.Colours[1]
	};

	public Look Current => _current;

	public string PresetFolder => _presetFolder;

	public event EventHandler Changed;

	public LookStore(string title, string key, Schema schema, Look defaults, List<Preset> builtIn, List<Theme> themes, Action<Look, Theme> applyTheme)
	{
		Title = title;
		Key = key;
		Schema = schema;
		Defaults = defaults;
		BuiltIn = builtIn;
		Themes = themes;
		_applyTheme = applyTheme;
		schema.DefaultsFrom(defaults);
		foreach (Preset item in builtIn)
		{
			item.BuiltIn = true;
		}
		_current = defaults.Clone();
	}

	public void Set(Setting s, object value)
	{
		Look look = _current.Clone();
		s.Set(look, value);
		Publish(look, !s.Personal);
	}

	public void Reset(Setting s)
	{
		Set(s, s.Default);
	}

	public void Reset(Func<Setting, bool> which)
	{
		Look look = _current.Clone();
		bool flag = false;
		foreach (Setting item in Schema.All)
		{
			if (which(item))
			{
				item.Set(look, item.Default);
				flag = true;
			}
		}
		if (flag)
		{
			Publish(look, modified: true);
		}
	}

	public void Apply(Preset p)
	{
		Look look = Defaults.Clone();
		foreach (Setting item in Schema.All)
		{
			string value;
			if (item.Personal)
			{
				item.Set(look, item.Get(_current));
			}
			else if (p.Values.TryGetValue(item.Key, out value))
			{
				object obj = item.Parse(value);
				if (obj != null)
				{
					item.Set(look, obj);
				}
			}
		}
		PresetName = p.Name;
		Publish(look, modified: false);
	}

	public void Apply(Theme t)
	{
		Look look = _current.Clone();
		_applyTheme(look, t);
		Publish(look, modified: true);
	}

	public Look Themed(Look look, Theme t)
	{
		Look look2 = look.Clone();
		_applyTheme(look2, t);
		return look2;
	}

	private void Publish(Look l, bool modified)
	{
		_current = l;
		if (modified)
		{
			Modified = PresetName.Length > 0;
		}
		else
		{
			Modified = false;
		}
		ScheduleSave();
		Changed?.Invoke(this, EventArgs.Empty);
	}

	public Preset Capture(string name, string description)
	{
		Preset preset = new Preset(name, description);
		foreach (Setting item in Schema.All)
		{
			if (!item.Personal && !item.IsDefault(_current))
			{
				preset.Values[item.Key] = item.Format(item.Get(_current));
			}
		}
		return preset;
	}

	public Look Preview(Preset p)
	{
		Look look = Defaults.Clone();
		foreach (KeyValuePair<string, string> value in p.Values)
		{
			Setting setting = Schema.Find(value.Key);
			object obj = setting?.Parse(value.Value);
			if (obj != null)
			{
				setting.Set(look, obj);
			}
		}
		return look;
	}

	public static int Int(Dictionary<string, string> values, string key, int fallback)
	{
		if (!values.TryGetValue(key, out var value) || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return fallback;
		}
		return result;
	}

	public static int Colour(Dictionary<string, string> values, string key, int fallback)
	{
		if (!values.TryGetValue(key, out var value) || !ColourMath.TryParseHex(value, out var argb))
		{
			return fallback;
		}
		return argb | -16777216;
	}

	public static string SafeName(string name)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (char c in name)
		{
			stringBuilder.Append((Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0) ? '_' : c);
		}
		string text = stringBuilder.ToString().Trim();
		if (text.Length != 0)
		{
			return text;
		}
		return "Preset";
	}
}
