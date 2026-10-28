using System.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
			int num = Ui.S(10);
			int num2 = 0;
			int num3 = Ui.S(4);
			int num4 = 0;
			foreach (Control control in Controls)
			{
				if (control is PresetCard)
				{
					int num5 = Math.Max(1, (width + num) / (Ui.S(230) + num));
					control.Width = (width - num * (num5 - 1)) / num5;
				}
				if (control is FlatButton)
				{
					control.Width = ((FlatButton)control).Ideal();
				}
				if (num2 > 0 && num2 + control.Width > width)
				{
					num2 = 0;
					num3 += num4 + num;
					num4 = 0;
				}
				control.Location = new Point(num2, num3);
				num2 += control.Width + num;
				num4 = Math.Max(num4, control.Height);
			}
			Height = num3 + num4 + Ui.S(6);
		}
	}

	private sealed class RackSectionCard : Widget
	{
		private readonly SettingsWindow _w;
		private readonly string _title;
		private readonly Func<Setting, bool> _members;
		private readonly LinkLabel _reset = new LinkLabel();
		public readonly List<SettingRow> Rows = new List<SettingRow>();

		public bool IsCardVisible => Rows.Any(r => r.IsRowVisible);

		public RackSectionCard(SettingsWindow w, string title, Func<Setting, bool> members)
		{
			_w = w;
			_title = title;
			_members = members;
			BackColor = Ui.Card;

			if (members != null)
			{
				_reset.Text = "Reset section";
				_reset.Font = Ui.Small;
				_reset.AutoSize = true;
				_reset.LinkColor = Ui.Accent;
				_reset.ActiveLinkColor = Ui.Text;
				_reset.LinkBehavior = LinkBehavior.HoverUnderline;
				_reset.BackColor = Color.Transparent;
				_reset.LinkClicked += (s, e) => _w.ResetMembers(_members);
				Controls.Add(_reset);
			}
		}

		public void AddRow(SettingRow row)
		{
			Rows.Add(row);
			Controls.Add(row);
		}

		public void FitWidth(int width)
		{
			Width = width;
			int headerH = Ui.S(38);
			if (_reset != null && _reset.Visible)
			{
				_reset.Location = new Point(width - _reset.Width - Ui.S(16), Ui.S(12));
			}

			int y = headerH;
			foreach (SettingRow r in Rows)
			{
				if (!r.IsRowVisible)
				{
					r.Visible = false;
					continue;
				}
				r.Visible = true;
				r.SetBounds(Ui.S(10), y, width - Ui.S(20), r.Height);
				y += r.Height + Ui.S(2);
			}
			Height = y + Ui.S(8);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			Ui.Smooth(g);
			RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
			Ui.Fill(g, r, Ui.Sf(8f), Ui.Card);
			Ui.Stroke(g, r, Ui.Sf(8f), Ui.Border, 1f);

			// Top specular chassis highlight
			using (Pen specPen = new Pen(Color.FromArgb(45, 55, 78), 1f))
			{
				g.DrawLine(specPen, Ui.Sf(10f), 1f, Width - Ui.Sf(10f), 1f);
			}

			// Amber/cyan module indicator dot
			using (SolidBrush dotB = new SolidBrush(Ui.Amber))
			{
				g.FillEllipse(dotB, Ui.Sf(14f), Ui.Sf(14f), Ui.Sf(6f), Ui.Sf(6f));
			}

			// Header Title
			Rectangle titleRect = new Rectangle(Ui.S(26), Ui.S(8), Width - Ui.S(130), Ui.S(24));
			Ui.DrawText(g, _title.ToUpperInvariant(), Ui.Section, Ui.Text, titleRect, TextFormatFlags.VerticalCenter);
		}
	}

	private sealed class PresetCard : Widget
	{
		private readonly SettingsWindow _w;
		private readonly Preset _p;

		public PresetCard(SettingsWindow w, Preset p)
		{
			_w = w;
			_p = p;
			Height = Ui.S(92);
			Width = Ui.S(230);
			Cursor = Cursors.Hand;
			if (p.Description.Length > 0)
			{
				w._tips.SetToolTip(this, p.Description);
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.Clear(Ui.Back);
			Ui.Smooth(g);
			bool isCurr = _w.IsCurrent(_p);
			RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
			Color bg = Down ? Ui.Raised : (Hot ? Ui.Hover : Ui.Card);
			Ui.Fill(g, rect, Ui.Sf(8f), bg);
			Ui.Stroke(g, rect, Ui.Sf(8f), isCurr ? Ui.Accent : Ui.Border, isCurr ? Ui.Sf(2f) : 1f);

			// Swatch dots
			int[] array = _w.Swatch(_p);
			float dotSize = Ui.Sf(14f);
			float startX = Width - Ui.Sf(12f) - array.Length * (dotSize - Ui.Sf(3f));
			for (int i = 0; i < array.Length; i++)
			{
				RectangleF dRect = new RectangleF(startX + i * (dotSize - Ui.Sf(3f)), Ui.Sf(12f), dotSize, dotSize);
				using (SolidBrush b = new SolidBrush(Color.FromArgb(array[i] | -16777216)))
				{
					g.FillEllipse(b, dRect);
				}
				using Pen pPen = new Pen(Ui.Card, Ui.Sf(1.5f));
				g.DrawEllipse(pPen, dRect);
			}

			// Preset Title
			Rectangle rTitle = new Rectangle(Ui.S(12), Ui.S(8), (int)startX - Ui.S(16), Ui.S(22));
			Ui.DrawText(g, _p.Name, Ui.Bold, Ui.Text, rTitle, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);

			// Description
			Rectangle rDesc = new Rectangle(Ui.S(12), Ui.S(34), Width - Ui.S(24), Height - Ui.S(40));
			string text = _p.Description.Length > 0 ? _p.Description : (_p.BuiltIn ? "" : "Your custom preset");
			Ui.DrawText(g, text, Ui.Small, Ui.Dim, rDesc, TextFormatFlags.EndEllipsis | TextFormatFlags.WordBreak);

			if (isCurr)
			{
				Ui.Glyph(g, "\ue73e", Ui.Icons, Ui.Accent, new Rectangle(Width - Ui.S(26), Height - Ui.S(24), Ui.S(18), Ui.S(18)));
			}
		}

		protected override void OnMouseClick(MouseEventArgs e)
		{
			base.OnMouseClick(e);
			if (e.Button == MouseButtons.Left) _w.ApplyPreset(_p);
			else if (e.Button == MouseButtons.Right) _w.PresetMenu(_p, this, e.Location);
		}
	}

	private sealed class ThemeChip : Widget
	{
		private readonly SettingsWindow _w;
		private readonly Theme _t;

		public ThemeChip(SettingsWindow w, Theme t)
		{
			_w = w;
			_t = t;
			Height = Ui.S(38);
			Width = TextRenderer.MeasureText(t.Name, Ui.Body).Width + Ui.S(100);
			Cursor = Cursors.Hand;
			w._tips.SetToolTip(this, "Palette: " + t.Name);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.Clear(Ui.Back);
			Ui.Smooth(g);
			RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
			Color c = Down ? Ui.Raised : (Hot ? Ui.Hover : Ui.Card);
			Ui.Fill(g, rect, Ui.Sf(18f), c);
			Ui.Stroke(g, rect, Ui.Sf(18f), Hot ? Ui.Accent : Ui.Border, 1f);

			int[] array = _t.Swatch();
			float num = Ui.Sf(16f);
			for (int i = 0; i < array.Length; i++)
			{
				RectangleF dRect = new RectangleF(Ui.Sf(8f) + i * Ui.Sf(12f), (Height - num) / 2f, num, num);
				using (SolidBrush brush = new SolidBrush(Color.FromArgb(array[i])))
				{
					g.FillEllipse(brush, dRect);
				}
				using Pen pen = new Pen(Ui.Card, Ui.Sf(1.2f));
				g.DrawEllipse(pen, dRect);
			}

			int txtX = Ui.S(8 + 12 * (array.Length - 1) + 16 + 8);
			Ui.DrawText(g, _t.Name, Ui.Body, Ui.Text, new Rectangle(txtX, 0, Width - txtX - Ui.S(6), Height), TextFormatFlags.VerticalCenter);
		}

		protected override void OnMouseClick(MouseEventArgs e)
		{
			base.OnMouseClick(e);
			if (e.Button == MouseButtons.Left) _w.ApplyTheme(_t);
		}
	}
}
