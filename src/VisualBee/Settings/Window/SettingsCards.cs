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

	private sealed class SectionHeader : Widget
	{
		private readonly SettingsWindow _w;

		private readonly Func<Setting, bool> _members;

		private readonly LinkLabel _reset = new LinkLabel();
		public bool IsHeaderVisible = true;

		public SectionHeader(SettingsWindow w, string text, Func<Setting, bool> members)
		{
			_w = w;
			_members = members;
			Text = text;
			Height = Ui.S(38);
			if (members != null)
			{
				_reset.Text = "Reset section";
				_reset.Font = Ui.Small;
				_reset.AutoSize = true;
				_reset.LinkColor = Ui.Accent;
				_reset.ActiveLinkColor = Ui.Text;
				_reset.LinkBehavior = LinkBehavior.HoverUnderline;
				_reset.BackColor = Ui.Back;
				_reset.LinkClicked += (object s, LinkLabelLinkClickedEventArgs a) =>
				{
					_w.ResetMembers(_members);
				};
				Controls.Add(_reset);
			}
		}

		public void ShowState(Look l)
		{
			if (_members != null)
			{
				IsHeaderVisible = !_w._hideInactive || !string.IsNullOrEmpty(_w._search.Box.Text.Trim()) || _w._store.Schema.All.Any((Setting s) => _members(s) && s.IsEnabled(l));
				Visible = IsHeaderVisible;
				_reset.Visible = _w._store.Schema.All.Any((Setting s) => _members(s) && !s.IsDefault(l));
			}
		}

		protected override void OnLayout(LayoutEventArgs e)
		{
			base.OnLayout(e);
			_reset.Location = new Point(Width - _reset.Width - Ui.S(22), Height - _reset.Height - Ui.S(6));
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.Clear(Ui.Back);
			Ui.DrawText(r: new Rectangle(Ui.S(16), 0, Width - Ui.S(140), Height - Ui.S(6)), g: e.Graphics, text: Text.ToUpperInvariant(), font: Ui.Section, colour: Ui.Accent, flags: TextFormatFlags.Bottom | TextFormatFlags.EndEllipsis);
			using Pen pen = new Pen(Ui.Border);
			e.Graphics.DrawLine(pen, Ui.S(16), Height - 1, Width - Ui.S(16), Height - 1);
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
			Height = Ui.S(78);
			Width = Ui.S(230);
			Cursor = Cursors.Hand;
			if (p.Description.Length > 0)
			{
				w._tips.SetToolTip(this, p.Description);
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics graphics = e.Graphics;
			graphics.Clear(Ui.Back);
			Ui.Smooth(graphics);
			bool flag = _w.IsCurrent(_p);
			RectangleF rectangleF = new RectangleF(0.5f, 0.5f, (float)Width - 1f, (float)Height - 1f);
			Graphics g = graphics;
			RectangleF r = rectangleF;
			float radius = Ui.Sf(8f);
			Color c;
			if (Down)
			{
				c = Ui.Raised;
			}
			else
			{
				c = (Hot ? Ui.Hover : Ui.Card);
			}
			Ui.Fill(g, r, radius, c);
			Ui.Stroke(graphics, rectangleF, Ui.Sf(8f), flag ? Ui.Accent : Ui.Border, flag ? Ui.Sf(2f) : 1f);
			int[] array = _w.Swatch(_p);
			float num = Ui.Sf(16f);
			float num2 = (float)Width - Ui.Sf(12f) - (float)array.Length * (num - Ui.Sf(4f)) - Ui.Sf(4f);
			for (int i = 0; i < array.Length; i++)
			{
				RectangleF rect = new RectangleF(num2 + (float)i * (num - Ui.Sf(4f)), Ui.Sf(12f), num, num);
				using (SolidBrush brush = new SolidBrush(Color.FromArgb(array[i] | -16777216)))
				{
					graphics.FillEllipse(brush, rect);
				}
				using Pen pen = new Pen(Ui.Card, Ui.Sf(1.5f));
				graphics.DrawEllipse(pen, rect);
			}
			Ui.DrawText(r: new Rectangle(Ui.S(12), Ui.S(9), (int)num2 - Ui.S(16), Ui.S(22)), g: graphics, text: _p.Name, font: Ui.Bold, colour: Ui.Text, flags: TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
			Rectangle r2 = new Rectangle(Ui.S(12), Ui.S(32), Width - Ui.S(24), Height - Ui.S(38));
			string text;
			if (_p.Description.Length > 0)
			{
				text = _p.Description;
			}
			else
			{
				text = (_p.BuiltIn ? "" : "Your look · right-click for more");
			}
			Ui.DrawText(graphics, text, Ui.Small, Ui.Dim, r2, TextFormatFlags.EndEllipsis | TextFormatFlags.WordBreak);
			if (flag)
			{
				Ui.Glyph(graphics, "\ue73e", Ui.Icons, Ui.Accent, new Rectangle(Width - Ui.S(30), Height - Ui.S(28), Ui.S(22), Ui.S(22)));
			}
		}

		protected override void OnMouseClick(MouseEventArgs e)
		{
			base.OnMouseClick(e);
			if (e.Button == MouseButtons.Left)
			{
				_w.ApplyPreset(_p);
			}
			else if (e.Button == MouseButtons.Right)
			{
				_w.PresetMenu(_p, this, e.Location);
			}
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
			Height = Ui.S(40);
			Width = TextRenderer.MeasureText(t.Name, Ui.Body).Width + Ui.S(104);
			Cursor = Cursors.Hand;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics graphics = e.Graphics;
			graphics.Clear(Ui.Back);
			Ui.Smooth(graphics);
			RectangleF rectangleF = new RectangleF(0.5f, 0.5f, (float)Width - 1f, (float)Height - 1f);
			Graphics g = graphics;
			RectangleF r = rectangleF;
			float radius = Ui.Sf(20f);
			Color c;
			if (Down)
			{
				c = Ui.Raised;
			}
			else
			{
				c = (Hot ? Ui.Hover : Ui.Card);
			}
			Ui.Fill(g, r, radius, c);
			Ui.Stroke(graphics, rectangleF, Ui.Sf(20f), Ui.Border);
			int[] array = _t.Swatch();
			float num = Ui.Sf(18f);
			for (int i = 0; i < array.Length; i++)
			{
				RectangleF rect = new RectangleF(Ui.Sf(10f) + (float)i * Ui.Sf(13f), ((float)Height - num) / 2f, num, num);
				using (SolidBrush brush = new SolidBrush(Color.FromArgb(array[i])))
				{
					graphics.FillEllipse(brush, rect);
				}
				using Pen pen = new Pen(Ui.Card, Ui.Sf(1.5f));
				graphics.DrawEllipse(pen, rect);
			}
			int num2 = Ui.S(10 + 13 * (array.Length - 1) + 18 + 10);
			Ui.DrawText(graphics, _t.Name, Ui.Body, Ui.Text, new Rectangle(num2, 0, Width - num2 - Ui.S(8), Height), TextFormatFlags.VerticalCenter);
		}

		protected override void OnMouseClick(MouseEventArgs e)
		{
			base.OnMouseClick(e);
			if (e.Button == MouseButtons.Left)
			{
				_w.ApplyTheme(_t);
			}
		}
	}

}
