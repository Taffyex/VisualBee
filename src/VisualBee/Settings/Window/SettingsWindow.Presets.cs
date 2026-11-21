using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed partial class SettingsWindow : Form
{
	private void BuildPresets()
	{
		_content.Controls.Add(new SectionHeaderBar(
			"Signature Presets",
			"Complete studio configurations covering geometry, glass optics, glow, and colour balance."));
		FlowPanel flowBuiltin = new FlowPanel();
		foreach (Preset item in _store.BuiltIn)
		{
			flowBuiltin.Controls.Add(new PresetCard(this, item));
		}
		_content.Controls.Add(flowBuiltin);

		_content.Controls.Add(new SectionHeaderBar(
			"Studio Colour Palettes",
			"Applies a harmonious palette across bars, background, glow, and grid without altering layout."));
		FlowPanel flowThemes = new FlowPanel();
		foreach (Theme theme in _store.Themes)
		{
			flowThemes.Controls.Add(new ThemeChip(this, theme));
		}
		_content.Controls.Add(flowThemes);

		FlatButton saveBtn = new FlatButton("Save current look…", Ui.GlyphSave) { Primary = true };
		saveBtn.Click += (object s, EventArgs a) => SaveLook();
		FlatButton copyCodeBtn = new FlatButton("Copy share code", Ui.GlyphCopy);
		copyCodeBtn.Click += (object s, EventArgs a) => CopyCurrentShareCode();
		FlatButton pasteCodeBtn = new FlatButton("Paste code…", Ui.GlyphPaste);
		pasteCodeBtn.Click += (object s, EventArgs a) => PasteLook();
		FlatButton importBtn = new FlatButton("Import .ini…", Ui.GlyphImport);
		importBtn.Click += (object s, EventArgs a) => ImportLook();
		FlatButton folderBtn = new FlatButton("Open folder", Ui.GlyphFolder);
		folderBtn.Click += (object s, EventArgs a) => OpenFolder();

		_tips.SetToolTip(saveBtn, "Save current settings as a user preset");
		_tips.SetToolTip(copyCodeBtn, "Copy a shareable VB2:... preset code for your current look to the clipboard");
		_tips.SetToolTip(pasteCodeBtn, "Paste a shared VB2:... code or preset text from another user");
		_tips.SetToolTip(importBtn, "Import a preset file (.ini) — you can also drag and drop .ini files onto this window");
		_tips.SetToolTip(folderBtn, "Open the folder where user presets are stored");

		_content.Controls.Add(new SectionHeaderBar(
			"User Presets",
			"Right-click any preset card to use, copy a share code, export .ini, or manage."));
		FlowPanel actionsFlow = new FlowPanel();
		actionsFlow.Controls.AddRange(new Control[] { saveBtn, copyCodeBtn, pasteCodeBtn, importBtn, folderBtn });
		_content.Controls.Add(actionsFlow);

		List<Preset> userPresets = _store.UserPresets();
		if (userPresets.Count == 0)
		{
			_content.Controls.Add(new EmptyStateCard(Ui.GlyphSave, "No saved user presets yet — save your current look, paste a share code, or drop a .ini file here."));
			return;
		}

		FlowPanel flowUser = new FlowPanel();
		foreach (Preset item in userPresets)
		{
			flowUser.Controls.Add(new PresetCard(this, item));
		}
		_content.Controls.Add(flowUser);
	}

	public void ApplyPreset(Preset p)
	{
		_store.Apply(p);
	}

	public void ApplyTheme(Theme t)
	{
		_store.Apply(t);
	}

	public bool IsCurrent(Preset p)
	{
		return _store.PresetName == p.Name;
	}

	public int[] Swatch(Preset p)
	{
		return _store.SwatchOf(_store.Preview(p));
	}

	private void SaveLook()
	{
		string initial = (_store.PresetName.Length > 0 && !_store.BuiltIn.Any((Preset b) => b.Name == _store.PresetName)) ? _store.PresetName : "My look";
		string name = Prompt.Ask(this, "Save look", "Name for this look:", initial);
		if (string.IsNullOrWhiteSpace(name)) return;
		name = name.Trim();
		Preset captured = _store.Capture(name, "");
		if (!ConfirmAndSaveUserPreset(captured)) return;
		_store.PresetName = name;
		_store.Modified = false;
		_store.Save();
		if (_page == PresetsPage && _search.Box.Text.Trim().Length == 0) ShowPage(PresetsPage);
		else ShowStatus();
		ShowNotice("Preset saved", "Saved “" + name + "” to User Presets.");
	}

	private bool ConfirmAndSaveUserPreset(Preset p)
	{
		Preset existing = _store.UserPresets().FirstOrDefault((Preset u) => string.Equals(u.Name, p.Name, StringComparison.CurrentCultureIgnoreCase));
		if (existing != null && MessageBox.Show(this, "Replace your look “" + p.Name + "”?", _store.Title, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
		{
			return false;
		}
		p.File = existing?.File;
		_store.SavePreset(p);
		return true;
	}

	private void CopyCurrentShareCode()
	{
		string label = !string.IsNullOrWhiteSpace(_store.PresetName) ? _store.PresetName : "Custom Look";
		CopyCodeToClipboard(label, _store.EncodeCurrentShareCode());
	}

	private void CopyPresetShareCode(Preset p)
	{
		if (p != null) CopyCodeToClipboard(p.Name, _store.EncodeShareCode(p));
	}

	private void CopyCodeToClipboard(string name, string code)
	{
		try
		{
			Clipboard.SetText(code);
			ShowNotice("Share code copied", "Copied “" + name + "” (" + code.Length + " chars) — paste anywhere to share.");
		}
		catch (Exception)
		{
			Prompt.Ask(this, "Share code", "Clipboard unavailable — copy this share code:", code);
		}
	}

	private void PasteLook()
	{
		string clip = "";
		try
		{
			if (Clipboard.ContainsText()) clip = Clipboard.GetText() ?? "";
		}
		catch (Exception)
		{
		}
		Preset detected = LookStore.TryParseShareText(clip, _store.Schema);
		string promptText = (detected != null)
			? ("Clipboard preset detected (“" + detected.Name + "”). Press OK to apply, or paste another code:")
			: "Paste a VisualBee share code (VB2:...) or preset text:";
		string raw = Prompt.Ask(this, "Paste shared look", promptText, (detected != null) ? clip.Trim() : "");
		if (string.IsNullOrWhiteSpace(raw)) return;

		Preset parsed = LookStore.TryParseShareText(raw, _store.Schema);
		if (parsed == null)
		{
			MessageBox.Show(this, "Could not find a valid VisualBee preset or share code in that text.", _store.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}

		_store.Apply(parsed);
		_store.Save();

		string saveName = Prompt.Ask(this, "Save shared look", "Applied “" + parsed.Name + "”. Enter a name to keep it in User Presets (or Cancel to just preview):", parsed.Name);
		if (!string.IsNullOrWhiteSpace(saveName))
		{
			parsed.Name = saveName.Trim();
			parsed.File = null;
			if (ConfirmAndSaveUserPreset(parsed))
			{
				_store.PresetName = parsed.Name;
				_store.Modified = false;
				_store.Save();
				if (_page == PresetsPage) ShowPage(PresetsPage);
				else ShowStatus();
				ShowNotice("Shared look saved", "Applied and saved “" + parsed.Name + "”.");
				return;
			}
		}
		ShowStatus();
		ShowNotice("Shared look applied", "Click “Save…” anytime to keep “" + parsed.Name + "”.");
	}

	private void ExportCurrentLookIni()
	{
		string name = !string.IsNullOrWhiteSpace(_store.PresetName) ? _store.PresetName : "Custom Look";
		ExportLookIni(_store.Capture(name, ""));
	}

	private void ExportLookIni(Preset p)
	{
		if (p == null) return;
		using SaveFileDialog dialog = new SaveFileDialog
		{
			Filter = "VisualBee Preset (*.ini)|*.ini|All files (*.*)|*.*",
			Title = "Export preset",
			FileName = LookStore.SafeName(p.Name) + ".ini"
		};
		if (dialog.ShowDialog(this) == DialogResult.OK)
		{
			_store.WritePreset(p, dialog.FileName);
			ShowNotice("Preset exported", "Saved " + Path.GetFileName(dialog.FileName));
		}
	}

	private void ImportLook()
	{
		using OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Filter = "Presets (*.ini)|*.ini|All files (*.*)|*.*",
			Title = "Import a preset"
		};
		if (openFileDialog.ShowDialog(this) == DialogResult.OK)
		{
			ImportPresetFile(openFileDialog.FileName);
		}
	}

	private void ImportPresetFile(string path)
	{
		Preset preset = LookStore.ReadPreset(path);
		if (preset == null || preset.Values.Count == 0)
		{
			MessageBox.Show(this, "That file has no settings in it.", _store.Title);
			return;
		}
		preset.File = null;
		if (!ConfirmAndSaveUserPreset(preset)) return;
		_store.Apply(preset);
		_store.Save();
		if (_page == PresetsPage && _search.Box.Text.Trim().Length == 0) ShowPage(PresetsPage);
		else ShowStatus();
		ShowNotice("Preset imported", "Applied and saved “" + preset.Name + "”.");
	}

	private void OnWindowDragEnter(object sender, DragEventArgs e)
	{
		if (e.Data.GetDataPresent(DataFormats.FileDrop) && e.Data.GetData(DataFormats.FileDrop) is string[] files
			&& files.Any(f => string.Equals(Path.GetExtension(f), ".ini", StringComparison.OrdinalIgnoreCase)))
		{
			e.Effect = DragDropEffects.Copy;
		}
	}

	private void OnWindowDragDrop(object sender, DragEventArgs e)
	{
		if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
		{
			string ini = files.FirstOrDefault(f => string.Equals(Path.GetExtension(f), ".ini", StringComparison.OrdinalIgnoreCase));
			if (!string.IsNullOrEmpty(ini)) ImportPresetFile(ini);
		}
	}

	private void ShowQuickShareMenu()
	{
		ContextMenuStrip menu = BuildShareMenu(includeRevert: false);
		menu.Show(_quickShare, new Point(0, _quickShare.Height + Ui.S(4)));
	}

	private void ShowActiveLookMenu(Point at)
	{
		ContextMenuStrip menu = BuildShareMenu(includeRevert: true);
		menu.Show(_lookStatus, at);
	}

	private ContextMenuStrip BuildShareMenu(bool includeRevert)
	{
		ContextMenuStrip menu = new ContextMenuStrip
		{
			Renderer = new DarkMenuRenderer(),
			ShowImageMargin = false
		};
		menu.Items.Add(new ToolStripMenuItem("Copy share code for current look", null, (s, a) => CopyCurrentShareCode()) { Font = Ui.Bold });
		menu.Items.Add(new ToolStripMenuItem("Paste share code…", null, (s, a) => PasteLook()));
		menu.Items.Add(new ToolStripSeparator());
		menu.Items.Add(new ToolStripMenuItem("Save current look as preset…", null, (s, a) => SaveLook()));
		menu.Items.Add(new ToolStripMenuItem("Export current look (.ini)…", null, (s, a) => ExportCurrentLookIni()));
		menu.Items.Add(new ToolStripMenuItem("Import preset (.ini)…", null, (s, a) => ImportLook()));
		if (includeRevert && _store.Modified && !string.IsNullOrEmpty(_store.PresetName))
		{
			Preset original = _store.BuiltIn.Concat(_store.UserPresets()).FirstOrDefault(p => string.Equals(p.Name, _store.PresetName, StringComparison.Ordinal));
			if (original != null)
			{
				menu.Items.Add(new ToolStripSeparator());
				menu.Items.Add(new ToolStripMenuItem("Revert to “" + original.Name + "”", null, (s, a) => ApplyPreset(original)));
			}
		}
		return menu;
	}

	private void OpenFolder()
	{
		string folder = _store.PresetFolder;
		Directory.CreateDirectory(folder);
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = folder,
				UseShellExecute = true
			});
		}
		catch (Exception)
		{
		}
	}

	private void PresetMenu(Preset p, Control near, Point at)
	{
		ContextMenuStrip menu = new ContextMenuStrip
		{
			Renderer = new DarkMenuRenderer(),
			ShowImageMargin = false
		};
		string useLabel = (IsCurrent(p) && _store.Modified) ? "Revert changes to this preset" : "Use";
		menu.Items.Add(new ToolStripMenuItem(useLabel, null, (object s, EventArgs a) => ApplyPreset(p)) { Font = Ui.Bold });
		menu.Items.Add(new ToolStripMenuItem("Copy share code", null, (object s, EventArgs a) => CopyPresetShareCode(p)));
		menu.Items.Add(new ToolStripMenuItem("Export .ini…", null, (object s, EventArgs a) => ExportLookIni(p)));
		if (!p.BuiltIn)
		{
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add(new ToolStripMenuItem("Rename…", null, (object s, EventArgs a) => RenameLook(p)));
			menu.Items.Add(new ToolStripMenuItem("Update with current settings", null, (object s, EventArgs a) => OverwriteLook(p)));
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add(new ToolStripMenuItem("Delete", null, (object s, EventArgs a) => DeleteLook(p)));
		}
		menu.Show(near, at);
	}

	private void RenameLook(Preset p)
	{
		string text = Prompt.Ask(this, "Rename look", "New name for “" + p.Name + "”:", p.Name);
		if (!string.IsNullOrWhiteSpace(text) && !(text == p.Name))
		{
			_store.RenamePreset(p, text.Trim());
			ShowPage(PresetsPage);
		}
	}

	private void OverwriteLook(Preset p)
	{
		if (MessageBox.Show(this, "Replace the settings in “" + p.Name + "” with the settings you have now?", _store.Title, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
		{
			Preset preset = _store.Capture(p.Name, p.Description);
			preset.File = p.File;
			_store.SavePreset(preset);
			_store.PresetName = p.Name;
			_store.Modified = false;
			_store.Save();
			ShowPage(PresetsPage);
		}
	}

	private void DeleteLook(Preset p)
	{
		if (MessageBox.Show(this, "Delete your look “" + p.Name + "”?", _store.Title, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
		{
			_store.DeletePreset(p);
			ShowPage(PresetsPage);
		}
	}
}
