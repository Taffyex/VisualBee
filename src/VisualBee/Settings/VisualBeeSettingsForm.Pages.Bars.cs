using System;
using System.Drawing;
using System.Windows.Forms;
using MusicBeePlugin.SettingsUi;

namespace MusicBeePlugin
{
    public sealed partial class VisualBeeSettingsForm
    {
        private VbSegmented _segBarStyle;
        private VbSlider _sldBarCount;
        private VbSlider _sldBarGap;
        private VbSlider _sldRoundness;
        private VbSlider _sldTrebleTaper;
        private VbSlider _sldLedSegH;
        private VbSlider _sldLedSegGap;

        private void BuildBarsPage(Panel host)
        {
            var pnl = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(8)
            };

            pnl.Controls.Add(CreateHeaderLabel("BAR STYLE"));

            _segBarStyle = new VbSegmented
            {
                Width = 520,
                Items = new[] { "Smooth Pills", "Frosted LEDs", "Solid Neo-Bars" },
                SelectedIndex = (int)_settings.BarStyle
            };
            _segBarStyle.SelectedIndexChanged += idx =>
            {
                _settings.BarStyle = (BarVisualEffect)idx;
                _settings.NotifyChanged();
                _settings.RequestSave();
                UpdateLedControlsState();
            };
            pnl.Controls.Add(_segBarStyle);

            pnl.Controls.Add(CreateSpacer(8));
            pnl.Controls.Add(CreateHeaderLabel("BAR GEOMETRY"));

            _sldBarCount = new VbSlider
            {
                Width = 520,
                Label = "Bar Count",
                Minimum = 16,
                Maximum = 160,
                DefaultValue = 64,
                Value = _settings.BarCount,
                DecimalPlaces = 0,
                Unit = "bars"
            };
            _sldBarCount.ValueChanged += val =>
            {
                _settings.BarCount = (int)Math.Round(val);
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_sldBarCount);

            _sldBarGap = new VbSlider
            {
                Width = 520,
                Label = "Bar Spacing / Gap",
                Minimum = 0f,
                Maximum = 60f,
                DefaultValue = 22f,
                Value = _settings.BarGap * 100f,
                DecimalPlaces = 0,
                Unit = "%"
            };
            _sldBarGap.ValueChanged += val =>
            {
                _settings.BarGap = val / 100f;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_sldBarGap);

            _sldRoundness = new VbSlider
            {
                Width = 520,
                Label = "Corner Roundness",
                Minimum = 0f,
                Maximum = 100f,
                DefaultValue = 60f,
                Value = _settings.BarRoundness * 100f,
                DecimalPlaces = 0,
                Unit = "%"
            };
            _sldRoundness.ValueChanged += val =>
            {
                _settings.BarRoundness = val / 100f;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_sldRoundness);

            _sldTrebleTaper = new VbSlider
            {
                Width = 520,
                Label = "Treble Taper (narrow bars toward highs)",
                Minimum = 0f,
                Maximum = 80f,
                DefaultValue = 10f,
                Value = _settings.TrebleTaper * 100f,
                DecimalPlaces = 0,
                Unit = "%"
            };
            _sldTrebleTaper.ValueChanged += val =>
            {
                _settings.TrebleTaper = val / 100f;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_sldTrebleTaper);

            pnl.Controls.Add(CreateSpacer(8));
            pnl.Controls.Add(CreateHeaderLabel("LED SEGMENTS (Frosted LEDs Style)"));

            _sldLedSegH = new VbSlider
            {
                Width = 520,
                Label = "LED Segment Height",
                Minimum = 2f,
                Maximum = 24f,
                DefaultValue = 6f,
                Value = _settings.LedSegmentHeight,
                DecimalPlaces = 1,
                Unit = "px"
            };
            _sldLedSegH.ValueChanged += val =>
            {
                _settings.LedSegmentHeight = val;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_sldLedSegH);

            _sldLedSegGap = new VbSlider
            {
                Width = 520,
                Label = "LED Segment Gap",
                Minimum = 1f,
                Maximum = 8f,
                DefaultValue = 2f,
                Value = _settings.LedSegmentGap,
                DecimalPlaces = 1,
                Unit = "px"
            };
            _sldLedSegGap.ValueChanged += val =>
            {
                _settings.LedSegmentGap = val;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_sldLedSegGap);

            UpdateLedControlsState();
            host.Controls.Add(pnl);
        }

        private void UpdateLedControlsState()
        {
            bool isLed = _settings.BarStyle == BarVisualEffect.Segmented;
            if (_sldLedSegH != null) _sldLedSegH.Enabled = isLed;
            if (_sldLedSegGap != null) _sldLedSegGap.Enabled = isLed;
        }

        private void RefreshBarsPageUi()
        {
            if (_segBarStyle != null) _segBarStyle.SelectedIndex = (int)_settings.BarStyle;
            if (_sldBarCount != null) _sldBarCount.Value = _settings.BarCount;
            if (_sldBarGap != null) _sldBarGap.Value = _settings.BarGap * 100f;
            if (_sldRoundness != null) _sldRoundness.Value = _settings.BarRoundness * 100f;
            if (_sldTrebleTaper != null) _sldTrebleTaper.Value = _settings.TrebleTaper * 100f;
            if (_sldLedSegH != null) _sldLedSegH.Value = _settings.LedSegmentHeight;
            if (_sldLedSegGap != null) _sldLedSegGap.Value = _settings.LedSegmentGap;
            UpdateLedControlsState();
        }
    }
}
