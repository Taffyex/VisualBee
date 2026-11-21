using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed partial class SettingsWindow : Form
{
	private void OnSearchChanged()
	{
		string query = _search.Box.Text.Trim();
		if (query == _renderedQuery && (query.Length > 0 || _page == _renderedPage))
		{
			ShowStatus();
			return;
		}
		ShowPage(_page);
	}

	public void JumpToPage(string page)
	{
		if (string.IsNullOrEmpty(page)) page = PresetsPage;
		_page = page;
		if (!string.IsNullOrEmpty(_search.Box.Text))
		{
			_search.Box.Text = "";
		}
		ShowPage(page);
	}

	private void ShowPage(string page)
	{
		if (string.IsNullOrEmpty(page)) page = PresetsPage;
		_page = page;
		_pages.Selected = _pages.Pages.IndexOf(_page);

		string query = _search.Box.Text.Trim();
		_renderedPage = _page;
		_renderedQuery = query;
		_content.SuspendLayout();
		foreach (Control item in _content.Controls.Cast<Control>().ToList())
		{
			item.Dispose();
		}
		_content.Controls.Clear();
		_rows.Clear();
		_cards.Clear();
		_content.AutoScrollPosition = Point.Empty;

		if (query.Length > 0)
		{
			_pageTitle.Text = "Search Results";
			BuildSearch(query);
		}
		else if (_page == PresetsPage)
		{
			_pageTitle.Text = "Signature Presets";
			_pageSubtitle.Text = PageSubtitle(PresetsPage);
			BuildPresets();
		}
		else
		{
			_pageTitle.Text = _page;
			_pageSubtitle.Text = PageSubtitle(_page);
			BuildPage(_page);
		}

		_content.ResumeLayout();
		LayoutContent();
		Hint(null);
		ShowStatus();
	}

	private void BuildPage(string page)
	{
		string currentSec = null;
		RackSectionCard currentCard = null;
		List<Setting> pageSettings = _store.Schema.All.Where(s => s.Page == page).ToList();

		for (int i = 0; i < pageSettings.Count; i++)
		{
			Setting s = pageSettings[i];
			if (s.Section != currentSec)
			{
				currentSec = s.Section;
				string secName = currentSec;
				Setting master = (s.SectionMaster && s.Kind == SettingKind.Toggle) ? s : null;
				currentCard = new RackSectionCard(this, secName, (Setting x) => x.Page == page && x.Section == secName, master);
				currentCard.RefreshState(_store.Current);
				_cards.Add(currentCard);
				_content.Controls.Add(currentCard);
				if (master != null) continue;
			}

			SettingRow row = new SettingRow(this, s, _tips);
			row.ShowValue(_store.Current);
			_rows.Add(row);
			currentCard?.AddRow(row);
		}
	}

	private void BuildSearch(string query)
	{
		string[] words = query.ToLowerInvariant().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		List<Preset> presetMatches = _store.BuiltIn.Concat(_store.UserPresets()).Where(p =>
		{
			string hay = (p.Name + " " + p.Description).ToLowerInvariant();
			return words.All(w => hay.Contains(w));
		}).ToList();
		List<Theme> themeMatches = _store.Themes.Where(t =>
		{
			string hay = t.Name.ToLowerInvariant();
			return words.All(w => hay.Contains(w));
		}).ToList();
		List<Setting> matches = _store.Schema.All.Where(s =>
		{
			string hay = (s.Label + " " + s.Hint + " " + s.Section + " " + s.Page + " " + s.Keywords).ToLowerInvariant();
			return words.All(w => hay.Contains(w));
		}).ToList();

		_pageSubtitle.Text = SearchSummary(query, presetMatches.Count + themeMatches.Count, matches.Count);

		if (presetMatches.Count == 0 && themeMatches.Count == 0 && matches.Count == 0)
		{
			_content.Controls.Add(new EmptyStateCard(Ui.GlyphSearch, "Try searching for terms like \"bloom\", \"bars\", \"wasapi\", \"hdr\", or a preset name."));
			return;
		}

		if (presetMatches.Count > 0 || themeMatches.Count > 0)
		{
			_content.Controls.Add(new SectionHeaderBar("Matching Presets & Palettes", "Click any preset or colour palette to apply it immediately."));
			FlowPanel matchFlow = new FlowPanel();
			foreach (Preset p in presetMatches)
			{
				matchFlow.Controls.Add(new PresetCard(this, p));
			}
			foreach (Theme t in themeMatches)
			{
				matchFlow.Controls.Add(new ThemeChip(this, t));
			}
			_content.Controls.Add(matchFlow);
		}

		string currentSec = null;
		RackSectionCard currentCard = null;

		foreach (Setting item in matches)
		{
			string groupTitle = item.Page + "  ›  " + item.Section;
			if (groupTitle != currentSec)
			{
				currentSec = groupTitle;
				string targetPage = item.Page;
				currentCard = new RackSectionCard(this, currentSec, null, null, () => JumpToPage(targetPage));
				_cards.Add(currentCard);
				_content.Controls.Add(currentCard);
			}
			SettingRow row = new SettingRow(this, item, _tips);
			row.ShowValue(_store.Current);
			_rows.Add(row);
			currentCard?.AddRow(row);
		}
		foreach (RackSectionCard card in _cards)
		{
			card.RefreshState(_store.Current);
		}
	}

	private static string SearchSummary(string query, int lookCount, int settingCount)
	{
		if (lookCount == 0 && settingCount == 0)
		{
			return "Nothing matches “" + query + "”";
		}
		List<string> parts = new List<string>();
		if (lookCount > 0) parts.Add(lookCount + ((lookCount == 1) ? " look" : " looks"));
		if (settingCount > 0) parts.Add(settingCount + ((settingCount == 1) ? " setting" : " settings"));
		return string.Join(" and ", parts) + " matching “" + query + "”";
	}

	private void LayoutContent()
	{
		int contentWidth = _content.ContentWidth;
		int scrollY = _content.AutoScrollPosition.Y;
		int padX = Ui.S(22);
		int curY = scrollY + Ui.S(16);
		bool isPresets = _page == PresetsPage && _search.Box.Text.Trim().Length == 0;
		int maxColW = isPresets ? Ui.S(980) : Ui.S(760);
		int colW = Math.Max(Ui.S(220), Math.Min(maxColW, contentWidth - padX * 2));
		int left = Math.Max(padX, (contentWidth - colW) / 2);

		_content.SuspendLayout();
		_content.AutoScrollMinSize = Size.Empty;
		foreach (Control control in _content.Controls)
		{
			if (control is RackSectionCard card)
			{
				if (!card.IsCardVisible)
				{
					card.Visible = false;
					card.SetBounds(0, 0, colW, card.Height);
					continue;
				}
				card.Visible = true;
				card.FitWidth(colW);
				card.SetBounds(left, curY, colW, card.Height);
				curY += card.Height + Ui.S(12);
			}
			else if (control is FlowPanel fp)
			{
				fp.FitWidth(colW);
				fp.SetBounds(left, curY, colW, fp.Height);
				curY += fp.Height + Ui.S(14);
			}
			else if (control is SectionHeaderBar bar)
			{
				bar.SetBounds(left, curY, colW, bar.Height);
				curY += bar.Height + Ui.S(6);
			}
			else
			{
				control.SetBounds(left, curY, colW, control.Height);
				curY += control.Height + Ui.S(10);
			}
		}

		_content.ResumeLayout(true);
		_content.AutoScrollMinSize = new Size(0, Math.Max(0, curY - scrollY + Ui.S(16)));
		_content.HorizontalScroll.Maximum = 0;
		_content.HorizontalScroll.Visible = false;
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
		if (MessageBox.Show(this, "Put every setting back to its default? Your audio device, sync delay, and frame rate stay as they are.", _store.Title, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
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
		colourPicker.Picked += (int v) => _store.Set(s, v);
		colourPicker.ShowNear(well);
	}
}
