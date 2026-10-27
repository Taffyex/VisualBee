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
		get
		{
			return _argb;
		}
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
		Height = Ui.S(26);
		Cursor = Cursors.Hand;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		Ui.Smooth(graphics);
		RectangleF r = new RectangleF(0.5f, 0.5f, (float)Width - 1f, (float)Height - 1f);
		Ui.Fill(graphics, r, Ui.Sf(5f), (Hot && Enabled) ? Ui.RaisedHover : Ui.Raised);
		Ui.Stroke(graphics, r, Ui.Sf(5f), (Hot && Enabled) ? Ui.Faint : Ui.Border);
		Rectangle rectangle = new Rectangle(Ui.S(4), Ui.S(4), Ui.S(30), Height - Ui.S(8));
		Region clip = graphics.Clip;
		using (GraphicsPath clip2 = Ui.Round(rectangle, Ui.Sf(3f)))
		{
			graphics.SetClip(clip2);
			Ui.Checker(graphics, rectangle);
			using (SolidBrush brush = new SolidBrush(Color.FromArgb(Enabled ? _argb : ((_argb & 0xFFFFFF) | 0x60000000))))
			{
				graphics.FillRectangle(brush, rectangle);
			}
			graphics.Clip = clip;
		}
		Ui.Stroke(graphics, rectangle, Ui.Sf(3f), Color.FromArgb(60, 255, 255, 255));
		Ui.DrawText(r: new Rectangle(rectangle.Right + Ui.S(8), 0, Width - rectangle.Right - Ui.S(10), Height), g: graphics, text: ColourMath.ToHex(_argb, Alpha), font: Ui.Body, colour: Enabled ? Ui.Text : Ui.Faint, flags: TextFormatFlags.VerticalCenter);
	}
}
