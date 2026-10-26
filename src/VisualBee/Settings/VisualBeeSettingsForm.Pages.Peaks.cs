using System;
using System.Drawing;
using System.Windows.Forms;
using MusicBeePlugin.SettingsUi;

namespace MusicBeePlugin
{
    public sealed partial class VisualBeeSettingsForm
    {
        private VbToggle _togPeakEnabled;
        private VbSlider _sldPeakHold;
        private VbSlider _sldPeakGravity;
        private VbSlider _sldPeakThickness;
        private VbSegmented _segPeakColorMode;
        private VbSwatch _swPeakCustom;

        private void BuildPeaksPage(Panel host)
        {
            var pnl = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(8)
            };

            pnl.Controls.Add(CreateHeaderLabel("PEAK CAPS"));

            _togPeakEnabled = new VbToggle
            {
                Width = 520,
                Label = "Enable Floating Peak Caps",
                Checked = _settings.PeakHoldEnabled
            };
            _togPeakEnabled.CheckedChanged += chk =>
            {
                _settings.PeakHoldEnabled = chk;
                _settings.NotifyChanged();
                _settings.RequestSave();
                UpdatePeakControlsState();
            };
            pnl.Controls.Add(_togPeakEnabled);

            pnl.Controls.Add(CreateSpacer(8));
            pnl.Controls.Add(CreateHeaderLabel("PHYSICS & DYNAMICS"));

            _sldPeakHold = new VbSlider
            {
                Width = 520,
                Label = "Peak Hold Time",
                Minimum = 50f,
                Maximum = 1500f,
                DefaultValue = 350f,
                Value = _settings.PeakHoldMs,
                DecimalPlaces = 0,
                Unit = "ms"
            };
            _sldPeakHold.ValueChanged += val =>
            {
                _settings.PeakHoldMs = val;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_sldPeakHold);

            _sldPeakGravity = new VbSlider
            {
                Width = 520,
                Label = "Fall Speed (Gravity)",
                Minimum = 0.5f,
                Maximum = 8.0f,
                DefaultValue = 2.8f,
                Value = _settings.PeakGravity,
                DecimalPlaces = 1,
                Unit = "g"
            };
            _sldPeakGravity.ValueChanged += val =>
            {
                _settings.PeakGravity = val;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_sldPeakGravity);

            pnl.Controls.Add(CreateSpacer(8));
            pnl.Controls.Add(CreateHeaderLabel("CAP APPEARANCE"));

            _sldPeakThickness = new VbSlider
            {
                Width = 520,
                Label = "Cap Thickness",
                Minimum = 1.0f,
                Maximum = 6.0f,
                DefaultValue = 2.0f,
                Value = _settings.PeakCapThickness,
                DecimalPlaces = 1,
                Unit = "px"
            };
            _sldPeakThickness.ValueChanged += val =>
            {
                _settings.PeakCapThickness = val;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_sldPeakThickness);

            pnl.Controls.Add(CreateSpacer(6));
            _segPeakColorMode = new VbSegmented
            {
                Width = 520,
                Items = new[] { "Palette Tip Highlight", "Bar Color Tip", "Custom Color" },
                SelectedIndex = (int)_settings.PeakCapColorMode
            };
            _segPeakColorMode.SelectedIndexChanged += idx =>
            {
                _settings.PeakCapColorMode = (PeakColorMode)idx;
                _settings.NotifyChanged();
                _settings.RequestSave();
                if (_swPeakCustom != null) _swPeakCustom.Enabled = (_settings.PeakCapColorMode == PeakColorMode.Custom);
            };
            pnl.Controls.Add(_segPeakColorMode);

            pnl.Controls.Add(CreateSpacer(6));
            _swPeakCustom = new VbSwatch
            {
                Width = 520,
                Label = "Custom Cap Color",
                Color = _settings.PeakCustomColor,
                Enabled = (_settings.PeakCapColorMode == PeakColorMode.Custom)
            };
            _swPeakCustom.ColorChanged += col =>
            {
                _settings.PeakCustomColor = col;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_swPeakCustom);

            UpdatePeakControlsState();
            host.Controls.Add(pnl);
        }

        private void UpdatePeakControlsState()
        {
            bool en = _settings.PeakHoldEnabled;
            if (_sldPeakHold != null) _sldPeakHold.Enabled = en;
            if (_sldPeakGravity != null) _sldPeakGravity.Enabled = en;
            if (_sldPeakThickness != null) _sldPeakThickness.Enabled = en;
            if (_segPeakColorMode != null) _segPeakColorMode.Enabled = en;
            if (_swPeakCustom != null) _swPeakCustom.Enabled = (en && _settings.PeakCapColorMode == PeakColorMode.Custom);
        }

        private void RefreshPeaksPageUi()
        {
            if (_togPeakEnabled != null) _togPeakEnabled.Checked = _settings.PeakHoldEnabled;
            if (_sldPeakHold != null) _sldPeakHold.Value = _settings.PeakHoldMs;
            if (_sldPeakGravity != null) _sldPeakGravity.Value = _settings.PeakGravity;
            if (_sldPeakThickness != null) _sldPeakThickness.Value = _settings.PeakCapThickness;
            if (_segPeakColorMode != null) _segPeakColorMode.SelectedIndex = (int)_settings.PeakCapColorMode;
            if (_swPeakCustom != null)
            {
                _swPeakCustom.Color = _settings.PeakCustomColor;
                _swPeakCustom.Enabled = (_settings.PeakHoldEnabled && _settings.PeakCapColorMode == PeakColorMode.Custom);
            }
            UpdatePeakControlsState();
        }
    }
}
