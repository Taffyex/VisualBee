using System;
using System.Drawing;
using System.Windows.Forms;
using MusicBeePlugin.SettingsUi;

namespace MusicBeePlugin
{
    public sealed partial class VisualBeeSettingsForm
    {
        private ComboBox _cboPresets;
        private FlowLayoutPanel _pnlSwatches;
        private VbSegmented _segColorMode;
        private VbSegmented _segHueSource;
        private VbSegmented _segHueMotion;
        private VbSlider _sldHueSpeed;

        // Atmosphere & Background Effects
        private VbToggle _togParticles;
        private VbSlider _sldPartInt;
        private VbSlider _sldPartParallax;
        private VbToggle _togCyberGrid;
        private VbSlider _sldCyberGrid;
        private VbSlider _sldScanline;

        // Lighting & Environment
        private VbSlider _sldBloom;
        private VbToggle _togIncandescence;
        private VbSegmented _segBgSource;
        private VbSlider _sldBgBrightness;
        private VbSlider _sldVignette;
        private VbSlider _sldFloorReflection;
        private VbToggle _togDecibelGrid;
        private VbSlider _sldDecibelGrid;

        private void BuildThemePage(Panel host)
        {
            var pnl = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(8)
            };

            pnl.Controls.Add(CreateHeaderLabel("COLOR THEME & PRESETS"));

            var pnlPresetRow = new Panel { Width = 540, Height = 32 };
            _cboPresets = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 340,
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
                Left = 350,
                Width = 130,
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

            pnl.Controls.Add(CreateSpacer(6));
            pnl.Controls.Add(CreateHeaderLabel("PALETTE SWATCHES (Click to customize)"));
            _pnlSwatches = new FlowLayoutPanel { Width = 540, Height = 44, FlowDirection = FlowDirection.LeftToRight };
            UpdateSwatches();
            pnl.Controls.Add(_pnlSwatches);

