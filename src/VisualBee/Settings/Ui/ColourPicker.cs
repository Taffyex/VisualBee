using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed partial class ColourPicker : Form
{
	public ColourPicker(int argb, bool alpha, IList<int> swatches)
	{
		_original = argb;
		_alpha = alpha;
		_a = (alpha ? ((argb >> 24) & 0xFF) : 255);
		ColourMath.ToHsv(argb, out _h, out _s, out _v);
		FormBorderStyle = FormBorderStyle.None;
		ShowInTaskbar = false;
		StartPosition = FormStartPosition.Manual;
		BackColor = Ui.Border;
		Padding = new Padding(1);
		KeyPreview = true;
		Font = Ui.Body;
		Panel panel = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = Ui.Card
		};
		Controls.Add(panel);
		int num = Ui.S(12);
		int num2 = Ui.S(190);
		_square = new Square(this)
		{
			Location = new Point(num, num),
			Size = new Size(num2, num2)
		};
		_hue = new Strip(this, opacity: false)
		{
			Location = new Point(num + num2 + Ui.S(10), num),
			Size = new Size(Ui.S(18), num2)
		};
		int right = _hue.Right;
		if (alpha)
		{
			_opacity = new Strip(this, opacity: true)
			{
				Location = new Point(right + Ui.S(8), num),
				Size = new Size(Ui.S(18), num2)
			};
			right = _opacity.Right;
		}
		panel.Controls.Add(_square);
		panel.Controls.Add(_hue);
		if (_opacity != null)
		{
			panel.Controls.Add(_opacity);
		}
		int num3 = num + num2 + Ui.S(10);
		_preview = new Panel
		{
			Location = new Point(num, num3),
			Size = new Size(Ui.S(52), Ui.S(26))
		};
		_preview.Paint += PaintPreview;
		_hex = new InputBox
		{
			Location = new Point(_preview.Right + Ui.S(8), num3),
			Size = new Size(right - _preview.Right - Ui.S(8), Ui.S(26))
		};
		_hex.Box.KeyDown += (object s, KeyEventArgs e) =>
		{
			if (e.KeyCode == Keys.Return)
			{
				ApplyHex();
				e.SuppressKeyPress = true;
			}
		};
		_hex.Box.LostFocus += (object s, EventArgs e) =>
		{
			ApplyHex();
		};
		panel.Controls.Add(_preview);
		panel.Controls.Add(_hex);
		num3 = _hex.Bottom + Ui.S(10);
		List<int> list = new List<int>();
		foreach (int item2 in swatches ?? new int[0])
		{
			int item = item2 | -16777216;
			if (!list.Contains(item))
			{
				list.Add(item);
			}
		}
		int num4 = Ui.S(20);
		int num5 = Ui.S(5);
		int num6 = num;
		foreach (int item3 in list)
		{
			if (num6 + num4 > right)
			{
				num6 = num;
				num3 += num4 + num5;
			}
			int colour = item3;
			Panel panel2 = new Panel
			{
				Location = new Point(num6, num3),
				Size = new Size(num4, num4),
				BackColor = Color.FromArgb(colour),
				Cursor = Cursors.Hand
			};
			panel2.Click += (object s, EventArgs e) =>
			{
				ColourMath.ToHsv(colour, out _h, out _s, out _v);
				Changed();
			};
			panel.Controls.Add(panel2);
			num6 += num4 + num5;
		}
		if (list.Count > 0)
		{
			num3 += num4;
		}
		ClientSize = new Size(right + num + 2, num3 + num + 2);
		ShowValue();
	}

	public void ShowNear(Control anchor)
	{
		Rectangle workingArea = Screen.FromControl(anchor).WorkingArea;
		Point location = anchor.PointToScreen(new Point(0, anchor.Height + Ui.S(4)));
		if (location.Y + Height > workingArea.Bottom)
		{
			location.Y = anchor.PointToScreen(Point.Empty).Y - Height - Ui.S(4);
		}
		location.X = Math.Max(workingArea.Left, Math.Min(location.X, workingArea.Right - Width));
		location.Y = Math.Max(workingArea.Top, location.Y);
		Location = location;
		Show(anchor.FindForm());
	}

	protected override void OnDeactivate(EventArgs e)
	{
		base.OnDeactivate(e);
		if (!_closing)
		{
			ApplyHex();
			Close();
		}
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (e.KeyCode == Keys.Escape)
		{
			_closing = true;
			if (Picked != null)
			{
				Picked(_original);
			}
			Close();
		}
		else if (e.KeyCode == Keys.Return && !_hex.Box.Focused)
		{
			_closing = true;
			Close();
		}
	}

	private void ApplyHex()
	{
		if (!_updating && ColourMath.TryParseHex(_hex.Box.Text, out var argb))
		{
			if (!_alpha)
			{
				argb |= -16777216;
			}
			if (argb != Value)
			{
				ColourMath.ToHsv(argb, out _h, out _s, out _v);
				_a = (_alpha ? ((argb >> 24) & 0xFF) : 255);
				Changed();
			}
		}
	}

	private void Changed()
	{
		ShowValue();
		if (Picked != null)
		{
			Picked(Value);
		}
	}

	private void ShowValue()
	{
		_updating = true;
		try
		{
			if (!_hex.Box.Focused)
			{
				_hex.Box.Text = ColourMath.ToHex(Value, _alpha);
			}
		}
		finally
		{
			_updating = false;
		}
		_square.Invalidate();
		_hue.Invalidate();
		if (_opacity != null)
		{
			_opacity.Invalidate();
		}
		_preview.Invalidate();
	}

	private void PaintPreview(object sender, PaintEventArgs e)
	{
		Rectangle clientRectangle = _preview.ClientRectangle;
		Ui.Checker(e.Graphics, clientRectangle);
		using (SolidBrush brush = new SolidBrush(Color.FromArgb(_original)))
		{
			e.Graphics.FillRectangle(brush, 0, 0, clientRectangle.Width / 2, clientRectangle.Height);
		}
		using SolidBrush brush2 = new SolidBrush(Color.FromArgb(Value));
		e.Graphics.FillRectangle(brush2, clientRectangle.Width / 2, 0, clientRectangle.Width - clientRectangle.Width / 2, clientRectangle.Height);
	}
}
