// Liuli · 看板窗口：无边框分层窗（ULW），一块看板一个窗口；拖动/缩放/折叠/滚轮/右键都在这里。
// 铁律：位置与尺寸只改 Board 模型；绘制只读模型，绘制里不碰 IO。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Liuli
{
    partial class BoardWindow : Form
    {
        public Board B;

        Bitmap canvas;
        Graphics g;
        bool dirty = true;

        long openT0;
        const int OpenMs = 260;
        float openT;

        long hoverT0 = Environment.TickCount - 1000;
        float hoverT;              // 0..1，鼠标进入后按钮淡入
        bool hoverBoard;
        int hoverBtn = -1;
        int hoverRow = -1;
        int hoverTile = -1;
        bool hoverContent;

        // 命中区（每帧重建）
        struct Zone
        {
            public Rectangle R;
            public string Tag;
            public int Index;
            public Zone(Rectangle r, string tag, int idx) { R = r; Tag = tag; Index = idx; }
        }
        List<Zone> hits = new List<Zone>();
        string pressedTag = "";
        int pressedIndex = -1;

        // 拖动
        enum DragMode { None, Move, L, R, T, B, TL, TR, BL, BR }
        DragMode drag = DragMode.None;
        Point dragStartCursor;
        Rectangle dragStartRect;
        bool dragMoved;

        // 折叠动画
        long collapseT0;
        float collapseT;
        bool collapseFrom;

        // 关闭二次确认
        long closeArmTick;
        long closingTick;

        // 滚动
        float scrollPx;
        float scrollTarget;
        long scrollAnimT0;

        // 菜单
        MenuForm menu;
        DropTarget oleDrop;

        // 双击判定（分层窗不一定拿到 DBLCLK，自己做时间窗）
        long lastClickTick;
        Point lastClickPos;
        int lastSecond = -1;

        // 文件条目：按下先不打开，移动超过阈值才拖出，松开才算单击
        int tilePressIndex = -1;
        Point tilePressScreen;
        bool tileDragged;
        long lastTileOpenTick;

        public BoardWindow(Board b)
        {
            B = b;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Black;
            DoubleBuffered = false;
            TopMost = false;
            SetBounds(b.X, b.Y, b.W, b.H);
            Bounds = new Rectangle(b.X, b.Y, b.W, b.H);
            openT0 = Environment.TickCount;
            collapseFrom = b.Collapsed;
            collapseT = b.Collapsed ? 1f : 0f;
            collapseT0 = Environment.TickCount - 1000;
        }

        public int WindowHeight()
        {
            if (!B.Collapsed) return B.H;
            float s = Theme.S;
            return Util.RoundI(44 * s) + Glass.Margin * 2;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= Native.WS_EX_LAYERED | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE;
                if (B != null && (B.Pinned || (App.Ws != null && App.Ws.Set.AlwaysTop))) cp.ExStyle |= Native.WS_EX_TOPMOST;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation { get { return true; } }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e) { }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try { Native.DragAcceptFiles(Handle, true); }
            catch (Exception ex) { Log.Warn("drop", "DragAcceptFiles 失败 " + ex.Message); }
            try
            {
                if (oleDrop != null) DropTarget.RevokeDragDrop(Handle);
                DropTarget.EnsureOle();
                oleDrop = new DropTarget(B.Title, delegate(string[] fs) { OnFilesDropped(new List<string>(fs)); }, DropTarget.MOVE);
                int hr = DropTarget.RegisterDragDrop(Handle, oleDrop);
                if (hr != 0) Log.Warn("drop", "OLE 注册失败 " + B.Title + " hr=" + hr);
            }
            catch (Exception ex) { Log.Warn("drop", "OLE 注册失败 " + ex.Message); }
            try { ApplyAccent(); } catch (Exception ex) { Log.Warn("glass", "玻璃应用失败 " + ex.Message); }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try { if (oleDrop != null) DropTarget.RevokeDragDrop(Handle); } catch { }
            base.OnFormClosed(e);
        }

        public void ApplyAccent()
        {
            // 实时玻璃：DWM 亚克力（GPU 合成）。拖拽期间关掉，避免跟手延迟。
            if (Theme.Live)
            {
                int a = (int)(Theme.GlassPct * 0.55f);
                Color tint = Theme.Light ? Color.FromArgb(150, 235, 240, 248) : Color.FromArgb(150, 20, 24, 34);
                int argb = (tint.A << 24) | (tint.B << 16) | (tint.G << 8) | tint.R;
                Native.ApplyAcrylic(Handle, true, argb);
            }
            else Native.ApplyAcrylic(Handle, false, 0);
        }

        public void SetLiveDuringDrag(bool on)
        {
            if (!Theme.Live) return;
            if (on) Native.ApplyAcrylic(Handle, false, 0);
            else ApplyAccent();
        }

        // ---------- 尺寸 / 位置 ----------
        public void SyncBounds()
        {
            int h = WindowHeight();
            if (Width != B.W || Height != h || Left != B.X || Top != B.Y)
            {
                Bounds = new Rectangle(B.X, B.Y, B.W, h);
                EnsureCanvas();
            }
        }

        public void EnsureCanvas()
        {
            int w = Math.Max(1, Width), h = Math.Max(1, Height);
            if (canvas != null && canvas.Width == w && canvas.Height == h) return;
            if (canvas != null) canvas.Dispose();
            if (g != null) g.Dispose();
            canvas = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            g = Graphics.FromImage(canvas);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            dirty = true;
        }

        public void MarkDirty()
        {
            dirty = true;
        }

        public bool IsDragging
        {
            get { return drag != DragMode.None; }
        }

        public void TickIfDirty()
        {
            if (!dirty || !Visible) return;
            Render();
            Push();
            dirty = false;
        }

        public bool IsAnimating()
        {
            if (openT < 1f) return true;
            if (Math.Abs(scrollTarget - scrollPx) > 0.5f) return true;
            if (collapseT > 0f && collapseT < 1f) return true;
            if (hoverT > 0f && hoverT < 1f) return true;
            if (closingTick != 0) return true;
            return false;
        }

        public void Tick()
        {
            long now = Environment.TickCount;
            float prevOpen = openT;
            openT = Util.OutCubic(Util.Progress(openT0, OpenMs));
            if (openT != prevOpen) dirty = true;

            float targetH = hoverBoard ? 1f : 0f;
            float prevH = hoverT;
            float step = 15f / Theme.AnimFast;
            if (hoverT < targetH) hoverT = Math.Min(targetH, hoverT + step);
            else if (hoverT > targetH) hoverT = Math.Max(targetH, hoverT - step * 1.6f);
            if (hoverT != prevH) dirty = true;

            if (Math.Abs(scrollTarget - scrollPx) > 0.4f)
            {
                scrollPx += (scrollTarget - scrollPx) * 0.22f;
                if (Math.Abs(scrollTarget - scrollPx) < 0.6f) scrollPx = scrollTarget;
                dirty = true;
            }

            if (collapseT > 0f && collapseT < 1f)
            {
                collapseT = Util.OutCubic(Util.Progress(collapseT0, Theme.AnimStd));
                SyncBounds();
                dirty = true;
            }

            if (closingTick != 0 && now - closingTick > 150) { closingTick = 0; }

            if (B.Kind == BoardKind.Clock)
            {
                int sec = DateTime.Now.Second;
                if (sec != lastSecond) { lastSecond = sec; dirty = true; }
            }

            if (dirty) { Render(); Push(); dirty = false; }
        }

        public void AnimateCollapse(bool collapsed)
        {
            if (collapseFrom == collapsed) return;
            collapseFrom = collapsed;
            collapseT0 = Environment.TickCount;
            collapseT = 0.01f;
            if (collapsed) { B.Collapsed = true; }
            else { B.Collapsed = false; }
            App.Save();
        }

        // ---------- 绘制 ----------
        void Render()
        {
            EnsureCanvas();
            hits.Clear();
            float s = Theme.S;
            g.ResetClip();
            g.Clear(Color.FromArgb(0));

            Rectangle win = new Rectangle(0, 0, Width, Height);
            Rectangle body = Glass.Body(win);
            Color accent = BoardAccent();

            if (B.Collapsed || collapseT >= 0.999f)
            {
                PaintCollapsed(body, accent);
                return;
            }
            if (collapseT > 0.001f)
            {
                // 折叠动画中途：按插值高度裁切
                int fullH = B.H;
                int smallH = WindowHeight();
                int h = Util.LerpI(fullH, smallH, collapseT);
                body = new Rectangle(Glass.Margin, Glass.Margin, Math.Max(1, B.W - Glass.Margin * 2), Math.Max(1, h - Glass.Margin * 2));
            }

            Glass.Paint(g, B, body, accent, hoverBoard, drag != DragMode.None, hoverBoard);
            PaintHeader(body, accent, s);
            if (collapseT <= 0.001f) PaintContent(body, accent, s);
        }

        public Color BoardAccent()
        {
            return Palette.Get(B.Accent, Theme.Light);
        }

        void PaintCollapsed(Rectangle body, Color accent)
        {
            float s = Theme.S;
            Glass.Paint(g, B, body, accent, hoverBoard, drag != DragMode.None, hoverBoard);
            int pad = Util.RoundI(14 * s);
            int iconBox = Util.RoundI(24 * s);
            Rectangle icon = new Rectangle(body.X + pad, body.Y + (body.Height - iconBox) / 2, iconBox, iconBox);
            using (GraphicsPath p = Theme.Round(icon, Util.RoundI(7 * s)))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(40, accent))) g.FillPath(b, p);
            Theme.Glyph(g, Kinds.Icon(B.Kind), Util.Inflate(icon, -Util.RoundI(5 * s)), accent, 1.4f * s);

            Rectangle tr = new Rectangle(icon.Right + Util.RoundI(8 * s), body.Y, body.Width - iconBox - pad * 3 - Util.RoundI(24 * s), body.Height);
            Theme.TextMiddle(g, B.Title, Theme.Font(13f, FontStyle.Bold), Theme.TextMain, tr, StringAlignment.Near);
            string sub = Summary();
            if (sub.Length > 0)
            {
                // 摘要贴右侧，窄了自然省略
                int w = Math.Min(Util.RoundI(150 * s), body.Width / 2);
                Rectangle sr = new Rectangle(body.Right - pad - w - Util.RoundI(24 * s), body.Y, w, body.Height);
                Theme.TextMiddle(g, sub, Theme.Font(9.5f, FontStyle.Regular), Theme.TextFaint, sr, StringAlignment.Far);
            }
            Rectangle chev = new Rectangle(body.Right - pad - Util.RoundI(20 * s), body.Y + (body.Height - Util.RoundI(20 * s)) / 2, Util.RoundI(20 * s), Util.RoundI(20 * s));
            Theme.Glyph(g, Glyph.Chevron, chev, Theme.TextSub, 1.4f * s);
            hits.Add(new Zone(new Rectangle(body.X, body.Y, body.Width, body.Height), "collapse", 0));
            hits.Add(new Zone(icon, "kind", 0));
        }

        string Summary()
        {
            if (B.Kind == BoardKind.Todo)
            {
                int done = 0;
                for (int i = 0; i < B.Items.Count; i++) if (B.Items[i].Done) done++;
                return done + "/" + B.Items.Count;
            }
            if (B.Kind == BoardKind.Files || B.Kind == BoardKind.Code) return B.Items.Count + " 项";
            if (B.Kind == BoardKind.Clock) return DateTime.Now.ToString("HH:mm");
            if (B.Kind == BoardKind.Note) return B.Text.Replace("\r", "").Replace("\n", " ").Trim();
            return "";
        }

        void PaintHeader(Rectangle body, Color accent, float s)
        {
            int pad = Util.RoundI(14 * s);
            int headerH = Util.RoundI(46 * s);
            int iconBox = Util.RoundI(24 * s);
            Rectangle icon = new Rectangle(body.X + pad, body.Y + (headerH - iconBox) / 2, iconBox, iconBox);
            using (GraphicsPath p = Theme.Round(icon, Util.RoundI(7 * s)))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(42, accent))) g.FillPath(b, p);
            Theme.Glyph(g, Kinds.Icon(B.Kind), Util.Inflate(icon, -Util.RoundI(5 * s)), accent, 1.4f * s);

            int buttonsW = Util.RoundI(4 * 28 * s) + Util.RoundI(6 * s);
            int titleW = body.Width - iconBox - pad * 3 - buttonsW - Util.RoundI(8 * s);
            Rectangle title = new Rectangle(icon.Right + Util.RoundI(9 * s), body.Y + Util.RoundI(6 * s), Math.Max(20, titleW), Util.RoundI(22 * s));
            Theme.Text(g, B.Title, Theme.Font(13.5f, FontStyle.Bold), Theme.TextMain, title, StringAlignment.Near, true);

            Rectangle sub = new Rectangle(title.X, body.Y + Util.RoundI(26 * s), Math.Max(20, titleW), Util.RoundI(15 * s));
            string subText = Kinds.Hint(B.Kind);
            if (B.Kind == BoardKind.Todo) subText = CountDone() + " / " + B.Items.Count + " 已完成";
            else if (B.Kind == BoardKind.Files) subText = B.Items.Count + " 个入口";
            else if (B.Kind == BoardKind.Code) subText = B.Items.Count + " 段代码";
            else if (B.Kind == BoardKind.Status) subText = B.City + (B.ShowWeather ? " · 天气" : "");
            Theme.Text(g, subText, Theme.Font(9f, FontStyle.Regular), Theme.TextFaint, sub, StringAlignment.Near, true);

            hits.Add(new Zone(title, "title", 0));
            hits.Add(new Zone(icon, "kind", 0));

            // 右上角按钮：悬停淡入
            int bs = Util.RoundI(26 * s);
            int gap = Util.RoundI(4 * s);
            int x = body.Right - pad - bs;
            int y = body.Y + (headerH - bs) / 2;
            int alpha = (int)(hoverT * 255);

            string[] tags = { "close", "more", "pin", "collapse" };
            Glyph[] icons = { Glyph.Close, Glyph.More, Glyph.Pin, Glyph.Chevron };
            for (int i = 0; i < tags.Length; i++)
            {
                Rectangle r = new Rectangle(x, y, bs, bs);
                bool hot = hoverBtn == i && hoverT > 0.25f;
                bool active = (i == 2 && B.Pinned);
                bool armed = i == 0 && IsCloseArmed();
                if (alpha > 40)
                {
                    Glass.Button(g, r, armed ? Glyph.Check : icons[i], hot, armed || active, armed ? Theme.Danger : accent);
                    if (armed) { using (GraphicsPath p = Theme.Round(r, bs / 2)) using (SolidBrush b = new SolidBrush(Color.FromArgb(26, Theme.Danger))) g.FillPath(b, p); }
                }
                if (hoverT > 0.55f) hits.Add(new Zone(r, tags[i], i));
                x -= bs + gap;
            }
        }

        int CountDone()
        {
            int n = 0;
            for (int i = 0; i < B.Items.Count; i++) if (B.Items[i].Done) n++;
            return n;
        }

        bool IsCloseArmed()
        {
            return closeArmTick != 0 && Environment.TickCount - closeArmTick < 2600;
        }

        void Push()
        {
            if (canvas == null) return;
            float t = openT;
            int offsetY = 0;
            byte alpha = 255;
            if (drag == DragMode.None && t < 1f)
            {
                offsetY = Util.RoundI((1f - t) * 10 * Theme.S);
                alpha = (byte)Util.Clamp((int)(t * 255), 0, 255);
            }
            Native.PushLayered(Handle, canvas, new Point(Left, Top + offsetY), alpha);
        }

        // ---------- 鼠标 ----------
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            App.KickAnimation();
            if (tilePressIndex >= 0 && !tileDragged && (Control.MouseButtons & MouseButtons.Left) != 0)
            {
                Point cur = Cursor.Position;
                System.Drawing.Size sz = SystemInformation.DragSize;
                if (Math.Abs(cur.X - tilePressScreen.X) > Math.Max(4, sz.Width / 2) ||
                    Math.Abs(cur.Y - tilePressScreen.Y) > Math.Max(4, sz.Height / 2))
                {
                    int idx = tilePressIndex;
                    tilePressIndex = -1;
                    tileDragged = true;
                    StartItemDrag(idx);
                }
            }
            if (drag != DragMode.None) { DoDrag(e); return; }
            UpdateHover(e.Location);
        }

        void UpdateHover(Point p)
        {
            int oldBtn = hoverBtn, oldRow = hoverRow, oldTile = hoverTile;
            bool oldContent = hoverContent;
            hoverBtn = -1; hoverRow = -1; hoverTile = -1; hoverContent = false;
            string tag = "";
            for (int i = hits.Count - 1; i >= 0; i--)
            {
                if (hits[i].R.Contains(p)) { tag = hits[i].Tag; if (i >= 0) { } break; }
            }
            if (tag == "close") hoverBtn = 0;
            else if (tag == "more") hoverBtn = 1;
            else if (tag == "pin") hoverBtn = 2;
            else if (tag == "collapse") hoverBtn = 3;
            else if (tag == "item") { hoverRow = IndexAt(p, "item"); }
            else if (tag == "check") { hoverRow = IndexAt(p, "check"); }
            else if (tag == "tile") { hoverTile = IndexAt(p, "tile"); }
            else if (tag == "content" || tag == "body") hoverContent = true;

            bool enter = !hoverBoard;
            if (p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height) hoverBoard = true;
            else hoverBoard = false;
            if (enter && hoverBoard) hoverT0 = Environment.TickCount;

            Cursor cur = Cursors.Default;
            if (ResizeCursorAt(p, out cur)) { Cursor = cur; }
            else if (tag == "close" || tag == "more" || tag == "pin" || tag == "collapse" || hoverRow >= 0 || hoverTile >= 0 || tag == "item" || tag == "tile" || tag == "add")
                Cursor = Cursors.Hand;
            else Cursor = Cursors.Default;

            if (oldBtn != hoverBtn || oldRow != hoverRow || oldTile != hoverTile || oldContent != hoverContent) dirty = true;
            if (hoverBoard) dirty = true;
        }

        int IndexAt(Point p, string tag)
        {
            for (int i = 0; i < hits.Count; i++) if (hits[i].Tag == tag && hits[i].R.Contains(p)) return hits[i].Index;
            return -1;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hoverBoard = false; hoverBtn = -1; hoverRow = -1; hoverTile = -1; hoverContent = false;
            dirty = true;
        }

        bool ResizeCursorAt(Point p, out Cursor cur)
        {
            cur = Cursors.Default;
            if (B.Collapsed || B.Locked) return false;
            Rectangle body = Glass.Body(new Rectangle(0, 0, Width, Height));
            int edge = Util.RoundI(7 * Theme.S);
            bool left = p.X >= body.X - 2 && p.X <= body.X + edge;
            bool right = p.X >= body.Right - edge && p.X <= body.Right + 2;
            bool top = p.Y >= body.Y - 2 && p.Y <= body.Y + edge;
            bool bottom = p.Y >= body.Bottom - edge && p.Y <= body.Bottom + 2;
            if (left && top) { cur = Cursors.SizeNWSE; return true; }
            if (right && bottom) { cur = Cursors.SizeNWSE; return true; }
            if (right && top) { cur = Cursors.SizeNESW; return true; }
            if (left && bottom) { cur = Cursors.SizeNESW; return true; }
            if (left) { cur = Cursors.SizeWE; return true; }
            if (right) { cur = Cursors.SizeWE; return true; }
            if (top) { cur = Cursors.SizeNS; return true; }
            if (bottom) { cur = Cursors.SizeNS; return true; }
            return false;
        }

        DragMode ResizeModeAt(Point p)
        {
            Rectangle body = Glass.Body(new Rectangle(0, 0, Width, Height));
            int edge = Util.RoundI(7 * Theme.S);
            bool left = p.X >= body.X - 2 && p.X <= body.X + edge;
            bool right = p.X >= body.Right - edge && p.X <= body.Right + 2;
            bool top = p.Y >= body.Y - 2 && p.Y <= body.Y + edge;
            bool bottom = p.Y >= body.Bottom - edge && p.Y <= body.Bottom + 2;
            if (left && top) return DragMode.TL;
            if (right && bottom) return DragMode.BR;
            if (right && top) return DragMode.TR;
            if (left && bottom) return DragMode.BL;
            if (left) return DragMode.L;
            if (right) return DragMode.R;
            if (top) return DragMode.T;
            if (bottom) return DragMode.B;
            return DragMode.None;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Right)
            {
                ShowMenu(e.Location);
                return;
            }
            if (e.Button != MouseButtons.Left) return;

            closeArmTick = IsCloseArmed() ? closeArmTick : 0;

            DragMode rm = ResizeModeAt(e.Location);
            if (rm != DragMode.None)
            {
                drag = rm;
                dragStartCursor = Cursor.Position;
                dragStartRect = Bounds;
                Capture = true;
                SetLiveDuringDrag(true);
                return;
            }

            string tag = "";
            for (int i = hits.Count - 1; i >= 0; i--) if (hits[i].R.Contains(e.Location)) { tag = hits[i].Tag; pressedIndex = hits[i].Index; break; }
            pressedTag = tag;

            long now = Environment.TickCount;
            Point sp = PointToScreen(e.Location);
            bool dbl = now - lastClickTick < 400 && Math.Abs(sp.X - lastClickPos.X) < 8 && Math.Abs(sp.Y - lastClickPos.Y) < 8;
            lastClickTick = now; lastClickPos = sp;
            if (dbl && tag != "close" && tag != "more" && tag != "pin")
            {
                lastClickTick = 0;
                if (tag == "title") { Rename(); return; }
                if (B.Kind == BoardKind.Note && (tag == "content" || tag == "body")) { EditNote(); return; }
                if (B.Kind == BoardKind.Todo && (tag == "item" || tag == "check")) { EditItem(pressedIndex); return; }
                if (B.Kind == BoardKind.Status && tag == "city") { EditCity(); return; }
            }

            if (tag == "collapse")
            {
                drag = DragMode.Move;
                dragStartCursor = Cursor.Position;
                dragStartRect = Bounds;
                dragMoved = false;
                Capture = true;
                SetLiveDuringDrag(true);
                return;
            }
            if (tag == "close")
            {
                if (IsCloseArmed()) { App.RemoveBoard(this); return; }
                closeArmTick = now;
                dirty = true;
                return;
            }
            if (tag == "more") { ShowMenu(HeaderMenuPoint()); return; }
            if (tag == "pin") { B.Pinned = !B.Pinned; ApplyTopMode(); dirty = true; App.Save(); return; }
            if (tag == "check") { ToggleItem(pressedIndex); return; }
            if (tag == "add") { AddItem(); return; }
            if (tag == "city") { EditCity(); return; }
            if (tag == "copy")
            {
                if (pressedIndex >= 0 && pressedIndex < B.Items.Count) { App.Copy(B.Items[pressedIndex].Text); Toast("已复制代码片段"); }
                return;
            }
            if (tag == "tile")
            {
                // 单击打开改到 MouseUp：先允许拖出，避免一按下就打开
                if (pressedIndex >= 0 && pressedIndex < B.Items.Count)
                {
                    tilePressIndex = pressedIndex;
                    tilePressScreen = PointToScreen(e.Location);
                    tileDragged = false;
                    return;
                }
            }
            if (tag == "item")
            {
                if (B.Kind == BoardKind.Todo && dbl) { EditItem(pressedIndex); return; }
                if (B.Kind == BoardKind.Todo) { EditItem(pressedIndex); return; }
                if (B.Kind == BoardKind.Code) { EditItem(pressedIndex); return; }
                return;
            }
            if (tag == "title" && dbl) { Rename(); return; }

            if (!B.Locked && (tag == "title" || tag == "kind" || tag == "body" || tag == "content" || tag == "" || tag == "item" || tag == "row"))
            {
                drag = DragMode.Move;
                dragStartCursor = Cursor.Position;
                dragStartRect = Bounds;
                dragMoved = false;
                Capture = true;
                SetLiveDuringDrag(true);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (tilePressIndex >= 0)
            {
                int idx = tilePressIndex;
                bool dragged = tileDragged;
                tilePressIndex = -1;
                tileDragged = false;
                if (!dragged && idx >= 0 && idx < B.Items.Count)
                {
                    long now = Environment.TickCount;
                    if (now - lastTileOpenTick > 400)
                    {
                        lastTileOpenTick = now;
                        string p = B.Items[idx].Path;
                        if (System.IO.Directory.Exists(p)) OpenItemFolder(idx);
                        else Native.ShellOpen(p);
                    }
                }
            }
            if (drag != DragMode.None)
            {
                if (drag == DragMode.Move && !dragMoved)
                {
                    if (B.Collapsed) AnimateCollapse(false);
                    else if (pressedTag == "kind") ShowMenu(HeaderMenuPoint());
                }
                FinishDrag();
            }
            pressedTag = ""; pressedIndex = -1;
        }

        void DoDrag(MouseEventArgs e)
        {
            Point cur = Cursor.Position;
            int dx = cur.X - dragStartCursor.X;
            int dy = cur.Y - dragStartCursor.Y;
            if (!dragMoved && (Math.Abs(dx) > 3 || Math.Abs(dy) > 3)) dragMoved = true;
            if (!dragMoved) return;

            if (drag == DragMode.Move)
            {
                int nx = dragStartRect.X + dx;
                int ny = dragStartRect.Y + dy;
                if (App.Ws.Set.Snap)
                {
                    Rectangle work = Screen.FromPoint(cur).WorkingArea;
                    int snap = Util.RoundI(10 * Theme.S);
                    int[] xs = { work.Left + Glass.Margin, work.Right - Width - Glass.Margin, dragStartRect.X };
                    int[] ys = { work.Top + Glass.Margin, work.Bottom - Height - Glass.Margin, dragStartRect.Y };
                    for (int i = 0; i < App.Windows.Count; i++)
                    {
                        BoardWindow o = App.Windows[i];
                        if (o == this || o.B == null) continue;
                        Rectangle or = o.Bounds;
                        xs = AddInt(xs, or.Left - Glass.Margin);
                        xs = AddInt(xs, or.Right - Width + Glass.Margin);
                        ys = AddInt(ys, or.Top - Glass.Margin);
                        ys = AddInt(ys, or.Bottom - Height + Glass.Margin);
                    }
                    nx = Util.Snap(nx, xs, snap);
                    ny = Util.Snap(ny, ys, snap);
                }
                B.X = nx; B.Y = ny;
                SetBounds(nx, ny, Width, Height);
                Push();
                return;
            }

            int l = dragStartRect.Left, t = dragStartRect.Top, r = dragStartRect.Right, btm = dragStartRect.Bottom;
            if (drag == DragMode.L || drag == DragMode.TL || drag == DragMode.BL) l += dx;
            if (drag == DragMode.R || drag == DragMode.TR || drag == DragMode.BR) r += dx;
            if (drag == DragMode.T || drag == DragMode.TL || drag == DragMode.TR) t += dy;
            if (drag == DragMode.B || drag == DragMode.BL || drag == DragMode.BR) btm += dy;

            int minW = Util.RoundI(240 * Theme.S), minH = Util.RoundI(150 * Theme.S);
            if (r - l < minW) { if (drag == DragMode.L || drag == DragMode.TL || drag == DragMode.BL) l = r - minW; else r = l + minW; }
            if (btm - t < minH) { if (drag == DragMode.T || drag == DragMode.TL || drag == DragMode.TR) t = btm - minH; else btm = t + minH; }

            Rectangle nb = new Rectangle(l, t, r - l, btm - t);
            if (nb.Width != B.W || nb.Height != B.H || nb.X != B.X || nb.Y != B.Y)
            {
                B.Bounds = nb;
                B.Collapsed = false;
                collapseT = 0f;
                Bounds = nb;
                EnsureCanvas();
                dirty = true;
                Render(); Push();
            }
        }

        static int[] AddInt(int[] arr, int v)
        {
            int[] r = new int[arr.Length + 1];
            Array.Copy(arr, r, arr.Length);
            r[arr.Length] = v;
            return r;
        }

        void FinishDrag()
        {
            bool wasMove = drag == DragMode.Move;
            drag = DragMode.None;
            Capture = false;
            SetLiveDuringDrag(false);
            B.Clamp(App.ScreenRects());
            Bounds = new Rectangle(B.X, B.Y, B.W, WindowHeight());
            EnsureCanvas();
            dirty = true;
            App.Save();
            if (wasMove) App.RaiseBoard(this);
        }

        public void ApplyTopMode()
        {
            if (B.Pinned || App.Ws.Set.AlwaysTop) Native.KeepAtTop(Handle);
            else Native.KeepAtBottom(Handle);
            ApplyAccent();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            float step = -e.Delta / 120f * Util.RoundI(46 * Theme.S);
            scrollTarget += step;
            scrollTarget = ClampScroll(scrollTarget);
            scrollAnimT0 = Environment.TickCount;
            dirty = true;
        }

        float ClampScroll(float v)
        {
            float max = Math.Max(0f, contentHeight - contentRect.Height);
            return Util.ClampF(v, 0f, max);
        }

        int contentHeight;       // 由绘制计算
        Rectangle contentRect;   // 由绘制计算

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            // 双击已在 OnMouseDown 里按时间窗自行判定，这里只吞掉原生事件避免二次触发
            base.OnMouseDoubleClick(e);
        }

        // ---------- 右键菜单 ----------
        Point HeaderMenuPoint()
        {
            return new Point(Width - Util.RoundI(34 * Theme.S), Util.RoundI(52 * Theme.S));
        }

        void ShowMenu(Point clientPoint)
        {
            if (menu != null) { menu.Close(); menu = null; }
            Point sp = PointToScreen(clientPoint);
            List<MenuForm.Item> items = new List<MenuForm.Item>();
            items.Add(new MenuForm.Item("重命名", delegate { Rename(); }, Glyph.Note));
            items.Add(new MenuForm.Item("换一个配色", delegate { B.Accent = B.Accent >= Palette.Count - 1 ? -1 : B.Accent + 1; dirty = true; App.Save(); }, Glyph.Image));
            items.Add(new MenuForm.Item(B.Pinned ? "取消置顶" : "总在最前", delegate { B.Pinned = !B.Pinned; ApplyTopMode(); dirty = true; App.Save(); }, Glyph.Pin));
            items.Add(new MenuForm.Item(B.Locked ? "解锁移动" : "锁定位置", delegate { B.Locked = !B.Locked; dirty = true; App.Save(); }, Glyph.Check));
            items.Add(new MenuForm.Item(B.Collapsed ? "展开" : "收起", delegate { AnimateCollapse(!B.Collapsed); }, Glyph.Chevron));
            items.Add(new MenuForm.Item("复制这块看板", delegate { App.DuplicateBoard(B); }, Glyph.Copy));
            items.Add(new MenuForm.Item("删除", delegate { App.RemoveBoard(this); }, Glyph.Close, true));
            menu = new MenuForm(items, Size);
            menu.ShowAt(sp);
            menu.FormClosed += delegate { menu = null; };
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_DROPFILES)
            {
                IntPtr h = m.WParam;
                uint n = Native.DragQueryFile(h, 0xFFFFFFFF, null, 0);
                List<string> files = new List<string>();
                for (uint i = 0; i < n; i++)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder(512);
                    Native.DragQueryFile(h, i, sb, 512);
                    if (sb.Length > 0) files.Add(sb.ToString());
                }
                Native.DragFinish(h);
                OnFilesDropped(files);
                return;
            }
            base.WndProc(ref m);
        }

        static bool IsOnDesktop(string p)
        {
            try
            {
                string dir = System.IO.Path.GetDirectoryName(p.TrimEnd('\\'));
                string d1 = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string d2 = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
                return string.Equals(dir, d1, StringComparison.OrdinalIgnoreCase) || string.Equals(dir, d2, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        static bool IsUnderLiuli(string p)
        {
            try { return p.StartsWith(Icons.LiuliRoot + "\\", StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }

        void OnFilesDropped(List<string> files)
        {
            if (files == null || files.Count == 0) return;
            Log.Info("drop", "收到拖入 " + files.Count + " 项，看板 " + B.Title);
            if (B.Kind == BoardKind.Files)
            {
                int moved = 0;
                for (int i = 0; i < files.Count; i++)
                {
                    string p = files[i];
                    string dest = null;
                    if (IsOnDesktop(p) && !IsUnderLiuli(p)) dest = Icons.MoveToBoard(p, B);
                    if (dest != null) { Icons.RecordMove(p, dest); p = dest; moved++; }
                    Item it = new Item(System.IO.Path.GetFileNameWithoutExtension(p), p);
                    if (System.IO.Directory.Exists(p)) it.Text = new System.IO.DirectoryInfo(p).Name;
                    else it.Text = System.IO.Path.GetFileNameWithoutExtension(p);
                    B.Items.Add(it);
                }
                dirty = true; App.Save();
                string dir = Icons.BindFolder(B, false);
                App.Toast(moved > 0 ? "已移入 " + moved + " 个原件到 " + dir : "已收进 " + files.Count + " 个入口");
            }
            else if (B.Kind == BoardKind.Note)
            {
                for (int i = 0; i < files.Count; i++) B.Text += (B.Text.Length > 0 ? "\n" : "") + files[i];
                dirty = true; App.Save();
            }
            else App.Toast("这块看板只收" + Kinds.Name(B.Kind) + "内容");
        }

        // ---------- 动作 ----------
        void Rename()
        {
            string v = EditorForm.AskText(this, "看板名称", B.Title, false);
            if (v != null && v.Trim().Length > 0) { B.Title = v.Trim(); dirty = true; App.Save(); }
        }

        void EditNote()
        {
            string v = EditorForm.AskText(this, "便签内容", B.Text, true);
            Log.Info("board", "edit note result=" + (v == null ? "null" : v.Length.ToString()) + " old=" + B.Text.Length);
            if (v != null) { B.Text = v; dirty = true; App.Save(); Log.Info("board", "note now=" + B.Text.Length + " id=" + B.Id); }
        }

        void EditItem(int idx)
        {
            if (idx < 0 || idx >= B.Items.Count) return;
            string v = EditorForm.AskText(this, B.Kind == BoardKind.Code ? "代码片段" : "待办内容", B.Items[idx].Text, B.Kind == BoardKind.Code);
            if (v == null) return;
            if (B.Kind == BoardKind.Todo && v.Trim().Length == 0) { B.Items.RemoveAt(idx); }
            else B.Items[idx].Text = v;
            dirty = true; App.Save();
        }

        void AddItem()
        {
            if (B.Kind == BoardKind.Todo)
            {
                string v = EditorForm.AskText(this, "新增待办", "", false);
                if (v != null && v.Trim().Length > 0) { B.Items.Add(new Item(v.Trim())); scrollTarget = 99999; dirty = true; App.Save(); }
            }
            else if (B.Kind == BoardKind.Files)
            {
                using (OpenFileDialog d = new OpenFileDialog())
                {
                    d.Title = "选择要收进看板的文件";
                    d.Multiselect = true;
                    if (d.ShowDialog() == DialogResult.OK)
                    {
                        for (int i = 0; i < d.FileNames.Length; i++)
                        {
                            string p = d.FileNames[i];
                            B.Items.Add(new Item(System.IO.Path.GetFileNameWithoutExtension(p), p));
                        }
                        dirty = true; App.Save();
                    }
                }
            }
            else if (B.Kind == BoardKind.Code)
            {
                string v = EditorForm.AskText(this, "新增代码片段", "", true);
                if (v != null && v.Trim().Length > 0) { B.Items.Add(new Item(v)); dirty = true; App.Save(); }
            }
        }

        void ToggleItem(int idx)
        {
            if (idx < 0 || idx >= B.Items.Count) return;
            B.Items[idx].Done = !B.Items[idx].Done;
            dirty = true; App.Save();
        }

        void StartItemDrag(int idx)
        {
            if (idx < 0 || idx >= B.Items.Count) return;
            string p = B.Items[idx].Path;
            if (!System.IO.File.Exists(p) && !System.IO.Directory.Exists(p)) return;
            try
            {
                DataObject data = new DataObject();
                data.SetData(DataFormats.FileDrop, new string[] { p });
                DragDropEffects eff = DoDragDrop(data, DragDropEffects.Move | DragDropEffects.Copy);
                // 被移到了别处（桌面/Explorer/另一个看板）：原条目移除，避免留下失效路径
                if (eff == DragDropEffects.Move)
                {
                    if (idx < B.Items.Count && B.Items[idx].Path == p) B.Items.RemoveAt(idx);
                    dirty = true; App.Save();
                    Toast("已移出 1 项");
                }
            }
            catch (Exception ex) { Log.Warn("drop", "拖出失败 " + ex.Message); }
        }

        void OpenItemFolder(int idx)
        {
            if (idx < 0 || idx >= B.Items.Count) return;
            string p = B.Items[idx].Path;
            if (p.Length > 0) Native.ShellOpenFolder(p);
        }

        void EditCity()
        {
            string v = EditorForm.AskText(this, "城市名称（内置：道孚/康定/成都/拉萨/北京/上海/广州）", B.City, false);
            if (v == null) return;
            v = v.Trim();
            if (v.Length == 0) return;
            double lat, lon;
            if (WeatherService.Lookup(v, out lat, out lon))
            {
                B.City = v; B.Lat = lat; B.Lon = lon;
                dirty = true; App.Save();
                App.Weather.Refresh(true);
                App.Toast("天气位置已切到 " + v);
            }
            else
            {
                App.Toast("没找到这个城市，试试内置的六个");
            }
        }

        void Toast(string s) { App.Toast(s); }

        public void CloseAnimated()
        {
            closingTick = Environment.TickCount;
            Timer t = new Timer();
            t.Interval = 140;
            t.Tick += delegate (object s, EventArgs e)
            {
                t.Stop(); t.Dispose();
                Close();
            };
            t.Start();
        }
    }
}
