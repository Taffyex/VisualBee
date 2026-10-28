using System.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MusicBeePlugin;

internal sealed partial class SettingsWindow : Form
{
	private void BuildPresets()
	{
		AddTitle("Signature Presets");
		FlowPanel flowPanelBuiltin = new FlowPanel();
		foreach (Preset item in _store.BuiltIn)
		{
			flowPanelBuiltin.Controls.Add(new PresetCard(this, item));
		}
		_content.Controls.Add(flowPanelBuiltin);

		AddTitle("Studio Colour Palettes");
		FlowPanel flowPanelThemes = new FlowPanel();
		foreach (Theme theme in _store.Themes)
		{
			flowPanelThemes.Controls.Add(new ThemeChip(this, theme));
		}
		_content.Controls.Add(flowPanelThemes);

		AddTitle("User Presets");
		FlowPanel flowPanelUser = new FlowPanel();
		List<Preset> list = _store.UserPresets();
		foreach (Preset item2 in list)
		{
			flowPanelUser.Controls.Add(new PresetCard(this, item2));
		}
		FlatButton flatButton = new FlatButton("Save current look…", "\ue74e")
		{
			Primary = true
		};
		flatButton.Click += (object s, EventArgs a) => SaveLook();
		FlatButton flatButton2 = new FlatButton("Import…", "\ue8b5");
		flatButton2.Click += (object s, EventArgs a) => ImportLook();
		FlatButton flatButton3 = new FlatButton("Open folder", "\ue838");
		flatButton3.Click += (object s, EventArgs a) => OpenFolder();
		_tips.SetToolTip(flatButton, "Saves every setting that differs from defaults as a preset");
		_tips.SetToolTip(flatButton2, "Import preset file (.ini)");
		_tips.SetToolTip(flatButton3, "Shows where your presets are stored");
		flowPanelUser.Controls.Add(flatButton);
		flowPanelUser.Controls.Add(flatButton2);
		flowPanelUser.Controls.Add(flatButton3);
		_content.Controls.Add(flowPanelUser);
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
		string name = Prompt.Ask(this, "Save look", "Name for this look:", (_store.PresetName.Length > 0 && !_store.BuiltIn.Any((Preset b) => b.Name == _store.PresetName)) ? _store.PresetName : "My look");
		if (!string.IsNullOrWhiteSpace(name))
		{
			name = name.Trim();
			Preset preset = _store.UserPresets().FirstOrDefault((Preset u) => string.Equals(u.Name, name, StringComparison.CurrentCultureIgnoreCase));
			if (preset == null || MessageBox.Show(this, "Replace your look “" + name + "”?", _store.Title, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
			{
				Preset preset2 = _store.Capture(name, "");
				preset2.File = preset?.File;
				_store.SavePreset(preset2);
				_store.PresetName = name;
				_store.Modified = false;
				_store.Save();
				ShowPage("Presets");
			}
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
			Preset preset = LookStore.ReadPreset(openFileDialog.FileName);
			if (preset == null || preset.Values.Count == 0)
			{
				MessageBox.Show(this, "That file has no settings in it.", _store.Title);
				return;
			}
			preset.File = null;
			_store.SavePreset(preset);
			ShowPage("Presets");
		}
	}

	private void OpenFolder()
	{
		string text = _store.PresetFolder;
		Directory.CreateDirectory(text);
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = text,
				UseShellExecute = true
			});
		}
		catch (Exception)
		{
		}
	}

	private void PresetMenu(Preset p, Control near, Point at)
	{
		ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
		ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem("Use", null, (object s, EventArgs a) => ApplyPreset(p));
		toolStripMenuItem.Font = Ui.Bold;
		contextMenuStrip.Items.Add(toolStripMenuItem);
		if (!p.BuiltIn)
		{
			contextMenuStrip.Items.Add(new ToolStripMenuItem("Rename…", null, (object s, EventArgs a) => RenameLook(p)));
			contextMenuStrip.Items.Add(new ToolStripMenuItem("Update with current settings", null, (object s, EventArgs a) => OverwriteLook(p)));
			contextMenuStrip.Items.Add(new ToolStripSeparator());
			contextMenuStrip.Items.Add(new ToolStripMenuItem("Delete", null, (object s, EventArgs a) => DeleteLook(p)));
		}
		contextMenuStrip.Show(near, at);
	}

	private void RenameLook(Preset p)
	{
		string text = Prompt.Ask(this, "Rename look", "New name for “" + p.Name + "”:", p.Name);
		if (!string.IsNullOrWhiteSpace(text) && !(text == p.Name))
		{
			_store.RenamePreset(p, text.Trim());
			ShowPage("Presets");
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
			ShowPage("Presets");
		}
	}

	private void DeleteLook(Preset p)
	{
		if (MessageBox.Show(this, "Delete your look “" + p.Name + "”?", _store.Title, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
		{
			_store.DeletePreset(p);
			ShowPage("Presets");
		}
	}
}
