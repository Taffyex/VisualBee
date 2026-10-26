using System;
using System.Runtime.InteropServices;

namespace MusicBeePlugin
{
    internal static class DisplayInfo
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hDc, int index);

        private const int VREFRESH = 116;

        public static int RefreshHz()
        {
            try
            {
                IntPtr dc = GetDC(IntPtr.Zero);
                if (dc == IntPtr.Zero) return 60;
                int hz = GetDeviceCaps(dc, VREFRESH);
                ReleaseDC(IntPtr.Zero, dc);
                if (hz >= 24 && hz <= 360) return hz;
            }
            catch { }
            return 60;
        }
    }
}
