using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal static class NativeScroll
{
	[DllImport("user32.dll")]
	private static extern bool ShowScrollBar(IntPtr hwnd, int bar, bool show);

	public static void Refresh(ListView list)
	{
		if (list.IsHandleCreated)
		{
			ShowScrollBar(list.Handle, 0, show: false);
		}
	}
}