            pnl.Controls.Add(CreateSpacer(6));
            pnl.Controls.Add(CreateHeaderLabel("COLOR MODE"));
            _segColorMode = new VbSegmented
            {
                Width = 540,
                Items = new[] { "Vertical Gradient", "Across Frequencies", "Solid Tip Accent", "Hue Cycle" },
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

            _segHueSource = new VbSegmented
            {
                Width = 540,
                Items = new[] { "Selected Palette Colors", "Full Rainbow Spectrum" },
                SelectedIndex = (int)_settings.HueSource,
                Enabled = (_settings.ColorMode == BarColorMode.HueCycle)
            };
            _segHueSource.SelectedIndexChanged += idx => { _settings.HueSource = (HueCycleSource)idx; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_segHueSource);

            _segHueMotion = new VbSegmented
            {
                Width = 540,
                Items = new[] { "Sideways Wave", "Spin (Diagonal)" },
                SelectedIndex = (int)_settings.HueMotion,
                Enabled = (_settings.ColorMode == BarColorMode.HueCycle)
            };
            _segHueMotion.SelectedIndexChanged += idx => { _settings.HueMotion = (HueCycleMotion)idx; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_segHueMotion);

            _sldHueSpeed = new VbSlider
            {
                Width = 540,
                Label = "Hue Rotation Speed",
                Minimum = 0.05f,
                Maximum = 2.0f,
                DefaultValue = 0.25f,
                Value = _settings.HueSpeed,
                DecimalPlaces = 2,
                Unit = "x",
                Enabled = (_settings.ColorMode == BarColorMode.HueCycle)
            };
            _sldHueSpeed.ValueChanged += val => { _settings.HueSpeed = val; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_sldHueSpeed);

            // Particles & Atmosphere Card
            pnl.Controls.Add(CreateSpacer(8));
            pnl.Controls.Add(CreateHeaderLabel("ATMOSPHERE & PARTICLE EFFECTS (GPU Shaders)"));

            _togParticles = new VbToggle { Width = 540, Label = "3D Parallax Bokeh & Floating Embers", Checked = _settings.ParticlesEnabled };
            _togParticles.CheckedChanged += chk => { _settings.ParticlesEnabled = chk; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_togParticles);

            _sldPartInt = new VbSlider { Width = 540, Label = "Bokeh Embers Intensity", Minimum = 0f, Maximum = 100f, DefaultValue = 45f, Value = _settings.ParticleIntensity * 100f, Unit = "%" };
            _sldPartInt.ValueChanged += val => { _settings.ParticleIntensity = val / 100f; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_sldPartInt);

            _sldPartParallax = new VbSlider { Width = 540, Label = "3D Depth Parallax Drift", Minimum = 0f, Maximum = 100f, DefaultValue = 60f, Value = _settings.ParticleParallax * 100f, Unit = "%" };
            _sldPartParallax.ValueChanged += val => { _settings.ParticleParallax = val / 100f; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_sldPartParallax);

            _togCyberGrid = new VbToggle { Width = 540, Label = "3D Horizon Cyber-Grid", Checked = _settings.CyberGridEnabled };
            _togCyberGrid.CheckedChanged += chk => { _settings.CyberGridEnabled = chk; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_togCyberGrid);

            _sldCyberGrid = new VbSlider { Width = 540, Label = "Cyber-Grid Luminance", Minimum = 0f, Maximum = 100f, DefaultValue = 35f, Value = _settings.CyberGridIntensity * 100f, Unit = "%" };
            _sldCyberGrid.ValueChanged += val => { _settings.CyberGridIntensity = val / 100f; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_sldCyberGrid);

            _sldScanline = new VbSlider { Width = 540, Label = "CRT / VFD Scanline Shimmer", Minimum = 0f, Maximum = 50f, DefaultValue = 15f, Value = _settings.ScanlineShimmer * 100f, Unit = "%" };
            _sldScanline.ValueChanged += val => { _settings.ScanlineShimmer = val / 100f; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_sldScanline);

            // Lighting & Shading Card
            pnl.Controls.Add(CreateSpacer(8));
            pnl.Controls.Add(CreateHeaderLabel("LIGHTING & ENVIRONMENT SHADING"));

            _sldBloom = new VbSlider { Width = 540, Label = "Bloom Glow Intensity", Minimum = 0f, Maximum = 2.5f, DefaultValue = 0.95f, Value = _settings.BloomIntensity, DecimalPlaces = 2, Unit = "x" };
            _sldBloom.ValueChanged += val => { _settings.BloomIntensity = val; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_sldBloom);

            _togIncandescence = new VbToggle { Width = 540, Label = "Amplitude HDR Incandescence (White-Hot Core Overdrive)", Checked = _settings.HdrIncandescence };
            _togIncandescence.CheckedChanged += chk => { _settings.HdrIncandescence = chk; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_togIncandescence);

            _segBgSource = new VbSegmented
            {
                Width = 540,
                Items = new[] { "Palette Theme Dark", "Match MusicBee Skin Panel", "Pure Black (OLED)" },
                SelectedIndex = (int)_settings.BgSource
            };
            _segBgSource.SelectedIndexChanged += idx => { _settings.BgSource = (BackgroundSource)idx; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_segBgSource);

            _sldBgBrightness = new VbSlider { Width = 540, Label = "Background Brightness", Minimum = 0f, Maximum = 2.0f, DefaultValue = 1.0f, Value = _settings.BgBrightness, DecimalPlaces = 2, Unit = "x" };
            _sldBgBrightness.ValueChanged += val => { _settings.BgBrightness = val; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_sldBgBrightness);

            _sldVignette = new VbSlider { Width = 540, Label = "Vignette Shading", Minimum = 0f, Maximum = 100f, DefaultValue = 35f, Value = _settings.Vignette * 100f, Unit = "%" };
            _sldVignette.ValueChanged += val => { _settings.Vignette = val / 100f; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_sldVignette);

            _sldFloorReflection = new VbSlider { Width = 540, Label = "Wet Lacquer Horizon Floor Reflection", Minimum = 0f, Maximum = 100f, DefaultValue = 25f, Value = _settings.FloorReflection * 100f, Unit = "%" };
            _sldFloorReflection.ValueChanged += val => { _settings.FloorReflection = val / 100f; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_sldFloorReflection);

            _togDecibelGrid = new VbToggle { Width = 540, Label = "Studio Decibel Reference Grid Lines (-24dB .. 0dB)", Checked = _settings.DecibelGrid };
            _togDecibelGrid.CheckedChanged += chk => { _settings.DecibelGrid = chk; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_togDecibelGrid);

            _sldDecibelGrid = new VbSlider { Width = 540, Label = "Decibel Grid Opacity", Minimum = 2f, Maximum = 40f, DefaultValue = 12f, Value = _settings.DecibelGridOpacity * 100f, Unit = "%" };
            _sldDecibelGrid.ValueChanged += val => { _settings.DecibelGridOpacity = val / 100f; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_sldDecibelGrid);

            UpdateHueControlsState();
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
                int colorIdx = i;
                var pBox = new Panel { Width = 64, Height = 36, BackColor = colors[i], Margin = new Padding(4), Cursor = Cursors.Hand };
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
                pBox.Click += delegate { PickSwatchColor(colorIdx, palette); };
                lbl.Click += delegate { PickSwatchColor(colorIdx, palette); };
                _pnlSwatches.Controls.Add(pBox);
            }
        }

