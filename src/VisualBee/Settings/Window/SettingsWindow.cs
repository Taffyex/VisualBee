using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed partial class SettingsWindow : Form
{
	private const string PresetsPage = "Presets";

	private readonly LookStore _store;
	private readonly Func<string> _audioStatus;
	private readonly PageList _pages = new PageList();
	private readonly ScrollPane _content = new ScrollPane();
	private readonly InputBox _search = new InputBox { SearchMode = true };
	private readonly SidebarStatusCard _lookStatus = new SidebarStatusCard();
	private readonly AudioTelemetryCard _telemetry = new AudioTelemetryCard();
	private readonly Label _pageTitle = new Label();
	private readonly Label _pageSubtitle = new Label();
	private readonly Label _hintTitle = new Label();
	private readonly Label _hintText = new Label();
	private readonly FlatButton _resetPage = new FlatButton("Reset page", Ui.GlyphReset);
	private readonly FlatButton _resetAll = new FlatButton("Reset all");
	public bool _hideInactive = true;
	private readonly FlatButton _hideBtn = new FlatButton("Active only", Ui.GlyphFilter) { Active = true };
	private readonly FlatButton _quickShare = new FlatButton("Share", Ui.GlyphShare);
	private readonly FlatButton _quickSave = new FlatButton("Save…", Ui.GlyphSave);
	private readonly FlatButton _close = new FlatButton("Close") { Primary = true };
	private readonly ToolTip _tips = new ToolTip();
	private readonly List<SettingRow> _rows = new List<SettingRow>();
	private readonly List<RackSectionCard> _cards = new List<RackSectionCard>();
	private readonly Timer _statusTimer = new Timer { Interval = 500 };
	private string _page;
	private string _renderedPage;
	private string _renderedQuery = "";

	public LookStore Store => _store;

	public SettingsWindow(LookStore store, Func<string> audioStatus)
	{
		_store = store;
		_audioStatus = audioStatus;
		Text = string.IsNullOrEmpty(store.Title) ? "VisualBee Studio" : store.Title;
		Font = Ui.Body;
		BackColor = Ui.Back;
		ForeColor = Ui.Text;
		AutoScaleMode = AutoScaleMode.None;
		StartPosition = FormStartPosition.Manual;
		ShowInTaskbar = false;
		KeyPreview = true;
		MinimumSize = new Size(Ui.S(780), Ui.S(540));
		Size = new Size(Ui.S(1020), Ui.S(700));
		Icon = null;
		ShowIcon = false;
		AllowDrop = true;
		DragEnter += OnWindowDragEnter;
		DragDrop += OnWindowDragDrop;
		SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);

		Panel sidebar = BuildSidebar();
		Panel main = BuildWorkspace();

		Controls.Add(main);
		Controls.Add(sidebar);

		_store.Changed += StoreChanged;
		_statusTimer.Tick += (s, a) => ShowAudio();
		_statusTimer.Start();
		ShowAudio();

		ShowPage(PresetsPage);
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
			if (_search.Box.Focused && _search.Box.Text.Length > 0) _search.Box.Text = "";
			else Close();
			return true;
		default:
			return base.ProcessCmdKey(ref msg, keyData);
		}
	}

	private void ShowAudio()
	{
		string text = (_audioStatus != null) ? _audioStatus() : "";
		if (_telemetry.UpdateText(text))
		{
			_tips.SetToolTip(_telemetry, text);
		}
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
			bool wasVis = row.IsRowVisible;
			row.ShowValue(current);
			if (row.IsRowVisible != wasVis) visChanged = true;
		}
		foreach (RackSectionCard card in _cards)
		{
			bool wasCardVis = card.Visible;
			card.RefreshState(current);
			if (card.IsCardVisible != wasCardVis) visChanged = true;
		}

		if (visChanged) LayoutContent();
		if (_page == PresetsPage)
		{
			foreach (Control control in _content.Controls) control.Invalidate();
		}
		ShowStatus();
	}

	private void ShowStatus()
	{
		Look l = _store.Current;
		int totalDiff = _store.Schema.All.Count(s => !s.Personal && !s.IsDefault(l));
		_lookStatus.SetState(_store.PresetName, _store.Modified, totalDiff);

		_pages.ModifiedCounts.Clear();
		foreach (Setting s in _store.Schema.All)
		{
			if (!s.IsDefault(l))
			{
				_pages.ModifiedCounts.TryGetValue(s.Page, out int count);
				_pages.ModifiedCounts[s.Page] = count + 1;
			}
		}
		_pages.Invalidate();
		_resetPage.Enabled = _page != PresetsPage && _search.Box.Text.Length == 0 && _store.Schema.All.Any(s => s.Page == _page && !s.IsDefault(l));
	}

	public void Hint(Setting s)
	{
		_hintTitle.ForeColor = Ui.Text;
		_hintText.ForeColor = Ui.Dim;
		if (s == null)
		{
			_hintTitle.Text = (_page == PresetsPage) ? "Signature Presets" : _page;
			_hintText.Text = PageHint(_page);
			return;
		}
		_hintTitle.Text = s.Label + (s.IsDefault(_store.Current) ? "" : ("   (default: " + Describe(s, s.Default) + ")"));
		_hintText.Text = s.Hint + (s.IsEnabled(_store.Current) ? "" : "   (Unused with current settings.)");
	}

	public void ShowNotice(string title, string detail)
	{
		_hintTitle.ForeColor = Ui.Accent;
		_hintText.ForeColor = Ui.Dim;
		_hintTitle.Text = title;
		_hintText.Text = detail ?? "";
	}

	private static string Describe(Setting s, object value)
	{
		return s.Kind switch
		{
			SettingKind.Toggle => (bool)value ? "on" : "off",
			SettingKind.Number => s.Display((double)value),
			SettingKind.Choice => s.Choices[(int)value],
			SettingKind.Device => WasapiLoopback.GetDeviceDisplayName(value as string),
			_ => ColourMath.ToHex((int)value, s.Alpha)
		};
	}

	private static string PageSubtitle(string page)
	{
		return page switch
		{
			"Presets" => "Signature looks, palettes & sharing",
			"Studio" => "Layouts, bars, optics, peaks & scope",
			"Optics & FX" => "Backdrop, 3D grid, particles, glow & lens",
			"Colours" => "Gradients, spectral stops & cycling",
			"Audio" => "Capture, FFT engine, range & pacing",
			_ => "Configure visualizer settings"
		};
	}

	private static string PageHint(string page)
	{
		return page switch
		{
			"Presets" => "Select a signature look or colour palette. Right-click a preset to manage it.",
			"Studio" => "Layout arrangement, bar geometry, vacuum tube optics, peaks, and waveform scope.",
			"Optics & FX" => "Background surface, 3D Cyber-Grid, floating particles, bloom glow, and lens effects.",
			"Colours" => "Spectral colour stops, gradients, grid tints, and audio-reactive colour modulation.",
			"Audio" => "Audio capture routing, FFT engine resolution, frequency bounds, and frame pacing.",
			_ => "Hover any setting to inspect its description and default value."
		};
	}
}
