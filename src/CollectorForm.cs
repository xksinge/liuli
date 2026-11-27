// Liuli v2 · 桌面收纳面板：单面板 + 分组标签，入口只存路径，双击打开、拖动换组。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace Liuli
{
    sealed class CollectorForm : V2Window
    {
        Timer scanTimer;
        DropTarget dropTarget;
        bool windowDrag;
        bool resizing;
        bool tileDrag;
        Point dragOffset;
        Point downCursor;
        Rectangle downBody;
        int downTile = -1;
        int dragTile = -1;
        int hoverTile = -1;
        int hoverGroup = -1;
        int groupTarget = -1;
        int pressedTile = -1;
        int page;
        int margin;
        Rectangle body = Rectangle.Empty;
        Rectangle headerR;
        Rectangle footerR;
        Rectangle prevR;
        Rectangle nextR;
        Rectangle[] groupRects = new Rectangle[0];
        string[] groupNames = new string[0];
        List<Rectangle> tileRects = new List<Rectangle>();
        List<CollectorItem> tileItems = new List<CollectorItem>();
        const int HTCLIENT = 1;
        const int HTTRANSPARENT = -1;

        public CollectorForm()
        {
            ShowInTaskbar = false;
            TopMost = true;
        }

        protected override void OnReady()
        {
            try
            {
                DropTarget.EnsureOle();
                dropTarget = new DropTarget("Collector", OnDrop, DropTarget.COPY);
                DropTarget.RegisterDragDrop(Handle, dropTarget);
            }
            catch (Exception ex) { Log.Warn("collector", "注册拖放失败 " + ex.Message); }

            scanTimer = new Timer();
            scanTimer.Interval = 2000;
            scanTimer.Tick += delegate { TickScan(); };
            scanTimer.Start();
            ApplyBounds();
            CollectorState.Sync(true);
            Render();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (scanTimer != null) { scanTimer.Stop(); scanTimer.Dispose(); scanTimer = null; }
            try { if (dropTarget != null && IsHandleCreated) DropTarget.RevokeDragDrop(Handle); } catch { }
            base.OnFormClosed(e);
        }

        public Rectangle Body()
        {
            int m = margin > 0 ? margin : Util.RoundI(12 * UiScale);
            return new Rectangle(m, m, Math.Max(1, ClientSize.Width - m * 2), Math.Max(1, ClientSize.Height - m * 2));
        }

        public void ApplyBounds()
        {
            if (!IsHandleCreated || V2Shell.Config == null) return;
            margin = Util.RoundI(12 * UiScale);
            ShellConfig c = V2Shell.Config;
            c.CollectorW = Util.Clamp(c.CollectorW, 420, 900);
            c.CollectorH = Util.Clamp(c.CollectorH, 300, 720);
            int w = Util.RoundI(c.CollectorW * UiScale);
            int h = Util.RoundI(c.CollectorH * UiScale);
            Screen target = Screen.FromPoint(new Point(c.CollectorX, c.CollectorY));
            Rectangle work = target.WorkingArea;
            if (c.CollectorX < 0 || c.CollectorY < 0)
            {
                c.CollectorX = work.Right - w - Util.RoundI(40 * UiScale);
                c.CollectorY = work.Top + Util.RoundI(96 * UiScale);
            }
            if (c.CollectorX < work.Left) c.CollectorX = work.Left;
            if (c.CollectorY < work.Top) c.CollectorY = work.Top;
            if (c.CollectorX + w > work.Right) c.CollectorX = work.Right - w;
            if (c.CollectorY + h > work.Bottom) c.CollectorY = work.Bottom - h;
            ClientSize = new Size(w + margin * 2, h + margin * 2);
            Location = new Point(c.CollectorX - margin, c.CollectorY - margin);
            EnsureCanvas();
            Render();
        }

        /// <summary>从 Dock 打开时，把收纳面板居中贴到该屏 Dock 上方；不改保存坐标。</summary>
        public void AnchorToDock(Screen target)
        {
            if (!IsHandleCreated || target == null) return;
            UiScale = DpiUiScale();
            margin = Util.RoundI(12 * UiScale);
            ShellConfig c = V2Shell.Config;
            int w = Util.RoundI(c.CollectorW * UiScale) + margin * 2;
            int h = Util.RoundI(c.CollectorH * UiScale) + margin * 2;
            Rectangle b = target.Bounds;
            int left = b.Left + (b.Width - w) / 2;
            int top = b.Bottom - Util.RoundI(152 * UiScale) - h + margin;
            if (top < b.Top) top = b.Top;
            ClientSize = new Size(w, h);
            Location = new Point(left, top);
            EnsureCanvas();
        }

        public void ApplyTextScale()
        {
            UiScale = DpiUiScale();
            ApplyBounds();
        }

        public void ApplyConfig()
        {
            ApplyBounds();
        }

        void SaveBounds()
        {
            if (V2Shell.Config == null) return;
            int m = margin > 0 ? margin : Util.RoundI(12 * UiScale);
            V2Shell.Config.CollectorX = Location.X + m;
            V2Shell.Config.CollectorY = Location.Y + m;
            V2Shell.Config.CollectorW = Util.RoundI((ClientSize.Width - m * 2) / UiScale);
            V2Shell.Config.CollectorH = Util.RoundI((ClientSize.Height - m * 2) / UiScale);
            V2Shell.Config.Save();
        }

        void TickScan()
        {
            if (V2Shell.Config == null) return;
            if (!V2Shell.Config.CollectorAutoScan) return;
            bool changed = CollectorState.Sync(true);
            if (changed)
            {
                if (page > 0 && page >= PageCount()) page = Math.Max(0, PageCount() - 1);
                Render();
            }
            // Explorer 重启后原生图标可能重新出现；收纳开启时再压一次。
            if (V2Shell.Config.DesktopIconsHidden && Native.DesktopIconsVisible())
                Native.SetDesktopIconsVisible(false);
        }

        void OnDrop(string[] files)
        {
            int added = CollectorState.AddPaths(files);
            if (added > 0)
            {
                V2Shell.Config.CollectorGroup = CollectorState.AllGroup;
                V2Shell.Config.Save();
                page = 0;
                Render();
                V2Shell.Toast("已加入桌面收纳（只存路径，不移动文件）");
            }
        }

        int PageCount()
        {
            int total = VisibleItems().Count;
            int size = PageSize();
            return Math.Max(1, (total + size - 1) / size);
        }

        int PageSize()
        {
            Rectangle b = Body();
            int pad = Util.RoundI(16 * UiScale);
            int cols = Math.Max(1, (b.Width - pad * 2) / Util.RoundI(88 * UiScale));
            int rows = Math.Max(1, (b.Height - Util.RoundI(116 * UiScale)) / Util.RoundI(92 * UiScale));
            return Math.Max(1, cols * rows);
        }

        List<CollectorItem> VisibleItems()
        {
            List<CollectorItem> list = new List<CollectorItem>();
            if (V2Shell.Config == null) return list;
            string group = V2Shell.Config.CollectorGroup;
            for (int i = 0; i < V2Shell.Config.CollectorItems.Count; i++)
            {
                CollectorItem item = V2Shell.Config.CollectorItems[i];
                if (item == null || item.Hidden) continue;
                if (group == CollectorState.AllGroup || string.Equals(item.Group, group, StringComparison.OrdinalIgnoreCase))
                    list.Add(item);
            }
            return list;
        }

        void Relayout()
        {
            body = Body();
            if (body.Width <= 40 || body.Height <= 40) return;
            int pad = Util.RoundI(16 * UiScale);
            int headerH = Util.RoundI(48 * UiScale);
            int groupH = Util.RoundI(30 * UiScale);
            int footerH = Util.RoundI(26 * UiScale);
            headerR = new Rectangle(body.X + pad, body.Y + Util.RoundI(7 * UiScale),
                body.Width - pad * 2, Util.RoundI(26 * UiScale));

            groupNames = CollectorState.GroupNames();
            int n = groupNames.Length;
            int gap = Util.RoundI(4 * UiScale);
            int avail = body.Width - pad * 2;
            int gw = Math.Min(Util.RoundI(66 * UiScale), Math.Max(Util.RoundI(34 * UiScale), (avail - (n - 1) * gap) / n));
            int total = n * gw + (n - 1) * gap;
            int gx = body.X + (body.Width - total) / 2;
            int gy = body.Y + headerH;
            groupRects = new Rectangle[n];
            for (int i = 0; i < n; i++)
            {
                groupRects[i] = new Rectangle(gx + i * (gw + gap), gy, gw, groupH);
            }

            int tileTop = gy + groupH + Util.RoundI(8 * UiScale);
            int tileBottom = body.Bottom - footerH - Util.RoundI(4 * UiScale);
            int tileW = Util.RoundI(88 * UiScale);
            int tileH = Util.RoundI(92 * UiScale);
            int cols = Math.Max(1, avail / tileW);
            int rows = Math.Max(1, (tileBottom - tileTop) / tileH);
            int pageSize = Math.Max(1, cols * rows);

            List<CollectorItem> all = VisibleItems();
            int maxPage = Math.Max(0, (all.Count + pageSize - 1) / pageSize - 1);
            page = Util.Clamp(page, 0, maxPage);

            tileRects.Clear();
            tileItems.Clear();
            int start = page * pageSize;
            for (int i = 0; i < pageSize && start + i < all.Count; i++)
            {
                int col = i % cols;
                int row = i / cols;
                Rectangle r = new Rectangle(body.X + pad + col * tileW, tileTop + row * tileH, tileW, tileH);
                tileRects.Add(r);
                tileItems.Add(all[start + i]);
            }

            footerR = new Rectangle(body.X + pad, body.Bottom - footerH, body.Width - pad * 2, footerH);
            int btn = Util.RoundI(22 * UiScale);
            nextR = new Rectangle(footerR.Right - btn, footerR.Y, btn, btn);
            prevR = new Rectangle(nextR.X - btn * 2 - Util.RoundI(6 * UiScale), footerR.Y, btn, btn);
        }

        public void Render()
        {
            if (!IsHandleCreated || V2Shell.Config == null) return;
            EnsureCanvas();
            using (Graphics g = Graphics.FromImage(Canvas))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.Transparent);
                Relayout();
                Rectangle b = body;
                PaintGlass(g, b, Math.Max(12, Theme.Radius));

                int pad = Util.RoundI(16 * UiScale);
                Theme.Text(g, "桌面收纳", Theme.Font(12f, FontStyle.Bold, UiScale), Theme.TextMain,
                    new Rectangle(b.X + pad, b.Y + Util.RoundI(10 * UiScale), Util.RoundI(150 * UiScale), Util.RoundI(22 * UiScale)), StringAlignment.Near, false);
                Theme.Text(g, "只存路径 · 双击打开 · 拖动换组", Theme.Font(8.8f, FontStyle.Regular, UiScale), Theme.TextFaint,
                    new Rectangle(b.X + pad, b.Y + Util.RoundI(29 * UiScale), b.Width - pad * 2 - Util.RoundI(90 * UiScale), Util.RoundI(16 * UiScale)), StringAlignment.Near, true);
                Theme.Text(g, CollectorState.VisibleCount() + " 项", Theme.Font(9f, FontStyle.Regular, UiScale), Theme.TextSub,
                    new Rectangle(b.Right - pad - Util.RoundI(80 * UiScale), b.Y + Util.RoundI(13 * UiScale), Util.RoundI(80 * UiScale), Util.RoundI(18 * UiScale)), StringAlignment.Far, false);

                string current = V2Shell.Config.CollectorGroup;
                for (int i = 0; i < groupRects.Length; i++)
                {
                    bool selected = string.Equals(groupNames[i], current, StringComparison.OrdinalIgnoreCase);
                    Rectangle r = groupRects[i];
                    using (GraphicsPath p = Theme.Round(r, Math.Max(7, r.Height / 2)))
                    {
                        Color bg = selected ? Color.FromArgb(72, Theme.Accent) :
                            (hoverGroup == i ? Theme.Hover : (Theme.Light ? Color.FromArgb(20, 12, 18, 30) : Color.FromArgb(16, 255, 255, 255)));
                        using (SolidBrush br = new SolidBrush(bg)) g.FillPath(br, p);
                        if (selected)
                            using (Pen pen = new Pen(Color.FromArgb(130, Theme.Accent), 1f)) g.DrawPath(pen, p);
                    }
                    Theme.TextMiddle(g, groupNames[i], Theme.Font(8.3f, selected ? FontStyle.Bold : FontStyle.Regular, UiScale),
                        selected ? Theme.TextMain : Theme.TextSub, r, StringAlignment.Center);
                }

                if (tileRects.Count == 0)
                {
                    Theme.Glyph(g, Glyph.Folder, new Rectangle(b.X + b.Width / 2 - Util.RoundI(20 * UiScale),
                        b.Y + Util.RoundI(150 * UiScale), Util.RoundI(40 * UiScale), Util.RoundI(40 * UiScale)), Theme.TextFaint, 1.5f * UiScale);
                    Theme.TextMiddle(g, "这个分组还没有图标", Theme.Font(10.5f, FontStyle.Regular, UiScale), Theme.TextSub,
                        new Rectangle(b.X, b.Y + Util.RoundI(198 * UiScale), b.Width, Util.RoundI(22 * UiScale)), StringAlignment.Center);
                    Theme.TextMiddle(g, "桌面新增文件会自动出现；也可以把文件拖进来", Theme.Font(8.8f, FontStyle.Regular, UiScale), Theme.TextFaint,
                        new Rectangle(b.X, b.Y + Util.RoundI(222 * UiScale), b.Width, Util.RoundI(20 * UiScale)), StringAlignment.Center);
                }
                else
                {
                    for (int i = 0; i < tileRects.Count; i++)
                        PaintTile(g, tileRects[i], tileItems[i], i);
                }

                PaintFooter(g);
            }
            Native.PushLayered(Handle, Canvas, Location, (byte)255);
        }

        void PaintTile(Graphics g, Rectangle r, CollectorItem item, int index)
        {
            if (item == null) return;
            bool hot = hoverTile == index;
            bool pressed = pressedTile == index;
            bool target = tileDrag && groupTarget >= 0 && groupTarget == hoverGroup;
            Rectangle card = new Rectangle(r.X + Util.RoundI(4 * UiScale), r.Y + Util.RoundI(2 * UiScale),
                r.Width - Util.RoundI(8 * UiScale), r.Height - Util.RoundI(6 * UiScale));
            if (hot || pressed || target)
            {
                using (GraphicsPath p = Theme.Round(card, Util.RoundI(12 * UiScale)))
                using (SolidBrush br = new SolidBrush(pressed ? Theme.Press : (target ? Color.FromArgb(52, Theme.Accent) : Theme.Hover)))
                    g.FillPath(br, p);
            }
            int icon = Util.RoundI(50 * UiScale);
            Rectangle iconR = new Rectangle(r.X + (r.Width - icon) / 2, r.Y + Util.RoundI(7 * UiScale), icon, icon);
            Icons.Draw(g, item.Path, iconR, 64);

            string name = item.DisplayName;
            Font f = Theme.Font(8.4f, FontStyle.Regular, UiScale);
            List<string> lines = Util.Wrap(g, name, f, r.Width - Util.RoundI(10 * UiScale), 2);
            int ty = iconR.Bottom + Util.RoundI(3 * UiScale);
            for (int i = 0; i < lines.Count; i++)
            {
                Theme.TextMiddle(g, lines[i], f, Theme.TextMain,
                    new Rectangle(r.X + Util.RoundI(5 * UiScale), ty, r.Width - Util.RoundI(10 * UiScale), Util.RoundI(14 * UiScale)), StringAlignment.Center);
                ty += Util.RoundI(13 * UiScale);
            }
        }

        void PaintFooter(Graphics g)
        {
            int total = VisibleItems().Count;
            int pages = PageCount();
            Theme.Text(g, "共 " + total + " 项 · 第 " + (page + 1) + "/" + pages + " 页 · 滚轮翻页", Theme.Font(8.4f, FontStyle.Regular, UiScale),
                Theme.TextFaint, new Rectangle(footerR.X, footerR.Y + Util.RoundI(4 * UiScale), footerR.Width - Util.RoundI(70 * UiScale), Util.RoundI(18 * UiScale)), StringAlignment.Near, true);
            if (pages > 1)
            {
                PaintPageButton(g, prevR, "<", hoverTile == -10);
                PaintPageButton(g, nextR, ">", hoverTile == -11);
            }
        }

        void PaintPageButton(Graphics g, Rectangle r, string text, bool hot)
        {
            using (GraphicsPath p = Theme.Round(r, r.Height / 2))
            using (SolidBrush br = new SolidBrush(hot ? Theme.Hover : (Theme.Light ? Color.FromArgb(18, 12, 18, 30) : Color.FromArgb(14, 255, 255, 255))))
                g.FillPath(br, p);
            Theme.TextMiddle(g, text, Theme.Font(10f, FontStyle.Bold, UiScale), hot ? Theme.TextMain : Theme.TextSub, r, StringAlignment.Center);
        }

        int HitGroup(Point p)
        {
            for (int i = 0; i < groupRects.Length; i++)
                if (groupRects[i].Contains(p)) return i;
            return -1;
        }

        int HitTile(Point p)
        {
            for (int i = 0; i < tileRects.Count; i++)
                if (tileRects[i].Contains(p)) return i;
            return -1;
        }

        bool IsResizeCorner(Point p)
        {
            return p.X >= body.Right - Util.RoundI(24 * UiScale) && p.Y >= body.Bottom - Util.RoundI(24 * UiScale);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (V2Shell.Config == null) return;
            if (!Body().Contains(e.Location)) return;
            if (e.Button == MouseButtons.Right)
            {
                int rightTile = HitTile(e.Location);
                if (rightTile >= 0 && rightTile < tileItems.Count) ShowItemMenu(tileItems[rightTile], PointToScreen(e.Location));
                else ShowBlankMenu(PointToScreen(e.Location));
                return;
            }
            if (e.Button != MouseButtons.Left) return;

            if (IsResizeCorner(e.Location))
            {
                resizing = true;
                downCursor = Cursor.Position;
                downBody = new Rectangle(Location.X, Location.Y, Width, Height);
                Capture = true;
                Cursor = Cursors.SizeNWSE;
                return;
            }
            if (headerR.Contains(e.Location))
            {
                windowDrag = true;
                dragOffset = e.Location;
                Capture = true;
                Cursor = Cursors.SizeAll;
                return;
            }
            if (prevR.Contains(e.Location) && PageCount() > 1)
            {
                page = Math.Max(0, page - 1);
                Render();
                return;
            }
            if (nextR.Contains(e.Location) && PageCount() > 1)
            {
                page = Math.Min(PageCount() - 1, page + 1);
                Render();
                return;
            }
            int gi = HitGroup(e.Location);
            if (gi >= 0)
            {
                V2Shell.Config.CollectorGroup = groupNames[gi];
                V2Shell.Config.Save();
                page = 0;
                Render();
                return;
            }
            int ti = HitTile(e.Location);
            if (ti >= 0)
            {
                downTile = ti;
                dragTile = ti;
                pressedTile = ti;
                tileDrag = false;
                downCursor = Cursor.Position;
                Capture = true;
                Render();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (V2Shell.Config == null) return;
            Point cursor = Cursor.Position;

            if (windowDrag)
            {
                Location = new Point(cursor.X - dragOffset.X, cursor.Y - dragOffset.Y);
                return;
            }
            if (resizing)
            {
                int dx = cursor.X - downCursor.X;
                int dy = cursor.Y - downCursor.Y;
                int w = Util.Clamp(ClientSize.Width + dx, Util.RoundI(420 * UiScale) + margin * 2, Util.RoundI(900 * UiScale) + margin * 2);
                int h = Util.Clamp(ClientSize.Height + dy, Util.RoundI(300 * UiScale) + margin * 2, Util.RoundI(720 * UiScale) + margin * 2);
                ClientSize = new Size(w, h);
                EnsureCanvas();
                Render();
                return;
            }
            if (downTile >= 0)
            {
                int dx = cursor.X - downCursor.X;
                int dy = cursor.Y - downCursor.Y;
                if (!tileDrag && (Math.Abs(dx) > 8 || Math.Abs(dy) > 8)) tileDrag = true;
                if (tileDrag)
                {
                    Point client = PointToClient(cursor);
                    if (!body.Contains(client))
                    {
                        int dragIndex = downTile;
                        CollectorItem item = (dragIndex >= 0 && dragIndex < tileItems.Count) ? tileItems[dragIndex] : null;
                        ResetTileDrag();
                        if (item != null) StartFileDrag(item);
                        return;
                    }
                    int dragGroup = HitGroup(client);
                    if (dragGroup != hoverGroup || groupTarget != dragGroup)
                    {
                        hoverGroup = dragGroup;
                        groupTarget = dragGroup;
                        Render();
                    }
                }
                return;
            }

            int ti = HitTile(e.Location);
            int gi = HitGroup(e.Location);
            bool pageHot = PageCount() > 1 && (prevR.Contains(e.Location) || nextR.Contains(e.Location));
            int nextHover = ti >= 0 ? ti : (pageHot ? (nextR.Contains(e.Location) ? -11 : -10) : -1);
            if (ti != hoverTile || gi != hoverGroup || nextHover != hoverTile)
            {
                hoverTile = nextHover;
                hoverGroup = gi;
                Render();
            }
            if (ti >= 0) Cursor = Cursors.Hand;
            else if (headerR.Contains(e.Location)) Cursor = Cursors.SizeAll;
            else if (IsResizeCorner(e.Location)) Cursor = Cursors.SizeNWSE;
            else Cursor = Cursors.Default;
        }

        void ResetTileDrag()
        {
            downTile = -1;
            dragTile = -1;
            pressedTile = -1;
            tileDrag = false;
            groupTarget = -1;
            Capture = false;
            Cursor = Cursors.Default;
        }

        void StartFileDrag(CollectorItem item)
        {
            if (item == null || !File.Exists(item.Path) && !Directory.Exists(item.Path)) return;
            try
            {
                DataObject data = new DataObject();
                data.SetData(DataFormats.FileDrop, new string[] { item.Path });
                DragDropEffects effect = DoDragDrop(data, DragDropEffects.Copy);
                if (effect != DragDropEffects.None)
                    V2Shell.Toast("已复制到目标位置（原文件未移动）");
            }
            catch (Exception ex) { Log.Warn("collector", "拖出失败 " + ex.Message); }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (V2Shell.Config == null) return;
            if (e.Button != MouseButtons.Left && !windowDrag && !resizing && !tileDrag) return;

            if (windowDrag)
            {
                windowDrag = false;
                Capture = false;
                Cursor = Cursors.Default;
                SaveBounds();
                return;
            }
            if (resizing)
            {
                resizing = false;
                Capture = false;
                Cursor = Cursors.Default;
                SaveBounds();
                Render();
                return;
            }
            if (downTile >= 0)
            {
                int ti = downTile;
                bool moved = tileDrag;
                int gi = groupTarget;
                ResetTileDrag();
                if (moved && gi >= 1 && gi < groupNames.Length && ti >= 0 && ti < tileItems.Count)
                {
                    CollectorItem item = tileItems[ti];
                    if (CollectorState.MoveToGroup(item.Path, groupNames[gi]))
                        V2Shell.Toast("已换到分组：" + groupNames[gi] + "（未移动文件）");
                }
                Render();
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (e.Button != MouseButtons.Left) return;
            int ti = HitTile(e.Location);
            if (ti >= 0 && ti < tileItems.Count) OpenItem(tileItems[ti]);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (hoverTile != -1 || hoverGroup != -1)
            {
                hoverTile = -1;
                hoverGroup = -1;
                Render();
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (PageCount() <= 1) return;
            if (e.Delta > 0) page = Math.Max(0, page - 1);
            else page = Math.Min(PageCount() - 1, page + 1);
            Render();
        }

        void OpenItem(CollectorItem item)
        {
            if (item == null) return;
            try
            {
                if (!File.Exists(item.Path) && !Directory.Exists(item.Path))
                {
                    CollectorState.Sync(true);
                    Render();
                    V2Shell.Toast("文件已不在桌面，入口已更新");
                    return;
                }
                Native.ShellOpen(item.Path);
            }
            catch (Exception ex)
            {
                Log.Warn("collector", "打开失败 " + ex.Message);
                V2Shell.Toast("打不开：" + item.DisplayName);
            }
        }

        void ShowItemMenu(CollectorItem item, Point screen)
        {
            List<MenuForm.Item> items = new List<MenuForm.Item>();
            items.Add(new MenuForm.Item("打开", delegate { OpenItem(item); }, Glyph.Dot));
            items.Add(new MenuForm.Item("在资源管理器中显示", delegate { Native.ShellOpenFolder(item.Path); }, Glyph.Folder));
            items.Add(new MenuForm.Item("移出收纳（不删除文件）", delegate
            {
                CollectorState.Hide(item.Path);
                Render();
                V2Shell.Toast("已移出收纳，原文件保留");
            }, Glyph.Close, true));
            new MenuForm(items, new Size(Util.RoundI(210 * Theme.S), Util.RoundI(8 * Theme.S))).ShowAt(screen);
        }

        void ShowBlankMenu(Point screen)
        {
            List<MenuForm.Item> items = new List<MenuForm.Item>();
            items.Add(new MenuForm.Item("重新扫描桌面", delegate { V2Shell.ScanCollectorNow(); }, Glyph.Restore));
            items.Add(new MenuForm.Item("恢复移出的图标", delegate
            {
                int n = CollectorState.RestoreHidden();
                Render();
                V2Shell.Toast(n > 0 ? "已恢复 " + n + " 个入口" : "没有移出的图标");
            }, Glyph.Restore));
            items.Add(new MenuForm.Item("桌面设置", delegate { V2Shell.OpenSettings(6); }, Glyph.Gear));
            items.Add(new MenuForm.Item("收纳设置", delegate { V2Shell.OpenSettings(7); }, Glyph.Gear));
            items.Add(new MenuForm.Item("关闭收纳面板", delegate { V2Shell.SetCollectorOn(false); }, Glyph.Close, true));
            new MenuForm(items, new Size(Util.RoundI(210 * Theme.S), Util.RoundI(8 * Theme.S))).ShowAt(screen);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0084 && V2Shell.Config != null)
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
    }
}
