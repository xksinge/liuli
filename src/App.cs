// Liuli · 控制器：工作区状态、窗口生命周期、托盘、热键、定时器、设置联动。
// 单 UI 线程：所有窗口共用一个消息循环，模型读写没有跨线程问题；壁纸烘焙走后台。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Liuli
{
    static class App
    {
        public const int HotkeyToggle = 1;
        public const int HotkeySettings = 2;
        public const uint MOD_ALT = 1, MOD_CONTROL = 2;
        public const string ActivateMessageName = "Liuli.Activate.v1";

        public static Workspace Ws;
        public static readonly List<BoardWindow> Windows = new List<BoardWindow>();
        public static SettingsForm Settings;
        public static BarWindow Bar;
        public static WeatherService Weather = new WeatherService();
        public static StatsNow StatsNow = new StatsNow();
        public static AppHost Host;
        public static int ActivateMessage;

        static NotifyIcon tray;
        static IntPtr trayIconHandle;
        static Timer fast, slow;
        static long slowCount;
        static bool quitting;
        static bool iconsHiddenByUs;
        static bool allHidden;

        // ---------- 启动 ----------
        public static void Boot()
        {
            Paths.Ensure();
            Rectangle[] screens = ScreenRects();
            Ws = Store.Load(screens);
            Theme.S = DpiScale();
            Theme.GlassPct = Ws.Set.GlassPct;
            Theme.BlurPct = Ws.Set.BlurPct;
            Theme.Radius = Ws.Set.Radius;
            Theme.Live = Ws.Set.LiveGlass;

            Backdrop.Start();
            if (Ws.Set.WallMode != "system") { WallEngine.Apply(); Backdrop.Rebuild(); }
            Theme.Accent = Backdrop.Accent;
            bool light = Ws.Set.Tone == "light" || (Ws.Set.Tone != "dark" && Backdrop.Light);
            Theme.Light = light;
            Theme.ApplyTone();
            Log.Info("theme", "light=" + light + " mean=" + Hex(Backdrop.Mean) + " accent=" + Hex(Theme.Accent) + " text=" + Hex(Theme.TextMain) + " dpi=" + Theme.S);
            SyncAutoStart();

            StatsNow = SysStats.Sample();

            // 天气位置取第一块状态看板
            for (int i = 0; i < Ws.Boards.Count; i++)
            {
                Board b = Ws.Boards[i];
                if (b.Kind == BoardKind.Status) { Weather.Configure(b.City, b.Lat, b.Lon); break; }
            }
            Weather.LoadCache();
            Weather.Refresh(false);

            Ws.Boards.Sort(delegate (Board a, Board c) { return a.Z.CompareTo(c.Z); });
            for (int i = 0; i < Ws.Boards.Count; i++) CreateWindow(Ws.Boards[i], false);
            ApplyTopModeAll();

            Bar = new BarWindow();
            Bar.EnsureCanvas();
            if (Ws.Set.ShowBar) { Bar.Show(); Bar.Render(); Log.Info("bar", "show=" + Bar.Visible + " rect=" + Bar.Bounds); }

            Host = new AppHost();
            ActivateMessage = Native.RegisterWindowMessage(ActivateMessageName);

            CreateTray();
            SyncDesktopIconState();

            fast = new Timer();
            fast.Interval = 15;
            fast.Tick += delegate { FastTick(); };
            fast.Start();

            slow = new Timer();
            slow.Interval = 1000;
            slow.Tick += delegate { SlowTick(); };
            slow.Start();

            if (Ws.Set.FirstRun)
            {
                Ws.Set.FirstRun = false;
                Save();
                Toast("按住标题栏拖一拖，玻璃就活了");
            }
            Log.Info("app", "启动完成，看板 " + Ws.Boards.Count + " 块，内存 " + (Environment.WorkingSet / 1048576) + "MB");
        }

        static string Hex(Color c) { return string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B); }

        public static int DpiScale()
        {
            try
            {
                uint dpi = Native.GetDpiForSystem();
                int s = (int)Math.Round(dpi / 96.0 * 2) / 2;
                return Math.Max(1, s);
            }
            catch { return 1; }
        }

        public static Rectangle[] ScreenRects()
        {
            Screen[] sc = Screen.AllScreens;
            Rectangle[] r = new Rectangle[sc.Length];
            for (int i = 0; i < sc.Length; i++) r[i] = sc[i].Bounds;
            return r;
        }

        // ---------- 窗口 ----------
        public static void CreateWindow(Board b, bool animate)
        {
            BoardWindow w = new BoardWindow(b);
            Windows.Add(w);
            w.Show();
            if (!animate) { w.MarkDirty(); w.Tick(); }
            else { w.MarkDirty(); RequestFrames(); }
        }

        public static void RemoveBoard(BoardWindow w)
        {
            if (w == null) return;
            Ws.Boards.Remove(w.B);
            Windows.Remove(w);
            w.CloseAnimated();
            Save();
            Toast("看板已移除");
        }

        public static void DuplicateBoard(Board b)
        {
            Board c = b.Clone();
            c.NewId();
            c.X += Util.RoundI(26 * Theme.S);
            c.Y += Util.RoundI(26 * Theme.S);
            c.Z = Ws.Boards.Count + 1;
            Ws.Boards.Add(c);
            CreateWindow(c, true);
            Save();
            Toast("已复制到旁边");
        }

        public static void AddBoard(BoardKind kind, Point near)
        {
            Board b = new Board();
            b.NewId();
            b.Kind = kind;
            b.Title = DefaultTitle(kind);
            Size size = DefaultSize(kind);
            b.W = size.Width; b.H = size.Height;
            Rectangle work = Screen.FromPoint(near).WorkingArea;
            b.X = Util.Clamp(near.X - b.W / 2, work.X + 8, work.Right - b.W - 8);
            b.Y = Util.Clamp(near.Y - b.H / 2, work.Y + 8, work.Bottom - b.H - 8);
            b.Z = Ws.Boards.Count + 1;
            if (kind == BoardKind.Status)
            {
                b.City = Weather.City;
                b.Lat = Weather.Lat;
                b.Lon = Weather.Lon;
            }
            Ws.Boards.Add(b);
            CreateWindow(b, true);
            Save();
            Toast("新建了「" + b.Title + "」");
        }

        static string DefaultTitle(BoardKind k)
        {
            switch (k)
            {
                case BoardKind.Todo: return "今日清单";
                case BoardKind.Files: return "文件入口";
                case BoardKind.Code: return "代码夹";
                case BoardKind.Status: return "桌面状态";
                case BoardKind.Clock: return "时间";
                default: return "便签";
            }
        }

        static Size DefaultSize(BoardKind k)
        {
            if (k == BoardKind.Clock) return new Size(320, 180);
            if (k == BoardKind.Status) return new Size(330, 300);
            if (k == BoardKind.Code) return new Size(360, 320);
            if (k == BoardKind.Files) return new Size(340, 330);
            if (k == BoardKind.Todo) return new Size(310, 320);
            return new Size(330, 300);
        }

        public static void ShowKindMenu(Point screen)
        {
            List<MenuForm.Item> items = new List<MenuForm.Item>();
            for (int i = 0; i < 6; i++)
            {
                BoardKind k = (BoardKind)i;
                items.Add(new MenuForm.Item(Kinds.Name(k) + " · " + Kinds.Hint(k),
                    delegate { AddBoard(k, screen); }, Kinds.Icon(k)));
            }
            MenuForm m = new MenuForm(items, new Size(200, 200));
            m.ShowAt(screen);
        }

        public static void RefreshBoard(Board b)
        {
            for (int i = 0; i < Windows.Count; i++) if (Windows[i].B == b) { Windows[i].MarkDirty(); }
            RequestFrames();
        }

        public static void RefreshAllBoards()
        {
            for (int i = 0; i < Windows.Count; i++) Windows[i].MarkDirty();
            RequestFrames();
        }

        public static void MarkDirty() { RefreshAllBoards(); }

        public static void MarkStatusDirty()
        {
            for (int i = 0; i < Windows.Count; i++)
                if (Windows[i].B.Kind == BoardKind.Status) Windows[i].MarkDirty();
            RequestFrames();
        }

        public static void ApplyTopModeAll()
        {
            for (int i = 0; i < Windows.Count; i++) Windows[i].ApplyTopMode();
            if (Bar != null) Bar.Render();
        }

        public static void RaiseBoard(BoardWindow w)
        {
            // 置顶模式下把刚拖过的看板提到最前，普通模式保持贴桌面
            if (w.B.Pinned || Ws.Set.AlwaysTop) Native.KeepAtTop(w.Handle);
            else Native.KeepAtBottom(w.Handle);
        }

        public static void RescueBoards()
        {
            Rectangle[] screens = ScreenRects();
            for (int i = 0; i < Windows.Count; i++)
            {
                Board b = Windows[i].B;
                b.Clamp(screens);
                Windows[i].SyncBounds();
                Windows[i].MarkDirty();
            }
            Save();
            RequestFrames();
            Toast("看板已收回屏幕内");
        }

        // ---------- 定时 ----------
        static void FastTick()
        {
            bool busy = false;
            for (int i = 0; i < Windows.Count; i++)
            {
                BoardWindow w = Windows[i];
                if (w.IsAnimating() || w.IsDragging) { busy = true; w.Tick(); }
                else if (w.B.Kind == BoardKind.Clock) w.Tick();
                else w.TickIfDirty();
            }
            if (fast != null)
            {
                int want = busy ? 16 : 200;
                if (fast.Interval != want) fast.Interval = want;
            }
        }

        public static void KickAnimation()
        {
            if (fast != null && fast.Interval > 16) fast.Interval = 16;
        }

        static void SlowTick()
        {
            slowCount++;
            if (slowCount % 3 == 0)
            {
                StatsNow = SysStats.Sample();
                MarkStatusDirty();
            }
            Weather.Refresh(false);
            if (slowCount % 10 == 0)
            {
                if (Backdrop.Tick())
                {
                    ApplyLookFromBackdrop();
                    RefreshAllBoards();
                    if (Bar != null) Bar.Render();
                    if (Settings != null) Settings.Invalidate();
                }
            }
            if (slowCount % 9 == 0) KeepZOrder();
            WallEngine.Tick();
        }

        static void KeepZOrder()
        {
            for (int i = 0; i < Windows.Count; i++)
            {
                BoardWindow w = Windows[i];
                if (w.IsDragging) continue;
                if (w.B.Pinned || Ws.Set.AlwaysTop) Native.KeepAtTop(w.Handle);
                else Native.KeepAtBottom(w.Handle);
            }
            if (Bar != null && Bar.Visible) Native.KeepAtTop(Bar.Handle);
        }

        public static void RequestFrames()
        {
            // 定时器常驻，这里只保证立刻画一帧
            for (int i = 0; i < Windows.Count; i++)
                if (Windows[i].Visible && Windows[i].IsAnimating()) Windows[i].Tick();
            if (Settings != null) Settings.Invalidate();
        }

        // ---------- 外观 ----------
        public static void RefreshLook(bool rebuildBackdrop) { RefreshLook(rebuildBackdrop, false); }

        public static void RefreshLook(bool rebuildBackdrop, bool reapplyAccent)
        {
            Theme.S = DpiScale();
            Theme.GlassPct = Util.Clamp(Ws.Set.GlassPct, 0, 100);
            Theme.BlurPct = Util.Clamp(Ws.Set.BlurPct, 0, 100);
            Theme.Radius = Util.Clamp(Ws.Set.Radius, 10, 30);
            Theme.Live = Ws.Set.LiveGlass;
            if (rebuildBackdrop) Backdrop.Rebuild();
            ApplyLookFromBackdrop();
            Theme.Live = Ws.Set.LiveGlass;

            for (int i = 0; i < Windows.Count; i++)
            {
                Windows[i].ApplyAccent();
                Windows[i].SyncBounds();
                Windows[i].MarkDirty();
            }
            if (Bar != null) { Bar.Render(); }
            if (Settings != null) Settings.Invalidate();
            Save();
            RequestFrames();
        }

        static void ApplyLookFromBackdrop()
        {
            Theme.Accent = Backdrop.Accent;
            bool light = Ws.Set.Tone == "light" || (Ws.Set.Tone != "dark" && Backdrop.Light);
            if (Theme.Light != light) Theme.Light = light;
            Theme.ApplyTone();
        }

        // ---------- 托盘 ----------
        static void CreateTray()
        {
            tray = new NotifyIcon();
            tray.Text = V.Name;
            tray.Icon = MakeIcon();
            tray.Visible = true;
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("打开设置", null, delegate { OpenSettings(); });
            menu.Items.Add("新建看板…", null, delegate { ShowKindBarMenu(); });
            menu.Items.Add("一键整理桌面图标", null, delegate { OrganizeDesktop(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("显示/隐藏所有看板", null, delegate { ToggleAllBoards(); });
            menu.Items.Add("显示/隐藏桌面图标", null, delegate { SetDesktopIconsHidden(!Ws.Set.HideDesktopIcons); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("退出 Liuli", null, delegate { Quit(); });
            tray.ContextMenuStrip = menu;
            tray.DoubleClick += delegate { OpenSettings(); };
        }

        static void ShowKindBarMenu()
        {
            Point p = Cursor.Position;
            ShowKindMenu(new Point(p.X - 100, p.Y - 100));
        }

        static Icon MakeIcon()
        {
            Bitmap b = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(b))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (GraphicsPath p = Theme.Round(new Rectangle(1, 1, 30, 30), 9))
                {
                    using (LinearGradientBrush lb = new LinearGradientBrush(new Rectangle(1, 1, 30, 30),
                        Util.Rgb(120, 176, 255), Util.Rgb(120, 90, 210), 60f))
                        g.FillPath(lb, p);
                    using (Pen pen = new Pen(Color.FromArgb(160, 255, 255, 255), 1f)) g.DrawPath(pen, p);
                }
                using (SolidBrush wb = new SolidBrush(Color.White))
                using (StringFormat sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    g.DrawString("琉", new Font("Microsoft YaHei UI", 15f, FontStyle.Bold, GraphicsUnit.Pixel), wb, new RectangleF(0, 0, 32, 32), sf);
                }
            }
            trayIconHandle = b.GetHicon();
            Icon ic = Icon.FromHandle(trayIconHandle);
            b.Dispose();
            return ic;
        }

        public static void ToggleAllBoards()
        {
            allHidden = !allHidden;
            for (int i = 0; i < Windows.Count; i++)
            {
                if (allHidden) Windows[i].Hide();
                else { Windows[i].Show(); Windows[i].MarkDirty(); }
            }
            if (Bar != null)
            {
                if (allHidden) Bar.Hide();
                else if (Ws.Set.ShowBar) { Bar.Show(); Bar.Render(); }
            }
            Toast(allHidden ? "看板已收起，Ctrl+Alt+L 唤回" : "看板回来了");
        }

        // ---------- 桌面图标 ----------
        static void SyncDesktopIconState()
        {
            bool visibleNow = DesktopIconsVisible();
            iconsHiddenByUs = Ws.Set.HideDesktopIcons;
            if (iconsHiddenByUs && visibleNow) Native.ToggleDesktopIcons(out visibleNow);
            else if (!iconsHiddenByUs && !visibleNow) Native.ToggleDesktopIcons(out visibleNow);
        }

        static bool DesktopIconsVisible()
        {
            // 探测逻辑收口到 Native：Win11 26200 上 FindWindow 不可靠，改用 EnumWindows + EnumChildWindows
            return Native.DesktopIconsVisible();
        }

        public static void SetDesktopIconsHidden(bool hidden)
        {
            bool visible = DesktopIconsVisible();
            // 目标状态已经成立：visible=true 且要显示，或 visible=false 且要隐藏
            if (visible != hidden)
            {
                Ws.Set.HideDesktopIcons = hidden;
                iconsHiddenByUs = hidden;
                Save();
                Toast(hidden ? "桌面图标已经是隐藏状态" : "桌面图标已经是显示状态");
                return;
            }
            bool after;
            if (Native.ToggleDesktopIcons(out after))
            {
                Ws.Set.HideDesktopIcons = hidden;
                iconsHiddenByUs = hidden;
                Save();
                Log.Info("icons", "桌面图标 nowVisible=" + after + " wantHidden=" + hidden);
                Toast(hidden ? "桌面图标已隐藏，看板接管入口" : "桌面图标已恢复");
            }
            else Toast("拿不到桌面图标开关，可能被第三方桌面工具接管了");
        }

        // ---------- 设置 / 备份 / 壁纸 ----------
        public static void OpenSettings()
        {
            if (Settings != null && !Settings.IsDisposed)
            {
                Settings.Show();
                Settings.Activate();
                Settings.Invalidate();
                return;
            }
            Settings = new SettingsForm();
            Settings.Show();
            Settings.Activate();
        }

        public static void SettingsClosed() { Settings = null; }

        public static void ExportBackup()
        {
            string path = Store.Export(Ws);
            if (path == null) { Toast("备份失败，看日志"); return; }
            Toast("备份好了：" + Path.GetFileName(path));
            Native.ShellOpenFolder(path);
        }

        public static void ImportBackup()
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Title = "选择 Liuli 备份 zip";
                d.Filter = "Liuli 备份|*.zip";
                d.InitialDirectory = Paths.Backups;
                if (d.ShowDialog() != DialogResult.OK) return;
                string msg;
                Workspace ws = Store.Import(d.FileName, ScreenRects(), out msg);
                if (ws == null) { Toast(msg); return; }
                Ws = ws;
                RebuildAllWindows();
                Toast(msg);
            }
        }

        public static void RebuildAllWindows()
        {
            for (int i = Windows.Count - 1; i >= 0; i--) { Windows[i].Close(); }
            Windows.Clear();
            for (int i = 0; i < Ws.Boards.Count; i++) CreateWindow(Ws.Boards[i], false);
            ApplyTopModeAll();
            ApplyLookFromBackdrop();
            RefreshAllBoards();
        }

        public static void ChooseWallpaperFile()
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Title = "选择壁纸";
                d.Filter = "图片或视频|*.jpg;*.jpeg;*.png;*.bmp;*.webp;*.gif;*.mp4;*.wmv;*.avi;*.mkv;*.mov;*.webm|图片|*.jpg;*.jpeg;*.png;*.bmp;*.webp;*.gif|视频|*.mp4;*.wmv;*.avi;*.mkv;*.mov;*.webm";
                if (d.ShowDialog() != DialogResult.OK) return;
                string ext = Path.GetExtension(d.FileName).ToLowerInvariant();
                bool video = ext == ".mp4" || ext == ".wmv" || ext == ".avi" || ext == ".mkv" || ext == ".mov" || ext == ".webm";
                Ws.Set.WallMode = video ? "video" : "image";
                Ws.Set.WallPath = d.FileName;
                WallApply();
            }
        }

        public static void WallApply()
        {
            WallEngine.Apply();
            Backdrop.Rebuild();
            ApplyLookFromBackdrop();
            RefreshAllBoards();
            if (Settings != null) Settings.Invalidate();
            Save();
            if (Ws.Set.WallMode == "system") Toast("已回到系统壁纸");
            else Toast(Ws.Set.WallMode == "video" ? "动态壁纸已开启" : "壁纸已换");
        }

        public static void ApplyBarVisible()
        {
            if (Bar == null) return;
            if (Ws.Set.ShowBar) { Bar.Show(); Bar.Render(); }
            else Bar.Hide();
        }

        public static void ResetLayout()
        {
            if (MessageBox.Show("只把看板位置和尺寸恢复默认，内容都保留。继续？", "Liuli", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            int left = work.X + Util.RoundI(40 * Theme.S);
            int top = work.Y + Util.RoundI(40 * Theme.S);
            int i = 0;
            for (int k = 0; k < Windows.Count; k++)
            {
                Board b = Windows[k].B;
                if (b.Kind == BoardKind.Clock) { b.X = left + 360; b.Y = top + 330; b.W = 320; b.H = 180; }
                else if (b.Kind == BoardKind.Status) { b.X = left; b.Y = top + 340; b.W = 330; b.H = 280; }
                else { b.X = left + (i % 2) * 360; b.Y = top + (i / 2) * 340; b.W = 330; b.H = 320; }
                b.Collapsed = false;
                Windows[k].SyncBounds();
                Windows[k].MarkDirty();
                i++;
            }
            Save();
            Toast("位置已归位");
        }

        public static void OrganizeDesktop()
        {
            List<DeskItem> items = Icons.ScanDesktop();
            if (items.Count == 0) { Toast("桌面没有可整理的图标"); return; }
            int created = Icons.OrganizeToBoards(items, Screen.PrimaryScreen.WorkingArea);
            Toast(created > 0 ? "整理了 " + items.Count + " 个入口，生成 " + created + " 块看板" : "入口已更新到文件看板");
        }

        public static void UndoOrganize()
        {
            if (!File.Exists(Paths.UndoFile)) { Toast("还没有可撤销的整理"); return; }
            try
            {
                File.Copy(Paths.UndoFile, Paths.BoardsFile, true);
                Ws = Store.Load(ScreenRects());
                RebuildAllWindows();
                File.Delete(Paths.UndoFile);
                Log.Info("icons", "撤销上次整理，看板 " + Ws.Boards.Count + " 块");
                Toast("已撤销上次整理");
            }
            catch (Exception ex)
            {
                Log.Error("icons", ex);
                Toast("撤销失败，看日志");
            }
        }

        // ---------- 开机自启 ----------
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string RunValue = "Liuli";

        public static void SetAutoStart(bool on)
        {
            Ws.Set.AutoStart = on;
            try
            {
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(RunKey))
                {
                    if (k == null) throw new Exception("无法打开 Run 注册表项");
                    if (on) k.SetValue(RunValue, "\"" + Application.ExecutablePath + "\"");
                    else k.DeleteValue(RunValue, false);
                }
                Log.Info("app", "开机自启 " + (on ? "on " : "off ") + Application.ExecutablePath);
            }
            catch (Exception ex) { Log.Error("app", "开机自启设置失败：" + ex.Message); }
            Save();
        }

        /// <summary>启动时对齐配置与注册表：配置要开却没写、配置关了却残留，都以配置为准。</summary>
        static void SyncAutoStart()
        {
            try
            {
                string val = null;
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey, false))
                    if (k != null) val = (string)k.GetValue(RunValue, null);
                bool reg = val != null && val.Length > 0;
                if (Ws.Set.AutoStart && !reg) SetAutoStart(true);
                else if (!Ws.Set.AutoStart && reg) SetAutoStart(false);
            }
            catch (Exception ex) { Log.Warn("app", "开机自启同步失败 " + ex.Message); }
        }

        // ---------- 杂项 ----------
        public static void Save()
        {
            try { Store.Save(Ws); } catch (Exception ex) { Log.Error("app", ex); }
        }

        public static void Copy(string text)
        {
            try { Clipboard.SetText(text ?? ""); } catch { }
        }

        public static void Toast(string text)
        {
            ToastForm.Show(text);
        }

        public static void UI(Action action)
        {
            if (action == null) return;
            if (Host != null && Host.IsHandleCreated)
            {
                try { Host.BeginInvoke(action); } catch { }
            }
            else action();
        }

        public static void Quit()
        {
            if (quitting) return;
            quitting = true;
            try
            {
                Save();
                if (iconsHiddenByUs && DesktopIconsVisible())
                {
                    bool after;
                    Native.ToggleDesktopIcons(out after);
                }
                WallEngine.Stop();
                Backdrop.Dispose();
                if (tray != null) { tray.Visible = false; tray.Dispose(); tray = null; }
                if (trayIconHandle != IntPtr.Zero) { Native.DestroyIcon(trayIconHandle); trayIconHandle = IntPtr.Zero; }
                if (fast != null) fast.Stop();
                if (slow != null) slow.Stop();
            }
            catch (Exception ex) { Log.Error("app", "退出清理失败 " + ex.Message); }
            if (Host != null) Host.Close();
            Application.Exit();
        }
    }

    /// <summary>消息宿主：不可见小窗，收热键、单实例唤醒，也承载所有定时器。</summary>
    sealed class AppHost : Form
    {
        int registered;

        public AppHost()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Bounds = new Rectangle(-4000, -4000, 1, 1);
            Opacity = 0.01;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                Native.RegisterHotKey(Handle, App.HotkeyToggle, App.MOD_CONTROL | App.MOD_ALT, (uint)Keys.L);
                Native.RegisterHotKey(Handle, App.HotkeySettings, App.MOD_CONTROL | App.MOD_ALT, (uint)Keys.G);
            }
            catch (Exception ex) { Log.Warn("hotkey", ex.Message); }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312)
            {
                int id = m.WParam.ToInt32();
                if (id == App.HotkeyToggle) App.ToggleAllBoards();
                else if (id == App.HotkeySettings) App.OpenSettings();
                return;
            }
            if (m.Msg == registered && registered != 0)
            {
                App.OpenSettings();
                return;
            }
            if (m.Msg == App.ActivateMessage && App.ActivateMessage != 0)
            {
                App.OpenSettings();
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
        }
    }
}
