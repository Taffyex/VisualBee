using System;
using System.Drawing;
using System.Windows.Forms;
using MusicBeePlugin.SettingsUi;

namespace MusicBeePlugin
{
    public sealed partial class VisualBeeSettingsForm : Form
    {
        private static VisualBeeSettingsForm _instance;

        private readonly MusicBeeApiInterface _api;
        private readonly VisualBeeSettings _settings;
        private readonly VisualBeeControl _control;

        private Panel _pnlHeader;
        private Panel _pnlNav;
        private Panel _pnlContent;
        private Panel _pnlFooter;
        private readonly Button[] _navButtons = new Button[4];
        private readonly Panel[] _pagePanels = new Panel[4];
        private int _currentPage = 0;

        public static void ShowOrFocus(MusicBeeApiInterface api, VisualBeeSettings settings, VisualBeeControl control)
        {
            if (_instance != null && !_instance.IsDisposed)
            {
                if (_instance.WindowState == FormWindowState.Minimized)
                    _instance.WindowState = FormWindowState.Normal;
                _instance.BringToFront();
                _instance.Focus();
                return;
            }

            _instance = new VisualBeeSettingsForm(api, settings, control);

            try
            {
                if (api.MB_GetWindowHandle != null)
                {
                    IntPtr mbHwnd = api.MB_GetWindowHandle();
                    if (mbHwnd != IntPtr.Zero)
                    {
                        var parentForm = Form.FromHandle(mbHwnd) as Form;
                        if (parentForm != null) _instance.Owner = parentForm;
                    }
                }
            }
            catch { }

            _instance.Show();
        }

        public static void CloseIfOpen()
        {
            try
            {
                if (_instance != null && !_instance.IsDisposed)
                {
                    _instance.Close();
                    _instance = null;
                }
            }
            catch { }
        }

        internal VisualBeeSettingsForm(MusicBeeApiInterface api, VisualBeeSettings settings, VisualBeeControl control)
        {
            _api = api;
            _settings = settings;
            _control = control;

            Text = "VisualBee Studio v2.0 Settings";
            BackColor = VbTheme.BgMain;
            ForeColor = VbTheme.TextPrimary;
            Size = new Size(820, 640);
            MinimumSize = new Size(720, 500);
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            Font = VbTheme.FontRegular;

            BuildShell();
            BuildPages();
            SelectPage(0);
        }

        private void BuildShell()
        {
            // Top Quick Preset Vibe Bar
            _pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = VbTheme.BgNav,
                Padding = new Padding(12, 6, 12, 6)
            };

            var lblVibe = new Label
            {
                Text = "⚡ 1-CLICK VIBES:",
                AutoSize = true,
                Font = VbTheme.FontBold,
                ForeColor = VbTheme.Accent,
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 4, 10, 0)
            };
            _pnlHeader.Controls.Add(lblVibe);

            void AddVibeButton(string label, Action action)
            {
                var btn = new Button
                {
                    Text = label,
                    Height = 28,
                    AutoSize = true,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = VbTheme.BgCard,
                    ForeColor = VbTheme.TextPrimary,
                    Cursor = Cursors.Hand,
                    Dock = DockStyle.Left,
                    Margin = new Padding(4, 0, 4, 0)
                };
                btn.FlatAppearance.BorderColor = VbTheme.Border;
                btn.FlatAppearance.MouseOverBackColor = VbTheme.BgHover;
                btn.Click += delegate { action(); };
                _pnlHeader.Controls.Add(btn);
            }

            AddVibeButton("  Glass Pillars (Ref 1)  ", () => ApplyVfdRef1Preset());
            AddVibeButton("  Center-Axis LED (Ref 2)  ", () => ApplyCenterLedRef2Preset());
            AddVibeButton("  Cyber Horizon  ", () => ApplyCyberHorizonPreset());
            AddVibeButton("  Liquid Waveform  ", () => ApplyWaveformPreset());

