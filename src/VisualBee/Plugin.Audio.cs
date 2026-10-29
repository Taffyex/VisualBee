using System;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        internal struct WindowKey
        {
            public int Segment;
            public long Start;
            public long AudioStart;
            public int Size;
            public int Rate;
            public int Mode;

            public bool SameAs(WindowKey o)
            {
                if (Segment == o.Segment && Start == o.Start && AudioStart == o.AudioStart && Size == o.Size && Rate == o.Rate)
                {
                    return Mode == o.Mode;
                }
                return false;
            }
        }

        internal int Rate
        {
            get
            {
                AudioTap tap = _host?.Tap;
                if (tap == null) return 44100;
                lock (tap)
                {
                    return (tap.HistoryRate > 0) ? tap.HistoryRate : 44100;
                }
            }
        }

        internal bool Direct => _host != null && _host.Tap.Direct;

        internal bool ReadWindow(int engine, int fftChoice, int channels, bool stereo, int delayMs, ref WindowKey last, ref float[] a, ref float[] b, out bool fresh)
        {
            fresh = false;
            if (_host == null) return false;
            AudioTap tap = _host.Tap;
            lock (tap)
            {
                if (!tap.BindTried && _api.NowPlaying_GetSpectrumData != null)
                {
                    tap.Bind(_api.NowPlaying_GetSpectrumData);
                }
                if (!tap.Direct || !tap.Begin())
                {
                    return false;
                }
                int rate = tap.Rate;
                int num = ((engine == 0) ? GaussianAnalyzer.WindowSize(rate) : SpectrumAnalyzer.WindowSize(fftChoice, rate));
                long num2 = (long)delayMs * (long)rate / 1000;
                if (!tap.Read(num + (int)num2, num))
                {
                    return false;
                }
                long num3 = tap.Heard - num2 - num / 2;
                long num4 = num3 + num - tap.AvailableEnd;
                if (num4 > 0)
                {
                    num3 -= num4;
                }
                WindowKey windowKey = new WindowKey
                {
                    Segment = tap.Segment,
                    Start = num3,
                    AudioStart = Math.Max(num3, tap.ViewStart),
                    Size = num,
                    Rate = rate,
                    Mode = (stereo ? 100 : channels)
                };
                if (windowKey.SameAs(last))
                {
                    return true;
                }
                last = windowKey;
                fresh = true;
                if (a == null || a.Length < num)
                {
                    a = new float[num];
                }
                if (stereo && (b == null || b.Length < num))
                {
                    b = new float[num];
                }
                int historyChannels = tap.HistoryChannels;
                if (!stereo && channels == 0)
                {
                    tap.CopyMono(num3, num, a);
                    return true;
                }
                if (_frames.Length < num * historyChannels)
                {
                    _frames = new float[num * historyChannels];
                }
                tap.CopyFrames(num3, num, _frames);
                int num5 = ((historyChannels > 1) ? 1 : 0);
                int num6 = 0;
                int num7 = 0;
                while (num6 < num)
                {
                    float num8 = _frames[num7];
                    float num9 = _frames[num7 + num5];
                    if (stereo)
                    {
                        a[num6] = num8;
                        b[num6] = num9;
                    }
                    else
                    {
                        switch (channels)
                        {
                            case 1:
                                a[num6] = num8;
                                break;
                            case 2:
                                a[num6] = num9;
                                break;
                            case 4:
                                a[num6] = (num8 - num9) * 0.5f;
                                break;
                            default:
                                a[num6] = (num8 + num9) * 0.5f;
                                break;
                        }
                    }
                    num6++;
                    num7 += historyChannels;
                }
                return true;
            }
        }

        internal bool ReadApiFft(out float[] magnitudes)
        {
            magnitudes = null;
            if (_host == null) return false;
            AudioTap tap = _host.Tap;
            lock (tap)
            {
                return tap.ReadApiFft(_api.NowPlaying_GetSpectrumData, out magnitudes);
            }
        }

        internal void StopAudio()
        {
            if (_host == null) return;
            AudioTap tap = _host.Tap;
            lock (tap)
            {
                tap.Stop();
            }
        }
    }
}
