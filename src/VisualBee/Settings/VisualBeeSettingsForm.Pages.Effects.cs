using System;
using System.Drawing;
using System.Windows.Forms;
using MusicBeePlugin.SettingsUi;

namespace MusicBeePlugin
{
    public sealed partial class VisualBeeSettingsForm
    {
        private VbSlider _sldBloom;
        private VbSegmented _segBgSource;
        private VbSlider _sldBgBrightness;
        private VbSlider _sldVignette;
        private VbSlider _sldFloorReflection;
        private VbToggle _togContactGlow;

        private void BuildEffectsPage(Panel host)
        {
            var pnl = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(8)
            };

            pnl.Controls.Add(CreateHeaderLabel("LIGHTING & GLOW"));

            _sldBloom = new VbSlider
            {
                Width = 520,
                Label = "Bloom Glow Intensity",
                Minimum = 0f,
                Maximum = 2.5f,
                DefaultValue = 0.85f,
                Value = _settings.BloomIntensity,
                DecimalPlaces = 2,
                Unit = "x"
            };
            _sldBloom.ValueChanged += val =>
            {
                _settings.BloomIntensity = val;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_sldBloom);

            _togContactGlow = new VbToggle
            {
                Width = 520,
                Label = "Ground Contact Light Pool",
                Checked = _settings.ContactGlow
            };
            _togContactGlow.CheckedChanged += chk =>
            {
                _settings.ContactGlow = chk;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_togContactGlow);

            pnl.Controls.Add(CreateSpacer(8));
            pnl.Controls.Add(CreateHeaderLabel("BACKGROUND"));

            _segBgSource = new VbSegmented
            {
                Width = 520,
                Items = new[] { "Palette Theme Color", "Match Skin Panel", "Pure Black (OLED)" },
                SelectedIndex = (int)_settings.BgSource
            };
            _segBgSource.SelectedIndexChanged += idx =>
            {
                _settings.BgSource = (BackgroundSource)idx;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_segBgSource);

            pnl.Controls.Add(CreateSpacer(6));
            _sldBgBrightness = new VbSlider
            {
                Width = 520,
                Label = "Background Brightness",
                Minimum = 0f,
                Maximum = 2.0f,
                DefaultValue = 1.0f,
                Value = _settings.BgBrightness,
                DecimalPlaces = 2,
                Unit = "x"
            };
            _sldBgBrightness.ValueChanged += val =>
            {
                _settings.BgBrightness = val;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_sldBgBrightness);

            _sldVignette = new VbSlider
            {
                Width = 520,
                Label = "Edge Vignette Shading",
                Minimum = 0f,
                Maximum = 100f,
                DefaultValue = 35f,
                Value = _settings.Vignette * 100f,
                DecimalPlaces = 0,
                Unit = "%"
            };
            _sldVignette.ValueChanged += val =>
            {
                _settings.Vignette = val / 100f;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_sldVignette);

            pnl.Controls.Add(CreateSpacer(8));
            pnl.Controls.Add(CreateHeaderLabel("ENVIRONMENT REFLECTIONS"));

            _sldFloorReflection = new VbSlider
            {
                Width = 520,
                Label = "Floor Mirror Reflection",
                Minimum = 0f,
                Maximum = 100f,
                DefaultValue = 0f,
                Value = _settings.FloorReflection * 100f,
                DecimalPlaces = 0,
                Unit = "%"
            };
            _sldFloorReflection.ValueChanged += val =>
            {
                _settings.FloorReflection = val / 100f;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_sldFloorReflection);

            host.Controls.Add(pnl);
        }

        private void RefreshEffectsPageUi()
        {
            if (_sldBloom != null) _sldBloom.Value = _settings.BloomIntensity;
            if (_togContactGlow != null) _togContactGlow.Checked = _settings.ContactGlow;
            if (_segBgSource != null) _segBgSource.SelectedIndex = (int)_settings.BgSource;
            if (_sldBgBrightness != null) _sldBgBrightness.Value = _settings.BgBrightness;
            if (_sldVignette != null) _sldVignette.Value = _settings.Vignette * 100f;
            if (_sldFloorReflection != null) _sldFloorReflection.Value = _settings.FloorReflection * 100f;
        }
    }
}
