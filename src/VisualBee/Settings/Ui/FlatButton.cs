using System;
using System.Drawing;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed class FlatButton : Widget
{
	public string Glyph;

	public bool Primary;

	public FlatButton(string text, string glyph = null)
	{
		Text = text;
		Glyph = glyph;
		Cursor = Cursors.Hand;
		Height = Ui.S(30);
		Width = Ideal();
	}

	public int Ideal()
	{
		return TextRenderer.MeasureText(Text, Ui.Body).Width + Ui.S((Glyph != null) ? 44 : 26);
	}

	protected override void OnTextChanged(EventArgs e)
	{
		base.OnTextChanged(e);
		Invalidate();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		Ui.Smooth(graphics);
		RectangleF r = new RectangleF(0.5f, 0.5f, (float)Width - 1f, (float)Height - 1f);
		Color c;
		if (Primary)
		{
			if (Down)
			{
				c = Ui.Mix(Ui.Accent, Color.Black, 0.2);
			}
			else
			{
				c = (Hot ? Ui.Mix(Ui.Accent, Color.White, 0.12) : Ui.Accent);
			}
		}
		else if (Down)
		{
			c = Ui.Pressed;
		}
		else
		{
			c = (Hot ? Ui.RaisedHover : Ui.Raised);
		}
		if (!Enabled)
		{
			c = Ui.Card;
		}
		Ui.Fill(graphics, r, Ui.Sf(6f), c);
		if (!Primary)
		{
			Ui.Stroke(graphics, r, Ui.Sf(6f), Ui.Border);
		}
		Color colour;
		if (Enabled)
		{
			colour = (Primary ? Color.White : Ui.Text);
		}
		else
		{
			colour = Ui.Faint;
		}
		int num = Ui.S(13);
		if (Glyph != null)
		{
			Ui.Glyph(graphics, Glyph, Ui.Icons, colour, new Rectangle(Ui.S(9), 0, Ui.S(20), Height));
			num = Ui.S(32);
		}
		Ui.DrawText(graphics, Text, Ui.Body, colour, new Rectangle(num, 0, Width - num - Ui.S(8), Height), TextFormatFlags.VerticalCenter);
	}
}
