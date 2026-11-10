using System;
using System.Diagnostics;
using System.Threading;

namespace MusicBeePlugin;

internal sealed partial class WasapiLoopback
{
	private void AdvanceIdleDecay(int count)
	{
		lock (_sync)
		{
			if (Math.Abs(_lastL) < 0.0001f && Math.Abs(_lastR) < 0.0001f) return;
			long head = _head;
			float l = _lastL;
			float r = _lastR;
			for (int i = 0; i < count; i++)
			{
				l *= 0.94f;
				r *= 0.94f;
				int idx = (int)((head + i) & RingMask);
				_ringL[idx] = l;
				_ringR[idx] = r;
			}
			_lastL = (Math.Abs(l) < 0.0001f) ? 0f : l;
			_lastR = (Math.Abs(r) < 0.0001f) ? 0f : r;
			_head = head + count;
			_lastWriteTick = Stopwatch.GetTimestamp();
		}
	}

	private unsafe void ProcessFrames(IntPtr pData, int frames, int chans, int bits, bool isFloat, bool silent)
	{
		if (frames <= 0) return;
		float maxAmp = 0f;
		long now = Stopwatch.GetTimestamp();
		_lastPacketTick = now;

		lock (_sync)
		{
			long head = _head;
			if (silent || pData == IntPtr.Zero || (bits != 16 && bits != 24 && bits != 32))
			{
				float l = _lastL, r = _lastR;
				for (int i = 0; i < frames; i++)
				{
					l *= 0.95f;
					r *= 0.95f;
					int idx = (int)((head + i) & RingMask);
					_ringL[idx] = l;
					_ringR[idx] = r;
				}
				_lastL = l;
				_lastR = r;
			}
			else if (bits == 32 && isFloat)
			{
				float* p = (float*)pData.ToPointer();
				for (int i = 0; i < frames; i++)
				{
					int baseIdx = i * chans;
					float l, r;
					if (chans == 1) { l = p[baseIdx]; r = l; }
					else if (chans == 2) { l = p[baseIdx]; r = p[baseIdx + 1]; }
					else if (chans == 4)
					{
						l = p[baseIdx] + 0.7071f * p[baseIdx + 2];
						r = p[baseIdx + 1] + 0.7071f * p[baseIdx + 3];
					}
					else
					{
						float c = (p[baseIdx + 2] + p[baseIdx + 3]) * 0.7071f;
						l = p[baseIdx] + c + p[baseIdx + 4] * 0.7071f;
						r = p[baseIdx + 1] + c + p[baseIdx + 5] * 0.7071f;
						if (chans >= 8) { l += p[baseIdx + 6] * 0.5f; r += p[baseIdx + 7] * 0.5f; }
					}
					if (float.IsNaN(l) || float.IsInfinity(l)) l = 0f;
					if (float.IsNaN(r) || float.IsInfinity(r)) r = 0f;
					int idx = (int)((head + i) & RingMask);
					_ringL[idx] = l;
					_ringR[idx] = r;
					float a = Math.Max(Math.Abs(l), Math.Abs(r));
					if (a > maxAmp) maxAmp = a;
					_lastL = l;
					_lastR = r;
				}
			}
			else if (bits == 32 && !isFloat)
			{
				int* p = (int*)pData.ToPointer();
				for (int i = 0; i < frames; i++)
				{
					int baseIdx = i * chans;
					float l = p[baseIdx] * (1f / 2147483648f);
					float r = (chans > 1) ? p[baseIdx + 1] * (1f / 2147483648f) : l;
					if (chans >= 6)
					{
						float c = (p[baseIdx + 2] + p[baseIdx + 3]) * (0.7071f / 2147483648f);
						l += c + p[baseIdx + 4] * (0.7071f / 2147483648f);
						r += c + p[baseIdx + 5] * (0.7071f / 2147483648f);
					}
					int idx = (int)((head + i) & RingMask);
					_ringL[idx] = l;
					_ringR[idx] = r;
					float a = Math.Max(Math.Abs(l), Math.Abs(r));
					if (a > maxAmp) maxAmp = a;
					_lastL = l;
					_lastR = r;
				}
			}
			else if (bits == 16)
			{
				short* p = (short*)pData.ToPointer();
				for (int i = 0; i < frames; i++)
				{
					int baseIdx = i * chans;
					float l = p[baseIdx] * (1f / 32768f);
					float r = (chans > 1) ? p[baseIdx + 1] * (1f / 32768f) : l;
					if (chans >= 6)
					{
						float c = (p[baseIdx + 2] + p[baseIdx + 3]) * (0.7071f / 32768f);
						l += c + p[baseIdx + 4] * (0.7071f / 32768f);
						r += c + p[baseIdx + 5] * (0.7071f / 32768f);
					}
					int idx = (int)((head + i) & RingMask);
					_ringL[idx] = l;
					_ringR[idx] = r;
					float a = Math.Max(Math.Abs(l), Math.Abs(r));
					if (a > maxAmp) maxAmp = a;
					_lastL = l;
					_lastR = r;
				}
			}
			else if (bits == 24)
			{
				byte* p = (byte*)pData.ToPointer();
				int frameBytes = chans * 3;
				for (int i = 0; i < frames; i++)
				{
					byte* bL = p + i * frameBytes;
					int valL = (sbyte)bL[2] << 16 | bL[1] << 8 | bL[0];
					float l = valL * (1f / 8388608f);
					float r = l;
					if (chans > 1)
					{
						byte* bR = bL + 3;
						int valR = (sbyte)bR[2] << 16 | bR[1] << 8 | bR[0];
						r = valR * (1f / 8388608f);
					}
					int idx = (int)((head + i) & RingMask);
					_ringL[idx] = l;
					_ringR[idx] = r;
					float a = Math.Max(Math.Abs(l), Math.Abs(r));
					if (a > maxAmp) maxAmp = a;
					_lastL = l;
					_lastR = r;
				}
			}
			_head = head + frames;
			_lastWriteTick = now;
		}

		if (maxAmp > 0.0001f)
		{
			Interlocked.Exchange(ref _lastSoundTime, now);
			float curEnv = _envPeak;
			if (maxAmp > curEnv)
			{
				_envPeak = maxAmp;
			}
			else
			{
				_envPeak = curEnv * 0.9997f + maxAmp * 0.0003f;
			}
		}
	}
}
