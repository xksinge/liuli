// Liuli · 设置中心：半透明玻璃面板，自绘开关/滑块/分段控件，改一项立即作用到所有看板。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Liuli
{
    sealed class SettingsForm : Form
    {
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);

        static readonly string[] nav = { "外观", "看板", "壁纸", "数据", "关于" };

        int page;
        int hoverNav = -1;
        int hoverRow = -1;
        int dragRow = -1;
        bool dragging;
        Point dragOff;
        Rectangle closeR;
        Rectangle[] navRects = new Rectangle[5];
        int contentX, contentRight, contentTop;
        List<Row> rows = new List<Row>();
        readonly Timer perfTimer;

        long lastPerfTick;
        float procCpu;
        long lastProcCpuTicks;
        long lastProcWall;

        sealed class Row
        {
            public string Kind;       // toggle / slider / button / seg / info / split
            public string Title = "";
            public string Desc = "";
            public Rectangle R;
            public Func<int> Get;
            public Action<int> Set;
            public Action Click;
            public string[] Options;
            public int Min, Max;
            public string Unit = "";
            public bool Danger;
            public int Value;          // info 用
            public string Text = "";
        }

        public SettingsForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            DoubleBuffered = true;
            KeyPreview = true;
            BackColor = Util.Rgb(12, 15, 22);
            ClientSize = new Size(Util.RoundI(840 * Theme.S), Util.RoundI(700 * Theme.S));
            ApplyRegion();
            PlaceCenter();
            BuildPage(0);

            perfTimer = new Timer();
            perfTimer.Interval = 1000;
            perfTimer.Tick += delegate { Invalidate(); };
            perfTimer.Start();

            MouseMove += OnMove;
            MouseDown += OnDown;
            MouseUp += OnUp;
            MouseWheel += delegate { };
        }

        void PlaceCenter()
        {
            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(work.X + (work.Width - Width) / 2, work.Y + (work.Height - Height) / 2);
        }

        public void ApplyRegion()
        {
            using (GraphicsPath p = Theme.Round(new Rectangle(0, 0, Width, Height), Util.RoundI(20 * Theme.S)))
                Region = new Region(p);
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }

        // ---------- 页面构建 ----------
        void BuildPage(int index)
        {
            page = index;
            rows.Clear();
            float s = Theme.S;
            int navW = Util.RoundI(150 * s);
            contentX = navW + Util.RoundI(26 * s);
            contentRight = Width - Util.RoundI(30 * s);
            contentTop = Util.RoundI(82 * s);

            int y = contentTop;
            nextY = contentTop;
            if (index == 0)
            {
                AddSlider("玻璃浓度", "越浓越遮挡壁纸，越淡越通透", y, 0, 100, "%",
                    delegate { return Theme.GlassPct; }, delegate (int v) { App.Ws.Set.GlassPct = v; App.RefreshLook(false); });
                AddSlider("磨砂强度", "换壁纸时一次性烘焙，不逐帧计算", y, 0, 100, "%",
                    delegate { return Theme.BlurPct; }, delegate (int v) { App.Ws.Set.BlurPct = v; App.RefreshLook(true); });
                AddSlider("圆角", "看板与胶囊的统一圆角", y, 10, 30, "px",
                    delegate { return Theme.Radius; }, delegate (int v) { App.Ws.Set.Radius = v; App.RefreshLook(false); });
                AddToggle("实时玻璃", "交给 DWM 亚克力实时模糊（GPU，跟手略有延迟）", y,
                    delegate { return Theme.Live ? 1 : 0; }, delegate (int v) { App.Ws.Set.LiveGlass = v == 1; App.RefreshLook(false, true); });
                AddSeg("明暗", "跟随壁纸自动判断，或手动锁定", y, new string[] { "自动", "深色", "浅色" },
                    delegate
                    {
                        if (App.Ws.Set.Tone == "dark") return 1;
                        if (App.Ws.Set.Tone == "light") return 2;
                        return 0;
                    },
                    delegate (int v)
                    {
                        App.Ws.Set.Tone = v == 1 ? "dark" : (v == 2 ? "light" : "auto");
                        App.RefreshLook(false);
                    });
                AddInfo("说明", "磨砂底来自壁纸文件（不截屏），窗口、图标、桌宠不会被烤进玻璃。实时玻璃模式则模糊屏幕真实内容。", y);
            }
            else if (index == 1)
            {
                AddToggle("磁吸对齐", "拖动时吸附屏幕边缘与其它看板", y,
                    delegate { return App.Ws.Set.Snap ? 1 : 0; }, delegate (int v) { App.Ws.Set.Snap = v == 1; App.Save(); });
                AddToggle("总在最前", "所有看板浮在普通窗口之上（默认贴桌面层）", y,
                    delegate { return App.Ws.Set.AlwaysTop ? 1 : 0; }, delegate (int v) { App.Ws.Set.AlwaysTop = v == 1; App.ApplyTopModeAll(); App.Save(); });
                AddToggle("显示控制条", "屏幕底部那枚新建/整理/设置胶囊", y,
                    delegate { return App.Ws.Set.ShowBar ? 1 : 0; }, delegate (int v) { App.Ws.Set.ShowBar = v == 1; App.ApplyBarVisible(); App.Save(); });
                AddToggle("开机自启", "登录 Windows 后自动启动 Liuli（写入当前用户 Run 项）", y,
                    delegate { return App.Ws.Set.AutoStart ? 1 : 0; }, delegate (int v) { App.SetAutoStart(v == 1); });
                AddButton("新建一块看板", "便签 / 待办 / 文件 / 代码 / 状态 / 时钟", y, false, delegate
                {
                    App.ShowKindMenu(new Point(Left + Width / 2, Top + Util.RoundI(160 * Theme.S)));
                });
                AddToggle("整理时移动原件", "真实把桌面文件移进 " + Icons.LiuliRoot + "\\分组，桌面不再保留副本；可撤销", y,
                    delegate { return App.Ws.Set.OrganizeMove ? 1 : 0; }, delegate (int v) { App.Ws.Set.OrganizeMove = v == 1; App.Save(); });
                AddButton("一键整理桌面图标", "按类型分组成看板，只收入口，不动桌面原件", y, false, delegate { App.OrganizeDesktop(); });
                AddButton("撤销上次整理", "恢复到上一次整理之前的看板布局（只保留一步）", y, false, delegate { App.UndoOrganize(); });
                AddInfo("自定义分组", "在 data\\groups.txt 里按 ext=分组 写规则，分组名用 应用/文件夹/文档/图片/影音/压缩包/代码/其他，重启 Liuli 后生效。", y);
                AddToggle("隐藏桌面图标", "由系统桌面开关控制，退出时恢复；随时可再打开", y,
                    delegate { return App.Ws.Set.HideDesktopIcons ? 1 : 0; }, delegate (int v) { App.SetDesktopIconsHidden(v == 1); });
                AddButton("把看板收回屏幕内", "多屏切换或分辨率变了以后用", y, false, delegate { App.RescueBoards(); });
            }
            else if (index == 2)
            {
                AddSeg("壁纸来源", "系统壁纸 / 本地图片 / 本地视频（视频需要系统解码器）", y, new string[] { "系统", "图片", "视频" },
                    delegate
                    {
                        if (App.Ws.Set.WallMode == "image") return 1;
                        if (App.Ws.Set.WallMode == "video") return 2;
                        return 0;
                    },
                    delegate (int v)
                    {
                        if (v == 0) { App.Ws.Set.WallMode = "system"; App.Ws.Set.WallPath = ""; App.WallApply(); return; }
                        using (OpenFileDialog d = new OpenFileDialog())
                        {
                            d.Title = v == 1 ? "选择一张壁纸图片" : "选择一段壁纸视频";
                            d.Filter = v == 1 ? "图片|*.jpg;*.jpeg;*.png;*.bmp;*.webp;*.gif" : "视频|*.mp4;*.wmv;*.avi;*.mkv;*.mov;*.webm";
                            if (d.ShowDialog() == DialogResult.OK)
                            {
                                App.Ws.Set.WallMode = v == 1 ? "image" : "video";
                                App.Ws.Set.WallPath = d.FileName;
                                App.WallApply();
                            }
                            else BuildPage(2);
                            Invalidate();
                        }
                    });
                AddButton("选择壁纸文件", App.Ws.Set.WallPath.Length > 0 ? System.IO.Path.GetFileName(App.Ws.Set.WallPath) : "当前跟随系统壁纸", y, false, delegate
                {
                    App.ChooseWallpaperFile();
                });
                AddButton("恢复系统壁纸", "停止动态壁纸，回到 Windows 自己的壁纸", y, false, delegate { App.Ws.Set.WallMode = "system"; App.Ws.Set.WallPath = ""; App.WallApply(); });
                AddSlider("壁纸压暗", "动态壁纸太亮时压一压，看板文字更清楚", y, 0, 80, "%",
                    delegate { return App.Ws.Set.WallDim; }, delegate (int v) { App.Ws.Set.WallDim = v; App.WallApply(); });
                AddToggle("全屏/锁屏暂停", "有全屏应用或锁屏时暂停动效，省电省 GPU", y,
                    delegate { return App.Ws.Set.WallPause ? 1 : 0; }, delegate (int v) { App.Ws.Set.WallPause = v == 1; App.MarkDirty(); });
                AddToggle("视频静音", "动态壁纸默认不出声", y,
                    delegate { return App.Ws.Set.WallMute ? 1 : 0; }, delegate (int v) { App.Ws.Set.WallMute = v == 1; App.WallApply(); });
            }
            else if (index == 3)
            {
                AddButton("收纳位置", (App.Ws.Set.StoreRoot.Length > 0 ? "自定义：" : "默认：") + Icons.LiuliRoot, y, false, delegate
                {
                    using (FolderBrowserDialog d = new FolderBrowserDialog())
                    {
                        d.Description = "选择 Liuli 收纳文件的根目录（下面会按看板分组建子文件夹）";
                        d.SelectedPath = Icons.LiuliRoot;
                        if (d.ShowDialog() == DialogResult.OK)
                        {
                            App.Ws.Set.StoreRoot = d.SelectedPath;
                            App.Save();
                            BuildPage(3);
                        }
                    }
                });
                AddButton("导出备份", "把看板与设置打成一个 zip，默认放 data\\backups", y, false, delegate { App.ExportBackup(); });
                AddButton("导入备份", "会先给当前状态留一份再覆盖", y, false, delegate { App.ImportBackup(); });
                AddButton("打开数据目录", Paths.Data, y, false, delegate { Native.ShellOpen(Paths.Data); });
                AddButton("清理图标缓存", "图标按路径+时间缓存，异常时清一次", y, false, delegate { Icons.Clear(); App.RefreshAllBoards(); App.Toast("图标缓存已清理"); });
                AddButton("恢复默认布局", "只重置看板位置与默认四块，不删你的内容", y, true, delegate { App.ResetLayout(); });
            }
            else
            {
                AddInfo("版本", V.Name + " v" + V.Version + "\n" + V.Tagline + "\n\nMIT 许可 · 完全本地 · 不联网也能用核心功能", y);
                AddInfo("运行时", "正在读取…", y);
                AddInfo("快捷键", "Ctrl+Alt+L 显示/隐藏所有看板 · Ctrl+Alt+G 打开设置\n双击桌面控制条图标也能开设置。", y);
                AddButton("打开日志目录", Paths.Logs, y, false, delegate { Native.ShellOpen(Paths.Logs); });
                AddButton("退出 Liuli", "看板与图标状态会先保存", y, true, delegate { App.Quit(); });
            }
            if (index == 4 && rows.Count >= 2) rows[1].Text = "运行时";
        }

        void AddSlider(string title, string desc, int y0, int min, int max, string unit, Func<int> get, Action<int> set)
        {
            y0 = nextY;
            Row r = new Row();
            r.Kind = "slider"; r.Title = title; r.Desc = desc; r.Min = min; r.Max = max; r.Unit = unit;
            r.Get = get; r.Set = set;
            r.R = new Rectangle(contentX, y0, contentRight - contentX, Util.RoundI(86 * Theme.S));
            rows.Add(r);
            NextY(r.R);
        }

        void AddToggle(string title, string desc, int y0, Func<int> get, Action<int> set)
        {
            y0 = nextY;
            Row r = new Row();
            r.Kind = "toggle"; r.Title = title; r.Desc = desc; r.Get = get; r.Set = set;
            r.R = new Rectangle(contentX, y0, contentRight - contentX, Util.RoundI(70 * Theme.S));
            rows.Add(r);
            NextY(r.R);
        }

        void AddButton(string title, string desc, int y0, bool danger, Action click)
        {
            y0 = nextY;
            Row r = new Row();
            r.Kind = "button"; r.Title = title; r.Desc = desc; r.Click = click; r.Danger = danger;
            r.R = new Rectangle(contentX, y0, contentRight - contentX, Util.RoundI(64 * Theme.S));
            rows.Add(r);
            NextY(r.R);
        }

        void AddSeg(string title, string desc, int y0, string[] options, Func<int> get, Action<int> set)
        {
            y0 = nextY;
            Row r = new Row();
            r.Kind = "seg"; r.Title = title; r.Desc = desc; r.Options = options; r.Get = get; r.Set = set;
            r.R = new Rectangle(contentX, y0, contentRight - contentX, Util.RoundI(88 * Theme.S));
            rows.Add(r);
            NextY(r.R);
        }

        void AddInfo(string title, string text, int y0)
        {
            y0 = nextY;
            Row r = new Row();
            r.Kind = "info"; r.Title = title; r.Text = text;
            int lines = 1;
            for (int i = 0; i < text.Length; i++) if (text[i] == '\n') lines++;
            int h = Math.Max(Util.RoundI(72 * Theme.S), Util.RoundI((26 + lines * 15) * Theme.S));
            r.R = new Rectangle(contentX, y0, contentRight - contentX, h);
            rows.Add(r);
            NextY(r.R);
        }

        void NextY(Rectangle r)
        {
            // 下一行 y 由最后一行决定；这里存到 Row 的下一个构建起点
            nextY = r.Bottom + Util.RoundI(4 * Theme.S);
        }

        int nextY;

        // ---------- 绘制 ----------
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            float s = Theme.S;
            Rectangle all = new Rectangle(0, 0, Width, Height);

            // 与看板同一块磨砂底：设置在桌面上也像一块玻璃
            Rectangle screenRect = new Rectangle(Left, Top, Width, Height);
            Backdrop.Draw(g, Util.ToF(all), screenRect);
            using (SolidBrush b = new SolidBrush(Theme.Light ? Color.FromArgb(212, 244, 247, 251) : Color.FromArgb(204, 14, 18, 26)))
                g.FillRectangle(b, all);
            using (LinearGradientBrush lb = new LinearGradientBrush(all, Color.FromArgb(0, 255, 255, 255), Theme.Light ? Color.FromArgb(26, 255, 255, 255) : Color.FromArgb(0, 0, 0, 0), 115f))
                g.FillRectangle(lb, all);
            // 顶部高光与边框
            using (LinearGradientBrush lb = new LinearGradientBrush(new Rectangle(24, 0, Width - 48, 2), Color.FromArgb(150, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 0f))
                g.FillRectangle(lb, 24, 0, Width - 48, 1);
            using (Pen pen = new Pen(Theme.Light ? Color.FromArgb(70, 0, 0, 0) : Color.FromArgb(60, 255, 255, 255), 1f)) g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);

            // 标题
            Rectangle hd = new Rectangle(Util.RoundI(26 * s), Util.RoundI(18 * s), Util.RoundI(400 * s), Util.RoundI(28 * s));
            Theme.Text(g, "Liuli 琉璃", Theme.Font(15f, FontStyle.Bold), Theme.TextMain, hd, StringAlignment.Near, false);
            Rectangle hsub = new Rectangle(hd.X, Util.RoundI(44 * s), Util.RoundI(430 * s), Util.RoundI(20 * s));
            Theme.Text(g, "桌面玻璃看板 · v" + V.Version, Theme.Font(9.5f, FontStyle.Regular), Theme.TextFaint, hsub, StringAlignment.Near, false);

            closeR = new Rectangle(Width - Util.RoundI(50 * s), Util.RoundI(16 * s), Util.RoundI(30 * s), Util.RoundI(30 * s));
            Glass.Button(g, closeR, Glyph.Close, hoverRow == -9, false, Theme.Accent);

            // 左侧导航
            int navW = Util.RoundI(150 * s);
            for (int i = 0; i < nav.Length; i++)
            {
                Rectangle r = new Rectangle(Util.RoundI(14 * s), Util.RoundI(96 * s) + i * Util.RoundI(44 * s), navW - Util.RoundI(6 * s), Util.RoundI(38 * s));
                navRects[i] = r;
                if (page == i)
                {
                    using (GraphicsPath p = Theme.Round(r, Util.RoundI(11 * s)))
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(44, Theme.Accent))) g.FillPath(b, p);
                    using (SolidBrush b = new SolidBrush(Theme.Accent))
                        g.FillRectangle(b, r.X + 2, r.Y + Util.RoundI(10 * s), Util.RoundI(3 * s), r.Height - Util.RoundI(20 * s));
                }
                else if (hoverRow == -100 - i)
                {
                    using (GraphicsPath p = Theme.Round(r, Util.RoundI(11 * s)))
                    using (SolidBrush b = new SolidBrush(Theme.Hover)) g.FillPath(b, p);
                }
                Color col = page == i ? Theme.TextMain : Theme.TextSub;
                Theme.TextMiddle(g, nav[i], Theme.Font(12f, page == i ? FontStyle.Bold : FontStyle.Regular), col, r, StringAlignment.Near);
            }
            // 导航竖线
            using (Pen pen = new Pen(Theme.Hair, 1f))
                g.DrawLine(pen, navW, Util.RoundI(90 * s), navW, Height - Util.RoundI(60 * s));

            // 内容
            foreach (Row r in rows) PaintRow(g, r, s);

            // 底部
            int by = Height - Util.RoundI(42 * s);
            using (Pen pen = new Pen(Theme.Hair, 1f)) g.DrawLine(pen, Util.RoundI(24 * s), by, Width - Util.RoundI(24 * s), by);
            string foot = "内存 " + (Environment.WorkingSet / 1048576) + " MB · 进程 CPU " + procCpu.ToString("0.0") + "% · 磨砂烘焙 " + (Backdrop.Ready ? "就绪" : "准备中");
            Rectangle fr = new Rectangle(Util.RoundI(26 * s), by + Util.RoundI(10 * s), Width - Util.RoundI(60 * s), Util.RoundI(20 * s));
            Theme.Text(g, foot, Theme.Font(9.5f, FontStyle.Regular), Theme.TextFaint, fr, StringAlignment.Near, true);

            SamplePerf();
        }

        void PaintRow(Graphics g, Row r, float s)
        {
            bool hot = hoverRow == rows.IndexOf(r);
            switch (r.Kind)
            {
                case "toggle":
                {
                    Theme.Text(g, r.Title, Theme.Font(12.5f, FontStyle.Bold), Theme.TextMain,
                        new Rectangle(r.R.X, r.R.Y + Util.RoundI(8 * s), r.R.Width - Util.RoundI(80 * s), Util.RoundI(22 * s)), StringAlignment.Near, true);
                    Theme.Text(g, r.Desc, Theme.Font(9.5f, FontStyle.Regular), Theme.TextFaint,
                        new Rectangle(r.R.X, r.R.Y + Util.RoundI(30 * s), r.R.Width - Util.RoundI(80 * s), Util.RoundI(18 * s)), StringAlignment.Near, true);
                    int sw = Util.RoundI(46 * s), sh = Util.RoundI(24 * s);
                    Rectangle track = new Rectangle(r.R.Right - sw, r.R.Y + Util.RoundI(14 * s), sw, sh);
                    int on = r.Get();
                    using (GraphicsPath p = Theme.Round(track, sh / 2))
                    using (SolidBrush b = new SolidBrush(on == 1 ? Color.FromArgb(220, Theme.Accent) : (Theme.Light ? Color.FromArgb(34, 12, 18, 30) : Color.FromArgb(36, 255, 255, 255))))
                        g.FillPath(b, p);
                    int knob = sh - Util.RoundI(6 * s);
                    int kx = on == 1 ? track.Right - knob - Util.RoundI(3 * s) : track.X + Util.RoundI(3 * s);
                    using (SolidBrush b = new SolidBrush(on == 1 ? Util.Rgb(13, 17, 25) : Util.Rgb(235, 241, 249)))
                        g.FillEllipse(b, kx, track.Y + Util.RoundI(3 * s), knob, knob);
                    break;
                }
                case "slider":
                {
                    Theme.Text(g, r.Title, Theme.Font(12.5f, FontStyle.Bold), Theme.TextMain,
                        new Rectangle(r.R.X, r.R.Y + Util.RoundI(4 * s), r.R.Width - Util.RoundI(80 * s), Util.RoundI(22 * s)), StringAlignment.Near, true);
                    int val = r.Get();
                    Rectangle vr = new Rectangle(r.R.Right - Util.RoundI(86 * s), r.R.Y + Util.RoundI(2 * s), Util.RoundI(86 * s), Util.RoundI(22 * s));
                    Theme.Text(g, val + r.Unit, Theme.Font(11f, FontStyle.Regular), Color.FromArgb(230, Theme.Accent), vr, StringAlignment.Far, false);
                    Theme.Text(g, r.Desc, Theme.Font(9.5f, FontStyle.Regular), Theme.TextFaint,
                        new Rectangle(r.R.X, r.R.Y + Util.RoundI(26 * s), r.R.Width, Util.RoundI(16 * s)), StringAlignment.Near, true);

                    Rectangle track = SliderTrack(r);
                    using (GraphicsPath p = Theme.Round(track, track.Height / 2))
                    using (SolidBrush b = new SolidBrush(Theme.Light ? Color.FromArgb(38, 12, 18, 30) : Color.FromArgb(28, 255, 255, 255))) g.FillPath(b, p);
                    float k = (val - r.Min) / (float)Math.Max(1, r.Max - r.Min);
                    int fillW = Math.Max(track.Height, (int)(track.Width * k));
                    Rectangle fill = new Rectangle(track.X, track.Y, fillW, track.Height);
                    using (GraphicsPath p = Theme.Round(fill, track.Height / 2))
                    using (LinearGradientBrush lb = new LinearGradientBrush(fill, Color.FromArgb(210, Theme.Accent), Color.FromArgb(240, Util.Mix(Theme.Accent, Color.White, 0.4f)), 0f))
                        g.FillPath(lb, p);
                    int knob = Util.RoundI(14 * s);
                    g.FillEllipse(new SolidBrush(Util.Rgb(240, 246, 255)), track.X + (int)(track.Width * k) - knob / 2, track.Y + (track.Height - knob) / 2, knob, knob);
                    break;
                }
                case "button":
                {
                    Rectangle rr = new Rectangle(r.R.X, r.R.Y + Util.RoundI(6 * s), r.R.Width, r.R.Height - Util.RoundI(10 * s));
                    using (GraphicsPath p = Theme.Round(rr, Util.RoundI(12 * s)))
                    using (SolidBrush b = new SolidBrush(Theme.Light ? (hot ? Color.FromArgb(50, 12, 18, 30) : Color.FromArgb(24, 12, 18, 30)) : (hot ? Color.FromArgb(40, 255, 255, 255) : Color.FromArgb(20, 255, 255, 255)))) g.FillPath(b, p);
                    using (GraphicsPath p = Theme.Round(rr, Util.RoundI(12 * s)))
                    using (Pen pen = new Pen(Theme.Light ? Color.FromArgb(hot ? 90 : 46, 12, 18, 30) : Color.FromArgb(hot ? 70 : 34, 255, 255, 255), 1f)) g.DrawPath(pen, p);
                    Color fg = r.Danger ? Theme.Danger : Theme.TextMain;
                    Theme.Text(g, r.Title, Theme.Font(11f, FontStyle.Bold), fg,
                        new Rectangle(rr.X + Util.RoundI(14 * s), rr.Y + Util.RoundI(8 * s), rr.Width - Util.RoundI(50 * s), Util.RoundI(22 * s)), StringAlignment.Near, true);
                    Theme.Text(g, r.Desc, Theme.Font(9.5f, FontStyle.Regular), Theme.TextFaint,
                        new Rectangle(rr.X + Util.RoundI(14 * s), rr.Y + Util.RoundI(28 * s), rr.Width - Util.RoundI(50 * s), Util.RoundI(16 * s)), StringAlignment.Near, true);
                    Theme.Glyph(g, Glyph.Chevron, new Rectangle(rr.Right - Util.RoundI(30 * s), rr.Y + (rr.Height - Util.RoundI(20 * s)) / 2, Util.RoundI(20 * s), Util.RoundI(20 * s)), Theme.TextSub, 1.3f * s);
                    break;
                }
                case "seg":
                {
                    Theme.Text(g, r.Title, Theme.Font(12.5f, FontStyle.Bold), Theme.TextMain,
                        new Rectangle(r.R.X, r.R.Y + Util.RoundI(2 * s), r.R.Width, Util.RoundI(20 * s)), StringAlignment.Near, false);
                    Rectangle seg = new Rectangle(r.R.X, r.R.Y + Util.RoundI(26 * s), Math.Min(r.R.Width, Util.RoundI(320 * s)), Util.RoundI(32 * s));
                    using (GraphicsPath p = Theme.Round(seg, Util.RoundI(16 * s)))
                    using (SolidBrush b = new SolidBrush(Theme.Light ? Color.FromArgb(30, 12, 18, 30) : Color.FromArgb(22, 255, 255, 255))) g.FillPath(b, p);
                    int n = r.Options.Length;
                    int sw = seg.Width / n;
                    int idx = r.Get();
                    for (int i = 0; i < n; i++)
                    {
                        Rectangle cell = new Rectangle(seg.X + i * sw + Util.RoundI(3 * s), seg.Y + Util.RoundI(3 * s), sw - Util.RoundI(6 * s), seg.Height - Util.RoundI(6 * s));
                        if (i == idx)
                        {
                            using (GraphicsPath p = Theme.Round(cell, Util.RoundI(13 * s)))
                            using (SolidBrush b = new SolidBrush(Color.FromArgb(70, Theme.Accent))) g.FillPath(b, p);
                        }
                        Theme.TextMiddle(g, r.Options[i], Theme.Font(9.5f, i == idx ? FontStyle.Bold : FontStyle.Regular),
                            i == idx ? Theme.TextMain : Theme.TextSub, cell, StringAlignment.Center);
                    }
                    Theme.Text(g, r.Desc, Theme.Font(9.5f, FontStyle.Regular), Theme.TextFaint,
                        new Rectangle(r.R.X + seg.Width + Util.RoundI(10 * s), r.R.Y + Util.RoundI(30 * s), r.R.Width - seg.Width - Util.RoundI(12 * s), Util.RoundI(18 * s)), StringAlignment.Near, true);
                    break;
                }
                case "info":
                {
                    Theme.Text(g, r.Title, Theme.Font(12.5f, FontStyle.Bold), Theme.TextMain,
                        new Rectangle(r.R.X, r.R.Y + Util.RoundI(2 * s), r.R.Width, Util.RoundI(20 * s)), StringAlignment.Near, false);
                    string body = r.Title == "运行时" ? RuntimeText() : r.Text;
                    Rectangle bodyR = new Rectangle(r.R.X, r.R.Y + Util.RoundI(24 * s), r.R.Width, r.R.Height - Util.RoundI(24 * s));
                    Theme.Text(g, body, Theme.Font(9f, FontStyle.Regular), Theme.TextSub, bodyR, StringAlignment.Near, false);
                    break;
                }
            }
        }

        string RuntimeText()
        {
            return "内存 " + (Environment.WorkingSet / 1048576) + " MB · 进程 CPU " + procCpu.ToString("0.0") + "%\n"
                + "磨砂烘焙 " + (Backdrop.Ready ? "就绪" : "准备中") + " · " + Backdrop.LastBuildMs.ToString("0") + " ms（换壁纸时一次）\n"
                + "看板 " + App.Ws.Boards.Count + " 块 · 壁纸 " + (App.Ws.Set.WallMode == "system" ? "跟随系统" : System.IO.Path.GetFileName(App.Ws.Set.WallPath));
        }

        Rectangle SliderTrack(Row r)
        {
            float s = Theme.S;
            return new Rectangle(r.R.X, r.R.Y + Util.RoundI(48 * s), r.R.Width, Util.RoundI(6 * s));
        }

        void SamplePerf()
        {
            long now = Environment.TickCount;
            if (now - lastPerfTick < 900) return;
            lastPerfTick = now;
            try
            {
                Process p = Process.GetCurrentProcess();
                if (lastProcWall != 0)
                {
                    double ms = now - lastProcWall;
                    double cpu = (p.TotalProcessorTime.Ticks - lastProcCpuTicks) / 10000.0 / ms * 100.0;
                    procCpu = (float)Math.Max(0, Math.Min(100, cpu));
                }
                lastProcWall = now;
                lastProcCpuTicks = p.TotalProcessorTime.Ticks;
            }
            catch { }
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
            int oldNav = hoverNav, oldRow = hoverRow;
            hoverNav = -1; hoverRow = -1;
            for (int i = 0; i < navRects.Length; i++) if (navRects[i].Contains(e.Location)) { hoverNav = i; hoverRow = -100 - i; }
            for (int i = 0; i < rows.Count; i++) if (rows[i].R.Contains(e.Location)) hoverRow = i;
            if (closeR.Contains(e.Location)) hoverRow = -9;

            if (dragRow >= 0) { ApplySlider(rows[dragRow], e.X); }
            if (oldNav != hoverNav || oldRow != hoverRow) Invalidate();
            Cursor = (hoverRow >= 0 || hoverNav >= 0) ? Cursors.Hand : (e.Y < Util.RoundI(70 * Theme.S) ? Cursors.SizeAll : Cursors.Default);
        }

        void OnDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (e.Y < Util.RoundI(70 * Theme.S) && !closeR.Contains(e.Location))
            {
                dragging = true;
                Point sp = PointToScreen(e.Location);
                dragOff = new Point(sp.X - Left, sp.Y - Top);
            }
        }

        void OnUp(object sender, MouseEventArgs e)
        {
            dragging = false;
            if (dragRow >= 0)
            {
                dragRow = -1;
                App.Save();
                App.RefreshAllBoards();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            for (int i = 0; i < navRects.Length; i++)
            {
                if (navRects[i].Contains(e.Location)) { BuildPage(i); Invalidate(); return; }
            }
            if (closeR.Contains(e.Location)) { Close(); return; }

            int hit = -1;
            for (int i = 0; i < rows.Count; i++) if (rows[i].R.Contains(e.Location)) { hit = i; break; }
            if (hit < 0) return;
            Row r = rows[hit];
            if (r.Kind == "toggle")
            {
                r.Set(r.Get() == 1 ? 0 : 1);
                Invalidate();
                App.Save();
                App.RefreshAllBoards();
            }
            else if (r.Kind == "button")
            {
                if (r.Click != null) r.Click();
                BuildPage(page);
                Invalidate();
            }
            else if (r.Kind == "slider")
            {
                dragRow = hit;
                ApplySlider(r, e.X);
            }
            else if (r.Kind == "seg")
            {
                Rectangle seg = new Rectangle(r.R.X, r.R.Y + Util.RoundI(26 * Theme.S), Math.Min(r.R.Width, Util.RoundI(320 * Theme.S)), Util.RoundI(32 * Theme.S));
                if (seg.Contains(e.Location))
                {
                    int n = r.Options.Length;
                    int idx = Util.Clamp((e.X - seg.X) * n / seg.Width, 0, n - 1);
                    r.Set(idx);
                    BuildPage(page);
                    Invalidate();
                    App.Save();
                }
            }
        }

        void ApplySlider(Row r, int mouseX)
        {
            Rectangle t = SliderTrack(r);
            float k = Util.ClampF((mouseX - t.X) / (float)Math.Max(1, t.Width), 0f, 1f);
            int v = r.Min + (int)Math.Round((r.Max - r.Min) * k);
            r.Set(v);
            Invalidate();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape) { Close(); return true; }
            if (keyData == Keys.Left) { page = Math.Max(0, page - 1); BuildPage(page); Invalidate(); return true; }
            if (keyData == Keys.Right) { page = Math.Min(nav.Length - 1, page + 1); BuildPage(page); Invalidate(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try { SetForegroundWindow(Handle); } catch { }
            Activate();
            SamplePerf();
            Log.Info("settings", "shown at " + Left + "," + Top + " " + Width + "x" + Height + " dpi=" + Theme.S);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            perfTimer.Stop();
            perfTimer.Dispose();
            App.SettingsClosed();
        }
    }
}
