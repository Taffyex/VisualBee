using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed class PageList : Widget
{
	public readonly List<string> Pages = new List<string>();

	public readonly HashSet<string> Marked = new HashSet<string>();

	private int _selected;

	private int _hot = -1;

	public int TabHeight => Ui.S(38);

	public int Selected
	{
		get => _selected;
		set
		{
			_selected = value;
			Invalidate();
		}
	}

	public event EventHandler Changed;

	public PageList()
	{
		Cursor = Cursors.Hand;
		Height = Ui.S(38);
	}

	private Rectangle TabRect(int index)
	{
		int count = Math.Max(1, Pages.Count);
		int tabW = Math.Max(Ui.S(60), (Width - Ui.S(16)) / count);
		int x = Ui.S(8) + index * tabW;
		return new Rectangle(x, Ui.S(3), tabW - Ui.S(4), Height - Ui.S(6));
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		Ui.Smooth(g);

		for (int i = 0; i < Pages.Count; i++)
		{
			Rectangle r = TabRect(i);
			bool isSel = (i == _selected);
			bool isHot = (i == _hot);

			if (isSel)
			{
				Ui.Fill(g, r, Ui.Sf(6f), Ui.Card);
				Ui.Stroke(g, new RectangleF(r.X, r.Y, r.Width, r.Height), Ui.Sf(6f), Ui.Border, 1f);
				Ui.Fill(g, new RectangleF(r.X + Ui.Sf(6f), r.Bottom - Ui.Sf(2.5f), r.Width - Ui.Sf(12f), Ui.Sf(2.5f)), Ui.Sf(1.25f), Ui.Accent);
			}
			else if (isHot)
			{
				Ui.Fill(g, r, Ui.Sf(6f), Ui.Hover);
			}

			Color txtCol = isSel ? Ui.Text : (isHot ? Ui.Text : Ui.Dim);
			Font font = isSel ? Ui.Bold : Ui.Body;
			Ui.DrawText(g, Pages[i], font, txtCol, r, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

			if (Marked.Contains(Pages[i]))
			{
				using SolidBrush b = new SolidBrush(Ui.Accent);
				g.FillEllipse(b, (float)r.Right - Ui.Sf(10f), (float)r.Y + Ui.Sf(6f), Ui.Sf(5f), Ui.Sf(5f));
			}
		}
	}

	private int HitIndex(Point pt)
	{
		for (int i = 0; i < Pages.Count; i++)
		{
			if (TabRect(i).Contains(pt)) return i;
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
