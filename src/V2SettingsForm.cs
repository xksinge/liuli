// Liuli v2 · 设置面板：主题 / 面板 / Dock / 灵动岛集中修改，所有改动即时作用到桌面层。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace Liuli
{
    sealed class V2SettingsForm : Form
    {
        static readonly string[] Nav = { "主题", "配色", "面板", "Dock", "灵动岛", "壁纸", "桌面", "收纳", "系统", "关于" };
        const int MaxThemes = 6;

        float S = 1f;
        int page;
        int hover = -1;
        int hoverPart = -1;
        int dragSlider = -1;
        bool dragging;
        Point dragOff;
        Rectangle closeR;
        Rectangle[] navRects = new Rectangle[10];
        Rectangle[] textRects = new Rectangle[4];
        List<Ctl> controls = new List<Ctl>();
        TextBox nameBox;
        Rectangle nameSaveR;
        Rectangle nameCancelR;
        bool naming;
        string pendingDeleteTheme = "";
        string pendingDeletePanel = "";
        Timer refresh;
        int initialPage;
        Bitmap previewBmp;
        string previewPath = "";

        sealed class Ctl
        {
            public string Kind;       // section / info / toggle / slider / seg / button / theme / panel
            public string Key = "";
            public string Title = "";
            public string Desc = "";
            public Rectangle R;
            public Func<int> Get;
            public Action<int> Set;
            public Action Click;
            public string[] Options;
            public int Min;
            public int Max;
            public string Unit = "";
            public bool Danger;
            public object Data;
            public int Index;
        }

        public V2SettingsForm(int startPage)
        {
            initialPage = Util.Clamp(startPage, 0, Nav.Length - 1);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            DoubleBuffered = true;
            KeyPreview = true;
            BackColor = Util.Rgb(12, 15, 22);
            ClientSize = new Size(900, 720);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            MouseMove += OnMove;
            MouseDown += OnDown;
            MouseUp += OnUp;
            MouseWheel += delegate { };
            refresh = new Timer();
            refresh.Interval = 180;
            refresh.Tick += delegate
            {
                refresh.Stop();
                if (naming || dragSlider >= 0) return;
                BuildPage(page);
                Invalidate();
            };
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int dpi = Native.DpiOf(Handle);
                if (dpi > 0) S = dpi / 96f * Theme.TextScale;
            }
            catch { S = Theme.TextScale; }
            ClientSize = new Size(Util.RoundI(900 * S), Util.RoundI(720 * S));
            ApplyRegion();
            PlaceCenter();
            BuildNameBox();
            BuildPage(initialPage);
            Invalidate();
        }

        void ApplyRegion()
        {
            using (GraphicsPath p = Theme.Round(new Rectangle(0, 0, Width, Height), Util.RoundI(18 * S)))
                Region = new Region(p);
        }

        void PlaceCenter()
        {
            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(work.X + (work.Width - Width) / 2, work.Y + (work.Height - Height) / 2);
        }

        void BuildNameBox()
        {
            if (nameBox != null) return;
            nameBox = new TextBox();
            nameBox.BorderStyle = BorderStyle.FixedSingle;
            nameBox.BackColor = Util.Rgb(27, 32, 43);
            nameBox.ForeColor = Util.Rgb(238, 244, 255);
            nameBox.Font = Theme.Font(11f, FontStyle.Regular, S);
            nameBox.MaxLength = 24;
            nameBox.Visible = false;
            nameBox.KeyDown += delegate (object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter) { CommitName(); e.SuppressKeyPress = true; }
                else if (e.KeyCode == Keys.Escape) { CancelName(); e.SuppressKeyPress = true; }
            };
            Controls.Add(nameBox);
        }

        int Cx() { return Util.RoundI(190 * S); }
        int Cr() { return Width - Util.RoundI(30 * S); }
        int Ct() { return Util.RoundI(94 * S); }

        // ---------- 页面构建 ----------
        void BuildPage(int index)
        {
            page = index;
            controls.Clear();
            if (nameBox != null && index != 0) nameBox.Visible = false;
            if (index == 0) BuildThemePage();
            else if (index == 1) BuildPalettePage();
            else if (index == 2) BuildPanelPage();
            else if (index == 3) BuildDockPage();
            else if (index == 4) BuildIslandPage();
            else if (index == 5) BuildWallpaperPage();
            else if (index == 6) BuildDesktopPage();
            else if (index == 7) BuildCollectorPage();
            else if (index == 8) BuildSystemPage();
            else BuildAboutPage();
        }

        public void ReloadPage()
        {
            BuildPage(page);
            Invalidate();
        }

        public void ShowPage(int index)
        {
            page = Util.Clamp(index, 0, Nav.Length - 1);
            naming = false;
            if (nameBox != null) nameBox.Visible = false;
            BuildPage(page);
            Invalidate();
        }

        void BuildThemePage()
        {
            int x = Cx();
            int rightW = Util.RoundI(282 * S);
            int rx = Cr() - rightW;
            int w = rx - x - Util.RoundI(26 * S);
            int y = naming ? Ct() + Util.RoundI(64 * S) : Ct();

            AddSection("玻璃外观", x, y, w); y += Util.RoundI(30 * S);
            AddSlider("玻璃浓度", "越高越遮挡壁纸，越低越通透", "glass", x, y, w, 20, 140, "%",
                delegate { return V2Shell.Config.GlassPct; },
                delegate (int v)
                {
                    V2Shell.Config.GlassPct = v;
                    V2Shell.ApplyVisualSettings(false);
                });
            y += Util.RoundI(72 * S);
            AddSlider("磨砂强度", "换壁纸时一次性烘焙，松手后生效", "blur", x, y, w, 10, 100, "%",
                delegate { return V2Shell.Config.BlurPct; },
                delegate (int v)
                {
                    V2Shell.Config.BlurPct = v;
                    Theme.BlurPct = v;
                    V2Shell.ApplyVisualSettings(false);
                });
            y += Util.RoundI(72 * S);
            AddSlider("圆角", "Dock、灵动岛与面板统一圆角", "radius", x, y, w, 10, 30, "px",
                delegate { return V2Shell.Config.Radius; },
                delegate (int v)
                {
                    V2Shell.Config.Radius = v;
                    V2Shell.ApplyVisualSettings(false);
                });
            y += Util.RoundI(82 * S);
            AddSeg("明暗", "自动跟随壁纸；也可手动锁定深色或浅色", "tone", x, y, w, new string[] { "自动", "深色", "浅色" },
                delegate
                {
                    if (string.Equals(V2Shell.Config.ThemeMode, "dark", StringComparison.OrdinalIgnoreCase)) return 1;
                    if (string.Equals(V2Shell.Config.ThemeMode, "light", StringComparison.OrdinalIgnoreCase)) return 2;
                    return 0;
                },
                delegate (int v)
                {
                    V2Shell.Config.ThemeMode = v == 1 ? "dark" : (v == 2 ? "light" : "auto");
                    V2Shell.ApplyVisualSettings(false);
                    V2Shell.Config.Save();
                });
            y += Util.RoundI(92 * S);
            AddButton("保存当前主题", "把面板布局、Dock/灵动岛参数、壁纸与配色存为命名快照", "saveTheme", x, y, w, false,
                delegate { StartNameEdit(); });
            y += Util.RoundI(62 * S);
            AddButton("恢复外观默认", "只重置玻璃、磨砂、圆角与明暗，不动面板布局", "defaultTheme", x, y, w, false,
                delegate { V2Shell.RestoreVisualDefaults(); BuildPage(0); Invalidate(); });
            y += Util.RoundI(54 * S);
            string active = string.IsNullOrEmpty(V2Shell.Config.ActiveTheme) ? "未加载自定义主题" : V2Shell.Config.ActiveTheme;
            AddInfo("当前主题", active + "\n主题库最多 6 个；加载会同时恢复保存时的壁纸与配色。", x, y, w);

            AddSection("主题库", rx, Ct(), rightW);
            int ty = Ct() + Util.RoundI(30 * S);
            if (V2Shell.Config.Themes.Count == 0)
            {
                AddInfo("暂无主题", "调好外观后，点左侧“保存当前主题”。", rx, ty, rightW);
                return;
            }
            for (int i = 0; i < V2Shell.Config.Themes.Count; i++)
            {
                ThemeSnapshot t = V2Shell.Config.Themes[i];
                AddThemeRow(t, rx, ty, rightW, i);
                ty += Util.RoundI(84 * S);
            }
        }

        void BuildPalettePage()
        {
            int x = Cx();
            int w = Cr() - x;
            int y = Ct();

            AddSection("强调色来源", x, y, w); y += Util.RoundI(30 * S);
            AddSeg("来源", "跟随壁纸会随壁纸主色自动变化；预设/自定义则保持固定", "accentSource", x, y, w,
                new string[] { "跟随壁纸", "预设", "自定义" },
                delegate
                {
                    string mode = V2Shell.Config.AccentMode;
                    if (string.Equals(mode, "preset", StringComparison.OrdinalIgnoreCase)) return 1;
                    if (string.Equals(mode, "custom", StringComparison.OrdinalIgnoreCase)) return 2;
                    return 0;
                },
                delegate (int v)
                {
                    if (v == 2) { PickCustomAccent(); return; }
                    V2Shell.Config.AccentMode = v == 1 ? "preset" : "auto";
                    V2Shell.ApplyVisualSettings(false);
                    V2Shell.Config.Save();
                });
            y += Util.RoundI(92 * S);

            AddSection("内置预设", x, y, w); y += Util.RoundI(30 * S);
            int gap = Util.RoundI(10 * S);
            int cw = (w - gap * 2) / 3;
            int ch = Util.RoundI(54 * S);
            for (int i = 0; i < 6; i++)
            {
                int col = i % 3;
                int row = i / 3;
                AddSwatch(i, Palette.Name(i), x + col * (cw + gap), y + row * (ch + Util.RoundI(8 * S)), cw, ch);
            }
            y += (ch + Util.RoundI(8 * S)) * 2 + Util.RoundI(18 * S);

            AddSection("自定义", x, y, w); y += Util.RoundI(30 * S);
            string custom = ShellConfig.NormalizeHexColor(V2Shell.Config.AccentCustom, "#7EB2FF");
            AddButton("自定义强调色 · " + custom, "打开系统取色器；取消不会改变当前配色", "customAccent", x, y, w, false,
                delegate { PickCustomAccent(); });
            y += Util.RoundI(62 * S);

            AddSection("当前配色", x, y, w); y += Util.RoundI(30 * S);
            string modeText = string.Equals(V2Shell.Config.AccentMode, "preset", StringComparison.OrdinalIgnoreCase)
                ? "预设 · " + Palette.Name(V2Shell.Config.AccentPreset)
                : (string.Equals(V2Shell.Config.AccentMode, "custom", StringComparison.OrdinalIgnoreCase) ? "自定义 · " + custom : "跟随壁纸");
            AddInfo("当前强调色", modeText + "\nDock、灵动岛、面板、设置窗共用同一个 Theme.Accent；换壁纸、切预设或改自定义值时才重算一次。", x, y, w);
        }

        void BuildPanelPage()
        {
            int x = Cx();
            int w = Cr() - x;
            int y = Ct();
            AddSection("面板管理", x, y, w); y += Util.RoundI(30 * S);
            bool full = V2Shell.Config.Panels.Count >= 6;
            AddButton(full ? "已达 6 个面板上限" : "新建面板", full ? "先删除一个面板再新建" : "默认 340×200，时钟内容，出现在主屏右侧",
                "addPanel", x, y, w, false, delegate
                {
                    if (!full)
                    {
                        PanelForm pf = V2Shell.AddPanel();
                        BuildPage(2);
                        Invalidate();
                    }
                });
            y += Util.RoundI(58 * S);
            AddSection("已创建面板", x, y, w); y += Util.RoundI(28 * S);
            if (V2Shell.Config.Panels.Count == 0)
            {
                AddInfo("没有面板", "点上方“新建面板”，或回到桌面点 Dock/灵动岛的“+”。", x, y, w);
                return;
            }
            for (int i = 0; i < V2Shell.Config.Panels.Count; i++)
            {
                PanelConfig p = V2Shell.Config.Panels[i];
                AddPanelRow(p, i, x, y, w);
                y += Util.RoundI(76 * S);
            }
        }

        void BuildDockPage()
        {
            int x = Cx();
            int w = Cr() - x;
            int y = Ct();
            AddSection("Dock 显示", x, y, w); y += Util.RoundI(30 * S);
            AddToggle("显示 Dock", "关闭后 Dock 窗口会被销毁，可从本页重新打开", "dockOn", x, y, w,
                delegate { return V2Shell.Config.DockOn ? 1 : 0; },
                delegate (int v) { V2Shell.SetDockOn(v == 1); });
            y += Util.RoundI(58 * S);
            AddToggle("自动隐藏", "鼠标触底唤出，离开约 650ms 收起", "dockAutoHide", x, y, w,
                delegate { return V2Shell.Config.DockAutoHide ? 1 : 0; },
                delegate (int v) { V2Shell.SetDockAutoHide(v == 1); });
            y += Util.RoundI(76 * S);
            AddSeg("Dock 尺寸", "整体缩放，小 / 标准 / 大", "dockPreset", x, y, w, new string[] { "小", "标准", "大" },
                delegate
                {
                    if (V2Shell.Config.DockPreset == "small") return 0;
                    if (V2Shell.Config.DockPreset == "large") return 2;
                    return 1;
                },
                delegate (int v)
                {
                    V2Shell.SetDockPreset(v == 0 ? "small" : (v == 2 ? "large" : "standard"));
                    BuildPage(3);
                    Invalidate();
                });
            y += Util.RoundI(92 * S);
            AddInfo("图标管理", "Dock 图标的拖动排序、移除和文件固定仍在 Dock 上操作；本页只调显示参数。", x, y, w);
        }

        void BuildIslandPage()
        {
            int x = Cx();
            int w = Cr() - x;
            int y = Ct();
            AddSection("显示与尺寸", x, y, w); y += Util.RoundI(26 * S);
            AddToggle("显示灵动岛", "关闭后隐藏顶部胶囊，可从本页重新打开", "islandOn", x, y, w,
                delegate { return V2Shell.Config.IslandOn ? 1 : 0; },
                delegate (int v)
                {
                    V2Shell.Config.IslandOn = v == 1;
                    V2Shell.ApplyIslandSettings();
                });
            y += Util.RoundI(54 * S);
            AddSlider("展开宽度", "360–520，收起态按比例派生", "islandW", x, y, w, 360, 520, "px",
                delegate { return V2Shell.Config.IslandWidth; },
                delegate (int v) { V2Shell.Config.IslandWidth = v; IslandPreview(); });
            y += Util.RoundI(64 * S);
            AddSlider("展开高度", "100–180，收起态按比例派生", "islandH", x, y, w, 100, 180, "px",
                delegate { return V2Shell.Config.IslandHeight; },
                delegate (int v) { V2Shell.Config.IslandHeight = v; IslandPreview(); });
            y += Util.RoundI(64 * S);
            AddSlider("顶部偏移", "0–80，贴近屏幕上缘", "islandTop", x, y, w, 0, 80, "px",
                delegate { return V2Shell.Config.IslandTop; },
                delegate (int v) { V2Shell.Config.IslandTop = v; IslandPreview(); });
            y += Util.RoundI(64 * S);
            AddSlider("展开动画", "0–400ms，0 为立即切换", "islandAnim", x, y, w, 0, 400, "ms",
                delegate { return V2Shell.Config.IslandAnimMs; },
                delegate (int v) { V2Shell.Config.IslandAnimMs = v; IslandPreview(); });
            y += Util.RoundI(60 * S);
            AddSection("内容开关", x, y, w); y += Util.RoundI(24 * S);
            AddToggle("天气", "收起态右侧显示天气，展开态显示详情", "islandWeather", x, y, w,
                delegate { return V2Shell.Config.IslandWeather ? 1 : 0; },
                delegate (int v) { V2Shell.Config.IslandWeather = v == 1; V2Shell.MarkThemeDirty(); V2Shell.Config.Save(); IslandPreview(); });
            y += Util.RoundI(54 * S);
            AddToggle("系统读数", "展开态底部 CPU / 内存 / 磁盘", "islandStats", x, y, w,
                delegate { return V2Shell.Config.IslandStats ? 1 : 0; },
                delegate (int v) { V2Shell.Config.IslandStats = v == 1; V2Shell.MarkThemeDirty(); V2Shell.Config.Save(); IslandPreview(); });
            y += Util.RoundI(54 * S);
            AddToggle("快捷动作", "展开态右下三个小按钮", "islandQuick", x, y, w,
                delegate { return V2Shell.Config.IslandQuick ? 1 : 0; },
                delegate (int v) { V2Shell.Config.IslandQuick = v == 1; V2Shell.MarkThemeDirty(); V2Shell.Config.Save(); IslandPreview(); });
        }

        void BuildWallpaperPage()
        {
            int x = Cx();
            int w = Cr() - x;
            int y = Ct();
            AddSection("当前壁纸", x, y, w); y += Util.RoundI(26 * S);
            AddPreview(x, y, Math.Min(Util.RoundI(380 * S), w), Util.RoundI(214 * S));
            y += Util.RoundI(224 * S);
            string path = Wallpaper.CurrentPath();
            string current = string.IsNullOrEmpty(path) || !File.Exists(path)
                ? "未找到当前壁纸文件" : Path.GetFileName(path);
            AddInfo("当前文件", current + "\n" + (string.IsNullOrEmpty(path) ? "请选择一张本地图片" : path) +
                "\n系统样式：" + StyleName(), x, y, w); y += Util.RoundI(80 * S);

            AddSection("壁纸压暗", x, y, w); y += Util.RoundI(26 * S);
            AddSlider("压暗", "0–80%，松手后重烘一次玻璃底", "wallDim", x, y, w, 0, 80, "%",
                delegate { return V2Shell.Config.WallpaperDim; },
                delegate (int v)
                {
                    V2Shell.Config.WallpaperDim = v;
                    Theme.WallpaperDim = v;
                });
            y += Util.RoundI(64 * S);
            AddButton("选择本地图片", "jpg / png / bmp / webp；应用后立即更新 Windows 壁纸与玻璃底", "pickWallpaper", x, y, w, false,
                delegate { PickWallpaper(); });
            y += Util.RoundI(54 * S);
            string orig = V2Shell.Config.WallpaperOrig;
            string restoreDesc = string.IsNullOrEmpty(orig) || !File.Exists(orig)
                ? "首次换图前会记录原壁纸；当前没有可恢复记录"
                : "回到 " + Path.GetFileName(orig);
            AddButton("恢复系统壁纸", restoreDesc, "restoreWallpaper", x, y, w, false,
                delegate
                {
                    if (V2Shell.RestoreSystemWallpaper())
                    {
                        EnsurePreview();
                        BuildPage(5);
                        Invalidate();
                    }
                });
            y += Util.RoundI(54 * S);
            AddInfo("说明", "预览从壁纸文件解码，不截屏，不会把窗口或图标烤进玻璃。" +
                "\nWebP 需系统 WIC 解码器支持；无效图片会保持原壁纸。", x, y, w);
        }

        void AddPreview(int x, int y, int w, int h)
        {
            Ctl c = new Ctl();
            c.Kind = "preview";
            c.R = new Rectangle(x, y, w, h);
            controls.Add(c);
            EnsurePreview();
        }

        void EnsurePreview()
        {
            string path = Wallpaper.CurrentPath();
            if (previewBmp != null && string.Equals(previewPath, path, StringComparison.OrdinalIgnoreCase)) return;
            if (previewBmp != null) { previewBmp.Dispose(); previewBmp = null; }
            previewPath = path;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                using (MemoryStream ms = new MemoryStream(bytes))
                using (Image img = Image.FromStream(ms))
                {
                    int w = 760, h = 428;
                    Bitmap bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Util.Rgb(18, 22, 30));
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        float k = Math.Max(w / (float)img.Width, h / (float)img.Height);
                        int dw = (int)(img.Width * k), dh = (int)(img.Height * k);
                        g.DrawImage(img, new Rectangle((w - dw) / 2, (h - dh) / 2, dw, dh));
                    }
                    previewBmp = bmp;
                }
            }
            catch (Exception ex)
            {
                Log.Warn("wall", "壁纸预览失败 " + ex.Message);
                previewBmp = null;
            }
        }

        string StyleName()
        {
            int style = Wallpaper.Style();
            if (style == 100) return "平铺";
            if (style == 0) return "居中";
            if (style == 2) return "拉伸";
            if (style == 6) return "适应";
            if (style == 22) return "跨屏";
            return "填充";
        }

        void PickWallpaper()
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "选择壁纸";
                dlg.Filter = "图片|*.jpg;*.jpeg;*.png;*.bmp;*.webp|所有文件|*.*";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                if (V2Shell.SetWallpaperImage(dlg.FileName))
                {
                    EnsurePreview();
                    BuildPage(5);
                    Invalidate();
                }
            }
        }

        void BuildDesktopPage()
        {
            int x = Cx();
            int w = Cr() - x;
            int y = Ct();
            AddSection("桌面图标", x, y, w); y += Util.RoundI(26 * S);
            AddToggle("隐藏全部桌面图标", "运行期只隐藏，不删除文件；退出 Liuli 时恢复启动前状态",
                "desktopAll", x, y, w,
                delegate { return V2Shell.Config.DesktopIconsHidden ? 1 : 0; },
                delegate (int v)
                {
                    V2Shell.Config.DesktopIconsHidden = v == 1;
                    V2Shell.ApplyDesktopSettings(true);
                });
            y += Util.RoundI(62 * S);
            AddInfo("说明", "只隐藏当前桌面视图，不改系统“显示桌面图标”偏好；文件与快捷方式原样保留。", x, y, w);
            y += Util.RoundI(48 * S);

            AddSection("系统图标", x, y, w); y += Util.RoundI(26 * S);
            AddShellToggle("此电脑", "HideShellThisPc", x, ref y, w);
            AddShellToggle("回收站", "HideShellRecycle", x, ref y, w);
            AddShellToggle("用户文件夹", "HideShellUserFiles", x, ref y, w);
            AddShellToggle("网络", "HideShellNetwork", x, ref y, w);
            AddShellToggle("控制面板", "HideShellControlPanel", x, ref y, w);
            AddInfo("提示", "关闭=桌面不显示；写入当前用户资源管理器设置，重启资源管理器仍保持；不会创建或删除快捷方式。" +
                "\nWindows 11 25H2 已不再绘制“控制面板”桌面图标，开关会写偏好但可能看不到图标。", x, y, w);
        }

        void AddShellToggle(string title, string key, int x, ref int y, int w)
        {
            AddToggle(title, "关闭=桌面不显示这个系统图标", key, x, y, w,
                delegate { return ShellToggleGet(key) ? 1 : 0; },
                delegate (int v)
                {
                    ShellToggleSet(key, v == 1);
                    if (!V2Shell.ApplyDesktopSettings(false)) V2Shell.Toast("系统图标切换失败");
                });
            y += Util.RoundI(58 * S);
        }

        bool ShellToggleGet(string key)
        {
            ShellConfig c = V2Shell.Config;
            if (key == "HideShellThisPc") return c.HideShellThisPc;
            if (key == "HideShellRecycle") return c.HideShellRecycle;
            if (key == "HideShellUserFiles") return c.HideShellUserFiles;
            if (key == "HideShellNetwork") return c.HideShellNetwork;
            return c.HideShellControlPanel;
        }

        void ShellToggleSet(string key, bool hidden)
        {
            ShellConfig c = V2Shell.Config;
            if (key == "HideShellThisPc") c.HideShellThisPc = hidden;
            else if (key == "HideShellRecycle") c.HideShellRecycle = hidden;
            else if (key == "HideShellUserFiles") c.HideShellUserFiles = hidden;
            else if (key == "HideShellNetwork") c.HideShellNetwork = hidden;
            else c.HideShellControlPanel = hidden;
        }

        void BuildCollectorPage()
        {
            int x = Cx();
            int w = Cr() - x;
            int y = Ct();
            AddSection("桌面收纳", x, y, w); y += Util.RoundI(26 * S);
            AddToggle("启用桌面收纳", "单面板 + 分组标签；入口只存原路径，不移动/复制/删除桌面文件",
                "collectorOn", x, y, w,
                delegate { return V2Shell.Config.CollectorOn ? 1 : 0; },
                delegate (int v) { V2Shell.SetCollectorOn(v == 1); BuildPage(7); Invalidate(); });
            y += Util.RoundI(62 * S);
            AddToggle("自动收集新图标", "每 2 秒扫描用户桌面与公共桌面；新增/删除/重命名在 5 秒内对齐",
                "collectorAutoScan", x, y, w,
                delegate { return V2Shell.Config.CollectorAutoScan ? 1 : 0; },
                delegate (int v)
                {
                    V2Shell.Config.CollectorAutoScan = v == 1;
                    V2Shell.Config.Save();
                    if (v == 1) V2Shell.ScanCollectorNow();
                });
            y += Util.RoundI(62 * S);
            AddButton("重新扫描桌面", "立即同步桌面入口；不会移动或删除文件", "collectorScan", x, y, w, false,
                delegate { V2Shell.ScanCollectorNow(); BuildPage(7); Invalidate(); });
            y += Util.RoundI(54 * S);
            AddButton("恢复移出的图标", "只把入口恢复显示；原文件一直都在", "collectorRestore", x, y, w, false,
                delegate
                {
                    int n = CollectorState.RestoreHidden();
                    V2Shell.Toast(n > 0 ? "已恢复 " + n + " 个入口" : "没有移出的图标");
                    BuildPage(7);
                    Invalidate();
                });
            y += Util.RoundI(54 * S);
            AddButton("打开收纳面板", "面板可拖动、滚轮翻页；双击打开，拖动图标换组", "collectorOpen", x, y, w, false,
                delegate { V2Shell.SetCollectorOn(true); });
            y += Util.RoundI(54 * S);
            AddInfo("当前收纳", "入口 " + CollectorState.VisibleCount() + " 个（隐藏 " +
                (V2Shell.Config.CollectorItems.Count - CollectorState.VisibleCount()) + " 个）\n" +
                "原生桌面图标：" + (V2Shell.Config.DesktopIconsHidden ? "已隐藏（推荐）" : "仍显示，会和面板重复") + "\n" +
                (V2Shell.Config.CollectorOn ? "" : "收纳已关闭；桌面图标仍按桌面页设置隐藏。\n") +
                "自定义分组：编辑 data\\groups.txt，格式 ext=分组", x, y, w);
            y += Util.RoundI(104 * S);
            AddButton("去桌面页设置原生图标", "隐藏全部桌面图标，避免面板与桌面重复", "collectorDesktop", x, y, w, false,
                delegate { ShowPage(6); });
        }

        void BuildSystemPage()
        {
            int x = Cx();
            int w = Cr() - x;
            int y = Ct();
            AddSection("启动", x, y, w); y += Util.RoundI(26 * S);
            AddToggle("开机自启", "写入当前用户 HKCU Run；移动 exe 后下次启动自动修正路径；关闭即删除。",
                "autoStart", x, y, w,
                delegate { return V2Shell.Config.AutoStart ? 1 : 0; },
                delegate (int v)
                {
                    V2Shell.SetAutoStart(v == 1);
                    BuildPage(8);
                    Invalidate();
                });
            y += Util.RoundI(62 * S);
            string run = AutoStart.ReadValue();
            AddInfo("启动项", "位置：HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run\\Liuli\n" +
                "当前值：" + (run.Length == 0 ? "未写入" : run) +
                "\n只写当前用户，不写 HKLM；不需要管理员权限。", x, y, w);
            y += Util.RoundI(104 * S);
            AddSection("快捷键", x, y, w); y += Util.RoundI(26 * S);
            AddInfo("默认热键", V2Shell.HotkeyStatus() + "\n默认组合固定；被其它程序占用时显示失败，可先重新注册再重启 Liuli 排查。", x, y, w);
            y += Util.RoundI(78 * S);
            AddButton("重新注册快捷键", "先注销再注册 Ctrl+Alt+L 显示/隐藏、Ctrl+Alt+G 打开设置", "hotkeyReset", x, y, w, false,
                delegate { V2Shell.ResetHotkeys(); BuildPage(8); Invalidate(); });
            y += Util.RoundI(58 * S);
            AddSection("日志诊断", x, y, w); y += Util.RoundI(26 * S);
            AddInfo("日志文件", "路径：" + Log.FilePath +
                "\n当前：" + Log.SizeText(Log.CurrentSize()) + " · 轮转：" + Log.SizeText(Log.RotatedSize()) +
                "\n超过 512 KB 自动把旧日志移到 liuli.log.1；异常写入带堆栈。", x, y, w);
            y += Util.RoundI(88 * S);
            AddButton("打开日志目录", "在资源管理器中定位 bin\\data\\logs", "logDir", x, y, w, false,
                delegate { Native.ShellOpenFolder(Paths.Logs); });
            y += Util.RoundI(54 * S);
            AddButton("复制诊断摘要", "版本、DPI、日志大小、配置路径与最近 16 行；可直接粘贴反馈", "logCopy", x, y, w, false,
                delegate
                {
                    try { Clipboard.SetText(Log.Diagnostic()); V2Shell.Toast("诊断摘要已复制"); }
                    catch { V2Shell.Toast("复制失败，请打开日志目录查看"); }
                    BuildPage(8);
                    Invalidate();
                });
        }

        void BuildAboutPage()
        {
            int x = Cx();
            int w = Cr() - x;
            int y = Ct();
            AddInfo("版本", "Liuli 琉璃 v" + V.Version + "\n" + V.Tagline + "\n\nMIT 许可 · 完全本地 · 不联网也能用核心功能", x, y, w);
            y += Util.RoundI(116 * S);
            AddInfo("运行状态", "Dock " + (V2Shell.Dock != null ? "运行" : "关闭") + " · 灵动岛 " + (V2Shell.Island != null ? "运行" : "关闭") +
                "\n收纳 " + (V2Shell.Collector != null ? "运行" : "关闭") + " · 面板 " + V2Shell.Panels.Count + " 个 · 主题 " + V2Shell.Config.Themes.Count + " 个 · 当前 " +
                (string.IsNullOrEmpty(V2Shell.Config.ActiveTheme) ? "未加载自定义主题" : V2Shell.Config.ActiveTheme), x, y, w);
            y += Util.RoundI(96 * S);
            AddInfo("快捷键", "Ctrl+Alt+L 显示/隐藏桌面层\nCtrl+Alt+G 打开设置\nEsc 关闭设置，←/→ 切换页面", x, y, w);
            y += Util.RoundI(96 * S);
            AddButton("打开数据目录", Paths.Data, "dataDir", x, y, w, false, delegate { Native.ShellOpenFolder(Paths.Data); });
            y += Util.RoundI(58 * S);
            AddButton("关闭设置", "设置不会退出 Liuli 桌面层", "closeSettings", x, y, w, false, delegate { Close(); });
        }

        // ---------- 控件注册 ----------
        void AddSection(string title, int x, int y, int w)
        {
            Ctl c = new Ctl();
            c.Kind = "section"; c.Title = title;
            c.R = new Rectangle(x, y, w, Util.RoundI(26 * S));
            controls.Add(c);
        }

        void AddInfo(string title, string text, int x, int y, int w)
        {
            Ctl c = new Ctl();
            c.Kind = "info"; c.Title = title; c.Desc = text;
            int lines = 1;
            for (int i = 0; i < text.Length; i++) if (text[i] == '\n') lines++;
            c.R = new Rectangle(x, y, w, Math.Max(Util.RoundI(52 * S), Util.RoundI((28 + lines * 17) * S)));
            controls.Add(c);
        }

        void AddToggle(string title, string desc, string key, int x, int y, int w, Func<int> get, Action<int> set)
        {
            Ctl c = new Ctl();
            c.Kind = "toggle"; c.Title = title; c.Desc = desc; c.Key = key; c.Get = get; c.Set = set;
            c.R = new Rectangle(x, y, w, Util.RoundI(54 * S));
            controls.Add(c);
        }

        void AddSlider(string title, string desc, string key, int x, int y, int w, int min, int max, string unit, Func<int> get, Action<int> set)
        {
            Ctl c = new Ctl();
            c.Kind = "slider"; c.Title = title; c.Desc = desc; c.Key = key; c.Get = get; c.Set = set;
            c.Min = min; c.Max = max; c.Unit = unit;
            c.R = new Rectangle(x, y, w, Util.RoundI(64 * S));
            controls.Add(c);
        }

        int TextChipAt(Point p)
        {
            for (int i = 0; i < textRects.Length; i++) if (textRects[i].Contains(p)) return i;
            return -1;
        }

        public void ApplyTextScale()
        {
            try
            {
                int dpi = Native.DpiOf(Handle);
                if (dpi > 0) S = dpi / 96f * Theme.TextScale;
            }
            catch { S = Theme.TextScale; }
            ClientSize = new Size(Util.RoundI(900 * S), Util.RoundI(720 * S));
            ApplyRegion();
            PlaceCenter();
            BuildPage(page);
            Invalidate();
        }

        void AddSeg(string title, string desc, string key, int x, int y, int w, string[] options, Func<int> get, Action<int> set)
        {
            Ctl c = new Ctl();
            c.Kind = "seg"; c.Title = title; c.Desc = desc; c.Key = key; c.Options = options; c.Get = get; c.Set = set;
            c.R = new Rectangle(x, y, w, Util.RoundI(82 * S));
            controls.Add(c);
        }

        void AddButton(string title, string desc, string key, int x, int y, int w, bool danger, Action click)
        {
            Ctl c = new Ctl();
            c.Kind = "button"; c.Title = title; c.Desc = desc; c.Key = key; c.Click = click; c.Danger = danger;
            c.R = new Rectangle(x, y, w, Util.RoundI(52 * S));
            controls.Add(c);
        }

        void AddSwatch(int index, string name, int x, int y, int w, int h)
        {
            Ctl c = new Ctl();
            c.Kind = "swatch"; c.Title = name; c.Index = index;
            c.R = new Rectangle(x, y, w, h);
            controls.Add(c);
        }

        void PickCustomAccent()
        {
            using (ColorDialog dlg = new ColorDialog())
            {
                dlg.FullOpen = true;
                dlg.Color = ShellConfig.ParseHexColor(V2Shell.Config.AccentCustom, Theme.Accent);
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                V2Shell.Config.AccentMode = "custom";
                V2Shell.Config.AccentCustom = "#" + dlg.Color.R.ToString("X2") + dlg.Color.G.ToString("X2") + dlg.Color.B.ToString("X2");
                V2Shell.ApplyVisualSettings(false);
                V2Shell.Config.Save();
                BuildPage(1);
                Invalidate();
            }
        }

        void AddThemeRow(ThemeSnapshot t, int x, int y, int w, int index)
        {
            Ctl c = new Ctl();
            c.Kind = "theme"; c.Data = t; c.Index = index;
            c.R = new Rectangle(x, y, w, Util.RoundI(78 * S));
            controls.Add(c);
        }

        void AddPanelRow(PanelConfig p, int index, int x, int y, int w)
        {
            Ctl c = new Ctl();
            c.Kind = "panel"; c.Data = p; c.Index = index;
            c.R = new Rectangle(x, y, w, Util.RoundI(72 * S));
            controls.Add(c);
        }

        void IslandPreview()
        {
            if (V2Shell.Island != null) V2Shell.Island.ApplyConfig();
        }

        // ---------- 绘制 ----------
        protected override void OnPaintBackground(PaintEventArgs e) { }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            Rectangle all = new Rectangle(0, 0, Width, Height);
            Backdrop.Draw(g, Util.ToF(all), new Rectangle(Left, Top, Width, Height));
            using (SolidBrush b = new SolidBrush(Theme.Light ? Color.FromArgb(218, 244, 247, 251) : Color.FromArgb(212, 14, 18, 26)))
                g.FillRectangle(b, all);
            using (LinearGradientBrush lb = new LinearGradientBrush(all, Color.FromArgb(0, 255, 255, 255), Theme.Light ? Color.FromArgb(26, 255, 255, 255) : Color.FromArgb(0, 0, 0, 0), 115f))
                g.FillRectangle(lb, all);
            using (LinearGradientBrush lb = new LinearGradientBrush(new Rectangle(Util.RoundI(24 * S), 0, Math.Max(1, Width - Util.RoundI(48 * S)), 2), Color.FromArgb(150, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 0f))
                g.FillRectangle(lb, Util.RoundI(24 * S), 0, Math.Max(1, Width - Util.RoundI(48 * S)), 1);
            using (Pen pen = new Pen(Theme.Light ? Color.FromArgb(70, 0, 0, 0) : Color.FromArgb(60, 255, 255, 255), 1f))
                g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);

            Rectangle hd = new Rectangle(Util.RoundI(26 * S), Util.RoundI(18 * S), Util.RoundI(420 * S), Util.RoundI(28 * S));
            Theme.Text(g, "Liuli 设置", Theme.Font(15f, FontStyle.Bold, S), Theme.TextMain, hd, StringAlignment.Near, false);
            Rectangle hsub = new Rectangle(hd.X, Util.RoundI(44 * S), Util.RoundI(500 * S), Util.RoundI(20 * S));
            Theme.Text(g, "v2 桌面层 · 主题 / 配色 / 面板 / Dock / 灵动岛 / 壁纸 / 桌面 / 收纳 / 系统", Theme.Font(9.5f, FontStyle.Regular, S), Theme.TextFaint, hsub, StringAlignment.Near, false);

            closeR = new Rectangle(Width - Util.RoundI(50 * S), Util.RoundI(16 * S), Util.RoundI(30 * S), Util.RoundI(30 * S));
            Glass.Button(g, closeR, Glyph.Close, hover == -2, false, Theme.Accent);

            int chipH = Util.RoundI(24 * S), chipW = Util.RoundI(44 * S), chipGap = Util.RoundI(4 * S);
            int chipTotal = chipW * 4 + chipGap * 3;
            int chipX = closeR.X - Util.RoundI(74 * S) - chipTotal;
            int chipY = Util.RoundI(19 * S);
            Theme.Text(g, "文字", Theme.Font(8.5f, FontStyle.Regular, S), Theme.TextFaint,
                new Rectangle(chipX - Util.RoundI(2 * S), chipY, Util.RoundI(34 * S), chipH), StringAlignment.Near, false);
            chipX += Util.RoundI(34 * S);
            int textAt = V2Shell.TextSizeIndex();
            for (int i = 0; i < 4; i++)
            {
                Rectangle r = new Rectangle(chipX + i * (chipW + chipGap), chipY, chipW, chipH);
                textRects[i] = r;
                bool on = i == textAt;
                bool hot = hover == -200 - i;
                if (on)
                {
                    using (GraphicsPath p = Theme.Round(r, chipH / 2))
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(46, Theme.Accent))) g.FillPath(b, p);
                }
                else if (hot)
                {
                    using (GraphicsPath p = Theme.Round(r, chipH / 2))
                    using (SolidBrush b = new SolidBrush(Theme.Hover)) g.FillPath(b, p);
                }
                Theme.TextMiddle(g, Theme.TextScaleName(i), Theme.Font(8.5f, on ? FontStyle.Bold : FontStyle.Regular, S),
                    on ? Theme.TextMain : Theme.TextSub, r, StringAlignment.Center);
            }

            int navW = Util.RoundI(136 * S);
            for (int i = 0; i < Nav.Length; i++)
            {
                Rectangle r = new Rectangle(Util.RoundI(20 * S), Util.RoundI(98 * S) + i * Util.RoundI(44 * S), navW, Util.RoundI(36 * S));
                navRects[i] = r;
                if (page == i)
                {
                    using (GraphicsPath p = Theme.Round(r, Util.RoundI(11 * S)))
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(44, Theme.Accent))) g.FillPath(b, p);
                    using (SolidBrush b = new SolidBrush(Theme.Accent))
                        g.FillRectangle(b, r.X + 2, r.Y + Util.RoundI(9 * S), Util.RoundI(3 * S), r.Height - Util.RoundI(18 * S));
                }
                else if (hover == -100 - i)
                {
                    using (GraphicsPath p = Theme.Round(r, Util.RoundI(11 * S)))
                    using (SolidBrush b = new SolidBrush(Theme.Hover)) g.FillPath(b, p);
                }
                Theme.TextMiddle(g, Nav[i], Theme.Font(12f, page == i ? FontStyle.Bold : FontStyle.Regular, S),
                    page == i ? Theme.TextMain : Theme.TextSub, r, StringAlignment.Near);
            }
            using (Pen pen = new Pen(Theme.Hair, 1f))
                g.DrawLine(pen, Util.RoundI(156 * S), Util.RoundI(92 * S), Util.RoundI(156 * S), Height - Util.RoundI(50 * S));

            if (page == 0 && naming) PaintNameEditor(g);
            for (int i = 0; i < controls.Count; i++) PaintControl(g, controls[i], hover == i, hoverPart);
            PaintFooter(g);
        }

        void PaintNameEditor(Graphics g)
        {
            Rectangle card = new Rectangle(Cx(), Ct(), Util.RoundI(382 * S), Util.RoundI(54 * S));
            using (GraphicsPath p = Theme.Round(card, Util.RoundI(12 * S)))
            using (SolidBrush b = new SolidBrush(Theme.Light ? Color.FromArgb(38, 12, 18, 30) : Color.FromArgb(30, 255, 255, 255))) g.FillPath(b, p);
            Theme.Text(g, "主题名称", Theme.Font(10.5f, FontStyle.Bold, S), Theme.TextMain,
                new Rectangle(card.X + Util.RoundI(12 * S), card.Y + Util.RoundI(4 * S), Util.RoundI(100 * S), Util.RoundI(18 * S)), StringAlignment.Near, false);
            nameBox.Bounds = new Rectangle(card.X + Util.RoundI(12 * S), card.Y + Util.RoundI(24 * S), Util.RoundI(200 * S), Util.RoundI(24 * S));
            nameSaveR = new Rectangle(card.Right - Util.RoundI(126 * S), card.Y + Util.RoundI(22 * S), Util.RoundI(54 * S), Util.RoundI(24 * S));
            nameCancelR = new Rectangle(card.Right - Util.RoundI(66 * S), card.Y + Util.RoundI(22 * S), Util.RoundI(54 * S), Util.RoundI(24 * S));
            MiniButton(g, nameSaveR, "保存", false, hover == -3);
            MiniButton(g, nameCancelR, "取消", false, hover == -4);
            Theme.Text(g, "输入后按 Enter 保存 · Esc 取消", Theme.Font(8.5f, FontStyle.Regular, S), Theme.TextFaint,
                new Rectangle(card.Right - Util.RoundI(184 * S), card.Y + Util.RoundI(4 * S), Util.RoundI(172 * S), Util.RoundI(16 * S)), StringAlignment.Far, false);
        }

        void PaintFooter(Graphics g)
        {
            int by = Height - Util.RoundI(40 * S);
            using (Pen pen = new Pen(Theme.Hair, 1f)) g.DrawLine(pen, Util.RoundI(24 * S), by, Width - Util.RoundI(24 * S), by);
            string foot = "配置 " + ShellConfig.FilePath + " · 修改即时生效";
            Theme.Text(g, foot, Theme.Font(9f, FontStyle.Regular, S), Theme.TextFaint,
                new Rectangle(Util.RoundI(26 * S), by + Util.RoundI(8 * S), Width - Util.RoundI(52 * S), Util.RoundI(18 * S)), StringAlignment.Near, true);
        }

        void PaintPreview(Graphics g, Ctl c)
        {
            using (GraphicsPath p = Theme.Round(c.R, Util.RoundI(14 * S)))
            using (SolidBrush b = new SolidBrush(Theme.Light ? Color.FromArgb(20, 12, 18, 30) : Color.FromArgb(16, 255, 255, 255)))
                g.FillPath(b, p);
            if (previewBmp != null)
            {
                using (GraphicsPath p = Theme.Round(c.R, Util.RoundI(14 * S)))
                {
                    g.SetClip(p);
                    float k = Math.Max(c.R.Width / (float)previewBmp.Width, c.R.Height / (float)previewBmp.Height);
                    int dw = (int)(previewBmp.Width * k), dh = (int)(previewBmp.Height * k);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(previewBmp, new Rectangle(c.R.X + (c.R.Width - dw) / 2, c.R.Y + (c.R.Height - dh) / 2, dw, dh));
                    g.ResetClip();
                }
            }
            else
            {
                Theme.Glyph(g, Glyph.Image, new Rectangle(c.R.X + c.R.Width / 2 - Util.RoundI(15 * S), c.R.Y + Util.RoundI(68 * S), Util.RoundI(30 * S), Util.RoundI(30 * S)), Theme.TextFaint, 2f * S);
                Theme.TextMiddle(g, "没有可预览的壁纸", Theme.Font(10f, FontStyle.Regular, S), Theme.TextFaint,
                    new Rectangle(c.R.X, c.R.Y + Util.RoundI(106 * S), c.R.Width, Util.RoundI(22 * S)), StringAlignment.Center);
            }
            using (GraphicsPath p = Theme.Round(c.R, Util.RoundI(14 * S)))
            using (Pen pen = new Pen(Theme.Hair, 1f)) g.DrawPath(pen, p);
        }

        void PaintControl(Graphics g, Ctl c, bool hot, int part)
        {
            switch (c.Kind)
            {
                case "section":
                    Theme.Text(g, c.Title, Theme.Font(12.5f, FontStyle.Bold, S), Theme.TextMain,
                        new Rectangle(c.R.X, c.R.Y, c.R.Width, c.R.Height), StringAlignment.Near, false);
                    using (SolidBrush b = new SolidBrush(Theme.Accent))
                        g.FillRectangle(b, c.R.X, c.R.Bottom - Util.RoundI(3 * S), Util.RoundI(20 * S), Util.RoundI(2 * S));
                    break;
                case "info":
                    Theme.Text(g, c.Title, Theme.Font(10.5f, FontStyle.Bold, S), Theme.TextSub,
                        new Rectangle(c.R.X, c.R.Y, c.R.Width, Util.RoundI(18 * S)), StringAlignment.Near, false);
                    Theme.Text(g, c.Desc, Theme.Font(9f, FontStyle.Regular, S), Theme.TextFaint,
                        new Rectangle(c.R.X, c.R.Y + Util.RoundI(20 * S), c.R.Width, c.R.Height - Util.RoundI(20 * S)), StringAlignment.Near, false);
                    break;
                case "toggle":
                {
                    Theme.Text(g, c.Title, Theme.Font(11.5f, FontStyle.Bold, S), Theme.TextMain,
                        new Rectangle(c.R.X, c.R.Y + Util.RoundI(4 * S), c.R.Width - Util.RoundI(76 * S), Util.RoundI(20 * S)), StringAlignment.Near, true);
                    Theme.Text(g, c.Desc, Theme.Font(9f, FontStyle.Regular, S), Theme.TextFaint,
                        new Rectangle(c.R.X, c.R.Y + Util.RoundI(26 * S), c.R.Width - Util.RoundI(76 * S), Util.RoundI(16 * S)), StringAlignment.Near, true);
                    int sw = Util.RoundI(44 * S), sh = Util.RoundI(24 * S);
                    Rectangle track = new Rectangle(c.R.Right - sw, c.R.Y + Util.RoundI(12 * S), sw, sh);
                    int on = c.Get();
                    using (GraphicsPath p = Theme.Round(track, sh / 2))
                    using (SolidBrush b = new SolidBrush(on == 1 ? Color.FromArgb(220, Theme.Accent) : (Theme.Light ? Color.FromArgb(34, 12, 18, 30) : Color.FromArgb(36, 255, 255, 255))))
                        g.FillPath(b, p);
                    int knob = sh - Util.RoundI(6 * S);
                    int kx = on == 1 ? track.Right - knob - Util.RoundI(3 * S) : track.X + Util.RoundI(3 * S);
                    using (SolidBrush b = new SolidBrush(on == 1 ? Util.Rgb(13, 17, 25) : Util.Rgb(235, 241, 249)))
                        g.FillEllipse(b, kx, track.Y + Util.RoundI(3 * S), knob, knob);
                    break;
                }
                case "slider":
                {
                    Theme.Text(g, c.Title, Theme.Font(11.5f, FontStyle.Bold, S), Theme.TextMain,
                        new Rectangle(c.R.X, c.R.Y, c.R.Width - Util.RoundI(76 * S), Util.RoundI(20 * S)), StringAlignment.Near, true);
                    int val = c.Get();
                    Theme.Text(g, val + c.Unit, Theme.Font(10.5f, FontStyle.Regular, S), Color.FromArgb(230, Theme.Accent),
                        new Rectangle(c.R.Right - Util.RoundI(76 * S), c.R.Y, Util.RoundI(76 * S), Util.RoundI(20 * S)), StringAlignment.Far, false);
                    Theme.Text(g, c.Desc, Theme.Font(8.8f, FontStyle.Regular, S), Theme.TextFaint,
                        new Rectangle(c.R.X, c.R.Y + Util.RoundI(21 * S), c.R.Width, Util.RoundI(15 * S)), StringAlignment.Near, true);
                    Rectangle track = SliderTrack(c);
                    using (GraphicsPath p = Theme.Round(track, track.Height / 2))
                    using (SolidBrush b = new SolidBrush(Theme.Light ? Color.FromArgb(38, 12, 18, 30) : Color.FromArgb(28, 255, 255, 255))) g.FillPath(b, p);
                    float k = (val - c.Min) / (float)Math.Max(1, c.Max - c.Min);
                    int fillW = Math.Max(track.Height, (int)(track.Width * k));
                    Rectangle fill = new Rectangle(track.X, track.Y, fillW, track.Height);
                    using (GraphicsPath p = Theme.Round(fill, track.Height / 2))
                    using (LinearGradientBrush lb = new LinearGradientBrush(fill, Color.FromArgb(210, Theme.Accent), Color.FromArgb(240, Util.Mix(Theme.Accent, Color.White, 0.4f)), 0f))
                        g.FillPath(lb, p);
                    int knob = Util.RoundI(13 * S);
                    using (SolidBrush b = new SolidBrush(Util.Rgb(240, 246, 255)))
                        g.FillEllipse(b, track.X + (int)(track.Width * k) - knob / 2, track.Y + (track.Height - knob) / 2, knob, knob);
                    break;
                }
                case "seg":
                {
                    Theme.Text(g, c.Title, Theme.Font(11.5f, FontStyle.Bold, S), Theme.TextMain,
                        new Rectangle(c.R.X, c.R.Y, c.R.Width, Util.RoundI(20 * S)), StringAlignment.Near, false);
                    Rectangle seg = new Rectangle(c.R.X, c.R.Y + Util.RoundI(24 * S), Math.Min(c.R.Width, Util.RoundI(310 * S)), Util.RoundI(30 * S));
                    using (GraphicsPath p = Theme.Round(seg, seg.Height / 2))
                    using (SolidBrush b = new SolidBrush(Theme.Light ? Color.FromArgb(30, 12, 18, 30) : Color.FromArgb(22, 255, 255, 255))) g.FillPath(b, p);
                    int n = c.Options.Length;
                    int sw = seg.Width / n;
                    int idx = c.Get();
                    for (int i = 0; i < n; i++)
                    {
                        Rectangle cell = new Rectangle(seg.X + i * sw + Util.RoundI(3 * S), seg.Y + Util.RoundI(3 * S), sw - Util.RoundI(6 * S), seg.Height - Util.RoundI(6 * S));
                        if (i == idx)
                        {
                            using (GraphicsPath p = Theme.Round(cell, cell.Height / 2))
                            using (SolidBrush b = new SolidBrush(Color.FromArgb(70, Theme.Accent))) g.FillPath(b, p);
                        }
                        Theme.TextMiddle(g, c.Options[i], Theme.Font(9.5f, i == idx ? FontStyle.Bold : FontStyle.Regular, S),
                            i == idx ? Theme.TextMain : Theme.TextSub, cell, StringAlignment.Center);
                    }
                    Theme.Text(g, c.Desc, Theme.Font(8.8f, FontStyle.Regular, S), Theme.TextFaint,
                        new Rectangle(c.R.X, c.R.Y + Util.RoundI(58 * S), c.R.Width, Util.RoundI(16 * S)), StringAlignment.Near, true);
                    break;
                }
                case "button":
                {
                    Rectangle rr = c.R;
                    using (GraphicsPath p = Theme.Round(rr, Util.RoundI(12 * S)))
                    using (SolidBrush b = new SolidBrush(Theme.Light ? (hot ? Color.FromArgb(50, 12, 18, 30) : Color.FromArgb(24, 12, 18, 30)) : (hot ? Color.FromArgb(40, 255, 255, 255) : Color.FromArgb(20, 255, 255, 255)))) g.FillPath(b, p);
                    using (GraphicsPath p = Theme.Round(rr, Util.RoundI(12 * S)))
                    using (Pen pen = new Pen(Theme.Light ? Color.FromArgb(hot ? 90 : 46, 12, 18, 30) : Color.FromArgb(hot ? 70 : 34, 255, 255, 255), 1f)) g.DrawPath(pen, p);
                    Color fg = c.Danger ? Theme.Danger : Theme.TextMain;
                    Theme.Text(g, c.Title, Theme.Font(10.5f, FontStyle.Bold, S), fg,
                        new Rectangle(rr.X + Util.RoundI(12 * S), rr.Y + Util.RoundI(6 * S), rr.Width - Util.RoundI(36 * S), Util.RoundI(20 * S)), StringAlignment.Near, true);
                    Theme.Text(g, c.Desc, Theme.Font(8.8f, FontStyle.Regular, S), Theme.TextFaint,
                        new Rectangle(rr.X + Util.RoundI(12 * S), rr.Y + Util.RoundI(26 * S), rr.Width - Util.RoundI(36 * S), Util.RoundI(16 * S)), StringAlignment.Near, true);
                    Theme.Glyph(g, Glyph.Chevron, new Rectangle(rr.Right - Util.RoundI(26 * S), rr.Y + (rr.Height - Util.RoundI(16 * S)) / 2, Util.RoundI(16 * S), Util.RoundI(16 * S)), Theme.TextSub, 1.3f * S);
                    break;
                }
                case "preview":
                    PaintPreview(g, c);
                    break;
                case "swatch":
                {
                    bool selected = string.Equals(V2Shell.Config.AccentMode, "preset", StringComparison.OrdinalIgnoreCase)
                        && V2Shell.Config.AccentPreset == c.Index;
                    using (GraphicsPath p = Theme.Round(c.R, Util.RoundI(12 * S)))
                    using (SolidBrush b = new SolidBrush(selected ? Color.FromArgb(48, Theme.Accent) : (hot ? Theme.Hover : (Theme.Light ? Color.FromArgb(18, 12, 18, 30) : Color.FromArgb(16, 255, 255, 255))))) g.FillPath(b, p);
                    using (GraphicsPath p = Theme.Round(c.R, Util.RoundI(12 * S)))
                    using (Pen pen = new Pen(selected ? Color.FromArgb(150, Theme.Accent) : Theme.Hair, 1f)) g.DrawPath(pen, p);
                    int dot = Util.RoundI(20 * S);
                    Rectangle dotR = new Rectangle(c.R.X + Util.RoundI(14 * S), c.R.Y + (c.R.Height - dot) / 2, dot, dot);
                    using (SolidBrush b = new SolidBrush(Palette.Get(c.Index, Theme.Light)))
                        g.FillEllipse(b, dotR);
                    if (selected)
                        using (Pen pen = new Pen(Theme.TextMain, 1.2f)) g.DrawEllipse(pen, dotR.X - 2, dotR.Y - 2, dotR.Width + 4, dotR.Height + 4);
                    Theme.TextMiddle(g, c.Title, Theme.Font(10.5f, selected ? FontStyle.Bold : FontStyle.Regular, S),
                        selected ? Theme.TextMain : Theme.TextSub,
                        new Rectangle(dotR.Right + Util.RoundI(10 * S), c.R.Y, c.R.Width - dotR.Width - Util.RoundI(34 * S), c.R.Height), StringAlignment.Near);
                    break;
                }
                case "theme":
                    PaintThemeRow(g, c, hot, part);
                    break;
                case "panel":
                    PaintPanelRow(g, c, hot, part);
                    break;
            }
        }

        void PaintThemeRow(Graphics g, Ctl c, bool hot, int part)
        {
            ThemeSnapshot t = c.Data as ThemeSnapshot;
            if (t == null) return;
            bool active = string.Equals(V2Shell.Config.ActiveTheme, t.Name, StringComparison.OrdinalIgnoreCase);
            using (GraphicsPath p = Theme.Round(c.R, Util.RoundI(13 * S)))
            using (SolidBrush b = new SolidBrush(active ? Color.FromArgb(44, Theme.Accent) : (hot ? Theme.Hover : (Theme.Light ? Color.FromArgb(16, 12, 18, 30) : Color.FromArgb(14, 255, 255, 255))))) g.FillPath(b, p);
            Theme.Text(g, t.Name, Theme.Font(10.5f, FontStyle.Bold, S), Theme.TextMain,
                new Rectangle(c.R.X + Util.RoundI(12 * S), c.R.Y + Util.RoundI(6 * S), c.R.Width - Util.RoundI(58 * S), Util.RoundI(19 * S)), StringAlignment.Near, true);
            if (active)
            {
                Rectangle chip = new Rectangle(c.R.Right - Util.RoundI(48 * S), c.R.Y + Util.RoundI(6 * S), Util.RoundI(38 * S), Util.RoundI(18 * S));
                using (GraphicsPath p = Theme.Round(chip, chip.Height / 2))
                using (SolidBrush b = new SolidBrush(Color.FromArgb(70, Theme.Accent))) g.FillPath(b, p);
                Theme.TextMiddle(g, "当前", Theme.Font(8f, FontStyle.Bold, S), Theme.Accent, chip, StringAlignment.Center);
            }
            string meta = "面板 " + t.Panels.Count + " · 玻璃 " + t.GlassPct + "% · 圆角 " + t.Radius + " · Dock " +
                (t.DockPreset == "small" ? "小" : (t.DockPreset == "large" ? "大" : "标准"));
            Theme.Text(g, meta, Theme.Font(8.5f, FontStyle.Regular, S), Theme.TextFaint,
                new Rectangle(c.R.X + Util.RoundI(12 * S), c.R.Y + Util.RoundI(25 * S), c.R.Width - Util.RoundI(24 * S), Util.RoundI(15 * S)), StringAlignment.Near, true);

            Rectangle load = new Rectangle(c.R.X + Util.RoundI(10 * S), c.R.Bottom - Util.RoundI(28 * S), Util.RoundI(54 * S), Util.RoundI(22 * S));
            Rectangle over = new Rectangle(load.Right + Util.RoundI(6 * S), load.Y, Util.RoundI(54 * S), Util.RoundI(22 * S));
            Rectangle del = new Rectangle(over.Right + Util.RoundI(6 * S), load.Y, Util.RoundI(54 * S), Util.RoundI(22 * S));
            MiniButton(g, load, "加载", false, hot && part == 0);
            MiniButton(g, over, "覆盖", false, hot && part == 1);
            MiniButton(g, del, pendingDeleteTheme == t.Name ? "确认删除" : "删除", true, hot && part == 2);
        }

        void PaintPanelRow(Graphics g, Ctl c, bool hot, int part)
        {
            PanelConfig p = c.Data as PanelConfig;
            if (p == null) return;
            Color cardBg = hot ? Theme.Hover : (Theme.Light ? Color.FromArgb(16, 12, 18, 30) : Color.FromArgb(14, 255, 255, 255));
            using (GraphicsPath path = Theme.Round(c.R, Util.RoundI(13 * S)))
            using (SolidBrush b = new SolidBrush(cardBg)) g.FillPath(b, path);
            string title = "面板 " + (c.Index + 1) + " · " + PanelConfig.ContentName(p.Content);
            Theme.Text(g, title, Theme.Font(10.5f, FontStyle.Bold, S), Theme.TextMain,
                new Rectangle(c.R.X + Util.RoundI(12 * S), c.R.Y + Util.RoundI(5 * S), c.R.Width - Util.RoundI(150 * S), Util.RoundI(19 * S)), StringAlignment.Near, true);
            Theme.Text(g, p.X + "," + p.Y + " · " + p.W + "×" + p.H + " · " + (p.TopMostPanel ? "置顶" : "置底"), Theme.Font(8.3f, FontStyle.Regular, S), Theme.TextFaint,
                new Rectangle(c.R.X + Util.RoundI(12 * S), c.R.Y + Util.RoundI(24 * S), c.R.Width - Util.RoundI(150 * S), Util.RoundI(14 * S)), StringAlignment.Near, true);

            string[] modes = { "empty", "clock", "weather", "stats" };
            int chipW = Util.RoundI(56 * S), gap = Util.RoundI(5 * S);
            int chipsX = c.R.X + Util.RoundI(12 * S);
            int chipY = c.R.Bottom - Util.RoundI(28 * S);
            for (int i = 0; i < modes.Length; i++)
            {
                Rectangle chip = new Rectangle(chipsX + i * (chipW + gap), chipY, chipW, Util.RoundI(22 * S));
                bool on = string.Equals(p.Content, modes[i], StringComparison.OrdinalIgnoreCase);
                Color chipBg = on ? Color.FromArgb(70, Theme.Accent) : (Theme.Light ? Color.FromArgb(24, 12, 18, 30) : Color.FromArgb(22, 255, 255, 255));
                using (GraphicsPath path = Theme.Round(chip, chip.Height / 2))
                using (SolidBrush b = new SolidBrush(chipBg)) g.FillPath(b, path);
                Theme.TextMiddle(g, PanelConfig.ContentName(modes[i]), Theme.Font(8f, on ? FontStyle.Bold : FontStyle.Regular, S),
                    on ? Theme.TextMain : Theme.TextSub, chip, StringAlignment.Center);
            }
            Rectangle topR = new Rectangle(c.R.Right - Util.RoundI(118 * S), c.R.Y + Util.RoundI(6 * S), Util.RoundI(50 * S), Util.RoundI(22 * S));
            Rectangle delR = new Rectangle(c.R.Right - Util.RoundI(62 * S), c.R.Y + Util.RoundI(6 * S), Util.RoundI(50 * S), Util.RoundI(22 * S));
            MiniButton(g, topR, p.TopMostPanel ? "置底" : "置顶", false, hot && part == 0);
            MiniButton(g, delR, pendingDeletePanel == p.Id ? "确认删" : "删除", true, hot && part == 1);
        }

        void MiniButton(Graphics g, Rectangle r, string text, bool danger, bool hot)
        {
            Color mbg = hot ? Color.FromArgb(50, Theme.Accent) : (Theme.Light ? Color.FromArgb(28, 12, 18, 30) : Color.FromArgb(24, 255, 255, 255));
            using (GraphicsPath p = Theme.Round(r, r.Height / 2))
            using (SolidBrush b = new SolidBrush(mbg)) g.FillPath(b, p);
            Color fg = danger ? Theme.Danger : Theme.TextSub;
            if (hot) fg = danger ? Theme.Danger : Theme.Accent;
            Theme.TextMiddle(g, text, Theme.Font(8.5f, FontStyle.Bold, S), fg, r, StringAlignment.Center);
        }

        Rectangle SliderTrack(Ctl c)
        {
            return new Rectangle(c.R.X, c.R.Bottom - Util.RoundI(12 * S), c.R.Width, Util.RoundI(5 * S));
        }

        // ---------- 主题命名 ----------
        void StartNameEdit()
        {
            if (!string.IsNullOrEmpty(pendingDeleteTheme)) pendingDeleteTheme = "";
            naming = true;
            nameBox.Text = NextThemeName();
            nameBox.Visible = true;
            BuildPage(0);
            Invalidate();
            nameBox.Focus();
            nameBox.SelectAll();
        }

        string NextThemeName()
        {
            for (int i = 1; i <= 99; i++)
            {
                string name = "主题 " + i;
                if (V2Shell.Config.FindTheme(name) == null) return name;
            }
            return "主题";
        }

        void CommitName()
        {
            if (!naming) return;
            // 让输入框先失焦，确保中文输入法的候选串已经提交到 Text。
            try { ActiveControl = null; Application.DoEvents(); } catch { }
            string name = ThemeSnapshot.CleanName(nameBox.Text);
            naming = false;
            nameBox.Visible = false;
            V2Shell.SaveCurrentTheme(name);
            BuildPage(0);
            Invalidate();
        }

        void CancelName()
        {
            naming = false;
            if (nameBox != null) nameBox.Visible = false;
            BuildPage(0);
            Invalidate();
        }

        // ---------- 输入 ----------
        void OnMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                Point p = PointToScreen(e.Location);
                Location = new Point(p.X - dragOff.X, p.Y - dragOff.Y);
                return;
            }
            if (dragSlider >= 0 && dragSlider < controls.Count && controls[dragSlider].Kind == "slider")
                ApplySlider(controls[dragSlider], e.X);

            int oldHover = hover, oldPart = hoverPart;
            hover = -1; hoverPart = -1;
            int th = TextChipAt(e.Location);
            if (th >= 0) hover = -200 - th;
            else if (closeR.Contains(e.Location)) hover = -2;
            else if (naming && nameSaveR.Contains(e.Location)) hover = -3;
            else if (naming && nameCancelR.Contains(e.Location)) hover = -4;
            else
            {
                for (int i = 0; i < navRects.Length; i++) if (navRects[i].Contains(e.Location)) hover = -100 - i;
                if (hover == -1)
                {
                    for (int i = 0; i < controls.Count; i++)
                    {
                        if (controls[i].R.Contains(e.Location))
                        {
                            hover = i;
                            hoverPart = PartAt(controls[i], e.Location);
                            break;
                        }
                    }
                }
            }
            if (oldHover != hover || oldPart != hoverPart) Invalidate();
            bool hand = hover >= 0 || hover == -2 || hover == -3 || hover == -4 || hover <= -100;
            Cursor = hand ? Cursors.Hand : Cursors.Default;
        }

        void OnDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (TextChipAt(e.Location) >= 0) return;
            if (e.Y < Util.RoundI(72 * S) && !closeR.Contains(e.Location) && !(nameBox != null && nameBox.Bounds.Contains(e.Location)))
            {
                dragging = true;
                Point sp = PointToScreen(e.Location);
                dragOff = new Point(sp.X - Left, sp.Y - Top);
            }
        }

        void ScheduleRefresh()
        {
            if (refresh == null) return;
            refresh.Stop();
            refresh.Start();
        }

        void OnUp(object sender, MouseEventArgs e)
        {
            dragging = false;
            if (dragSlider >= 0)
            {
                Ctl c = controls[dragSlider];
                dragSlider = -1;
                if (V2Shell.Config != null) V2Shell.Config.Save();
                if (c.Key == "blur") V2Shell.ApplyVisualSettings(true);
                if (c.Key == "wallDim") V2Shell.ApplyVisualSettings(true);
                if (page == 4) V2Shell.ApplyIslandSettings();
                Invalidate();
                ScheduleRefresh();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            int th = TextChipAt(e.Location);
            if (th >= 0) { V2Shell.SetTextSize(th); return; }
            for (int i = 0; i < navRects.Length; i++)
            {
                if (navRects[i].Contains(e.Location)) { BuildPage(i); Invalidate(); return; }
            }
            if (closeR.Contains(e.Location)) { Close(); return; }
            if (naming && nameSaveR.Contains(e.Location)) { CommitName(); return; }
            if (naming && nameCancelR.Contains(e.Location)) { CancelName(); return; }

            int hit = -1;
            for (int i = 0; i < controls.Count; i++) if (controls[i].R.Contains(e.Location)) { hit = i; break; }
            if (hit < 0) return;
            Ctl c = controls[hit];
            if (c.Kind == "slider")
            {
                dragSlider = hit;
                ApplySlider(c, e.X);
                Invalidate();
            }
            else if (c.Kind == "toggle")
            {
                c.Set(c.Get() == 1 ? 0 : 1);
                if (V2Shell.Config != null) V2Shell.Config.Save();
                Invalidate();
                ScheduleRefresh();
            }
            else if (c.Kind == "button")
            {
                if (c.Click != null) c.Click();
                BuildPage(page);
                Invalidate();
            }
            else if (c.Kind == "seg")
            {
                Rectangle seg = SegRect(c);
                if (seg.Contains(e.Location))
                {
                    int n = c.Options.Length;
                    int idx = Util.Clamp((e.X - seg.X) * n / seg.Width, 0, n - 1);
                    c.Set(idx);
                    if (V2Shell.Config != null) V2Shell.Config.Save();
                    BuildPage(page);
                    Invalidate();
                }
            }
            else if (c.Kind == "swatch")
            {
                V2Shell.Config.AccentMode = "preset";
                V2Shell.Config.AccentPreset = c.Index;
                V2Shell.ApplyVisualSettings(false);
                V2Shell.Config.Save();
                BuildPage(1);
                Invalidate();
            }
            else if (c.Kind == "theme") ThemeClick(c, e.Location);
            else if (c.Kind == "panel") PanelClick(c, e.Location);
        }

        void ThemeClick(Ctl c, Point p)
        {
            ThemeSnapshot t = c.Data as ThemeSnapshot;
            if (t == null) return;
            int part = PartAt(c, p);
            if (part == 2)
            {
                if (pendingDeleteTheme == t.Name)
                {
                    V2Shell.DeleteTheme(t.Name);
                    pendingDeleteTheme = "";
                    BuildPage(0);
                    Invalidate();
                }
                else
                {
                    pendingDeleteTheme = t.Name;
                    Invalidate();
                }
                return;
            }
            pendingDeleteTheme = "";
            if (part == 0)
            {
                V2Shell.LoadTheme(t);
                BuildPage(0);
                Invalidate();
            }
            else if (part == 1)
            {
                V2Shell.SaveCurrentTheme(t.Name);
                BuildPage(0);
                Invalidate();
            }
        }

        void PanelClick(Ctl c, Point p)
        {
            PanelConfig cfg = c.Data as PanelConfig;
            if (cfg == null) return;
            int part = PartAt(c, p);
            if (part == 1)
            {
                if (pendingDeletePanel == cfg.Id)
                {
                    V2Shell.RemovePanel(FindPanel(cfg));
                    pendingDeletePanel = "";
                    BuildPage(2);
                    Invalidate();
                }
                else
                {
                    pendingDeletePanel = cfg.Id;
                    Invalidate();
                }
                return;
            }
            pendingDeletePanel = "";
            if (part == 0)
            {
                cfg.TopMostPanel = !cfg.TopMostPanel;
                PanelForm pf = FindPanel(cfg);
                if (pf != null) { pf.ApplyConfig(); pf.Render(); }
                V2Shell.MarkThemeDirty();
                V2Shell.Config.Save();
                Invalidate();
                return;
            }

            string[] modes = { "empty", "clock", "weather", "stats" };
            int chipW = Util.RoundI(56 * S), gap = Util.RoundI(5 * S);
            int chipsX = c.R.X + Util.RoundI(12 * S);
            int chipY = c.R.Bottom - Util.RoundI(28 * S);
            for (int i = 0; i < modes.Length; i++)
            {
                Rectangle chip = new Rectangle(chipsX + i * (chipW + gap), chipY, chipW, Util.RoundI(22 * S));
                if (chip.Contains(p))
                {
                    cfg.Content = modes[i];
                    PanelForm pf = FindPanel(cfg);
                    if (pf != null) { pf.Cfg.Content = cfg.Content; pf.Render(); }
                    V2Shell.MarkThemeDirty();
                    V2Shell.Config.Save();
                    Invalidate();
                    return;
                }
            }
        }

        PanelForm FindPanel(PanelConfig cfg)
        {
            for (int i = 0; i < V2Shell.Panels.Count; i++)
                if (V2Shell.Panels[i] != null && V2Shell.Panels[i].Cfg == cfg) return V2Shell.Panels[i];
            return null;
        }

        int PartAt(Ctl c, Point p)
        {
            if (c.Kind == "theme")
            {
                Rectangle load = new Rectangle(c.R.X + Util.RoundI(10 * S), c.R.Bottom - Util.RoundI(28 * S), Util.RoundI(54 * S), Util.RoundI(22 * S));
                Rectangle over = new Rectangle(load.Right + Util.RoundI(6 * S), load.Y, Util.RoundI(54 * S), Util.RoundI(22 * S));
                Rectangle del = new Rectangle(over.Right + Util.RoundI(6 * S), load.Y, Util.RoundI(54 * S), Util.RoundI(22 * S));
                if (load.Contains(p)) return 0;
                if (over.Contains(p)) return 1;
                if (del.Contains(p)) return 2;
                return -1;
            }
            if (c.Kind == "panel")
            {
                Rectangle topR = new Rectangle(c.R.Right - Util.RoundI(118 * S), c.R.Y + Util.RoundI(6 * S), Util.RoundI(50 * S), Util.RoundI(22 * S));
                Rectangle delR = new Rectangle(c.R.Right - Util.RoundI(62 * S), c.R.Y + Util.RoundI(6 * S), Util.RoundI(50 * S), Util.RoundI(22 * S));
                if (topR.Contains(p)) return 0;
                if (delR.Contains(p)) return 1;
                string[] modes = { "empty", "clock", "weather", "stats" };
                int chipW = Util.RoundI(56 * S), gap = Util.RoundI(5 * S);
                int chipsX = c.R.X + Util.RoundI(12 * S);
                int chipY = c.R.Bottom - Util.RoundI(28 * S);
                for (int i = 0; i < modes.Length; i++)
                {
                    Rectangle chip = new Rectangle(chipsX + i * (chipW + gap), chipY, chipW, Util.RoundI(22 * S));
                    if (chip.Contains(p)) return 10 + i;
                }
                return -1;
            }
            return 0;
        }

        Rectangle SegRect(Ctl c)
        {
            return new Rectangle(c.R.X, c.R.Y + Util.RoundI(24 * S), Math.Min(c.R.Width, Util.RoundI(310 * S)), Util.RoundI(30 * S));
        }

        void ApplySlider(Ctl c, int mouseX)
        {
            Rectangle t = SliderTrack(c);
            float k = Util.ClampF((mouseX - t.X) / (float)Math.Max(1, t.Width), 0f, 1f);
            int v = c.Min + (int)Math.Round((c.Max - c.Min) * k);
            c.Set(v);
            Invalidate();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (nameBox != null && nameBox.Visible) return base.ProcessCmdKey(ref msg, keyData);
            if (keyData == Keys.Escape) { Close(); return true; }
            if (keyData == Keys.Left) { BuildPage(Math.Max(0, page - 1)); Invalidate(); return true; }
            if (keyData == Keys.Right) { BuildPage(Math.Min(Nav.Length - 1, page + 1)); Invalidate(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Activate();
            Log.Info("settings", "shown at " + Left + "," + Top + " " + Width + "x" + Height + " dpi=" + S);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (refresh != null) { refresh.Stop(); refresh.Dispose(); refresh = null; }
            if (nameBox != null) { nameBox.Dispose(); nameBox = null; }
            if (previewBmp != null) { previewBmp.Dispose(); previewBmp = null; }
            base.OnFormClosed(e);
            V2Shell.SettingsClosed();
        }
    }
}
