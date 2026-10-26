using System;
using System.Drawing;
using System.Windows.Forms;

namespace MusicBeePlugin
{
    public sealed partial class VisualBeeControl
    {
        public void TogglePopout()
        {
            if (_popoutForm != null && !_popoutForm.IsDisposed)
            {
                _popoutForm.Close();
                _popoutForm = null;
                return;
            }

            var form = new Form
            {
                Text = "VisualBee v1.0 - Audio Visualizer",
                StartPosition = FormStartPosition.CenterScreen,
                Size = new Size(960, 560),
                BackColor = _palette.Background
            };

            var popoutCtrl = new VisualBeeControl(_api, _settings, _audio) { Dock = DockStyle.Fill };
            form.Controls.Add(popoutCtrl);
            form.FormClosed += delegate { popoutCtrl.Stop(); popoutCtrl.Dispose(); };
            _popoutForm = form;
            form.Show();
            popoutCtrl.Start();
        }

        private void SetupMenu()
        {
            _menu.ShowImageMargin = true;
            _menu.ShowCheckMargin = false;
            _menu.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable(_palette));
            _menu.Opening += (s, e) => RebuildMenu();
        }

        private void RebuildMenu()
        {
            _menu.Items.Clear();
            _menu.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable(_palette));

            // Telemetry Header
            var r = _renderer;
            string tapDesc = _audio != null ? _audio.TapDescription : "direct";
            string rateText = _settings.AnalysisHz <= 0 ? "Unlimited" : _settings.AnalysisHz + " Hz";
            string headerText = r != null
                ? $"{r.Fps:0.0} fps · {r.FrameMs:0.0} ms · audio {rateText} · {tapDesc}"
                : $"VisualBee v1.0 · audio {rateText} · {tapDesc}";

            var headerItem = new ToolStripMenuItem(headerText) { Enabled = false };
            headerItem.ForeColor = Color.FromArgb(140, 155, 175);
            _menu.Items.Add(headerItem);
            _menu.Items.Add(new ToolStripSeparator());

