using System.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed partial class SettingsWindow : Form
{
	private void ShowPage(string page)
	{
		if (page == null)
		{
			page = "Presets";
		}
		_page = page;
		string text = _search.Box.Text.Trim();
		_content.SuspendLayout();
		foreach (Control item in _content.Controls.Cast<Control>().ToList())
		{
			item.Dispose();
		}
		_content.Controls.Clear();
		_rows.Clear();
		_headers.Clear();
		_content.AutoScrollPosition = Point.Empty;
		if (text.Length > 0)
		{
			BuildSearch(text);
		}
		else if (page == "Presets")
		{
			BuildPresets();
		}
		else
		{
			BuildPage(page);
		}
		_content.ResumeLayout();
		LayoutContent();
		Hint(null);
		ShowStatus();
	}

	private void BuildPage(string page)
	{
		AddTitle(page);
		string text = null;
		foreach (Setting s in _store.Schema.All)
		{
			if (s.Page != page)
			{
				continue;
			}
			if (s.Section != text)
			{
				text = s.Section;
				AddSection(text, (Setting x) => x.Page == page && x.Section == s.Section);
			}
			AddRow(s);
		}
	}

	private void BuildSearch(string query)
	{
		string[] words = query.ToLowerInvariant().Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		List<Setting> list = _store.Schema.All.Where((Setting s) =>
		{
			string text3 = (s.Label + " " + s.Hint + " " + s.Section + " " + s.Page + " " + s.Keywords).ToLowerInvariant();
			return words.All((string w) => text3.Contains(w));
		}).ToList();
		AddTitle((list.Count == 0) ? ("Nothing matches “" + query + "”") : (list.Count + " settings match “" + query + "”"));
		string text = null;
		foreach (Setting item in list)
		{
			string text2 = item.Page + "  ›  " + item.Section;
			if (text2 != text)
			{
				text = text2;
				AddSection(text2, null);
			}
			AddRow(item);
		}
	}

	private void AddTitle(string text)
	{
		Label value = new Label
		{
			Text = text,
			Font = Ui.Heading,
			ForeColor = Ui.Text,
			AutoSize = false,
			Height = Ui.S(46),
			TextAlign = ContentAlignment.BottomLeft,
			Padding = new Padding(Ui.S(14), 0, 0, Ui.S(6))
		};
		_content.Controls.Add(value);
	}

	private void AddSection(string text, Func<Setting, bool> members)
	{
		SectionHeader sectionHeader = new SectionHeader(this, text, members);
		_headers.Add(sectionHeader);
		sectionHeader.ShowState(_store.Current);
		_content.Controls.Add(sectionHeader);
	}

	private void AddRow(Setting s)
	{
		SettingRow settingRow = new SettingRow(this, s, _tips);
		settingRow.ShowValue(_store.Current);
		_rows.Add(settingRow);
		_content.Controls.Add(settingRow);
	}

	private void LayoutContent()
	{
		int contentWidth = _content.ContentWidth;
		int num = _content.AutoScrollPosition.Y;
		int num2 = num;
		_content.SuspendLayout();
		foreach (Control control in _content.Controls)
		{
			if (control is SettingRow r && !r.IsRowVisible) continue;
			if (control is SectionHeader h && !h.IsHeaderVisible) continue;
			if (control is FlowPanel)
			{
				((FlowPanel)control).FitWidth(contentWidth - Ui.S(28));
			}
			int num3 = ((control is FlowPanel) ? Ui.S(14) : 0);
			control.SetBounds(num3, num2, (control is FlowPanel) ? (contentWidth - Ui.S(28)) : contentWidth, control.Height);
			num2 += control.Height + ((control is FlowPanel) ? Ui.S(6) : 0);
		}
		_content.AutoScrollMinSize = new Size(0, num2 - num);
		_content.ResumeLayout();
	}

	public void ResetSetting(Setting s)
	{
		_store.Reset(s);
	}

	public void ResetMembers(Func<Setting, bool> members)
	{
		_store.Reset(members);
	}

	private void ResetPage()
	{
		string page = _page;
		_store.Reset((Setting s) => s.Page == page);
	}

	private void ResetAll()
	{
		if (MessageBox.Show(this, "Put every setting back to its default? Your sync delay and frame rate stay as they are.", _store.Title, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
		{
			_store.Reset((Setting s) => !s.Personal);
		}
	}

	public void Pick(Setting s, ColourWell well)
	{
		Look current = _store.Current;
		List<int> list = new List<int>();
		foreach (Setting item in _store.Schema.All)
		{
			if (item.Kind == SettingKind.Colour)
			{
				list.Add((int)item.Get(current));
			}
		}
		ColourPicker colourPicker = new ColourPicker((int)s.Get(current), s.Alpha, list);
		colourPicker.Picked += (int v) =>
		{
			_store.Set(s, v);
		};
		colourPicker.ShowNear(well);
	}

}
