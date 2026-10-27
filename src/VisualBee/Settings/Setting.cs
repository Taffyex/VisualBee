using System;
using System.Globalization;

namespace MusicBeePlugin;

internal sealed class Setting
{
	public string Key;

	public string Page;

	public string Section;

	public string Label;

	public string Hint;

	public SettingKind Kind;

	public double Min;

	public double Max;

	public double Step;

	public int Decimals;

	public string Unit = "";

	public bool Logarithmic;

	public string[] Choices;

	public string[] Tokens;

	public bool Alpha;

	public bool Personal;

	public object Default;

	public Func<Look, object> Get;

	public Action<Look, object> Set;

	public Func<Look, bool> Enabled;

	public string Keywords = "";

	public int Index;

	public bool IsEnabled(Look look)
	{
		if (Enabled != null)
		{
			return Enabled(look);
		}
		return true;
	}

	public bool IsDefault(Look look)
	{
		return Same(Get(look), Default);
	}

	public bool Same(object a, object b)
	{
		if (Kind == SettingKind.Number)
		{
			return Math.Abs(Convert.ToDouble(a, CultureInfo.InvariantCulture) - Convert.ToDouble(b, CultureInfo.InvariantCulture)) < Math.Pow(10.0, -Decimals) * 0.5;
		}
		return object.Equals(a, b);
	}

	public string Format(object value)
	{
		switch (Kind)
		{
		case SettingKind.Toggle:
			if (!(bool)value)
			{
				return "0";
			}
			return "1";
		case SettingKind.Number:
			return ((double)value).ToString("0.######", CultureInfo.InvariantCulture);
		case SettingKind.Choice:
			return Tokens[Math.Max(0, Math.Min(Tokens.Length - 1, (int)value))];
		default:
			return ColourMath.ToHex((int)value, Alpha);
		}
	}

	public object Parse(string text)
	{
		text = (text ?? "").Trim();
		switch (Kind)
		{
		case SettingKind.Toggle:
			if (text == "1" || text.Equals("true", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			if (text == "0" || text.Equals("false", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			return null;
		case SettingKind.Number:
		{
			if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) || double.IsNaN(result))
			{
				return null;
			}
			return Clamp(result);
		}
		case SettingKind.Choice:
		{
			for (int i = 0; i < Tokens.Length; i++)
			{
				if (string.Equals(Tokens[i], text, StringComparison.OrdinalIgnoreCase))
				{
					return i;
				}
			}
			return null;
		}
		default:
		{
			if (!ColourMath.TryParseHex(text, out var argb))
			{
				return null;
			}
			return Alpha ? argb : (argb | -16777216);
		}
		}
	}

	public double Clamp(double v)
	{
		v = Math.Max(Min, Math.Min(Max, v));
		return Math.Round(v, Decimals);
	}

	public string Display(double v)
	{
		string text = ((Decimals <= 0) ? "0" : ("0." + new string('0', Decimals)));
		string text2 = v.ToString(text, CultureInfo.CurrentCulture);
		if (Unit.Length != 0)
		{
			if (!(Unit == "%") && !(Unit == "°") && !(Unit == "×"))
			{
				return text2 + " " + Unit;
			}
			return text2 + Unit;
		}
		return text2;
	}
}
