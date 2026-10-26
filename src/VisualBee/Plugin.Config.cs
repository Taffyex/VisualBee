using System;
using System.Drawing;
using System.Windows.Forms;
using MusicBeePlugin.SettingsUi;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        public bool Configure(IntPtr panelHandle)
        {
            if (panelHandle == IntPtr.Zero)
            {
                VisualBeeSettingsForm.ShowOrFocus(Api, Settings, _control);
                return true;
            }

            Control host = Control.FromHandle(panelHandle);
            if (host == null || host.IsDisposed) return false;

            host.Controls.Clear();
            host.SuspendLayout();

            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = VbTheme.BgMain,
                Padding = new Padding(16)
            };

            var btnOpen = new Button
            {
                Text = "Open VisualBee Settings...",
                Width = 240,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                BackColor = VbTheme.Accent,
                ForeColor = Color.Black,
                Font = VbTheme.FontBold,
                Cursor = Cursors.Hand
            };
            btnOpen.Click += delegate
            {
                VisualBeeSettingsForm.ShowOrFocus(Api, Settings, _control);
            };

            pnl.Controls.Add(btnOpen);
            host.Controls.Add(pnl);
            host.ResumeLayout(true);
            return true;
        }

        public void SaveSettings()
        {
            Settings?.Save();
        }
    }
}
