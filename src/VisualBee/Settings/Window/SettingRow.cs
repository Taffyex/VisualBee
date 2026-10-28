using System.Globalization;
using System.Text;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed partial class SettingsWindow : Form
{
	private sealed class SettingRow : Widget
	{
		private readonly SettingsWindow _w;

		private readonly Setting _s;
		public bool IsRowVisible = true;

		private Slider _slider;

		private InputBox _number;

		private Toggle _toggle;

		private Segmented _segments;

		private Dropdown _dropdown;

		private ColourWell _well;

		private readonly IconButton _reset = new IconButton("\ue7a7");

		private readonly IconButton _info = new IconButton("\ue946")
		{
			Passive = true,
			Colour = Ui.Faint
		};

		private bool _showing;

		private bool _hover;

		private bool _dim;

		public SettingRow(SettingsWindow w, Setting s, ToolTip tips)
		{
			_w = w;
			_s = s;
			Height = Ui.S(42);
			switch (s.Kind)
			{
			case SettingKind.Toggle:
				_toggle = new Toggle();
				_toggle.Changed += (object o, EventArgs a) =>
				{
					Commit(_toggle.On);
				};
				Controls.Add(_toggle);
				break;
			case SettingKind.Number:
				_slider = new Slider
				{
					Min = s.Min,
					Max = s.Max,
					Step = s.Step,
					Log = s.Logarithmic
				};
				_slider.Changed += (object o, EventArgs a) =>
				{
					Commit(_slider.Value);
				};
				_number = new InputBox();
				_number.Box.TextAlign = HorizontalAlignment.Right;
				_number.Box.KeyDown += (object o, KeyEventArgs a) =>
				{
					if (a.KeyCode == Keys.Return)
					{
						CommitText();
						a.SuppressKeyPress = true;
					}
				};
				_number.Box.LostFocus += (object o, EventArgs a) =>
				{
					CommitText();
				};
				Controls.Add(_slider);
				Controls.Add(_number);
				break;
			case SettingKind.Choice:
			{
				Segmented segmented = new Segmented(s.Choices);
				if (s.Choices.Length <= 4 && segmented.Ideal() <= Ui.S(360))
				{
					_segments = segmented;
					_segments.Changed += (object o, EventArgs a) =>
					{
						Commit(_segments.Selected);
					};
					Controls.Add(_segments);
					break;
				}
				segmented.Dispose();
				_dropdown = new Dropdown(s.Choices);
				_dropdown.Changed += (object o, EventArgs a) =>
				{
					Commit(_dropdown.Selected);
				};
				Controls.Add(_dropdown);
				break;
			}
			default:
				_well = new ColourWell
				{
					Alpha = s.Alpha
				};
				_well.Click += (object o, EventArgs a) =>
				{
					_w.Pick(_s, _well);
				};
				Controls.Add(_well);
				break;
			}
			_reset.Click += (object o, EventArgs a) =>
			{
				_w.ResetSetting(_s);
			};
			Controls.Add(_reset);
			Controls.Add(_info);
			tips.SetToolTip(_info, Wrap(s.Hint + "\n(Default: " + s.Format(s.Default) + ")"));
			tips.SetToolTip(this, Wrap(s.Label + ": " + s.Hint));
			tips.SetToolTip(_reset, "Reset to default: " + s.Format(s.Default));
			Hook(this);
		}

		private static string Wrap(string text)
		{
			StringBuilder stringBuilder = new StringBuilder();
			int num = 0;
			string[] array = text.Split(' ');
			foreach (string text2 in array)
			{
				if (num > 0 && num + text2.Length > 70)
				{
					stringBuilder.Append('\n');
					num = 0;
				}
				else if (num > 0)
				{
					stringBuilder.Append(' ');
					num++;
				}
				stringBuilder.Append(text2);
				num += text2.Length;
			}
			return stringBuilder.ToString();
		}

		private void Hook(Control c)
		{
			c.MouseEnter += (object o, EventArgs a) =>
			{
				PointerIn();
			};
			c.MouseLeave += (object o, EventArgs a) =>
			{
				PointerOut();
			};
			foreach (Control control in c.Controls)
			{
				Hook(control);
			}
		}

		private void PointerIn()
		{
			_w.Hint(_s);
			if (!_hover)
			{
				_hover = true;
				Invalidate();
			}
		}

		private void PointerOut()
		{
			if (!ClientRectangle.Contains(PointToClient(Cursor.Position)))
			{
				_hover = false;
				Invalidate();
			}
		}

		private void Commit(object value)
		{
			if (!_showing)
			{
				_w._store.Set(_s, value);
			}
		}

		private void CommitText()
		{
			if (_showing)
			{
				return;
			}
			string text = _number.Box.Text.Trim();
			string[] array = new string[7] { _s.Unit, "%", "°", "ms", "px", "dB", "Hz" };
			foreach (string text2 in array)
			{
				if (text2.Length > 0 && text.EndsWith(text2, StringComparison.OrdinalIgnoreCase))
				{
					text = text.Substring(0, text.Length - text2.Length).Trim();
				}
			}
			if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out var result) || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
			{
				_w._store.Set(_s, _s.Clamp(result));
			}
			else
			{
				ShowValue(_w._store.Current);
			}
		}

		public void ShowValue(Look l)
		{
			_showing = true;
			try
			{
				object obj = _s.Get(l);
				bool flag = _s.IsEnabled(l);
				switch (_s.Kind)
				{
				case SettingKind.Toggle:
					_toggle.On = (bool)obj;
					_toggle.Enabled = flag;
					break;
				case SettingKind.Number:
					_slider.Value = (double)obj;
					if (!_number.Box.Focused)
					{
						_number.Box.Text = _s.Display((double)obj);
					}
					_slider.Enabled = flag;
					_number.Enabled = flag;
					break;
				case SettingKind.Choice:
					if (_segments != null)
					{
						_segments.Selected = (int)obj;
						_segments.Enabled = flag;
					}
					else
					{
						_dropdown.Selected = (int)obj;
						_dropdown.Enabled = flag;
					}
					break;
				default:
					_well.Argb = (int)obj;
					_well.Enabled = flag;
					break;
				}
				_reset.Visible = !_s.IsDefault(l);
				IsRowVisible = !_w._hideInactive || !string.IsNullOrEmpty(_w._search.Box.Text.Trim()) || flag;
				Visible = IsRowVisible;
				if (_dim == flag)
				{
					_dim = !flag;
					Invalidate();
				}
			}
			finally
			{
				_showing = false;
			}
		}

		protected override void OnLayout(LayoutEventArgs e)
		{
			base.OnLayout(e);
			int num = Ui.S(200);
			int num2 = Ui.S(16) + num + Ui.S(10);
			int num3 = Width - Ui.S(16) - Ui.S(26) * 2 - Ui.S(10);
			int num4 = Math.Max(Ui.S(80), num3 - num2);
			int num5 = Height / 2;
			if (_toggle != null)
			{
				_toggle.Location = new Point(num2, num5 - _toggle.Height / 2);
			}
			if (_slider != null)
			{
				int num6 = Ui.S(84);
				_slider.SetBounds(num2 - Ui.S(6), num5 - _slider.Height / 2, Math.Min(num4 - num6 - Ui.S(4), Ui.S(420)), _slider.Height);
				_number.SetBounds(_slider.Right + Ui.S(6), num5 - Ui.S(13), num6, Ui.S(26));
			}
			if (_segments != null)
			{
				_segments.SetBounds(num2, num5 - _segments.Height / 2, Math.Min(num4, _segments.Ideal()), _segments.Height);
			}
			if (_dropdown != null)
			{
				_dropdown.SetBounds(num2, num5 - _dropdown.Height / 2, Math.Min(num4, Math.Max(Ui.S(180), _dropdown.Ideal())), _dropdown.Height);
			}
			if (_well != null)
			{
				_well.SetBounds(num2, num5 - _well.Height / 2, Math.Min(num4, Ui.S(140)), _well.Height);
			}
			_reset.Location = new Point(Width - Ui.S(16) - Ui.S(26) * 2 - Ui.S(4), num5 - _reset.Height / 2);
			_info.Location = new Point(Width - Ui.S(16) - Ui.S(26), num5 - _info.Height / 2);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics graphics = e.Graphics;
			graphics.Clear(Ui.Back);
			if (_hover)
			{
				Ui.Smooth(graphics);
				Ui.Fill(graphics, new RectangleF(Ui.Sf(8f), Ui.Sf(2f), (float)Width - Ui.Sf(16f), (float)Height - Ui.Sf(4f)), Ui.Sf(6f), Ui.Card);
			}
			Ui.DrawText(r: new Rectangle(Ui.S(16), 0, Ui.S(200), Height), g: graphics, text: _s.Label, font: Ui.Body, colour: _dim ? Ui.Faint : Ui.Text, flags: TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
		}
	}

}
