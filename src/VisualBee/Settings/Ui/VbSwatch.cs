using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MusicBeePlugin.SettingsUi
{
    internal sealed class VbSwatch : Control
    {
        public event Action<Color> ColorChanged;

        private string _label = "";
        private Color _color = Color.White;

        public string Label
        {
            get => _label;
            set { _label = value; Invalidate(); }
        }

        public Color Color
        {
            get => _color;
            set
            {
                if (_color != value)
                {
                    _color = value;
                    Invalidate();
                    ColorChanged?.Invoke(_color);
                }
            }
        }

        public VbSwatch()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            Height = 32;
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

            // Draw Swatch Box
            int swW = 44;
            int swH = 22;
            int swX = Width - swW - 8;
            int swY = (Height - swH) / 2;

            var rect = new Rectangle(swX, swY, swW, swH);
            using (var pSwatch = CreateRoundedPath(rect, 4))
            using (var bSwatch = new SolidBrush(_color))
            using (var pBorder = new Pen(VbTheme.BorderLight, 1.5f))
            {
                g.FillPath(bSwatch, pSwatch);
                g.DrawPath(pBorder, pSwatch);
            }
        }

        private static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            if (rect.Width <= d || rect.Height <= d)
            {
                path.AddRectangle(rect);
                return path;
            }
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void PickColor()
        {
            using (var dlg = new ColorDialog())
            {
                dlg.Color = _color;
                dlg.FullOpen = true;
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                {
                    Color = dlg.Color;
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (e.Button == MouseButtons.Left)
            {
                PickColor();
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Space || keyData == Keys.Enter)
            {
                PickColor();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
