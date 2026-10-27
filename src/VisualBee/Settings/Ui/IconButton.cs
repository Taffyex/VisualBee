using System.Drawing;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed class IconButton : Widget
{
	public string Glyph;

	public Color Colour = Ui.Dim;

	public bool Passive;

	public IconButton(string glyph)
	{
		Glyph = glyph;
		Size = new Size(Ui.S(26), Ui.S(26));
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		Ui.Smooth(graphics);
		if (Hot && Enabled && !Passive)
		{
			using SolidBrush brush = new SolidBrush(Down ? Ui.Pressed : Ui.RaisedHover);
			graphics.FillEllipse(brush, 1, 1, Width - 2, Height - 2);
		}
		Graphics g = graphics;
		string glyph = Glyph;
		Font icons = Ui.Icons;
		Color colour;
		if (Enabled)
		{
			colour = (Hot ? Ui.Text : Colour);
		}
		else
		{
			colour = Color.FromArgb(70, Colour);
		}
		Ui.Glyph(g, glyph, icons, colour, ClientRectangle);
	}
}
