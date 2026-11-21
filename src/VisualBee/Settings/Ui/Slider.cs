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
		get => _value;
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

	private bool LogScale => Log && Min > 0.0 && Max > Min;

	public event EventHandler Changed;

	public Slider()
	{
		SetStyle(ControlStyles.Selectable, value: true);
		TabStop = true;
		Height = Ui.S(26);
		Cursor = Cursors.Hand;
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
		if (Max <= Min) return 0.0;
		if (LogScale) return Math.Log(v / Min) / Math.Log(Max / Min);
		return (v - Min) / (Max - Min);
	}

	private double FromFraction(double t)
	{
		t = Math.Max(0.0, Math.Min(1.0, t));
		if (LogScale) return Min * Math.Pow(Max / Min, t);
		return Min + (Max - Min) * t;
	}

	private RectangleF Track()
	{
		float pad = Ui.Sf(9f);
		return new RectangleF(pad, Height / 2f - Ui.Sf(2.5f), Math.Max(1f, Width - 2f * pad), Ui.Sf(5f));
	}

	private void SetFromMouse(int x)
	{
		RectangleF track = Track();
		Set(FromFraction((x - track.X) / track.Width));
	}

	private void Set(double v)
	{
		double prev = _value;
		Value = v;
		if (_value != prev)
		{
			Changed?.Invoke(this, EventArgs.Empty);
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		Graphics g = e.Graphics;
		Ui.Smooth(g);
		RectangleF track = Track();
		float frac = (float)Math.Max(0.0, Math.Min(1.0, ToFraction(_value)));
		Ui.Fill(g, track, track.Height / 2f, Ui.Raised);
		Ui.Stroke(g, track, track.Height / 2f, Ui.Border, 1f);

		Color accent = Enabled ? Ui.Accent : Ui.Faint;
		if (frac > 0f)
		{
			float fillW = Math.Max(track.Height, track.Width * frac);
			Ui.Fill(g, new RectangleF(track.X, track.Y, Math.Min(track.Width, fillW), track.Height), track.Height / 2f, accent);
		}

		float radius = Ui.Sf((Hot || Down || Focused) ? 7.5f : 6.5f);
		float cx = track.X + track.Width * frac;
		float cy = track.Y + track.Height / 2f;

		if ((Hot || Down || Focused) && Enabled)
		{
			using SolidBrush halo = new SolidBrush(Color.FromArgb(45, Ui.Accent));
			g.FillEllipse(halo, cx - radius - Ui.Sf(4f), cy - radius - Ui.Sf(4f), 2f * radius + Ui.Sf(8f), 2f * radius + Ui.Sf(8f));
		}
		using (SolidBrush knob = new SolidBrush(Enabled ? Ui.Text : Ui.Dim))
		{
			g.FillEllipse(knob, cx - radius, cy - radius, 2f * radius, 2f * radius);
		}
		using Pen rim = new Pen(accent, Ui.Sf(2f));
		g.DrawEllipse(rim, cx - radius + Ui.Sf(0.75f), cy - radius + Ui.Sf(0.75f), 2f * radius - Ui.Sf(1.5f), 2f * radius - Ui.Sf(1.5f));
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
		if (Down) SetFromMouse(e.X);
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
		Nudge((e.Delta > 0) ? 1 : -1);
		((HandledMouseEventArgs)e).Handled = true;
	}

	private void Nudge(int direction)
	{
		double step = (Step > 0.0) ? Step : ((Max - Min) / 100.0);
		if ((Control.ModifierKeys & Keys.Shift) == 0 && (Max - Min) / step > 60.0)
		{
			step *= 5.0;
		}
		if (LogScale)
		{
			Set(FromFraction(ToFraction(_value) + direction * 0.02));
		}
		else
		{
			Set(_value + direction * step);
		}
	}

	protected override bool IsInputKey(Keys keyData)
	{
		Keys key = keyData & Keys.KeyCode;
		if (key == Keys.Left || key == Keys.Right || key == Keys.Up || key == Keys.Down)
		{
			return true;
		}
		return base.IsInputKey(keyData);
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Down) Nudge(-1);
		else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Up) Nudge(1);
		else if (e.KeyCode == Keys.Home) Set(Min);
		else if (e.KeyCode == Keys.End) Set(Max);
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
