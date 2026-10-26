using System;
using System.Diagnostics;
using System.Threading;

namespace MusicBeePlugin
{
    /// <summary>
    /// Background audio analysis engine: direct BASS PCM when available, official
    /// spectrum API fallback. Publishes thread-safe snapshots for visualizer renderers.
    /// </summary>
    public sealed partial class AudioEngine : IDisposable
    {
        public const int BandCount = 160;

        public static bool PreviewMode;
        private double _previewTime;

        private readonly MusicBeeApiInterface _api;
        private readonly VisualBeeSettings _settings;
        private readonly AudioTap _tap = new AudioTap();
        private readonly FoobarAnalyzer _analyzer = new FoobarAnalyzer();
        private readonly PreciseTimer _timer = new PreciseTimer();

        private readonly double[] _db = new double[BandCount];
        private readonly float[] _levels = new float[BandCount];
        private readonly float[] _prevLevels = new float[BandCount];
        private float[] _mono;
        private float[] _left;
        private float[] _right;
        private readonly double[] _dbL = new double[BandCount];
        private readonly double[] _dbR = new double[BandCount];
        private readonly float[] _levelsL = new float[BandCount];
        private readonly float[] _levelsR = new float[BandCount];
        private readonly float[] _waveform = new float[256];
        private readonly float[] _waveformL = new float[256];
        private readonly float[] _waveformR = new float[256];

        private readonly object _gate = new object();
        private readonly float[] _publishedLevels = new float[BandCount];
        private readonly float[] _publishedLevelsL = new float[BandCount];
        private readonly float[] _publishedLevelsR = new float[BandCount];
        private readonly float[] _publishedWaveform = new float[256];
        private readonly float[] _publishedWaveformL = new float[256];
        private readonly float[] _publishedWaveformR = new float[256];
        private AudioSnapshot _snapshot;
        private long _publishedTimestamp;
        private ulong _sequence;
        private string _status = "waiting for playback";

        private Thread _thread;
        private volatile bool _running;
        private volatile bool _active = true;
        private volatile bool _reset;

        private int _subLo, _subHi;
        private int _bassLo, _bassHi;
        private int _lowMidLo, _lowMidHi;
        private int _midLo, _midHi;
        private int _highMidLo, _highMidHi;
        private int _highLo, _highHi;
        private int _airLo, _airHi;

        private float _envSub, _envBass, _envLowMid, _envMid, _envHighMid, _envHigh, _envAir;
        private float _envRms, _envTransient, _envBeat;
        private float _lowPulse;
        private float _prevLow;
        private float _maxSub = 0.2f, _maxBass = 0.2f, _maxLowMid = 0.2f;
        private float _maxMid = 0.2f, _maxHighMid = 0.2f, _maxHigh = 0.2f, _maxAir = 0.2f;
        private float _maxRms = 0.2f;
        private float _fluxAvg, _fluxVar;
        private long _lastBeatTicks;

        public AudioEngine(MusicBeeApiInterface api, VisualBeeSettings settings)
        {
            _api = api;
            _settings = settings;
            _snapshot.Waveform = _publishedWaveform;
            _snapshot.WaveformL = _publishedWaveformL;
            _snapshot.WaveformR = _publishedWaveformR;
            BuildBandRanges();
        }

        public void Probe()
        {
            try
            {
                _tap.Bind(_api.NowPlaying_GetSpectrumData);
            }
            catch { }
        }

        public void Start()
        {
            if (_thread != null) return;
            _running = true;
            _thread = new Thread(Loop)
            {
                IsBackground = true,
                Name = "VisualBee audio engine",
                Priority = ThreadPriority.Normal
            };
            _thread.Start();
        }

        public void SetActive(bool active)
        {
            _active = active;
        }

        public void NotifyTrackChanged()
        {
            _reset = true;
        }

        public void NotifyPlayStateChanged()
        {
        }

        public AudioSnapshot GetSnapshot()
        {
            lock (_gate) return _snapshot;
        }

