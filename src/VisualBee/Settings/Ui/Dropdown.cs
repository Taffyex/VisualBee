using System;
using System.Drawing;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed class Dropdown : Widget
{
	private string[] _items;

	private int _selected;

	private readonly ContextMenuStrip _menu = new ContextMenuStrip();

	public Action BeforeOpen;

	public int Selected
	{
		get => _selected;
		set
		{
			_selected = (_items == null || _items.Length == 0) ? 0 : Math.Max(0, Math.Min(_items.Length - 1, value));
			Invalidate();
		}
	}

	public event EventHandler Changed;

	public Dropdown(string[] items)
	{
		Height = Ui.S(28);
		Cursor = Cursors.Hand;
		_menu.Renderer = new DarkMenuRenderer();
		_menu.ShowImageMargin = false;
		_menu.ShowCheckMargin = true;
		_menu.Font = Ui.Body;
		SetItems(items);
	}

	public void SetItems(string[] items)
	{
		_items = items ?? new string[0];
		_menu.Items.Clear();
		for (int i = 0; i < _items.Length; i++)
		{
			int index = i;
			ToolStripMenuItem item = new ToolStripMenuItem(_items[i])
			{
				ForeColor = Ui.Text
			};
			item.Click += (object s, EventArgs a) =>
			{
				if (index != _selected)
				{
					Selected = index;
					Changed?.Invoke(this, EventArgs.Empty);
				}
			};
			_menu.Items.Add(item);
		}
		if (_items.Length == 0) _selected = 0;
		else if (_selected >= _items.Length) _selected = _items.Length - 1;
		Invalidate();
	}

	public int Ideal()
	{
		int num = 0;
		string[] items = _items ?? new string[0];
		foreach (string text in items)
		{
			num = Math.Max(num, TextRenderer.MeasureText(text, Ui.Body, Size.Empty, TextFormatFlags.NoPrefix).Width);
		}
		return num + Ui.S(46);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		Ui.Smooth(g);
		RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
		Color fill = Down ? Ui.Pressed : ((Hot && Enabled) ? Ui.RaisedHover : Ui.Raised);
		Color border = (Hot && Enabled) ? Ui.Specular : Ui.Border;
		Ui.Fill(g, rect, Ui.Sf(5f), fill);
		Ui.Stroke(g, rect, Ui.Sf(5f), border);

		string text = (_items != null && _selected >= 0 && _selected < _items.Length) ? _items[_selected] : "";
		Ui.DrawText(g, text, Ui.Body, Enabled ? Ui.Text : Ui.Faint, new Rectangle(Ui.S(10), 0, Math.Max(1, Width - Ui.S(36)), Height), TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
		Ui.Glyph(g, Ui.GlyphChevron, Ui.Icons, Enabled ? Ui.Dim : Ui.Faint, new Rectangle(Width - Ui.S(28), 0, Ui.S(22), Height));
	}

	protected override void OnClick(EventArgs e)
	{
		base.OnClick(e);
		BeforeOpen?.Invoke();
		for (int i = 0; i < _menu.Items.Count; i++)
		{
			((ToolStripMenuItem)_menu.Items[i]).Checked = i == _selected;
		}
		_menu.MinimumSize = new Size(Width, 0);
		_menu.Show(this, new Point(0, Height));
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing) _menu.Dispose();
		base.Dispose(disposing);
	}
}
