using System;

namespace MusicBeePlugin;

internal sealed partial class SpectrumAnalyzer
{
	public static double ToHz(double t, double min, double max, int scale)
	{
		switch (scale)
		{
		case 1:
		{
			double num4 = Mel(min);
			double num5 = Mel(max);
			double num6 = num4 + (num5 - num4) * t;
			return 700.0 * (Math.Pow(10.0, num6 / 2595.0) - 1.0);
		}
		case 2:
		{
			double num = Bark(min);
			double num2 = Bark(max);
			double num3 = num + (num2 - num) * t;
			return 1960.0 * (num3 + 0.53) / (26.28 - num3);
		}
		case 3:
			return min + (max - min) * t;
		default:
			return min * Math.Pow(max / min, t);
		}
	}

	public static double ToPosition(double hz, double min, double max, int scale)
	{
		return scale switch
		{
			1 => (Mel(hz) - Mel(min)) / (Mel(max) - Mel(min)), 
			2 => (Bark(hz) - Bark(min)) / (Bark(max) - Bark(min)), 
			3 => (hz - min) / (max - min), 
			_ => Math.Log(hz / min) / Math.Log(max / min), 
		};
	}

	private static double Mel(double f)
	{
		return 2595.0 * Math.Log10(1.0 + f / 700.0);
	}

	private static double Bark(double f)
	{
		return 26.81 * f / (1960.0 + f) - 0.53;
	}
}
