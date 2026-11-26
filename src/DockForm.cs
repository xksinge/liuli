// Liuli v2 · 底部 Dock：默认系统入口 + Windows 自带应用，支持拖入固定、右键移除。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace Liuli
{
    sealed class DockForm : V2Window
    {
        Timer positionTimer;
        Timer animTimer;
        Timer autoTimer;
        Timer slideTimer;
        DropTarget oleDrop;
        int hoverItem = -1;
        int hoverBtn = -1;
        int pressedItem = -1;
        int pressedBtn = -1;
        int bounceItem = -1;
        int bounceBtn = -1;
        int flashItem = -1;
        long bounceAt;
        long flashAt;
        bool down;
        Point downPoint;

        bool autoRevealed = true;
        long awayAt;
        int slide;
        int slideFrom;
        int slideTo;
        long slideAt;
        const int SlideMs = 170;

        Rectangle body = Rectangle.Empty;
        List<Rectangle> itemRects = new List<Rectangle>();
        Rectangle[] btnRects = new Rectangle[3];

        public DockForm()
        {
            Relayout();
            positionTimer = new Timer();
            positionTimer.Interval = 320;
            positionTimer.Tick += delegate { TickPosition(); };
            positionTimer.Start();

            animTimer = new Timer();
            animTimer.Interval = 16;
            animTimer.Tick += delegate { TickAnim(); };

            autoTimer = new Timer();
            autoTimer.Interval = 55;
            autoTimer.Tick += delegate { TickAutoHide(); };
            autoTimer.Start();

            slideTimer = new Timer();
            slideTimer.Interval = 16;
            slideTimer.Tick += delegate { TickSlide(); };
        }

        protected override void OnReady()
        {
            try
            {
                DropTarget.EnsureOle();
                oleDrop = new DropTarget("Dock", OnDrop, DropTarget.COPY);
                DropTarget.RegisterDragDrop(Handle, oleDrop);
            }
            catch (Exception ex) { Log.Warn("dock", "注册拖放失败 " + ex.Message); }
            Relayout();
        }

        List<DockItem> Items()
        {
            if (V2Shell.Config == null) return new List<DockItem>();
            return V2Shell.Config.DockItems;
        }

        public void Relayout()
        {
            if (!IsHandleCreated) return;
            UiScale = DpiUiScale();
            int pad = Util.RoundI(16 * UiScale);
            int icon = Util.RoundI(44 * UiScale);
            int gap = Util.RoundI(5 * UiScale);
            int sep = Items().Count > 0 ? Util.RoundI(10 * UiScale) : 0;
            int btn = Util.RoundI(36 * UiScale);
            int bgap = Util.RoundI(5 * UiScale);
            int bodyH = Util.RoundI(60 * UiScale);
            int margin = Util.RoundI(12 * UiScale);
            int tagH = Util.RoundI(16 * UiScale);
            int n = Items().Count;
            int bodyW = pad + n * (icon + gap) + sep + btn * 3 + bgap * 2 + pad;
            if (bodyW < Util.RoundI(220 * UiScale)) bodyW = Util.RoundI(220 * UiScale);

            itemRects.Clear();
            int x = margin + pad;
            int y = margin + tagH + (bodyH - icon) / 2;
            for (int i = 0; i < n; i++)
            {
                itemRects.Add(new Rectangle(x, y, icon, icon));
                x += icon + gap;
            }

            int right = margin + bodyW - pad;
            int by = margin + tagH + (bodyH - btn) / 2;
            btnRects[0] = new Rectangle(right - btn * 3 - bgap * 2, by, btn, btn);
            btnRects[1] = new Rectangle(right - btn * 2 - bgap, by, btn, btn);
            btnRects[2] = new Rectangle(right - btn, by, btn, btn);

            body = new Rectangle(margin, margin + tagH, bodyW, bodyH);
            ClientSize = new Size(bodyW + margin * 2, bodyH + tagH + margin * 2);
            if (!autoRevealed) slide = HiddenOffset();
            EnsureCanvas();
            ApplyPosition();
            Render();
        }

        int BaseTopY()
        {
            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            int gap = Util.RoundI(8 * UiScale);
            int bodyBottom = work.Bottom - gap;
            if (Taskbar.Visible())
            {
                int tb = Taskbar.VisibleHeight();
                if (tb > 0) bodyBottom = Math.Min(bodyBottom, work.Bottom - tb - gap);
            }
            int y = bodyBottom - body.Bottom;
            if (y < work.Top + Util.RoundI(40 * UiScale)) y = work.Top + Util.RoundI(40 * UiScale);
            return y;
        }

        int HiddenOffset()
        {
            // 以屏幕底边为准，保证任务栏露出时也能把整个窗口推出去。
            int baseY = BaseTopY();
            int toBottom = Screen.PrimaryScreen.Bounds.Bottom + 2 - baseY;
            return Math.Max(1, Math.Max(Height, toBottom));
        }

        public void ApplyPosition()
        {
            if (!IsHandleCreated) return;
            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            int x = work.X + (work.Width - Width) / 2;
            Location = new Point(x, BaseTopY() + slide);
            Native.KeepAtTop(Handle);
        }

        public void RelayoutPublic() { Relayout(); }

        public void TickPositionPublic() { TickPosition(); }

        void TickPosition()
        {
            if (!IsHandleCreated) return;
            if (!autoRevealed && (slideTimer == null || !slideTimer.Enabled)) slide = HiddenOffset();
            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            Point target = new Point(work.X + (work.Width - Width) / 2, BaseTopY() + slide);
            if (target != Location)
            {
                Location = target;
                Native.KeepAtTop(Handle);
                Push();
            }
        }

        // ---------- 自动隐藏 ----------
        bool AutoHideEnabled()
        {
            return V2Shell.Config != null && V2Shell.Config.DockAutoHide;
        }

        public void RefreshAutoHide()
        {
            if (!IsHandleCreated) return;
            awayAt = 0;
            if (!AutoHideEnabled()) { SlideTo(true); return; }
            // 切到自动隐藏时先显示，鼠标不在底部则 650ms 后收起。
            if (!autoRevealed) SlideTo(true);
        }

        void TickAutoHide()
        {
            if (!IsHandleCreated || !Visible) return;
            if (!AutoHideEnabled())
            {
                if (!autoRevealed) SlideTo(true);
                return;
            }

            Point p = Cursor.Position;
            Rectangle screen = Screen.PrimaryScreen.Bounds;
            int hot = Math.Max(3, Util.RoundI(6 * UiScale));
            bool nearBottom = p.Y >= screen.Bottom - hot;
            bool inDock = Bounds.Contains(p);
            if (nearBottom || inDock)
            {
                awayAt = 0;
                if (!autoRevealed) SlideTo(true);
                return;
            }

            if (!autoRevealed) return;
            long now = Environment.TickCount;
            if (awayAt == 0) awayAt = now;
            else if (now - awayAt > 650) SlideTo(false);
        }

        void SlideTo(bool visible)
        {
            autoRevealed = visible;
            int target = visible ? 0 : HiddenOffset();
            if (slide == target)
            {
                if (slideTimer != null) slideTimer.Stop();
                return;
            }
            slideFrom = slide;
            slideTo = target;
            slideAt = Environment.TickCount;
            if (slideTimer != null && !slideTimer.Enabled) slideTimer.Start();
        }

        void TickSlide()
        {
            float t = (Environment.TickCount - slideAt) / (float)SlideMs;
            if (t < 0f) t = 0f;
            if (t >= 1f)
            {
                slide = slideTo;
                if (slideTimer != null) slideTimer.Stop();
            }
            else
            {
                float ease = 1f - (1f - t) * (1f - t) * (1f - t);
                slide = slideFrom + Util.RoundI((slideTo - slideFrom) * ease);
            }
            ApplyPosition();
            Push();
        }

        public void PreviewReveal()
        {
            if (autoTimer != null) autoTimer.Stop();
            if (slideTimer != null) slideTimer.Stop();
            autoRevealed = true;
            slide = 0;
            ApplyPosition();
        }

        public void PreviewHide()
        {
            if (autoTimer != null) autoTimer.Stop();
            if (slideTimer != null) slideTimer.Stop();
            autoRevealed = false;
            slide = HiddenOffset();
            ApplyPosition();
        }

        void OnDrop(string[] files)
        {
            if (files == null || files.Length == 0) return;
            int added = V2Shell.PinPaths(files);
            if (added > 0)
            {
                V2Shell.Toast("已固定 " + added + " 项到 Dock");
                Relayout();
            }
            else V2Shell.Toast("这些项目已经在 Dock 上");
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

                PaintGlass(g, body, body.Height / 2);

                List<DockItem> items = Items();
                for (int i = 0; i < itemRects.Count && i < items.Count; i++)
                {
                    bool pressed = pressedItem == i;
                    bool bouncing = bounceItem == i;
                    float sc = pressed ? 0.94f : (bouncing ? BounceScale() : 1f);
                    Rectangle baseRect = itemRects[i];
                    Rectangle r = ScaleRect(baseRect, sc);
                    if (hoverItem == i || pressed)
                    {
                        Rectangle bg = Util.Inflate(baseRect, Util.RoundI(3 * UiScale));
                        using (GraphicsPath p = Theme.Round(bg, Util.RoundI(10 * UiScale)))
                        using (SolidBrush b = new SolidBrush(pressed ? Theme.Press : Theme.Hover)) g.FillPath(b, p);
                    }
                    DockGlyphs.Draw(g, items[i], r, UiScale);

                    if (flashItem == i && flashAt != 0)
                    {
                        float t = (Environment.TickCount - flashAt) / 280f;
                        if (t >= 0f && t < 1f)
                        {
                            int alpha = (int)(120 * (1f - t));
                            int radius = baseRect.Width / 2 + Util.RoundI(baseRect.Width * 0.34f * t);
                            Rectangle ring = new Rectangle(baseRect.X + baseRect.Width / 2 - radius, baseRect.Y + baseRect.Height / 2 - radius,
                                radius * 2, radius * 2);
                            using (Pen pen = new Pen(Color.FromArgb(alpha, Theme.Accent), 1.4f * UiScale))
                                g.DrawEllipse(pen, ring);
                        }
                    }
                }

                if (items.Count > 0 && itemRects.Count > 0)
                {
                    int sx = itemRects[itemRects.Count - 1].Right + Util.RoundI(8 * UiScale);
                    using (Pen pen = new Pen(Color.FromArgb(44, 255, 255, 255), 1f * UiScale))
                        g.DrawLine(pen, sx, body.Y + Util.RoundI(12 * UiScale), sx, body.Bottom - Util.RoundI(12 * UiScale));
                }

                Glyph[] glyphs = { Glyph.Plus, Glyph.Broom, Glyph.Gear };
                float buttonScale = BounceScale();
                for (int i = 0; i < 3; i++)
                {
                    bool pressed = pressedBtn == i;
                    float sc = pressed ? 0.94f : (bounceBtn == i ? buttonScale : 1f);
                    PaintIconButton(g, btnRects[i], glyphs[i], hoverBtn == i, pressed, sc);
                }

                if (hoverItem >= 0 && hoverItem < items.Count)
                {
                    PaintTag(g, items[hoverItem].DisplayName, itemRects[hoverItem].X + itemRects[hoverItem].Width / 2,
                        Util.RoundI(1 * UiScale), Util.RoundI(220 * UiScale));
                }
                else if (hoverBtn >= 0)
                {
                    string[] tips = { "应用面板（M4）", "桌面图标（M5）", "设置" };
                    PaintTag(g, tips[hoverBtn], btnRects[hoverBtn].X + btnRects[hoverBtn].Width / 2,
                        Util.RoundI(1 * UiScale), Util.RoundI(180 * UiScale));
                }

                Native.PushLayered(Handle, Canvas, Location, (byte)255);
            }
            finally { g.Dispose(); }
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

        void StartAnim()
        {
            if (animTimer != null && !animTimer.Enabled) animTimer.Start();
        }

        void TickAnim()
        {
            long now = Environment.TickCount;
            if (bounceAt != 0 && now - bounceAt > 280)
            {
                bounceItem = -1;
                bounceBtn = -1;
                flashItem = -1;
                bounceAt = 0;
                if (animTimer != null) animTimer.Stop();
                Render();
                return;
            }
            Render();
        }

        public void PreviewPress(int index)
        {
            pressedItem = index;
            Render();
        }

        // ---------- 交互 ----------
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int hi = -1, hb = -1;
            for (int i = 0; i < itemRects.Count; i++) if (Util.Inflate(itemRects[i], Util.RoundI(3 * UiScale)).Contains(e.Location)) hi = i;
            for (int i = 0; i < btnRects.Length; i++) if (btnRects[i].Contains(e.Location)) hb = i;
            if (hi != hoverItem || hb != hoverBtn)
            {
                hoverItem = hi;
                hoverBtn = hb;
                Render();
            }
            Cursor = (hi >= 0 || hb >= 0) ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (hoverItem >= 0 || hoverBtn >= 0)
            {
                hoverItem = -1;
                hoverBtn = -1;
                Render();
                Cursor = Cursors.Default;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Right)
            {
                List<DockItem> items = Items();
                for (int i = 0; i < itemRects.Count && i < items.Count; i++)
                {
                    if (!itemRects[i].Contains(e.Location)) continue;
                    int idx = i;
                    List<MenuForm.Item> menu = new List<MenuForm.Item>();
                    menu.Add(new MenuForm.Item("从 Dock 移除", delegate { V2Shell.RemoveDockItem(idx); Relayout(); }, Glyph.Close, true));
                    new MenuForm(menu, new Size(Util.RoundI(160 * UiScale), Util.RoundI(8 * UiScale))).ShowAt(PointToScreen(e.Location));
                    return;
                }
            }
            if (e.Button == MouseButtons.Left)
            {
                down = true;
                downPoint = e.Location;
                pressedItem = -1;
                pressedBtn = -1;
                for (int i = 0; i < itemRects.Count; i++)
                    if (Util.Inflate(itemRects[i], Util.RoundI(3 * UiScale)).Contains(e.Location)) { pressedItem = i; break; }
                for (int i = 0; i < btnRects.Length; i++)
                    if (btnRects[i].Contains(e.Location)) { pressedBtn = i; break; }
                if (pressedItem >= 0 || pressedBtn >= 0) Render();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!down || e.Button != MouseButtons.Left) return;
            down = false;
            int pi = pressedItem;
            int pb = pressedBtn;
            pressedItem = -1;
            pressedBtn = -1;
            if (Math.Abs(e.X - downPoint.X) > 4 || Math.Abs(e.Y - downPoint.Y) > 4)
            {
                Render();
                return;
            }

            List<DockItem> items = Items();
            if (pi >= 0 && pi < itemRects.Count && pi < items.Count && itemRects[pi].Contains(e.Location))
            {
                Pulse(pi, -1);
                items[pi].Launch();
                return;
            }
            if (pb >= 0 && pb < btnRects.Length && btnRects[pb].Contains(e.Location))
            {
                Pulse(-1, pb);
                if (pb == 0) V2Shell.Toast("应用面板将在 M4 提供");
                else if (pb == 1) V2Shell.Toast("桌面图标管理将在 M5 提供");
                else V2Shell.ShowMenu(PointToScreen(e.Location));
                return;
            }
            Render();
        }

        void Pulse(int item, int button)
        {
            bounceItem = item;
            bounceBtn = button;
            flashItem = item;
            bounceAt = Environment.TickCount;
            flashAt = Environment.TickCount;
            StartAnim();
            Render();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (autoTimer == null) return;
            if (!Visible) { autoTimer.Stop(); if (slideTimer != null) slideTimer.Stop(); }
            else if (!autoTimer.Enabled) autoTimer.Start();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (positionTimer != null) positionTimer.Stop();
            if (animTimer != null) animTimer.Stop();
            if (autoTimer != null) autoTimer.Stop();
            if (slideTimer != null) slideTimer.Stop();
            try { if (oleDrop != null) DropTarget.RevokeDragDrop(Handle); } catch { }
            base.OnFormClosed(e);
        }
    }
}
