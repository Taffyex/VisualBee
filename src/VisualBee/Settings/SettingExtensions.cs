using System;

namespace MusicBeePlugin;

internal static class SettingExtensions
{
	public static Setting When(this Setting s, Func<Look, bool> enabled)
	{
		s.Enabled = enabled;
		return s;
	}

	public static Setting Log(this Setting s)
	{
		s.Logarithmic = true;
		return s;
	}

	public static Setting Mine(this Setting s)
	{
		s.Personal = true;
		return s;
	}

	public static Setting Words(this Setting s, string keywords)
	{
		s.Keywords = keywords;
		return s;
	}
}