        private void PickSwatchColor(int index, PaletteColors p)
        {
            using (var dlg = new ColorDialog())
            {
                Color cur = index == 0 ? p.ColorA : index == 1 ? p.ColorB : index == 2 ? p.ColorC : index == 3 ? p.ColorD : p.Background;
                dlg.Color = cur;
                dlg.FullOpen = true;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    Color newCol = dlg.Color;
                    Color a = index == 0 ? newCol : p.ColorA;
                    Color b = index == 1 ? newCol : p.ColorB;
                    Color c = index == 2 ? newCol : p.ColorC;
                    Color d = index == 3 ? newCol : p.ColorD;
                    Color bg = index == 4 ? newCol : p.Background;
                    string customKey = "custom_" + DateTime.Now.ToString("HHmmss");
                    PresetManager.SaveCustomPreset(customKey, new PaletteColors(a, b, c, d, bg));
                    _settings.Palette = customKey;
                    _settings.NotifyChanged();
                    _settings.RequestSave();
                    _control?.RefreshPalette();
                    PopulatePresetCombo();
                    UpdateSwatches();
                }
            }
        }

        private void UpdateHueControlsState()
        {
            bool isHue = (_settings.ColorMode == BarColorMode.HueCycle);
            if (_segHueSource != null) _segHueSource.Enabled = isHue;
            if (_segHueMotion != null) _segHueMotion.Enabled = isHue;
            if (_sldHueSpeed != null) _sldHueSpeed.Enabled = isHue;
        }

        private void RefreshThemePageUi()
        {
            PopulatePresetCombo();
            UpdateSwatches();
            if (_segColorMode != null) _segColorMode.SelectedIndex = (int)_settings.ColorMode;
            if (_segHueSource != null) _segHueSource.SelectedIndex = (int)_settings.HueSource;
            if (_segHueMotion != null) _segHueMotion.SelectedIndex = (int)_settings.HueMotion;
            if (_sldHueSpeed != null) _sldHueSpeed.Value = _settings.HueSpeed;
            if (_togParticles != null) _togParticles.Checked = _settings.ParticlesEnabled;
            if (_sldPartInt != null) _sldPartInt.Value = _settings.ParticleIntensity * 100f;
            if (_sldPartParallax != null) _sldPartParallax.Value = _settings.ParticleParallax * 100f;
            if (_togCyberGrid != null) _togCyberGrid.Checked = _settings.CyberGridEnabled;
            if (_sldCyberGrid != null) _sldCyberGrid.Value = _settings.CyberGridIntensity * 100f;
            if (_sldScanline != null) _sldScanline.Value = _settings.ScanlineShimmer * 100f;
            if (_sldBloom != null) _sldBloom.Value = _settings.BloomIntensity;
            if (_togIncandescence != null) _togIncandescence.Checked = _settings.HdrIncandescence;
            if (_segBgSource != null) _segBgSource.SelectedIndex = (int)_settings.BgSource;
            if (_sldBgBrightness != null) _sldBgBrightness.Value = _settings.BgBrightness;
            if (_sldVignette != null) _sldVignette.Value = _settings.Vignette * 100f;
            if (_sldFloorReflection != null) _sldFloorReflection.Value = _settings.FloorReflection * 100f;
            if (_togDecibelGrid != null) _togDecibelGrid.Checked = _settings.DecibelGrid;
            if (_sldDecibelGrid != null) _sldDecibelGrid.Value = _settings.DecibelGridOpacity * 100f;
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
