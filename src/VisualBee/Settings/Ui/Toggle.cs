using System;
using System.Drawing;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed class Toggle : Widget
{
	private bool _on;

	public bool On
	{
		get
		{
			return _on;
		}
		set
		{
			if (value != _on)
			{
				_on = value;
				Invalidate();
			}
		}
	}

	public event EventHandler Changed;

	public Toggle()
	{
		SetStyle(ControlStyles.Selectable, value: true);
		TabStop = true;
		Size = new Size(Ui.S(40), Ui.S(22));
		Cursor = Cursors.Hand;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		Ui.Smooth(graphics);
		RectangleF r = new RectangleF(Ui.Sf(1f), (float)Height / 2f - Ui.Sf(9f), (float)Width - Ui.Sf(2f), Ui.Sf(18f));
		Color c;
		if (_on)
		{
			c = (Enabled ? Ui.Accent : Ui.AccentDeep);
		}
		else
		{
			c = ((Hot && Enabled) ? Ui.RaisedHover : Ui.Raised);
		}
		Ui.Fill(graphics, r, r.Height / 2f, c);
		if (!_on)
		{
			Ui.Stroke(graphics, r, r.Height / 2f, Ui.Border);
		}
		float num = r.Height - Ui.Sf(6f);
		float num2 = (_on ? (r.Right - num - Ui.Sf(3f)) : (r.X + Ui.Sf(3f)));
		Color color;
		if (_on)
		{
			color = Color.White;
		}
		else
		{
			color = (Enabled ? Ui.Dim : Ui.Faint);
		}
		using (SolidBrush brush = new SolidBrush(color))
		{
			graphics.FillEllipse(brush, num2, r.Y + Ui.Sf(3f), num, num);
		}
		if (Focused)
		{
			Ui.Stroke(graphics, new RectangleF(0.5f, r.Y - Ui.Sf(2f), (float)Width - 1f, r.Height + Ui.Sf(4f)), r.Height / 2f + Ui.Sf(2f), Color.FromArgb(120, Ui.Accent));
		}
	}

	protected override void OnClick(EventArgs e)
	{
		base.OnClick(e);
		Flip();
	}

	private void Flip()
	{
		On = !_on;
		if (Changed != null)
		{
			Changed(this, EventArgs.Empty);
		}
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Return)
		{
			Flip();
		}
	}
}
