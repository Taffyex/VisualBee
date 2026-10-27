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
		get
		{
			return _selected;
		}
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
		Height = Ui.S(26);
		Cursor = Cursors.Hand;
	}

	public int Ideal()
	{
		int num = 0;
		string[] items = _items;
		foreach (string text in items)
		{
			num += TextRenderer.MeasureText(text, Ui.Bold).Width + Ui.S(18);
		}
		return num + Ui.S(4);
	}

	private Rectangle Cell(int i)
	{
		int num = Width - Ui.S(4);
		int num2 = 0;
		int num3 = 0;
		for (int j = 0; j < _items.Length; j++)
		{
			int num4 = TextRenderer.MeasureText(_items[j], Ui.Bold).Width + Ui.S(18);
			if (j < i)
			{
				num3 += num4;
			}
			num2 += num4;
		}
		int num5 = TextRenderer.MeasureText(_items[i], Ui.Bold).Width + Ui.S(18);
		int num6 = Ui.S(2) + num * num3 / num2;
		int num7 = Ui.S(2) + num * (num3 + num5) / num2;
		return new Rectangle(num6, Ui.S(2), num7 - num6, Height - Ui.S(4));
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		Ui.Smooth(graphics);
		Ui.Fill(graphics, new RectangleF(0.5f, 0.5f, (float)Width - 1f, (float)Height - 1f), Ui.Sf(6f), Ui.Raised);
		for (int i = 0; i < _items.Length; i++)
		{
			Rectangle rectangle = Cell(i);
			if (i == _selected)
			{
				Ui.Fill(graphics, rectangle, Ui.Sf(5f), Enabled ? Ui.Accent : Ui.AccentDeep);
			}
			else if (i == _hot && Enabled)
			{
				Ui.Fill(graphics, rectangle, Ui.Sf(5f), Ui.RaisedHover);
			}
			Color colour;
			if (i == _selected)
			{
				colour = Color.White;
			}
			else
			{
				colour = (Enabled ? Ui.Text : Ui.Faint);
			}
			Ui.DrawText(graphics, _items[i], (i == _selected) ? Ui.Bold : Ui.Body, colour, rectangle, TextFormatFlags.EndEllipsis | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		int num = -1;
		for (int i = 0; i < _items.Length; i++)
		{
			if (Cell(i).Contains(e.Location))
			{
				num = i;
			}
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
		for (int i = 0; i < _items.Length; i++)
		{
			if (Cell(i).Contains(e.Location) && i != _selected)
			{
				Selected = i;
				if (Changed != null)
				{
					Changed(this, EventArgs.Empty);
				}
			}
		}
	}
}
