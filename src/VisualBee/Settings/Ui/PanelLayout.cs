using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal static class PanelLayout
{
	private static float _scale;

	public static float Scale
	{
		get
		{
			if (_scale == 0f)
			{
				float val = 1f;
				try
				{
					using Graphics graphics = Graphics.FromHwnd(IntPtr.Zero);
					val = graphics.DpiX / 96f;
				}
				catch (Exception)
				{
				}
				_scale = Math.Max(1f, val);
			}
			return _scale;
		}
	}

	public static int S(int px)
	{
		return (int)Math.Round((float)px * Scale);
	}

	public static void Fit(Control host)
	{
		List<Control> list = host.Controls.Cast<Control>().ToList();
		int width = host.ClientSize.Width;
		int num = S(4);
		host.SuspendLayout();
		try
		{
			foreach (Control item in list)
			{
				item.Location = new Point(S(item.Left), S(item.Top));
				if (!SizesToText(item))
				{
					item.Width = S(item.Width);
				}
				if (item is Button button)
				{
					Size size = TextRenderer.MeasureText(button.Text, button.Font);
					button.Width = Math.Max(button.Width, size.Width + S(16));
					button.Height = Math.Max(S(button.Height), size.Height + S(8));
				}
			}
			int shift = 0;
			foreach (List<Control> item2 in Rows(list).ToList())
			{
				item2.ForEach((Control c) =>
				{
					c.Top += shift;
				});
				int num2 = item2.Max((Control c) => c.Bottom);
				for (int num3 = 1; num3 < item2.Count; num3++)
				{
					int num4 = item2[num3 - 1].Right + num;
					if (item2[num3].Left < num4)
					{
						item2[num3].Left = num4;
					}
				}
				int num5 = item2[item2.Count - 1].Right - width;
				TextBox box = item2.OfType<TextBox>().FirstOrDefault();
				if (num5 > 0 && box != null && box.Width - num5 >= S(120))
				{
					box.Width -= num5;
					foreach (Control item3 in item2.Where((Control c) => c.Left > box.Left))
					{
						item3.Left -= num5;
					}
				}
				else if (num5 > 0)
				{
					Control control = item2[item2.Count - 1];
					int num6 = Math.Max(S(80), width - control.Left);
					if (control is CheckBox { AutoSize: not false, Width: var width2 } checkBox)
					{
						int num7 = width2 - TextRenderer.MeasureText(checkBox.Text, checkBox.Font).Width;
						Size size2 = TextRenderer.MeasureText(checkBox.Text, checkBox.Font, new Size(num6 - num7, 0), TextFormatFlags.WordBreak);
						checkBox.AutoSize = false;
						checkBox.CheckAlign = ContentAlignment.TopLeft;
						checkBox.TextAlign = ContentAlignment.TopLeft;
						checkBox.Size = new Size(num6, size2.Height + S(6));
					}
					else if (SizesToText(control))
					{
						control.MaximumSize = new Size(num6, 0);
					}
				}
				shift += Math.Max(0, item2.Max((Control c) => c.Bottom) - num2);
			}
		}
		finally
		{
			host.ResumeLayout();
		}
	}

	private static bool SizesToText(Control c)
	{
		if (c.AutoSize)
		{
			if (!(c is Label))
			{
				return c is CheckBox;
			}
			return true;
		}
		return false;
	}

	private static IEnumerable<List<Control>> Rows(List<Control> controls)
	{
		List<Control> list = null;
		foreach (Control c in controls.OrderBy((Control control) => control.Top))
		{
			if (list != null && c.Top - list[0].Top < S(12))
			{
				list.Add(c);
				continue;
			}
			if (list != null)
			{
				yield return list.OrderBy((Control r) => r.Left).ToList();
			}
			list = new List<Control> { c };
		}
		if (list != null)
		{
			yield return list.OrderBy((Control r) => r.Left).ToList();
		}
	}
}
