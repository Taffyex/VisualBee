using System;

namespace MusicBeePlugin;

internal sealed class VisStream
{
	private const double ReadMargin = 0.05;

	private const double ReachMargin = 0.015;

	private readonly AudioTap _tap;

	private int _segment = int.MinValue;

	private double _lastTime;

	private double _reach = double.PositiveInfinity;

	private bool _reachDecoding;

	private double _latency;

	public double Latency => _latency;

	public VisStream(AudioTap tap)
	{
		_tap = tap;
	}

	public void Restart()
	{
		_segment = int.MinValue;
	}

	public bool GetAbsoluteTime(bool read, double duration, int delayMs, out double time, out bool restarted, double pastSeconds = 0.0)
	{
		time = 0.0;
		restarted = false;
		bool flag = _tap.AvailableEnd <= _tap.SegmentStart;
		if (read | flag)
		{
			if (!_tap.Begin())
			{
				return false;
			}
			if (_tap.Decoding != _reachDecoding)
			{
				_reachDecoding = _tap.Decoding;
				_reach = double.PositiveInfinity;
			}
			int rate = _tap.Rate;
			double num = duration * 0.5;
			int pastFrames = (int)Math.Ceiling((num + pastSeconds + (double)delayMs * 0.001 + _latency + 0.05) * (double)rate);
			int aheadFrames = (int)Math.Ceiling((num + 0.05) * (double)rate);
			if (!_tap.Read(pastFrames, aheadFrames))
			{
				return false;
			}
		}
		int historyRate = _tap.HistoryRate;
		if (historyRate <= 0)
		{
			return false;
		}
		_latency = Math.Max(0.0, duration * 0.5 - (double)delayMs * 0.001 - _reach);
		if (_tap.Segment != _segment)
		{
			_segment = _tap.Segment;
			_lastTime = double.NegativeInfinity;
			restarted = true;
		}
		long num2 = (long)delayMs * (long)historyRate / 1000;
		double num3 = (double)(_tap.Heard - _tap.SegmentStart - num2) / (double)historyRate - _latency;
		if (num3 < _lastTime)
		{
			num3 = _lastTime;
		}
		_lastTime = num3;
		time = num3;
		return true;
	}

	public bool GetChunk(double start, double length, bool downmix, ref float[] data, out int rate, out int nch, out int samples, bool partial = false)
	{
		rate = _tap.HistoryRate;
		nch = (downmix ? 1 : _tap.HistoryChannels);
		samples = 0;
		if (rate <= 0 || nch <= 0)
		{
			return false;
		}
		long num = _tap.SegmentStart + (long)Math.Floor(start * (double)rate + 0.5);
		int num2 = (int)Math.Round((double)rate * length, MidpointRounding.AwayFromZero);
		long num3 = num + num2 - _tap.AvailableEnd;
		if (num3 > 0)
		{
			if (start > 0.0)
			{
				double num4 = (double)(_tap.AvailableEnd - _tap.Heard) / (double)rate;
				_reach = Math.Min(_reach, num4 - 0.015);
			}
			if (!partial || num3 >= num2)
			{
				return false;
			}
			num2 -= (int)num3;
		}
		if (data == null || data.Length < num2 * nch)
		{
			data = new float[num2 * nch];
		}
		if (downmix)
		{
			_tap.CopyMono(num, num2, data);
		}
		else
		{
			_tap.CopyFrames(num, num2, data);
		}
		samples = num2;
		return true;
	}
}
