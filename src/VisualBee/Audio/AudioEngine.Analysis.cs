using System;
using System.Diagnostics;

namespace MusicBeePlugin
{
    public sealed partial class AudioEngine
    {
        private void Tick(float dt)
        {
            try
            {
                if (_settings == null) return;
                CheckBandRanges();

                if (PreviewMode)
                {
                    PublishPreview(dt);
                    return;
                }

                PlayState state = PlayState.Playing;
                if (_api.Player_GetPlayState != null)
                {
                    try { state = _api.Player_GetPlayState(); } catch { }
                }

                if (state != PlayState.Playing && state != PlayState.Paused)
                {
                    PublishSilent();
                    _status = "not playing";
                    return;
                }

                if (state == PlayState.Paused)
                {
                    lock (_gate) _status = "paused (frozen)";
                    return;
                }

                if (_reset)
                {
                    _reset = false;
                    Array.Clear(_prevLevels, 0, _prevLevels.Length);
                    _envSub = _envBass = _envLowMid = _envMid = _envHighMid = _envHigh = _envAir = 0f;
                    _envRms = _envTransient = _envBeat = 0f;
                    _lowPulse = 0f;
                    _prevLow = 0f;
                    _fluxAvg = _fluxVar = 0f;
                    _maxSub = _maxBass = _maxLowMid = _maxMid = 0.2f;
                    _maxHighMid = _maxHigh = _maxAir = _maxRms = 0.2f;
                }

                _tap.Bind(_api.NowPlaying_GetSpectrumData);

                bool useApi = _settings.AudioSource == AudioSourceMode.ApiFft;
                bool got = false;
                bool usedDirect = false;
                int rate = 0;
                int window = 0;
                string sourceText = "";
                float width = 0f, corr = 1f;

                if (!useApi && _settings.AudioSource != AudioSourceMode.ApiFft && _tap.Direct)
                {
                    float[] mono = _mono;
                    float[] left = _left;
                    float[] right = _right;
                    int size, r;
                    if (_tap.ReadPcm(_settings.SyncOffsetMs, ref mono, ref left, ref right, out size, out r))
                    {
                        _mono = mono;
                        _left = left;
                        _right = right;
                        _analyzer.Analyze(mono, size, r, BandCount, _db, _settings.MinFreqHz, _settings.MaxFreqHz);
                        if (_settings.StereoMode != StereoMode.Mono)
                        {
                            _analyzer.Analyze(left, size, r, BandCount, _dbL, _settings.MinFreqHz, _settings.MaxFreqHz);
                            _analyzer.Analyze(right, size, r, BandCount, _dbR, _settings.MinFreqHz, _settings.MaxFreqHz);
                        }
                        rate = r;
                        window = size;
                        got = true;
                        usedDirect = true;
                        sourceText = "direct PCM";

                        ComputeStereoMetrics(left, right, size, out width, out corr);
                        ExtractStereoWaveforms(left, right, size, dt);

                        // Zero-crossing phase-locked waveform extraction (256 points)
                        if (size >= 256)
                        {
                            int triggerIndex = 0;
                            int searchLimit = Math.Min(size - 256, size / 2);
                            for (int j = 1; j < searchLimit; j++)
                            {
                                if (mono[j - 1] <= 0f && mono[j] > 0.003f)
                                {
                                    triggerIndex = j;
                                    break;
                                }
                            }
                            if (triggerIndex == 0)
                            {
                                for (int j = 1; j < searchLimit; j++)
                                {
                                    if (mono[j - 1] <= 0f && mono[j] > 0f)
                                    {
                                        triggerIndex = j;
                                        break;
                                    }
                                }
                            }

                            for (int i = 0; i < 256; i++)
                            {
                                int idx = triggerIndex + i;
                                float raw = idx < size ? mono[idx] : 0f;
                                float prev = _waveform[i];
                                float tau = Math.Abs(raw) > Math.Abs(prev) ? 0.015f : 0.030f;
                                float coeff = 1.0f - (float)Math.Exp(-dt / tau);
                                _waveform[i] = prev + (raw - prev) * coeff;
                            }
                        }
                    }
                }

                if (!got && _settings.AudioSource != AudioSourceMode.DirectPcm)
                {
                    float[] mags;
                    int count;
                    if (_tap.ReadApiFft(_api.NowPlaying_GetSpectrumData, out mags, out count))
                    {
                        FoobarAnalyzer.BandsFromMagnitudes(mags, count, 44100, BandCount, _db, _settings.MinFreqHz, _settings.MaxFreqHz);
                        rate = 44100;
                        window = count * 2;
                        got = true;
                        sourceText = "MusicBee spectrum API";

                        // Synthesize smooth wave from bands when in API FFT mode
                        for (int i = 0; i < 256; i++)
                        {
                            float angle = (float)(i * Math.PI * 2.0 / 256.0);
                            float synth = (float)(Math.Sin(angle * 2.0) * 0.4 + Math.Sin(angle * 5.0) * 0.2);
                            _waveform[i] = synth;
                            _waveformL[i] = synth;
                            _waveformR[i] = synth;
                        }
                    }
                }

                if (!got)
                {
                    PublishSilent();
                    _status = _tap.Direct ? "direct tap returned no data" : "spectrum API unavailable";
                    return;
                }

                bool isStereo = (_settings.StereoMode != StereoMode.Mono);
                double tilt = _settings.TrebleTiltDb;
                double dbRange = Math.Max(20.0, (double)_settings.DbRange);

                for (int i = 0; i < BandCount; i++)
                {
                    _prevLevels[i] = _levels[i];
                    double db = _db[i];
                    double tiltDb = (i / (double)BandCount) * tilt;
                    _levels[i] = (float)Math.Max(0.0, Math.Min(1.0, (db + tiltDb + dbRange) / dbRange));
                    if (isStereo && usedDirect)
                    {
                        double dbL = _dbL[i];
                        double dbR = _dbR[i];
                        _levelsL[i] = (float)Math.Max(0.0, Math.Min(1.0, (dbL + tiltDb + dbRange) / dbRange));
                        _levelsR[i] = (float)Math.Max(0.0, Math.Min(1.0, (dbR + tiltDb + dbRange) / dbRange));
                    }
                    else
                    {
                        _levelsL[i] = _levels[i];
                        _levelsR[i] = _levels[i];
                    }
                }

                float sub = Group(_subLo, _subHi);
                float bass = Group(_bassLo, _bassHi);
                float lowMid = Group(_lowMidLo, _lowMidHi);
                float mid = Group(_midLo, _midHi);
                float highMid = Group(_highMidLo, _highMidHi);
                float high = Group(_highLo, _highHi);
                float air = Group(_airLo, _airHi);
                float peak = Peak();

                sub = Adaptive(ref _maxSub, sub);
                bass = Adaptive(ref _maxBass, bass);
                lowMid = Adaptive(ref _maxLowMid, lowMid);
                mid = Adaptive(ref _maxMid, mid);
                highMid = Adaptive(ref _maxHighMid, highMid);
                high = Adaptive(ref _maxHigh, high);
                air = Adaptive(ref _maxAir, air);

                float sens = _settings.Sensitivity;
                sub = Clamp01(Boost(sub) * sens);
                bass = Clamp01(Boost(bass) * sens);
                lowMid = Clamp01(Boost(lowMid) * sens);
                mid = Clamp01(Boost(mid) * sens);
                highMid = Clamp01(Boost(highMid) * sens);
                high = Clamp01(Boost(high) * sens);
                air = Clamp01(Boost(air) * sens);

                float rms = _tap.Direct && _mono != null
                    ? ComputeRms()
                    : Clamp01(0.45f * bass + 0.35f * mid + 0.20f * highMid);
                rms = Clamp01(Boost(Adaptive(ref _maxRms, rms)));

                float fluxLow = Flux(_subLo, _midHi);
                float fluxHigh = Flux(_highLo, _airHi);
                UpdateBeat(fluxLow, dt);

                float lowNow = (sub + bass) * 0.5f;
                float kick = Math.Max(0f, lowNow - _prevLow) * 4.0f;
                _prevLow = lowNow;
                if (kick > 0.22f)
                    _envBeat = Math.Max(_envBeat, Clamp01(kick));

                _envSub = Follow(_envSub, sub, dt, 0.010f, 0.15f);
                _envBass = Follow(_envBass, bass, dt, 0.010f, 0.15f);
                _envLowMid = Follow(_envLowMid, lowMid, dt, 0.012f, 0.16f);
                _envMid = Follow(_envMid, mid, dt, 0.012f, 0.16f);
                _envHighMid = Follow(_envHighMid, highMid, dt, 0.010f, 0.14f);
                _envHigh = Follow(_envHigh, high, dt, 0.010f, 0.12f);
                _envAir = Follow(_envAir, air, dt, 0.010f, 0.12f);
                _envRms = Follow(_envRms, rms, dt, 0.015f, 0.18f);
                _envTransient = Follow(_envTransient, Clamp01(fluxHigh * 2.0f), dt, 0.008f, 0.12f);
                _lowPulse = Follow(_lowPulse, Math.Max(sub, bass), dt, 0.010f, 0.14f);
                _envBeat = Math.Max(_envBeat * (float)Math.Exp(-dt * 6.0), 0f);

                float centroid = ComputeCentroid(rate);

                lock (_gate)
                {
                    Array.Copy(_levels, _publishedLevels, BandCount);
                    Array.Copy(_levelsL, _publishedLevelsL, BandCount);
                    Array.Copy(_levelsR, _publishedLevelsR, BandCount);
                    Array.Copy(_waveform, _publishedWaveform, 256);
                    Array.Copy(_waveformL, _publishedWaveformL, 256);
                    Array.Copy(_waveformR, _publishedWaveformR, 256);

                    _snapshot.Sub = _envSub;
                    _snapshot.Bass = _envBass;
                    _snapshot.LowMid = _envLowMid;
                    _snapshot.Mid = _envMid;
                    _snapshot.HighMid = _envHighMid;
                    _snapshot.High = _envHigh;
                    _snapshot.Air = _envAir;
                    _snapshot.Rms = _envRms;
                    _snapshot.Peak = peak;
                    _snapshot.Transient = _envTransient;
                    _snapshot.Beat = _envBeat;
                    _snapshot.Centroid = centroid;
                    _snapshot.LowPulse = _lowPulse;
                    _snapshot.StereoWidth = width;
                    _snapshot.PhaseCorrelation = corr;
                    _snapshot.SampleRate = rate;
                    _snapshot.WindowSize = window;
                    _snapshot.ValidBands = BandCount;
                    _snapshot.Source = usedDirect ? AudioSourceKind.DirectPcm : AudioSourceKind.ApiFft;
                    _snapshot.Waveform = _publishedWaveform;
                    _snapshot.WaveformL = _publishedWaveformL;
                    _snapshot.WaveformR = _publishedWaveformR;

                    _publishedTimestamp = Stopwatch.GetTimestamp();
                    _sequence++;
                    _status = sourceText;
                }
            }
            catch (Exception ex)
            {
                lock (_gate) _status = "audio error: " + ex.GetType().Name;
            }
        }

