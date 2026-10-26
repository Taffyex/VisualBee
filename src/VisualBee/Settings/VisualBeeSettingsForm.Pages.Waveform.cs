using System;
using System.Drawing;
using System.Windows.Forms;
using MusicBeePlugin.SettingsUi;

namespace MusicBeePlugin
{
    public sealed partial class VisualBeeSettingsForm
    {
        private VbSlider _sldRibbonThickness;
        private VbToggle _togRibbonFill;
        private VbToggle _togRibbonFloor;

        private void BuildWaveformPage(Panel host)
        {
            var pnl = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(8)
            };

            pnl.Controls.Add(CreateHeaderLabel("WAVEFORM"));
            pnl.Controls.Add(CreateSubtitleLabel("Applies to Waveform mode"));

            pnl.Controls.Add(CreateHeaderLabel("LINE APPEARANCE"));

            _sldRibbonThickness = new VbSlider
            {
                Width = 520,
                Label = "Line Thickness",
                Minimum = 0.5f,
                Maximum = 8.0f,
                DefaultValue = 2.5f,
                Value = _settings.RibbonThickness,
                DecimalPlaces = 1,
                Unit = "px"
            };
            _sldRibbonThickness.ValueChanged += val =>
            {
                _settings.RibbonThickness = val;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_sldRibbonThickness);

            _togRibbonFill = new VbToggle
            {
                Width = 520,
                Label = "Audio-Reactive Baseline Fill (Luminous glow to centerline)",
                Checked = _settings.RibbonFill
            };
            _togRibbonFill.CheckedChanged += chk =>
            {
                _settings.RibbonFill = chk;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_togRibbonFill);

            pnl.Controls.Add(CreateSpacer(8));
            pnl.Controls.Add(CreateHeaderLabel("ENVIRONMENT"));

            _togRibbonFloor = new VbToggle
            {
                Width = 520,
                Label = "Floor Mirror Reflection",
                Checked = _settings.RibbonFloorReflection
            };
            _togRibbonFloor.CheckedChanged += chk =>
            {
                _settings.RibbonFloorReflection = chk;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_togRibbonFloor);

            host.Controls.Add(pnl);
        }

        private void RefreshWaveformPageUi()
        {
            if (_sldRibbonThickness != null) _sldRibbonThickness.Value = _settings.RibbonThickness;
            if (_togRibbonFill != null) _togRibbonFill.Checked = _settings.RibbonFill;
            if (_togRibbonFloor != null) _togRibbonFloor.Checked = _settings.RibbonFloorReflection;
        }
    }
}
