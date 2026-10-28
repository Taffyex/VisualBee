using System.Collections.Generic;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed partial class VisualiserHost
{
	public void ToggleFullScreen(GpuView from)
	{
		if (_fullScreenForm != null)
		{
			CloseFullScreen();
			return;
		}
		Screen screen = (from != null) ? Screen.FromControl(from) : Screen.PrimaryScreen;
		Form form = new Form
		{
			FormBorderStyle = FormBorderStyle.None,
			StartPosition = FormStartPosition.Manual,
			Bounds = screen.Bounds,
			ShowInTaskbar = true,
			TopMost = false,
			KeyPreview = true,
			BackColor = Color.FromArgb(Store.Current.Colours[0] | -16777216),
			Text = Store.Title
		};
		form.KeyDown += (object s, KeyEventArgs a) =>
		{
			if (a.KeyCode == Keys.Escape || a.KeyCode == Keys.F11)
			{
				CloseFullScreen();
				a.Handled = true;
			}
		};
		GpuView view = _makeView(this, arg2: true);
		view.Dock = DockStyle.Fill;
		form.Controls.Add(view);
		form.FormClosed += (object s, FormClosedEventArgs a) =>
		{
			if (_fullScreenForm == form)
			{
				_fullScreenForm = null;
				_fullScreenView = null;
			}
			view.Dispose();
		};
		_fullScreenForm = form;
		_fullScreenView = view;
		IWin32Window owner = from?.FindForm();
		if (owner != null) form.Show(owner); else form.Show();
		view.Start();
		view.Focus();
		view.ShowNotification("Press Esc, F11 or double-click to exit", 3.0);
	}

	public void OpenSettings(Control near)
	{
		if (_settings != null && !_settings.IsDisposed)
		{
			if (_settings.WindowState == FormWindowState.Minimized)
			{
				_settings.WindowState = FormWindowState.Normal;
			}
			_settings.Activate();
			return;
		}
		SettingsWindow w = new SettingsWindow(Store, StatusText);
		Form form = near?.FindForm();
		if (form == null)
		{
			form = MainForm();
		}
		if (form != null && form.TopMost)
		{
			w.TopMost = true;
		}
		Rectangle rectangle = ((form != null) ? Screen.FromControl(form).WorkingArea : Screen.PrimaryScreen.WorkingArea);
		Point point = ((form != null && form.Visible) ? new Point(form.Left + form.Width / 2, form.Top + form.Height / 2) : new Point(rectangle.Left + rectangle.Width / 2, rectangle.Top + rectangle.Height / 2));
		w.Location = new Point(Math.Max(rectangle.Left, Math.Min(point.X - w.Width / 2, rectangle.Right - w.Width)), Math.Max(rectangle.Top, Math.Min(point.Y - w.Height / 2, rectangle.Bottom - w.Height)));
		w.FormClosed += (object s, FormClosedEventArgs a) =>
		{
			if (_settings == w)
			{
				_settings = null;
			}
		};
		_settings = w;
		if (form != null)
		{
			w.Show(form);
		}
		else
		{
			w.Show();
		}
	}

	private Form MainForm()
	{
		try
		{
			if (_api.MB_GetWindowHandle != null)
			{
				Control control = Control.FromHandle(_api.MB_GetWindowHandle());
				if (control != null)
				{
					return control.FindForm();
				}
			}
		}
		catch (Exception)
		{
		}
		if (Application.OpenForms.Count <= 0)
		{
			return null;
		}
		return Application.OpenForms[0];
	}

	public bool Configure(IntPtr panelHandle)
	{
		if (panelHandle == IntPtr.Zero)
		{
			return false;
		}
		Control host = Control.FromHandle(panelHandle);
		if (host == null)
		{
			return false;
		}
		host.Controls.Clear();
		Label label = new Label
		{
			Text = "Preset",
			AutoSize = true,
			Left = 8,
			Top = 12
		};
		ComboBox looks = new ComboBox
		{
			DropDownStyle = ComboBoxStyle.DropDownList,
			Left = 50,
			Top = 8,
			Width = 200
		};
		List<Preset> all = new List<Preset>(Store.BuiltIn);
		all.AddRange(Store.UserPresets());
		foreach (Preset item in all)
		{
			looks.Items.Add(item.Name);
		}
		int num = all.FindIndex((Preset p) => p.Name == Store.PresetName);
		if (num >= 0)
		{
			looks.SelectedIndex = num;
		}
		looks.SelectedIndexChanged += (object s, EventArgs a) =>
		{
			if (looks.SelectedIndex >= 0 && all[looks.SelectedIndex].Name != Store.PresetName)
			{
				Store.Apply(all[looks.SelectedIndex]);
			}
		};
		Button button = new Button
		{
			Text = "Open VisualBee Studio…",
			Left = 260,
			Top = 7,
			Width = 170,
			Height = 24
		};
		button.Click += (object s, EventArgs a) =>
		{
			OpenSettings(host);
		};
		Label label2 = new Label
		{
			Text = "Colours, layouts, effects and how the audio is analysed are in the settings window (also on the panel's right-click menu).",
			AutoSize = true,
			Left = 8,
			Top = 44
		};
		Label status = new Label
		{
			AutoSize = true,
			Left = 8,
			Top = 84,
			Text = StatusText()
		};
		System.Windows.Forms.Timer refresh = new System.Windows.Forms.Timer
		{
			Interval = 500
		};
		refresh.Tick += (object s, EventArgs a) =>
		{
			status.Text = StatusText();
		};
		status.Disposed += (object s, EventArgs a) =>
		{
			refresh.Dispose();
		};
		refresh.Start();
		host.Controls.AddRange(new Control[5] { label, looks, button, label2, status });
		PanelLayout.Fit(host);
		return true;
	}
}
