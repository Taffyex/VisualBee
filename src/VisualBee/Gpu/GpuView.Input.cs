using System.Globalization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal abstract partial class GpuView
{
	internal void CaptureFrame(Action<Bitmap> done)
	{
		_capture = done;
		_dirty = true;
	}

	internal void CaptureLayers(Action<Bitmap[]> done)
	{
		_captureLayers = done;
		_dirty = true;
	}

	private Bitmap Layer(GpuTexture layer, int width, int height)
	{
		if (layer == null)
		{
			return null;
		}
		using GpuTexture gpuTexture = _gpu.CreateTexture(width, height, 87, renderTarget: true, IntPtr.Zero, 0);
		_canvas.Begin(gpuTexture, 0f);
		_canvas.Clear(new Vec4(0f, 0f, 0f, 1f));
		_canvas.Blend = BlendMode.Replace;
		_canvas.Quad(layer, TexQuad.Make(0f, 0f, width, height, 0f, 0f, 1f, 1f, new Vec4(1f, 1f, 1f, 1f), mask: false));
		_canvas.Flush();
		_canvas.Blend = BlendMode.Over;
		return _gpu.ReadBack(gpuTexture.Texture, width, height);
	}

	protected virtual void DrawBackground(ref Frame f)
	{
	}

	protected abstract void DrawContent(ref Frame f);

	protected GpuTexture Cover(ref Frame f)
	{
		if (_backdrop == null)
		{
			return null;
		}
		return _backdrop.Cover(f.Gpu, Host.Art);
	}

	protected virtual void ReleaseResources()
	{
	}

	protected virtual void AddMenuItems(Menu.MenuItemCollection items)
	{
	}

	private void DrawOverlays(ref Frame f, double now)
	{
		string text = "";
		if (f.Look.ShowFps)
		{
			text = _fpsText;
		}
		if (now < _hudUntil)
		{
			text = ((text.Length > 0) ? (_hud + "\n" + text) : _hud);
		}
		if (text.Length == 0)
		{
			return;
		}
		if (text != _overlayText || _overlay == null || f.SizeChanged)
		{
			if (_overlay == null)
			{
				_overlay = new TextSheet();
			}
			if (_overlayFont == IntPtr.Zero)
			{
				_overlayFont = VuFont.CreateLabel(1.0, bold: true);
			}
			_overlay.Clear();
			int num = (int)(8f * f.Scale);
			_overlay.Add(text, new Gdi.RECT(num, num, Math.Min(f.Width - num, num + (int)(320f * f.Scale)), num + (int)(48f * f.Scale)), 2048u);
			_overlay.Build(_gpu, _overlayFont);
			_overlayText = text;
		}
		Canvas canvas = f.Canvas;
		canvas.Begin(_gpu.BackBufferView, f.Width, f.Height, f.Time);
		canvas.Blend = BlendMode.Over;
		canvas.Rect(RectShape.Solid(4f * f.Scale, 4f * f.Scale, Math.Min((float)f.Width - 4f * f.Scale, 220f * f.Scale), (float)((text.IndexOf('\n') >= 0) ? 50 : 30) * f.Scale, new Vec4(0f, 0f, 0f, 0.55f)).Rounded(5f * f.Scale));
		_overlay.Draw(canvas, new Vec4(1f, 1f, 1f, 0.92f));
	}

	private void CountFrame(double now, double cpuMs)
	{
		_fpsFrames++;
		_fpsCpu += cpuMs;
		if (!(now - _fpsStart < 0.5))
		{
			double num = (double)_fpsFrames / (now - _fpsStart);
			_fpsText = num.ToString("0", CultureInfo.InvariantCulture) + " fps   " + (_fpsCpu / (double)_fpsFrames).ToString("0.00", CultureInfo.InvariantCulture) + " ms" + ((_gpu != null && _gpu.Warp) ? "   (software)" : "");
			_fpsFrames = 0;
			_fpsCpu = 0.0;
			_fpsStart = now;
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		if (!_hasGpu)
		{
			using SolidBrush brush = new SolidBrush(Color.FromArgb(Host.Store.Current.Colours[0] | -16777216));
			e.Graphics.FillRectangle(brush, ClientRectangle);
		}
		_dirty = true;
	}

	protected override void OnResize(EventArgs e)
	{
		base.OnResize(e);
		_dirty = true;
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
		if (!_hasGpu)
		{
			base.OnPaintBackground(e);
		}
	}

	protected override void WndProc(ref Message m)
	{
		if (m.Msg == 0x0014 && _hasGpu)
		{
			m.Result = (IntPtr)1;
			return;
		}
		if (m.Msg == 123)
		{
			long num = m.LParam.ToInt64();
			Point at = ((num != -1) ? PointToClient(new Point((short)(num & 0xFFFF), (short)((num >> 16) & 0xFFFF))) : new Point(0, 0));
			ShowMenu(at);
		}
		else
		{
			base.WndProc(ref m);
		}
	}

	protected override void OnMouseDoubleClick(MouseEventArgs e)
	{
		base.OnMouseDoubleClick(e);
		if (e.Button == MouseButtons.Left)
		{
			Host.ToggleFullScreen(this);
		}
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (!Focused)
		{
			Focus();
		}
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		base.OnMouseWheel(e);
		if (e.Delta != 0 && (Control.ModifierKeys & Keys.Control) == Keys.Control)
		{
			int num = (((Control.ModifierKeys & Keys.Shift) == Keys.Shift) ? 1 : 5);
			int num2 = Host.NudgeDelay((e.Delta > 0) ? num : (-num));
			lock (_sync)
			{
				_hud = num2 + " ms sync delay";
				_hudUntil = _clock.Elapsed.TotalSeconds + 2.0;
			}
			_dirty = true;
		}
	}

	internal void ShowNotification(string text, double seconds = 3.0)
	{
		lock (_sync)
		{
			_hud = text;
			_hudUntil = _clock.Elapsed.TotalSeconds + seconds;
		}
		_dirty = true;
	}

	protected override bool IsInputKey(Keys keyData)
	{
		if (keyData == Keys.Escape || keyData == Keys.F11)
		{
			return true;
		}
		return base.IsInputKey(keyData);
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (FullScreen && (e.KeyCode == Keys.Escape || e.KeyCode == Keys.F11))
		{
			Host.ToggleFullScreen(this);
			e.Handled = true;
		}
	}

	private void ShowMenu(Point at)
	{
		ContextMenu menu = _menu;
		menu.MenuItems.Clear();
		menu.MenuItems.Add(new MenuItem("&Settings…", (object s, EventArgs a) =>
		{
			Host.OpenSettings(this);
		}));
		MenuItem menuItem = new MenuItem(FullScreen ? "Leave &full screen" : "&Full screen", (object s, EventArgs a) =>
		{
			Host.ToggleFullScreen(this);
		});
		menuItem.DefaultItem = true;
		menu.MenuItems.Add(menuItem);
		menu.MenuItems.Add("-");
		MenuItem menuItem2 = new MenuItem("&Presets");
		LookStore store = Host.Store;
		foreach (Preset item in store.BuiltIn)
		{
			menuItem2.MenuItems.Add(PresetItem(item));
		}
		List<Preset> list = store.UserPresets();
		if (list.Count > 0)
		{
			menuItem2.MenuItems.Add("-");
			foreach (Preset item2 in list)
			{
				menuItem2.MenuItems.Add(PresetItem(item2));
			}
		}
		menu.MenuItems.Add(menuItem2);
		MenuItem menuItem3 = new MenuItem("Colour &themes");
		foreach (Theme theme2 in store.Themes)
		{
			Theme theme = theme2;
			menuItem3.MenuItems.Add(new MenuItem(theme2.Name, (object s, EventArgs a) =>
			{
				store.Apply(theme);
			}));
		}
		menu.MenuItems.Add(menuItem3);
		AddMenuItems(menu.MenuItems);
		menu.Show(this, at);
	}

	private MenuItem PresetItem(Preset p)
	{
		Preset preset = p;
		return new MenuItem(p.Name, (object s, EventArgs a) =>
		{
			Host.Store.Apply(preset);
		})
		{
			RadioCheck = true,
			Checked = (Host.Store.PresetName == p.Name)
		};
	}

	protected MenuItem ToggleItem(string text, string key)
	{
		Setting s = Host.Store.Schema.Find(key);
		return new MenuItem(text, (object o, EventArgs a) =>
		{
			Host.Store.Set(s, !(bool)s.Get(Host.Store.Current));
		})
		{
			Checked = (bool)s.Get(Host.Store.Current)
		};
	}

	protected MenuItem ChoiceItem(string text, string key)
	{
		Setting s = Host.Store.Schema.Find(key);
		MenuItem menuItem = new MenuItem(text);
		int num = (int)s.Get(Host.Store.Current);
		for (int i = 0; i < s.Choices.Length; i++)
		{
			int index = i;
			MenuItem menuItem2 = new MenuItem(s.Choices[i], (object o, EventArgs a) =>
			{
				Host.Store.Set(s, index);
			});
			menuItem2.RadioCheck = true;
			menuItem2.Checked = i == num;
			menuItem.MenuItems.Add(menuItem2);
		}
		return menuItem;
	}

	private void ThreadEnding()
	{
		lock (_sync)
		{
			ReleaseGpu();
		}
	}

	private void ReleaseGpu()
	{
		ReleaseResources();
		if (_overlay != null)
		{
			_overlay.Dispose();
		}
		_overlay = null;
		_overlayText = null;
		if (_overlayFont != IntPtr.Zero)
		{
			Gdi.DeleteObject(_overlayFont);
		}
		_overlayFont = IntPtr.Zero;
		if (_backdrop != null)
		{
			_backdrop.Dispose();
		}
		_backdrop = null;
		if (_post != null)
		{
			_post.Dispose();
		}
		_post = null;
		if (_canvas != null)
		{
			_canvas.Dispose();
		}
		_canvas = null;
		if (_gpu != null)
		{
			_gpu.Dispose();
		}
		_gpu = null;
		_gpuHwnd = IntPtr.Zero;
		_drawnLook = null;
	}

}
