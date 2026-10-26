using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace MusicBeePlugin
{
    /// <summary>
    /// Direct PCM tap into MusicBee's BASS mixer with latency compensation
    /// and official spectrum API fallback.
    /// </summary>
    internal sealed partial class AudioTap
    {
        private const int BassDataFloat = 0x40000000;
        private const int BassPosByte = 0;
        private const int BassStreamDecode = 0x200000;
        private const int RingSize = 262144;
        private const long RingMask = 262143L;

        private readonly object _bindLock = new object();
        private FieldInfo _playerField;
        private FieldInfo _handleField;
        private bool _bindAttempted;
        private DateTime _lastBindAttempt = DateTime.MinValue;

        private float[] _interleaved = new float[0];
        private readonly float[] _apiFft = new float[2048];
        private readonly float[] _ring = new float[RingSize];
        private readonly float[] _ringL = new float[RingSize];
        private readonly float[] _ringR = new float[RingSize];
        private float[] _dummyL;
        private float[] _dummyR;
        private int _ringHandle;
        private long _ringAnchor;
        private long _ringStart;
        private long _ringEnd;
        private bool _noWasapi;
        private bool _noAsio;

        public string Status = "unbound";
        private int _timingQueueMs = -1;
        private int _timingShortfallMs;
        private bool _timingDecodeStream;

        public string Timing
        {
            get
            {
                string text = _timingDecodeStream
                    ? (_timingQueueMs >= 0 ? "output queue " + _timingQueueMs + " ms" : "output queue")
                    : "BASS output";
                return _timingShortfallMs > 0
                    ? text + ", " + _timingShortfallMs + " ms short of look-ahead"
                    : text;
            }
        }

        public bool Direct
        {
            get { lock (_bindLock) return _handleField != null; }
        }

        public void Bind(Delegate spectrumApi)
        {
            lock (_bindLock)
            {
                if (_handleField != null) return;
                if (_bindAttempted && (DateTime.UtcNow - _lastBindAttempt).TotalSeconds < 2.0) return;
                _bindAttempted = true;
                _lastBindAttempt = DateTime.UtcNow;
                try
                {
                    if (spectrumApi == null)
                    {
                        Status = "spectrum API missing";
                        return;
                    }
                    BindMethod(spectrumApi.Method);
                }
                catch (Exception ex)
                {
                    _playerField = null;
                    _handleField = null;
                    Status = "MusicBee audio hook failed: " + ex.GetType().Name;
                }
            }
        }

        public string Describe()
        {
            lock (_bindLock)
            {
                if (_handleField == null) return "unbound";
                string player = _playerField != null
                    ? _playerField.DeclaringType.FullName + "::" + _playerField.Name + " -> "
                    : "";
                return player + _handleField.DeclaringType.FullName + "::" + _handleField.Name;
            }
        }

        private int CurrentHandle()
        {
            object player = null;
            if (_playerField != null)
            {
                player = _playerField.GetValue(null);
                if (player == null) return 0;
            }
            object value = _handleField.GetValue(player);
            return value is int ? (int)value : 0;
        }

        public bool ReadPcm(int delayMs, ref float[] mono, out int size, out int rate)
        {
            return ReadPcm(delayMs, ref mono, ref _dummyL, ref _dummyR, out size, out rate);
        }

        public bool ReadPcm(int delayMs, ref float[] mono, ref float[] left, ref float[] right, out int size, out int rate)
        {
            size = 0;
            rate = 0;

            int handle;
            try
            {
                lock (_bindLock)
                {
                    if (_handleField == null) return false;
                    handle = CurrentHandle();
                }
            }
            catch { return false; }
            if (handle == 0) return false;

            BassNative.BassChannelInfo info = default(BassNative.BassChannelInfo);
            if (!BassNative.Bass.ChannelGetInfo(handle, ref info) || info.freq <= 0 || info.chans <= 0)
                return false;

            rate = info.freq;
            int chans = info.chans;
            size = FoobarAnalyzer.WindowSize(rate);
            int half = size / 2;
            long delaySamples = (long)delayMs * rate / 1000;

            bool decodeStream = false;
            int mixer = BassNative.Bass.MixerChannelGetMixer(handle);
            BassNative.BassChannelInfo mixerInfo = default(BassNative.BassChannelInfo);
            if (mixer != 0 && BassNative.Bass.ChannelGetInfo(mixer, ref mixerInfo))
                decodeStream = (mixerInfo.flags & BassStreamDecode) != 0;

            double queueSeconds = 0.0;
            long before = -1;
            int read = -1;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                long posBefore = BassNative.Bass.MixerChannelGetPosition(handle, BassPosByte);
                if (decodeStream) queueSeconds = OutputQueueSeconds();
                int want = decodeStream
                    ? size + (int)(queueSeconds * rate) + (int)delaySamples
                    : size;
                if (_interleaved.Length < want * chans)
                    _interleaved = new float[want * chans];

                read = BassNative.Bass.MixerChannelGetData(handle, _interleaved, (want * chans * 4) | BassDataFloat);
                long posAfter = BassNative.Bass.MixerChannelGetPosition(handle, BassPosByte);
                before = posAfter;
                if (!decodeStream || posBefore == posAfter) break;
            }

            if (read <= 0 || before < 0) return false;

            int frames = read / (4 * chans);
            long endSample = (long)Math.Round(BassNative.Bass.ChannelBytes2Seconds(handle, before) * rate);
            long blockStart = decodeStream ? endSample - frames : endSample;
            long audioNow = decodeStream
                ? endSample - (long)Math.Round(queueSeconds * rate)
                : endSample;

            Store(handle, decodeStream ? endSample : blockStart, blockStart, frames, chans);

            long readStart = audioNow - delaySamples - half;
            long shortfall = readStart + size - _ringEnd;
            if (shortfall > 0) readStart -= shortfall;

            if (mono == null || mono.Length < size) mono = new float[size];
            if (left == null || left.Length < size) left = new float[size];
            if (right == null || right.Length < size) right = new float[size];

            for (int i = 0; i < size; i++)
            {
                long pos = readStart + i;
                int maskIdx = (int)(pos & RingMask);
                bool inRange = (pos >= _ringStart && pos < _ringEnd);
                mono[i] = inRange ? _ring[maskIdx] : 0f;
                left[i] = inRange ? _ringL[maskIdx] : 0f;
                right[i] = inRange ? _ringR[maskIdx] : 0f;
            }

            _timingDecodeStream = decodeStream;
            _timingQueueMs = decodeStream ? (int)Math.Round(queueSeconds * 1000.0) : -1;
            _timingShortfallMs = shortfall > 0 ? (int)Math.Round(shortfall * 1000.0 / rate) : 0;
            return true;
        }

        private void Store(int handle, long anchor, long blockStart, int got, int chans)
        {
            long end = blockStart + got;
            if (handle != _ringHandle || anchor < _ringAnchor || blockStart > _ringEnd)
            {
                _ringHandle = handle;
                _ringStart = blockStart;
                _ringEnd = blockStart;
            }
            _ringAnchor = anchor;

            long start = Math.Max(blockStart, end - RingSize);
            float scale = 1f / chans;
            for (long pos = start; pos < end; pos++)
            {
                int src = (int)(pos - blockStart) * chans;
                float l = _interleaved[src];
                float r = chans > 1 ? _interleaved[src + 1] : l;
                float sum = l;
                for (int c = 1; c < chans; c++) sum += _interleaved[src + c];
                int maskIdx = (int)(pos & RingMask);
                _ring[maskIdx] = sum * scale;
                _ringL[maskIdx] = l;
                _ringR[maskIdx] = r;
            }
            if (end > _ringEnd) _ringEnd = end;
            if (start < _ringStart) _ringStart = start;
            if (_ringStart < _ringEnd - RingSize) _ringStart = _ringEnd - RingSize;
        }

        private double OutputQueueSeconds()
        {
            return BassNative.QueryOutputQueueSeconds(ref _noWasapi, ref _noAsio);
        }

        public bool ReadApiFft(NowPlaying_GetSpectrumDataDelegate api, out float[] magnitudes, out int count)
        {
            magnitudes = _apiFft;
            count = 0;
            if (api == null) return false;

            Array.Clear(_apiFft, 0, _apiFft.Length);
            int n;
            try { n = api(_apiFft); }
            catch { return false; }
            if (n <= 0) return false;

            if (n > _apiFft.Length)
            {
                if (n % 4 == 0 && n / 4 <= _apiFft.Length) n /= 4;
                else n = _apiFft.Length;
            }
            count = n;
            return true;
        }
    }
}
