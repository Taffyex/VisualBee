using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace MusicBeePlugin;

internal sealed partial class WasapiLoopback : IDisposable
{
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
	private const int AudclntStreamflagsEventCallback = 0x00040000;
	private const int AudclntBufferflagsSilent = 0x00000002;
	private const int AudclntDeviceInvalidated = unchecked((int)0x88890004);

	private readonly object _sync = new object();
	private readonly ManualResetEvent _stopEvent = new ManualResetEvent(false);
	private readonly AutoResetEvent _wakeEvent = new AutoResetEvent(false);
	private readonly AutoResetEvent _packetEvent = new AutoResetEvent(false);
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
	private volatile string _targetDeviceId = "default";
	private int _deviceVersion;

	public int SampleRate => _sampleRate;
	public string Status => _status;
	public bool IsRunning => _running;
	public string TargetDeviceId => _targetDeviceId;

	public void SetDevice(string deviceId)
	{
		string norm = string.IsNullOrWhiteSpace(deviceId) ? "default" : deviceId.Trim();
		if (string.Equals(_targetDeviceId, norm, StringComparison.OrdinalIgnoreCase)) return;
		_targetDeviceId = norm;
		Interlocked.Increment(ref _deviceVersion);
		try { _wakeEvent.Set(); } catch { }
	}

