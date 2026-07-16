using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.IO.Compression;

namespace YouTubeDownloader
{
    //  CUSTOM CONTROLS (unchanged from your original)
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    public class RoundedPanel : Panel
    {
        private int _cornerRadius = 10;
        public int CornerRadius { get { return _cornerRadius; } set { _cornerRadius = value; } }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = RoundRect(ClientRectangle, CornerRadius))
            using (SolidBrush brush = new SolidBrush(BackColor))
            {
                e.Graphics.FillPath(brush, path);
            }
        }

        private GraphicsPath RoundRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class IsolateButton : Button
    {
        private Color _normalBack;
        private Color _hoverBack;
        private Color _pressBack;
        private Color _current;
        private bool _hovered;
        private bool _pressed;
        private int _cornerRadius = 8;
        public int CornerRadius { get { return _cornerRadius; } set { _cornerRadius = value; } }

        public IsolateButton(Color normal, Color hover, Color press)
        {
            _normalBack = normal;
            _hoverBack = hover;
            _pressBack = press;
            _current = normal;

            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = normal;
            ForeColor = Color.White;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs e) { _hovered = true; _current = _hoverBack; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovered = false; _current = _pressed ? _pressBack : _normalBack; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; _current = _pressBack; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; _current = _hovered ? _hoverBack : _normalBack; Invalidate(); base.OnMouseUp(e); }

        public void UpdateColors(Color normal, Color hover, Color press)
        {
            _normalBack = normal;
            _hoverBack = hover;
            _pressBack = press;
            _current = _hovered ? hover : normal;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            if (Parent != null) g.Clear(Parent.BackColor);
            else g.Clear(BackColor);

            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle r = ClientRectangle;
            r.Width -= 1;
            r.Height -= 1;

            using (GraphicsPath path = RoundRect(r, CornerRadius))
            {
                using (SolidBrush b = new SolidBrush(_current))
                    g.FillPath(b, path);

                TextRenderer.DrawText(g, Text, Font, ClientRectangle, ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            }
        }

        private GraphicsPath RoundRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class IsolateProgressBar : Control
    {
        private int _value = 0;
        private int _max = 100;
        private Color _barColor = Color.FromArgb(229, 57, 53);
        private Color _trackColor = Color.FromArgb(40, 40, 44);

        public int Value
        {
            get { return _value; }
            set { _value = Math.Max(0, Math.Min(_max, value)); Invalidate(); }
        }
        public int Maximum
        {
            get { return _max; }
            set { _max = value; Invalidate(); }
        }
        public Color BarColor
        {
            get { return _barColor; }
            set { _barColor = value; Invalidate(); }
        }
        public Color TrackColor
        {
            get { return _trackColor; }
            set { _trackColor = value; Invalidate(); }
        }

        public IsolateProgressBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer, true);
            Height = 6;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath track = RoundRect(ClientRectangle, 3))
            using (SolidBrush tb = new SolidBrush(_trackColor))
                g.FillPath(tb, track);

            if (_value > 0 && _max > 0)
            {
                int barW = (int)((double)_value / _max * Width);
                Rectangle barRect = new Rectangle(0, 0, Math.Max(6, barW), Height);
                using (GraphicsPath bar = RoundRect(barRect, 3))
                using (LinearGradientBrush lb = new LinearGradientBrush(
                    barRect, Color.FromArgb(239, 83, 80), Color.FromArgb(183, 28, 28), 0f))
                    g.FillPath(lb, bar);
            }
        }

        private GraphicsPath RoundRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            if (r.Width < d) d = r.Width;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }


    public class IsolateComboBox : ComboBox
    {
        public IsolateComboBox()
        {
            DropDownStyle = ComboBoxStyle.DropDownList;
            DrawMode = DrawMode.OwnerDrawFixed;
            FlatStyle = FlatStyle.Flat;
            BackColor = Color.FromArgb(30, 30, 35);
            ForeColor = Color.FromArgb(230, 230, 230);
            ItemHeight = 22;
            MaxDropDownItems = 30;
            IntegralHeight = false;
            SetStyle(ControlStyles.UserPaint, false);
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            bool isEdit = (e.State & DrawItemState.ComboBoxEdit) == DrawItemState.ComboBoxEdit;

            using (SolidBrush b = new SolidBrush(Color.FromArgb(30, 30, 35)))
                e.Graphics.FillRectangle(b, e.Bounds);

            Color fg = (isSelected && !isEdit) ? Color.White : Color.FromArgb(230, 230, 230);
            int textX = 6;

            if (isSelected && !isEdit)
            {
                using (SolidBrush dot = new SolidBrush(Color.FromArgb(67, 160, 71)))
                {
                    int dotSize = 6;
                    int dotY = e.Bounds.Y + (e.Bounds.Height - dotSize) / 2;
                    e.Graphics.FillEllipse(dot, e.Bounds.X + 8, dotY, dotSize, dotSize);
                }
                textX = 22;
            }

            TextRenderer.DrawText(e.Graphics, Items[e.Index].ToString(), e.Font,
                new Rectangle(e.Bounds.X + textX, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height),
                fg, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr BeginPaint(IntPtr hwnd, out PAINTSTRUCT lpPaint);
        
        [DllImport("user32.dll")]
        private static extern bool EndPaint(IntPtr hwnd, ref PAINTSTRUCT lpPaint);

        [StructLayout(LayoutKind.Sequential)]
        private struct PAINTSTRUCT
        {
            public IntPtr hdc;
            public bool fErase;
            public Rectangle rcPaint;
            public bool fRestore;
            public bool fIncUpdate;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] rgbReserved;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0014) // WM_ERASEBKGND
            {
                m.Result = (IntPtr)1;
                return;
            }

            if (m.Msg == 0x000F) // WM_PAINT
            {
                PAINTSTRUCT ps;
                IntPtr hdc = BeginPaint(m.HWnd, out ps);
                using (Graphics g = Graphics.FromHdc(hdc))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;

                    if (!this.Enabled)
                    {
                        using (SolidBrush b = new SolidBrush(Color.FromArgb(24, 24, 28)))
                            g.FillRectangle(b, 0, 0, Width, Height);

                        TextRenderer.DrawText(g, Text, Font, new Rectangle(4, 0, Width, Height),
                            Color.FromArgb(100, 100, 105), TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

                        // Draw disabled arrow so it doesn't look like it disappeared
                        using (SolidBrush b = new SolidBrush(Color.FromArgb(80, 80, 85)))
                        {
                            Point[] arrow = new Point[] {
                                new Point(Width - 14, Height / 2 - 2),
                                new Point(Width - 6, Height / 2 - 2),
                                new Point(Width - 10, Height / 2 + 3)
                            };
                            g.FillPolygon(b, arrow);
                        }

                        using (Pen p = new Pen(Color.FromArgb(35, 35, 40), 1))
                            g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
                    }
                    else
                    {
                        using (SolidBrush b = new SolidBrush(Color.FromArgb(30, 30, 35)))
                            g.FillRectangle(b, 0, 0, Width, Height);

                        TextRenderer.DrawText(g, Text, Font, new Rectangle(4, 0, Width, Height),
                            Color.FromArgb(230, 230, 230), TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

                        using (SolidBrush b = new SolidBrush(Color.FromArgb(160, 160, 168)))
                        {
                            Point[] arrow = new Point[] {
                                new Point(Width - 14, Height / 2 - 2),
                                new Point(Width - 6, Height / 2 - 2),
                                new Point(Width - 10, Height / 2 + 3)
                            };
                            g.FillPolygon(b, arrow);
                        }

                        using (Pen p = new Pen(Color.FromArgb(40, 40, 45), 2))
                            g.DrawRectangle(p, 1, 1, Width - 2, Height - 2);

                        using (Pen p = new Pen(Color.FromArgb(30, 30, 35), 1))
                            g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
                    }
                }
                EndPaint(m.HWnd, ref ps);
                m.Result = IntPtr.Zero;
                return; // DO NOT call base.WndProc! This prevents native white borders!
            }

            base.WndProc(ref m);
        }
    }

    public class IsolateRadioButton : RadioButton
    {
        public IsolateRadioButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            ForeColor = Color.FromArgb(230, 230, 230);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            if (Parent != null) g.Clear(Parent.BackColor);
            else g.Clear(BackColor);

            g.SmoothingMode = SmoothingMode.AntiAlias;

            int circleSize = 16;
            int y = (Height - circleSize) / 2;
            Rectangle circleRect = new Rectangle(0, y, circleSize, circleSize);

            using (Pen p = new Pen(Checked ? Color.FromArgb(229, 57, 53) : Color.FromArgb(100, 100, 105), 2f))
                g.DrawEllipse(p, circleRect);

            if (Checked)
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(229, 57, 53)))
                    g.FillEllipse(b, new Rectangle(4, y + 4, 8, 8));
            }

            TextRenderer.DrawText(g, Text, Font, new Point(24, (Height - TextRenderer.MeasureText(Text, Font).Height) / 2), ForeColor);
        }
    }

    public class IsolateCheckBox : CheckBox
    {
        public IsolateCheckBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            ForeColor = Color.FromArgb(230, 230, 230);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            if (Parent != null) g.Clear(Parent.BackColor);
            else g.Clear(BackColor);

            g.SmoothingMode = SmoothingMode.AntiAlias;

            int boxSize = 16;
            int y = (Height - boxSize) / 2;
            Rectangle boxRect = new Rectangle(0, y, boxSize, boxSize);

            using (Pen p = new Pen(Checked ? Color.FromArgb(229, 57, 53) : Color.FromArgb(100, 100, 105), 2f))
                g.DrawRectangle(p, boxRect);

            if (Checked)
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(229, 57, 53)))
                    g.FillRectangle(b, new Rectangle(3, y + 3, 11, 11));
            }

            TextRenderer.DrawText(g, Text, Font, new Point(24, (Height - TextRenderer.MeasureText(Text, Font).Height) / 2), ForeColor);
        }
    }

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

}
