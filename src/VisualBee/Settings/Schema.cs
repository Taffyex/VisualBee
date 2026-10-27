using System;
using System.Collections.Generic;
using System.Globalization;

namespace MusicBeePlugin;

internal sealed class Schema
{
	public readonly List<Setting> All = new List<Setting>();

	private readonly Dictionary<string, Setting> _byKey = new Dictionary<string, Setting>(StringComparer.OrdinalIgnoreCase);

	private string _page = "";

	private string _section = "";

	public Schema Page(string page)
	{
		_page = page;
		_section = "";
		return this;
	}

	public Schema Section(string section)
	{
		_section = section;
		return this;
	}

	public Setting Find(string key)
	{
		if (!_byKey.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public IEnumerable<string> Pages()
	{
		List<string> list = new List<string>();
		foreach (Setting item in All)
		{
			if (!list.Contains(item.Page))
			{
				list.Add(item.Page);
			}
		}
		return list;
	}

	public void DefaultsFrom(Look look)
	{
		foreach (Setting item in All)
		{
			item.Default = item.Get(look);
		}
	}

	private Setting Add(Setting s)
	{
		s.Page = _page;
		s.Section = _section;
		s.Index = All.Count;
		All.Add(s);
		_byKey[s.Key] = s;
		return s;
	}

	public Setting Toggle<T>(string key, string label, string hint, Func<T, bool> get, Action<T, bool> set) where T : Look
	{
		return Add(new Setting
		{
			Key = key,
			Label = label,
			Hint = hint,
			Kind = SettingKind.Toggle,
			Get = (Look l) => get((T)l),
			Set = (Look l, object v) =>
			{
				set((T)l, (bool)v);
			}
		});
	}

	public Setting Number<T>(string key, string label, string hint, double min, double max, double step, int decimals, string unit, Func<T, double> get, Action<T, double> set) where T : Look
	{
		return Add(new Setting
		{
			Key = key,
			Label = label,
			Hint = hint,
			Kind = SettingKind.Number,
			Min = min,
			Max = max,
			Step = step,
			Decimals = decimals,
			Unit = (unit ?? ""),
			Get = (Look l) => get((T)l),
			Set = (Look l, object v) =>
			{
				set((T)l, Convert.ToDouble(v, CultureInfo.InvariantCulture));
			}
		});
	}

	public Setting Integer<T>(string key, string label, string hint, int min, int max, int step, string unit, Func<T, int> get, Action<T, int> set) where T : Look
	{
		return Number(key, label, hint, min, max, step, 0, unit, (T l) => get(l), (T l, double v) =>
		{
			set(l, (int)Math.Round(v));
		});
	}

	public Setting Choice<T>(string key, string label, string hint, string[] names, string[] tokens, Func<T, int> get, Action<T, int> set) where T : Look
	{
		return Add(new Setting
		{
			Key = key,
			Label = label,
			Hint = hint,
			Kind = SettingKind.Choice,
			Choices = names,
			Tokens = tokens,
			Get = (Look l) => get((T)l),
			Set = (Look l, object v) =>
			{
				set((T)l, (int)v);
			}
		});
	}

	public Setting Colour(string key, string label, string hint, int slot, bool alpha = false)
	{
		return Add(new Setting
		{
			Key = key,
			Label = label,
			Hint = hint,
			Kind = SettingKind.Colour,
			Alpha = alpha,
			Get = (Look l) => l.Colours[slot],
			Set = (Look l, object v) =>
			{
				l.Colours[slot] = (int)v;
			}
		});
	}
}
