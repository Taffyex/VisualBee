using System;
using System.Drawing;
using System.Windows.Forms;
using MusicBeePlugin.SettingsUi;

namespace MusicBeePlugin
{
    public sealed partial class VisualBeeSettingsForm
    {
        private VbSegmented _segMode;
        private VbToggle _togMirrorH;
        private VbToggle _togMirrorV;

        private void BuildGeneralPage(Panel host)
        {
            var pnl = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(8)
            };

            pnl.Controls.Add(CreateHeaderLabel("GENERAL & WINDOW"));
            pnl.Controls.Add(CreateSubtitleLabel("Applies to all modes"));

            pnl.Controls.Add(CreateHeaderLabel("VISUALIZER MODE"));

            _segMode = new VbSegmented
            {
                Width = 520,
                Items = new[] { "Spectrum Bars", "Waveform" },
                SelectedIndex = (int)_settings.Mode
            };
            _segMode.SelectedIndexChanged += idx =>
            {
                _settings.Mode = (VisualizerMode)idx;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_segMode);

            pnl.Controls.Add(CreateSpacer(12));
            pnl.Controls.Add(CreateHeaderLabel("MIRROR MODES"));

            _togMirrorH = new VbToggle
            {
                Width = 520,
                Label = "Horizontal Mirror (Bass in center, spreading outward)",
                Checked = (_settings.Mirror & BarMirror.Horizontal) != 0
            };
            _togMirrorH.CheckedChanged += chk =>
            {
                if (chk) _settings.Mirror |= BarMirror.Horizontal;
                else _settings.Mirror &= ~BarMirror.Horizontal;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_togMirrorH);

            _togMirrorV = new VbToggle
            {
                Width = 520,
                Label = "Vertical Mirror (Bars/wave grow up and down from center line)",
                Checked = (_settings.Mirror & BarMirror.Vertical) != 0
            };
            _togMirrorV.CheckedChanged += chk =>
            {
                if (chk) _settings.Mirror |= BarMirror.Vertical;
                else _settings.Mirror &= ~BarMirror.Vertical;
                _settings.NotifyChanged();
                _settings.RequestSave();
            };
            pnl.Controls.Add(_togMirrorV);

            pnl.Controls.Add(CreateSpacer(16));
            pnl.Controls.Add(CreateHeaderLabel("WINDOW POPOUT"));

            var btnPopout = new Button
            {
                Text = "Popout Visualizer Window",
                Width = 220,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                ForeColor = VbTheme.TextPrimary,
                BackColor = VbTheme.BgCard,
                Cursor = Cursors.Hand
            };
            btnPopout.FlatAppearance.BorderColor = VbTheme.Border;
            btnPopout.FlatAppearance.MouseOverBackColor = VbTheme.BgHover;
            btnPopout.Click += delegate { _control?.TogglePopout(); };
            pnl.Controls.Add(btnPopout);

            host.Controls.Add(pnl);
        }

        private void RefreshGeneralPageUi()
        {
            if (_segMode != null) _segMode.SelectedIndex = (int)_settings.Mode;
            if (_togMirrorH != null) _togMirrorH.Checked = (_settings.Mirror & BarMirror.Horizontal) != 0;
            if (_togMirrorV != null) _togMirrorV.Checked = (_settings.Mirror & BarMirror.Vertical) != 0;
        }

        private static Label CreateHeaderLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = VbTheme.FontSection,
                ForeColor = VbTheme.Accent,
                Margin = new Padding(0, 4, 0, 6)
            };
        }

        private static Control CreateSpacer(int height)
        {
            return new Panel { Height = height, Width = 10 };
        }
    }
}
