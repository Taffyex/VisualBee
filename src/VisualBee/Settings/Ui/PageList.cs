using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed class PageList : Widget
{
	public readonly List<string> Pages = new List<string>();

	public readonly Dictionary<string, int> ModifiedCounts = new Dictionary<string, int>(StringComparer.Ordinal);

	private int _selected;

	private int _hot = -1;

	public int Selected
	{
		get => _selected;
		set
		{
			if (_selected != value)
			{
				_selected = value;
				Invalidate();
			}
		}
	}

	public event EventHandler Changed;

	public PageList()
	{
		BackColor = Ui.Side;
		Cursor = Cursors.Hand;
	}

	private Rectangle ItemRect(int index)
	{
		int rowH = Ui.S(38);
		int gap = Ui.S(4);
		int padX = Ui.S(10);
		return new Rectangle(padX, Ui.S(4) + index * (rowH + gap), Math.Max(1, Width - padX * 2), rowH);
	}

	private static string PageGlyph(string page)
	{
		return page switch
		{
			"Presets" => Ui.GlyphPresets,
			"Studio" => Ui.GlyphStudio,
			"Optics & FX" => Ui.GlyphOptics,
			"Colours" => Ui.GlyphPalette,
			"Audio" => Ui.GlyphAudio,
			"Audio & System" => Ui.GlyphAudio,
			_ => Ui.GlyphSparkle,
		};
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		g.Clear(Ui.Side);
		Ui.Smooth(g);

		for (int i = 0; i < Pages.Count; i++)
		{
			Rectangle r = ItemRect(i);
			bool isSel = i == _selected;
			bool isHot = i == _hot;

			if (isSel)
			{
				Ui.Fill(g, r, Ui.Sf(6f), Ui.Card);
				Ui.Stroke(g, new RectangleF(r.X + 0.5f, r.Y + 0.5f, r.Width - 1f, r.Height - 1f), Ui.Sf(6f), Ui.Border, 1f);
				Ui.Fill(g, new RectangleF(r.X + Ui.Sf(1f), r.Y + Ui.Sf(7f), Ui.Sf(3f), r.Height - Ui.Sf(14f)), Ui.Sf(1.5f), Ui.Accent);
			}
			else if (isHot)
			{
				Ui.Fill(g, r, Ui.Sf(6f), Ui.Hover);
			}

			Color glyphCol = isSel ? Ui.Accent : (isHot ? Ui.Text : Ui.Dim);
			Ui.Glyph(g, PageGlyph(Pages[i]), Ui.Icons, glyphCol, new Rectangle(r.X + Ui.S(10), r.Y, Ui.S(22), r.Height));

			int count = ModifiedCounts.TryGetValue(Pages[i], out int c) ? c : 0;
			int pillReserve = (count > 0) ? Ui.S(36) : Ui.S(8);
			Rectangle textRect = new Rectangle(r.X + Ui.S(36), r.Y, Math.Max(1, r.Width - Ui.S(36) - pillReserve), r.Height);
			Ui.DrawText(g, Pages[i], isSel ? Ui.Bold : Ui.Body, (isSel || isHot) ? Ui.Text : Ui.Dim, textRect, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

			if (count > 0)
			{
				string countText = count.ToString();
				int pillW = Math.Max(Ui.S(22), TextRenderer.MeasureText(countText, Ui.SmallBold, Size.Empty, TextFormatFlags.NoPrefix).Width + Ui.S(10));
				int pillH = Ui.S(18);
				Rectangle pillRect = new Rectangle(r.Right - pillW - Ui.S(8), r.Y + (r.Height - pillH) / 2, pillW, pillH);
				Ui.Fill(g, pillRect, Ui.Sf(9f), Ui.AccentSoft);
				Ui.Stroke(g, new RectangleF(pillRect.X + 0.5f, pillRect.Y + 0.5f, pillRect.Width - 1f, pillRect.Height - 1f), Ui.Sf(9f), Ui.AccentBorder);
				Ui.DrawText(g, countText, Ui.SmallBold, Ui.Accent, pillRect, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
			}
		}
	}

	private int HitIndex(Point pt)
	{
		for (int i = 0; i < Pages.Count; i++)
		{
			if (ItemRect(i).Contains(pt)) return i;
		}
		return -1;
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		int hit = HitIndex(e.Location);
		if (hit != _hot)
		{
			_hot = hit;
			Invalidate();
		}
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		_hot = -1;
		Invalidate();
	}

	protected override void OnMouseClick(MouseEventArgs e)
	{
		base.OnMouseClick(e);
		int hit = HitIndex(e.Location);
		if (hit >= 0 && hit < Pages.Count && hit != _selected)
		{
			Selected = hit;
			Changed?.Invoke(this, EventArgs.Empty);
		}
	}
}