        public AudioSnapshot GetSnapshot(float[] bandDest)
        {
            lock (_gate)
            {
                if (bandDest != null)
                {
                    int n = Math.Min(bandDest.Length, _publishedLevels.Length);
                    Array.Copy(_publishedLevels, bandDest, n);
                    if (n < bandDest.Length)
                        Array.Clear(bandDest, n, bandDest.Length - n);
                }
                return _snapshot;
            }
        }

        public bool GetFrame(BandFrame dest)
        {
            if (dest == null) return false;
            lock (_gate)
            {
                Array.Copy(_publishedLevels, dest.Levels, BandCount);
                Array.Copy(_publishedLevelsL, dest.LevelsL, BandCount);
                Array.Copy(_publishedLevelsR, dest.LevelsR, BandCount);
                Array.Copy(_publishedWaveform, dest.Waveform, 256);
                Array.Copy(_publishedWaveformL, dest.WaveformL, 256);
                Array.Copy(_publishedWaveformR, dest.WaveformR, 256);
                dest.Metrics = _snapshot;
                dest.TimestampTicks = _publishedTimestamp;
                dest.Sequence = _sequence;
                return true;
            }
        }

        public string Status
        {
            get { lock (_gate) return _status; }
        }

        public string Timing
        {
            get { return _tap.Timing; }
        }

        public bool Direct
        {
            get { return _tap.Direct; }
        }

        public string TapDescription
        {
            get { return _tap.Describe(); }
        }

        public void Dispose()
        {
            _running = false;
            Thread t = _thread;
            _thread = null;
            if (t != null && t.IsAlive)
            {
                try { t.Join(500); } catch { }
            }
            _timer.Dispose();
        }

        private void BuildBandRanges()
        {
            if (_settings != null)
            {
                _appliedMinFreq = _settings.MinFreqHz;
                _appliedMaxFreq = _settings.MaxFreqHz;
            }
            _subLo = FindBand(20); _subHi = FindBand(60);
            _bassLo = FindBand(60); _bassHi = FindBand(120);
            _lowMidLo = FindBand(120); _lowMidHi = FindBand(300);
            _midLo = FindBand(300); _midHi = FindBand(1000);
            _highMidLo = FindBand(1000); _highMidHi = FindBand(3000);
            _highLo = FindBand(3000); _highHi = FindBand(8000);
            _airLo = FindBand(8000); _airHi = FindBand(16000);
        }

        private float _appliedMinFreq = 50.0f;
        private float _appliedMaxFreq = 20000.0f;

        private void CheckBandRanges()
        {
            if (_settings == null) return;
            if (Math.Abs(_settings.MinFreqHz - _appliedMinFreq) > 0.5f ||
                Math.Abs(_settings.MaxFreqHz - _appliedMaxFreq) > 0.5f)
            {
                BuildBandRanges();
            }
        }

        private int FindBand(double hz)
        {
            int best = 0;
            double minF = _appliedMinFreq;
            double maxF = _appliedMaxFreq;
            for (int i = 0; i < BandCount; i++)
            {
                if (FoobarAnalyzer.BandFrequency(i, BandCount, minF, maxF) <= hz) best = i;
                else break;
            }
            return best;
        }

        private void Loop()
        {
            long next = Stopwatch.GetTimestamp();
            long lastTicks = next;

            while (_running)
            {
                int targetHz = _settings != null ? _settings.AnalysisHz : 120;
                if (targetHz <= 0)
                {
                    long now = Stopwatch.GetTimestamp();
                    long diff = now - lastTicks;
                    if (diff > 0)
                    {
                        float dt = (float)diff / Stopwatch.Frequency;
                        if (_active) Tick(dt);
                        lastTicks = now;
                    }
                    Thread.Yield();
                    continue;
                }

                long interval = Math.Max(1, Stopwatch.Frequency / targetHz);
                long nowFixed = Stopwatch.GetTimestamp();
                if (nowFixed >= next)
                {
                    float dt = (float)interval / Stopwatch.Frequency;
                    if (_active) Tick(dt);
                    next += interval;
                    if (nowFixed - next > interval * 4) next = nowFixed + interval;
                    lastTicks = nowFixed;
                }
                else
                {
                    _timer.WaitUntil(next);
                }
            }
        }
    }
}
