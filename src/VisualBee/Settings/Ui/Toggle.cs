using System;
using System.Drawing;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed class Toggle : Widget
{
	private bool _on;

	public bool On
	{
		get => _on;
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
		Size = new Size(Ui.S(42), Ui.S(24));
		Cursor = Cursors.Hand;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		Ui.Smooth(g);
		RectangleF r = new RectangleF(Ui.Sf(1f), Height / 2f - Ui.Sf(10f), Width - Ui.Sf(2f), Ui.Sf(20f));
		Color track = _on
			? (Enabled ? (Hot ? Ui.Mix(Ui.Accent, Color.White, 0.14) : Ui.Accent) : Ui.AccentDeep)
			: ((Hot && Enabled) ? Ui.RaisedHover : Ui.Raised);

		Ui.Fill(g, r, r.Height / 2f, track);
		if (!_on)
		{
			Ui.Stroke(g, r, r.Height / 2f, (Hot && Enabled) ? Ui.Specular : Ui.Border);
		}

		float knobSize = r.Height - Ui.Sf(6f);
		float knobX = _on ? (r.Right - knobSize - Ui.Sf(3f)) : (r.X + Ui.Sf(3f));
		Color knobCol = _on
			? (Enabled ? Ui.AccentText : Ui.Dim)
			: (Enabled ? Ui.Dim : Ui.Faint);

		using (SolidBrush brush = new SolidBrush(knobCol))
		{
			g.FillEllipse(brush, knobX, r.Y + Ui.Sf(3f), knobSize, knobSize);
		}
		if (Focused)
		{
			Ui.Stroke(g, new RectangleF(0.5f, r.Y - Ui.Sf(2f), Width - 1f, r.Height + Ui.Sf(4f)), r.Height / 2f + Ui.Sf(2f), Ui.AccentBorder);
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
		Changed?.Invoke(this, EventArgs.Empty);
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