            // 1. Settings Window
            var settingsItem = new ToolStripMenuItem("⚙ VisualBee Settings...", null, delegate { ShowSettingsWindow(); })
            {
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 192, 166)
            };
            _menu.Items.Add(settingsItem);
            _menu.Items.Add(new ToolStripSeparator());

            // Mode Selector
            var modeMenu = new ToolStripMenuItem("Visualizer Mode");
            AddRadio(modeMenu, "Spectrum Bars", _settings.Mode == VisualizerMode.SpectrumBars, () => _settings.Mode = VisualizerMode.SpectrumBars);
            AddRadio(modeMenu, "Liquid Waveform", _settings.Mode == VisualizerMode.Waveform, () => _settings.Mode = VisualizerMode.Waveform);
            _menu.Items.Add(modeMenu);

            // Stereo Spectrum Layout
            var stereoMenu = new ToolStripMenuItem("Stereo Mode");
            AddRadio(stereoMenu, "Standard Left-to-Right", _settings.StereoMode == StereoMode.Mono, () => {
                _settings.StereoMode = StereoMode.Mono;
                _settings.Mirror &= ~BarMirror.Horizontal;
            });
            AddRadio(stereoMenu, "Center-Out Stereo Wings", _settings.StereoMode == StereoMode.CenterOut, () => _settings.StereoMode = StereoMode.CenterOut);
            AddRadio(stereoMenu, "Split L/R", _settings.StereoMode == StereoMode.SplitLR, () => _settings.StereoMode = StereoMode.SplitLR);
            _menu.Items.Add(stereoMenu);

            // 2. Bar Visual Style
            var styleMenu = new ToolStripMenuItem("Bar Style");
            AddRadio(styleMenu, "Smooth Rounded Pills", _settings.BarStyle == BarVisualEffect.Pills, () => _settings.BarStyle = BarVisualEffect.Pills);
            AddRadio(styleMenu, "Frosted Segmented LEDs", _settings.BarStyle == BarVisualEffect.Segmented, () => _settings.BarStyle = BarVisualEffect.Segmented);
            AddRadio(styleMenu, "Solid Neo-Bars", _settings.BarStyle == BarVisualEffect.Solid, () => _settings.BarStyle = BarVisualEffect.Solid);
            _menu.Items.Add(styleMenu);

            // 3. Mirror Modes
            var mirrorMenu = new ToolStripMenuItem("Bar Mirror");
            AddRadio(mirrorMenu, "None (Standard Left-to-Right)", _settings.Mirror == BarMirror.None && _settings.StereoMode == StereoMode.Mono, () => {
                _settings.Mirror = BarMirror.None;
                _settings.StereoMode = StereoMode.Mono;
            });
            AddRadio(mirrorMenu, "Horizontal (Center → Edges)", (_settings.Mirror & BarMirror.Horizontal) != 0 || _settings.StereoMode == StereoMode.CenterOut, () => _settings.Mirror |= BarMirror.Horizontal);
            AddRadio(mirrorMenu, "Vertical (Center Ground Line)", (_settings.Mirror & BarMirror.Vertical) != 0, () => _settings.Mirror |= BarMirror.Vertical);
            AddRadio(mirrorMenu, "Quad (Horizontal & Vertical)", (_settings.Mirror & (BarMirror.Horizontal | BarMirror.Vertical)) == (BarMirror.Horizontal | BarMirror.Vertical), () => _settings.Mirror = BarMirror.Horizontal | BarMirror.Vertical);
            _menu.Items.Add(mirrorMenu);

            // 4. Color Palettes & User Presets
            var palMenu = new ToolStripMenuItem("Theme Presets");
            var allPresets = PresetManager.GetAllPresets(_api);
            foreach (var p in allPresets)
            {
                string key = p.Key;
                AddRadio(palMenu, p.DisplayName, string.Equals(_settings.Palette, key, StringComparison.OrdinalIgnoreCase), () => {
                    _settings.Palette = key;
                    RefreshPalette();
                });
            }
            _menu.Items.Add(palMenu);

            // 5. Audio Analysis Update Rate (including Unlimited)
            var rateMenu = new ToolStripMenuItem("Audio Update Rate");
            AddRadio(rateMenu, "Unlimited (Uncapped / Sub-ms)", _settings.AnalysisHz <= 0, () => _settings.AnalysisHz = 0);
            AddRadio(rateMenu, "1000 Hz (Ultra Fast)", _settings.AnalysisHz == 1000, () => _settings.AnalysisHz = 1000);
            AddRadio(rateMenu, "500 Hz (High Speed)", _settings.AnalysisHz == 500, () => _settings.AnalysisHz = 500);
            AddRadio(rateMenu, "240 Hz (Ultra Fidelity)", _settings.AnalysisHz == 240, () => _settings.AnalysisHz = 240);
            AddRadio(rateMenu, "120 Hz (High Smoothness)", _settings.AnalysisHz == 120, () => _settings.AnalysisHz = 120);
            AddRadio(rateMenu, "60 Hz (Standard)", _settings.AnalysisHz == 60, () => _settings.AnalysisHz = 60);
            _menu.Items.Add(rateMenu);

            // 6. Display Frame Rate Cap
            var fpsMenu = new ToolStripMenuItem("Frame Rate Cap");
            AddRadio(fpsMenu, "Monitor VSync (Native)", _settings.FrameCap == 0, () => _settings.FrameCap = 0);
            AddRadio(fpsMenu, "60 fps (Power-Saver)", _settings.FrameCap == 60, () => _settings.FrameCap = 60);
            AddRadio(fpsMenu, "120 fps", _settings.FrameCap == 120, () => _settings.FrameCap = 120);
            AddRadio(fpsMenu, "144 fps", _settings.FrameCap == 144, () => _settings.FrameCap = 144);
            AddRadio(fpsMenu, "240 fps", _settings.FrameCap == 240, () => _settings.FrameCap = 240);
            AddRadio(fpsMenu, "360 fps", _settings.FrameCap == 360, () => _settings.FrameCap = 360);
            AddRadio(fpsMenu, "Uncapped", _settings.FrameCap == -1, () => _settings.FrameCap = -1);
            _menu.Items.Add(fpsMenu);

            _menu.Items.Add(new ToolStripSeparator());

            // 7. Diagnostics
            AddCheck(_menu, "Show FPS & Telemetry", _settings.ShowDiagnostics, val => _settings.ShowDiagnostics = val);

            // 8. Window Popout
            var popoutItem = new ToolStripMenuItem("Popout Visualizer Window", null, delegate { TogglePopout(); })
            {
                ForeColor = Color.FromArgb(200, 210, 225)
            };
            _menu.Items.Add(popoutItem);

            StyleMenuRecursively(_menu.Items);
        }

        private void AddRadio(ToolStripMenuItem parent, string text, bool isChecked, Action onSelect)
        {
            var item = new ToolStripMenuItem(text) { Checked = isChecked };
            item.Click += delegate { Apply(onSelect); };
            parent.DropDownItems.Add(item);
        }

        private void AddCheck(ContextMenuStrip parent, string text, bool isChecked, Action<bool> onToggle)
        {
            var item = new ToolStripMenuItem(text) { Checked = isChecked };
            item.Click += delegate { Apply(() => onToggle(!item.Checked)); };
            parent.Items.Add(item);
        }

        private void Apply(Action act)
        {
            try
            {
                act?.Invoke();
                _settings.NotifyChanged();
                _settings.RequestSave();
            }
            catch { }
        }

        private static void StyleMenuRecursively(ToolStripItemCollection items)
        {
            foreach (ToolStripItem it in items)
            {
                if (it is ToolStripMenuItem mi)
                {
                    if (mi.Enabled && mi.ForeColor != Color.FromArgb(20, 192, 166))
                        mi.ForeColor = Color.FromArgb(230, 235, 245);
                    if (mi.DropDownItems.Count > 0) StyleMenuRecursively(mi.DropDownItems);
                }
            }
        }

        private sealed class DarkColorTable : ProfessionalColorTable
        {
            private readonly Color _bg;
            private readonly Color _accent;

            public DarkColorTable(PaletteColors p)
            {
                _bg = Color.FromArgb(22, 25, 32);
                _accent = p.ColorB.A != 0 ? p.ColorB : Color.FromArgb(20, 192, 166);
            }

            public override Color ToolStripDropDownBackground => _bg;
            public override Color MenuBorder => Color.FromArgb(45, 50, 62);
            public override Color MenuItemSelected => Color.FromArgb(38, 44, 58);
            public override Color MenuItemSelectedGradientBegin => Color.FromArgb(38, 44, 58);
            public override Color MenuItemSelectedGradientEnd => Color.FromArgb(38, 44, 58);
            public override Color MenuItemBorder => _accent;
            public override Color CheckBackground => Color.FromArgb(34, 40, 52);
            public override Color CheckSelectedBackground => Color.FromArgb(46, 54, 70);
            public override Color SeparatorDark => Color.FromArgb(45, 50, 62);
            public override Color ImageMarginGradientBegin => _bg;
            public override Color ImageMarginGradientMiddle => _bg;
            public override Color ImageMarginGradientEnd => _bg;
        }
    }
}