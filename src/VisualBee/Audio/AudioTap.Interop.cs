using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace MusicBeePlugin;

internal sealed partial class AudioTap
{
	internal struct BassChannelInfo
	{
		public int freq;

		public int chans;

		public int flags;

		public int ctype;

		public int origres;

		public int plugin;

		public int sample;

		public IntPtr filename;
	}

	internal struct WasapiInfo
	{
		public int initflags;

		public int freq;

		public int chans;

		public int format;

		public int buflen;

		public float volmax;

		public float volmin;

		public float volstep;
	}

	private static class Bass
	{
		[DllImport("bass.dll", EntryPoint = "BASS_ErrorGetCode")]
		public static extern int ErrorGetCode();

		[DllImport("bass.dll", EntryPoint = "BASS_ChannelGetInfo")]
		public static extern bool ChannelGetInfo(int handle, ref BassChannelInfo info);

		[DllImport("bass.dll", EntryPoint = "BASS_ChannelIsActive")]
		public static extern int ChannelIsActive(int handle);

		[DllImport("bassmix.dll", EntryPoint = "BASS_Mixer_ChannelFlags")]
		public static extern int MixerChannelFlags(int handle, int flags, int mask);

		[DllImport("bass.dll", EntryPoint = "BASS_ChannelBytes2Seconds")]
		public static extern double ChannelBytes2Seconds(int handle, long position);

		[DllImport("bassmix.dll", EntryPoint = "BASS_Mixer_ChannelGetData")]
		public static extern int MixerChannelGetData(int handle, [Out] float[] buffer, int length);

		[DllImport("bassmix.dll", EntryPoint = "BASS_Mixer_ChannelGetPosition")]
		public static extern long MixerChannelGetPosition(int handle, int mode);

		[DllImport("bassmix.dll", EntryPoint = "BASS_Mixer_ChannelGetMixer")]
		public static extern int MixerChannelGetMixer(int handle);

		[DllImport("bassmix.dll", EntryPoint = "BASS_Mixer_ChannelIsActive")]
		public static extern int MixerChannelIsActive(int handle);

		[DllImport("bassmix.dll", EntryPoint = "BASS_Mixer_StreamGetChannels")]
		public static extern int MixerStreamGetChannels(int mixer, [Out] int[] channels, int count);
	}

	private static class Wasapi
	{
		[DllImport("basswasapi.dll", EntryPoint = "BASS_WASAPI_IsStarted")]
		public static extern bool IsStarted();

		[DllImport("basswasapi.dll", EntryPoint = "BASS_WASAPI_GetData")]
		public static extern int GetData(IntPtr buffer, int length);

		[DllImport("basswasapi.dll", EntryPoint = "BASS_WASAPI_GetInfo")]
		public static extern bool GetInfo(out WasapiInfo info);
	}

	private static class Asio
	{
		[DllImport("bassasio.dll", EntryPoint = "BASS_ASIO_IsStarted")]
		public static extern bool IsStarted();

		[DllImport("bassasio.dll", EntryPoint = "BASS_ASIO_GetLatency")]
		public static extern int GetLatency(bool input);

		[DllImport("bassasio.dll", EntryPoint = "BASS_ASIO_GetRate")]
		public static extern double GetRate();
	}

	private const int BassDataFloat = 1073741824;

	private const int BassPosByte = 0;

	private const int BassStreamDecode = 2097152;

	private const int BassActivePlaying = 1;

	private const int BassMixerChanPause = 131072;

	private const int RingFrames = 524288;

	private const long RingMask = 524287L;

	private FieldInfo _playerField;

	private FieldInfo _handleField;

	private bool _bindTried;

	internal bool BindTried => _bindTried;

	private float[] _interleaved = new float[0];

	private readonly float[] _apiFft = new float[2048];

	private float[] _ring = new float[0];

	private int _ringHandle;

	private int _ringRate;

	private int _ringChans;

	private long _offset;

	private long _ringAnchor;

	private long _ringStart;

	private long _ringEnd;

	private int _handle;

	private readonly int[] _sources = new int[16];

	private bool _decoding;

	private bool _noWasapi;

	private bool _noAsio;

	private int _srcRate;

	private int _factor = 1;

	private int _ringFactor = 1;

	private DsdConverter _dsd;

	private float[] _dsdOut = new float[0];

	private long _convNext;

	private long _outStart;

	private long _outEnd;

	private int _probeHandle;

	private int _probeFactor;

	private float[] _probe = new float[0];

	public string Status = "";

	public string Problem = "";

	public int Polls;

	public int Rate;

	public int Channels;

	public long Heard;

	public double Queued;

	public long SegmentStart;

	public int Segment;

	private static bool _noMixerIsActive;

	public bool Direct => _handleField != null;

	public bool Decoding => _decoding;

	public long AvailableEnd
	{
		get
		{
			if (_ringFactor <= 1)
			{
				return _ringEnd;
			}
			return _outEnd;
		}
	}

	public int HistoryRate
	{
		get
		{
			if (_ringFactor <= 1)
			{
				return _ringRate;
			}
			return _ringRate / _ringFactor;
		}
	}

	public int HistoryChannels => _ringChans;

	internal Func<double> QueueProbe { get; set; }

	public long ViewStart
	{
		get
		{
			if (_ringFactor <= 1)
			{
				return _ringStart;
			}
			return _outStart;
		}
	}

	public bool ReadApiFft(NowPlaying_GetSpectrumDataDelegate api, out float[] magnitudes)
	{
		magnitudes = _apiFft;
		if (api == null)
		{
			return false;
		}
		Array.Clear(_apiFft, 0, _apiFft.Length);
		int num;
		try
		{
			num = api(_apiFft);
		}
		catch (Exception)
		{
			return false;
		}
		return num > 0;
	}
}
