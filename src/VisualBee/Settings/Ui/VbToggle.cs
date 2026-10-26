using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MusicBeePlugin.SettingsUi
{
    internal sealed class VbToggle : Control
    {
        public event Action<bool> CheckedChanged;

        private string _label = "";
        private bool _checked = false;

        public string Label
        {
            get => _label;
            set { _label = value; Invalidate(); }
        }

        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked != value)
                {
                    _checked = value;
                    Invalidate();
                    CheckedChanged?.Invoke(_checked);
                }
            }
        }

        public VbToggle()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            Height = 28;
            Margin = new Padding(0, 1, 0, 1);
            TabStop = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw Label
            using (var bText = new SolidBrush(VbTheme.TextPrimary))
            {
                g.DrawString(_label, VbTheme.FontRegular, bText, 6, (Height - 16) / 2);
            }

            // Draw Switch Pill
            int swW = 38;
            int swH = 20;
            int swX = Width - swW - 8;
            int swY = (Height - swH) / 2;

            var swRect = new Rectangle(swX, swY, swW, swH);
            using (var pTrack = new GraphicsPath())
            {
                pTrack.AddArc(swRect.X, swRect.Y, swH, swH, 90, 180);
                pTrack.AddArc(swRect.Right - swH, swRect.Y, swH, swH, 270, 180);
                pTrack.CloseFigure();

                Color trackColor = _checked ? VbTheme.Accent : VbTheme.Border;
                using (var bTrack = new SolidBrush(trackColor))
                    g.FillPath(bTrack, pTrack);
            }

            // Draw Knob
            int knobRadius = 7;
            int knobX = _checked ? (swRect.Right - swH / 2) : (swRect.X + swH / 2);
            int knobY = swRect.Y + swH / 2;

            using (var bKnob = new SolidBrush(Color.White))
            {
                g.FillEllipse(bKnob, knobX - knobRadius, knobY - knobRadius, knobRadius * 2, knobRadius * 2);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (e.Button == MouseButtons.Left)
            {
                Checked = !_checked;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Space || keyData == Keys.Enter)
            {
                Checked = !_checked;
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