        private void PublishPreview(float dt)
        {
            _previewTime += dt;
            float t = (float)_previewTime;
            float beat = (float)Math.Pow(Math.Max(0.0, Math.Sin(t * 2.1)), 10.0);

            for (int i = 0; i < 256; i++)
            {
                float x = (float)(i * Math.PI * 2.0 / 256.0);
                _waveform[i] = (float)(Math.Sin(x * 3.0 + t * 4.0) * 0.5 + Math.Sin(x * 7.0 - t * 2.0) * 0.25);
            }

            for (int i = 0; i < BandCount; i++)
            {
                float x = i / (float)BandCount;
                _levels[i] = 0.25f + 0.55f * (0.5f + 0.5f * (float)Math.Sin(t * 1.7 + x * 12.0f));
            }

            lock (_gate)
            {
                Array.Copy(_levels, _publishedLevels, BandCount);
                Array.Copy(_waveform, _publishedWaveform, 256);

                _snapshot.Sub = 0.50f + 0.35f * (float)Math.Sin(t * 1.25);
                _snapshot.Bass = 0.55f + 0.35f * (float)Math.Sin(t * 0.85 + 1.0);
                _snapshot.LowMid = 0.45f + 0.30f * (float)Math.Sin(t * 0.65 + 2.0);
                _snapshot.Mid = 0.40f + 0.30f * (float)Math.Sin(t * 1.10 + 0.5);
                _snapshot.HighMid = 0.35f + 0.30f * (float)Math.Sin(t * 1.70 + 2.4);
                _snapshot.High = 0.30f + 0.25f * (float)Math.Sin(t * 2.30 + 1.7);
                _snapshot.Air = 0.25f + 0.25f * (float)Math.Sin(t * 3.10 + 0.2);
                _snapshot.Rms = 0.55f + 0.25f * (float)Math.Sin(t * 0.9);
                _snapshot.Peak = 0.75f;
                _snapshot.Transient = beat * 0.9f;
                _snapshot.Beat = beat;
                _snapshot.Centroid = 0.45f + 0.2f * (float)Math.Sin(t * 0.3);
                _snapshot.LowPulse = 0.45f + 0.45f * (float)Math.Sin(t * 2.1);
                _snapshot.SampleRate = 44100;
                _snapshot.WindowSize = 8192;
                _snapshot.ValidBands = BandCount;
                _snapshot.Source = AudioSourceKind.DirectPcm;
                _snapshot.Waveform = _publishedWaveform;

                _publishedTimestamp = Stopwatch.GetTimestamp();
                _sequence++;
                _status = "preview synth";
            }
        }

        private void PublishSilent()
        {
            lock (_gate)
            {
                Array.Clear(_publishedLevels, 0, BandCount);
                Array.Clear(_publishedWaveform, 0, 256);
                _snapshot = default(AudioSnapshot);
                _snapshot.Source = AudioSourceKind.Silent;
                _snapshot.Waveform = _publishedWaveform;
                _publishedTimestamp = Stopwatch.GetTimestamp();
                _sequence++;
            }
        }
    }
}
