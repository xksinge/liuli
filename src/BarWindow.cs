// Liuli · 控制条：桌面上一枚玻璃胶囊，新建看板 / 整理图标 / 设置；可拖动，位置记住。
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Liuli
{
    sealed class BarWindow : Form
    {
        int hoverBtn = -1;
        bool down;
        bool moved;
        Point downCursor;
        Point startLoc;
        Bitmap canvas;
        long hoverT0 = Environment.TickCount - 1000;
        int[] hoverAlpha = new int[3];

        public BarWindow()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = Color.Black;
            ClientSize = ComputeSize();
            ApplyPosition();
            EnsureCanvas();
        }

        Size ComputeSize()
        {
            float s = Theme.S;
            return new Size(Util.RoundI(150 * s), Util.RoundI(54 * s));
        }

        public void EnsureCanvas()
        {
            if (canvas != null && canvas.Width == Width && canvas.Height == Height) return;
            if (canvas != null) canvas.Dispose();
            canvas = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        }

        public void ApplyPosition()
        {
            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            int x = App.Ws.Set.BarX, y = App.Ws.Set.BarY;
            bool onScreen = false;
            if (!(x == -1 && y == -1))
            {
                foreach (Screen sc in Screen.AllScreens)
                {
                    if (sc.WorkingArea.IntersectsWith(new Rectangle(x, y, Width, Height))) { onScreen = true; break; }
                }
            }
            if (!onScreen)
            {
                x = work.X + (work.Width - Width) / 2;
                y = work.Bottom - Height - Util.RoundI(22 * Theme.S);
            }
            Bounds = new Rectangle(x, y, Width, Height);
            Native.KeepAtTop(Handle);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= Native.WS_EX_LAYERED | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE | Native.WS_EX_TOPMOST;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation { get { return true; } }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e) { }

        Rectangle[] Buttons()
        {
            float s = Theme.S;
            int bs = Util.RoundI(38 * s);
            int gap = Util.RoundI(6 * s);
            int left = Util.RoundI(18 * s);
            int y = (Height - bs) / 2;
            return new Rectangle[] {
                new Rectangle(left, y, bs, bs),
                new Rectangle(left + bs + gap, y, bs, bs),
                new Rectangle(left + bs * 2 + gap * 2, y, bs, bs)
            };
        }

        public void Render()
        {
            EnsureCanvas();
            Graphics g = Graphics.FromImage(canvas);
            try
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.FromArgb(0));
                Rectangle body = new Rectangle(Util.RoundI(6 * Theme.S), Util.RoundI(6 * Theme.S), Width - Util.RoundI(12 * Theme.S), Height - Util.RoundI(12 * Theme.S));
                int radius = body.Height / 2;
                Theme.DrawShadow(g, body, radius, Util.RoundI(9 * Theme.S), 96);
                using (GraphicsPath p = Theme.Round(body, radius))
                {
                    g.SetClip(p);
                    if (!Theme.Live) Backdrop.Draw(g, Util.ToF(body), new Rectangle(Left + body.X, Top + body.Y, body.Width, body.Height));
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(52, Theme.Light ? 250 : 10, Theme.Light ? 250 : 13, Theme.Light ? 250 : 22))) g.FillPath(b, p);
                    g.ResetClip();
                    using (Pen pen = new Pen(Color.FromArgb(80, 255, 255, 255), 1f)) g.DrawPath(pen, p);
                }
                Rectangle glow = new Rectangle(body.X + Util.RoundI(8 * Theme.S), body.Y + 1, body.Width - Util.RoundI(16 * Theme.S), Util.RoundI(2 * Theme.S));
                using (LinearGradientBrush lb = new LinearGradientBrush(glow, Color.FromArgb(0, 255, 255, 255), Color.FromArgb(80, 255, 255, 255), 0f))
                    g.FillRectangle(lb, glow);

                // 拖动握把
                for (int i = 0; i < 3; i++)
                    g.FillEllipse(new SolidBrush(Color.FromArgb(80, Theme.TextSub)),
                        body.X + Util.RoundI(8 * Theme.S), body.Y + body.Height / 2 - Util.RoundI(8 * Theme.S) + i * Util.RoundI(6 * Theme.S),
                        Util.RoundI(3 * Theme.S), Util.RoundI(3 * Theme.S));

                Rectangle[] rs = Buttons();
                Glyph[] gl = { Glyph.Plus, Glyph.Broom, Glyph.Gear };
                for (int i = 0; i < 3; i++)
                {
                    Glass.Button(g, rs[i], gl[i], hoverBtn == i, false, Theme.Accent);
                }
                Native.PushLayered(Handle, canvas, Location, (byte)255);
            }
            finally { g.Dispose(); }
        }

        void UpdateHover(Point p)
        {
            int hb = -1;
            Rectangle[] rs = Buttons();
            for (int i = 0; i < 3; i++) if (rs[i].Contains(p)) hb = i;
            if (hb != hoverBtn) { hoverBtn = hb; Render(); }
            Cursor = hb >= 0 ? Cursors.Hand : Cursors.SizeAll;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            App.KickAnimation();
            if (down)
            {
                Point cur = Cursor.Position;
                if (!moved && (Math.Abs(cur.X - downCursor.X) > 3 || Math.Abs(cur.Y - downCursor.Y) > 3)) moved = true;
                if (moved)
                {
                    Location = new Point(startLoc.X + cur.X - downCursor.X, startLoc.Y + cur.Y - downCursor.Y);
                    return;
                }
            }
            UpdateHover(e.Location);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            down = true; moved = false;
            downCursor = Cursor.Position;
            startLoc = Location;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!down) return;
            down = false;
            if (moved)
            {
                App.Ws.Set.BarX = Left; App.Ws.Set.BarY = Top;
                App.Save();
                return;
            }
            Rectangle[] rs = Buttons();
            for (int i = 0; i < 3; i++)
            {
                if (!rs[i].Contains(e.Location)) continue;
                if (i == 0) App.ShowKindMenu(PointToScreen(e.Location));
                else if (i == 1) App.OrganizeDesktop();
                else App.OpenSettings();
                return;
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
        }
    }
}
