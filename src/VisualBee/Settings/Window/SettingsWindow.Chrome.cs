using System;
using System.Drawing;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed partial class SettingsWindow : Form
{
	private Panel BuildSidebar()
	{
		Panel sidebar = new Panel
		{
			Dock = DockStyle.Left,
			Width = Ui.S(208),
			BackColor = Ui.Side
		};
		sidebar.Paint += (s, e) =>
		{
			using Pen border = new Pen(Ui.Border, 1f);
			e.Graphics.DrawLine(border, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);
		};

		Panel brand = new Panel
		{
			Height = Ui.S(58),
			BackColor = Ui.Side
		};
		brand.Paint += PaintBrand;

		_pages.Pages.Add(PresetsPage);
		_pages.Pages.AddRange(_store.Schema.Pages());
		_pages.Changed += (s, a) =>
		{
			string target = _pages.Pages[_pages.Selected];
			_page = target;
			if (!string.IsNullOrEmpty(_search.Box.Text))
			{
				_search.Box.Text = "";
			}
			if (_renderedPage != target || _renderedQuery.Length > 0)
			{
				ShowPage(target);
			}
		};

		_lookStatus.MouseEnter += (s, a) => ShowNotice("Active Look", "Click to open Presets; right-click to copy share code, save, or revert.");
		_lookStatus.MouseLeave += (s, a) => Hint(null);
		_lookStatus.MouseClick += (s, a) =>
		{
			if (a.Button == MouseButtons.Left) JumpToPage(PresetsPage);
			else if (a.Button == MouseButtons.Right) ShowActiveLookMenu(a.Location);
		};
		_tips.SetToolTip(_lookStatus, "Click to view Presets; right-click to share, save, or revert");

		sidebar.Controls.AddRange(new Control[] { brand, _lookStatus, _pages, _telemetry });
		sidebar.Resize += (s, a) =>
		{
			int pad = Ui.S(10);
			int innerW = sidebar.Width - pad * 2 - 1;
			brand.SetBounds(0, 0, sidebar.Width - 1, Ui.S(58));
			_lookStatus.SetBounds(pad, brand.Bottom + Ui.S(2), innerW, _lookStatus.Height);
			_telemetry.SetBounds(pad, sidebar.Height - _telemetry.Height - Ui.S(12), innerW, _telemetry.Height);
			int navTop = _lookStatus.Bottom + Ui.S(10);
			int navH = Math.Max(Ui.S(120), _telemetry.Top - navTop - Ui.S(10));
			_pages.SetBounds(0, navTop, sidebar.Width - 1, navH);
		};
		return sidebar;
	}

	private static void PaintBrand(object sender, PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		Ui.Smooth(g);
		RectangleF badge = new RectangleF(Ui.Sf(14f), Ui.Sf(16f), Ui.Sf(24f), Ui.Sf(24f));
		Ui.Fill(g, badge, Ui.Sf(6f), Ui.AccentSoft);
		Ui.Stroke(g, badge, Ui.Sf(6f), Ui.AccentBorder);

		float bx = badge.X + Ui.Sf(5.5f);
		float by = badge.Bottom - Ui.Sf(6f);
		float bw = Ui.Sf(2.5f);
		float[] bh = new float[] { 6f, 11f, 8f, 13f };
		using (SolidBrush barBrush = new SolidBrush(Ui.Accent))
		{
			for (int i = 0; i < bh.Length; i++)
			{
				float h = Ui.Sf(bh[i]);
				g.FillRectangle(barBrush, bx + i * Ui.Sf(3.5f), by - h, bw, h);
			}
		}

		Ui.DrawText(g, "VISUALBEE", Ui.Bold, Ui.Text, new Rectangle(Ui.S(46), Ui.S(14), Ui.S(150), Ui.S(16)), TextFormatFlags.VerticalCenter);
		Ui.DrawText(g, "STUDIO", Ui.Section, Ui.Accent, new Rectangle(Ui.S(46), Ui.S(30), Ui.S(150), Ui.S(14)), TextFormatFlags.VerticalCenter);
	}

	private Panel BuildWorkspace()
	{
		Panel main = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = Ui.Back
		};

		Panel topBar = new Panel
		{
			Dock = DockStyle.Top,
			Height = Ui.S(60),
			BackColor = Ui.Back
		};
		topBar.Paint += (s, e) =>
		{
			using Pen div = new Pen(Ui.Border, 1f);
			e.Graphics.DrawLine(div, 0, topBar.Height - 1, topBar.Width, topBar.Height - 1);
		};

		_pageTitle.Font = Ui.Heading;
		_pageTitle.ForeColor = Ui.Text;
		_pageTitle.AutoSize = false;
		_pageTitle.UseMnemonic = false;
		_pageTitle.AutoEllipsis = true;

		_pageSubtitle.Font = Ui.Small;
		_pageSubtitle.ForeColor = Ui.Dim;
		_pageSubtitle.AutoSize = false;
		_pageSubtitle.UseMnemonic = false;
		_pageSubtitle.AutoEllipsis = true;

		Ui.CueBanner(_search.Box, "Search (Ctrl+F)");
		_search.Box.TextChanged += (s, a) => OnSearchChanged();

		_hideBtn.Click += (s, a) =>
		{
			_hideInactive = !_hideInactive;
			_hideBtn.Text = _hideInactive ? "Active only" : "All settings";
			_hideBtn.Active = _hideInactive;
			_hideBtn.Width = _hideBtn.Ideal();
			LayoutTopBar(topBar);
			StoreChanged(this, EventArgs.Empty);
		};
		_quickShare.Click += (s, a) => ShowQuickShareMenu();
		_quickSave.Click += (s, a) => SaveLook();
		_tips.SetToolTip(_hideBtn, "Toggle hiding controls unused in the current visualizer mode");
		_tips.SetToolTip(_quickShare, "Copy or paste a share code, or export/import a .ini preset");
		_tips.SetToolTip(_quickSave, "Save current configuration as a preset");

		topBar.Controls.AddRange(new Control[] { _pageTitle, _pageSubtitle, _quickSave, _quickShare, _hideBtn, _search });
		topBar.Resize += (s, a) => LayoutTopBar(topBar);

		Panel footer = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = Ui.S(52),
			BackColor = Ui.Side
		};
		footer.Paint += (s, e) =>
		{
			using Pen div = new Pen(Ui.Border, 1f);
			e.Graphics.DrawLine(div, 0, 0, footer.Width, 0);
		};

		_hintTitle.Font = Ui.Bold;
		_hintTitle.ForeColor = Ui.Text;
		_hintTitle.AutoSize = false;
		_hintTitle.UseMnemonic = false;
		_hintTitle.AutoEllipsis = true;

		_hintText.Font = Ui.Small;
		_hintText.ForeColor = Ui.Dim;
		_hintText.AutoSize = false;
		_hintText.UseMnemonic = false;
		_hintText.AutoEllipsis = true;

		_resetPage.Click += (s, a) => ResetPage();
		_resetAll.Click += (s, a) => ResetAll();
		_close.Click += (s, a) => Close();
		_tips.SetToolTip(_resetPage, "Restore all settings on the current page to default");
		_tips.SetToolTip(_resetAll, "Reset all visual settings to default");

		footer.Controls.AddRange(new Control[] { _hintTitle, _hintText, _resetPage, _resetAll, _close });
		footer.Resize += (s, a) => LayoutFooter(footer);

		_content.Dock = DockStyle.Fill;
		_content.MouseLeave += (s, a) => Hint(null);
		_content.Resize += (s, a) => LayoutContent();

		main.Controls.Add(_content);
		main.Controls.Add(footer);
		main.Controls.Add(topBar);
		return main;
	}

	private void LayoutTopBar(Panel topBar)
	{
		int pad = Ui.S(20);
		int right = topBar.Width - pad;
		int searchW = Math.Min(Ui.S(195), Math.Max(Ui.S(115), topBar.Width / 4));
		_search.SetBounds(right - searchW, (topBar.Height - _search.Height) / 2, searchW, _search.Height);
		right = _search.Left - Ui.S(6);

		_hideBtn.Width = _hideBtn.Ideal();
		_hideBtn.Location = new Point(right - _hideBtn.Width, (topBar.Height - _hideBtn.Height) / 2);
		right = _hideBtn.Left - Ui.S(6);

		_quickShare.Width = _quickShare.Ideal();
		_quickShare.Location = new Point(right - _quickShare.Width, (topBar.Height - _quickShare.Height) / 2);
		right = _quickShare.Left - Ui.S(6);

		_quickSave.Width = _quickSave.Ideal();
		_quickSave.Location = new Point(right - _quickSave.Width, (topBar.Height - _quickSave.Height) / 2);

		int titleW = Math.Max(Ui.S(90), _quickSave.Left - pad - Ui.S(10));
		_pageTitle.SetBounds(pad, Ui.S(9), titleW, Ui.S(24));
		_pageSubtitle.SetBounds(pad, Ui.S(34), titleW, Ui.S(18));
	}

	private void LayoutFooter(Panel footer)
	{
		int pad = Ui.S(20);
		int right = footer.Width - pad;
		FlatButton[] btns = new FlatButton[] { _close, _resetAll, _resetPage };
		foreach (FlatButton b in btns)
		{
			b.Width = b.Ideal();
			right -= b.Width;
			b.Location = new Point(right, (footer.Height - b.Height) / 2);
			right -= Ui.S(8);
		}

		int hintW = Math.Max(Ui.S(120), right - pad - Ui.S(8));
		_hintTitle.SetBounds(pad, Ui.S(8), hintW, Ui.S(18));
		_hintText.SetBounds(pad, Ui.S(27), hintW, Ui.S(18));
	}
}
