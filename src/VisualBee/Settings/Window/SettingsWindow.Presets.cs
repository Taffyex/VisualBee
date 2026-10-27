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
		AddTitle("Your Presets");
		AddSection("Custom looks you saved or imported", null);
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
		flatButton.Click += (object s, EventArgs a) =>
		{
			SaveLook();
		};
		FlatButton flatButton2 = new FlatButton("Import…", "\ue8b5");
		flatButton2.Click += (object s, EventArgs a) =>
		{
			ImportLook();
		};
		FlatButton flatButton3 = new FlatButton("Open folder", "\ue838");
		flatButton3.Click += (object s, EventArgs a) =>
		{
			OpenFolder();
		};
		_tips.SetToolTip(flatButton, "Saves every setting that differs from the defaults as a preset of your own");
		_tips.SetToolTip(flatButton2, "Adds a preset file (.ini) someone shared");
		_tips.SetToolTip(flatButton3, "Shows where your presets are kept, to copy or share them");
		flowPanelUser.Controls.Add(flatButton);
		flowPanelUser.Controls.Add(flatButton2);
		flowPanelUser.Controls.Add(flatButton3);
		_content.Controls.Add(flowPanelUser);

		AddTitle("Signature Looks");
		AddSection("Built-in studio presets", null);
		FlowPanel flowPanelBuiltin = new FlowPanel();
		foreach (Preset item in _store.BuiltIn)
		{
			flowPanelBuiltin.Controls.Add(new PresetCard(this, item));
		}
		_content.Controls.Add(flowPanelBuiltin);

		AddTitle("Colour Themes");
		AddSection("Changes only the colours; your layout & timing stay", null);
		FlowPanel flowPanelThemes = new FlowPanel();
		foreach (Theme theme in _store.Themes)
		{
			flowPanelThemes.Controls.Add(new ThemeChip(this, theme));
		}
		_content.Controls.Add(flowPanelThemes);
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
		string presetFolder = _store.PresetFolder;
		if (string.IsNullOrEmpty(presetFolder))
		{
			return;
		}
		try
		{
			Directory.CreateDirectory(presetFolder);
			Process.Start("explorer.exe", "\"" + presetFolder + "\"");
		}
		catch (Exception)
		{
		}
	}

	public void PresetMenu(Preset p, Control at, Point where)
	{
		if (p.BuiltIn)
		{
			return;
		}
		ContextMenuStrip menu = new ContextMenuStrip
		{
			Renderer = new DarkMenuRenderer(),
			ShowImageMargin = false,
			Font = Ui.Body
		};
		menu.Items.Add("Update with the current look", null, (object s, EventArgs a) =>
		{
			Preset preset = _store.Capture(p.Name, p.Description);
			preset.File = p.File;
			_store.SavePreset(preset);
			_store.PresetName = p.Name;
			_store.Modified = false;
			_store.Save();
			ShowPage("Presets");
		});
		menu.Items.Add("Rename…", null, (object s, EventArgs a) =>
		{
			string text = Prompt.Ask(this, "Rename look", "New name:", p.Name);
			if (!string.IsNullOrWhiteSpace(text) && !(text.Trim() == p.Name))
			{
				string file = p.File;
				p.Name = text.Trim();
				p.File = null;
				_store.SavePreset(p);
				TryDelete(file);
				ShowPage("Presets");
			}
		});
		menu.Items.Add("Export…", null, (object s, EventArgs a) =>
		{
			using SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				Filter = "Presets (*.ini)|*.ini",
				FileName = LookStore.SafeName(p.Name) + ".ini"
			};
			if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				_store.WritePreset(p, saveFileDialog.FileName);
			}
		});
		menu.Items.Add(new ToolStripSeparator());
		menu.Items.Add("Delete", null, (object s, EventArgs a) =>
		{
			if (MessageBox.Show(this, "Delete your look “" + p.Name + "”?", _store.Title, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
			{
				TryDelete(p.File);
				ShowPage("Presets");
			}
		});
		foreach (ToolStripItem item in menu.Items)
		{
			item.ForeColor = Ui.Text;
		}
		menu.Closed += (object s, ToolStripDropDownClosedEventArgs a) =>
		{
			BeginInvoke(new Action(menu.Dispose));
		};
		menu.Show(at, where);
	}

	private static void TryDelete(string file)
	{
		try
		{
			if (!string.IsNullOrEmpty(file) && File.Exists(file))
			{
				File.Delete(file);
			}
		}
		catch (Exception)
		{
		}
	}
}