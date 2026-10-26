using System;
using System.Drawing;
using System.Windows.Forms;
using MusicBeePlugin.SettingsUi;

namespace MusicBeePlugin
{
    public sealed partial class VisualBeeSettingsForm
    {
        private VbSegmented _segFrameCap;
        private VbSegmented _segAudioHz;
        private VbToggle _togIdleThrottle;
        private VbToggle _togDiagnostics;

        private static readonly int[] FrameCapValues = { 0, 60, 120, 144, 240, 360, -1 };
        private static readonly int[] AudioHzValues = { 60, 120, 240, 500, 1000, 0 };

        private void BuildPerformancePage(Panel host)
        {
            var pnl = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(8)
            };

            pnl.Controls.Add(CreateHeaderLabel("PERFORMANCE"));
            pnl.Controls.Add(CreateSubtitleLabel("Applies to all modes"));

            pnl.Controls.Add(CreateHeaderLabel("DISPLAY FRAME RATE CAP"));

            _segFrameCap = new VbSegmented
            {
                Width = 520,
                Items = new[] { "VSync", "60", "120", "144", "240", "360", "Uncapped" },
                SelectedIndex = MapFrameCapToIndex(_settings.FrameCap)
            };
            _segFrameCap.SelectedIndexChanged += idx =>
            {
                if (idx >= 0 && idx < FrameCapValues.Length)
                {
                    _settings.FrameCap = FrameCapValues[idx];
                    _settings.NotifyChanged();
                    _settings.RequestSave();
                }
            };
            pnl.Controls.Add(_segFrameCap);

            pnl.Controls.Add(CreateSpacer(12));
            pnl.Controls.Add(CreateHeaderLabel("AUDIO ANALYSIS UPDATE RATE"));

            _segAudioHz = new VbSegmented
            {
                Width = 520,
                Items = new[] { "60 Hz", "120 Hz", "240 Hz", "500 Hz", "1000 Hz", "Uncapped" },
                SelectedIndex = MapAudioHzToIndex(_settings.AnalysisHz)
            };
            _segAudioHz.SelectedIndexChanged += idx =>
            {
                if (idx >= 0 && idx < AudioHzValues.Length)
                {
                    _settings.AnalysisHz = AudioHzValues[idx];
                    _settings.NotifyChanged();
                    _settings.RequestSave();
                }
            };
            pnl.Controls.Add(_segAudioHz);

            pnl.Controls.Add(CreateSpacer(12));
            pnl.Controls.Add(CreateHeaderLabel("POWER & DIAGNOSTICS"));

            _togIdleThrottle = new VbToggle
            {
                Width = 520,
                Label = "Idle Power Throttle (drops to 20 fps when paused or silent)",
                Checked = _settings.IdleThrottle
            };
            _togIdleThrottle.CheckedChanged += chk =>
            {
                _settings.IdleThrottle = chk;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_togIdleThrottle);

            _togDiagnostics = new VbToggle
            {
                Width = 520,
                Label = "Show On-Screen FPS & Telemetry Overlay",
                Checked = _settings.ShowDiagnostics
            };
            _togDiagnostics.CheckedChanged += chk =>
            {
                _settings.ShowDiagnostics = chk;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_togDiagnostics);

            host.Controls.Add(pnl);
        }

        private static int MapFrameCapToIndex(int cap)
        {
            for (int i = 0; i < FrameCapValues.Length; i++)
                if (FrameCapValues[i] == cap) return i;
            return 0;
        }

        private static int MapAudioHzToIndex(int hz)
        {
            for (int i = 0; i < AudioHzValues.Length; i++)
                if (AudioHzValues[i] == hz) return i;
            return 1; // 120 Hz default
        }

        private void RefreshPerformancePageUi()
        {
            if (_segFrameCap != null) _segFrameCap.SelectedIndex = MapFrameCapToIndex(_settings.FrameCap);
            if (_segAudioHz != null) _segAudioHz.SelectedIndex = MapAudioHzToIndex(_settings.AnalysisHz);
            if (_togIdleThrottle != null) _togIdleThrottle.Checked = _settings.IdleThrottle;
            if (_togDiagnostics != null) _togDiagnostics.Checked = _settings.ShowDiagnostics;
        }
    }
}
