using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed partial class VisualiserHost
{
	private void HealPanelMetrics(Control panel)
	{
		if (panel == null)
		{
			return;
		}

		try
		{
			// 1. Heal in-memory MusicBee panel height dictionary
			Assembly mbAsm = panel.GetType().Assembly;
			foreach (Type type in mbAsm.GetTypes())
			{
				FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
				foreach (FieldInfo field in fields)
				{
					if (field.FieldType == typeof(Dictionary<int, int>))
					{
						if (field.GetValue(null) is Dictionary<int, int> dict)
						{
							lock (dict)
							{
								List<int> keys = new List<int>(dict.Keys);
								foreach (int k in keys)
								{
									if (dict[k] <= -32767)
									{
										dict[k] = -PanelLayout.S(200);
									}
								}
							}
						}
					}
				}
			}

			// 2. Heal parent dock container if it already computed a >10,000px height
			Control parent = panel.Parent;
			if (parent != null && parent.Height > 10000)
			{
				parent.Height = PanelLayout.S(200);
				parent.PerformLayout();
			}

			// 3. Heal persisted MusicBee3Settings.ini on disk if poisoned
			HealSettingsFile();
		}
		catch
		{
		}
	}

	private void HealSettingsFile()
	{
		try
		{
			string folder = StorageFolder();
			string iniPath = Path.Combine(folder, "MusicBee3Settings.ini");
			if (!File.Exists(iniPath))
			{
				return;
			}

			string content = File.ReadAllText(iniPath);
			bool modified = false;

			int idx = 0;
			while ((idx = content.IndexOf("<PanelHeight>-32", idx, StringComparison.OrdinalIgnoreCase)) >= 0)
			{
				int endIdx = content.IndexOf("</PanelHeight>", idx, StringComparison.OrdinalIgnoreCase);
				if (endIdx > idx)
				{
					string tag = content.Substring(idx, endIdx + 14 - idx);
					content = content.Replace(tag, "<PanelHeight>-200</PanelHeight>");
					modified = true;
				}
				idx += 13;
			}

			if (modified)
			{
				File.WriteAllText(iniPath, content);
			}
		}
		catch
		{
		}
	}

	private void DisableHostScrollOverhang(Control panel)
	{
		if (panel == null)
		{
			return;
		}

		try
		{
			// Reset ScrollBars property or field via reflection
			Type t = panel.GetType();
			PropertyInfo pScroll = t.GetProperty("ScrollBars", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (pScroll != null && pScroll.CanWrite)
			{
				pScroll.SetValue(panel, ScrollBars.None, null);
			}

			// Also reset internal boolean vertical scroll flag if present
			FieldInfo[] fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			foreach (FieldInfo f in fields)
			{
				if (f.FieldType == typeof(ScrollBars))
				{
					f.SetValue(panel, ScrollBars.None);
				}
				else if (f.FieldType == typeof(bool) && f.Name.IndexOf("Scroll", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					f.SetValue(panel, false);
				}
			}

			if (panel.Region != null)
			{
				panel.Region.Dispose();
				panel.Region = null;
			}
		}
		catch
		{
		}
	}

	private void SyncViewBounds()
	{
		if (_view == null || _panel == null || _panel.IsDisposed)
		{
			return;
		}

		int targetW = _panel.ClientSize.Width;
		int targetH = _panel.ClientSize.Height;

		// Check if panel.Region is narrower than ClientSize (the +17px scrollbar overhang)
		if (_panel.Region != null)
		{
			try
			{
				using (Graphics g = _panel.CreateGraphics())
				{
					RectangleF bounds = _panel.Region.GetBounds(g);
					if (bounds.Width > 0 && bounds.Width < targetW)
					{
						targetW = (int)Math.Round(bounds.Width);
					}
				}
			}
			catch
			{
			}
		}

		// Check parent dock container width if docked inside a side panel
		Control parent = _panel.Parent;
		if (parent != null && parent.ClientSize.Width > 0 && parent.ClientSize.Width < targetW)
		{
			targetW = parent.ClientSize.Width;
		}

		if (targetW > 0 && targetH > 0)
		{
			if (_view.Left != 0 || _view.Top != 0 || _view.Width != targetW || _view.Height != targetH)
			{
				_view.SetBounds(0, 0, targetW, targetH);
			}
		}
	}
}
