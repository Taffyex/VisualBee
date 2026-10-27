using System.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed partial class SettingsWindow : Form
{
	private const string PresetsPage = "Presets";

	private readonly LookStore _store;

	private readonly Func<string> _audioStatus;

	private readonly PageList _pages = new PageList();

	private readonly ScrollPane _content = new ScrollPane();

	private readonly InputBox _search = new InputBox();

	private readonly Label _title = new Label();

	private readonly Label _subtitle = new Label();

	private readonly Label _hintTitle = new Label();

	private readonly Label _hintText = new Label();

	private readonly FlatButton _resetPage = new FlatButton("Reset page", "\ue7a7");

	private readonly FlatButton _resetAll = new FlatButton("Reset all");

	public bool _hideInactive = true;

	private readonly FlatButton _hideBtn = new FlatButton("Hide unused", "\ue71c");

	private readonly FlatButton _quickSave = new FlatButton("Save Look…", "\ue74e");

	private readonly FlatButton _close = new FlatButton("Close")
	{
		Primary = true
	};

	private readonly ToolTip _tips = new ToolTip();

	private readonly List<SettingRow> _rows = new List<SettingRow>();

	private readonly List<SectionHeader> _headers = new List<SectionHeader>();

	private readonly Timer _statusTimer = new Timer
	{
		Interval = 500
	};

	private string _page;

	private Label _audio;

	public LookStore Store => _store;

	public SettingsWindow(LookStore store, Func<string> audioStatus)
	{
		_store = store;
		_audioStatus = audioStatus;
		Text = store.Title + " settings";
		Font = Ui.Body;
		BackColor = Ui.Back;
		ForeColor = Ui.Text;
		AutoScaleMode = AutoScaleMode.None;
		StartPosition = FormStartPosition.Manual;
		ShowInTaskbar = false;
		KeyPreview = true;
		MinimumSize = new Size(Ui.S(720), Ui.S(520));
		Size = new Size(Ui.S(940), Ui.S(700));
		Icon = null;
		ShowIcon = false;
		SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		Panel header = new Panel
		{
			Dock = DockStyle.Top,
			Height = Ui.S(64),
			BackColor = Ui.Back
		};
		_title.Text = store.Title;
		_title.Font = Ui.Heading;
		_title.ForeColor = Ui.Text;
		_title.AutoSize = true;
		_title.Location = new Point(Ui.S(20), Ui.S(10));
		_subtitle.Font = Ui.Small;
		_subtitle.ForeColor = Ui.Dim;
		_subtitle.AutoSize = true;
		_subtitle.Location = new Point(Ui.S(22), Ui.S(38));
		_search.Size = new Size(Ui.S(240), Ui.S(30));
		_search.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		Ui.CueBanner(_search.Box, "Search settings  (Ctrl+F)");
		_search.Box.TextChanged += (object s, EventArgs a) =>
		{
			ShowPage(_page);
		};
		_hideBtn.Click += (object s, EventArgs a) => { _hideInactive = !_hideInactive; _hideBtn.Text = _hideInactive ? "Hide unused" : "Show all"; StoreChanged(this, EventArgs.Empty); };
		_quickSave.Click += (object s, EventArgs a) => SaveLook();
		_tips.SetToolTip(_hideBtn, "Toggles hiding settings that do not apply to current configuration");
		_tips.SetToolTip(_quickSave, "Save current configuration as a preset");
		header.Controls.AddRange(new Control[5] { _title, _subtitle, _quickSave, _hideBtn, _search });
		header.Resize += (object s, EventArgs a) =>
		{
			_search.Location = new Point(header.Width - _search.Width - Ui.S(20), Ui.S(17));
			_hideBtn.Location = new Point(_search.Left - _hideBtn.Width - Ui.S(8), Ui.S(17));
			_quickSave.Location = new Point(_hideBtn.Left - _quickSave.Width - Ui.S(8), Ui.S(17));
		};
		Panel panel = new Panel
		{
			Dock = DockStyle.Left,
			Width = Ui.S(184),
			BackColor = Ui.Side
		};
		_pages.Dock = DockStyle.Fill;
		_pages.Pages.Add("Presets");
		_pages.Pages.AddRange(store.Schema.Pages());
		_pages.Changed += (object s, EventArgs a) =>
		{
			_search.Box.Text = "";
			ShowPage(_pages.Pages[_pages.Selected]);
		};
		_audio = new Label
		{
			Dock = DockStyle.Bottom,
			Height = Ui.S(70),
			ForeColor = Ui.Faint,
			Font = Ui.Small,
			Padding = new Padding(Ui.S(14), 0, Ui.S(8), Ui.S(10))
		};
		panel.Controls.Add(_pages);
		panel.Controls.Add(_audio);
		Panel footer = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = Ui.S(92),
			BackColor = Ui.Side
		};
		_hintTitle.Font = Ui.Bold;
		_hintTitle.ForeColor = Ui.Text;
		_hintTitle.AutoSize = false;
		_hintText.Font = Ui.Body;
		_hintText.ForeColor = Ui.Dim;
		_hintText.AutoSize = false;
		footer.Controls.AddRange(new Control[5] { _hintTitle, _hintText, _resetPage, _resetAll, _close });
		footer.Resize += (object s, EventArgs a) =>
		{
			LayoutFooter(footer);
		};
		_resetPage.Click += (object s, EventArgs a) =>
		{
			ResetPage();
		};
		_resetAll.Click += (object s, EventArgs a) =>
		{
			ResetAll();
		};
		_close.Click += (object s, EventArgs a) =>
		{
			Close();
		};
		_tips.SetToolTip(_resetPage, "Puts every setting on this page back to its default");
		_tips.SetToolTip(_resetAll, "Puts every setting back to its default (your sync delay and frame rate stay)");
		_content.Dock = DockStyle.Fill;
		_content.BackColor = Ui.Back;
		_content.MouseLeave += (object s, EventArgs a) =>
		{
			Hint(null);
		};
		_content.Resize += (object s, EventArgs a) =>
		{
			LayoutContent();
		};
		Controls.Add(_content);
		Controls.Add(footer);
		Controls.Add(panel);
		Controls.Add(header);
		_store.Changed += StoreChanged;
		_statusTimer.Tick += (object s, EventArgs a) =>
		{
			ShowAudio();
		};
		_statusTimer.Start();
		ShowAudio();
		ShowPage((_pages.Pages.Count > 1) ? _pages.Pages[1] : "Presets");
		_pages.Selected = _pages.Pages.IndexOf(_page);
		ShowStatus();
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		Palette.DarkTitle(this, Palette.From(Ui.Back, Ui.Text, Ui.Back, Ui.Text));
	}

	protected override void OnFormClosed(FormClosedEventArgs e)
	{
		_store.Changed -= StoreChanged;
		_statusTimer.Dispose();
		_tips.Dispose();
		base.OnFormClosed(e);
	}

	protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
	{
		switch (keyData)
		{
		case Keys.F | Keys.Control:
			_search.Box.Focus();
			_search.Box.SelectAll();
			return true;
		case Keys.Escape:
			if (_search.Box.Focused && _search.Box.Text.Length > 0)
			{
				_search.Box.Text = "";
			}
			else
			{
				Close();
			}
			return true;
		default:
			return base.ProcessCmdKey(ref msg, keyData);
		}
	}

	private void LayoutFooter(Panel footer)
	{
		int num = Ui.S(16);
		int num2 = footer.Width - num;
		FlatButton[] array = new FlatButton[3] { _close, _resetAll, _resetPage };
		foreach (FlatButton flatButton in array)
		{
			flatButton.Width = flatButton.Ideal();
			num2 -= flatButton.Width;
			flatButton.Location = new Point(num2, footer.Height - flatButton.Height - Ui.S(14));
			num2 -= Ui.S(8);
		}
		int num3 = footer.Width - num;
		_hintTitle.SetBounds(num, Ui.S(10), num3 - num, Ui.S(18));
		_hintText.SetBounds(num, Ui.S(29), Math.Max(Ui.S(100), _resetPage.Left - num - Ui.S(16)), footer.Height - Ui.S(33));
	}

	private void ShowAudio()
	{
		string text = ((_audioStatus != null) ? _audioStatus() : "");
		if (_audio.Text != text)
		{
			_audio.Text = text;
		}
	}

	private void StoreChanged(object sender, EventArgs e)
	{
		if (IsDisposed)
		{
			return;
		}
		if (InvokeRequired)
		{
			BeginInvoke((Action)(() =>
			{
				StoreChanged(sender, e);
			}));
			return;
		}
		Look current = _store.Current;
		bool anyVisibilityChanged = false;
		foreach (SettingRow row in _rows)
		{
			bool wasVisible = row.Visible;
			row.ShowValue(current);
			if (row.Visible != wasVisible)
			{
				anyVisibilityChanged = true;
			}
		}
		foreach (SectionHeader header in _headers)
		{
			bool wasVisible = header.Visible;
			header.ShowState(current);
			if (header.Visible != wasVisible)
			{
				anyVisibilityChanged = true;
			}
		}
		if (anyVisibilityChanged)
		{
			LayoutContent();
		}

		if (_page == "Presets")
		{
			foreach (Control control in _content.Controls)
			{
				control.Invalidate();
			}
		}
		ShowStatus();
	}

	private void ShowStatus()
	{
		Look l = _store.Current;
		string text = ((_store.PresetName.Length > 0) ? (_store.PresetName + (_store.Modified ? " (changed)" : "")) : "your own settings");
		int num = _store.Schema.All.Count((Setting s) => !s.Personal && !s.IsDefault(l));
		_subtitle.Text = "Look: " + text + "   ·   " + ((num == 0) ? "all settings at their defaults" : (num + " settings differ from the defaults"));
		_pages.Marked.Clear();
		foreach (Setting item in _store.Schema.All)
		{
			if (!item.IsDefault(l))
			{
				_pages.Marked.Add(item.Page);
			}
		}
		_pages.Invalidate();
		_resetPage.Enabled = _page != "Presets" && _search.Box.Text.Length == 0 && _store.Schema.All.Any((Setting s) => s.Page == _page && !s.IsDefault(l));
	}

	public void Hint(Setting s)
	{
		if (s == null)
		{
			_hintTitle.Text = ((_page == "Presets") ? "Presets" : _page);
			_hintText.Text = PageHint(_page);
		}
		else
		{
			_hintTitle.Text = s.Label + (s.IsDefault(_store.Current) ? "" : ("   (default: " + Describe(s, s.Default) + ")"));
			_hintText.Text = s.Hint + (s.IsEnabled(_store.Current) ? "" : "   Not used with the current settings.");
		}
	}

	private static string Describe(Setting s, object value)
	{
		switch (s.Kind)
		{
		case SettingKind.Toggle:
			if (!(bool)value)
			{
				return "off";
			}
			return "on";
		case SettingKind.Number:
			return s.Display((double)value);
		case SettingKind.Choice:
			return s.Choices[(int)value];
		default:
			return ColourMath.ToHex((int)value, s.Alpha);
		}
	}

	private static string PageHint(string page)
	{
		return page switch
		{
			"Presets" => "Click a look to use it; colour themes change only the colours. Hover any setting on the other pages to see what it does.", 
			"Studio" => "Layout arrangement, bar count, shape, transparent glass tubes, symmetry, and waveform.", 
			"Atmosphere & FX" => "Background, 3D Cyber-Grid horizon, floating particles, bloom glow, afterglow, and screen optics.", 
			"Colours" => "Every colour the visualisation uses, and how colours move.", 
			"Audio & System" => "Audio refresh rate, sensitivity gain, FFT analysis resolution, ballistics, and display frame rate.", 
			_ => "Hover a setting to see what it does. The arrow beside a changed setting puts it back to its default.", 
		};
	}

}
