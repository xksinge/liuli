// Liuli v2 · 玻璃面板：可拖动/缩放/置顶，内容由 PanelContent 独立绘制。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Liuli
{
    sealed class PanelForm : V2Window
    {
        public PanelConfig Cfg;
        Timer tick;
        bool dragging;
        bool resizing;
        bool rightDown;
        Point downCursor;
        Rectangle downBody;
        int margin;
        const int HTCLIENT = 1;
        const int HTTRANSPARENT = -1;

        public PanelForm(PanelConfig cfg)
        {
            Cfg = cfg;
            TopMost = cfg == null || cfg.TopMostPanel;
            ShowInTaskbar = false;
            tick = new Timer();
            tick.Interval = 5000;
            tick.Tick += delegate { Render(); };
            ApplyTickInterval();
        }

        protected override void OnReady()
        {
            ApplyBounds();
            Render();
            tick.Start();
        }

        public Rectangle Body()
        {
            int m = margin > 0 ? margin : Util.RoundI(12 * UiScale);
            return new Rectangle(m, m, Math.Max(1, ClientSize.Width - m * 2), Math.Max(1, ClientSize.Height - m * 2));
        }

        public void ApplyBounds()
        {
            if (!IsHandleCreated || Cfg == null) return;
            margin = Util.RoundI(12 * UiScale);
            float ts = Theme.TextScale;
            Cfg.W = Util.Clamp(Cfg.W, 220, 720);
            Cfg.H = Util.Clamp(Cfg.H, 120, 520);
            int w = Util.RoundI(Cfg.W * ts);
            int h = Util.RoundI(Cfg.H * ts);
            Screen target = Screen.FromPoint(new Point(Cfg.X, Cfg.Y));
            Rectangle work = target.WorkingArea;
            if (w > work.Width - 40) { w = Math.Max(220, work.Width - 40); Cfg.W = Util.RoundI(w / ts); }
            if (h > work.Height - 60) { h = Math.Max(120, work.Height - 60); Cfg.H = Util.RoundI(h / ts); }
            int x = Cfg.X;
            int y = Cfg.Y;
            if (x < work.Left) x = work.Left;
            if (y < work.Top) y = work.Top;
            if (x + w > work.Right) x = work.Right - w;
            if (y + h > work.Bottom) y = work.Bottom - h;
            ClientSize = new Size(w + margin * 2, h + margin * 2);
            Location = new Point(x - margin, y - margin);
            EnsureCanvas();
            Render();
        }

        public void ApplyTextScale()
        {
            UiScale = DpiUiScale();
            ApplyBounds();
        }

        public void ApplyConfig()
        {
            if (Cfg == null) return;
            ApplyTickInterval();
            TopMost = Cfg.TopMostPanel;
            ApplyBounds();
        }

        public void ApplyTickInterval()
        {
            if (Cfg == null || tick == null) return;
            int interval = 5000;
            string mode = PanelConfig.NormalizeContent(Cfg.Content);
            if (mode == "stats") interval = 2000;
            else if (mode == "clock" && V2Shell.Config != null && V2Shell.Config.ClockSeconds) interval = 1000;
            if (tick.Interval != interval) tick.Interval = interval;
        }

        public void SaveBounds()
        {
            if (Cfg == null) return;
            V2Shell.SavePanels();
        }

        public void Render()
        {
            if (!IsHandleCreated || Cfg == null) return;
            EnsureCanvas();
            using (Graphics g = Graphics.FromImage(Canvas))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                Rectangle body = Body();
                PaintGlass(g, body, Math.Max(12, Theme.Radius));
                PanelContent.Paint(g, body, Cfg.Content, Cfg.TopMostPanel, UiScale);
            }
            Native.PushLayered(Handle, Canvas, Location, (byte)255);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (Cfg == null) return;
            if (!Body().Contains(e.Location)) return;
            if (e.Button == MouseButtons.Right)
            {
                rightDown = true;
                return;
            }
            if (e.Button != MouseButtons.Left) return;
            Rectangle body = Body();
            resizing = e.X >= body.Right - Util.RoundI(22 * UiScale) && e.Y >= body.Bottom - Util.RoundI(22 * UiScale);
            dragging = !resizing;
            downCursor = Cursor.Position;
            downBody = new Rectangle(Cfg.X, Cfg.Y, Cfg.W, Cfg.H);
            Capture = true;
            Cursor = resizing ? Cursors.SizeNWSE : Cursors.SizeAll;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (Cfg == null) return;
            if (!dragging && !resizing)
            {
                Rectangle body = Body();
                Cursor = body.Contains(e.Location) &&
                    e.X >= body.Right - Util.RoundI(22 * UiScale) && e.Y >= body.Bottom - Util.RoundI(22 * UiScale)
                    ? Cursors.SizeNWSE : (body.Contains(e.Location) ? Cursors.SizeAll : Cursors.Default);
                return;
            }

            Point now = Cursor.Position;
            int dx = now.X - downCursor.X;
            int dy = now.Y - downCursor.Y;
            Rectangle work = Screen.FromPoint(now).WorkingArea;
            if (dragging)
            {
                int x = downBody.X + dx;
                int y = downBody.Y + dy;
                if (x < work.Left) x = work.Left;
                if (y < work.Top) y = work.Top;
                if (x > work.Right - 80) x = work.Right - 80;
                if (y > work.Bottom - 60) y = work.Bottom - 60;
                Cfg.X = x;
                Cfg.Y = y;
                Location = new Point(x - margin, y - margin);
                return;
            }

            float ts = Theme.TextScale;
            int w = downBody.Width + dx;
            int h = downBody.Height + dy;
            w = Util.Clamp(w, Util.RoundI(220 * ts), Util.RoundI(720 * ts));
            h = Util.Clamp(h, Util.RoundI(120 * ts), Util.RoundI(520 * ts));
            if (Cfg.X + w > work.Right) w = Math.Max(Util.RoundI(220 * ts), work.Right - Cfg.X);
            if (Cfg.Y + h > work.Bottom) h = Math.Max(Util.RoundI(120 * ts), work.Bottom - Cfg.Y);
            Cfg.W = Util.RoundI(w / ts);
            Cfg.H = Util.RoundI(h / ts);
            ClientSize = new Size(w + margin * 2, h + margin * 2);
            EnsureCanvas();
            Render();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (Cfg == null) return;
            if (e.Button == MouseButtons.Right)
            {
                if (rightDown && Body().Contains(e.Location)) ShowMenu(PointToScreen(e.Location));
                rightDown = false;
                return;
            }
            if (e.Button != MouseButtons.Left) return;
            bool changed = dragging || resizing;
            dragging = false;
            resizing = false;
            Capture = false;
            Cursor = Cursors.Default;
            if (changed) SaveBounds();
        }

        void ShowMenu(Point screen)
        {
            List<MenuForm.Item> items = new List<MenuForm.Item>();
            string[] modes = { "empty", "clock", "weather", "stats" };
            for (int i = 0; i < modes.Length; i++)
            {
                string mode = modes[i];
                string label = PanelConfig.ContentName(mode);
                if (string.Equals(Cfg.Content, mode, StringComparison.OrdinalIgnoreCase)) label = label + "（当前）";
                items.Add(new MenuForm.Item("内容：" + label, delegate { SetContent(mode); }, Glyph.Dot));
            }
            if (PanelConfig.NormalizeContent(Cfg.Content) == "clock")
            {
                bool use24 = V2Shell.Config == null || V2Shell.Config.Clock24;
                bool seconds = V2Shell.Config != null && V2Shell.Config.ClockSeconds;
                items.Add(new MenuForm.Item("时间制：" + (use24 ? "24 小时" : "12 小时"), delegate { V2Shell.ToggleClock24(); }, Glyph.Clock));
                items.Add(new MenuForm.Item("显示秒：" + (seconds ? "开" : "关"), delegate { V2Shell.ToggleClockSeconds(); }, Glyph.Clock));
            }
            items.Add(new MenuForm.Item(Cfg.TopMostPanel ? "置底显示" : "置顶显示", delegate { ToggleTop(); }, Glyph.Pin));
            items.Add(new MenuForm.Item("新建面板", delegate { V2Shell.AddPanel(); }, Glyph.Plus));
            items.Add(new MenuForm.Item("删除此面板", delegate { V2Shell.RemovePanel(this); }, Glyph.Close, true));
            new MenuForm(items, new Size(Util.RoundI(210 * Theme.S), Util.RoundI(8 * Theme.S))).ShowAt(screen);
        }

        void SetContent(string mode)
        {
            if (Cfg == null) return;
            Cfg.Content = PanelConfig.NormalizeContent(mode);
            ApplyTickInterval();
            SaveBounds();
            Render();
            V2Shell.Toast("面板内容：" + PanelConfig.ContentName(Cfg.Content));
        }

        void ToggleTop()
        {
            if (Cfg == null) return;
            Cfg.TopMostPanel = !Cfg.TopMostPanel;
            TopMost = Cfg.TopMostPanel;
            SaveBounds();
            Render();
            V2Shell.Toast(Cfg.TopMostPanel ? "面板已置顶" : "面板已置底");
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0084 && Cfg != null)
            {
                int lp = m.LParam.ToInt32();
                int sx = (short)(lp & 0xffff);
                int sy = (short)((lp >> 16) & 0xffff);
                Point p = PointToClient(new Point(sx, sy));
                m.Result = new IntPtr(Body().Contains(p) ? HTCLIENT : HTTRANSPARENT);
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (tick != null) { tick.Stop(); tick.Dispose(); tick = null; }
            base.OnFormClosed(e);
        }
    }
}
