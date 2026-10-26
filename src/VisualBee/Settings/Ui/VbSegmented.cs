using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MusicBeePlugin.SettingsUi
{
    internal sealed class VbSegmented : Control
    {
        public event Action<int> SelectedIndexChanged;

        private string[] _items = new string[0];
        private int _selectedIndex = -1;
        private int _hoverIndex = -1;

        public string[] Items
        {
            get => _items;
            set
            {
                _items = value ?? new string[0];
                Invalidate();
            }
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                    Invalidate();
                    SelectedIndexChanged?.Invoke(_selectedIndex);
                }
            }
        }

        public VbSegmented()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            Height = 32;
            Margin = new Padding(0, 2, 0, 2);
            TabStop = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_items == null || _items.Length == 0) return;

            int n = _items.Length;
            float itemW = (float)(Width - 2) / n;
            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);

            using (var pBg = CreateRoundedPath(bounds, 6))
            using (var bBg = new SolidBrush(VbTheme.BgCard))
            using (var pBorder = new Pen(VbTheme.Border))
            {
                g.FillPath(bBg, pBg);
                g.DrawPath(pBorder, pBg);
            }

            Color accentCol = Enabled ? VbTheme.Accent : VbTheme.BorderLight;

            for (int i = 0; i < n; i++)
            {
                var itemRect = new Rectangle((int)(i * itemW + 2), 2, (int)itemW - 4, Height - 5);
                bool isSelected = (i == _selectedIndex);
                bool isHover = (i == _hoverIndex && !isSelected && Enabled);

                if (isSelected)
                {
                    using (var pItem = CreateRoundedPath(itemRect, 4))
                    using (var bSel = new SolidBrush(accentCol))
                        g.FillPath(bSel, pItem);
                }
                else if (isHover)
                {
                    using (var pItem = CreateRoundedPath(itemRect, 4))
                    using (var bHover = new SolidBrush(VbTheme.BgHover))
                        g.FillPath(bHover, pItem);
                }

                Color textCol = isSelected ? Color.Black : (isHover ? Color.White : (Enabled ? VbTheme.TextPrimary : VbTheme.TextMuted));
                using (var bText = new SolidBrush(textCol))
                {
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center,
                        Trimming = StringTrimming.EllipsisCharacter
                    };
                    g.DrawString(_items[i], isSelected ? VbTheme.FontBold : VbTheme.FontRegular, bText, itemRect, sf);
                }
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

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!Enabled) return;
            Focus();
            if (e.Button == MouseButtons.Left && _items.Length > 0)
            {
                float itemW = (float)(Width - 2) / _items.Length;
                int idx = (int)(e.X / itemW);
                if (idx >= 0 && idx < _items.Length)
                {
                    SelectedIndex = idx;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!Enabled) return;
            if (_items.Length > 0)
            {
                float itemW = (float)(Width - 2) / _items.Length;
                int idx = (int)(e.X / itemW);
                if (idx != _hoverIndex)
                {
                    _hoverIndex = idx;
                    Invalidate();
                }
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoverIndex != -1)
            {
                _hoverIndex = -1;
                Invalidate();
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (!Enabled) return base.ProcessCmdKey(ref msg, keyData);
            if (_items.Length > 0)
            {
                if (keyData == Keys.Left || keyData == Keys.Up)
                {
                    SelectedIndex = Math.Max(0, _selectedIndex - 1);
                    return true;
                }
                if (keyData == Keys.Right || keyData == Keys.Down)
                {
                    SelectedIndex = Math.Min(_items.Length - 1, _selectedIndex + 1);
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
