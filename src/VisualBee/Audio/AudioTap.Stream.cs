using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace MusicBeePlugin;

internal sealed partial class AudioTap
{
	public bool Begin()
	{
		try
		{
			return BeginTrack();
		}
		catch (Exception ex)
		{
			Problem = "finding the track's audio failed (" + ex.GetType().Name + ": " + ex.Message + ")";
			return false;
		}
	}

	private bool BeginTrack()
	{
		if (_handleField == null)
		{
			return false;
		}
		int num;
		try
		{
			num = CurrentHandle();
		}
		catch (Exception ex)
		{
			Problem = "reading MusicBee's track handle failed (" + ex.GetType().Name + ")";
			return false;
		}
		if (num == 0)
		{
			_handle = 0;
			Problem = "MusicBee has no mixer source for the track (" + Describe() + ")";
			return false;
		}
		num = PickSource(num);
		if (num == _handle && _srcRate > 0)
		{
			return true;
		}
		BassChannelInfo info = default;
		if (!Bass.ChannelGetInfo(num, ref info) || info.freq <= 0 || info.chans <= 0)
		{
			Problem = "BASS_ChannelGetInfo failed on source " + num + ", BASS error " + Bass.ErrorGetCode();
			return false;
		}
		int num2 = ProbeDop(num, info.freq, info.chans);
		if (num2 == 0)
		{
			Problem = "checking " + info.freq + " Hz source " + num + " for DSD, BASS error " + Bass.ErrorGetCode();
			return false;
		}
		_handle = num;
		_srcRate = info.freq;
		_factor = num2;
		Rate = info.freq / num2;
		Channels = info.chans;
		_decoding = false;
		int num3 = Bass.MixerChannelGetMixer(num);
		BassChannelInfo info2 = default;
		if (num3 != 0 && Bass.ChannelGetInfo(num3, ref info2))
		{
			_decoding = (info2.flags & 0x200000) != 0;
		}
		return true;
	}

	public bool Read(int pastFrames, int aheadFrames)
	{
		try
		{
			return ReadAudio(pastFrames, aheadFrames);
		}
		catch (Exception ex)
		{
			Problem = "reading the track's audio failed (" + ex.GetType().Name + ": " + ex.Message + ")";
			return false;
		}
	}

	private bool ReadAudio(int pastFrames, int aheadFrames)
	{
		int handle = _handle;
		int srcRate = _srcRate;
		int factor = _factor;
		int channels = Channels;
		pastFrames *= factor;
		aheadFrames *= factor;
		bool decoding = _decoding;
		if (decoding && handle != _ringHandle && _ringHandle != 0)
		{
			ReadTail();
		}
		double num = 0.0;
		long num2 = -1L;
		int num3 = -1;
		for (int i = 0; i < 3; i++)
		{
			long num4 = Bass.MixerChannelGetPosition(handle, 0);
			if (decoding)
			{
				num = OutputQueueSeconds();
			}
			int num5 = (decoding ? (pastFrames + (int)(num * (double)srcRate)) : aheadFrames);
			if (_interleaved.Length < num5 * channels)
			{
				_interleaved = new float[num5 * channels];
			}
			num3 = Bass.MixerChannelGetData(handle, _interleaved, (num5 * channels * 4) | 0x40000000);
			num2 = Bass.MixerChannelGetPosition(handle, 0);
			if (!decoding || num4 == num2)
			{
				break;
			}
		}
		if (num3 <= 0 || num2 < 0)
		{
			Problem = "no audio from source " + handle + " (" + num3 + " bytes, position " + num2 + ", BASS error " + Bass.ErrorGetCode() + (decoding ? ", decoding mixer" : "") + ")";
			return false;
		}
		Problem = "";
		int num6 = num3 / (4 * channels);
		long num7 = (long)Math.Round(Bass.ChannelBytes2Seconds(handle, num2) * (double)srcRate);
		long num8 = (decoding ? (num7 - num6) : num7);
		long num9 = (decoding ? (num7 - (long)Math.Round(num * (double)srcRate)) : num7);
		Store(handle, srcRate, channels, factor, decoding ? num7 : num8, num8, num6);
		Heard = ((_ringFactor > 1) ? FloorDiv(num9 + _offset, _ringFactor) : (num9 + _offset));
		Queued = num;
		return true;
	}

	private void ReadTail()
	{
		int ringHandle = _ringHandle;
		long num = Bass.MixerChannelGetPosition(ringHandle, 0);
		if (num < 0)
		{
			return;
		}
		int ringChans = _ringChans;
		long num2 = (long)Math.Round(Bass.ChannelBytes2Seconds(ringHandle, num) * (double)_ringRate) + _offset;
		long num3 = num2 - _ringEnd;
		if (num3 <= 0 || num3 > _ringRate)
		{
			return;
		}
		int num4 = (int)num3;
		if (_interleaved.Length < num4 * ringChans)
		{
			_interleaved = new float[num4 * ringChans];
		}
		int num5 = Bass.MixerChannelGetData(ringHandle, _interleaved, (num4 * ringChans * 4) | 0x40000000);
		if (num5 > 0)
		{
			int num6 = num5 / (4 * ringChans);
			if (num6 >= num4)
			{
				long num7 = num2 - _offset;
				Store(ringHandle, _ringRate, ringChans, _ringFactor, num7, num7 - num6, num6);
			}
		}
	}

	public void Stop()
	{
		_ringHandle = 0;
	}

