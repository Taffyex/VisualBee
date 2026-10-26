using System;
using System.Runtime.InteropServices;

namespace MusicBeePlugin
{
    internal static class BassNative
    {
        [StructLayout(LayoutKind.Sequential)]
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

        [StructLayout(LayoutKind.Sequential)]
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

        [StructLayout(LayoutKind.Sequential)]
        internal struct BassInfo
        {
            public int flags;
            public int hwsize;
            public int hwfree;
            public int freesam;
            public int free3d;
            public int minrate;
            public int maxrate;
            public int eax;
            public int minbuf;
            public int dsver;
            public int latency;
            public int initflags;
            public int speakers;
            public int freq;
        }

        internal static class Bass
        {
            [DllImport("bass.dll", EntryPoint = "BASS_GetInfo")]
            public static extern bool GetInfo(ref BassInfo info);

            [DllImport("bass.dll", EntryPoint = "BASS_ChannelGetInfo")]
            public static extern bool ChannelGetInfo(int handle, ref BassChannelInfo info);

            [DllImport("bass.dll", EntryPoint = "BASS_ChannelBytes2Seconds")]
            public static extern double ChannelBytes2Seconds(int handle, long position);

            [DllImport("bassmix.dll", EntryPoint = "BASS_Mixer_ChannelGetData")]
            public static extern int MixerChannelGetData(int handle, [Out] float[] buffer, int length);

            [DllImport("bassmix.dll", EntryPoint = "BASS_Mixer_ChannelGetPosition")]
            public static extern long MixerChannelGetPosition(int handle, int mode);

            [DllImport("bassmix.dll", EntryPoint = "BASS_Mixer_ChannelGetMixer")]
            public static extern int MixerChannelGetMixer(int handle);
        }

        internal static class Wasapi
        {
            [DllImport("basswasapi.dll", EntryPoint = "BASS_WASAPI_IsStarted")]
            public static extern bool IsStarted();

            [DllImport("basswasapi.dll", EntryPoint = "BASS_WASAPI_GetData")]
            public static extern int GetData(IntPtr buffer, int length);

            [DllImport("basswasapi.dll", EntryPoint = "BASS_WASAPI_GetInfo")]
            public static extern bool GetInfo(out WasapiInfo info);
        }

        internal static class Asio
        {
            [DllImport("bassasio.dll", EntryPoint = "BASS_ASIO_IsStarted")]
            public static extern bool IsStarted();

            [DllImport("bassasio.dll", EntryPoint = "BASS_ASIO_GetLatency")]
            public static extern int GetLatency(bool input);

            [DllImport("bassasio.dll", EntryPoint = "BASS_ASIO_GetRate")]
            public static extern double GetRate();
        }

        internal static double QueryOutputQueueSeconds(ref bool noWasapi, ref bool noAsio)
        {
            if (!noWasapi)
            {
                try
                {
                    if (Wasapi.IsStarted())
                    {
                        int data = Wasapi.GetData(IntPtr.Zero, 0);
                        WasapiInfo info;
                        if (data >= 0 && Wasapi.GetInfo(out info) && info.freq > 0 && info.chans > 0)
                            return (double)data / (info.freq * info.chans * 4);
                    }
                }
                catch (DllNotFoundException) { noWasapi = true; }
                catch (EntryPointNotFoundException) { noWasapi = true; }
            }

            if (!noAsio)
            {
                try
                {
                    if (Asio.IsStarted())
                    {
                        int latency = Asio.GetLatency(false);
                        double rate = Asio.GetRate();
                        if (latency > 0 && rate > 0.0) return latency / rate;
                    }
                }
                catch (DllNotFoundException) { noAsio = true; }
                catch (EntryPointNotFoundException) { noAsio = true; }
            }

            try
            {
                var bi = default(BassInfo);
                if (Bass.GetInfo(ref bi) && bi.latency > 0)
                    return bi.latency / 1000.0;
            }
            catch { }
            return 0.0;
        }
    }
}
