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

        private Panel _pnlNav;
        private Panel _pnlContent;
        private Panel _pnlFooter;
        private readonly Button[] _navButtons = new Button[8];
        private readonly Panel[] _pagePanels = new Panel[8];
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

            // Modeless ownership by MusicBee main window
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

            Text = "VisualBee Settings";
            BackColor = VbTheme.BgMain;
            ForeColor = VbTheme.TextPrimary;
            Size = new Size(780, 600);
            MinimumSize = new Size(680, 480);
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
            // Bottom Footer
            _pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                BackColor = VbTheme.BgNav,
                Padding = new Padding(12, 8, 12, 8)
            };

            var btnResetPage = CreateFooterButton("Reset this page", () => ResetCurrentPage());
            btnResetPage.Dock = DockStyle.Left;
            btnResetPage.Width = 120;

            var btnResetAll = CreateFooterButton("Reset all", () => ResetAllSettings());
            btnResetAll.Dock = DockStyle.Left;
            btnResetAll.Width = 90;

            var btnClose = CreateFooterButton("Close", () => Close());
            btnClose.Dock = DockStyle.Right;
            btnClose.Width = 80;
            btnClose.BackColor = VbTheme.Accent;
            btnClose.ForeColor = Color.Black;
            btnClose.Font = VbTheme.FontBold;

            _pnlFooter.Controls.Add(btnClose);
            _pnlFooter.Controls.Add(btnResetAll);
            _pnlFooter.Controls.Add(btnResetPage);

            // Left Navigation
            _pnlNav = new Panel
            {
                Dock = DockStyle.Left,
                Width = 150,
                BackColor = VbTheme.BgNav,
                Padding = new Padding(8, 12, 8, 12)
            };

            string[] navTitles = { "General", "Bars", "Colours", "Peaks", "Analyzer", "Effects", "Performance", "About" };
            for (int i = navTitles.Length - 1; i >= 0; i--)
            {
                int pageIdx = i;
                var btn = new Button
                {
                    Text = "  " + navTitles[i],
                    Dock = DockStyle.Top,
                    Height = 36,
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = VbTheme.FontRegular,
                    ForeColor = VbTheme.TextSecondary,
                    BackColor = Color.Transparent,
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = VbTheme.BgHover;
                btn.Click += delegate { SelectPage(pageIdx); };
                _navButtons[i] = btn;
                _pnlNav.Controls.Add(btn);
            }

            // Right Content Container
            _pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = VbTheme.BgMain,
                Padding = new Padding(16, 12, 16, 12)
            };

            Controls.Add(_pnlContent);
            Controls.Add(_pnlNav);
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
            string[] sections = { "general", "bars", "colours", "peaks", "analyzer", "effects", "performance", "about" };
            if (_currentPage >= 0 && _currentPage < sections.Length && sections[_currentPage] != "about")
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
            for (int i = 0; i < 8; i++)
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

            BuildGeneralPage(_pagePanels[0]);
            BuildBarsPage(_pagePanels[1]);
            BuildColoursPage(_pagePanels[2]);
            BuildPeaksPage(_pagePanels[3]);
            BuildAnalyzerPage(_pagePanels[4]);
            BuildEffectsPage(_pagePanels[5]);
            BuildPerformancePage(_pagePanels[6]);
            BuildAboutPage(_pagePanels[7]);
        }

        private void RefreshCurrentPageUi()
        {
            switch (_currentPage)
            {
                case 0: RefreshGeneralPageUi(); break;
                case 1: RefreshBarsPageUi(); break;
                case 2: RefreshColoursPageUi(); break;
                case 3: RefreshPeaksPageUi(); break;
                case 4: RefreshAnalyzerPageUi(); break;
                case 5: RefreshEffectsPageUi(); break;
                case 6: RefreshPerformancePageUi(); break;
                case 7: RefreshAboutPageUi(); break;
            }
        }

        private void RefreshAllPagesUi()
        {
            RefreshGeneralPageUi();
            RefreshBarsPageUi();
            RefreshColoursPageUi();
            RefreshPeaksPageUi();
            RefreshAnalyzerPageUi();
            RefreshEffectsPageUi();
            RefreshPerformancePageUi();
            RefreshAboutPageUi();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _settings.RequestSave();
            if (ReferenceEquals(_instance, this)) _instance = null;
        }
    }
}
