using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed partial class ColourPicker : Form
{
	private sealed class Square : Widget
	{
		private readonly ColourPicker _p;

		public Square(ColourPicker p)
		{
			_p = p;
			Cursor = Cursors.Cross;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics graphics = e.Graphics;
			Rectangle clientRectangle = ClientRectangle;
			using (SolidBrush brush = new SolidBrush(Color.FromArgb(ColourMath.FromHsv(_p._h, 1.0, 1.0))))
			{
				graphics.FillRectangle(brush, clientRectangle);
			}
			using (LinearGradientBrush brush2 = new LinearGradientBrush(clientRectangle, Color.White, Color.FromArgb(0, Color.White), LinearGradientMode.Horizontal))
			{
				graphics.FillRectangle(brush2, clientRectangle);
			}
			using (LinearGradientBrush brush3 = new LinearGradientBrush(clientRectangle, Color.FromArgb(0, Color.Black), Color.Black, LinearGradientMode.Vertical))
			{
				graphics.FillRectangle(brush3, clientRectangle);
			}
			Ui.Smooth(graphics);
			float num = (float)(_p._s * (double)(Width - 1));
			float num2 = (float)((1.0 - _p._v) * (double)(Height - 1));
			float num3 = Ui.Sf(12f);
			using (Pen pen = new Pen(Color.Black, Ui.Sf(3f)))
			{
				graphics.DrawEllipse(pen, num - num3 / 2f, num2 - num3 / 2f, num3, num3);
			}
			using Pen pen2 = new Pen(Color.White, Ui.Sf(1.5f));
			graphics.DrawEllipse(pen2, num - num3 / 2f, num2 - num3 / 2f, num3, num3);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			Capture = true;
			Pick(e.Location);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (Down)
			{
				Pick(e.Location);
			}
		}

		private void Pick(Point p)
		{
			_p._s = Math.Max(0.0, Math.Min(1.0, (double)p.X / (double)Math.Max(1, Width - 1)));
			_p._v = 1.0 - Math.Max(0.0, Math.Min(1.0, (double)p.Y / (double)Math.Max(1, Height - 1)));
			_p.Changed();
		}
	}

	private sealed class Strip : Widget
	{
		private readonly ColourPicker _p;

		private readonly bool _opacity;

		public Strip(ColourPicker p, bool opacity)
		{
			_p = p;
			_opacity = opacity;
			Cursor = Cursors.Hand;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics graphics = e.Graphics;
			Rectangle clientRectangle = ClientRectangle;
			if (_opacity)
			{
				Ui.Checker(graphics, clientRectangle);
				Color color = Color.FromArgb(ColourMath.FromHsv(_p._h, _p._s, _p._v));
				using LinearGradientBrush brush = new LinearGradientBrush(clientRectangle, color, Color.FromArgb(0, color), LinearGradientMode.Vertical);
				graphics.FillRectangle(brush, clientRectangle);
			}
			else
			{
				using LinearGradientBrush linearGradientBrush = new LinearGradientBrush(clientRectangle, Color.Red, Color.Red, LinearGradientMode.Vertical);
				ColorBlend colorBlend = new ColorBlend(7);
				for (int i = 0; i < 7; i++)
				{
					colorBlend.Colors[i] = Color.FromArgb(ColourMath.FromHsv(i * 60, 1.0, 1.0));
					colorBlend.Positions[i] = (float)i / 6f;
				}
				linearGradientBrush.InterpolationColors = colorBlend;
				graphics.FillRectangle(linearGradientBrush, clientRectangle);
			}
			float num = (_opacity ? (1f - (float)_p._a / 255f) : ((float)(_p._h / 360.0))) * (float)(Height - 1);
			Ui.Smooth(graphics);
			RectangleF r = new RectangleF(0.5f, num - Ui.Sf(3f), (float)Width - 1f, Ui.Sf(6f));
			Ui.Stroke(graphics, r, Ui.Sf(2f), Color.Black, Ui.Sf(3f));
			Ui.Stroke(graphics, r, Ui.Sf(2f), Color.White, Ui.Sf(1.5f));
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			Capture = true;
			Pick(e.Y);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (Down)
			{
				Pick(e.Y);
			}
		}

		private void Pick(int y)
		{
			double num = Math.Max(0.0, Math.Min(1.0, (double)y / (double)Math.Max(1, Height - 1)));
			if (_opacity)
			{
				_p._a = (int)Math.Round((1.0 - num) * 255.0);
			}
			else
			{
				_p._h = num * 360.0;
			}
			_p.Changed();
		}
	}

	private readonly int _original;

	private readonly bool _alpha;

	private double _h;

	private double _s;

	private double _v;

	private int _a;

	private readonly Square _square;

	private readonly Strip _hue;

	private readonly Strip _opacity;

	private readonly InputBox _hex;

	private readonly Panel _preview;

	private bool _updating;

	private bool _closing;

	public int Value => ColourMath.FromHsv(_h, _s, _v, _a);

	public event Action<int> Picked;

}
