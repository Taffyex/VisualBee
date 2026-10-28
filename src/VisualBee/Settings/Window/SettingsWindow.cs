using System.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
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
	private readonly Label _hintTitle = new Label();
	private readonly Label _hintText = new Label();
	private readonly FlatButton _resetPage = new FlatButton("Reset page", "\ue7a7");
	private readonly FlatButton _resetAll = new FlatButton("Reset all");
	public bool _hideInactive = true;
	private readonly FlatButton _hideBtn = new FlatButton("Hide unused", "\ue71c");
	private readonly FlatButton _quickSave = new FlatButton("Save Look…", "\ue74e");
	private readonly FlatButton _close = new FlatButton("Close") { Primary = true };
	private readonly ToolTip _tips = new ToolTip();
	private readonly List<SettingRow> _rows = new List<SettingRow>();
	private readonly List<RackSectionCard> _cards = new List<RackSectionCard>();
	private readonly Timer _statusTimer = new Timer { Interval = 500 };
	private string _page;
	private Label _audio;

	public LookStore Store => _store;

	public SettingsWindow(LookStore store, Func<string> audioStatus)
	{
		_store = store;
		_audioStatus = audioStatus;
		Text = "VisualBee Studio";
		Font = Ui.Body;
		BackColor = Ui.Back;
		ForeColor = Ui.Text;
		AutoScaleMode = AutoScaleMode.None;
		StartPosition = FormStartPosition.Manual;
		ShowInTaskbar = false;
		KeyPreview = true;
		MinimumSize = new Size(Ui.S(740), Ui.S(520));
		Size = new Size(Ui.S(980), Ui.S(680));
		Icon = null;
		ShowIcon = false;
		SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);

		// 1. Top Studio Deck Header (Height = 52px)
		Panel header = new Panel
		{
			Dock = DockStyle.Top,
			Height = Ui.S(52),
			BackColor = Ui.Back
		};

		_title.Text = "🐝 VISUALBEE STUDIO";
		_title.Font = Ui.Bold;
		_title.ForeColor = Ui.Accent;
		_title.AutoSize = true;
		_title.Location = new Point(Ui.S(16), Ui.S(16));

		_pages.Pages.Add("Presets");
		_pages.Pages.AddRange(store.Schema.Pages());
		_pages.Height = Ui.S(38);
		_pages.Changed += (s, a) =>
		{
			_search.Box.Text = "";
			ShowPage(_pages.Pages[_pages.Selected]);
		};

		_search.Size = new Size(Ui.S(180), Ui.S(28));
		Ui.CueBanner(_search.Box, "Search (Ctrl+F)");
		_search.Box.TextChanged += (s, a) => ShowPage(_page);

		_hideBtn.Click += (s, a) =>
		{
			_hideInactive = !_hideInactive;
			_hideBtn.Text = _hideInactive ? "Hide unused" : "Show all";
			StoreChanged(this, EventArgs.Empty);
		};
		_quickSave.Click += (s, a) => SaveLook();
		_tips.SetToolTip(_hideBtn, "Toggle hiding inactive controls for the current mode");
		_tips.SetToolTip(_quickSave, "Save current configuration as a preset");

		header.Controls.AddRange(new Control[5] { _title, _pages, _quickSave, _hideBtn, _search });
		header.Resize += (s, a) =>
		{
			_search.Location = new Point(header.Width - _search.Width - Ui.S(14), Ui.S(12));
			_hideBtn.Location = new Point(_search.Left - _hideBtn.Width - Ui.S(6), Ui.S(12));
			_quickSave.Location = new Point(_hideBtn.Left - _quickSave.Width - Ui.S(6), Ui.S(12));
			int pagesLeft = _title.Right + Ui.S(16);
			int pagesW = Math.Max(Ui.S(200), _quickSave.Left - pagesLeft - Ui.S(8));
			_pages.SetBounds(pagesLeft, Ui.S(7), pagesW, Ui.S(38));
		};

		// 2. Bottom Hardware Telemetry Strip (Height = 44px)
		Panel footer = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = Ui.S(44),
			BackColor = Ui.Side
		};

		_audio = new Label
		{
			AutoSize = false,
			Width = Ui.S(240),
			Height = Ui.S(36),
			Location = new Point(Ui.S(14), Ui.S(4)),
			ForeColor = Ui.Faint,
			Font = Ui.Small,
			TextAlign = ContentAlignment.MiddleLeft
		};

		_hintTitle.Font = Ui.Bold;
		_hintTitle.ForeColor = Ui.Text;
		_hintTitle.AutoSize = false;
		_hintText.Font = Ui.Small;
		_hintText.ForeColor = Ui.Dim;
		_hintText.AutoSize = false;

		_resetPage.Click += (s, a) => ResetPage();
		_resetAll.Click += (s, a) => ResetAll();
		_close.Click += (s, a) => Close();
		_tips.SetToolTip(_resetPage, "Puts every setting on this page back to default");
		_tips.SetToolTip(_resetAll, "Reset all visual settings to default");

		footer.Controls.AddRange(new Control[6] { _audio, _hintTitle, _hintText, _resetPage, _resetAll, _close });
		footer.Resize += (s, a) => LayoutFooter(footer);

		// 3. Main Scrollable Rack Content
		_content.Dock = DockStyle.Fill;
		_content.MouseLeave += (s, a) => Hint(null);
		_content.Resize += (s, a) => LayoutContent();

		Controls.Add(_content);
		Controls.Add(footer);
		Controls.Add(header);

		_store.Changed += StoreChanged;
		_statusTimer.Tick += (s, a) => ShowAudio();
		_statusTimer.Start();
		ShowAudio();

		ShowPage("Presets");
		_pages.Selected = _pages.Pages.IndexOf(_page);
		ShowStatus();
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		Palette.DarkTitle(this, Palette.From(Ui.Back, Ui.Text, Ui.Back, Ui.Text));
	}

	protected override void OnShown(EventArgs e)
	{
		base.OnShown(e);
		LayoutContent();
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
		int pad = Ui.S(14);
		int curRight = footer.Width - pad;
		FlatButton[] btns = new FlatButton[3] { _close, _resetAll, _resetPage };
		foreach (FlatButton b in btns)
		{
			b.Width = b.Ideal();
			curRight -= b.Width;
			b.Location = new Point(curRight, footer.Height - b.Height - Ui.S(8));
			curRight -= Ui.S(6);
		}

		int hintW = Math.Max(Ui.S(120), curRight - _audio.Right - Ui.S(16));
		_hintTitle.SetBounds(_audio.Right + Ui.S(10), Ui.S(4), hintW, Ui.S(16));
		_hintText.SetBounds(_audio.Right + Ui.S(10), Ui.S(22), hintW, Ui.S(18));
	}

	private void ShowAudio()
	{
		string text = (_audioStatus != null) ? _audioStatus() : "";
		if (_audio.Text != text) _audio.Text = text;
	}

	private void StoreChanged(object sender, EventArgs e)
	{
		if (IsDisposed) return;
		if (InvokeRequired)
		{
			BeginInvoke(new Action(() => StoreChanged(sender, e)));
			return;
		}

		Look current = _store.Current;
		bool visChanged = false;
		foreach (SettingRow row in _rows)
		{
			bool wasVis = row.Visible;
			row.ShowValue(current);
			if (row.Visible != wasVis) visChanged = true;
		}
		if (visChanged) LayoutContent();

		if (_page == "Presets")
		{
			foreach (Control control in _content.Controls) control.Invalidate();
		}
		ShowStatus();
	}

	private void ShowStatus()
	{
		Look l = _store.Current;
		string text = (_store.PresetName.Length > 0) ? (_store.PresetName + (_store.Modified ? " (changed)" : "")) : "your own settings";
		int diff = _store.Schema.All.Count(s => !s.Personal && !s.IsDefault(l));
		_pages.Marked.Clear();
		foreach (Setting s in _store.Schema.All)
		{
			if (!s.IsDefault(l)) _pages.Marked.Add(s.Page);
		}
		_pages.Invalidate();
		_resetPage.Enabled = _page != "Presets" && _search.Box.Text.Length == 0 && _store.Schema.All.Any(s => s.Page == _page && !s.IsDefault(l));
	}

	public void Hint(Setting s)
	{
		if (s == null)
		{
			_hintTitle.Text = (_page == "Presets") ? "Signature Presets" : _page;
			_hintText.Text = PageHint(_page);
		}
		else
		{
			_hintTitle.Text = s.Label + (s.IsDefault(_store.Current) ? "" : ("   (default: " + Describe(s, s.Default) + ")"));
			_hintText.Text = s.Hint + (s.IsEnabled(_store.Current) ? "" : "   Not used with current settings.");
		}
	}

	private static string Describe(Setting s, object value)
	{
		return s.Kind switch
		{
			SettingKind.Toggle => (bool)value ? "on" : "off",
			SettingKind.Number => s.Display((double)value),
			SettingKind.Choice => s.Choices[(int)value],
			_ => ColourMath.ToHex((int)value, s.Alpha)
		};
	}

	private static string PageHint(string page)
	{
		return page switch
		{
			"Presets" => "Select a signature look or palette. Hover any control for its audio specification.",
			"Studio" => "Rack layout, bar geometry, transparent vacuum tubes, symmetry, and waveform.",
			"Optics & FX" => "Screen optics, 3D Cyber-Grid horizon, floating particles, bloom glow, and scale lines.",
			"Colours" => "Complete spectral color balance, gradient stops, and audio-reactive color modulation.",
			"Audio & System" => "Audio refresh cadence, direct BASS tap, FFT resolution, and display frame rate.",
			_ => "Hover any control to inspect its hardware telemetry.",
		};
	}
}
