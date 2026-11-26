// Liuli · 桌面底座：默认是底部应用 Dock（图标 + 新建/整理/设置），关掉 Dock 回到三键控制条。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace Liuli
{
    sealed class BarWindow : Form
    {
        int hoverBtn = -1;
        int hoverApp = -1;
        bool down;
        bool moved;
        Point downCursor;
        Point startLoc;
        Bitmap canvas;
        DropTarget oleDrop;

        public BarWindow()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = Color.Black;
            if (DockMode)
            {
                string apps = App.Ws.Set.DockApps;
                if (apps.Length == 0)
                {
                    App.Ws.Set.DockApps = Join(DefaultApps());
                    App.Save();
                }
            }
            ClientSize = ComputeSize();
            ApplyPosition();
            EnsureCanvas();
        }

        bool DockMode { get { return App.Ws != null && App.Ws.Set.DockOn; } }

        public void RefreshMode()
        {
            ClientSize = ComputeSize();
            ApplyPosition();
            EnsureCanvas();
            Render();
        }

        Size ComputeSize()
        {
            float s = Theme.S;
            if (!DockMode) return new Size(Util.RoundI(150 * s), Util.RoundI(54 * s));
            int n = Apps().Count;
            int pad = Util.RoundI(16 * s);
            int icon = Util.RoundI(40 * s);
            int gap = Util.RoundI(4 * s);
            int sep = n > 0 ? Util.RoundI(10 * s) : 0;
            int btn = Util.RoundI(36 * s), bgap = Util.RoundI(5 * s);
            int w = pad + n * (icon + gap) + sep + btn * 3 + bgap * 2 + pad;
            return new Size(Math.Max(Util.RoundI(190 * s), w), Util.RoundI(84 * s));
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
            if (DockMode)
            {
                int bottom = work.Bottom - Util.RoundI(12 * Theme.S);
                try
                {
                    IntPtr taskbar = Native.FindWindow("Shell_TrayWnd", null);
                    Native.RECT tr;
                    if (taskbar != IntPtr.Zero && Native.GetWindowRect(taskbar, out tr))
                    {
                        int top = tr.Top;
                        if (top > work.Top + Util.RoundI(40 * Theme.S) && top < work.Bottom)
                            bottom = top - Util.RoundI(12 * Theme.S);
                    }
                }
                catch { }
                Bounds = new Rectangle(work.X + (work.Width - Width) / 2, bottom - Height, Width, Height);
                Native.KeepAtTop(Handle);
                return;
            }
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

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                DropTarget.EnsureOle();
                oleDrop = new DropTarget("Dock", OnDockDrop, DropTarget.COPY);
                DropTarget.RegisterDragDrop(Handle, oleDrop);
            }
            catch (Exception ex) { Log.Warn("dock", "注册拖放失败 " + ex.Message); }
        }

        // ---------- Dock 应用 ----------
        public static string Join(List<string> list)
        {
            if (list == null) return "";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null || list[i].Trim().Length == 0) continue;
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(list[i].Trim());
            }
            return sb.Length == 0 ? "none" : sb.ToString();
        }

        public static List<string> DefaultApps()
        {
            List<string> list = new List<string>();
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Microsoft\\Internet Explorer\\Quick Launch\\User Pinned\\TaskBar");
                if (Directory.Exists(dir))
                {
                    foreach (string p in Directory.GetFiles(dir, "*.lnk"))
                    {
                        if (list.Count >= 6) break;
                        list.Add(p);
                    }
                }
            }
            catch { }
            if (list.Count < 2)
            {
                string exe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
                if (File.Exists(exe)) list.Add(exe);
            }
            return list;
        }

        List<string> Apps()
        {
            List<string> list = new List<string>();
            if (App.Ws == null) return list;
            string raw = App.Ws.Set.DockApps;
            if (raw == null || raw.Trim().Length == 0) return DefaultApps();
            if (raw.Trim() == "none") return list;
            string[] lines = raw.Split('\n');
            for (int i = 0; i < lines.Length && list.Count < 12; i++)
            {
                string p = lines[i].Trim();
                if (p.Length > 0) list.Add(p);
            }
            return list;
        }

        void OnDockDrop(string[] files)
        {
            if (files == null || files.Length == 0) return;
            List<string> list = Apps();
            int added = 0;
            for (int i = 0; i < files.Length; i++)
            {
                string p = files[i];
                if (p == null || p.Trim().Length == 0 || !File.Exists(p) && !Directory.Exists(p)) continue;
                bool has = false;
                for (int k = 0; k < list.Count; k++) if (string.Equals(list[k], p, StringComparison.OrdinalIgnoreCase)) { has = true; break; }
                if (has) continue;
                list.Add(p);
                added++;
            }
            if (added == 0) return;
            App.Ws.Set.DockApps = Join(list);
            App.Save();
            RefreshMode();
            App.Toast("已固定 " + added + " 个到 Dock");
        }

        void RemoveApp(string path)
        {
            List<string> list = Apps();
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i], path, StringComparison.OrdinalIgnoreCase)) { list.RemoveAt(i); break; }
            }
            App.Ws.Set.DockApps = Join(list);
            App.Save();
            RefreshMode();
            App.Toast("已从 Dock 移除");
        }

        void Launch(string path)
        {
            try
            {
                if (Directory.Exists(path) || File.Exists(path)) Native.ShellOpen(path);
                else App.Toast("找不到：" + Path.GetFileName(path));
            }
            catch (Exception ex) { Log.Warn("dock", "打开失败 " + ex.Message); }
        }

        // ---------- 布局 ----------
        Rectangle Body()
        {
            float s = Theme.S;
            if (!DockMode) return new Rectangle(Util.RoundI(6 * s), Util.RoundI(6 * s), Width - Util.RoundI(12 * s), Height - Util.RoundI(12 * s));
            return new Rectangle(Util.RoundI(6 * s), Util.RoundI(22 * s), Width - Util.RoundI(12 * s), Height - Util.RoundI(28 * s));
        }

        Rectangle[] AppRects()
        {
            List<string> apps = Apps();
            int n = apps.Count;
            Rectangle[] rs = new Rectangle[n];
            if (n == 0 || !DockMode) return rs;
            Rectangle body = Body();
            float s = Theme.S;
            int icon = Util.RoundI(40 * s);
            int gap = Util.RoundI(4 * s);
            int x = body.X + Util.RoundI(14 * s);
            int y = body.Y + (body.Height - icon) / 2;
            for (int i = 0; i < n; i++)
            {
                rs[i] = new Rectangle(x, y, icon, icon);
                x += icon + gap;
            }
            return rs;
        }

        Rectangle[] Buttons()
        {
            float s = Theme.S;
            int bs = Util.RoundI(DockMode ? 36 : 38);
            int gap = Util.RoundI(6 * s);
            int right = Width - Util.RoundI(18 * s);
            int y = Body().Y + (Body().Height - bs) / 2;
            return new Rectangle[] {
                new Rectangle(right - bs * 3 - gap * 2, y, bs, bs),
                new Rectangle(right - bs * 2 - gap, y, bs, bs),
                new Rectangle(right - bs, y, bs, bs)
            };
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
                Rectangle body = Body();
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

                if (!DockMode)
                {
                    for (int i = 0; i < 3; i++)
                        g.FillEllipse(new SolidBrush(Color.FromArgb(80, Theme.TextSub)),
                            body.X + Util.RoundI(8 * Theme.S), body.Y + body.Height / 2 - Util.RoundI(8 * Theme.S) + i * Util.RoundI(6 * Theme.S),
                            Util.RoundI(3 * Theme.S), Util.RoundI(3 * Theme.S));
                }

                Rectangle[] apps = AppRects();
                List<string> list = Apps();
                for (int i = 0; i < apps.Length; i++)
                {
                    if (hoverApp == i)
                    {
                        using (GraphicsPath p = Theme.Round(new Rectangle(apps[i].X - Util.RoundI(3 * Theme.S), apps[i].Y - Util.RoundI(3 * Theme.S), apps[i].Width + Util.RoundI(6 * Theme.S), apps[i].Height + Util.RoundI(6 * Theme.S)), Util.RoundI(10 * Theme.S)))
                        using (SolidBrush b = new SolidBrush(Theme.Hover)) g.FillPath(b, p);
                    }
                    Icons.Draw(g, list[i], apps[i]);
                }

                // Dock 分隔线
                if (apps.Length > 0)
                {
                    int sx = apps[apps.Length - 1].Right + Util.RoundI(10 * Theme.S);
                    int top = body.Y + Util.RoundI(12 * Theme.S), bottom = body.Bottom - Util.RoundI(12 * Theme.S);
                    using (Pen pen = new Pen(Color.FromArgb(44, 255, 255, 255), 1f)) g.DrawLine(pen, sx, top, sx, bottom);
                }

                Rectangle[] rs = Buttons();
                Glyph[] gl = { Glyph.Plus, Glyph.Broom, Glyph.Gear };
                for (int i = 0; i < 3; i++)
                {
                    Glass.Button(g, rs[i], gl[i], hoverBtn == i, false, Theme.Accent);
                }

                // 悬停标签（窗口顶部留出的窄条里）
                int showApp = hoverApp;
                if (showApp >= 0 && showApp < list.Count)
                {
                    string name = Directory.Exists(list[showApp]) ? new DirectoryInfo(list[showApp]).Name : Path.GetFileNameWithoutExtension(list[showApp]);
                    if (name.Length > 16) name = name.Substring(0, 15) + "…";
                    DrawTag(g, name, apps[showApp].X + apps[showApp].Width / 2);
                }
                else if (hoverBtn >= 0)
                {
                    string[] tips = { "新建看板", "整理桌面", "设置" };
                    DrawTag(g, tips[hoverBtn], rs[hoverBtn].X + rs[hoverBtn].Width / 2);
                }

                Native.PushLayered(Handle, canvas, Location, (byte)255);
            }
            finally { g.Dispose(); }
        }

        void DrawTag(Graphics g, string text, int cx)
        {
            float s = Theme.S;
            Font f = Theme.Font(9f, FontStyle.Regular);
            SizeF sz = g.MeasureString(text, f);
            int w = Util.RoundI((sz.Width + 18 * s));
            int h = Util.RoundI(18 * s);
            int x = Util.Clamp(cx - w / 2, Util.RoundI(4 * s), Width - w - Util.RoundI(4 * s));
            Rectangle r = new Rectangle(x, Util.RoundI(1 * s), w, h);
            using (GraphicsPath p = Theme.Round(r, h / 2))
            using (SolidBrush b = new SolidBrush(Theme.Light ? Color.FromArgb(220, 250, 250, 252) : Color.FromArgb(220, 18, 22, 30))) g.FillPath(b, p);
            Theme.TextMiddle(g, text, f, Theme.TextMain, r, StringAlignment.Center);
        }

        // ---------- 交互 ----------
        void UpdateHover(Point p)
        {
            int hb = -1, ha = -1;
            Rectangle[] rs = Buttons();
            for (int i = 0; i < rs.Length; i++) if (rs[i].Contains(p)) hb = i;
            Rectangle[] apps = AppRects();
            for (int i = 0; i < apps.Length; i++) if (apps[i].Contains(p)) ha = i;
            if (hb != hoverBtn || ha != hoverApp) { hoverBtn = hb; hoverApp = ha; Render(); }
            Cursor = (hb >= 0 || ha >= 0) ? Cursors.Hand : (DockMode ? Cursors.Default : Cursors.SizeAll);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            App.KickAnimation();
            if (down && !DockMode)
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

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (hoverBtn >= 0 || hoverApp >= 0) { hoverBtn = -1; hoverApp = -1; Render(); }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Right && DockMode)
            {
                Rectangle[] apps = AppRects();
                List<string> list = Apps();
                for (int i = 0; i < apps.Length; i++)
                {
                    if (!apps[i].Contains(e.Location)) continue;
                    List<MenuForm.Item> items = new List<MenuForm.Item>();
                    string path = list[i];
                    items.Add(new MenuForm.Item("从 Dock 移除", delegate { RemoveApp(path); }, Glyph.Close, true));
                    new MenuForm(items, new Size(Util.RoundI(170 * Theme.S), Util.RoundI(8 * Theme.S))).ShowAt(PointToScreen(e.Location));
                    return;
                }
            }
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
            Rectangle[] apps = AppRects();
            List<string> list = Apps();
            for (int i = 0; i < apps.Length; i++)
            {
                if (!apps[i].Contains(e.Location)) continue;
                Launch(list[i]);
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
    }
}
