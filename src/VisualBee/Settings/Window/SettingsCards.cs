using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed partial class SettingsWindow : Form
{
	private sealed class ScrollPane : Panel
	{
		public int ContentWidth => Math.Max(1, Width - SystemInformation.VerticalScrollBarWidth);

		[DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
		private static extern int SetWindowTheme(IntPtr hwnd, string app, string idList);

		[DllImport("user32.dll")]
		private static extern bool ShowScrollBar(IntPtr hwnd, int bar, bool show);

		public ScrollPane()
		{
			AutoScroll = true;
			BackColor = Ui.Back;
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			try
			{
				SetWindowTheme(Handle, "DarkMode_Explorer", null);
			}
			catch (Exception)
			{
			}
		}

		protected override Point ScrollToControl(Control activeControl)
		{
			return DisplayRectangle.Location;
		}

		protected override void OnLayout(LayoutEventArgs levent)
		{
			base.OnLayout(levent);
			if (IsHandleCreated)
			{
				HorizontalScroll.Maximum = 0;
				HorizontalScroll.Visible = false;
				ShowScrollBar(Handle, 0, false);
			}
		}
	}

	internal sealed class FlowPanel : Panel
	{
		public FlowPanel()
		{
			BackColor = Ui.Back;
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		}

		public void FitWidth(int width)
		{
			int gap = Ui.S(10);
			int x = 0;
			int y = Ui.S(2);
			int rowH = 0;
			foreach (Control control in Controls)
			{
				if (control is PresetCard)
				{
					int cols = Math.Max(1, (width + gap) / (Ui.S(240) + gap));
					control.Width = (width - gap * (cols - 1)) / cols;
				}
				if (control is FlatButton fb)
				{
					control.Width = fb.Ideal();
				}
				if (x > 0 && x + control.Width > width)
				{
					x = 0;
					y += rowH + gap;
					rowH = 0;
				}
				control.Location = new Point(x, y);
				x += control.Width + gap;
				rowH = Math.Max(rowH, control.Height);
			}
			Height = y + rowH + Ui.S(6);
		}
	}

	private sealed class RackSectionCard : Widget
	{
		private readonly SettingsWindow _w;
		private readonly string _title;
		private readonly Func<Setting, bool> _members;
		private readonly Setting _master;
		private readonly Action _onHeaderClick;
		private readonly Toggle _masterToggle;
		public readonly List<SettingRow> Rows = new List<SettingRow>();

		private Rectangle _resetRect;
		private bool _hasDiff;
		private bool _hasVisibleRows;
		private bool _resetHot;
		private bool _headerHot;
		private bool _masterActive = true;
		private bool _showing;

		public bool IsCardVisible
		{
			get
			{
				if (_master != null)
				{
					bool searching = !string.IsNullOrEmpty(_w._search.Box.Text.Trim());
					if (!_w._hideInactive || searching || _master.IsEnabled(_w._store.Current))
					{
						return true;
					}
				}
				return Rows.Any(r => r.IsRowVisible);
			}
		}

		public RackSectionCard(SettingsWindow w, string title, Func<Setting, bool> members, Setting master = null, Action onHeaderClick = null)
		{
			_w = w;
			_title = title;
			_members = members;
			_master = master;
			_onHeaderClick = onHeaderClick;
			BackColor = Ui.Back;

			if (_master != null)
			{
				_masterToggle = new Toggle();
				_masterToggle.Changed += (object s, EventArgs e) =>
				{
					if (!_showing) _w._store.Set(_master, _masterToggle.On);
				};
				_masterToggle.MouseEnter += (object s, EventArgs e) => _w.Hint(_master);
				Controls.Add(_masterToggle);
				_w._tips.SetToolTip(_masterToggle, _master.Hint);
			}
		}

		public void AddRow(SettingRow row)
		{
			Rows.Add(row);
			Controls.Add(row);
		}

		public void RefreshState(Look l)
		{
			if (_master != null && _masterToggle != null)
			{
				_showing = true;
				try
				{
					bool on = (bool)_master.Get(l);
					bool en = _master.IsEnabled(l);
					_masterToggle.On = on;
					_masterToggle.Enabled = en;
					_masterActive = on && en;
				}
				finally
				{
					_showing = false;
				}
			}

			bool prevDiff = _hasDiff;
			_hasDiff = (_members != null)
				? _w._store.Schema.All.Any(s => _members(s) && !s.IsDefault(l))
				: ((_master != null && !_master.IsDefault(l)) || Rows.Any(r => !r.Setting.IsDefault(l)));

			if (!_hasDiff) _resetHot = false;
			if (prevDiff != _hasDiff || _master != null) Invalidate();
		}

		public void FitWidth(int width)
		{
			Width = width;
			int headerH = Ui.S(44);

			int rightEdge = width - Ui.S(16);
			if (_masterToggle != null)
			{
				_masterToggle.Location = new Point(rightEdge - _masterToggle.Width, (headerH - _masterToggle.Height) / 2);
				rightEdge = _masterToggle.Left - Ui.S(12);
			}

			int resetW = Ui.S(64);
			_resetRect = new Rectangle(rightEdge - resetW, (headerH - Ui.S(24)) / 2, resetW, Ui.S(24));

			List<SettingRow> visible = new List<SettingRow>();
			foreach (SettingRow r in Rows)
			{
				r.Visible = r.IsRowVisible;
				if (r.IsRowVisible) visible.Add(r);
			}

			_hasVisibleRows = visible.Count > 0;
			if (!_hasVisibleRows)
			{
				Height = headerH;
				return;
			}

			int y = headerH + Ui.S(2);
			for (int i = 0; i < visible.Count; i++)
			{
				SettingRow r = visible[i];
				r.ShowDivider = i < visible.Count - 1;
				r.SetBounds(Ui.S(2), y, width - Ui.S(4), r.Height);
				y += r.Height;
			}
			Height = y + Ui.S(6);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.Clear(Ui.Back);
			Ui.Smooth(g);

			RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
			Ui.Fill(g, r, Ui.Sf(8f), Ui.Card);
			Ui.Stroke(g, r, Ui.Sf(8f), Ui.Border, 1f);

			using (Pen specPen = new Pen(Ui.Specular, 1f))
			{
				g.DrawLine(specPen, Ui.Sf(10f), 1f, Width - Ui.Sf(10f), 1f);
			}

			int headerH = Ui.S(44);
			Color dotCol = (_master != null) ? (_masterActive ? Ui.Accent : Ui.Faint) : Ui.Amber;
			using (SolidBrush dot = new SolidBrush(dotCol))
			{
				g.FillEllipse(dot, Ui.Sf(16f), (headerH - Ui.Sf(6f)) / 2f, Ui.Sf(6f), Ui.Sf(6f));
			}

			int titleRight = (_hasDiff && _members != null)
				? (_resetRect.Left - Ui.S(8))
				: (_masterToggle != null ? _masterToggle.Left - Ui.S(12) : (_onHeaderClick != null ? Width - Ui.S(92) : Width - Ui.S(16)));
			Rectangle titleRect = new Rectangle(Ui.S(30), 0, Math.Max(1, titleRight - Ui.S(30)), headerH);
			Color titleCol = (_master != null && !_masterActive) ? Ui.Dim : (_headerHot ? Ui.Accent : Ui.Text);
			Ui.DrawText(g, _title.ToUpperInvariant(), Ui.Section, titleCol, titleRect, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

			if (_onHeaderClick != null)
			{
				Ui.DrawText(g, "Open page ›", Ui.SmallBold, _headerHot ? Ui.Accent : Ui.Faint, new Rectangle(Width - Ui.S(92), 0, Ui.S(78), headerH), TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
			}

			if (_hasDiff && _members != null)
			{
				if (_resetHot)
				{
					Ui.Fill(g, _resetRect, Ui.Sf(5f), Ui.Hover);
				}
				Color resetCol = _resetHot ? Ui.Accent : Ui.Dim;
				Ui.Glyph(g, Ui.GlyphReset, Ui.Icons, resetCol, new Rectangle(_resetRect.X + Ui.S(4), _resetRect.Y, Ui.S(16), _resetRect.Height));
				Ui.DrawText(g, "Reset", Ui.SmallBold, resetCol, new Rectangle(_resetRect.X + Ui.S(20), _resetRect.Y, _resetRect.Width - Ui.S(22), _resetRect.Height), TextFormatFlags.VerticalCenter);
			}

			if (_hasVisibleRows)
			{
				using Pen div = new Pen(Ui.Divider, 1f);
				g.DrawLine(div, Ui.Sf(16f), headerH, Width - Ui.Sf(16f), headerH);
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			bool overHeader = e.Y < Ui.S(44);
			bool overReset = _hasDiff && _members != null && _resetRect.Contains(e.Location);
			bool overJump = _onHeaderClick != null && overHeader && !overReset;
			if (overReset != _resetHot || overJump != _headerHot)
			{
				_resetHot = overReset;
				_headerHot = overJump;
				Cursor = (overReset || overJump) ? Cursors.Hand : Cursors.Default;
				Invalidate();
			}
			if (_master != null && overHeader && !overReset)
			{
				_w.Hint(_master);
			}
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			if (_resetHot || _headerHot)
			{
				_resetHot = false;
				_headerHot = false;
				Cursor = Cursors.Default;
				Invalidate();
			}
		}

		protected override void OnMouseClick(MouseEventArgs e)
		{
			base.OnMouseClick(e);
			if (e.Button != MouseButtons.Left) return;
			if (_hasDiff && _members != null && _resetRect.Contains(e.Location))
			{
				_w.ResetMembers(_members);
			}
			else if (_onHeaderClick != null && e.Y < Ui.S(44))
			{
				_onHeaderClick();
			}
		}
	}
}
