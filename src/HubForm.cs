// Liuli v2 · 应用面板 + 控制中心：一个玻璃面板两个页签，不抢系统外壳。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Liuli
{
    sealed class HubForm : V2Window
    {
        public const int ModeApps = 0;
        public const int ModeControl = 1;

        int mode;
        int page;
        string query = "";
        bool searchFocus;
        List<AppEntry> filtered = new List<AppEntry>();
        ControlSnapshot ctl = new ControlSnapshot();
        Timer tick;

        Rectangle body;
        Rectangle tabApps;
        Rectangle tabControl;
        Rectangle closeRect;
        Rectangle newPanelRect;
        Rectangle searchRect;
        Rectangle gridRect;
        Rectangle prevRect;
        Rectangle nextRect;
        List<ControlTile> tiles = new List<ControlTile>();

        int hoverItem = -1;
        int hoverNav = -1;
        int hoverTab = -1;
        int hoverTile = -1;
        int pressedItem = -1;
        int pressedTile = -1;
        int downItem = -1;
        Point downPoint;
        bool dragging;

        sealed class ControlTile
        {
            public int Kind;
            public Rectangle Rect;
            public string Title;
            public string Value;
            public string Sub;
            public int Pct;
        }

        public HubForm(int startMode)
        {
            mode = startMode == ModeControl ? ModeControl : ModeApps;
            TopMost = true;
            tick = new Timer();
            tick.Interval = 2000;
            tick.Tick += delegate
            {
                if (!Visible || mode != ModeControl) return;
                ctl = ControlCenter.Sample();
                Render();
            };
        }

        protected override bool ShowWithoutActivation { get { return false; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle &= ~Native.WS_EX_NOACTIVATE;
                return cp;
            }
        }

        protected override void OnReady()
        {
            UiScale = DpiUiScale();
            ApplySize();
            PlaceOnScreen();
            RefreshApps();
            ctl = ControlCenter.Sample();
            Render();
            tick.Start();
        }

        void ApplySize()
        {
            int w = Util.RoundI(620 * UiScale);
            int h = Util.RoundI(460 * UiScale);
            int m = Util.RoundI(14 * UiScale);
            ClientSize = new Size(w + m * 2, h + m * 2);
        }

        void PlaceOnScreen()
        {
            Screen screen = Screen.FromPoint(Cursor.Position);
            Rectangle work = screen.WorkingArea;
            int x = work.Left + (work.Width - Width) / 2;
            int y = work.Bottom - Height - Util.RoundI(152 * UiScale);
            if (y < work.Top + Util.RoundI(20 * UiScale)) y = work.Top + Util.RoundI(20 * UiScale);
            if (x < work.Left) x = work.Left;
            if (x + Width > work.Right) x = work.Right - Width;
            Location = new Point(x, y);
        }

        public void ApplyTextScale()
        {
            UiScale = DpiUiScale();
            ApplySize();
            PlaceOnScreen();
            Render();
        }

        public void SetMode(int m)
        {
            int next = m == ModeControl ? ModeControl : ModeApps;
            if (next != mode) page = 0;
            mode = next;
            searchFocus = false;
            if (mode == ModeApps) RefreshApps();
            else ctl = ControlCenter.Sample();
            Render();
        }

        void RefreshApps()
        {
            filtered = AppCatalog.Query(query, 400);
            int pages = PageCount();
            if (page >= pages) page = Math.Max(0, pages - 1);
        }

        int PageCount()
        {
            return Math.Max(1, (filtered.Count + 15) / 16);
        }

        void UpdateLayout()
        {
            int m = Util.RoundI(14 * UiScale);
            body = new Rectangle(m, m, Math.Max(1, ClientSize.Width - m * 2), Math.Max(1, ClientSize.Height - m * 2));
            int x = body.X + Util.RoundI(20 * UiScale);
            int y = body.Y + Util.RoundI(16 * UiScale);
            tabApps = new Rectangle(x, y, Util.RoundI(68 * UiScale), Util.RoundI(27 * UiScale));
            tabControl = new Rectangle(tabApps.Right + Util.RoundI(6 * UiScale), y, Util.RoundI(88 * UiScale), Util.RoundI(27 * UiScale));
            newPanelRect = new Rectangle(tabControl.Right + Util.RoundI(10 * UiScale), y, Util.RoundI(92 * UiScale), Util.RoundI(27 * UiScale));
            closeRect = new Rectangle(body.Right - Util.RoundI(38 * UiScale), y - Util.RoundI(2 * UiScale),
                Util.RoundI(24 * UiScale), Util.RoundI(24 * UiScale));
            searchRect = new Rectangle(x, y + Util.RoundI(40 * UiScale), body.Width - Util.RoundI(40 * UiScale),
                Util.RoundI(30 * UiScale));

            int top = searchRect.Bottom + Util.RoundI(10 * UiScale);
            int bottom = body.Bottom - Util.RoundI(42 * UiScale);
            int gap = Util.RoundI(8 * UiScale);
            int cellW = Math.Max(40, (body.Width - Util.RoundI(40 * UiScale) - gap * 3) / 4);
            int cellH = Math.Max(40, (bottom - top - gap * 3) / 4);
            gridRect = new Rectangle(x, top, cellW * 4 + gap * 3, cellH * 4 + gap * 3);
            prevRect = new Rectangle(body.X + Util.RoundI(22 * UiScale), body.Bottom - Util.RoundI(32 * UiScale),
                Util.RoundI(22 * UiScale), Util.RoundI(22 * UiScale));
            nextRect = new Rectangle(body.Right - Util.RoundI(44 * UiScale), body.Bottom - Util.RoundI(32 * UiScale),
                Util.RoundI(22 * UiScale), Util.RoundI(22 * UiScale));
        }

        Rectangle CellRect(int indexInPage)
        {
            int gap = Util.RoundI(8 * UiScale);
            int col = indexInPage % 4;
            int row = indexInPage / 4;
            int cellW = (gridRect.Width - gap * 3) / 4;
            int cellH = (gridRect.Height - gap * 3) / 4;
            return new Rectangle(gridRect.X + col * (cellW + gap), gridRect.Y + row * (cellH + gap), cellW, cellH);
        }

        void BuildTiles()
        {
            tiles.Clear();
            if (ctl.VolumeOk)
            {
                ControlTile t = new ControlTile();
                t.Kind = 0; t.Title = "音量";
                t.Value = ctl.Mute ? "已静音" : ctl.Volume + "%";
                t.Sub = "点击静音 · 滚轮调节";
                t.Pct = ctl.Volume;
                tiles.Add(t);
            }
            if (ctl.NetworkOk)
            {
                ControlTile t = new ControlTile();
                t.Kind = 1; t.Title = "网络"; t.Value = ctl.Network;
                t.Sub = "点击打开网络设置";
                tiles.Add(t);
            }
            if (ctl.BluetoothOk)
            {
                ControlTile t = new ControlTile();
                t.Kind = 2; t.Title = "蓝牙"; t.Value = ctl.Bluetooth;
                t.Sub = "点击打开蓝牙设置";
                tiles.Add(t);
            }
            if (ctl.BrightnessOk)
            {
                ControlTile t = new ControlTile();
                t.Kind = 3; t.Title = "亮度"; t.Value = ctl.Brightness + "%";
                t.Sub = "点击循环 25 / 50 / 75 / 100";
                t.Pct = ctl.Brightness;
                tiles.Add(t);
            }
            int n = tiles.Count;
            if (n == 0) return;
            int x = body.X + Util.RoundI(20 * UiScale);
            int top = body.Y + Util.RoundI(60 * UiScale);
            int bottom = body.Bottom - Util.RoundI(20 * UiScale);
            int gap = Util.RoundI(12 * UiScale);
            int innerW = body.Width - Util.RoundI(40 * UiScale);
            int innerH = bottom - top;
            if (n == 1)
            {
                int h = Math.Min(innerH, Util.RoundI(158 * UiScale));
                tiles[0].Rect = new Rectangle(x, top + (innerH - h) / 2, innerW, h);
            }
            else
            {
                int rows = 2;
                int h = Math.Min(Util.RoundI(126 * UiScale), (innerH - gap) / rows);
                int total = rows * h + gap;
                int y0 = top + (innerH - total) / 2;
                int w = (innerW - gap) / 2;
                if (n == 2)
                {
                    tiles[0].Rect = new Rectangle(x, y0, innerW, h);
                    tiles[1].Rect = new Rectangle(x, y0 + h + gap, innerW, h);
                }
                else if (n == 3)
                {
                    tiles[0].Rect = new Rectangle(x, y0, w, h);
                    tiles[1].Rect = new Rectangle(x + w + gap, y0, w, h);
                    tiles[2].Rect = new Rectangle(x, y0 + h + gap, innerW, h);
                }
                else
                {
                    for (int i = 0; i < n && i < 4; i++)
                        tiles[i].Rect = new Rectangle(x + (i % 2) * (w + gap), y0 + (i / 2) * (h + gap), w, h);
                }
            }
        }

        public void Render()
        {
            if (!IsHandleCreated) return;
            EnsureCanvas();
            UpdateLayout();
            BuildTiles();
            using (Graphics g = Graphics.FromImage(Canvas))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.Transparent);
                PaintGlass(g, body, Theme.Radius);
                PaintHeader(g);
                if (mode == ModeApps) PaintApps(g);
                else PaintControl(g);
            }
            Native.PushLayered(Handle, Canvas, Location, (byte)255);
        }

        void PaintHeader(Graphics g)
        {
            PaintTab(g, tabApps, "应用", mode == ModeApps, hoverTab == 0);
            PaintTab(g, tabControl, "控制中心", mode == ModeControl, hoverTab == 1);
            if (mode == ModeApps)
            {
                Color c = hoverTab == 2 ? Theme.Accent : Theme.TextSub;
                Theme.Text(g, "＋ 新建面板", Theme.Font(11.5f, FontStyle.Regular, UiScale), c,
                    newPanelRect, StringAlignment.Near, true);
            }
            PaintIconButton(g, closeRect, Glyph.Close, hoverTab == 3, false, 1f);
        }

        void PaintTab(Graphics g, Rectangle r, string text, bool active, bool hover)
        {
            using (GraphicsPath p = Theme.Round(r, Util.RoundI(9 * UiScale)))
            {
                if (active) using (SolidBrush b = new SolidBrush(Color.FromArgb(46, Theme.Accent))) g.FillPath(b, p);
                else if (hover) using (SolidBrush b = new SolidBrush(Theme.Hover)) g.FillPath(b, p);
                if (active)
                    using (Pen pen = new Pen(Color.FromArgb(80, Theme.Accent), Math.Max(1f, UiScale))) g.DrawPath(pen, p);
            }
            Theme.TextMiddle(g, text, Theme.Font(12.5f, active ? FontStyle.Bold : FontStyle.Regular, UiScale),
                active ? Theme.TextMain : Theme.TextSub, r, StringAlignment.Center);
        }

        void PaintApps(Graphics g)
        {
            using (GraphicsPath p = Theme.Round(searchRect, Util.RoundI(10 * UiScale)))
            using (SolidBrush b = new SolidBrush(searchFocus ? Theme.Press : Theme.Hover)) g.FillPath(b, p);
            Theme.StrokeRound(g, searchRect, Util.RoundI(10 * UiScale),
                searchFocus ? Color.FromArgb(120, Theme.Accent) : Theme.Hair, Math.Max(1f, UiScale));
            string hint = query.Length > 0 ? query : "搜索应用";
            Theme.Text(g, hint + (searchFocus ? "｜" : ""), Theme.Font(11.5f, FontStyle.Regular, UiScale),
                query.Length > 0 ? Theme.TextMain : Theme.TextFaint,
                new Rectangle(searchRect.X + Util.RoundI(12 * UiScale), searchRect.Y,
                    searchRect.Width - Util.RoundI(24 * UiScale), searchRect.Height), StringAlignment.Near, true);

            int start = page * 16;
            for (int i = 0; i < 16; i++)
            {
                int index = start + i;
                if (index >= filtered.Count) break;
                AppEntry it = filtered[index];
                Rectangle c = CellRect(i);
                bool hover = hoverItem == index;
                bool pressed = pressedItem == index;
                if (hover || pressed)
                {
                    using (GraphicsPath p = Theme.Round(c, Util.RoundI(12 * UiScale)))
                    using (SolidBrush b = new SolidBrush(pressed ? Theme.Press : Theme.Hover)) g.FillPath(b, p);
                }
                int size = Util.RoundI(34 * UiScale);
                Rectangle icon = new Rectangle(c.X + (c.Width - size) / 2, c.Y + Util.RoundI(8 * UiScale), size, size);
                Icons.Draw(g, it.Path, icon, 48);
                Theme.Text(g, it.Name, Theme.Font(10.5f, FontStyle.Regular, UiScale), Theme.TextMain,
                    new Rectangle(c.X + Util.RoundI(5 * UiScale), icon.Bottom + Util.RoundI(3 * UiScale),
                        c.Width - Util.RoundI(10 * UiScale), Util.RoundI(17 * UiScale)),
                    StringAlignment.Center, true);
            }

            if (filtered.Count == 0)
                Theme.TextMiddle(g, "没有匹配的应用", Theme.Font(12f, FontStyle.Regular, UiScale), Theme.TextFaint,
                    gridRect, StringAlignment.Center);

            bool hasPrev = page > 0;
            bool hasNext = page + 1 < PageCount();
            PaintNav(g, prevRect, "‹", hasPrev, hoverNav == 0);
            PaintNav(g, nextRect, "›", hasNext, hoverNav == 1);
            string foot = (page + 1) + " / " + PageCount() + "  ·  共 " + filtered.Count + " 项";
            Theme.TextMiddle(g, foot, Theme.Font(10.5f, FontStyle.Regular, UiScale), Theme.TextFaint,
                new Rectangle(prevRect.Right, prevRect.Y, nextRect.X - prevRect.Right, prevRect.Height), StringAlignment.Center);
        }

        void PaintNav(Graphics g, Rectangle r, string text, bool enabled, bool hover)
        {
            if (hover && enabled)
            {
                using (GraphicsPath p = Theme.Round(r, r.Height / 2))
                using (SolidBrush b = new SolidBrush(Theme.Hover)) g.FillPath(b, p);
            }
            Theme.TextMiddle(g, text, Theme.Font(15f, FontStyle.Bold, UiScale),
                enabled ? (hover ? Theme.TextMain : Theme.TextSub) : Theme.TextFaint, r, StringAlignment.Center);
        }

        void PaintControl(Graphics g)
        {
            if (tiles.Count == 0)
            {
                Theme.TextMiddle(g, "系统快捷项暂不可用", Theme.Font(12f, FontStyle.Regular, UiScale), Theme.TextFaint,
                    new Rectangle(body.X, body.Y + Util.RoundI(80 * UiScale), body.Width,
                        body.Height - Util.RoundI(100 * UiScale)), StringAlignment.Center);
                return;
            }
            for (int i = 0; i < tiles.Count; i++) PaintTile(g, tiles[i], hoverTile == tiles[i].Kind, pressedTile == tiles[i].Kind);
        }

        void PaintTile(Graphics g, ControlTile t, bool hover, bool pressed)
        {
            using (GraphicsPath p = Theme.Round(t.Rect, Util.RoundI(13 * UiScale)))
            {
                Color bg = pressed ? Theme.Press : (hover ? Theme.Hover : Color.FromArgb(18, 255, 255, 255));
                using (SolidBrush b = new SolidBrush(bg)) g.FillPath(b, p);
                using (Pen pen = new Pen(hover ? Color.FromArgb(80, Theme.Accent) : Theme.Hair, Math.Max(1f, UiScale)))
                    g.DrawPath(pen, p);
            }
            Rectangle icon = new Rectangle(t.Rect.X + Util.RoundI(16 * UiScale), t.Rect.Y + Util.RoundI(16 * UiScale),
                Util.RoundI(28 * UiScale), Util.RoundI(28 * UiScale));
            if (t.Kind == 0) Theme.Glyph(g, Glyph.Speaker, icon, Theme.Accent, 1.5f * UiScale);
            else if (t.Kind == 1) Theme.Glyph(g, Glyph.Wifi, icon, Theme.Accent, 1.5f * UiScale);
            else if (t.Kind == 2) Theme.Glyph(g, Glyph.Bluetooth, icon, Theme.Accent, 1.5f * UiScale);
            else Theme.Glyph(g, Glyph.Sun, icon, Theme.Accent, 1.5f * UiScale);

            int tx = icon.Right + Util.RoundI(10 * UiScale);
            Theme.Text(g, t.Title, Theme.Font(12f, FontStyle.Bold, UiScale), Theme.TextSub,
                new Rectangle(tx, t.Rect.Y + Util.RoundI(13 * UiScale),
                    t.Rect.Right - tx - Util.RoundI(12 * UiScale), Util.RoundI(20 * UiScale)), StringAlignment.Near, true);
            Theme.Text(g, t.Value, Theme.Font(15.5f, FontStyle.Bold, UiScale), Theme.TextMain,
                new Rectangle(tx, t.Rect.Y + Util.RoundI(36 * UiScale),
                    t.Rect.Right - tx - Util.RoundI(12 * UiScale), Util.RoundI(28 * UiScale)), StringAlignment.Near, true);
            Theme.Text(g, t.Sub, Theme.Font(10.5f, FontStyle.Regular, UiScale), Theme.TextFaint,
                new Rectangle(tx, t.Rect.Bottom - Util.RoundI(34 * UiScale),
                    t.Rect.Right - tx - Util.RoundI(12 * UiScale), Util.RoundI(18 * UiScale)), StringAlignment.Near, true);

            if (t.Kind == 0 || t.Kind == 3)
            {
                Rectangle bar = new Rectangle(t.Rect.X + Util.RoundI(16 * UiScale), t.Rect.Bottom - Util.RoundI(14 * UiScale),
                    t.Rect.Width - Util.RoundI(32 * UiScale), Util.RoundI(4 * UiScale));
                using (GraphicsPath bg = Theme.Round(bar, bar.Height / 2))
                using (SolidBrush b = new SolidBrush(Color.FromArgb(30, 255, 255, 255))) g.FillPath(b, bg);
                int fillW = Math.Max(1, bar.Width * Util.Clamp(t.Pct, 0, 100) / 100);
                Rectangle fill = new Rectangle(bar.X, bar.Y, fillW, bar.Height);
                using (GraphicsPath fg = Theme.Round(fill, fill.Height / 2))
                using (SolidBrush b = new SolidBrush(Theme.Accent)) g.FillPath(b, fg);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            UpdateLayout();
            int hi = -1, ht = -1;
            if (mode == ModeApps)
            {
                int start = page * 16;
                for (int i = 0; i < 16; i++)
                {
                    int index = start + i;
                    if (index >= filtered.Count) break;
                    if (CellRect(i).Contains(e.Location)) { hi = index; break; }
                }
            }
            if (tabApps.Contains(e.Location)) ht = 0;
            else if (tabControl.Contains(e.Location)) ht = 1;
            else if (mode == ModeApps && newPanelRect.Contains(e.Location)) ht = 2;
            else if (closeRect.Contains(e.Location)) ht = 3;
            int ti = -1;
            for (int i = 0; i < tiles.Count; i++) if (tiles[i].Rect.Contains(e.Location)) { ti = tiles[i].Kind; break; }
            int hv = -1;
            if (mode == ModeApps)
            {
                if (prevRect.Contains(e.Location)) hv = 0;
                else if (nextRect.Contains(e.Location)) hv = 1;
            }
            if (hi != hoverItem || ht != hoverTab || ti != hoverTile || hv != hoverNav)
            {
                hoverItem = hi; hoverTab = ht; hoverTile = ti; hoverNav = hv;
                Cursor = (hi >= 0 || ht >= 0 || ti >= 0 || hv >= 0) ? Cursors.Hand : Cursors.Default;
                Render();
            }
            if (e.Button == MouseButtons.Left && downItem >= 0 && mode == ModeApps && !dragging)
            {
                if (Math.Abs(e.X - downPoint.X) + Math.Abs(e.Y - downPoint.Y) > Util.RoundI(9 * UiScale))
                    StartDrag(downItem);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            UpdateLayout();
            Focus();
            if (e.Button == MouseButtons.Right && mode == ModeApps)
            {
                int start = page * 16;
                for (int i = 0; i < 16; i++)
                {
                    if (start + i >= filtered.Count) break;
                    if (CellRect(i).Contains(e.Location)) { Pin(filtered[start + i]); return; }
                }
                return;
            }
            if (e.Button != MouseButtons.Left) return;
            searchFocus = mode == ModeApps && searchRect.Contains(e.Location);
            if (searchFocus)
            {
                Render();
                return;
            }
            if (mode == ModeApps)
            {
                int start = page * 16;
                for (int i = 0; i < 16; i++)
                {
                    if (start + i >= filtered.Count) break;
                    if (CellRect(i).Contains(e.Location))
                    {
                        pressedItem = start + i; downItem = pressedItem; downPoint = e.Location;
                        Render();
                        return;
                    }
                }
            }
            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i].Rect.Contains(e.Location)) { pressedTile = tiles[i].Kind; Render(); return; }
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (mode == ModeApps)
            {
                if (e.Delta > 0) Page(-1); else Page(1);
                return;
            }
            if (hoverTile == 0)
            {
                ControlCenter.StepVolume(e.Delta > 0 ? 4 : -4);
                ctl = ControlCenter.Sample();
                Render();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;
            if (dragging) return;
            int pi = pressedItem;
            int pt = pressedTile;
            pressedItem = -1; pressedTile = -1; downItem = -1;
            UpdateLayout();
            if (tabApps.Contains(e.Location)) { SetMode(ModeApps); return; }
            if (tabControl.Contains(e.Location)) { SetMode(ModeControl); return; }
            if (closeRect.Contains(e.Location)) { Hide(); return; }
            if (mode == ModeApps && newPanelRect.Contains(e.Location)) { V2Shell.AddPanel(); Hide(); return; }
            if (mode == ModeApps)
            {
                if (pi >= 0 && filtered.Count > pi && CellRect(pi % 16).Contains(e.Location))
                {
                    Launch(filtered[pi]);
                    return;
                }
                if (prevRect.Contains(e.Location)) { Page(-1); return; }
                if (nextRect.Contains(e.Location)) { Page(1); return; }
            }
            else if (pt >= 0)
            {
                for (int i = 0; i < tiles.Count; i++)
                {
                    if (tiles[i].Kind != pt || !tiles[i].Rect.Contains(e.Location)) continue;
                    TileAction(pt);
                    break;
                }
            }
            Render();
        }

        void Page(int delta)
        {
            int next = page + delta;
            if (next < 0) next = 0;
            if (next >= PageCount()) next = PageCount() - 1;
            if (next != page) { page = next; Render(); }
        }

        void TileAction(int kind)
        {
            if (kind == 0)
            {
                if (!ControlCenter.ToggleMute()) V2Shell.Toast("音量接口不可用");
                ctl = ControlCenter.Sample();
                Render();
            }
            else if (kind == 1)
            {
                ShellOpen("ms-settings:network");
                Hide();
            }
            else if (kind == 2)
            {
                ShellOpen("ms-settings:bluetooth");
                Hide();
            }
            else if (kind == 3)
            {
                int next = 25;
                if (ctl.Brightness >= 75) next = 25;
                else if (ctl.Brightness >= 50) next = 75;
                else if (ctl.Brightness >= 25) next = 50;
                ControlCenter.StepBrightness(next);
                ctl = ControlCenter.Sample();
                Render();
            }
        }

        void Launch(AppEntry it)
        {
            if (it == null) return;
            try
            {
                Process.Start(new ProcessStartInfo(it.Path) { UseShellExecute = true });
                Log.Info("apps", "启动 " + it.Path);
                Hide();
            }
            catch (Exception ex)
            {
                Log.Warn("apps", "启动失败 " + ex.Message);
                V2Shell.Toast("启动失败：" + it.Name);
            }
        }

        void Pin(AppEntry it)
        {
            if (it == null) return;
            int n = V2Shell.PinPaths(new string[] { it.Path });
            if (n > 0) V2Shell.Toast("已固定到 Dock：" + it.Name);
            else V2Shell.Toast("未固定：Dock 已满或已存在");
            Log.Info("apps", "右键固定 " + it.Path + " n=" + n);
        }

        void StartDrag(int index)
        {
            if (index < 0 || index >= filtered.Count) return;
            dragging = true;
            pressedItem = -1;
            downItem = -1;
            Render();
            try
            {
                DropTarget.EnsureOle();
                DataObject data = new DataObject();
                data.SetData(DataFormats.FileDrop, new string[] { filtered[index].Path });
                DragDropEffects eff = DoDragDrop(data, DragDropEffects.Copy);
                if (eff == DragDropEffects.Copy) Hide();
            }
            catch (Exception ex) { Log.Warn("apps", "拖到 Dock 失败 " + ex.Message); }
            dragging = false;
        }

        void ShellOpen(string target)
        {
            try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); }
            catch (Exception ex) { Log.Warn("control", "打开设置失败 " + ex.Message); }
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            if (mode != ModeApps || !searchFocus) return;
            char c = e.KeyChar;
            if (c == (char)8)
            {
                if (query.Length > 0) query = query.Substring(0, query.Length - 1);
                page = 0; RefreshApps(); Render();
                e.Handled = true;
            }
            else if (c >= 32 && c != 127)
            {
                query += c;
                page = 0; RefreshApps(); Render();
                e.Handled = true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape) { Hide(); e.Handled = true; return; }
            if (mode == ModeApps && searchFocus)
            {
                if (e.Control && e.KeyCode == Keys.A)
                {
                    query = ""; page = 0; RefreshApps(); Render();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Back)
                {
                    if (query.Length > 0) query = query.Substring(0, query.Length - 1);
                    page = 0; RefreshApps(); Render();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Enter && filtered.Count > 0) { Launch(filtered[page * 16]); e.Handled = true; }
            }
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            if (!dragging) Hide();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (tick != null) tick.Stop();
            base.OnFormClosed(e);
        }
    }
}
