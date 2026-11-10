using System;
using System.Diagnostics;
using System.Threading;

namespace MusicBeePlugin;

internal sealed partial class WasapiLoopback
{
	public bool HasActiveAudio(double timeoutSeconds = 0.6)
	{
		long last = Interlocked.Read(ref _lastSoundTime);
		if (last == 0) return false;
		double elapsed = (double)(Stopwatch.GetTimestamp() - last) / Stopwatch.Frequency;
		return elapsed < timeoutSeconds;
	}

	public bool ReadWindow(int count, float[] dstL, float[] dstR, bool stereo, int channelMode, out int sampleRate, out bool fresh)
	{
		return ReadWindow(count, 0, dstL, dstR, stereo, channelMode, ref _lastReadHead, out sampleRate, out fresh, true);
	}

	public bool ReadWindow(int count, int delayMs, float[] dstL, float[] dstR, bool stereo, int channelMode, ref long lastReadHead, out int sampleRate, out bool fresh)
	{
		return ReadWindow(count, delayMs, dstL, dstR, stereo, channelMode, ref lastReadHead, out sampleRate, out fresh, true);
	}

	public bool ReadWindow(int count, int delayMs, float[] dstL, float[] dstR, bool stereo, int channelMode, ref long lastReadHead, out int sampleRate, out bool fresh, bool agc)
	{
		sampleRate = _sampleRate;
		if (count <= 0 || dstL == null || (stereo && dstR == null))
		{
			fresh = false;
			return false;
		}
		int rate = (sampleRate > 0) ? sampleRate : 48000;
		int delaySamples = Math.Max(0, Math.Min(RingSize - count - 16, (int)((long)delayMs * (long)rate / 1000)));

		float gain = 1.0f;
		if (agc)
		{
			float peak = _envPeak;
			if (peak > 0.005f && peak < 0.90f)
			{
				gain = Math.Min(3.5f, 0.92f / peak);
			}
		}

		lock (_sync)
		{
			long head = _head;
			long nowTick = Stopwatch.GetTimestamp();
			long writeTick = _lastWriteTick;
			long interpFrames = 0;
			if (writeTick > 0 && nowTick > writeTick)
			{
				double elapsedSec = (double)(nowTick - writeTick) / Stopwatch.Frequency;
				if (elapsedSec > 0.0 && elapsedSec < 0.025)
				{
					interpFrames = (long)(elapsedSec * rate);
				}
			}
			long currentHead = head + interpFrames;

			fresh = currentHead != lastReadHead;
			lastReadHead = currentHead;
			long delayedHead = currentHead - delaySamples;
			long start = delayedHead - count;

			if (stereo)
			{
				CopyRingRange(_ringL, start, count, dstL, gain);
				CopyRingRange(_ringR, start, count, dstR, gain);
			}
			else
			{
				switch (channelMode)
				{
					case 1: CopyRingRange(_ringL, start, count, dstL, gain); break;
					case 2: CopyRingRange(_ringR, start, count, dstR, gain); break;
					case 4:
						for (int k = 0; k < count; k++)
						{
							int idx = (int)((start + k) & RingMask);
							dstL[k] = (_ringL[idx] - _ringR[idx]) * (0.5f * gain);
						}
						break;
					default:
						for (int k = 0; k < count; k++)
						{
							int idx = (int)((start + k) & RingMask);
							dstL[k] = (_ringL[idx] + _ringR[idx]) * (0.5f * gain);
						}
						break;
				}
			}
		}
		return true;
	}

	private static void CopyRingRange(float[] ring, long start, int count, float[] dst, float gain = 1.0f)
	{
		int ringStart = (int)(start & RingMask);
		int firstChunk = Math.Min(count, RingSize - ringStart);
		if (Math.Abs(gain - 1.0f) < 0.001f)
		{
			Array.Copy(ring, ringStart, dst, 0, firstChunk);
			int remaining = count - firstChunk;
			if (remaining > 0)
			{
				Array.Copy(ring, 0, dst, firstChunk, remaining);
			}
		}
		else
		{
			for (int i = 0; i < firstChunk; i++) dst[i] = ring[ringStart + i] * gain;
			int remaining = count - firstChunk;
			for (int i = 0; i < remaining; i++) dst[firstChunk + i] = ring[i] * gain;
		}
	}
}
