using System;
using System.Drawing;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed partial class SettingsWindow : Form
{
	private sealed class SidebarStatusCard : Widget
	{
		private string _preset = "";
		private bool _modified;
		private int _diffCount;

		public SidebarStatusCard()
		{
			Height = Ui.S(60);
			Cursor = Cursors.Hand;
		}

		public void SetState(string presetName, bool modified, int diffCount)
		{
			string p = presetName ?? "";
			if (_preset == p && _modified == modified && _diffCount == diffCount) return;
			_preset = p;
			_modified = modified;
			_diffCount = diffCount;
			Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.Clear(Ui.Side);
			Ui.Smooth(g);

			RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
			Color cardBg = Down ? Ui.Raised : (Hot ? Ui.Hover : Ui.Card);
			Color cardBorder = Hot ? Ui.AccentBorder : Ui.Border;
			Ui.Fill(g, rect, Ui.Sf(7f), cardBg);
			Ui.Stroke(g, rect, Ui.Sf(7f), cardBorder);

			Ui.DrawText(g, "ACTIVE LOOK", Ui.Section, Ui.Faint, new Rectangle(Ui.S(10), Ui.S(7), Width - Ui.S(34), Ui.S(14)), TextFormatFlags.VerticalCenter);
			if (Hot)
			{
				Ui.Glyph(g, Ui.GlyphShare, Ui.Icons, Ui.Accent, new Rectangle(Width - Ui.S(24), Ui.S(5), Ui.S(16), Ui.S(16)));
			}

			string title = (_preset.Length > 0) ? _preset : "Custom Look";
			Ui.DrawText(g, title, Ui.Bold, Ui.Text, new Rectangle(Ui.S(10), Ui.S(21), Width - Ui.S(20), Ui.S(18)), TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

			Color dotCol;
			string status;
			if (_preset.Length > 0 && !_modified)
			{
				dotCol = Ui.Accent;
				status = "Preset intact";
			}
			else if (_diffCount > 0)
			{
				dotCol = Ui.Amber;
				status = _diffCount + ((_diffCount == 1) ? " modified" : " modified");
			}
			else
			{
				dotCol = Ui.Dim;
				status = "All defaults";
			}

			using (SolidBrush b = new SolidBrush(dotCol))
			{
				g.FillEllipse(b, Ui.Sf(10f), Ui.Sf(44f), Ui.Sf(6f), Ui.Sf(6f));
			}
			Ui.DrawText(g, status, Ui.Small, (_modified || (_preset.Length == 0 && _diffCount > 0)) ? Ui.Amber : Ui.Dim, new Rectangle(Ui.S(20), Ui.S(39), Width - Ui.S(28), Ui.S(16)), TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
		}
	}

	private sealed class AudioTelemetryCard : Widget
	{
		private string _line1 = "Audio idle";
		private string _line2 = "Waiting for engine";

		public AudioTelemetryCard()
		{
			Height = Ui.S(60);
		}

		public bool UpdateText(string raw)
		{
			string s = (raw ?? "").Trim();
			string l1 = s;
			string l2 = "";
			int sep = s.IndexOf('|');
			if (sep < 0) sep = s.IndexOf('\n');
			if (sep >= 0)
			{
				l1 = s.Substring(0, sep).Trim();
				l2 = s.Substring(sep + 1).Replace('\n', ' ').Replace('\r', ' ').Trim();
			}
			if (l1.Length == 0) l1 = "Audio idle";
			if (_line1 == l1 && _line2 == l2) return false;
			_line1 = l1;
			_line2 = l2;
			Invalidate();
			return true;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.Clear(Ui.Side);
			Ui.Smooth(g);

			RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
			Ui.Fill(g, rect, Ui.Sf(7f), Ui.Back);
			Ui.Stroke(g, rect, Ui.Sf(7f), Ui.Border);

			using (SolidBrush b = new SolidBrush(Ui.Accent))
			{
				g.FillEllipse(b, Ui.Sf(10f), Ui.Sf(11f), Ui.Sf(5f), Ui.Sf(5f));
			}
			Ui.DrawText(g, "ENGINE TELEMETRY", Ui.Section, Ui.Faint, new Rectangle(Ui.S(19), Ui.S(6), Width - Ui.S(28), Ui.S(14)), TextFormatFlags.VerticalCenter);
			Ui.DrawText(g, _line1, Ui.Mono, Ui.Text, new Rectangle(Ui.S(10), Ui.S(22), Width - Ui.S(18), Ui.S(16)), TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
			if (_line2.Length > 0)
			{
				Ui.DrawText(g, _line2, Ui.Mono, Ui.Dim, new Rectangle(Ui.S(10), Ui.S(38), Width - Ui.S(18), Ui.S(16)), TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
			}
		}
	}

	private sealed class SectionHeaderBar : Panel
	{
		private readonly Label _title = new Label();
		private readonly Label _subtitle = new Label();
		private readonly FlatButton[] _actions;

		public SectionHeaderBar(string title, string subtitle = null, params FlatButton[] actions)
		{
			BackColor = Ui.Back;
			_actions = actions ?? new FlatButton[0];
			bool hasSub = !string.IsNullOrEmpty(subtitle);
			Height = hasSub ? Ui.S(48) : Ui.S(34);

			_title.Text = title;
			_title.Font = Ui.SubHeading;
			_title.ForeColor = Ui.Text;
			_title.UseMnemonic = false;
			_title.AutoEllipsis = true;
			_title.AutoSize = false;
			Controls.Add(_title);

			if (hasSub)
			{
				_subtitle.Text = subtitle;
				_subtitle.Font = Ui.Small;
				_subtitle.ForeColor = Ui.Dim;
				_subtitle.UseMnemonic = false;
				_subtitle.AutoEllipsis = true;
				_subtitle.AutoSize = false;
				Controls.Add(_subtitle);
			}

			foreach (FlatButton b in _actions)
			{
				Controls.Add(b);
			}
		}

		protected override void OnLayout(LayoutEventArgs e)
		{
			base.OnLayout(e);
			int right = Width;
			for (int i = _actions.Length - 1; i >= 0; i--)
			{
				FlatButton b = _actions[i];
				b.Width = b.Ideal();
				right -= b.Width;
				b.Location = new Point(right, (Height - b.Height) / 2);
				right -= Ui.S(6);
			}

			int textW = Math.Max(0, right - Ui.S(8));
			if (_subtitle.Parent == this)
			{
				_title.SetBounds(Ui.S(2), Ui.S(4), textW, Ui.S(22));
				_subtitle.SetBounds(Ui.S(2), Ui.S(26), textW, Ui.S(18));
			}
			else
			{
				_title.SetBounds(Ui.S(2), Ui.S(6), textW, Ui.S(24));
			}
		}
	}

	private sealed class EmptyStateCard : Widget
	{
		private readonly string _glyph;
		private readonly string _message;

		public EmptyStateCard(string glyph, string message)
		{
			_glyph = glyph;
			_message = message;
			Height = Ui.S(58);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.Clear(Ui.Back);
			Ui.Smooth(g);
			RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
			Ui.Fill(g, rect, Ui.Sf(8f), Ui.Card);
			Ui.Stroke(g, rect, Ui.Sf(8f), Ui.Border);
			Ui.Glyph(g, _glyph, Ui.IconsLarge, Ui.Faint, new Rectangle(Ui.S(14), 0, Ui.S(24), Height));
			Ui.DrawText(g, _message, Ui.Body, Ui.Dim, new Rectangle(Ui.S(44), Ui.S(6), Math.Max(1, Width - Ui.S(58)), Math.Max(1, Height - Ui.S(12))), TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
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
			Height = Ui.S(108);
			Width = Ui.S(240);
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
			bool isMod = isCurr && _w.Store.Modified;
			RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
			Color bg = Down ? Ui.Raised : (Hot ? Ui.Hover : Ui.Card);
			Color border = isCurr ? (isMod ? Ui.Amber : Ui.Accent) : (Hot ? Ui.Specular : Ui.Border);
			Ui.Fill(g, rect, Ui.Sf(8f), bg);
			Ui.Stroke(g, rect, Ui.Sf(8f), border, isCurr ? Ui.Sf(1.5f) : 1f);

			int[] swatch = _w.Swatch(_p);
			float dotSize = Ui.Sf(13f);
			float step = Ui.Sf(10f);
			float startX = Width - Ui.Sf(12f) - (dotSize + Math.Max(0, swatch.Length - 1) * step);
			for (int i = 0; i < swatch.Length; i++)
			{
				RectangleF dRect = new RectangleF(startX + i * step, Ui.Sf(12f), dotSize, dotSize);
				using (SolidBrush b = new SolidBrush(Color.FromArgb(swatch[i] | -16777216)))
				{
					g.FillEllipse(b, dRect);
				}
				using Pen pPen = new Pen(bg, Ui.Sf(1.4f));
				g.DrawEllipse(pPen, dRect);
			}

			Rectangle rTitle = new Rectangle(Ui.S(12), Ui.S(9), Math.Max(1, (int)startX - Ui.S(18)), Ui.S(20));
			Ui.DrawText(g, _p.Name, Ui.Bold, Ui.Text, rTitle, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);

			int bottomReserve = (isCurr || !_p.BuiltIn) ? Ui.S(28) : Ui.S(12);
			Rectangle rDesc = new Rectangle(Ui.S(12), Ui.S(33), Math.Max(1, Width - Ui.S(24)), Math.Max(1, Height - Ui.S(33) - bottomReserve));
			string desc = _p.Description.Length > 0 ? _p.Description : (_p.BuiltIn ? "" : "Custom user preset (right-click to manage)");
			Ui.DrawText(g, desc, Ui.Small, Ui.Dim, rDesc, TextFormatFlags.EndEllipsis | TextFormatFlags.WordBreak);

			if (!_p.BuiltIn)
			{
				Ui.DrawText(g, "USER", Ui.SmallBold, Ui.Faint, new Rectangle(Ui.S(12), Height - Ui.S(22), Ui.S(50), Ui.S(14)), TextFormatFlags.VerticalCenter);
			}

			if (isCurr)
			{
				string tag = isMod ? "MODIFIED" : "ACTIVE";
				Color tagCol = isMod ? Ui.Amber : Ui.Accent;
				Color tagBg = isMod ? Ui.AmberSoft : Ui.AccentSoft;
				int tagW = TextRenderer.MeasureText(tag, Ui.SmallBold, Size.Empty, TextFormatFlags.NoPrefix).Width + Ui.S(12);
				int tagH = Ui.S(17);
				Rectangle pill = new Rectangle(Width - tagW - Ui.S(10), Height - tagH - Ui.S(8), tagW, tagH);
				Ui.Fill(g, pill, Ui.Sf(8.5f), tagBg);
				Ui.DrawText(g, tag, Ui.SmallBold, tagCol, pill, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
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
			int[] swatch = t.Swatch();
			int swatchSpan = Ui.S(10) + Ui.S(14) + Math.Max(0, swatch.Length - 1) * Ui.S(10);
			int txtW = TextRenderer.MeasureText(t.Name, Ui.Body, Size.Empty, TextFormatFlags.NoPrefix).Width;
			Height = Ui.S(36);
			Width = swatchSpan + Ui.S(10) + txtW + Ui.S(16);
			Cursor = Cursors.Hand;
			w._tips.SetToolTip(this, "Apply colour palette: " + t.Name);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.Clear(Ui.Back);
			Ui.Smooth(g);
			RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
			Color c = Down ? Ui.Raised : (Hot ? Ui.Hover : Ui.Card);
			Ui.Fill(g, rect, Ui.Sf(17f), c);
			Ui.Stroke(g, rect, Ui.Sf(17f), Hot ? Ui.AccentBorder : Ui.Border, 1f);

			int[] swatch = _t.Swatch();
			float dot = Ui.Sf(14f);
			float step = Ui.Sf(10f);
			for (int i = 0; i < swatch.Length; i++)
			{
				RectangleF dRect = new RectangleF(Ui.Sf(10f) + i * step, (Height - dot) / 2f, dot, dot);
				using (SolidBrush brush = new SolidBrush(Color.FromArgb(swatch[i])))
				{
					g.FillEllipse(brush, dRect);
				}
				using Pen pen = new Pen(c, Ui.Sf(1.2f));
				g.DrawEllipse(pen, dRect);
			}

			int txtX = Ui.S(10 + 14 + Math.Max(0, swatch.Length - 1) * 10 + 10);
			Ui.DrawText(g, _t.Name, Ui.Body, Hot ? Ui.Text : Ui.Dim, new Rectangle(txtX, 0, Math.Max(1, Width - txtX - Ui.S(10)), Height), TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
		}

		protected override void OnMouseClick(MouseEventArgs e)
		{
			base.OnMouseClick(e);
			if (e.Button == MouseButtons.Left) _w.ApplyTheme(_t);
		}
	}
}
