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
                Text = "VisualBee v2.0 - Audio Visualizer Studio",
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

            var r = _renderer;
            string tapDesc = _audio != null ? _audio.TapDescription : "direct";
            string rateText = _settings.AnalysisHz <= 0 ? "Unlimited" : _settings.AnalysisHz + " Hz";
            string headerText = r != null
                ? $"{r.Fps:0.0} fps · {r.FrameMs:0.0} ms · audio {rateText} · {tapDesc}"
                : $"VisualBee v2.0 · audio {rateText} · {tapDesc}";

            var headerItem = new ToolStripMenuItem(headerText) { Enabled = false };
            headerItem.ForeColor = Color.FromArgb(140, 155, 175);
            _menu.Items.Add(headerItem);
            _menu.Items.Add(new ToolStripSeparator());

            var settingsItem = new ToolStripMenuItem("⚙ VisualBee Studio Settings...", null, delegate { ShowSettingsWindow(); })
            {
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 192, 166)
            };
            _menu.Items.Add(settingsItem);
            _menu.Items.Add(new ToolStripSeparator());

            // 1. Mode Selector
            var modeMenu = new ToolStripMenuItem("Visualizer Mode");
            AddRadio(modeMenu, "Spectrum Bars", _settings.Mode == VisualizerMode.SpectrumBars, () => _settings.Mode = VisualizerMode.SpectrumBars);
            AddRadio(modeMenu, "Liquid Waveform", _settings.Mode == VisualizerMode.Waveform, () => _settings.Mode = VisualizerMode.Waveform);
            _menu.Items.Add(modeMenu);

            // 2. Bar Visual Style
            var styleMenu = new ToolStripMenuItem("Bar Visual Style");
            AddRadio(styleMenu, "Glass Pillars & VFD Studio (Ref 1 & 2)", _settings.BarStyle == BarVisualEffect.GlassPillar, () => _settings.BarStyle = BarVisualEffect.GlassPillar);
            AddRadio(styleMenu, "Frosted Segmented LEDs", _settings.BarStyle == BarVisualEffect.Segmented, () => _settings.BarStyle = BarVisualEffect.Segmented);
            AddRadio(styleMenu, "Solid Neo-Bars", _settings.BarStyle == BarVisualEffect.Solid, () => _settings.BarStyle = BarVisualEffect.Solid);
            AddRadio(styleMenu, "Smooth Rounded Pills", _settings.BarStyle == BarVisualEffect.Pills, () => _settings.BarStyle = BarVisualEffect.Pills);
            _menu.Items.Add(styleMenu);

            // 3. Layout & Symmetry
            var layoutMenu = new ToolStripMenuItem("Layout & Symmetry");
            AddRadio(layoutMenu, "Grounded (Bottom-Up)", (_settings.Mirror & BarMirror.Vertical) == 0, () => _settings.Mirror &= ~BarMirror.Vertical);
            AddRadio(layoutMenu, "Center-Symmetric (Up & Down Axis)", (_settings.Mirror & BarMirror.Vertical) != 0, () => _settings.Mirror |= BarMirror.Vertical);
            layoutMenu.DropDownItems.Add(new ToolStripSeparator());
            AddCheck(layoutMenu, "Center Laser Horizon Beam", _settings.CenterLineGlow, val => _settings.CenterLineGlow = val);
            AddCheck(layoutMenu, "Horizontal Mirror (Center Bass)", (_settings.Mirror & BarMirror.Horizontal) != 0, val => {
                if (val) _settings.Mirror |= BarMirror.Horizontal; else _settings.Mirror &= ~BarMirror.Horizontal;
            });
            _menu.Items.Add(layoutMenu);

            // 4. Stereo Spectrum Layout
            var stereoMenu = new ToolStripMenuItem("Stereo Mode");
            AddRadio(stereoMenu, "Standard Left-to-Right", _settings.StereoMode == StereoMode.Mono, () => {
                _settings.StereoMode = StereoMode.Mono;
                _settings.Mirror &= ~BarMirror.Horizontal;
            });
            AddRadio(stereoMenu, "Center-Out Stereo Wings", _settings.StereoMode == StereoMode.CenterOut, () => _settings.StereoMode = StereoMode.CenterOut);
            AddRadio(stereoMenu, "Split Left / Right", _settings.StereoMode == StereoMode.SplitLR, () => _settings.StereoMode = StereoMode.SplitLR);
            _menu.Items.Add(stereoMenu);

            // 5. Color Palettes
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

            // 6. Atmosphere & Effects Toggles
            var fxMenu = new ToolStripMenuItem("Atmosphere & Effects");
            AddCheck(fxMenu, "Parallax Bokeh & Floating Embers", _settings.ParticlesEnabled, val => _settings.ParticlesEnabled = val);
            AddCheck(fxMenu, "3D Cyber Horizon Grid", _settings.CyberGridEnabled, val => _settings.CyberGridEnabled = val);
            AddCheck(fxMenu, "Studio Decibel Reference Grid", _settings.DecibelGrid, val => _settings.DecibelGrid = val);
            AddCheck(fxMenu, "Amplitude HDR Incandescence", _settings.HdrIncandescence, val => _settings.HdrIncandescence = val);
            _menu.Items.Add(fxMenu);

            _menu.Items.Add(new ToolStripSeparator());

            // 7. Diagnostics HUD Overlay
            AddCheck(_menu, "Show FPS & Diagnostics HUD", _settings.ShowDiagnostics, val => _settings.ShowDiagnostics = val);

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

        private void AddCheck(ToolStripMenuItem parent, string text, bool isChecked, Action<bool> onToggle)
        {
            var item = new ToolStripMenuItem(text) { Checked = isChecked };
            item.Click += delegate { Apply(() => onToggle(!item.Checked)); };
            parent.DropDownItems.Add(item);
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
