using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MusicBeePlugin.SettingsUi
{
    internal sealed class VbSlider : Control
    {
        public event Action<float> ValueChanged;

        private string _label = "";
        private float _value = 0f;
        private float _min = 0f;
        private float _max = 100f;
        private float _default = 0f;
        private string _unit = "";
        private int _decimalPlaces = 0;
        private bool _dragging = false;

        public string Label { get => _label; set { _label = value; Invalidate(); } }
        public float Minimum { get => _min; set { _min = value; Invalidate(); } }
        public float Maximum { get => _max; set { _max = value; Invalidate(); } }
        public float DefaultValue { get => _default; set => _default = value; }
        public string Unit { get => _unit; set { _unit = value; Invalidate(); } }
        public int DecimalPlaces { get => _decimalPlaces; set { _decimalPlaces = value; Invalidate(); } }

        public float Value
        {
            get => _value;
            set
            {
                float clamped = Math.Max(_min, Math.Min(_max, value));
                if (Math.Abs(clamped - _value) > 0.0001f)
                {
                    _value = clamped;
                    Invalidate();
                    ValueChanged?.Invoke(_value);
                }
            }
        }

        public VbSlider()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            Height = 44;
            Margin = new Padding(0, 1, 0, 1);
            TabStop = true;
        }

        private Rectangle GetTrackRect()
        {
            return new Rectangle(8, 26, Width - 16, 6);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color textCol = Enabled ? VbTheme.TextPrimary : VbTheme.TextMuted;
            Color accentCol = Enabled ? VbTheme.Accent : VbTheme.BorderLight;

            using (var bText = new SolidBrush(textCol))
            using (var bVal = new SolidBrush(accentCol))
            {
                g.DrawString(_label, VbTheme.FontRegular, bText, 6, 4);

                string valStr = _decimalPlaces > 0
                    ? _value.ToString("F" + _decimalPlaces) + (_unit != "" ? " " + _unit : "")
                    : ((int)Math.Round(_value)).ToString() + (_unit != "" ? " " + _unit : "");
                var valSize = g.MeasureString(valStr, VbTheme.FontBold);
                g.DrawString(valStr, VbTheme.FontBold, bVal, Width - valSize.Width - 6, 4);
            }

            var track = GetTrackRect();
            if (track.Width <= 0) return;

            using (var pTrack = new GraphicsPath())
            {
                pTrack.AddArc(track.X, track.Y, track.Height, track.Height, 90, 180);
                pTrack.AddArc(track.Right - track.Height, track.Y, track.Height, track.Height, 270, 180);
                pTrack.CloseFigure();
                using (var bTrack = new SolidBrush(VbTheme.Border))
                    g.FillPath(bTrack, pTrack);
            }

            float norm = _max > _min ? (_value - _min) / (_max - _min) : 0f;
            norm = Math.Max(0f, Math.Min(1f, norm));
            int fillW = (int)(track.Width * norm);
            if (fillW > 4)
            {
                using (var pFill = new GraphicsPath())
                {
                    pFill.AddArc(track.X, track.Y, track.Height, track.Height, 90, 180);
                    pFill.AddArc(track.X + fillW - track.Height, track.Y, track.Height, track.Height, 270, 180);
                    pFill.CloseFigure();
                    using (var bFill = new SolidBrush(accentCol))
                        g.FillPath(bFill, pFill);
                }
            }

            int thumbX = track.X + (int)(track.Width * norm);
            int thumbY = track.Y + track.Height / 2;
            int radius = 7;
            using (var bThumb = new SolidBrush(Enabled ? Color.White : VbTheme.TextMuted))
            using (var pBorder = new Pen(accentCol, 2f))
            {
                g.FillEllipse(bThumb, thumbX - radius, thumbY - radius, radius * 2, radius * 2);
                g.DrawEllipse(pBorder, thumbX - radius, thumbY - radius, radius * 2, radius * 2);
            }
        }

        private void UpdateFromMouse(int mouseX)
        {
            if (!Enabled) return;
            var track = GetTrackRect();
            if (track.Width <= 0) return;
            float norm = (float)(mouseX - track.X) / track.Width;
            norm = Math.Max(0f, Math.Min(1f, norm));
            Value = _min + norm * (_max - _min);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!Enabled) return;
            Focus();
            if (e.Button == MouseButtons.Left)
            {
                _dragging = true;
                Capture = true;
                UpdateFromMouse(e.X);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging && Enabled)
            {
                UpdateFromMouse(e.X);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_dragging)
            {
                _dragging = false;
                Capture = false;
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (Enabled && e.Button == MouseButtons.Left)
            {
                Value = _default;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            // Only adjust slider value if Ctrl is held down or the user is actively dragging.
            // Otherwise, forward the scroll event up to the parent scrollable container
            // so scrolling down settings never changes sliders accidentally!
            if (Enabled && ((ModifierKeys & Keys.Control) != 0 || _dragging))
            {
                base.OnMouseWheel(e);
                float step = (_max - _min) / 40f;
                if (_decimalPlaces == 0) step = Math.Max(1f, (float)Math.Round(step));
                Value += (e.Delta > 0 ? step : -step);
            }
            else
            {
                // Forward wheel to parent container for silky smooth page scrolling
                Parent?.Focus();
                var sc = FindScrollableParent(Parent);
                if (sc != null)
                {
                    try
                    {
                        sc.AutoScrollPosition = new Point(
                            -sc.AutoScrollPosition.X,
                            -sc.AutoScrollPosition.Y - (e.Delta > 0 ? 40 : -40));
                    }
                    catch { }
                }
            }
        }

        private static ScrollableControl FindScrollableParent(Control c)
        {
            while (c != null)
            {
                if (c is ScrollableControl sc && sc.AutoScroll) return sc;
                c = c.Parent;
            }
            return null;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (!Enabled) return base.ProcessCmdKey(ref msg, keyData);

            float step = (_max - _min) / 50f;
            if (_decimalPlaces == 0) step = Math.Max(1f, (float)Math.Round(step));

            if (keyData == Keys.Left || keyData == Keys.Down)
            {
                Value -= step;
                return true;
            }
            if (keyData == Keys.Right || keyData == Keys.Up)
            {
                Value += step;
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
