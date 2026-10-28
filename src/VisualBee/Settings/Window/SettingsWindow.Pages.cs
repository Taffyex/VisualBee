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
		_cards.Clear();
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
		string currentSec = null;
		RackSectionCard currentCard = null;

		foreach (Setting s in _store.Schema.All)
		{
			if (s.Page != page) continue;

			if (s.Section != currentSec)
			{
				currentSec = s.Section;
				currentCard = new RackSectionCard(this, currentSec, (Setting x) => x.Page == page && x.Section == s.Section);
				_cards.Add(currentCard);
				_content.Controls.Add(currentCard);
			}

			SettingRow row = new SettingRow(this, s, _tips);
			row.ShowValue(_store.Current);
			_rows.Add(row);
			currentCard?.AddRow(row);
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
		string currentSec = null;
		RackSectionCard currentCard = null;

		foreach (Setting item in list)
		{
			string text2 = item.Page + "  ›  " + item.Section;
			if (text2 != currentSec)
			{
				currentSec = text2;
				currentCard = new RackSectionCard(this, currentSec, null);
				_cards.Add(currentCard);
				_content.Controls.Add(currentCard);
			}
			SettingRow row = new SettingRow(this, item, _tips);
			row.ShowValue(_store.Current);
			_rows.Add(row);
			currentCard?.AddRow(row);
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
			Height = Ui.S(44),
			TextAlign = ContentAlignment.BottomLeft,
			Padding = new Padding(Ui.S(14), 0, 0, Ui.S(6))
		};
		_content.Controls.Add(value);
	}

	private void LayoutContent()
	{
		int contentWidth = _content.ContentWidth;
		int scrollY = _content.AutoScrollPosition.Y;
		int curY = scrollY;
		_content.SuspendLayout();

		if (_page == "Presets" || _search.Box.Text.Length > 0)
		{
			foreach (Control control in _content.Controls)
			{
				if (control is SettingRow r && !r.IsRowVisible) continue;
				if (control is RackSectionCard card && !card.IsCardVisible) continue;
				if (control is FlowPanel fp)
				{
					fp.FitWidth(contentWidth - Ui.S(28));
				}
				int left = (control is FlowPanel || control is Label) ? Ui.S(14) : Ui.S(14);
				control.SetBounds(left, curY, contentWidth - Ui.S(28), control.Height);
				curY += control.Height + ((control is FlowPanel) ? Ui.S(8) : Ui.S(4));
			}
		}
		else
		{
			int colGap = Ui.S(14);
			int padX = Ui.S(14);
			int numCols = (contentWidth >= Ui.S(740)) ? 2 : 1;

			if (numCols == 2)
			{
				int colW = (contentWidth - padX * 2 - colGap) / 2;
				int yLeft = curY + Ui.S(8);
				int yRight = curY + Ui.S(8);

				foreach (RackSectionCard card in _cards)
				{
					if (!card.IsCardVisible)
					{
						card.Visible = false;
						continue;
					}
					card.Visible = true;
					card.FitWidth(colW);

					if (yLeft <= yRight)
					{
						card.SetBounds(padX, yLeft, colW, card.Height);
						yLeft += card.Height + Ui.S(12);
					}
					else
					{
						card.SetBounds(padX + colW + colGap, yRight, colW, card.Height);
						yRight += card.Height + Ui.S(12);
					}
				}
				curY = Math.Max(yLeft, yRight);
			}
			else
			{
				int colW = contentWidth - padX * 2;
				int y = curY + Ui.S(8);
				foreach (RackSectionCard card in _cards)
				{
					if (!card.IsCardVisible)
					{
						card.Visible = false;
						continue;
					}
					card.Visible = true;
					card.FitWidth(colW);
					card.SetBounds(padX, y, colW, card.Height);
					y += card.Height + Ui.S(12);
				}
				curY = y;
			}
		}

		_content.AutoScrollMinSize = new Size(0, curY - scrollY + Ui.S(16));
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
