using System;
using System.Drawing;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed class Segmented : Widget
{
	private readonly string[] _items;

	private int _selected;

	private int _hot = -1;

	public int Selected
	{
		get => _selected;
		set
		{
			if (value != _selected)
			{
				_selected = value;
				Invalidate();
			}
		}
	}

	public event EventHandler Changed;

	public Segmented(string[] items)
	{
		_items = items;
		Height = Ui.S(28);
		Cursor = Cursors.Hand;
	}

	public int Ideal()
	{
		int num = 0;
		foreach (string text in _items)
		{
			num += TextRenderer.MeasureText(text, Ui.Bold, Size.Empty, TextFormatFlags.NoPrefix).Width + Ui.S(20);
		}
		return num + Ui.S(6);
	}

	private Rectangle Cell(int i)
	{
		int innerW = Width - Ui.S(6);
		int total = 0;
		int offset = 0;
		for (int j = 0; j < _items.Length; j++)
		{
			int itemW = TextRenderer.MeasureText(_items[j], Ui.Bold, Size.Empty, TextFormatFlags.NoPrefix).Width + Ui.S(20);
			if (j < i) offset += itemW;
			total += itemW;
		}
		int curW = TextRenderer.MeasureText(_items[i], Ui.Bold, Size.Empty, TextFormatFlags.NoPrefix).Width + Ui.S(20);
		int x1 = Ui.S(3) + innerW * offset / Math.Max(1, total);
		int x2 = Ui.S(3) + innerW * (offset + curW) / Math.Max(1, total);
		return new Rectangle(x1, Ui.S(3), Math.Max(1, x2 - x1), Height - Ui.S(6));
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		Ui.Smooth(g);
		RectangleF outer = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
		Ui.Fill(g, outer, Ui.Sf(6f), Ui.Raised);
		Ui.Stroke(g, outer, Ui.Sf(6f), Ui.Border);

		for (int i = 0; i < _items.Length; i++)
		{
			Rectangle cell = Cell(i);
			if (i == _selected)
			{
				Ui.Fill(g, cell, Ui.Sf(4f), Enabled ? Ui.Accent : Ui.AccentDeep);
			}
			else if (i == _hot && Enabled)
			{
				Ui.Fill(g, cell, Ui.Sf(4f), Ui.RaisedHover);
			}

			Color colour = (i == _selected)
				? (Enabled ? Ui.AccentText : Ui.Dim)
				: (Enabled ? Ui.Text : Ui.Faint);

			Ui.DrawText(g, _items[i], (i == _selected) ? Ui.Bold : Ui.Body, colour, cell, TextFormatFlags.EndEllipsis | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		int hit = -1;
		for (int i = 0; i < _items.Length; i++)
		{
			if (Cell(i).Contains(e.Location)) hit = i;
		}
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
		for (int i = 0; i < _items.Length; i++)
		{
			if (Cell(i).Contains(e.Location) && i != _selected)
			{
				Selected = i;
				Changed?.Invoke(this, EventArgs.Empty);
			}
		}
	}
}
