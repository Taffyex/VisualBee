using System;
using System.Drawing;
using System.Windows.Forms;
using MusicBeePlugin.SettingsUi;

namespace MusicBeePlugin
{
    public sealed partial class VisualBeeSettingsForm
    {
        private VbSlider _sldSens;
        private VbSlider _sldAttack;
        private VbSlider _sldDecay;
        private VbSlider _sldMinFreq;
        private VbSlider _sldMaxFreq;
        private VbSlider _sldDbRange;
        private VbSlider _sldTilt;
        private VbSlider _sldSync;

        private void BuildAnalyzerPage(Panel host)
        {
            var pnl = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(8)
            };

            pnl.Controls.Add(CreateHeaderLabel("BALLISTICS & SENSITIVITY"));

            _sldSens = new VbSlider
            {
                Width = 520,
                Label = "Audio Sensitivity",
                Minimum = 0.2f,
                Maximum = 3.0f,
                DefaultValue = 1.0f,
                Value = _settings.Sensitivity,
                DecimalPlaces = 2,
                Unit = "x"
            };
            _sldSens.ValueChanged += val =>
            {
                _settings.Sensitivity = val;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_sldSens);

            _sldAttack = new VbSlider
            {
                Width = 520,
                Label = "Attack Time (Rise speed)",
                Minimum = 2f,
                Maximum = 40f,
                DefaultValue = 8f,
                Value = _settings.AttackMs,
                DecimalPlaces = 0,
                Unit = "ms"
            };
            _sldAttack.ValueChanged += val =>
            {
                _settings.AttackMs = (int)Math.Round(val);
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_sldAttack);

            _sldDecay = new VbSlider
            {
                Width = 520,
                Label = "Decay Time (Smoothing fall)",
                Minimum = 10f,
                Maximum = 250f,
                DefaultValue = 65f,
                Value = _settings.DecayMs,
                DecimalPlaces = 0,
                Unit = "ms"
            };
            _sldDecay.ValueChanged += val =>
            {
                _settings.DecayMs = (int)Math.Round(val);
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_sldDecay);

            pnl.Controls.Add(CreateSpacer(8));
            pnl.Controls.Add(CreateHeaderLabel("FREQUENCY RANGE & SCALING"));

            _sldMinFreq = new VbSlider
            {
                Width = 520,
                Label = "Minimum Frequency",
                Minimum = 20f,
                Maximum = 150f,
                DefaultValue = 50f,
                Value = _settings.MinFreqHz,
                DecimalPlaces = 0,
                Unit = "Hz"
            };
            _sldMinFreq.ValueChanged += val =>
            {
                _settings.MinFreqHz = val;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_sldMinFreq);

            _sldMaxFreq = new VbSlider
            {
                Width = 520,
                Label = "Maximum Frequency",
                Minimum = 8000f,
                Maximum = 24000f,
                DefaultValue = 20000f,
                Value = _settings.MaxFreqHz,
                DecimalPlaces = 0,
                Unit = "Hz"
            };
            _sldMaxFreq.ValueChanged += val =>
            {
                _settings.MaxFreqHz = val;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_sldMaxFreq);

            _sldDbRange = new VbSlider
            {
                Width = 520,
                Label = "Dynamic Range Window",
                Minimum = 30f,
                Maximum = 90f,
                DefaultValue = 60f,
                Value = _settings.DbRange,
                DecimalPlaces = 0,
                Unit = "dB"
            };
            _sldDbRange.ValueChanged += val =>
            {
                _settings.DbRange = val;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_sldDbRange);

            _sldTilt = new VbSlider
            {
                Width = 520,
                Label = "Treble Tilt (Equalize high-frequency energy)",
                Minimum = 0f,
                Maximum = 24f,
                DefaultValue = 13f,
                Value = _settings.TrebleTiltDb,
                DecimalPlaces = 0,
                Unit = "dB"
            };
            _sldTilt.ValueChanged += val =>
            {
                _settings.TrebleTiltDb = val;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_sldTilt);

            pnl.Controls.Add(CreateSpacer(8));
            pnl.Controls.Add(CreateHeaderLabel("SYNCHRONIZATION"));

            _sldSync = new VbSlider
            {
                Width = 520,
                Label = "Audio / Visual Sync Offset",
                Minimum = -250f,
                Maximum = 250f,
                DefaultValue = 0f,
                Value = _settings.SyncOffsetMs,
                DecimalPlaces = 0,
                Unit = "ms"
            };
            _sldSync.ValueChanged += val =>
            {
                _settings.SyncOffsetMs = (int)Math.Round(val);
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_sldSync);

            host.Controls.Add(pnl);
        }

        private void RefreshAnalyzerPageUi()
        {
            if (_sldSens != null) _sldSens.Value = _settings.Sensitivity;
            if (_sldAttack != null) _sldAttack.Value = _settings.AttackMs;
            if (_sldDecay != null) _sldDecay.Value = _settings.DecayMs;
            if (_sldMinFreq != null) _sldMinFreq.Value = _settings.MinFreqHz;
            if (_sldMaxFreq != null) _sldMaxFreq.Value = _settings.MaxFreqHz;
            if (_sldDbRange != null) _sldDbRange.Value = _settings.DbRange;
            if (_sldTilt != null) _sldTilt.Value = _settings.TrebleTiltDb;
            if (_sldSync != null) _sldSync.Value = _settings.SyncOffsetMs;
        }
    }
}
