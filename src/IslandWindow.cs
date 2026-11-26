// Liuli · 顶部灵动岛：收起只显示时间与天气，悬停展开系统读数与三个快捷动作。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Liuli
{
    sealed class IslandWindow : Form
    {
        Bitmap canvas;
        Timer clock;
        Timer hover;
        bool expanded;
        int hoverBtn = -1;
        string lastClock = "";

        public IslandWindow()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = Color.Black;
            Size = new Size(Util.RoundI(400 * Theme.S), Util.RoundI(130 * Theme.S));
            ApplyPosition();
            EnsureCanvas();

            clock = new Timer();
            clock.Interval = 1000;
            clock.Tick += delegate { TickClock(); };
            clock.Start();
            hover = new Timer();
            hover.Interval = 140;
            hover.Tick += delegate { TickHover(); };
            hover.Start();
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

        public void ApplyPosition()
        {
            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            Bounds = new Rectangle(work.X + (work.Width - Width) / 2, work.Y + Util.RoundI(4 * Theme.S), Width, Height);
            Native.KeepAtTop(Handle);
        }

        public void EnsureCanvas()
        {
            if (canvas != null && canvas.Width == Width && canvas.Height == Height) return;
            if (canvas != null) canvas.Dispose();
            canvas = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        }

        Rectangle CollapsedPill()
        {
            float s = Theme.S;
            int w = Util.RoundI(230 * s), h = Util.RoundI(40 * s);
            return new Rectangle((Width - w) / 2, Util.RoundI(8 * s), w, h);
        }

        Rectangle ExpandedPill()
        {
            float s = Theme.S;
            int w = Util.RoundI(360 * s), h = Util.RoundI(104 * s);
            return new Rectangle((Width - w) / 2, Util.RoundI(8 * s), w, h);
        }

        Rectangle[] QuickButtons()
        {
            float s = Theme.S;
            Rectangle body = ExpandedPill();
            int bs = Util.RoundI(22 * s), gap = Util.RoundI(4 * s);
            int y = body.Bottom - Util.RoundI(30 * s);
            int right = body.Right - Util.RoundI(16 * s);
            return new Rectangle[] {
                new Rectangle(right - bs * 3 - gap * 2, y, bs, bs),
                new Rectangle(right - bs * 2 - gap, y, bs, bs),
                new Rectangle(right - bs, y, bs, bs)
            };
        }

        void TickClock()
        {
            string now = DateTime.Now.ToString("HH:mm");
            if (now != lastClock || expanded)
            {
                lastClock = now;
                Render();
            }
        }

        void TickHover()
        {
            Point p = PointToClient(Cursor.Position);
            Rectangle hit = expanded ? ExpandedPill() : CollapsedPill();
            bool inside = hit.Contains(p);
            if (expanded)
            {
                if (!inside)
                {
                    expanded = false;
                    hoverBtn = -1;
                    Render();
                }
                else
                {
                    int hb = -1;
                    Rectangle[] rs = QuickButtons();
                    for (int i = 0; i < rs.Length; i++) if (rs[i].Contains(p)) hb = i;
                    if (hb != hoverBtn) { hoverBtn = hb; Render(); }
                    Cursor = hb >= 0 ? Cursors.Hand : Cursors.Default;
                }
            }
            else
            {
                if (inside)
                {
                    expanded = true;
                    hoverBtn = -1;
                    Render();
                }
                Cursor = Cursors.Default;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;
            if (expanded)
            {
                Rectangle[] rs = QuickButtons();
                for (int i = 0; i < rs.Length; i++)
                {
                    if (!rs[i].Contains(e.Location)) continue;
                    if (i == 0) App.ShowKindMenu(PointToScreen(e.Location));
                    else if (i == 1) App.OrganizeDesktop();
                    else App.OpenSettings();
                    return;
                }
            }
            App.OpenSettings();
        }

        // ---------- 绘制 ----------
        public void Render()
        {
            EnsureCanvas();
            Graphics g = Graphics.FromImage(canvas);
            try
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.FromArgb(0));
                Rectangle body = expanded ? ExpandedPill() : CollapsedPill();
                int radius = body.Height / 2;
                Theme.DrawShadow(g, body, radius, Util.RoundI(9 * Theme.S), 90);
                using (GraphicsPath p = Theme.Round(body, radius))
                {
                    g.SetClip(p);
                    if (!Theme.Live) Backdrop.Draw(g, Util.ToF(body), new Rectangle(Left + body.X, Top + body.Y, body.Width, body.Height));
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(58, Theme.Light ? 250 : 9, Theme.Light ? 250 : 12, Theme.Light ? 252 : 20))) g.FillPath(b, p);
                    g.ResetClip();
                    using (Pen pen = new Pen(Color.FromArgb(76, 255, 255, 255), 1f)) g.DrawPath(pen, p);
                }
                Rectangle glow = new Rectangle(body.X + Util.RoundI(10 * Theme.S), body.Y + 1, body.Width - Util.RoundI(20 * Theme.S), Util.RoundI(2 * Theme.S));
                using (LinearGradientBrush lb = new LinearGradientBrush(glow, Color.FromArgb(0, 255, 255, 255), Color.FromArgb(76, 255, 255, 255), 0f))
                    g.FillRectangle(lb, glow);

                if (expanded) PaintExpanded(g, body);
                else PaintCollapsed(g, body);

                Native.PushLayered(Handle, canvas, Location, (byte)255);
            }
            finally { g.Dispose(); }
        }

        void PaintCollapsed(Graphics g, Rectangle body)
        {
            float s = Theme.S;
            WeatherNow wx = App.Weather.Now;
            int x = body.X + Util.RoundI(13 * s);
            if (wx.Has) WeatherService.Glyph(g, wx.Code, new Rectangle(x + Util.RoundI(2 * s), body.Y + Util.RoundI(12 * s), Util.RoundI(16 * s), Util.RoundI(16 * s)), Theme.Accent);
            else using (SolidBrush b = new SolidBrush(Theme.Accent)) g.FillEllipse(b, x + Util.RoundI(5 * s), body.Y + body.Height / 2 - Util.RoundI(4 * s), Util.RoundI(8 * s), Util.RoundI(8 * s));

            Rectangle tr = new Rectangle(x + Util.RoundI(24 * s), body.Y, Util.RoundI(62 * s), body.Height);
            Theme.TextMiddle(g, DateTime.Now.ToString("HH:mm"), Theme.Font(13f, FontStyle.Bold), Theme.TextMain, tr, StringAlignment.Near);

            int dx = tr.Right + Util.RoundI(4 * s);
            using (Pen pen = new Pen(Color.FromArgb(70, Theme.TextSub), 1f))
                g.DrawLine(pen, dx, body.Y + Util.RoundI(11 * s), dx, body.Bottom - Util.RoundI(11 * s));

            string info = wx.Has ? (wx.Temp + "° " + wx.Cond) : ("CPU " + (int)App.StatsNow.Cpu + "%");
            Rectangle ir = new Rectangle(dx + Util.RoundI(10 * s), body.Y, body.Width - (dx - body.X) - Util.RoundI(18 * s), body.Height);
            Theme.TextMiddle(g, info, Theme.Font(9.5f, FontStyle.Regular), Theme.TextSub, ir, StringAlignment.Near);
        }

        void PaintExpanded(Graphics g, Rectangle body)
        {
            float s = Theme.S;
            WeatherNow wx = App.Weather.Now;
            StatsNow st = App.StatsNow;

            Rectangle tr = new Rectangle(body.X + Util.RoundI(20 * s), body.Y + Util.RoundI(9 * s), Util.RoundI(110 * s), Util.RoundI(34 * s));
            Theme.TextMiddle(g, DateTime.Now.ToString("HH:mm"), Theme.Font(24f, FontStyle.Bold), Theme.TextMain, tr, StringAlignment.Near);
            Rectangle dr = new Rectangle(body.X + Util.RoundI(22 * s), body.Y + Util.RoundI(42 * s), body.Width - Util.RoundI(44 * s), Util.RoundI(16 * s));
            Theme.TextMiddle(g, DateLine(), Theme.Font(8.5f, FontStyle.Regular), Theme.TextSub, dr, StringAlignment.Near);

            // 天气：图标 + 温度 + 描述
            int wxX = body.Right - Util.RoundI(128 * s);
            if (wx.Has)
            {
                WeatherService.Glyph(g, wx.Code, new Rectangle(wxX, body.Y + Util.RoundI(12 * s), Util.RoundI(28 * s), Util.RoundI(28 * s)), Theme.Accent);
                Rectangle tempr = new Rectangle(wxX + Util.RoundI(32 * s), body.Y + Util.RoundI(8 * s), Util.RoundI(88 * s), Util.RoundI(34 * s));
                Theme.TextMiddle(g, wx.Temp + "°", Theme.Font(19f, FontStyle.Bold), Theme.TextMain, tempr, StringAlignment.Near);
                Rectangle cond = new Rectangle(wxX + Util.RoundI(33 * s), body.Y + Util.RoundI(38 * s), Util.RoundI(96 * s), Util.RoundI(16 * s));
                Theme.TextMiddle(g, wx.Cond + " · 湿度 " + wx.Humidity + "% · 风 " + wx.Wind.ToString("0.0") + "m/s", Theme.Font(7.5f, FontStyle.Regular), Theme.TextSub, cond, StringAlignment.Near);
            }
            else
            {
                Rectangle nowr = new Rectangle(wxX, body.Y + Util.RoundI(14 * s), Util.RoundI(120 * s), body.Height / 3);
                Theme.TextMiddle(g, "天气离线", Theme.Font(9f, FontStyle.Regular), Theme.TextSub, nowr, StringAlignment.Near);
            }

            using (Pen pen = new Pen(Color.FromArgb(38, Theme.TextSub), 1f))
                g.DrawLine(pen, body.X + Util.RoundI(20 * s), body.Y + Util.RoundI(62 * s), body.Right - Util.RoundI(20 * s), body.Y + Util.RoundI(62 * s));

            int y = body.Bottom - Util.RoundI(42 * s);
            Stat(g, body.X + Util.RoundI(22 * s), y, "CPU", st.CpuText, st.Cpu / 100f);
            Stat(g, body.X + Util.RoundI(98 * s), y, "内存", st.MemText, st.MemPct / 100f);
            Stat(g, body.X + Util.RoundI(176 * s), y, "磁盘", st.DiskText, st.DiskPct / 100f);

            Rectangle[] rs = QuickButtons();
            Glyph[] gl = { Glyph.Plus, Glyph.Broom, Glyph.Gear };
            for (int i = 0; i < 3; i++)
                Glass.Button(g, rs[i], gl[i], hoverBtn == i, false, Theme.Accent);
        }

        void Stat(Graphics g, int x, int y, string label, string text, float pct)
        {
            float s = Theme.S;
            Rectangle lr = new Rectangle(x, y, Util.RoundI(70 * s), Util.RoundI(14 * s));
            Theme.TextMiddle(g, label + "  " + text, Theme.Font(8f, FontStyle.Regular), Theme.TextSub, lr, StringAlignment.Near);
            Rectangle bar = new Rectangle(x, y + Util.RoundI(17 * s), Util.RoundI(66 * s), Util.RoundI(3 * s));
            using (GraphicsPath p = Theme.Round(bar, Math.Max(1, bar.Height / 2)))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(46, Theme.TextSub))) g.FillPath(b, p);
            float fp = pct; if (fp < 0f) fp = 0f; if (fp > 1f) fp = 1f;
            int w = (int)(bar.Width * fp);
            if (w < 2) w = 2;
            Rectangle fill = new Rectangle(bar.X, bar.Y, w, bar.Height);
            using (GraphicsPath p = Theme.Round(fill, Math.Max(1, fill.Height / 2)))
            using (SolidBrush b = new SolidBrush(Theme.Accent)) g.FillPath(b, p);
        }

        static string DateLine()
        {
            string[] wd = { "日", "一", "二", "三", "四", "五", "六" };
            DateTime d = DateTime.Now;
            return (d.Month) + "月" + d.Day + "日 星期" + wd[(int)d.DayOfWeek];
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (clock != null) clock.Stop();
            if (hover != null) hover.Stop();
            base.OnFormClosed(e);
        }
    }
}
