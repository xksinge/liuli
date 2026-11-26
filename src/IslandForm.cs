// Liuli v2 · 顶部灵动岛：收起显示时间/天气，悬停展开日期、系统读数与快捷动作。
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Liuli
{
    sealed class IslandForm : V2Window
    {
        Timer clock;
        Timer hover;
        Timer animTimer;
        bool expanded;
        int hoverBtn = -1;
        int pressedBtn = -1;
        int bounceBtn = -1;
        bool pressedBody;
        long bounceAt;
        long pulseAt;
        string lastClock = "";
        long leaveAt;

        public IslandForm()
        {
            ClientSize = CollapsedSize();
            ApplyPosition();

            clock = new Timer();
            clock.Interval = 1000;
            clock.Tick += delegate { TickClock(); };
            clock.Start();

            hover = new Timer();
            hover.Interval = 140;
            hover.Tick += delegate { TickHover(); };
            hover.Start();

            animTimer = new Timer();
            animTimer.Interval = 16;
            animTimer.Tick += delegate { TickAnim(); };
        }

        protected override void OnReady()
        {
            UiScale = DpiUiScale();
            if (expanded) ClientSize = ExpandedSize(); else ClientSize = CollapsedSize();
            EnsureCanvas();
            ApplyPosition();
            Render();
        }

        Size CollapsedSize()
        {
            int m = Util.RoundI(12 * UiScale);
            return new Size(Util.RoundI(240 * UiScale) + m * 2, Util.RoundI(40 * UiScale) + m * 2);
        }

        Size ExpandedSize()
        {
            int m = Util.RoundI(12 * UiScale);
            return new Size(Util.RoundI(380 * UiScale) + m * 2, Util.RoundI(112 * UiScale) + m * 2);
        }

        Rectangle CurrentBody()
        {
            int m = Util.RoundI(12 * UiScale);
            return new Rectangle(m, m, Width - m * 2, Height - m * 2);
        }

        Rectangle[] QuickButtons()
        {
            Rectangle body = CurrentBody();
            int bs = Util.RoundI(24 * UiScale), gap = Util.RoundI(4 * UiScale);
            int y = body.Bottom - Util.RoundI(34 * UiScale);
            int right = body.Right - Util.RoundI(15 * UiScale);
            return new Rectangle[] {
                new Rectangle(right - bs * 3 - gap * 2, y, bs, bs),
                new Rectangle(right - bs * 2 - gap, y, bs, bs),
                new Rectangle(right - bs, y, bs, bs)
            };
        }

        public void ApplyPosition()
        {
            if (!IsHandleCreated) return;
            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(work.X + (work.Width - Width) / 2, work.Y + Util.RoundI(6 * UiScale));
            Native.KeepAtTop(Handle);
        }

        void SetExpanded(bool on)
        {
            if (expanded == on) return;
            expanded = on;
            ClientSize = expanded ? ExpandedSize() : CollapsedSize();
            EnsureCanvas();
            ApplyPosition();
            Render();
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
            if (!IsHandleCreated) return;
            Point p = PointToClient(Cursor.Position);
            Rectangle hit = CurrentBody();
            bool inside = hit.Contains(p);
            if (expanded)
            {
                if (!inside)
                {
                    long now = Environment.TickCount;
                    if (leaveAt == 0) leaveAt = now;
                    else if (now - leaveAt > 900) { leaveAt = 0; SetExpanded(false); }
                    return;
                }
                leaveAt = 0;
                int hb = -1;
                Rectangle[] rs = QuickButtons();
                for (int i = 0; i < rs.Length; i++) if (rs[i].Contains(p)) hb = i;
                if (hb != hoverBtn) { hoverBtn = hb; Render(); }
                Cursor = hb >= 0 ? Cursors.Hand : Cursors.Default;
            }
            else
            {
                leaveAt = 0;
                if (inside)
                {
                    hoverBtn = -1;
                    SetExpanded(true);
                }
                Cursor = Cursors.Default;
            }
        }

        public void RefreshIsland()
        {
            if (IsHandleCreated) Render();
        }

        /// <summary>离屏预览用：强制展开并重排。</summary>
        public void ExpandForPreview()
        {
            if (!IsHandleCreated) return;
            expanded = true;
            ClientSize = ExpandedSize();
            EnsureCanvas();
            ApplyPosition();
            Render();
        }

        // ---------- 点击反馈 ----------
        float BounceScale()
        {
            if (bounceAt == 0) return 1f;
            float t = (Environment.TickCount - bounceAt) / 190f;
            if (t < 0f) t = 0f;
            if (t >= 1f) return 1f;
            float e = 1f - (1f - t) * (1f - t) * (1f - t);
            return 0.94f + 0.10f * e + 0.06f * (float)Math.Sin(Math.PI * t);
        }

        void Pulse(int button)
        {
            bounceBtn = button;
            bounceAt = Environment.TickCount;
            pulseAt = Environment.TickCount;
            if (animTimer != null && !animTimer.Enabled) animTimer.Start();
            Render();
        }

        void TickAnim()
        {
            long now = Environment.TickCount;
            if (bounceAt != 0 && now - bounceAt > 220)
            {
                bounceBtn = -1;
                bounceAt = 0;
                pulseAt = 0;
                if (animTimer != null) animTimer.Stop();
                Render();
                return;
            }
            Render();
        }

        public void PreviewPressBtn(int index)
        {
            pressedBtn = index;
            Render();
        }

        // ---------- 绘制 ----------
        public void Render()
        {
            if (!IsHandleCreated) return;
            EnsureCanvas();
            Graphics g = Graphics.FromImage(Canvas);
            try
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.FromArgb(0));
                Rectangle body = CurrentBody();
                PaintGlass(g, body, body.Height / 2);

                int pressAlpha = 0;
                if (pressedBody) pressAlpha = 42;
                else if (pulseAt != 0)
                {
                    float t = (Environment.TickCount - pulseAt) / 190f;
                    if (t >= 0f && t < 1f) pressAlpha = (int)(38 * (1f - t));
                }
                if (pressAlpha > 0)
                {
                    using (GraphicsPath p = Theme.Round(body, body.Height / 2))
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(pressAlpha, 0, 0, 0)))
                        g.FillPath(b, p);
                }

                if (expanded) PaintExpanded(g, body);
                else PaintCollapsed(g, body);
                Native.PushLayered(Handle, Canvas, Location, (byte)255);
            }
            finally { g.Dispose(); }
        }

        void PaintCollapsed(Graphics g, Rectangle body)
        {
            WeatherNow wx = V2Shell.Weather != null ? V2Shell.Weather.Now : new WeatherNow();
            int x = body.X + Util.RoundI(13 * UiScale);
            if (wx.Has)
                WeatherService.Glyph(g, wx.Code, new Rectangle(x + Util.RoundI(2 * UiScale), body.Y + Util.RoundI(11 * UiScale),
                    Util.RoundI(16 * UiScale), Util.RoundI(16 * UiScale)), Theme.Accent);
            else
                using (SolidBrush b = new SolidBrush(Theme.Accent))
                    g.FillEllipse(b, x + Util.RoundI(5 * UiScale), body.Y + body.Height / 2 - Util.RoundI(4 * UiScale),
                        Util.RoundI(8 * UiScale), Util.RoundI(8 * UiScale));

            Rectangle tr = new Rectangle(x + Util.RoundI(24 * UiScale), body.Y, Util.RoundI(62 * UiScale), body.Height);
            Theme.TextMiddle(g, DateTime.Now.ToString("HH:mm"), Theme.Font(13f, FontStyle.Bold, UiScale), Theme.TextMain, tr, StringAlignment.Near);

            int dx = tr.Right + Util.RoundI(4 * UiScale);
            using (Pen pen = new Pen(Color.FromArgb(70, Theme.TextSub), 1f))
                g.DrawLine(pen, dx, body.Y + Util.RoundI(10 * UiScale), dx, body.Bottom - Util.RoundI(10 * UiScale));

            string info = wx.Has ? (wx.Temp + "° " + wx.Cond) : ("CPU " + (int)V2Shell.Stats.Cpu + "%");
            Rectangle ir = new Rectangle(dx + Util.RoundI(10 * UiScale), body.Y,
                body.Width - (dx - body.X) - Util.RoundI(16 * UiScale), body.Height);
            Theme.TextMiddle(g, info, Theme.Font(10.5f, FontStyle.Regular, UiScale), Theme.TextSub, ir, StringAlignment.Near);
        }

        void PaintExpanded(Graphics g, Rectangle body)
        {
            WeatherNow wx = V2Shell.Weather != null ? V2Shell.Weather.Now : new WeatherNow();
            StatsNow st = V2Shell.Stats;

            Rectangle tr = new Rectangle(body.X + Util.RoundI(20 * UiScale), body.Y + Util.RoundI(8 * UiScale),
                Util.RoundI(118 * UiScale), Util.RoundI(36 * UiScale));
            Theme.TextMiddle(g, DateTime.Now.ToString("HH:mm"), Theme.Font(26f, FontStyle.Bold, UiScale), Theme.TextMain, tr, StringAlignment.Near);
            Rectangle dr = new Rectangle(body.X + Util.RoundI(22 * UiScale), body.Y + Util.RoundI(46 * UiScale),
                body.Width - Util.RoundI(44 * UiScale), Util.RoundI(16 * UiScale));
            Theme.TextMiddle(g, DateLine(), Theme.Font(10.5f, FontStyle.Regular, UiScale), Theme.TextSub, dr, StringAlignment.Near);

            int wxX = body.Right - Util.RoundI(138 * UiScale);
            if (wx.Has)
            {
                WeatherService.Glyph(g, wx.Code, new Rectangle(wxX, body.Y + Util.RoundI(14 * UiScale),
                    Util.RoundI(27 * UiScale), Util.RoundI(27 * UiScale)), Theme.Accent);
                Rectangle tempr = new Rectangle(wxX + Util.RoundI(31 * UiScale), body.Y + Util.RoundI(9 * UiScale),
                    Util.RoundI(88 * UiScale), Util.RoundI(34 * UiScale));
                Theme.TextMiddle(g, wx.Temp + "°", Theme.Font(20f, FontStyle.Bold, UiScale), Theme.TextMain, tempr, StringAlignment.Near);
                Rectangle cond = new Rectangle(wxX + Util.RoundI(32 * UiScale), body.Y + Util.RoundI(41 * UiScale),
                    Util.RoundI(102 * UiScale), Util.RoundI(14 * UiScale));
                Theme.TextMiddle(g, wx.Cond + " · " + wx.Humidity + "% · " + wx.Wind.ToString("0.0") + "m/s",
                    Theme.Font(9.5f, FontStyle.Regular, UiScale), Theme.TextSub, cond, StringAlignment.Near);
            }
            else
            {
                Rectangle nowr = new Rectangle(wxX, body.Y + Util.RoundI(14 * UiScale), Util.RoundI(120 * UiScale), Util.RoundI(28 * UiScale));
                Theme.TextMiddle(g, "天气离线", Theme.Font(10f, FontStyle.Regular, UiScale), Theme.TextSub, nowr, StringAlignment.Near);
            }

            using (Pen pen = new Pen(Color.FromArgb(38, Theme.TextSub), 1f))
                g.DrawLine(pen, body.X + Util.RoundI(20 * UiScale), body.Y + Util.RoundI(62 * UiScale),
                    body.Right - Util.RoundI(20 * UiScale), body.Y + Util.RoundI(62 * UiScale));

            int y = body.Bottom - Util.RoundI(40 * UiScale);
            if (V2Shell.Config == null || V2Shell.Config.IslandStats)
            {
                Stat(g, body.X + Util.RoundI(22 * UiScale), y, "CPU", st.CpuText, st.Cpu / 100f);
                Stat(g, body.X + Util.RoundI(101 * UiScale), y, "内存", st.MemPct + "%", st.MemPct / 100f);
                Stat(g, body.X + Util.RoundI(180 * UiScale), y, "磁盘", st.DiskPct + "%", st.DiskPct / 100f);
            }

            Rectangle[] rs = QuickButtons();
            Glyph[] gl = { Glyph.Plus, Glyph.Broom, Glyph.Gear };
            float bs = BounceScale();
            for (int i = 0; i < 3; i++)
            {
                bool pressed = pressedBtn == i;
                float sc = pressed ? 0.94f : (bounceBtn == i ? bs : 1f);
                PaintIconButton(g, rs[i], gl[i], hoverBtn == i, pressed, sc);
            }
        }

        void Stat(Graphics g, int x, int y, string label, string text, float pct)
        {
            Rectangle lr = new Rectangle(x, y, Util.RoundI(74 * UiScale), Util.RoundI(14 * UiScale));
            Theme.TextMiddle(g, label + "  " + text, Theme.Font(10f, FontStyle.Regular, UiScale), Theme.TextSub, lr, StringAlignment.Near);
            Rectangle bar = new Rectangle(x, y + Util.RoundI(17 * UiScale), Util.RoundI(68 * UiScale), Math.Max(2, Util.RoundI(3 * UiScale)));
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
            return d.Month + "月" + d.Day + "日 星期" + wd[(int)d.DayOfWeek];
        }

        // ---------- 交互 ----------
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            if (!CurrentBody().Contains(e.Location)) return;
            pressedBody = true;
            pressedBtn = -1;
            if (expanded)
            {
                Rectangle[] rs = QuickButtons();
                for (int i = 0; i < rs.Length; i++)
                {
                    if (!rs[i].Contains(e.Location)) continue;
                    pressedBtn = i;
                    break;
                }
            }
            Render();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;
            int pb = pressedBtn;
            bool bodyPressed = pressedBody;
            pressedBtn = -1;
            pressedBody = false;
            if (!bodyPressed && pb < 0) return;
            if (pb >= 0 && expanded)
            {
                Rectangle[] rs = QuickButtons();
                if (pb < rs.Length && rs[pb].Contains(e.Location))
                {
                    Pulse(pb);
                    if (pb == 0) V2Shell.Toast("应用面板将在 M4 提供");
                    else if (pb == 1) V2Shell.Toast("桌面图标管理将在 M5 提供");
                    else V2Shell.ShowMenu(PointToScreen(e.Location));
                    return;
                }
                Render();
                return;
            }
            if (CurrentBody().Contains(e.Location))
            {
                Pulse(-1);
                V2Shell.ShowMenu(PointToScreen(e.Location));
            }
            else Render();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hoverBtn = -1;
            if (!expanded) leaveAt = 0;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (clock != null) clock.Stop();
            if (hover != null) hover.Stop();
            if (animTimer != null) animTimer.Stop();
            base.OnFormClosed(e);
        }
    }
}
