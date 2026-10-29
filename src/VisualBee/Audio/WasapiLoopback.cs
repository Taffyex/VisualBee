using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace MusicBeePlugin;

internal sealed class WasapiLoopback : IDisposable
{
	[ComImport]
	[Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
	private class MMDeviceEnumeratorComObject { }

	[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IMMDeviceEnumerator
	{
		int EnumAudioEndpoints(int dataFlow, int dwStateMask, out IntPtr ppDevices);
		[PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppEndpoint);
		int GetDevice(string pwstrId, out IMMDevice ppDevice);
		int RegisterEndpointNotificationCallback(IntPtr pClient);
		int UnregisterEndpointNotificationCallback(IntPtr pClient);
	}

	[Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IMMDevice
	{
		[PreserveSig] int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
		int OpenPropertyStore(int stgmAccess, out IntPtr ppProperties);
		int GetId(out IntPtr ppstrId);
		int GetState(out int pdwState);
	}

	[Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IAudioClient
	{
		[PreserveSig] int Initialize(int shareMode, int streamFlags, long hnsBufferDuration, long hnsPeriodicity, IntPtr pFormat, IntPtr audioSessionGuid);
		[PreserveSig] int GetBufferSize(out uint pNumBufferFrames);
		[PreserveSig] int GetStreamLatency(out long phnsLatency);
		[PreserveSig] int GetCurrentPadding(out uint pNumPaddingFrames);
		[PreserveSig] int IsFormatSupported(int shareMode, IntPtr pFormat, out IntPtr ppClosestMatch);
		[PreserveSig] int GetMixFormat(out IntPtr ppDeviceFormat);
		[PreserveSig] int GetDevicePeriod(out long phnsDefaultDevicePeriod, out long phnsMinimumDevicePeriod);
		[PreserveSig] int Start();
		[PreserveSig] int Stop();
		[PreserveSig] int Reset();
		[PreserveSig] int SetEventHandle(IntPtr eventHandle);
		[PreserveSig] int GetService(ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
	}

	[Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IAudioCaptureClient
	{
		[PreserveSig] int GetBuffer(out IntPtr ppData, out uint pNumFramesToRead, out int pdwFlags, out ulong pu64DevicePosition, out ulong pu64QPCPosition);
		[PreserveSig] int ReleaseBuffer(uint numFramesRead);
		[PreserveSig] int GetNextPacketSize(out uint pNumFramesInNextPacket);
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	private struct WaveFormatEx
	{
		public ushort wFormatTag, nChannels;
		public uint nSamplesPerSec, nAvgBytesPerSec;
		public ushort nBlockAlign, wBitsPerSample, cbSize;
	}

	private const int RingSize = 65536;
	private const int RingMask = RingSize - 1;
	private const int AudclntStreamflagsLoopback = 0x00020000;
	private const int AudclntBufferflagsSilent = 0x00000002;
	private const int AudclntDeviceInvalidated = unchecked((int)0x88890004);

	private readonly object _sync = new object();
	private readonly ManualResetEvent _stopEvent = new ManualResetEvent(false);
	private readonly float[] _ringL = new float[RingSize];
	private readonly float[] _ringR = new float[RingSize];

	private Thread _thread;
	private long _head;
	private long _lastReadHead = -1;
	private long _lastSoundTime;
	private volatile int _sampleRate = 48000;
	private volatile bool _running;
	private bool _disposed;
	private string _status = "Idle";

	public int SampleRate => _sampleRate;
	public string Status => _status;
	public bool IsRunning => _running;

	public void Start()
	{
		lock (_sync)
		{
			if (_running || _disposed) return;
			_running = true;
			_stopEvent.Reset();
			_thread = new Thread(CaptureLoop)
			{
				IsBackground = true,
				Name = "VisualBee.WasapiLoopback"
			};
			_thread.Start();
		}
	}

	public void Stop()
	{
		lock (_sync)
		{
			if (!_running) return;
			_running = false;
			_stopEvent.Set();
		}
		if (_thread != null && _thread.IsAlive)
		{
			_thread.Join(250);
			_thread = null;
		}
	}

	public bool HasActiveAudio(double timeoutSeconds = 0.6)
	{
		long last = Interlocked.Read(ref _lastSoundTime);
		if (last == 0) return false;
		double elapsed = (double)(Stopwatch.GetTimestamp() - last) / Stopwatch.Frequency;
		return elapsed < timeoutSeconds;
	}

	public bool ReadWindow(int count, float[] dstL, float[] dstR, bool stereo, int channelMode, out int sampleRate, out bool fresh)
	{
		return ReadWindow(count, dstL, dstR, stereo, channelMode, ref _lastReadHead, out sampleRate, out fresh);
	}

	public bool ReadWindow(int count, float[] dstL, float[] dstR, bool stereo, int channelMode, ref long lastReadHead, out int sampleRate, out bool fresh)
	{
		sampleRate = _sampleRate;
		if (count <= 0 || dstL == null || (stereo && dstR == null))
		{
			fresh = false;
			return false;
		}
		lock (_sync)
		{
			long head = _head;
			fresh = head != lastReadHead;
			lastReadHead = head;
			long start = head - count;
			for (int k = 0; k < count; k++)
			{
				int idx = (int)((start + k) & RingMask);
				float l = _ringL[idx];
				float r = _ringR[idx];
				if (stereo)
				{
					dstL[k] = l;
					dstR[k] = r;
				}
				else
				{
					switch (channelMode)
					{
						case 1: dstL[k] = l; break;
						case 2: dstL[k] = r; break;
						case 4: dstL[k] = (l - r) * 0.5f; break;
						default: dstL[k] = (l + r) * 0.5f; break;
					}
				}
			}
		}
		return true;
	}

	private unsafe void CaptureLoop()
	{
		while (!_stopEvent.WaitOne(0))
		{
			IMMDeviceEnumerator enumerator = null;
			IMMDevice endpoint = null;
			IAudioClient client = null;
			IAudioCaptureClient capture = null;
			try
			{
				enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
				if (enumerator.GetDefaultAudioEndpoint(0, 1, out endpoint) != 0 || endpoint == null)
				{
					_status = "No default audio endpoint";
					if (_stopEvent.WaitOne(1000)) break;
					continue;
				}
				var iidClient = new Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");
				if (endpoint.Activate(ref iidClient, 23, IntPtr.Zero, out var clientObj) != 0 || clientObj == null)
				{
					_status = "AudioClient activation failed";
					if (_stopEvent.WaitOne(1000)) break;
					continue;
				}
				client = (IAudioClient)clientObj;
				IntPtr pFormat = IntPtr.Zero;
				int chans = 2;
				int rate = 48000;
				int bits = 32;
				bool isFloat = true;
				int hr = 0;
				try
				{
					if (client.GetMixFormat(out pFormat) != 0 || pFormat == IntPtr.Zero)
					{
						_status = "GetMixFormat failed";
						if (_stopEvent.WaitOne(1000)) break;
						continue;
					}
					var fmt = (WaveFormatEx)Marshal.PtrToStructure(pFormat, typeof(WaveFormatEx));
					chans = Math.Max(1, (int)fmt.nChannels);
					rate = Math.Max(8000, (int)fmt.nSamplesPerSec);
					bits = fmt.wBitsPerSample;
					isFloat = (fmt.wFormatTag == 3);
					if (fmt.wFormatTag == 0xFFFE && fmt.cbSize >= 22)
					{
						isFloat = (Marshal.ReadInt32(pFormat, 24) == 3);
					}
					_sampleRate = rate;

					hr = client.Initialize(0, AudclntStreamflagsLoopback, 2000000, 0, pFormat, IntPtr.Zero);
					if (hr != 0)
					{
						_status = "Loopback init failed: 0x" + hr.ToString("X8");
						if (_stopEvent.WaitOne(1000)) break;
						continue;
					}
				}
				finally
				{
					if (pFormat != IntPtr.Zero) Marshal.FreeCoTaskMem(pFormat);
				}
				var iidCapture = new Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317");
				if (client.GetService(ref iidCapture, out var captureObj) != 0 || captureObj == null)
				{
					_status = "CaptureClient unavailable";
					if (_stopEvent.WaitOne(1000)) break;
					continue;
				}
				capture = (IAudioCaptureClient)captureObj;
				client.Start();
				_status = "Active (" + rate + " Hz, " + chans + "ch)";

				while (!_stopEvent.WaitOne(0))
				{
					hr = capture.GetNextPacketSize(out var packetLength);
					if (hr < 0) break;
					if (packetLength == 0)
					{
						if (_stopEvent.WaitOne(4)) break;
						continue;
					}
					while (packetLength > 0 && !_stopEvent.WaitOne(0))
					{
						hr = capture.GetBuffer(out var pData, out var frames, out var flags, out _, out _);
						if (hr < 0) break;
						bool silent = (flags & AudclntBufferflagsSilent) != 0;
						ProcessFrames(pData, (int)frames, chans, bits, isFloat, silent);
						capture.ReleaseBuffer(frames);
						hr = capture.GetNextPacketSize(out packetLength);
						if (hr < 0) break;
					}
					if (hr < 0) break;
					if (_stopEvent.WaitOne(2)) break;
				}
			}
			catch (Exception ex)
			{
				_status = "Loopback error: " + ex.Message;
			}
			finally
			{
				try { client?.Stop(); } catch { }
				if (capture != null) { try { Marshal.ReleaseComObject(capture); } catch { } capture = null; }
				if (client != null) { try { Marshal.ReleaseComObject(client); } catch { } client = null; }
				if (endpoint != null) { try { Marshal.ReleaseComObject(endpoint); } catch { } endpoint = null; }
				if (enumerator != null) { try { Marshal.ReleaseComObject(enumerator); } catch { } enumerator = null; }
			}
			if (!_stopEvent.WaitOne(0)) _stopEvent.WaitOne(500);
		}
		_status = "Stopped";
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

	public void Dispose()
	{
		Stop();
		lock (_sync)
		{
			_disposed = true;
			try { _stopEvent.Dispose(); } catch { }
		}
	}
}
