using System;
using System.Drawing;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed class InputBox : Widget
{
	public readonly TextBox Box = new TextBox();

	public InputBox()
	{
		Box.BorderStyle = BorderStyle.None;
		Box.BackColor = Ui.Raised;
		Box.ForeColor = Ui.Text;
		Box.Font = Ui.Body;
		Controls.Add(Box);
		Height = Ui.S(26);
		Box.GotFocus += (object s, EventArgs a) =>
		{
			Invalidate();
		};
		Box.LostFocus += (object s, EventArgs a) =>
		{
			Invalidate();
		};
	}

	protected override void OnLayout(LayoutEventArgs e)
	{
		base.OnLayout(e);
		int num = Ui.S(7);
		Box.SetBounds(num, (Height - Box.PreferredHeight) / 2 + 1, Math.Max(1, Width - 2 * num), Box.PreferredHeight);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Ui.Smooth(e.Graphics);
		RectangleF r = new RectangleF(0.5f, 0.5f, (float)Width - 1f, (float)Height - 1f);
		Ui.Fill(e.Graphics, r, Ui.Sf(5f), Ui.Raised);
		Ui.Stroke(e.Graphics, r, Ui.Sf(5f), Box.Focused ? Ui.Accent : Ui.Border);
	}

	protected override void OnEnabledChanged(EventArgs e)
	{
		base.OnEnabledChanged(e);
		Box.ForeColor = (Enabled ? Ui.Text : Ui.Faint);
	}
}
