using System;
using System.Drawing;
using System.Text;
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
        private Label _lblTelemetry;
        private Timer _telemetryTimer;

        private static readonly int[] FrameCapValues = { 0, 60, 120, 144, 240, 360, -1 };
        private static readonly int[] AudioHzValues = { 60, 120, 240, 500, 1000, 0 };

        private void BuildSystemPage(Panel host)
        {
            var pnl = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(8)
            };

            pnl.Controls.Add(CreateHeaderLabel("DISPLAY FRAME RATE CAP"));
            _segFrameCap = new VbSegmented
            {
                Width = 540,
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

            pnl.Controls.Add(CreateSpacer(8));
            pnl.Controls.Add(CreateHeaderLabel("AUDIO ANALYSIS UPDATE RATE"));
            _segAudioHz = new VbSegmented
            {
                Width = 540,
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

            pnl.Controls.Add(CreateSpacer(8));
            pnl.Controls.Add(CreateHeaderLabel("POWER, WINDOW & HUD"));

            _togIdleThrottle = new VbToggle { Width = 540, Label = "Idle Power Throttle (drops to 20 fps when paused or silent)", Checked = _settings.IdleThrottle };
            _togIdleThrottle.CheckedChanged += chk => { _settings.IdleThrottle = chk; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_togIdleThrottle);

            _togDiagnostics = new VbToggle { Width = 540, Label = "Show On-Screen FPS & Telemetry HUD Overlay", Checked = _settings.ShowDiagnostics };
            _togDiagnostics.CheckedChanged += chk => { _settings.ShowDiagnostics = chk; _settings.NotifyChanged(); _settings.RequestSave(); };
            pnl.Controls.Add(_togDiagnostics);

            var btnPopout = new Button
            {
                Text = "Popout Visualizer into Dedicated Window",
                Width = 260,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                ForeColor = VbTheme.TextPrimary,
                BackColor = VbTheme.BgCard,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 4, 0, 8)
            };
            btnPopout.FlatAppearance.BorderColor = VbTheme.Border;
            btnPopout.FlatAppearance.MouseOverBackColor = VbTheme.BgHover;
            btnPopout.Click += delegate { _control?.TogglePopout(); };
            pnl.Controls.Add(btnPopout);

            pnl.Controls.Add(CreateSpacer(8));
            pnl.Controls.Add(CreateHeaderLabel("LIVE TELEMETRY & DIAGNOSTICS"));

            var pnlCard = new Panel { Width = 540, Height = 95, BackColor = VbTheme.BgCard, Padding = new Padding(10) };
            _lblTelemetry = new Label { Dock = DockStyle.Fill, Font = VbTheme.FontRegular, ForeColor = VbTheme.TextPrimary, Text = "Loading live telemetry..." };
            pnlCard.Controls.Add(_lblTelemetry);
            pnl.Controls.Add(pnlCard);

            var btnCopyDiag = new Button
            {
                Text = "Copy Diagnostics to Clipboard",
                Width = 220,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = VbTheme.BgCard,
                ForeColor = VbTheme.TextPrimary,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 6, 0, 0)
            };
            btnCopyDiag.FlatAppearance.BorderColor = VbTheme.Border;
            btnCopyDiag.FlatAppearance.MouseOverBackColor = VbTheme.BgHover;
            btnCopyDiag.Click += delegate
            {
                try
                {
                    Clipboard.SetText(GenerateDiagnosticsReport());
                    MessageBox.Show("Diagnostics report copied to clipboard!", "VisualBee", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch { }
            };
            pnl.Controls.Add(btnCopyDiag);

            _telemetryTimer = new Timer { Interval = 250 };
            _telemetryTimer.Tick += delegate { UpdateTelemetry(); };
            _telemetryTimer.Start();

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
            return 1;
        }

        private void UpdateTelemetry()
        {
            if (_lblTelemetry == null || _lblTelemetry.IsDisposed) return;
            var r = _control?.Renderer;
            string fpsText = r != null ? $"{r.Fps:0.0} fps ({r.FrameMs:0.1} ms)" : "offline";
            string presentMode = r?.PresentMode ?? "N/A";
            string audioSource = Plugin.Audio != null ? Plugin.Audio.TapDescription : "N/A";
            string timing = Plugin.Audio?.Timing ?? "N/A";

            _lblTelemetry.Text = $"Renderer: Direct3D 11 ({presentMode}) · Res: {r?.Width ?? 0}x{r?.Height ?? 0}\n" +
                                 $"Display: {fpsText} · Style: {_settings.BarStyle}\n" +
                                 $"Audio Tap: {audioSource} ({timing}) · Analysis: {_settings.AnalysisHz} Hz";
        }

        private string GenerateDiagnosticsReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== VisualBee Studio v2.0 Diagnostics Report ===");
            sb.AppendLine($"Timestamp: {DateTime.UtcNow:O}");
            sb.AppendLine($"OS: {Environment.OSVersion}");
            var r = _control?.Renderer;
            sb.AppendLine($"Renderer: {r?.PresentMode ?? "N/A"} ({r?.Fps:0.0} fps)");
            sb.AppendLine($"Resolution: {r?.Width ?? 0}x{r?.Height ?? 0}");
            sb.AppendLine($"Mode: {_settings.Mode} · Style: {_settings.BarStyle}");
            sb.AppendLine($"Palette: {_settings.Palette}");
            return sb.ToString();
        }

        private void RefreshSystemPageUi()
        {
            if (_segFrameCap != null) _segFrameCap.SelectedIndex = MapFrameCapToIndex(_settings.FrameCap);
            if (_segAudioHz != null) _segAudioHz.SelectedIndex = MapAudioHzToIndex(_settings.AnalysisHz);
            if (_togIdleThrottle != null) _togIdleThrottle.Checked = _settings.IdleThrottle;
            if (_togDiagnostics != null) _togDiagnostics.Checked = _settings.ShowDiagnostics;
            UpdateTelemetry();
        }
    }
}
