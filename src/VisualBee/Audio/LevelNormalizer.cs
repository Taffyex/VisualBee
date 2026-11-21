using System;

namespace MusicBeePlugin;

internal sealed class LevelNormalizer
{
	private double _envDb = double.NaN;
	private double _offsetDb;
	private int _lastTrackSerial = -1;
	private double[] _scratch = new double[0];

	public double Update(double[][] raw, int channels, int bands, float dt, SpectrumLook look, bool usingLoopback, float trackGainDb, float albumGainDb, int trackSerial)
	{
		if (look == null || raw == null || bands <= 0 || channels <= 0)
		{
			return 0.0;
		}
		int mode = look.AutoLevel;
		if (mode == SpectrumLook.AutoLevelOff)
		{
			_offsetDb = 0.0;
			return 0.0;
		}
		if (mode == SpectrumLook.AutoLevelDefault)
		{
			if (!usingLoopback || !look.LoopbackAgc)
			{
				_offsetDb = 0.0;
				return 0.0;
			}
			mode = SpectrumLook.AutoLevelAdaptive;
		}
		if (trackSerial != _lastTrackSerial)
		{
			_lastTrackSerial = trackSerial;
			if (!double.IsNaN(_envDb))
			{
				_envDb = double.NaN;
			}
		}
		double maxBoost = Math.Max(0.0, look.AutoLevelMaxBoost);
		if (mode == SpectrumLook.AutoLevelReplayGainTrack && !float.IsNaN(trackGainDb))
		{
			return SmoothOffset(ClampGain(trackGainDb, maxBoost), dt, 0.25);
		}
		if (mode == SpectrumLook.AutoLevelReplayGainAlbum && !float.IsNaN(albumGainDb))
		{
			return SmoothOffset(ClampGain(albumGainDb, maxBoost), dt, 0.25);
		}

		double frameDb = PercentileDb(raw, channels, bands);
		double silenceGate = look.FloorDb + 6.0;
		if (double.IsNaN(frameDb) || frameDb < silenceGate)
		{
			return _offsetDb;
		}

		if (double.IsNaN(_envDb))
		{
			_envDb = frameDb;
		}
		else
		{
			double tau = (frameDb > _envDb) ? 0.35 : Math.Max(1.0, look.AutoLevelSpeed);
			double alpha = (dt > 0f) ? (1.0 - Math.Exp(-(double)dt / tau)) : 1.0;
			_envDb += (frameDb - _envDb) * alpha;
		}

		double targetDb = look.CeilingDb - Math.Max(6.0, look.CeilingDb - look.FloorDb) * 0.28;
		double desired = ClampGain(targetDb - _envDb, maxBoost);
		return SmoothOffset(desired, dt, 0.20);
	}

	private double SmoothOffset(double desired, float dt, double tau)
	{
		double alpha = (dt > 0f) ? (1.0 - Math.Exp(-(double)dt / tau)) : 1.0;
		_offsetDb += (desired - _offsetDb) * alpha;
		return _offsetDb;
	}

	private static double ClampGain(double gainDb, double maxBoost)
	{
		return Math.Max(-12.0, Math.Min(maxBoost, gainDb));
	}

	private double PercentileDb(double[][] raw, int channels, int bands)
	{
		int total = channels * bands;
		if (_scratch.Length < total)
		{
			_scratch = new double[total];
		}
		int n = 0;
		for (int c = 0; c < channels; c++)
		{
			double[] arr = raw[c];
			if (arr == null) continue;
			for (int b = 0; b < bands && b < arr.Length; b++)
			{
				double v = arr[b];
				if (v > -143.49)
				{
					_scratch[n++] = v;
				}
			}
		}
		if (n == 0)
		{
			return double.NaN;
		}
		Array.Sort(_scratch, 0, n);
		int idx = Math.Max(0, Math.Min(n - 1, (int)Math.Round((n - 1) * 0.85)));
		return _scratch[idx];
	}
}