	public string Timing()
	{
		return ((_ringFactor > 1) ? ("DSD" + (int)Math.Round((double)(_ringRate * 16) / 44100.0) + " converted, ") : "") + (_decoding ? ("output queue " + (int)Math.Round(Queued * 1000.0) + " ms") : "BASS output");
	}

	public void CopyMono(long start, int count, float[] dst)
	{
		int ringChans = _ringChans;
		float num = (float)(1.0 / (double)ringChans);
		long viewStart = ViewStart;
		long availableEnd = AvailableEnd;
		for (int i = 0; i < count; i++)
		{
			long num2 = start + i;
			if (num2 < viewStart || num2 >= availableEnd)
			{
				dst[i] = 0f;
				continue;
			}
			int num3 = (int)(num2 & 0x7FFFF) * ringChans;
			float num4 = 0f;
			for (int j = 0; j < ringChans; j++)
			{
				num4 += _ring[num3 + j];
			}
			dst[i] = num4 * num;
		}
	}

	public void CopyFrames(long start, int count, float[] dst)
	{
		int ringChans = _ringChans;
		long num = start + count;
		long num2 = Math.Max(start, ViewStart);
		long num3 = Math.Min(num, AvailableEnd);
		if (num3 <= num2)
		{
			Array.Clear(dst, 0, count * ringChans);
			return;
		}
		Array.Clear(dst, 0, (int)(num2 - start) * ringChans);
		int num6;
		for (long num4 = num2; num4 < num3; num4 += num6)
		{
			int num5 = (int)(num4 & 0x7FFFF);
			num6 = (int)Math.Min(num3 - num4, 524288 - num5);
			Array.Copy(_ring, num5 * ringChans, dst, (int)(num4 - start) * ringChans, num6 * ringChans);
		}
		Array.Clear(dst, (int)(num3 - start) * ringChans, (int)(num - num3) * ringChans);
	}

	private void ToRing(float[] src, int srcFrame, long frame, int count)
	{
		int ringChans = _ringChans;
		while (count > 0)
		{
			int num = (int)(frame & 0x7FFFF);
			int num2 = Math.Min(count, 524288 - num);
			Array.Copy(src, srcFrame * ringChans, _ring, num * ringChans, num2 * ringChans);
			srcFrame += num2;
			frame += num2;
			count -= num2;
		}
	}

	private void Store(int handle, int rate, int chans, int factor, long anchor, long blockStart, int got)
	{
		if (handle != _ringHandle)
		{
			if (_ringHandle != 0 && rate == _ringRate && chans == _ringChans && factor == _ringFactor && _ringEnd > _ringStart)
			{
				_ringHandle = handle;
				_offset = _ringEnd - blockStart;
				_ringAnchor = long.MinValue;
			}
			else
			{
				_ringHandle = handle;
				_ringRate = rate;
				_ringChans = chans;
				_ringFactor = factor;
				if (factor > 1 && (_dsd == null || _dsd.Factor != factor || _dsd.Channels != chans))
				{
					_dsd = new DsdConverter(rate, chans);
				}
				if (_ring.Length != 524288 * chans)
				{
					_ring = new float[524288 * chans];
				}
				_offset = 0L;
				Restart(blockStart);
			}
		}
		long num = blockStart + _offset;
		long num2 = num + got;
		anchor += _offset;
		if (anchor < _ringAnchor || num > _ringEnd)
		{
			Restart(num);
		}
		_ringAnchor = anchor;
		long num3 = Math.Max(num, num2 - 524288);
		if (_ringFactor > 1)
		{
			Convert(num, num3, num2);
		}
		else
		{
			ToRing(_interleaved, (int)(num3 - num), num3, (int)(num2 - num3));
		}
		if (num2 > _ringEnd)
		{
			_ringEnd = num2;
		}
		if (num3 < _ringStart)
		{
			_ringStart = num3;
		}
		if (_ringStart < _ringEnd - 524288)
		{
			_ringStart = _ringEnd - 524288;
		}
	}

	private void Restart(long at)
	{
		_ringStart = at;
		_ringEnd = at;
		_ringAnchor = long.MinValue;
		SegmentStart = ((_ringFactor > 1) ? RestartDsd(at) : at);
		Segment++;
	}

	private long RestartDsd(long at)
	{
		int ringFactor = _ringFactor;
		_dsd.Reset();
		long num = -FloorDiv(-at, ringFactor);
		_convNext = num * ringFactor;
		_outStart = (_outEnd = num - _dsd.Delay);
		return num;
	}

	private void Convert(long start, long first, long end)
	{
		if (first > _convNext)
		{
			RestartDsd(first);
		}
		long num = Math.Max(first, _convNext);
		if (num < end)
		{
			int ringChans = _ringChans;
			int frames = (int)(end - num);
			int num2 = _dsd.MaxOutput(frames) * ringChans;
			if (_dsdOut.Length < num2)
			{
				_dsdOut = new float[num2];
			}
			int num3 = _dsd.Process(_interleaved, (int)(num - start), frames, _dsdOut, 0);
			ToRing(_dsdOut, 0, _outEnd, num3);
			_outEnd += num3;
			_convNext = end;
			if (_outStart < _outEnd - 524288)
			{
				_outStart = _outEnd - 524288;
			}
		}
	}

	private static long FloorDiv(long a, int b)
	{
		long num = a / b;
		if (a % b == 0L || a >= 0)
		{
			return num;
		}
		return num - 1;
	}

}
