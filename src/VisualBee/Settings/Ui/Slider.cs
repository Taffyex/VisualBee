using System;
using System.Drawing;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed class Slider : Widget
{
	public double Min;

	public double Max = 1.0;

	public double Step = 0.01;

	public bool Log;

	private double _value;

	public double Value
	{
		get
		{
			return _value;
		}
		set
		{
			double num = Snap(value);
			if (num != _value)
			{
				_value = num;
				Invalidate();
			}
		}
	}

	private bool LogScale
	{
		get
		{
			if (Log && Min > 0.0)
			{
				return Max > Min;
			}
			return false;
		}
	}

	public event EventHandler Changed;

	public Slider()
	{
		SetStyle(ControlStyles.Selectable, value: true);
		TabStop = true;
		Height = Ui.S(24);
	}

	private double Snap(double v)
	{
		v = Math.Max(Min, Math.Min(Max, v));
		if (Step > 0.0)
		{
			v = Min + Math.Round((v - Min) / Step) * Step;
		}
		return Math.Max(Min, Math.Min(Max, Math.Round(v, 6)));
	}

	private double ToFraction(double v)
	{
		if (Max <= Min)
		{
			return 0.0;
		}
		if (LogScale)
		{
			return Math.Log(v / Min) / Math.Log(Max / Min);
		}
		return (v - Min) / (Max - Min);
	}

	private double FromFraction(double t)
	{
		t = Math.Max(0.0, Math.Min(1.0, t));
		if (LogScale)
		{
			return Min * Math.Pow(Max / Min, t);
		}
		return Min + (Max - Min) * t;
	}

	private RectangleF Track()
	{
		float num = Ui.Sf(8f);
		return new RectangleF(num, (float)Height / 2f - Ui.Sf(2f), Math.Max(1f, (float)Width - 2f * num), Ui.Sf(4f));
	}

	private void SetFromMouse(int x)
	{
		RectangleF rectangleF = Track();
		Set(FromFraction(((float)x - rectangleF.X) / rectangleF.Width));
	}

	private void Set(double v)
	{
		double value = _value;
		Value = v;
		if (_value != value && Changed != null)
		{
			Changed(this, EventArgs.Empty);
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics graphics = e.Graphics;
		Ui.Smooth(graphics);
		RectangleF r = Track();
		float num = (float)Math.Max(0.0, Math.Min(1.0, ToFraction(_value)));
		Ui.Fill(graphics, r, r.Height / 2f, Ui.Raised);
		Color color = (Enabled ? Ui.Accent : Ui.Faint);
		if (num > 0f)
		{
			Ui.Fill(graphics, new RectangleF(r.X, r.Y, r.Width * num, r.Height), r.Height / 2f, color);
		}
		float num2 = Ui.Sf((Hot || Down || Focused) ? 8 : 7);
		float num3 = r.X + r.Width * num;
		float num4 = r.Y + r.Height / 2f;
		if ((Hot || Down) && Enabled)
		{
			using SolidBrush brush = new SolidBrush(Color.FromArgb(50, Ui.Accent));
			graphics.FillEllipse(brush, num3 - num2 - Ui.Sf(4f), num4 - num2 - Ui.Sf(4f), 2f * num2 + Ui.Sf(8f), 2f * num2 + Ui.Sf(8f));
		}
		using (SolidBrush brush2 = new SolidBrush(Enabled ? Ui.Text : Ui.Dim))
		{
			graphics.FillEllipse(brush2, num3 - num2, num4 - num2, 2f * num2, 2f * num2);
		}
		using Pen pen = new Pen(color, Ui.Sf(2f));
		graphics.DrawEllipse(pen, num3 - num2 + Ui.Sf(1f), num4 - num2 + Ui.Sf(1f), 2f * num2 - Ui.Sf(2f), 2f * num2 - Ui.Sf(2f));
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (e.Button == MouseButtons.Left)
		{
			Focus();
			Capture = true;
			SetFromMouse(e.X);
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (Down)
		{
			SetFromMouse(e.X);
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		Capture = false;
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		if (!Focused || !ClientRectangle.Contains(e.Location))
		{
			base.OnMouseWheel(e);
			return;
		}
		Nudge((e.Delta > 0) ? 1 : (-1));
		((HandledMouseEventArgs)e).Handled = true;
	}

	private void Nudge(int direction)
	{
		double num = ((Step > 0.0) ? Step : ((Max - Min) / 100.0));
		if ((Control.ModifierKeys & Keys.Shift) == 0 && (Max - Min) / num > 60.0)
		{
			num *= 5.0;
		}
		if (LogScale)
		{
			Set(FromFraction(ToFraction(_value) + (double)direction * 0.02));
		}
		else
		{
			Set(_value + (double)direction * num);
		}
	}

	protected override bool IsInputKey(Keys keyData)
	{
		Keys keys = keyData & Keys.KeyCode;
		if (keys != Keys.Left && keys != Keys.Right && keys != Keys.Up && keys != Keys.Down)
		{
			return base.IsInputKey(keyData);
		}
		return true;
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Down)
		{
			Nudge(-1);
		}
		else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Up)
		{
			Nudge(1);
		}
		else if (e.KeyCode == Keys.Home)
		{
			Set(Min);
		}
		else if (e.KeyCode == Keys.End)
		{
			Set(Max);
		}
	}

	protected override void OnGotFocus(EventArgs e)
	{
		base.OnGotFocus(e);
		Invalidate();
	}

	protected override void OnLostFocus(EventArgs e)
	{
		base.OnLostFocus(e);
		Invalidate();
	}
}
