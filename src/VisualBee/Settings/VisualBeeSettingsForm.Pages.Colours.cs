using System;
using System.Drawing;
using System.Windows.Forms;
using MusicBeePlugin.SettingsUi;

namespace MusicBeePlugin
{
    public sealed partial class VisualBeeSettingsForm
    {
        private ComboBox _cboPresets;
        private VbSegmented _segColorMode;
        private VbSlider _sldHueSpeed;
        private FlowLayoutPanel _pnlSwatches;

        private void BuildColoursPage(Panel host)
        {
            var pnl = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(8)
            };

            pnl.Controls.Add(CreateHeaderLabel("THEME PRESETS"));

            var pnlPresetRow = new Panel { Width = 520, Height = 32 };
            _cboPresets = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 320,
                Height = 28,
                BackColor = VbTheme.BgCard,
                ForeColor = VbTheme.TextPrimary,
                FlatStyle = FlatStyle.Flat
            };
            PopulatePresetCombo();
            _cboPresets.SelectedIndexChanged += delegate
            {
                if (_cboPresets.SelectedItem is PresetItem item)
                {
                    _settings.Palette = item.Key;
                    _settings.NotifyChanged();
                    _settings.RequestSave();
                    _control?.RefreshPalette();
                    UpdateSwatches();
                }
            };
            pnlPresetRow.Controls.Add(_cboPresets);

            var btnMatchSkin = new Button
            {
                Text = "Match MB Skin",
                Left = 330,
                Width = 140,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = VbTheme.BgCard,
                ForeColor = VbTheme.TextPrimary,
                Cursor = Cursors.Hand
            };
            btnMatchSkin.FlatAppearance.BorderColor = VbTheme.Border;
            btnMatchSkin.FlatAppearance.MouseOverBackColor = VbTheme.BgHover;
            btnMatchSkin.Click += delegate
            {
                _settings.Palette = "skin";
                _settings.NotifyChanged();
                _settings.RequestSave();
                _control?.RefreshPalette();
                PopulatePresetCombo();
                UpdateSwatches();
            };
            pnlPresetRow.Controls.Add(btnMatchSkin);
            pnl.Controls.Add(pnlPresetRow);

            pnl.Controls.Add(CreateSpacer(8));
            pnl.Controls.Add(CreateHeaderLabel("RESOLVED PALETTE SWATCHES"));

            _pnlSwatches = new FlowLayoutPanel
            {
                Width = 520,
                Height = 44,
                FlowDirection = FlowDirection.LeftToRight
            };
            UpdateSwatches();
            pnl.Controls.Add(_pnlSwatches);

            pnl.Controls.Add(CreateSpacer(8));
            pnl.Controls.Add(CreateHeaderLabel("COLOUR MODE"));

            _segColorMode = new VbSegmented
            {
                Width = 520,
                Items = new[] { "Vertical Gradient", "Across Frequencies", "Solid Tip", "Hue Cycle" },
                SelectedIndex = (int)_settings.ColorMode
            };
            _segColorMode.SelectedIndexChanged += idx =>
            {
                _settings.ColorMode = (BarColorMode)idx;
                _settings.NotifyChanged();
                _settings.RequestSave();
                if (_sldHueSpeed != null) _sldHueSpeed.Enabled = (_settings.ColorMode == BarColorMode.HueCycle);
            };
            pnl.Controls.Add(_segColorMode);

            pnl.Controls.Add(CreateSpacer(8));
            _sldHueSpeed = new VbSlider
            {
                Width = 520,
                Label = "Hue Rotation Speed (for Hue Cycle mode)",
                Minimum = 0.05f,
                Maximum = 2.0f,
                DefaultValue = 0.25f,
                Value = _settings.HueSpeed,
                DecimalPlaces = 2,
                Unit = "x",
                Enabled = (_settings.ColorMode == BarColorMode.HueCycle)
            };
            _sldHueSpeed.ValueChanged += val =>
            {
                _settings.HueSpeed = val;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_sldHueSpeed);

            host.Controls.Add(pnl);
        }

        private void PopulatePresetCombo()
        {
            if (_cboPresets == null) return;
            _cboPresets.Items.Clear();
            var presets = PresetManager.GetAllPresets(_api);
            int selectedIdx = 0;
            for (int i = 0; i < presets.Count; i++)
            {
                _cboPresets.Items.Add(new PresetItem { Key = presets[i].Key, Name = presets[i].DisplayName });
                if (string.Equals(presets[i].Key, _settings.Palette, StringComparison.OrdinalIgnoreCase))
                    selectedIdx = i;
            }
            if (_cboPresets.Items.Count > selectedIdx)
                _cboPresets.SelectedIndex = selectedIdx;
        }

        private void UpdateSwatches()
        {
            if (_pnlSwatches == null) return;
            _pnlSwatches.Controls.Clear();
            var palette = PaletteManager.Resolve(_settings.Palette, _api);
            Color[] colors = { palette.ColorA, palette.ColorB, palette.ColorC, palette.ColorD, palette.Background };
            string[] names = { "A", "B", "C", "D", "Bg" };

            for (int i = 0; i < colors.Length; i++)
            {
                var pBox = new Panel
                {
                    Width = 64,
                    Height = 36,
                    BackColor = colors[i],
                    Margin = new Padding(4)
                };
                var lbl = new Label
                {
                    Text = names[i],
                    Dock = DockStyle.Bottom,
                    Height = 16,
                    Font = VbTheme.FontSmall,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.FromArgb(160, 0, 0, 0),
                    ForeColor = Color.White
                };
                pBox.Controls.Add(lbl);
                _pnlSwatches.Controls.Add(pBox);
            }
        }

        private void RefreshColoursPageUi()
        {
            PopulatePresetCombo();
            UpdateSwatches();
            if (_segColorMode != null) _segColorMode.SelectedIndex = (int)_settings.ColorMode;
            if (_sldHueSpeed != null)
            {
                _sldHueSpeed.Value = _settings.HueSpeed;
                _sldHueSpeed.Enabled = (_settings.ColorMode == BarColorMode.HueCycle);
            }
        }

        private sealed class PresetItem
        {
            public string Key;
            public string Name;
            public override string ToString() => Name;
        }
    }
}
