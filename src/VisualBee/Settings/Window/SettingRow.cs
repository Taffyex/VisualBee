using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed partial class SettingsWindow : Form
{
	private sealed class SettingRow : Widget
	{
		private readonly SettingsWindow _w;
		private readonly Setting _s;
		public bool IsRowVisible = true;
		public bool ShowDivider;

		private Slider _slider;
		private InputBox _number;
		private Toggle _toggle;
		private Segmented _segments;
		private Dropdown _dropdown;
		private ColourWell _well;
		private List<WasapiLoopback.DeviceInfo> _deviceList;

		private readonly IconButton _reset = new IconButton(Ui.GlyphReset);

		private bool _showing;
		private bool _hover;
		private bool _dim;
		private bool _modified;

		public Setting Setting => _s;

		public SettingRow(SettingsWindow w, Setting s, ToolTip tips)
		{
			_w = w;
			_s = s;
			BackColor = Ui.Card;
			Height = Ui.S(44);
			switch (s.Kind)
			{
			case SettingKind.Toggle:
				_toggle = new Toggle();
				_toggle.Changed += (object o, EventArgs a) => Commit(_toggle.On);
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
				_slider.Changed += (object o, EventArgs a) => Commit(_slider.Value);
				_number = new InputBox { Mono = true };
				_number.Box.TextAlign = HorizontalAlignment.Right;
				_number.Box.KeyDown += (object o, KeyEventArgs a) =>
				{
					if (a.KeyCode == Keys.Return)
					{
						CommitText();
						a.SuppressKeyPress = true;
					}
				};
				_number.Box.LostFocus += (object o, EventArgs a) => CommitText();
				Controls.Add(_slider);
				Controls.Add(_number);
				break;
			case SettingKind.Choice:
			{
				Segmented segmented = new Segmented(s.Choices);
				if (s.Choices.Length <= 4 && segmented.Ideal() <= Ui.S(330))
				{
					_segments = segmented;
					_segments.Changed += (object o, EventArgs a) => Commit(_segments.Selected);
					Controls.Add(_segments);
					break;
				}
				segmented.Dispose();
				_dropdown = new Dropdown(s.Choices);
				_dropdown.Changed += (object o, EventArgs a) => Commit(_dropdown.Selected);
				Controls.Add(_dropdown);
				break;
			}
			case SettingKind.Device:
				InitDeviceDropdown();
				break;
			default:
				_well = new ColourWell { Alpha = s.Alpha };
				_well.Click += (object o, EventArgs a) => _w.Pick(_s, _well);
				Controls.Add(_well);
				break;
			}

			_reset.Cursor = Cursors.Hand;
			_reset.Click += (object o, EventArgs a) => _w.ResetSetting(_s);
			Controls.Add(_reset);

			string defDisplay = (s.Kind == SettingKind.Device)
				? WasapiLoopback.GetDeviceDisplayName(s.Default as string)
				: s.Format(s.Default);
			tips.SetToolTip(this, Wrap(s.Label + ": " + s.Hint + "\nDefault: " + defDisplay));
			tips.SetToolTip(_reset, "Reset to default: " + defDisplay);
			Hook(this);
		}

		private void InitDeviceDropdown()
		{
			RefreshDeviceList();
			_dropdown = new Dropdown(GetDeviceNames());
			_dropdown.BeforeOpen = () =>
			{
				RefreshDeviceList();
				_dropdown.SetItems(GetDeviceNames());
				SyncDeviceDropdownSelection();
			};
			_dropdown.Changed += (object o, EventArgs a) =>
			{
				if (_deviceList != null && _dropdown.Selected >= 0 && _dropdown.Selected < _deviceList.Count)
				{
					Commit(_deviceList[_dropdown.Selected].Id);
				}
			};
			Controls.Add(_dropdown);
		}

		private void RefreshDeviceList()
		{
			string currentId = _s.Get(_w._store.Current) as string ?? "default";
			_deviceList = WasapiLoopback.EnumerateDevices(currentId);
		}

		private string[] GetDeviceNames()
		{
			if (_deviceList == null) return new string[0];
			string[] names = new string[_deviceList.Count];
			for (int i = 0; i < _deviceList.Count; i++)
			{
				names[i] = _deviceList[i].Name;
			}
			return names;
		}

		private void SyncDeviceDropdownSelection()
		{
			if (_dropdown == null || _deviceList == null) return;
			string currentId = _s.Get(_w._store.Current) as string ?? "default";
			int idx = 0;
			for (int i = 0; i < _deviceList.Count; i++)
			{
				if (string.Equals(_deviceList[i].Id, currentId, StringComparison.OrdinalIgnoreCase))
				{
					idx = i;
					break;
				}
			}
			_dropdown.Selected = idx;
		}

		private static string Wrap(string text)
		{
			StringBuilder sb = new StringBuilder();
			int len = 0;
			foreach (string word in text.Split(' '))
			{
				if (len > 0 && len + word.Length > 70)
				{
					sb.Append('\n');
					len = 0;
				}
				else if (len > 0)
				{
					sb.Append(' ');
					len++;
				}
				sb.Append(word);
				len += word.Length;
			}
			return sb.ToString();
		}

		private void Hook(Control c)
		{
			c.MouseEnter += (object o, EventArgs a) => PointerIn();
			c.MouseLeave += (object o, EventArgs a) => PointerOut();
			foreach (Control child in c.Controls)
			{
				Hook(child);
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
			if (_showing) return;
			string text = _number.Box.Text.Trim();
			string[] units = new string[7] { _s.Unit, "%", "°", "ms", "px", "dB", "Hz" };
			foreach (string unit in units)
			{
				if (unit.Length > 0 && text.EndsWith(unit, StringComparison.OrdinalIgnoreCase))
				{
					text = text.Substring(0, text.Length - unit.Length).Trim();
				}
			}
			if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out double result)
				|| double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
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
				object val = _s.Get(l);
				bool enabled = _s.IsEnabled(l);
				switch (_s.Kind)
				{
				case SettingKind.Toggle:
					_toggle.On = (bool)val;
					_toggle.Enabled = enabled;
					break;
				case SettingKind.Number:
					_slider.Value = (double)val;
					if (!_number.Box.Focused)
					{
						_number.Box.Text = _s.Display((double)val);
					}
					_slider.Enabled = enabled;
					_number.Enabled = enabled;
					break;
				case SettingKind.Choice:
					if (_segments != null)
					{
						_segments.Selected = (int)val;
						_segments.Enabled = enabled;
					}
					else
					{
						_dropdown.Selected = (int)val;
						_dropdown.Enabled = enabled;
					}
					break;
				case SettingKind.Device:
					RefreshDeviceList();
					_dropdown.SetItems(GetDeviceNames());
					SyncDeviceDropdownSelection();
					_dropdown.Enabled = enabled;
					break;
				default:
					_well.Argb = (int)val;
					_well.Enabled = enabled;
					break;
				}

				bool mod = !_s.IsDefault(l);
				_reset.Visible = mod;
				IsRowVisible = !_w._hideInactive || !string.IsNullOrEmpty(_w._search.Box.Text.Trim()) || enabled;
				Visible = IsRowVisible;

				if (_dim == enabled || _modified != mod)
				{
					_dim = !enabled;
					_modified = mod;
					Invalidate();
				}
			}
			finally
			{
				_showing = false;
			}
		}

		private int LabelWidth()
		{
			return Math.Min(Ui.S(240), Math.Max(Ui.S(130), (int)(Width * 0.32)));
		}

		protected override void OnLayout(LayoutEventArgs e)
		{
			base.OnLayout(e);
			int labelX = Ui.S(18);
			int labelW = LabelWidth();
			int controlX = labelX + labelW + Ui.S(14);
			int rightEdge = Width - Ui.S(12) - Ui.S(26) - Ui.S(8);
			int controlW = Math.Max(Ui.S(40), rightEdge - controlX);
			int midY = Height / 2;

			if (_toggle != null)
			{
				_toggle.Location = new Point(controlX, midY - _toggle.Height / 2);
			}
			if (_slider != null)
			{
				int numW = Ui.S(78);
				int sliderW = Math.Max(Ui.S(50), Math.Min(controlW - numW - Ui.S(8), Ui.S(310)));
				_slider.SetBounds(controlX - Ui.S(6), midY - _slider.Height / 2, sliderW, _slider.Height);
				_number.SetBounds(_slider.Right + Ui.S(8), midY - _number.Height / 2, numW, _number.Height);
			}
			if (_segments != null)
			{
				_segments.SetBounds(controlX, midY - _segments.Height / 2, Math.Min(controlW, _segments.Ideal()), _segments.Height);
			}
			if (_dropdown != null)
			{
				_dropdown.SetBounds(controlX, midY - _dropdown.Height / 2, Math.Min(controlW, Math.Max(Ui.S(180), _dropdown.Ideal())), _dropdown.Height);
			}
			if (_well != null)
			{
				_well.SetBounds(controlX, midY - _well.Height / 2, Math.Min(controlW, Ui.S(150)), _well.Height);
			}
			_reset.Location = new Point(Width - Ui.S(12) - _reset.Width, midY - _reset.Height / 2);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.Clear(Ui.Card);
			Ui.Smooth(g);

			if (_hover)
			{
				Ui.Fill(g, new RectangleF(Ui.Sf(8f), Ui.Sf(2f), Width - Ui.Sf(16f), Height - Ui.Sf(4f)), Ui.Sf(6f), Ui.RowHover);
			}
			else if (ShowDivider)
			{
				using Pen div = new Pen(Ui.Divider, 1f);
				g.DrawLine(div, Ui.Sf(16f), Height - 1f, Width - Ui.Sf(16f), Height - 1f);
			}

			if (_modified)
			{
				using SolidBrush b = new SolidBrush(Ui.Accent);
				g.FillEllipse(b, Ui.Sf(8f), (Height - Ui.Sf(4f)) / 2f, Ui.Sf(4f), Ui.Sf(4f));
			}

			int labelW = LabelWidth();
			Ui.DrawText(g, _s.Label, Ui.Body, _dim ? Ui.Faint : Ui.Text, new Rectangle(Ui.S(18), 0, labelW, Height), TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
		}
	}
}
