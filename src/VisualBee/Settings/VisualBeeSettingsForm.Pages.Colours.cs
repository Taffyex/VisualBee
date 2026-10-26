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
        private VbSegmented _segHueSource;
        private VbSegmented _segHueMotion;
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

            pnl.Controls.Add(CreateHeaderLabel("COLOURS & THEME"));
            pnl.Controls.Add(CreateSubtitleLabel("Applies to all modes"));

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
                UpdateHueControlsState();
            };
            pnl.Controls.Add(_segColorMode);

            pnl.Controls.Add(CreateSpacer(8));
            pnl.Controls.Add(CreateHeaderLabel("HUE CYCLE SETTINGS"));

            _segHueSource = new VbSegmented
            {
                Width = 520,
                Items = new[] { "Selected Palette Colors", "Full Rainbow Spectrum" },
                SelectedIndex = (int)_settings.HueSource,
                Enabled = (_settings.ColorMode == BarColorMode.HueCycle)
            };
            _segHueSource.SelectedIndexChanged += idx =>
            {
                _settings.HueSource = (HueCycleSource)idx;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_segHueSource);

            pnl.Controls.Add(CreateSpacer(6));

            _segHueMotion = new VbSegmented
            {
                Width = 520,
                Items = new[] { "Sideways Wave", "Spin (Diagonal)" },
                SelectedIndex = (int)_settings.HueMotion,
                Enabled = (_settings.ColorMode == BarColorMode.HueCycle)
            };
            _segHueMotion.SelectedIndexChanged += idx =>
            {
                _settings.HueMotion = (HueCycleMotion)idx;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_segHueMotion);

            pnl.Controls.Add(CreateSpacer(6));
            _sldHueSpeed = new VbSlider
            {
                Width = 520,
                Label = "Hue Rotation Speed",
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

        private void UpdateHueControlsState()
        {
            bool isHue = (_settings.ColorMode == BarColorMode.HueCycle);
            if (_segHueSource != null) _segHueSource.Enabled = isHue;
            if (_segHueMotion != null) _segHueMotion.Enabled = isHue;
            if (_sldHueSpeed != null) _sldHueSpeed.Enabled = isHue;
        }

        private void RefreshColoursPageUi()
        {
            PopulatePresetCombo();
            UpdateSwatches();
            if (_segColorMode != null) _segColorMode.SelectedIndex = (int)_settings.ColorMode;
            if (_segHueSource != null) _segHueSource.SelectedIndex = (int)_settings.HueSource;
            if (_segHueMotion != null) _segHueMotion.SelectedIndex = (int)_settings.HueMotion;
            if (_sldHueSpeed != null) _sldHueSpeed.Value = _settings.HueSpeed;
            UpdateHueControlsState();
        }

        private sealed class PresetItem
        {
            public string Key;
            public string Name;
            public override string ToString() => Name;
        }
    }
}