	public void Start()
	{
		lock (_sync)
		{
			if (_running || _disposed) return;
			_running = true;
			_stopEvent.Reset();
			_wakeEvent.Reset();
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
			_wakeEvent.Set();
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
		return ReadWindow(count, 0, dstL, dstR, stereo, channelMode, ref _lastReadHead, out sampleRate, out fresh);
	}

	public bool ReadWindow(int count, int delayMs, float[] dstL, float[] dstR, bool stereo, int channelMode, ref long lastReadHead, out int sampleRate, out bool fresh)
	{
		sampleRate = _sampleRate;
		if (count <= 0 || dstL == null || (stereo && dstR == null))
		{
			fresh = false;
			return false;
		}
		int rate = (sampleRate > 0) ? sampleRate : 48000;
		int delaySamples = Math.Max(0, Math.Min(RingSize - count - 16, (int)((long)delayMs * (long)rate / 1000)));

		lock (_sync)
		{
			long head = _head;
			fresh = head != lastReadHead;
			lastReadHead = head;
			long delayedHead = head - delaySamples;
			long start = delayedHead - count;

			if (stereo)
			{
				CopyRingRange(_ringL, start, count, dstL);
				CopyRingRange(_ringR, start, count, dstR);
			}
			else
			{
				switch (channelMode)
				{
					case 1: CopyRingRange(_ringL, start, count, dstL); break;
					case 2: CopyRingRange(_ringR, start, count, dstL); break;
					case 4:
						for (int k = 0; k < count; k++)
						{
							int idx = (int)((start + k) & RingMask);
							dstL[k] = (_ringL[idx] - _ringR[idx]) * 0.5f;
						}
						break;
					default:
						for (int k = 0; k < count; k++)
						{
							int idx = (int)((start + k) & RingMask);
							dstL[k] = (_ringL[idx] + _ringR[idx]) * 0.5f;
						}
						break;
				}
			}
		}
		return true;
	}

	private static void CopyRingRange(float[] ring, long start, int count, float[] dst)
	{
		int ringStart = (int)(start & RingMask);
		int firstChunk = Math.Min(count, RingSize - ringStart);
		Array.Copy(ring, ringStart, dst, 0, firstChunk);
		int remaining = count - firstChunk;
		if (remaining > 0)
		{
			Array.Copy(ring, 0, dst, firstChunk, remaining);
		}
	}

	private unsafe void CaptureLoop()
	{
		while (!_stopEvent.WaitOne(0))
		{
			IMMDeviceEnumerator enumerator = null;
			IMMDevice endpoint = null;
			IAudioClient client = null;
			IAudioCaptureClient capture = null;
			int activeVersion = Volatile.Read(ref _deviceVersion);
			string targetId = _targetDeviceId;
			string activeEndpointId = null;
			string deviceDisplayName = "Default";
			bool isCapture = false;
			bool isFallback = false;
			var iidClient = new Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");

			try
			{
				if (string.Equals(targetId, "process", StringComparison.OrdinalIgnoreCase))
				{
					if (TryActivateProcessLoopback(out client) && client != null)
					{
						deviceDisplayName = "MusicBee & Browser2 Tree";
					}
					else
					{
						targetId = "default";
					}
				}

				if (client == null)
				{
					enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
					bool isDefaultTarget = string.IsNullOrEmpty(targetId) || string.Equals(targetId, "default", StringComparison.OrdinalIgnoreCase);

					if (!isDefaultTarget)
					{
						if (enumerator.GetDevice(targetId, out endpoint) != 0 || endpoint == null)
						{
							isFallback = true;
							isDefaultTarget = true;
						}
					}

					if (isDefaultTarget)
					{
						if (enumerator.GetDefaultAudioEndpoint(0, 1, out endpoint) != 0 || endpoint == null)
						{
							_status = "No default audio endpoint";
							if (_stopEvent.WaitOne(1000)) break;
							continue;
						}
					}

					if (endpoint.GetId(out activeEndpointId) == 0 && !string.IsNullOrEmpty(activeEndpointId))
					{
						deviceDisplayName = isFallback ? ("Fallback -> " + GetDeviceDisplayName(activeEndpointId)) : GetDeviceDisplayName(activeEndpointId);
					}

					if (IsCaptureDeviceId(activeEndpointId) ||
					    (endpoint is IMMEndpoint ep && ep.GetDataFlow(out int flow) == 0 && flow == 1))
					{
						isCapture = true;
					}

					if (endpoint.Activate(ref iidClient, 23, IntPtr.Zero, out var clientObj) != 0 || clientObj == null)
					{
						_status = "AudioClient activation failed";
						if (_stopEvent.WaitOne(1000)) break;
						continue;
					}
					client = (IAudioClient)clientObj;
				}

				IntPtr pFormat = IntPtr.Zero;
				int chans = 2;
				int rate = 48000;
				int bits = 32;
				bool isFloat = true;
				int hr = 0;
				bool isEventDriven = false;

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

					int streamFlags = isCapture ? 0 : AudclntStreamflagsLoopback;
					_packetEvent.Reset();
					hr = client.Initialize(0, streamFlags | AudclntStreamflagsEventCallback, 500000, 0, pFormat, IntPtr.Zero);
					if (hr == 0 && client.SetEventHandle(_packetEvent.SafeWaitHandle.DangerousGetHandle()) == 0)
					{
						isEventDriven = true;
					}
					else if (endpoint != null)
					{
						try { Marshal.ReleaseComObject(client); } catch { }
						client = null;
						if (endpoint.Activate(ref iidClient, 23, IntPtr.Zero, out var freshObj) == 0 && freshObj is IAudioClient freshClient)
						{
							client = freshClient;
							hr = client.Initialize(0, streamFlags, 500000, 0, pFormat, IntPtr.Zero);
						}
					}

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
				_status = "Active [" + deviceDisplayName + "] (" + rate + " Hz, " + chans + "ch)";

				WaitHandle[] waitHandles = isEventDriven
					? new WaitHandle[] { _stopEvent, _wakeEvent, _packetEvent }
					: new WaitHandle[] { _stopEvent, _wakeEvent };

				long lastProbeTime = Stopwatch.GetTimestamp();

				while (!_stopEvent.WaitOne(0))
				{
					if (activeVersion != Volatile.Read(ref _deviceVersion))
					{
						break;
					}

					int waitIdx = WaitHandle.WaitAny(waitHandles, 25);
					if (waitIdx == 0) break;
					if (waitIdx == 1)
					{
						if (activeVersion != Volatile.Read(ref _deviceVersion)) break;
					}

					hr = capture.GetNextPacketSize(out var packetLength);
					if (hr < 0) break;

					if (packetLength == 0)
					{
						FadeOutIdleFrames(128);
					}

					while (packetLength > 0 && !_stopEvent.WaitOne(0))
					{
						if (activeVersion != Volatile.Read(ref _deviceVersion)) break;
						hr = capture.GetBuffer(out var pData, out var frames, out var flags, out _, out _);
						if (hr < 0) break;
						bool silent = (flags & AudclntBufferflagsSilent) != 0;
						ProcessFrames(pData, (int)frames, chans, bits, isFloat, silent);
						capture.ReleaseBuffer(frames);
						hr = capture.GetNextPacketSize(out packetLength);
						if (hr < 0) break;
					}
					if (hr < 0) break;

					long now = Stopwatch.GetTimestamp();
					if ((double)(now - lastProbeTime) / Stopwatch.Frequency > 1.5)
					{
						lastProbeTime = now;
						if (string.Equals(_targetDeviceId, "default", StringComparison.OrdinalIgnoreCase) && enumerator != null)
						{
							if (enumerator.GetDefaultAudioEndpoint(0, 1, out var curDef) == 0 && curDef != null)
							{
								try
								{
									if (curDef.GetId(out string newDefId) == 0 && !string.Equals(activeEndpointId, newDefId, StringComparison.OrdinalIgnoreCase))
									{
										break;
									}
								}
								finally
								{
									try { Marshal.ReleaseComObject(curDef); } catch { }
								}
							}
						}
						else if (isFallback && enumerator != null)
						{
							if (enumerator.GetDevice(_targetDeviceId, out var recDev) == 0 && recDev != null)
							{
								try { Marshal.ReleaseComObject(recDev); } catch { }
								break;
							}
						}
					}
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

			if (!_stopEvent.WaitOne(0) && activeVersion == Volatile.Read(ref _deviceVersion))
			{
				_stopEvent.WaitOne(400);
			}
		}
		_status = "Stopped";
	}

	public void Dispose()
	{
		Stop();
		lock (_sync)
		{
			_disposed = true;
			try { _stopEvent.Dispose(); } catch { }
			try { _wakeEvent.Dispose(); } catch { }
			try { _packetEvent.Dispose(); } catch { }
		}
	}
}