            // Bottom Footer
            _pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                BackColor = VbTheme.BgNav,
                Padding = new Padding(12, 7, 12, 7)
            };

            var btnResetPage = CreateFooterButton("Reset current tab", () => ResetCurrentPage());
            btnResetPage.Dock = DockStyle.Left;
            btnResetPage.Width = 130;

            var btnResetAll = CreateFooterButton("Reset all settings", () => ResetAllSettings());
            btnResetAll.Dock = DockStyle.Left;
            btnResetAll.Width = 120;

            var btnClose = CreateFooterButton("Close", () => Close());
            btnClose.Dock = DockStyle.Right;
            btnClose.Width = 84;
            btnClose.BackColor = VbTheme.Accent;
            btnClose.ForeColor = Color.Black;
            btnClose.Font = VbTheme.FontBold;

            _pnlFooter.Controls.Add(btnClose);
            _pnlFooter.Controls.Add(btnResetAll);
            _pnlFooter.Controls.Add(btnResetPage);

            // Left Navigation Tabs (4 Smart Tabs)
            _pnlNav = new Panel
            {
                Dock = DockStyle.Left,
                Width = 180,
                BackColor = VbTheme.BgNav,
                Padding = new Padding(6, 12, 6, 10)
            };

            var flowNav = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false,
                BackColor = Color.Transparent
            };

            void AddNavButton(int pageIdx, string text, string subtext)
            {
                var btn = new Button
                {
                    Text = "  " + text + "\n  " + subtext,
                    Width = 168,
                    Height = 46,
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = VbTheme.FontRegular,
                    ForeColor = VbTheme.TextSecondary,
                    BackColor = Color.Transparent,
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0, 2, 0, 4)
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = VbTheme.BgHover;
                btn.Click += delegate { SelectPage(pageIdx); };
                _navButtons[pageIdx] = btn;
                flowNav.Controls.Add(btn);
            }

            AddNavButton(0, "Visualizer Studio", "Mode, Styles & Symmetry");
            AddNavButton(1, "Theme & Atmosphere", "Colors, Particles & FX");
            AddNavButton(2, "Audio & Dynamics", "Ballistics, EQ & Range");
            AddNavButton(3, "System & Telemetry", "Framerate, HUD & Info");

            _pnlNav.Controls.Add(flowNav);

            // Content Panel
            _pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = VbTheme.BgMain,
                Padding = new Padding(16, 12, 16, 12)
            };

            Controls.Add(_pnlContent);
            Controls.Add(_pnlNav);
            Controls.Add(_pnlHeader);
            Controls.Add(_pnlFooter);
        }

        private Button CreateFooterButton(string text, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                ForeColor = VbTheme.TextPrimary,
                BackColor = VbTheme.BgCard,
                Cursor = Cursors.Hand,
                Margin = new Padding(4)
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = VbTheme.Border;
            btn.FlatAppearance.MouseOverBackColor = VbTheme.BgHover;
            btn.Click += delegate { onClick?.Invoke(); };
            return btn;
        }

        private void SelectPage(int index)
        {
            if (index < 0 || index >= _pagePanels.Length) return;
            _currentPage = index;

            for (int i = 0; i < _navButtons.Length; i++)
            {
                bool isSel = (i == index);
                _navButtons[i].BackColor = isSel ? VbTheme.BgActive : Color.Transparent;
                _navButtons[i].ForeColor = isSel ? VbTheme.Accent : VbTheme.TextSecondary;
                _navButtons[i].Font = isSel ? VbTheme.FontBold : VbTheme.FontRegular;

                if (_pagePanels[i] != null)
                    _pagePanels[i].Visible = isSel;
            }
        }

        internal void SelectPageForCapture(int index) => SelectPage(index);

        private void ResetCurrentPage()
        {
            string[] sections = { "studio", "theme", "audio", "system" };
            if (_currentPage >= 0 && _currentPage < sections.Length)
            {
                _settings.ResetToDefaults(sections[_currentPage]);
                RefreshCurrentPageUi();
            }
        }

        private void ResetAllSettings()
        {
            _settings.ResetToDefaults();
            RefreshAllPagesUi();
        }

        private void BuildPages()
        {
            for (int i = 0; i < 4; i++)
            {
                var pnl = new Panel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    Visible = false,
                    BackColor = VbTheme.BgMain
                };
                _pagePanels[i] = pnl;
                _pnlContent.Controls.Add(pnl);
            }

            BuildStudioPage(_pagePanels[0]);
            BuildThemePage(_pagePanels[1]);
            BuildAudioPage(_pagePanels[2]);
            BuildSystemPage(_pagePanels[3]);
        }

        private void RefreshCurrentPageUi()
        {
            switch (_currentPage)
            {
                case 0: RefreshStudioPageUi(); break;
                case 1: RefreshThemePageUi(); break;
                case 2: RefreshAudioPageUi(); break;
                case 3: RefreshSystemPageUi(); break;
            }
        }

        private void RefreshAllPagesUi()
        {
            RefreshStudioPageUi();
            RefreshThemePageUi();
            RefreshAudioPageUi();
            RefreshSystemPageUi();
        }

        private void ApplyVfdRef1Preset()
        {
            _settings.Mode = VisualizerMode.SpectrumBars;
            _settings.BarStyle = BarVisualEffect.GlassPillar;
            _settings.GlassFill = GlassFillMode.Mixed;
            _settings.GlassTubeOverlay = true;
            _settings.Mirror &= ~BarMirror.Vertical;
            _settings.Palette = "vfd_glass";
            _settings.BarCount = 32;
            _settings.BarGap = 0.26f;
            _settings.LedSegmentHeight = 5.0f;
            _settings.LedSegmentGap = 2.0f;
            _settings.FloorReflection = 0.35f;
            _settings.DecibelGrid = true;
            _settings.HdrIncandescence = true;
            _settings.BloomIntensity = 1.05f;
            _settings.ParticlesEnabled = true;
            _settings.ParticleIntensity = 0.45f;
            _settings.NotifyChanged();
            _settings.RequestSave();
            _control?.RefreshPalette();
            RefreshAllPagesUi();
        }

        private void ApplyCenterLedRef2Preset()
        {
            _settings.Mode = VisualizerMode.SpectrumBars;
            _settings.BarStyle = BarVisualEffect.GlassPillar;
            _settings.GlassFill = GlassFillMode.SegmentedRungs;
            _settings.GlassTubeOverlay = true;
            _settings.Mirror |= BarMirror.Vertical;
            _settings.CenterLineGlow = true;
            _settings.SymmetricCenterColor = true;
            _settings.Palette = "vfd_glass";
            _settings.BarCount = 30;
            _settings.BarGap = 0.25f;
            _settings.LedSegmentHeight = 4.0f;
            _settings.LedSegmentGap = 2.0f;
            _settings.FloorReflection = 0.0f;
            _settings.HdrIncandescence = true;
            _settings.DecibelGrid = true;
            _settings.NotifyChanged();
            _settings.RequestSave();
            _control?.RefreshPalette();
            RefreshAllPagesUi();
        }

        private void ApplyCyberHorizonPreset()
        {
            _settings.Mode = VisualizerMode.SpectrumBars;
            _settings.BarStyle = BarVisualEffect.Solid;
            _settings.Palette = "cyberpunk";
            _settings.CyberGridEnabled = true;
            _settings.CyberGridIntensity = 0.50f;
            _settings.ParticlesEnabled = true;
            _settings.ParticleIntensity = 0.60f;
            _settings.FloorReflection = 0.40f;
            _settings.NotifyChanged();
            _settings.RequestSave();
            _control?.RefreshPalette();
            RefreshAllPagesUi();
        }

        private void ApplyWaveformPreset()
        {
            _settings.Mode = VisualizerMode.Waveform;
            _settings.Palette = "aurora";
            _settings.RibbonThickness = 3.0f;
            _settings.RibbonFill = true;
            _settings.RibbonFloorReflection = true;
            _settings.ParticlesEnabled = true;
            _settings.NotifyChanged();
            _settings.RequestSave();
            _control?.RefreshPalette();
            RefreshAllPagesUi();
        }

        internal static Label CreateHeaderLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = VbTheme.FontSection,
                ForeColor = VbTheme.Accent,
                Margin = new Padding(0, 6, 0, 4)
            };
        }

        internal static Label CreateSubtitleLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = VbTheme.FontSmall,
                ForeColor = VbTheme.TextSecondary,
                Margin = new Padding(0, 0, 0, 8)
            };
        }

        internal static Control CreateSpacer(int height)
        {
            return new Panel { Height = height, Width = 10 };
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _settings.RequestSave();
            if (ReferenceEquals(_instance, this)) _instance = null;
        }
    }
}
