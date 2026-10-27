using System;

namespace MusicBeePlugin;

internal sealed class BeatTracker
{
	private double _average;

	private double _deviation;

	private double _previous;

	private double _sinceBeat = 10.0;

	public float Pulse;

	public void Reset()
	{
		_average = 0.0;
		_deviation = 0.0;
		_previous = 0.0;
		Pulse = 0f;
	}

	public void Feed(double energy, float dt, double sensitivity = 1.0)
	{
		if (!(dt <= 0f))
		{
			_sinceBeat += dt;
			double num = energy - _previous;
			_previous = energy;
			bool num2 = _average > 1E-09 && _sinceBeat > 0.2 && num > 0.0 && energy > _average + (1.6 - 0.6 * sensitivity) * Math.Max(_deviation, _average * 0.15);
			double num3 = 1.0 - Math.Exp((double)(0f - dt) / 0.6);
			_average += (energy - _average) * num3;
			_deviation += (Math.Abs(energy - _average) - _deviation) * num3;
			Pulse *= (float)Math.Exp((double)(0f - dt) / 0.14);
			if (num2)
			{
				Pulse = 1f;
				_sinceBeat = 0.0;
			}
		}
	}
}
