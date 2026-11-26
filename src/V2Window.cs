// Liuli v2 · 外壳窗口基类：分层窗口 + 透明画布 + 统一玻璃材质，Dock/灵动岛共用。
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Liuli
{
    abstract class V2Window : Form
    {
        protected Bitmap Canvas;
        protected float UiScale = 1f;

        protected V2Window()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = Color.Black;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= Native.WS_EX_LAYERED | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE;
                if (TopMost) cp.ExStyle |= Native.WS_EX_TOPMOST;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation { get { return true; } }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e) { }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UiScale = DpiUiScale();
            OnReady();
        }

        protected float DpiUiScale()
        {
            try
            {
                int dpi = Native.DpiOf(Handle);
                if (dpi <= 0) dpi = 96;
                return dpi / 96f;
            }
            catch { return 1f; }
        }

        /// <summary>句柄就绪后做一次布局；子类在此登记拖放等。</summary>
        protected virtual void OnReady() { }

        protected void EnsureCanvas()
        {
            if (Canvas != null && Canvas.Width == Width && Canvas.Height == Height) return;
            if (Canvas != null) Canvas.Dispose();
            Canvas = new Bitmap(Math.Max(1, Width), Math.Max(1, Height), System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (IsHandleCreated && Width > 0 && Height > 0) Push();
        }

        protected void Push()
        {
            if (!IsHandleCreated || Canvas == null) return;
            Native.PushLayered(Handle, Canvas, Location, (byte)255);
        }

        /// <summary>离屏预览用：返回画布副本，不改变窗口可见性。</summary>
        public Bitmap Snapshot()
        {
            if (Canvas == null) return null;
            return new Bitmap(Canvas);
        }

        /// <summary>统一玻璃：磨砂壁纸切片 + 色调纱 + 顶边高光 + 1px 渐变描边。</summary>
        protected void PaintGlass(Graphics g, Rectangle body, int radius)
        {
            Theme.DrawShadow(g, body, radius, Util.RoundI(10 * UiScale), 98);
            using (GraphicsPath path = Theme.Round(body, radius))
            {
                g.SetClip(path);
                if (!Theme.Live) Backdrop.Draw(g, Util.ToF(body), new Rectangle(Left + body.X, Top + body.Y, body.Width, body.Height));
                int veil = PlotVeil();
                Color veilColor = Theme.Light
                    ? Color.FromArgb(veil, 250, 251, 255)
                    : Color.FromArgb(veil, 10, 13, 22);
                using (SolidBrush vb = new SolidBrush(veilColor)) g.FillPath(vb, path);

                using (LinearGradientBrush top = new LinearGradientBrush(
                    new Rectangle(body.X, body.Y, body.Width, Math.Max(1, body.Height / 2)),
                    Color.FromArgb(Theme.Light ? 30 : 14, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 90f))
                {
                    g.FillRectangle(top, new Rectangle(body.X, body.Y, body.Width, Math.Max(1, body.Height / 2)));
                }
                using (LinearGradientBrush bottom = new LinearGradientBrush(
                    new Rectangle(body.X, body.Bottom - body.Height / 3, body.Width, Math.Max(1, body.Height / 3)),
                    Color.FromArgb(0, 0, 0, 0), Color.FromArgb(Theme.Light ? 8 : 30, 0, 0, 0), 90f))
                {
                    g.FillRectangle(bottom, new Rectangle(body.X, body.Bottom - body.Height / 3, body.Width, Math.Max(1, body.Height / 3)));
                }

                using (GraphicsPath glowPath = new GraphicsPath())
                {
                    float gw = body.Width * 1.1f, gh = body.Height * 1.2f;
                    glowPath.AddEllipse(body.X - gw * 0.4f, body.Y - gh * 0.8f, gw, gh);
                    using (PathGradientBrush pg = new PathGradientBrush(glowPath))
                    {
                        pg.CenterColor = Color.FromArgb(Theme.Light ? 22 : 30, Theme.Accent);
                        pg.SurroundColors = new Color[] { Color.FromArgb(0, Theme.Accent) };
                        pg.CenterPoint = new PointF(body.X + body.Width * 0.2f, body.Y + body.Height * 0.1f);
                        g.FillPath(pg, glowPath);
                    }
                }
                g.ResetClip();

                using (LinearGradientBrush edge = new LinearGradientBrush(
                    new Rectangle(body.X, body.Y, body.Width, Math.Max(1, body.Height)),
                    Theme.Light ? Color.FromArgb(150, 255, 255, 255) : Color.FromArgb(118, 255, 255, 255),
                    Theme.Light ? Color.FromArgb(26, 0, 0, 0) : Color.FromArgb(22, 0, 0, 0), 90f))
                using (Pen pen = new Pen(edge, Math.Max(1f, 1.1f * UiScale)))
                {
                    pen.LineJoin = LineJoin.Round;
                    g.DrawPath(pen, path);
                }
            }
            Rectangle gloss = new Rectangle(body.X + radius, body.Y + 1,
                Math.Max(2, body.Width - radius * 2), Math.Max(1, Util.RoundI(1.4f * UiScale)));
            using (LinearGradientBrush lb = new LinearGradientBrush(gloss,
                Color.FromArgb(0, 255, 255, 255), Color.FromArgb(80, 255, 255, 255), 0f))
            {
                g.FillRectangle(lb, gloss);
            }
        }

        int PlotVeil()
        {
            int veil = 24 + (int)(Theme.GlassPct * 0.66f);
            if (veil < 36) veil = 36;
            if (veil > 112) veil = 112;
            return veil;
        }

        protected void PaintIconButton(Graphics g, Rectangle r, Glyph glyph, bool hover, bool pressed, float iconScale)
        {
            int radius = r.Height / 2;
            using (GraphicsPath p = Theme.Round(r, radius))
            {
                if (pressed)
                {
                    using (SolidBrush b = new SolidBrush(Theme.Press)) g.FillPath(b, p);
                    using (Pen pen = new Pen(Color.FromArgb(58, 255, 255, 255), 1f)) g.DrawPath(pen, p);
                }
                else if (hover)
                {
                    using (SolidBrush b = new SolidBrush(Theme.Hover)) g.FillPath(b, p);
                    using (Pen pen = new Pen(Color.FromArgb(40, 255, 255, 255), 1f)) g.DrawPath(pen, p);
                }
            }
            Rectangle inner = Util.Inflate(r, -(int)(r.Width * 0.27f));
            inner = ScaleRect(inner, iconScale);
            Theme.Glyph(g, glyph, inner, (hover || pressed) ? Theme.TextMain : Theme.TextSub, 1.5f * UiScale);
        }

        protected static Rectangle ScaleRect(Rectangle r, float scale)
        {
            if (scale < 0.01f) scale = 0.01f;
            int w = Math.Max(1, Util.RoundI(r.Width * scale));
            int h = Math.Max(1, Util.RoundI(r.Height * scale));
            return new Rectangle(r.X + (r.Width - w) / 2, r.Y + (r.Height - h) / 2, w, h);
        }

        protected void PaintTag(Graphics g, string text, int centerX, int y, int maxWidth)
        {
            Font f = Theme.Font(9f, FontStyle.Regular, UiScale);
            SizeF sz = g.MeasureString(text, f);
            int w = Math.Min(maxWidth, Util.RoundI(sz.Width + 18 * UiScale));
            int h = Util.RoundI(18 * UiScale);
            int x = Util.Clamp(centerX - w / 2, Util.RoundI(4 * UiScale), Math.Max(Util.RoundI(4 * UiScale), Width - w - Util.RoundI(4 * UiScale)));
            Rectangle r = new Rectangle(x, y, w, h);
            using (GraphicsPath p = Theme.Round(r, h / 2))
            using (SolidBrush b = new SolidBrush(Theme.Light ? Color.FromArgb(224, 250, 250, 252) : Color.FromArgb(224, 18, 22, 30)))
            {
                g.FillPath(b, p);
            }
            Theme.TextMiddle(g, text, f, Theme.TextMain, r, StringAlignment.Center);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (Canvas != null) { Canvas.Dispose(); Canvas = null; }
            base.OnFormClosed(e);
        }
    }
}
