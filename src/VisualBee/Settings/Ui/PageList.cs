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

	public int RowHeight => Ui.S(34);

	public int Selected
	{
		get
		{
			return _selected;
		}
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
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		Ui.Smooth(graphics);
		for (int i = 0; i < Pages.Count; i++)
		{
			Rectangle rectangle = new Rectangle(Ui.S(8), i * RowHeight + Ui.S(2), Width - Ui.S(16), RowHeight - Ui.S(4));
			if (i == _selected)
			{
				Ui.Fill(graphics, rectangle, Ui.Sf(6f), Ui.Hover);
				Ui.Fill(graphics, new RectangleF(rectangle.X, (float)rectangle.Y + Ui.Sf(8f), Ui.Sf(3f), (float)rectangle.Height - Ui.Sf(16f)), Ui.Sf(1.5f), Ui.Accent);
			}
			else if (i == _hot)
			{
				Ui.Fill(graphics, rectangle, Ui.Sf(6f), Ui.Card);
			}
			Ui.DrawText(graphics, Pages[i], (i == _selected) ? Ui.Bold : Ui.Body, (i == _selected) ? Ui.Text : Ui.Dim, new Rectangle(rectangle.X + Ui.S(14), rectangle.Y, rectangle.Width - Ui.S(30), rectangle.Height), TextFormatFlags.VerticalCenter);
			if (Marked.Contains(Pages[i]))
			{
				using SolidBrush brush = new SolidBrush(Ui.Accent);
				graphics.FillEllipse(brush, (float)rectangle.Right - Ui.Sf(14f), (float)rectangle.Y + (float)rectangle.Height / 2f - Ui.Sf(3f), Ui.Sf(6f), Ui.Sf(6f));
			}
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		int num = e.Y / RowHeight;
		if (num >= Pages.Count)
		{
			num = -1;
		}
		if (num != _hot)
		{
			_hot = num;
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
		int num = e.Y / RowHeight;
		if (num >= 0 && num < Pages.Count && num != _selected)
		{
			Selected = num;
			if (Changed != null)
			{
				Changed(this, EventArgs.Empty);
			}
		}
	}
}
