using System;
using System.Drawing;
using System.Windows.Forms;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        private void DisposePanel()
        {
            TeardownControlOnly();
        }

        public int OnDockablePanelCreated(Control panel)
        {
            if (panel == null || panel.IsDisposed) return 0;
            try
            {
                if (!panel.IsHandleCreated && !panel.InvokeRequired)
                {
                    try { panel.CreateControl(); } catch { }
                }
                if (!panel.IsHandleCreated) return 0;

                if (panel.InvokeRequired)
                    panel.Invoke(new Action<Control>(AttachControl), panel);
                else
                    AttachControl(panel);
            }
            catch { }
            return 0;
        }

        private void AttachControl(Control host)
        {
            if (host == null || host.IsDisposed || !host.IsHandleCreated) return;
            if (Settings == null || Audio == null) return;

            if (_control != null && !_control.IsDisposed && ReferenceEquals(_control.Parent, host))
            {
                _control.Start();
                return;
            }

            TeardownControlOnly();

            var control = new VisualBeeControl(Api, Settings, Audio)
            {
                Dock = DockStyle.Fill,
                TabStop = true
            };
            host.Controls.Add(control);
            _control = control;
            control.Start();
        }

        private void TeardownControlOnly()
        {
            VisualBeeControl ctrl = _control;
            _control = null;
            if (ctrl == null) return;

            try
            {
                ctrl.Stop();
                if (!ctrl.IsDisposed)
                {
                    if (ctrl.InvokeRequired && ctrl.IsHandleCreated)
                        ctrl.Invoke(new Action(ctrl.Dispose));
                    else
                        ctrl.Dispose();
                }
            }
            catch { }
        }
    }
}
