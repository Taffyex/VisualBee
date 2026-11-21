using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed class ColourWell : Widget
{
	private int _argb = -16777216;

	public bool Alpha;

	public int Argb
	{
		get => _argb;
		set
		{
			if (value != _argb)
			{
				_argb = value;
				Invalidate();
			}
		}
	}

	public ColourWell()
	{
		Height = Ui.S(28);
		Cursor = Cursors.Hand;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		Ui.Smooth(g);
		RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
		Ui.Fill(g, r, Ui.Sf(5f), (Hot && Enabled) ? Ui.RaisedHover : Ui.Raised);
		Ui.Stroke(g, r, Ui.Sf(5f), (Hot && Enabled) ? Ui.AccentBorder : Ui.Border);

		Rectangle swatch = new Rectangle(Ui.S(4), Ui.S(4), Ui.S(32), Height - Ui.S(8));
		Region clip = g.Clip;
		using (GraphicsPath round = Ui.Round(swatch, Ui.Sf(3f)))
		{
			g.SetClip(round);
			Ui.Checker(g, swatch);
			using (SolidBrush brush = new SolidBrush(Color.FromArgb(Enabled ? _argb : ((_argb & 0xFFFFFF) | 0x60000000))))
			{
				g.FillRectangle(brush, swatch);
			}
			g.Clip = clip;
		}
		Ui.Stroke(g, swatch, Ui.Sf(3f), Color.FromArgb(70, 255, 255, 255));
		Ui.DrawText(g, ColourMath.ToHex(_argb, Alpha), Ui.Mono, Enabled ? Ui.Text : Ui.Faint, new Rectangle(swatch.Right + Ui.S(8), 0, Width - swatch.Right - Ui.S(10), Height), TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
	}
}
