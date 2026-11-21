using System;
using System.Drawing;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed class FlatButton : Widget
{
	public string Glyph;

	public bool Primary;

	private bool _active;

	public bool Active
	{
		get => _active;
		set
		{
			if (_active != value)
			{
				_active = value;
				Invalidate();
			}
		}
	}

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
		Font f = Primary ? Ui.Bold : Ui.Body;
		return TextRenderer.MeasureText(Text ?? "", f, Size.Empty, TextFormatFlags.NoPrefix).Width + Ui.S((Glyph != null) ? 48 : 30);
	}

	protected override void OnTextChanged(EventArgs e)
	{
		base.OnTextChanged(e);
		Invalidate();
	}

	protected override void OnEnabledChanged(EventArgs e)
	{
		base.OnEnabledChanged(e);
		Cursor = Enabled ? Cursors.Hand : Cursors.Default;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		Ui.Smooth(g);
		RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);

		Color fill;
		Color border = Ui.Border;
		Color textCol;
		Color glyphCol;
		Font font = Primary ? Ui.Bold : Ui.Body;

		if (!Enabled)
		{
			fill = Ui.Card;
			border = Ui.Divider;
			textCol = Ui.Faint;
			glyphCol = Ui.Faint;
		}
		else if (Primary)
		{
			fill = Down ? Ui.Mix(Ui.Accent, Color.Black, 0.18) : (Hot ? Ui.Mix(Ui.Accent, Color.White, 0.14) : Ui.Accent);
			textCol = Ui.AccentText;
			glyphCol = Ui.AccentText;
		}
		else if (_active)
		{
			fill = Down ? Ui.Pressed : (Hot ? Ui.Mix(Ui.Accent, Ui.Card, 0.80) : Ui.AccentSoft);
			border = Ui.AccentBorder;
			textCol = Ui.Text;
			glyphCol = Ui.Accent;
		}
		else
		{
			fill = Down ? Ui.Pressed : (Hot ? Ui.RaisedHover : Ui.Raised);
			border = Hot ? Ui.Specular : Ui.Border;
			textCol = Ui.Text;
			glyphCol = Ui.Dim;
		}

		Ui.Fill(g, r, Ui.Sf(6f), fill);
		if (!Primary)
		{
			Ui.Stroke(g, r, Ui.Sf(6f), border);
		}

		int textLeft = Ui.S(14);
		if (Glyph != null)
		{
			Ui.Glyph(g, Glyph, Ui.Icons, glyphCol, new Rectangle(Ui.S(10), 0, Ui.S(20), Height));
			textLeft = Ui.S(34);
		}
		Ui.DrawText(g, Text, font, textCol, new Rectangle(textLeft, 0, Math.Max(1, Width - textLeft - Ui.S(12)), Height), TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
	}
}
