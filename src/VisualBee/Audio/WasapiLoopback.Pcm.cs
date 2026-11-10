using System;
using System.Diagnostics;
using System.Threading;

namespace MusicBeePlugin;

internal sealed partial class WasapiLoopback
{
	private void FadeOutIdleFrames(int count)
	{
		lock (_sync)
		{
			long head = _head;
			for (int i = 0; i < count; i++)
			{
				int idx = (int)((head + i) & RingMask);
				_ringL[idx] = 0f;
				_ringR[idx] = 0f;
			}
			_head = head + count;
		}
	}

	private unsafe void ProcessFrames(IntPtr pData, int frames, int chans, int bits, bool isFloat, bool silent)
	{
		if (frames <= 0) return;
		float maxAmp = 0f;
		lock (_sync)
		{
			long head = _head;
			if (silent || pData == IntPtr.Zero || (bits != 16 && bits != 24 && bits != 32))
			{
				for (int i = 0; i < frames; i++)
				{
					int idx = (int)((head + i) & RingMask);
					_ringL[idx] = 0f;
					_ringR[idx] = 0f;
				}
			}
			else if (bits == 32 && isFloat)
			{
				float* p = (float*)pData.ToPointer();
				for (int i = 0; i < frames; i++)
				{
					int baseIdx = i * chans;
					float l = p[baseIdx];
					float r = (chans > 1) ? p[baseIdx + 1] : l;
					if (float.IsNaN(l) || float.IsInfinity(l)) l = 0f;
					if (float.IsNaN(r) || float.IsInfinity(r)) r = 0f;
					int idx = (int)((head + i) & RingMask);
					_ringL[idx] = l;
					_ringR[idx] = r;
					float a = Math.Max(Math.Abs(l), Math.Abs(r));
					if (a > maxAmp) maxAmp = a;
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
					int idx = (int)((head + i) & RingMask);
					_ringL[idx] = l;
					_ringR[idx] = r;
					float a = Math.Max(Math.Abs(l), Math.Abs(r));
					if (a > maxAmp) maxAmp = a;
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
					int idx = (int)((head + i) & RingMask);
					_ringL[idx] = l;
					_ringR[idx] = r;
					float a = Math.Max(Math.Abs(l), Math.Abs(r));
					if (a > maxAmp) maxAmp = a;
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
				}
			}
			_head = head + frames;
		}
		if (maxAmp > 0.0001f)
		{
			Interlocked.Exchange(ref _lastSoundTime, Stopwatch.GetTimestamp());
		}
	}
}
