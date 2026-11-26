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
        DropTarget oleDrop;
        int hoverItem = -1;
        int hoverBtn = -1;
        bool down;
        Point downPoint;

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
            int pad = Util.RoundI(14 * UiScale);
            int icon = Util.RoundI(40 * UiScale);
            int gap = Util.RoundI(4 * UiScale);
            int sep = Items().Count > 0 ? Util.RoundI(10 * UiScale) : 0;
            int btn = Util.RoundI(34 * UiScale);
            int bgap = Util.RoundI(4 * UiScale);
            int bodyH = Util.RoundI(56 * UiScale);
            int margin = Util.RoundI(6 * UiScale);
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
            EnsureCanvas();
            ApplyPosition();
            Render();
        }

        public void ApplyPosition()
        {
            if (!IsHandleCreated) return;
            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            int gap = Util.RoundI(8 * UiScale);
            int bottom = work.Bottom - gap;
            if (Taskbar.Visible())
            {
                int tb = Taskbar.VisibleHeight();
                if (tb > 0) bottom = Math.Min(bottom, work.Bottom - tb - gap);
            }
            int x = work.X + (work.Width - Width) / 2;
            int y = bottom - Height;
            if (y < work.Top + Util.RoundI(40 * UiScale)) y = work.Top + Util.RoundI(40 * UiScale);
            Location = new Point(x, y);
            Native.KeepAtTop(Handle);
        }

        public void RelayoutPublic() { Relayout(); }

        public void TickPositionPublic() { TickPosition(); }

        void TickPosition()
        {
            if (!IsHandleCreated) return;
            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            int gap = Util.RoundI(8 * UiScale);
            int bottom = work.Bottom - gap;
            if (Taskbar.Visible())
            {
                int tb = Taskbar.VisibleHeight();
                if (tb > 0) bottom = Math.Min(bottom, work.Bottom - tb - gap);
            }
            Point target = new Point(work.X + (work.Width - Width) / 2, bottom - Height);
            if (target != Location)
            {
                Location = target;
                Native.KeepAtTop(Handle);
                Push();
            }
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
                    Rectangle r = itemRects[i];
                    if (hoverItem == i)
                    {
                        Rectangle bg = Util.Inflate(r, Util.RoundI(3 * UiScale));
                        using (GraphicsPath p = Theme.Round(bg, Util.RoundI(10 * UiScale)))
                        using (SolidBrush b = new SolidBrush(Theme.Hover)) g.FillPath(b, p);
                    }
                    DockGlyphs.Draw(g, items[i], r, UiScale);
                }

                if (items.Count > 0 && itemRects.Count > 0)
                {
                    int sx = itemRects[itemRects.Count - 1].Right + Util.RoundI(8 * UiScale);
                    using (Pen pen = new Pen(Color.FromArgb(44, 255, 255, 255), 1f * UiScale))
                        g.DrawLine(pen, sx, body.Y + Util.RoundI(12 * UiScale), sx, body.Bottom - Util.RoundI(12 * UiScale));
                }

                Glyph[] glyphs = { Glyph.Plus, Glyph.Broom, Glyph.Gear };
                for (int i = 0; i < 3; i++) PaintIconButton(g, btnRects[i], glyphs[i], hoverBtn == i);

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
            if (e.Button == MouseButtons.Left) { down = true; downPoint = e.Location; }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!down || e.Button != MouseButtons.Left) return;
            down = false;
            if (Math.Abs(e.X - downPoint.X) > 4 || Math.Abs(e.Y - downPoint.Y) > 4) return;

            List<DockItem> items = Items();
            for (int i = 0; i < itemRects.Count && i < items.Count; i++)
            {
                if (!itemRects[i].Contains(e.Location)) continue;
                items[i].Launch();
                return;
            }
            for (int i = 0; i < btnRects.Length; i++)
            {
                if (!btnRects[i].Contains(e.Location)) continue;
                if (i == 0) V2Shell.Toast("应用面板将在 M4 提供");
                else if (i == 1) V2Shell.Toast("桌面图标管理将在 M5 提供");
                else V2Shell.ShowMenu(PointToScreen(e.Location));
                return;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (positionTimer != null) positionTimer.Stop();
            try { if (oleDrop != null) DropTarget.RevokeDragDrop(Handle); } catch { }
            base.OnFormClosed(e);
        }
    }
}
