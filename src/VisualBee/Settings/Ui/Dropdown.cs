using System;
using System.Drawing;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed class Dropdown : Widget
{
	private readonly string[] _items;

	private int _selected;

	private readonly ContextMenuStrip _menu = new ContextMenuStrip();

	public int Selected
	{
		get
		{
			return _selected;
		}
		set
		{
			_selected = Math.Max(0, Math.Min(_items.Length - 1, value));
			Invalidate();
		}
	}

	public event EventHandler Changed;

	public Dropdown(string[] items)
	{
		_items = items;
		Height = Ui.S(26);
		Cursor = Cursors.Hand;
		_menu.Renderer = new DarkMenuRenderer();
		_menu.ShowImageMargin = false;
		_menu.ShowCheckMargin = true;
		_menu.Font = Ui.Body;
		for (int i = 0; i < items.Length; i++)
		{
			int index = i;
			ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem(items[i])
			{
				ForeColor = Ui.Text
			};
			toolStripMenuItem.Click += (object s, EventArgs a) =>
			{
				if (index != _selected)
				{
					Selected = index;
					if (Changed != null)
					{
						Changed(this, EventArgs.Empty);
					}
				}
			};
			_menu.Items.Add(toolStripMenuItem);
		}
	}

	public int Ideal()
	{
		int num = 0;
		string[] items = _items;
		foreach (string text in items)
		{
			num = Math.Max(num, TextRenderer.MeasureText(text, Ui.Body).Width);
		}
		return num + Ui.S(42);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		Ui.Smooth(graphics);
		RectangleF rectangleF = new RectangleF(0.5f, 0.5f, (float)Width - 1f, (float)Height - 1f);
		Graphics g = graphics;
		RectangleF r = rectangleF;
		float radius = Ui.Sf(5f);
		Color c;
		if (Down)
		{
			c = Ui.Pressed;
		}
		else
		{
			c = ((Hot && Enabled) ? Ui.RaisedHover : Ui.Raised);
		}
		Ui.Fill(g, r, radius, c);
		Ui.Stroke(graphics, rectangleF, Ui.Sf(5f), Ui.Border);
		Ui.DrawText(r: new Rectangle(Ui.S(9), 0, Width - Ui.S(34), Height), g: graphics, text: _items[_selected], font: Ui.Body, colour: Enabled ? Ui.Text : Ui.Faint, flags: TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
		Ui.Glyph(graphics, "\ue70d", Ui.Icons, Enabled ? Ui.Dim : Ui.Faint, new Rectangle(Width - Ui.S(28), 0, Ui.S(22), Height));
	}

	protected override void OnClick(EventArgs e)
	{
		base.OnClick(e);
		for (int i = 0; i < _menu.Items.Count; i++)
		{
			((ToolStripMenuItem)_menu.Items[i]).Checked = i == _selected;
		}
		_menu.MinimumSize = new Size(Width, 0);
		_menu.Show(this, new Point(0, Height));
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_menu.Dispose();
		}
		base.Dispose(disposing);
	}
}
