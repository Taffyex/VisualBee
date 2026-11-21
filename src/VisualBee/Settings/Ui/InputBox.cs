using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed class InputBox : Widget
{
	private sealed class StudioTextBox : TextBox
	{
		private const int WH_GETMESSAGE = 3;
		private const int PM_REMOVE = 1;
		private const int WM_NULL = 0x0000;
		private const int WM_KEYDOWN = 0x0100;

		private delegate IntPtr HookDelegate(int nCode, IntPtr wParam, IntPtr lParam);

		[StructLayout(LayoutKind.Sequential)]
		private struct NativeMsg
		{
			public IntPtr hwnd;
			public int message;
			public IntPtr wParam;
			public IntPtr lParam;
			public uint time;
			public int ptX;
			public int ptY;
		}

		[DllImport("user32.dll", SetLastError = true)]
		private static extern IntPtr SetWindowsHookExW(int idHook, HookDelegate lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll")]
		private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern bool TranslateMessage(ref NativeMsg lpMsg);

		[DllImport("user32.dll")]
		private static extern IntPtr DispatchMessageW(ref NativeMsg lpMsg);

		[DllImport("kernel32.dll")]
		private static extern uint GetCurrentThreadId();

		private static readonly HookDelegate HookCallback = MessageHookProc;

		[ThreadStatic]
		private static IntPtr _hook;

		[ThreadStatic]
		private static int _hookCount;

		[ThreadStatic]
		private static bool _inHook;

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			if (_hookCount == 0 && _hook == IntPtr.Zero)
			{
				_hook = SetWindowsHookExW(WH_GETMESSAGE, HookCallback, IntPtr.Zero, GetCurrentThreadId());
			}
			_hookCount++;
		}

		protected override void OnHandleDestroyed(EventArgs e)
		{
			if (_hookCount > 0)
			{
				_hookCount--;
			}
			if (_hookCount == 0 && _hook != IntPtr.Zero)
			{
				UnhookWindowsHookEx(_hook);
				_hook = IntPtr.Zero;
			}
			base.OnHandleDestroyed(e);
		}

		private static IntPtr MessageHookProc(int nCode, IntPtr wParam, IntPtr lParam)
		{
			IntPtr currentHook = _hook;
			if (nCode >= 0 && !_inHook && lParam != IntPtr.Zero && (((long)wParam) & PM_REMOVE) != 0)
			{
				int msg = Marshal.ReadInt32(lParam, IntPtr.Size);
				if (msg == WM_KEYDOWN)
				{
					IntPtr hwnd = Marshal.ReadIntPtr(lParam, 0);
					if (hwnd != IntPtr.Zero && Control.FromHandle(hwnd) is StudioTextBox box && !box.IsDisposed)
					{
						// Neutralize WM_KEYDOWN before MusicBee's IMessageFilter inspects it so Space and single-key hotkeys don't trigger host playback shortcuts while typing.
						NativeMsg nativeMsg = Marshal.PtrToStructure<NativeMsg>(lParam);
						Marshal.WriteInt32(lParam, IntPtr.Size, WM_NULL);
						_inHook = true;
						try
						{
							Message m = Message.Create(nativeMsg.hwnd, nativeMsg.message, nativeMsg.wParam, nativeMsg.lParam);
							if (box.PreProcessControlMessage(ref m) != PreProcessControlState.MessageProcessed)
							{
								TranslateMessage(ref nativeMsg);
								DispatchMessageW(ref nativeMsg);
							}
						}
						finally
						{
							_inHook = false;
						}
					}
				}
			}
			return CallNextHookEx(currentHook, nCode, wParam, lParam);
		}
	}

	public readonly TextBox Box = new StudioTextBox();

	private IconButton _clear;

	private bool _searchMode;

	private bool _mono;

	public bool SearchMode
	{
		get => _searchMode;
		set
		{
			if (_searchMode == value) return;
			_searchMode = value;
			if (_searchMode && _clear == null)
			{
				_clear = new IconButton(Ui.GlyphClose)
				{
					Size = new Size(Ui.S(20), Ui.S(20)),
					Visible = Box.TextLength > 0,
					Cursor = Cursors.Hand
				};
				_clear.Click += (object s, EventArgs a) =>
				{
					Box.Text = "";
					Box.Focus();
				};
				Controls.Add(_clear);
			}
			else if (!_searchMode && _clear != null)
			{
				_clear.Visible = false;
			}
			PerformLayout();
			Invalidate();
		}
	}

	public bool Mono
	{
		get => _mono;
		set
		{
			if (_mono == value) return;
			_mono = value;
			Box.Font = _mono ? Ui.Mono : Ui.Body;
			PerformLayout();
			Invalidate();
		}
	}

	public InputBox()
	{
		Box.BorderStyle = BorderStyle.None;
		Box.BackColor = Ui.Raised;
		Box.ForeColor = Ui.Text;
		Box.Font = Ui.Body;
		Controls.Add(Box);
		Height = Ui.S(28);
		Box.GotFocus += (object s, EventArgs a) => Invalidate();
		Box.LostFocus += (object s, EventArgs a) => Invalidate();
		Box.TextChanged += (object s, EventArgs a) =>
		{
			if (_searchMode && _clear != null)
			{
				_clear.Visible = Box.TextLength > 0;
				PerformLayout();
			}
		};
	}

	protected override void OnLayout(LayoutEventArgs e)
	{
		base.OnLayout(e);
		int left = _searchMode ? Ui.S(26) : Ui.S(8);
		int rightPad = (_searchMode && _clear != null && _clear.Visible) ? Ui.S(24) : Ui.S(8);
		Box.SetBounds(left, (Height - Box.PreferredHeight) / 2 + 1, Math.Max(1, Width - left - rightPad), Box.PreferredHeight);
		if (_clear != null)
		{
			_clear.Location = new Point(Width - _clear.Width - Ui.S(4), (Height - _clear.Height) / 2);
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Ui.Smooth(e.Graphics);
		RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
		Ui.Fill(e.Graphics, r, Ui.Sf(5f), Ui.Raised);
		Color border = Box.Focused ? Ui.Accent : ((Hot && Enabled) ? Ui.Specular : Ui.Border);
		Ui.Stroke(e.Graphics, r, Ui.Sf(5f), border);
		if (_searchMode)
		{
			Ui.Glyph(e.Graphics, Ui.GlyphSearch, Ui.Icons, Box.Focused ? Ui.Accent : Ui.Dim, new Rectangle(Ui.S(5), 0, Ui.S(18), Height));
		}
	}

	protected override void OnEnabledChanged(EventArgs e)
	{
		base.OnEnabledChanged(e);
		Box.ForeColor = Enabled ? Ui.Text : Ui.Faint;
	}
}
