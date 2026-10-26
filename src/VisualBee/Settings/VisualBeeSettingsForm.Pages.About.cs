using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MusicBeePlugin.SettingsUi;

namespace MusicBeePlugin
{
    public sealed partial class VisualBeeSettingsForm
    {
        private Label _lblTelemetry;
        private Timer _telemetryTimer;

        private void BuildAboutPage(Panel host)
        {
            var pnl = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(8)
            };

            pnl.Controls.Add(CreateHeaderLabel("ABOUT"));
            pnl.Controls.Add(CreateSubtitleLabel("Plugin information and live telemetry"));

            var lblTitle = new Label
            {
                Text = "VisualBee v1.0.0",
                AutoSize = true,
                Font = VbTheme.FontTitle,
                ForeColor = VbTheme.Accent,
                Margin = new Padding(0, 0, 0, 4)
            };
            pnl.Controls.Add(lblTitle);

            var lblDesc = new Label
            {
                Text = "High-performance Direct3D 11 audio visualizer for MusicBee.\nFlip-model presentation, low-latency BASS tap, and Gaussian FFT.",
                AutoSize = true,
                Font = VbTheme.FontRegular,
                ForeColor = VbTheme.TextSecondary,
                Margin = new Padding(0, 0, 0, 16)
            };
            pnl.Controls.Add(lblDesc);

            pnl.Controls.Add(CreateHeaderLabel("LIVE TELEMETRY"));

            var pnlCard = new Panel
            {
                Width = 520,
                Height = 110,
                BackColor = VbTheme.BgCard,
                Padding = new Padding(12)
            };

            _lblTelemetry = new Label
            {
                Dock = DockStyle.Fill,
                Font = VbTheme.FontRegular,
                ForeColor = VbTheme.TextPrimary,
                Text = "Loading live telemetry..."
            };
            pnlCard.Controls.Add(_lblTelemetry);
            pnl.Controls.Add(pnlCard);

            pnl.Controls.Add(CreateSpacer(12));

            var btnCopyDiag = new Button
            {
                Text = "Copy Diagnostics to Clipboard",
                Width = 240,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = VbTheme.BgCard,
                ForeColor = VbTheme.TextPrimary,
                Cursor = Cursors.Hand
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

        private void UpdateTelemetry()
        {
            if (_lblTelemetry == null || _lblTelemetry.IsDisposed) return;
            var r = _control?.Renderer;
            string fpsText = r != null ? $"{r.Fps:0.0} fps ({r.FrameMs:0.1} ms)" : "not running";
            string presentMode = r?.PresentMode ?? "N/A";
            string audioSource = Plugin.Audio != null ? Plugin.Audio.TapDescription : "N/A";
            string timing = Plugin.Audio?.Timing ?? "N/A";

            _lblTelemetry.Text = $"Renderer: Direct3D 11 ({presentMode})\n" +
                                 $"Display: {fpsText} · Resolution: {r?.Width ?? 0}x{r?.Height ?? 0}\n" +
                                 $"Audio Tap: {audioSource} ({timing})\n" +
                                 $"Analysis: {_settings.AnalysisHz} Hz target";
        }

        private string GenerateDiagnosticsReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== VisualBee v1.0.0 Diagnostics Report ===");
            sb.AppendLine($"Timestamp: {DateTime.UtcNow:O}");
            sb.AppendLine($"OS: {Environment.OSVersion}");
            sb.AppendLine($"Host Runtime: .NET Framework {Environment.Version}");
            var r = _control?.Renderer;
            sb.AppendLine($"Renderer Ready: {r?.IsReady ?? false}");
            sb.AppendLine($"Present Mode: {r?.PresentMode ?? "N/A"}");
            sb.AppendLine($"FPS: {r?.Fps:0.0} (Frame Ms: {r?.FrameMs:0.1})");
            sb.AppendLine($"Resolution: {r?.Width ?? 0}x{r?.Height ?? 0}");
            sb.AppendLine($"Tearing Supported: {r?.TearingSupported ?? false}");
            sb.AppendLine($"Audio Source: {Plugin.Audio?.TapDescription ?? "N/A"}");
            sb.AppendLine($"Audio Timing: {Plugin.Audio?.Timing ?? "N/A"}");
            sb.AppendLine($"Settings Mode: {_settings.Mode}");
            sb.AppendLine($"Settings FrameCap: {_settings.FrameCap}");
            sb.AppendLine($"Settings AnalysisHz: {_settings.AnalysisHz}");
            sb.AppendLine($"Settings Palette: {_settings.Palette}");
            return sb.ToString();
        }

        private void RefreshAboutPageUi()
        {
            UpdateTelemetry();
        }
    }
}
