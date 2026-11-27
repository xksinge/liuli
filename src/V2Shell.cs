// Liuli v2 · 新外壳：Dock + 灵动岛 + 任务栏自动隐藏 + 托盘/热键，旧看板入口不再创建。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace Liuli
{
    static class V2Shell
    {
        public const string ActivateMessageName = "Liuli.Desktop.Shell.v2.Activate";
        const int HotkeyToggle = 11;
        const int HotkeyMenu = 12;
        const uint MOD_ALT = 1, MOD_CONTROL = 2;

        public static ShellConfig Config;
        public static DockForm Dock;
        public static IslandForm Island;
        public static CollectorForm Collector;
        public static readonly List<DockForm> ExtraDocks = new List<DockForm>();
        public static readonly List<PanelForm> Panels = new List<PanelForm>();
        public static V2Host Host;
        public static bool HotkeyToggleOk;
        public static bool HotkeySettingsOk;

        public static string HotkeyStatus()
        {
            return "Ctrl+Alt+L 显示/隐藏：" + (HotkeyToggleOk ? "已注册" : "失败") + "\n" +
                "Ctrl+Alt+G 打开设置：" + (HotkeySettingsOk ? "已注册" : "失败");
        }

        public static bool ResetHotkeys()
        {
            if (Host == null || !Host.IsHandleCreated) return false;
            bool ok = Host.RegisterDefaultHotkeys();
            Toast(ok ? "快捷键已重新注册" : "快捷键注册失败，可能被其它程序占用");
            return ok;
        }

        public static int TextSizeIndex()
        {
            return Config == null ? 1 : Util.Clamp(Config.TextSize, 0, 3);
        }

        public static string TextSizeStatus()
        {
            int i = TextSizeIndex();
            return Theme.TextScaleName(i) + " · " + (int)(Theme.TextScaleOf(i) * 100) + "%";
        }

        public static void SetTextSize(int size)
        {
            if (Config == null) return;
            size = Util.Clamp(size, 0, 3);
            Config.TextSize = size;
            Theme.TextScale = Theme.TextScaleOf(size);
            Config.Save();
            RelayoutDocks();
            if (Island != null) Island.ApplyConfig();
            for (int i = 0; i < Panels.Count; i++) if (Panels[i] != null) Panels[i].ApplyTextScale();
            if (Collector != null) Collector.ApplyTextScale();
            if (Hub != null && !Hub.IsDisposed) Hub.ApplyTextScale();
            if (Settings != null && !Settings.IsDisposed) Settings.ApplyTextScale();
            Toast("文字大小：" + TextSizeStatus());
            Log.Info("textsize", TextSizeStatus());
        }

        public static IEnumerable<DockForm> LiveDocks()
        {
            if (Dock != null) yield return Dock;
            for (int i = 0; i < ExtraDocks.Count; i++) if (ExtraDocks[i] != null) yield return ExtraDocks[i];
        }

        public static void OpenExtraDocks()
        {
            CloseExtraDocks();
            if (Config == null || !Config.DockOn) return;
            if (!string.Equals(Config.DisplayMode, "all", StringComparison.OrdinalIgnoreCase)) return;
            Screen[] screens = Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                if (screens[i].Primary) continue;
                try
                {
                    DockForm d = new DockForm(screens[i]);
                    d.Show();
                    ExtraDocks.Add(d);
                }
                catch (Exception ex) { Log.Warn("dock", "副屏 Dock 创建失败 " + ex.Message); }
            }
            Log.Info("display", "screens=" + screens.Length + " extraDocks=" + ExtraDocks.Count);
        }

        public static void CloseExtraDocks()
        {
            for (int i = 0; i < ExtraDocks.Count; i++)
            {
                try { if (ExtraDocks[i] != null) { ExtraDocks[i].Hide(); ExtraDocks[i].Close(); } }
                catch { }
            }
            ExtraDocks.Clear();
        }

        public static void SetDisplayMode(string mode)
        {
            if (Config == null) return;
            Config.DisplayMode = ShellConfig.NormalizeDisplayMode(mode);
            Config.Save();
            if (Dock != null) { Dock.Show(); Dock.RelayoutPublic(); }
            OpenExtraDocks();
            Toast(Config.DisplayMode == "all" ? "多屏模式：所有屏 Dock" : "多屏模式：仅主屏");
        }

        static void ShowAllDocks()
        {
            if (Dock != null) { Dock.Show(); Dock.RelayoutPublic(); Dock.RefreshAutoHide(); }
            for (int i = 0; i < ExtraDocks.Count; i++) if (ExtraDocks[i] != null) { ExtraDocks[i].Show(); ExtraDocks[i].RelayoutPublic(); ExtraDocks[i].RefreshAutoHide(); }
        }

        static void HideAllDocks()
        {
            for (int i = 0; i < ExtraDocks.Count; i++) if (ExtraDocks[i] != null) ExtraDocks[i].Hide();
        }

        static void RelayoutDocks()
        {
            for (int i = 0; i < ExtraDocks.Count; i++) if (ExtraDocks[i] != null) ExtraDocks[i].RelayoutPublic();
            if (Dock != null) Dock.RelayoutPublic();
        }

        public static void OnDisplayChanged()
        {
            if (Config == null) return;
            Screen primary = Screen.PrimaryScreen;
            if (Dock != null && primary != null) Dock.RelayoutForScreen(primary);
            OpenExtraDocks();
            if (Island != null) { Island.ApplyConfig(); Island.ApplyPosition(); }
            for (int i = 0; i < Panels.Count; i++) if (Panels[i] != null) Panels[i].ApplyTextScale();
            if (Collector != null) Collector.ApplyTextScale();
            if (Hub != null && !Hub.IsDisposed) Hub.ApplyTextScale();
            if (Settings != null && !Settings.IsDisposed) Settings.ApplyTextScale();
            Toast("显示器变化，桌面层已重新排布");
            Log.Info("display", "reflow screens=" + Screen.AllScreens.Length + " taskbars=" + Taskbar.Describe());
        }

        public static HubForm Hub;
        public static WeatherService Weather;
        public static StatsNow Stats = new StatsNow();
        public static int ActivateMessage;
        public static V2SettingsForm Settings;

        static NotifyIcon tray;
        static Timer statsTimer;
        static Timer wallTimer;
        static bool allHidden;
        static bool desktopIconOriginalVisible = true;
        static bool desktopIconsHiddenByUs;
        static long bootAt;

        public static void Boot()
        {
            bootAt = Environment.TickCount;
            Config = ShellConfig.Load();
            Theme.TextScale = Theme.TextScaleOf(Config.TextSize);
            AutoStart.Apply(Config.AutoStart);
            Theme.BlurPct = Util.Clamp(Config.BlurPct, 10, 100);
            Theme.GlassPct = Util.Clamp(Config.GlassPct, 20, 140);
            Theme.WallpaperDim = Util.Clamp(Config.WallpaperDim, 0, 80);

            RecoverDesktopIcons();
            desktopIconOriginalVisible = Native.DesktopIconsVisible();
            ApplyDesktopSettings(false);

            Backdrop.Start();
            Config.ApplyTheme();

            Stats = SysStats.Sample();
            Weather = new WeatherService();
            Weather.LoadCache();
            try { Weather.Refresh(false); } catch (Exception ex) { Log.Warn("weather", ex.Message); }

            ApplyTaskbarPreference();

            Host = new V2Host();
            if (Config.DockOn)
            {
                Dock = new DockForm();
                Dock.Show();
                OpenExtraDocks();
            }
            if (Config.IslandOn)
            {
                Island = new IslandForm();
                Island.Show();
            }
            for (int i = 0; i < Config.Panels.Count; i++)
            {
                PanelForm pf = new PanelForm(Config.Panels[i]);
                Panels.Add(pf);
                pf.Show();
            }
            if (Config.CollectorOn)
            {
                Collector = new CollectorForm();
                Collector.Show();
            }

            statsTimer = new Timer();
            statsTimer.Interval = 2000;
            statsTimer.Tick += delegate
            {
                Stats = SysStats.Sample();
                if (Island != null && Island.Visible) Island.RefreshIsland();
                foreach (DockForm d in LiveDocks()) if (d.Visible) d.TickPositionPublic();
            };
            statsTimer.Start();

            wallTimer = new Timer();
            wallTimer.Interval = 6000;
            wallTimer.Tick += delegate
            {
                try
                {
                    if (!Backdrop.Tick()) return;
                    Config.ApplyTheme();
                    RelayoutDocks();
                    if (Island != null) Island.RefreshIsland();
                    Log.Info("wall", "壁纸变化，外壳配色已更新");
                }
                catch (Exception ex) { Log.Warn("wall", ex.Message); }
            };
            wallTimer.Start();

            Log.Info("v2", "外壳启动完成 ms=" + (Environment.TickCount - bootAt));
        }

        static void ApplyTaskbarPreference()
        {
            try
            {
                bool current = Taskbar.AutoHide();
                Config.TaskbarOriginalAutoHide = current;
                Log.Info("taskbar", "任务栏实例 " + Taskbar.Describe() + " autohide=" + current);
                if (Config.TaskbarAutoHide != current)
                {
                    bool ok = Taskbar.SetAutoHide(Config.TaskbarAutoHide);
                    Log.Info("taskbar", "自动隐藏=" + Config.TaskbarAutoHide + " ok=" + ok);
                }
                Config.Save();
            }
            catch (Exception ex) { Log.Warn("taskbar", "应用任务栏偏好失败：" + ex.Message); }
        }

        // ---------- 托盘 / 菜单 ----------
        public static void CreateTray()
        {
            if (tray != null) return;
            tray = new NotifyIcon();
            tray.Text = "Liuli · 桌面";
            tray.Icon = MakeIcon();
            tray.Visible = true;
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("打开设置", null, delegate { OpenSettings(); });
            menu.Items.Add("应用面板", null, delegate { OpenHub(HubForm.ModeApps); });
            menu.Items.Add("控制中心", null, delegate { OpenHub(HubForm.ModeControl); });
            menu.Items.Add("显示/隐藏桌面层", null, delegate { ToggleAll(); });
            menu.Items.Add("Dock 自动隐藏：开/关", null, delegate { ToggleDockAutoHide(); });
            menu.Items.Add("Dock 尺寸：小/标准/大（点击切换）", null, delegate { CycleDockPreset(); });
            menu.Items.Add("灵动岛设置", null, delegate { OpenIslandSettingsLater(Cursor.Position); });
            menu.Items.Add("新建面板", null, delegate { AddPanel(); });
            menu.Items.Add("桌面收纳：开/关", null, delegate { ToggleCollector(); });
            menu.Items.Add("重新扫描桌面收纳", null, delegate { ScanCollectorNow(); });
            menu.Items.Add("任务栏自动隐藏：开/关", null, delegate { ToggleTaskbarAutoHide(); });
            menu.Items.Add("恢复系统任务栏", null, delegate { RestoreTaskbar(); });
            menu.Items.Add("重载 Dock 默认组合", null, delegate { ResetDock(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("重新读取壁纸配色", null, delegate { ReloadWallpaperTheme(); });
            menu.Items.Add("打开数据目录", null, delegate { Native.ShellOpenFolder(Paths.Data); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("退出 Liuli", null, delegate { Quit(); });
            tray.ContextMenuStrip = menu;
            tray.DoubleClick += delegate { OpenSettings(); };
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
                    {
                        g.FillPath(lb, p);
                    }
                    using (Pen pen = new Pen(Color.FromArgb(160, 255, 255, 255), 1f)) g.DrawPath(pen, p);
                }
                using (SolidBrush wb = new SolidBrush(Color.White))
                using (StringFormat sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    g.DrawString("琉", new Font("Microsoft YaHei UI", 15f, FontStyle.Bold, GraphicsUnit.Pixel), wb,
                        new RectangleF(0, 0, 32, 32), sf);
                }
            }
            IntPtr h = b.GetHicon();
            b.Dispose();
            try { return Icon.FromHandle(h); } catch { return SystemIcons.Application; }
        }

        public static void ShowMenu(Point screen)
        {
            List<MenuForm.Item> items = new List<MenuForm.Item>();
            items.Add(new MenuForm.Item("打开设置", delegate { OpenSettings(); }, Glyph.Gear));
            items.Add(new MenuForm.Item("应用面板", delegate { OpenHub(HubForm.ModeApps); }, Glyph.Folder));
            items.Add(new MenuForm.Item("控制中心", delegate { OpenHub(HubForm.ModeControl); }, Glyph.Pulse));
            items.Add(new MenuForm.Item("显示/隐藏桌面层", delegate { ToggleAll(); }));
            items.Add(new MenuForm.Item("Dock 自动隐藏：开/关", delegate { ToggleDockAutoHide(); }, Glyph.Gear));
            items.Add(new MenuForm.Item("Dock 尺寸：" + DockPresetLabel(Config == null ? "standard" : Config.DockPreset) + "（点击切换）", delegate { CycleDockPreset(); }, Glyph.Gear));
            items.Add(new MenuForm.Item("灵动岛设置", delegate { OpenIslandSettingsLater(screen); }, Glyph.Gear));
            items.Add(new MenuForm.Item("新建面板", delegate { AddPanel(); }, Glyph.Plus));
            items.Add(new MenuForm.Item("桌面收纳：" + (Config != null && Config.CollectorOn ? "开" : "关"), delegate { ToggleCollector(); }, Glyph.Folder));
            items.Add(new MenuForm.Item("重新扫描桌面收纳", delegate { ScanCollectorNow(); }, Glyph.Restore));
            items.Add(new MenuForm.Item("任务栏自动隐藏：开/关", delegate { ToggleTaskbarAutoHide(); }, Glyph.Gear));
            items.Add(new MenuForm.Item("恢复系统任务栏", delegate { RestoreTaskbar(); }, Glyph.Restore));
            items.Add(new MenuForm.Item("重载 Dock 默认组合", delegate { ResetDock(); }, Glyph.Restore));
            items.Add(new MenuForm.Item("重新读取壁纸配色", delegate { ReloadWallpaperTheme(); }, Glyph.Image));
            items.Add(new MenuForm.Item("打开数据目录", delegate { Native.ShellOpenFolder(Paths.Data); }, Glyph.Folder));
            items.Add(new MenuForm.Item("退出 Liuli", delegate { Quit(); }, Glyph.Close, true));
            new MenuForm(items, new Size(Util.RoundI(220 * Theme.S), Util.RoundI(8 * Theme.S))).ShowAt(screen);
        }

        public static void ToggleAll()
        {
            if (allHidden) ShowAll();
            else HideAll();
        }

        public static void ShowAll()
        {
            allHidden = false;
            if (Config != null && Config.DockOn) ShowAllDocks();
            if (Config != null && Config.IslandOn)
            {
                if (Island == null) { Island = new IslandForm(); Island.Show(); }
                else { Island.Show(); Island.RefreshIsland(); }
            }
            for (int i = 0; i < Panels.Count; i++)
            {
                if (Panels[i] == null) continue;
                Panels[i].Show();
                Panels[i].ApplyBounds();
            }
            if (Config != null && Config.CollectorOn)
            {
                if (Collector == null) { Collector = new CollectorForm(); Collector.Show(); }
                else { Collector.Show(); Collector.ApplyBounds(); }
            }
        }

        public static void HideAll()
        {
            allHidden = true;
            if (Dock != null) Dock.Hide();
            HideAllDocks();
            if (Hub != null && Hub.Visible) Hub.Hide();
            if (Island != null) Island.Hide();
            if (Collector != null) Collector.Hide();
            for (int i = 0; i < Panels.Count; i++)
                if (Panels[i] != null) Panels[i].Hide();
        }

        public static void ToggleDockAutoHide()
        {
            if (Config == null) return;
            Config.DockAutoHide = !Config.DockAutoHide;
            Config.Save();
            foreach (DockForm d in LiveDocks()) d.RefreshAutoHide();
            Toast(Config.DockAutoHide ? "Dock 已设为自动隐藏，鼠标触底唤出" : "Dock 已设为常显");
        }

        public static string DockPresetLabel(string preset)
        {
            if (string.Equals(preset, "small", StringComparison.OrdinalIgnoreCase)) return "小";
            if (string.Equals(preset, "large", StringComparison.OrdinalIgnoreCase)) return "大";
            return "标准";
        }

        public static void CycleDockPreset()
        {
            if (Config == null) return;
            string next = "standard";
            if (string.Equals(Config.DockPreset, "standard", StringComparison.OrdinalIgnoreCase)) next = "large";
            else if (string.Equals(Config.DockPreset, "large", StringComparison.OrdinalIgnoreCase)) next = "small";
            Config.DockPreset = next;
            Config.Save();
            RelayoutDocks();
            Toast("Dock 尺寸：" + DockPresetLabel(next));
        }

        // ---------- 灵动岛设置 ----------

        static void OpenIslandSettingsLater(Point screen)
        {
            Timer t = new Timer();
            t.Interval = 140;
            t.Tick += delegate
            {
                t.Stop();
                t.Dispose();
                ShowIslandSettings(screen);
            };
            t.Start();
        }

        public static void ShowIslandSettings(Point screen)
        {
            if (Config == null) return;
            List<MenuForm.Item> items = new List<MenuForm.Item>();
            items.Add(new MenuForm.Item("宽度：" + Config.IslandWidth + "（点击切换）", delegate { CycleIslandWidth(); }, Glyph.Gear));
            items.Add(new MenuForm.Item("高度：" + Config.IslandHeight + "（点击切换）", delegate { CycleIslandHeight(); }, Glyph.Gear));
            items.Add(new MenuForm.Item("顶部偏移：" + Config.IslandTop + "px（点击切换）", delegate { CycleIslandTop(); }, Glyph.Gear));
            items.Add(new MenuForm.Item("展开动画：" + Config.IslandAnimMs + "ms（点击切换）", delegate { CycleIslandAnim(); }, Glyph.Gear));
            items.Add(new MenuForm.Item("天气：" + (Config.IslandWeather ? "开" : "关"), delegate { Config.IslandWeather = !Config.IslandWeather; ApplyIslandConfig(); }, Glyph.Gear));
            items.Add(new MenuForm.Item("系统读数：" + (Config.IslandStats ? "开" : "关"), delegate { Config.IslandStats = !Config.IslandStats; ApplyIslandConfig(); }, Glyph.Gear));
            items.Add(new MenuForm.Item("快捷动作：" + (Config.IslandQuick ? "开" : "关"), delegate { Config.IslandQuick = !Config.IslandQuick; ApplyIslandConfig(); }, Glyph.Gear));
            items.Add(new MenuForm.Item("恢复灵动岛默认", delegate { ResetIslandSettings(); }, Glyph.Restore));
            new MenuForm(items, new Size(Util.RoundI(200 * Theme.S), Util.RoundI(8 * Theme.S))).ShowAt(screen);
        }

        static int NextPreset(int current, int[] values)
        {
            for (int i = 0; i < values.Length; i++)
                if (current == values[i]) return values[(i + 1) % values.Length];
            for (int i = 0; i < values.Length; i++)
                if (current < values[i]) return values[i];
            return values[0];
        }

        public static void CycleIslandWidth()
        {
            if (Config == null) return;
            Config.IslandWidth = NextPreset(Config.IslandWidth, new int[] { 360, 380, 420, 460, 520 });
            ApplyIslandConfig();
            Toast("灵动岛宽度：" + Config.IslandWidth);
        }

        public static void CycleIslandHeight()
        {
            if (Config == null) return;
            Config.IslandHeight = NextPreset(Config.IslandHeight, new int[] { 100, 112, 136, 160, 180 });
            ApplyIslandConfig();
            Toast("灵动岛高度：" + Config.IslandHeight);
        }

        public static void CycleIslandTop()
        {
            if (Config == null) return;
            Config.IslandTop = NextPreset(Config.IslandTop, new int[] { 0, 6, 16, 28, 48 });
            ApplyIslandConfig();
            Toast("灵动岛顶部偏移：" + Config.IslandTop);
        }

        public static void CycleIslandAnim()
        {
            if (Config == null) return;
            Config.IslandAnimMs = NextPreset(Config.IslandAnimMs, new int[] { 0, 120, 180, 260, 360 });
            ApplyIslandConfig();
            Toast("灵动岛动画：" + Config.IslandAnimMs + "ms");
        }

        public static void ApplyIslandConfig()
        {
            if (Config == null) return;
            Config.NormalizeIsland();
            Config.Save();
            if (Island != null) Island.ApplyConfig();
        }

        public static void ResetIslandSettings()
        {
            if (Config == null) return;
            Config.IslandWidth = 380;
            Config.IslandHeight = 112;
            Config.IslandTop = 6;
            Config.IslandAnimMs = 180;
            Config.IslandWeather = true;
            Config.IslandStats = true;
            Config.IslandQuick = true;
            ApplyIslandConfig();
            Toast("灵动岛已恢复默认");
        }

        public static PanelForm AddPanel()
        {
            if (Config == null) return null;
            if (Config.Panels.Count >= 6)
            {
                Toast("面板最多 6 个");
                return null;
            }
            PanelConfig cfg = Config.NewPanel(Config.Panels.Count);
            MarkThemeDirty();
            Config.Panels.Add(cfg);
            Config.Save();
            PanelForm pf = new PanelForm(cfg);
            Panels.Add(pf);
            pf.Show();
            if (allHidden) pf.Hide();
            Toast("已新建面板，右键可切换内容");
            return pf;
        }

        public static void RemovePanel(PanelForm panel)
        {
            if (Config == null || panel == null || panel.Cfg == null) return;
            MarkThemeDirty();
            Panels.Remove(panel);
            Config.Panels.Remove(panel.Cfg);
            Config.Save();
            try { panel.Close(); } catch { }
            Toast("面板已删除");
        }

        public static void SavePanels()
        {
            if (Config == null) return;
            MarkThemeDirty();
            Config.Save();
        }

        public static void MarkThemeDirty()
        {
            if (Config != null && !string.IsNullOrEmpty(Config.ActiveTheme)) Config.ActiveTheme = "";
        }

        public static void RefreshPanels()
        {
            for (int i = 0; i < Panels.Count; i++)
            {
                if (Panels[i] != null && Panels[i].Visible) Panels[i].Render();
            }
        }

        public static void ToggleClock24()
        {
            if (Config == null) return;
            SetClock24(!Config.Clock24);
        }

        public static void SetClock24(bool use24)
        {
            if (Config == null) return;
            Config.Clock24 = use24;
            Config.Save();
            for (int i = 0; i < Panels.Count; i++)
            {
                if (Panels[i] == null) continue;
                Panels[i].ApplyTickInterval();
                Panels[i].Render();
            }
            Toast(use24 ? "时钟已切到 24 小时制" : "时钟已切到 12 小时制");
        }

        public static void ToggleClockSeconds()
        {
            if (Config == null) return;
            SetClockSeconds(!Config.ClockSeconds);
        }

        public static void SetClockSeconds(bool on)
        {
            if (Config == null) return;
            Config.ClockSeconds = on;
            Config.Save();
            for (int i = 0; i < Panels.Count; i++)
            {
                if (Panels[i] == null) continue;
                Panels[i].ApplyTickInterval();
                Panels[i].Render();
            }
            Toast(on ? "时钟已显示秒" : "时钟已隐藏秒");
        }

        public static void SetAutoStart(bool on)
        {
            if (Config == null) return;
            if (!AutoStart.Apply(on))
            {
                Toast("开机自启设置失败，请检查当前用户注册表权限");
                return;
            }
            Config.AutoStart = on;
            Config.Save();
            Toast(on ? "已开启开机自启（当前用户）" : "已关闭开机自启");
        }

        public static void OpenHub(int mode)
        {
            try
            {
                if (Hub == null || Hub.IsDisposed)
                {
                    Hub = new HubForm(mode);
                    Hub.Show();
                }
                else
                {
                    Hub.SetMode(mode);
                    Hub.Show();
                    Hub.ApplyTextScale();
                }
                Hub.Activate();
                Hub.BringToFront();
                Log.Info("hub", mode == HubForm.ModeControl ? "控制中心打开" : "应用面板打开");
            }
            catch (Exception ex) { Log.Warn("hub", "打开失败 " + ex.Message); Toast("应用面板打开失败"); }
        }

        public static void OpenSettings()
        {
            OpenSettings(0);
        }

        public static void OpenSettings(int startPage)
        {
            if (Settings != null && !Settings.IsDisposed)
            {
                Settings.Show();
                Settings.ShowPage(startPage);
                Settings.Activate();
                return;
            }
            Settings = new V2SettingsForm(startPage);
            Settings.Show();
            Settings.Activate();
        }

        public static void SettingsClosed()
        {
            Settings = null;
        }

        /// <summary>主题/外观参数变了以后统一重绘；磨砂变化时重烘壁纸。</summary>
        public static void ApplyVisualSettings(bool rebake)
        {
            if (Config == null) return;
            MarkThemeDirty();
            Config.ApplyTheme();
            if (rebake)
            {
                Backdrop.Rebuild();
                // 重烘会更新壁纸明暗与自动强调色，必须再应用一次 Theme token。
                Config.ApplyTheme();
            }
            foreach (DockForm d in LiveDocks()) { d.RelayoutPublic(); d.Render(); }
            if (Island != null) Island.RefreshIsland();
            RefreshPanels();
            if (Collector != null) Collector.Render();
        }

        public static void SetDockOn(bool on)
        {
            if (Config == null) return;
            MarkThemeDirty();
            Config.DockOn = on;
            Config.Save();
            if (on)
            {
                if (Dock == null) { Dock = new DockForm(); Dock.Show(); }
                else { Dock.Show(); Dock.RelayoutPublic(); Dock.RefreshAutoHide(); }
                OpenExtraDocks();
                if (allHidden) HideAllDocks();
            }
            else if (Dock != null)
            {
                CloseExtraDocks();
                Dock.Hide();
                Dock.Close();
                Dock = null;
            }
        }

        public static void SetDockAutoHide(bool on)
        {
            if (Config == null) return;
            MarkThemeDirty();
            Config.DockAutoHide = on;
            Config.Save();
            foreach (DockForm d in LiveDocks()) d.RefreshAutoHide();
        }

        public static void ToggleCollector()
        {
            if (Config == null) return;
            SetCollectorOn(!Config.CollectorOn);
        }

        public static void SetCollectorOn(bool on)
        {
            if (Config == null) return;
            MarkThemeDirty();
            Config.CollectorOn = on;
            Config.Save();
            if (on)
            {
                if (Collector == null) { Collector = new CollectorForm(); Collector.Show(); }
                else { Collector.Show(); Collector.ApplyBounds(); }
                if (allHidden && Collector != null) Collector.Hide();
                CollectorState.Sync(true);
                if (Collector != null) Collector.Render();
                Toast("桌面收纳已开启；建议在桌面页隐藏原生图标");
            }
            else if (Collector != null)
            {
                Collector.Hide();
                Collector.Close();
                Collector = null;
                Toast("桌面收纳已关闭，原文件未移动；桌面图标仍按桌面页设置");
            }
        }

        public static void ScanCollectorNow()
        {
            if (Config == null) return;
            bool changed = CollectorState.Sync(true);
            if (Collector != null) Collector.Render();
            Toast(changed ? "桌面收纳已刷新" : "桌面没有变化");
        }

        public static void RefreshCollector()
        {
            if (Collector == null) return;
            Collector.ApplyBounds();
            Collector.Render();
        }

        public static void SetDockPreset(string preset)
        {
            if (Config == null) return;
            MarkThemeDirty();
            Config.DockPreset = string.Equals(preset, "small", StringComparison.OrdinalIgnoreCase) ? "small" :
                (string.Equals(preset, "large", StringComparison.OrdinalIgnoreCase) ? "large" : "standard");
            Config.Save();
            RelayoutDocks();
        }

        public static void ApplyIslandSettings()
        {
            if (Config == null) return;
            MarkThemeDirty();
            Config.NormalizeIsland();
            Config.Save();
            if (!Config.IslandOn)
            {
                if (Island != null) { Island.Hide(); Island.Close(); Island = null; }
                return;
            }
            if (Island == null)
            {
                Island = new IslandForm();
                Island.Show();
            }
            else
            {
                Island.Show();
                Island.ApplyConfig();
            }
            if (allHidden && Island != null) Island.Hide();
        }

        public static void SaveCurrentTheme(string name)
        {
            if (Config == null) return;
            ThemeSnapshot t = Config.SaveTheme(name);
            Toast("主题已保存：" + t.Name);
            if (Settings != null) Settings.ReloadPage();
        }

        public static void DeleteTheme(string name)
        {
            if (Config == null) return;
            if (Config.DeleteTheme(name))
            {
                Toast("主题已删除：" + name);
                if (Settings != null) Settings.ReloadPage();
            }
        }

        public static void LoadTheme(ThemeSnapshot t)
        {
            if (Config == null || t == null) return;
            t.ApplyTo(Config);
            if (!string.IsNullOrEmpty(t.WallpaperPath) && File.Exists(t.WallpaperPath))
                Wallpaper.Set(t.WallpaperPath, t.WallpaperStyle, t.WallpaperTile);
            // 先应用主题 token（含压暗），再重烘玻璃底，最后用新壁纸的明暗重新算一次文字色。
            Config.ApplyTheme();
            Backdrop.Rebuild();
            Config.ApplyTheme();
            RebuildPanels();
            ApplyDockRuntime();
            ApplyIslandRuntime();
            Config.Save();
            if (Settings != null) Settings.ReloadPage();
            Toast("已加载主题：" + t.Name);
        }

        public static void RestoreVisualDefaults()
        {
            if (Config == null) return;
            Config.GlassPct = 100;
            Config.BlurPct = 62;
            Config.Radius = 20;
            Config.WallpaperDim = 0;
            Config.ThemeMode = "auto";
            Config.ThemePreset = "mist";
            Config.AccentMode = "auto";
            Config.AccentPreset = 0;
            Config.AccentCustom = "#7EB2FF";
            Config.ActiveTheme = "";
            Config.Save();
            ApplyVisualSettings(true);
            if (Settings != null) Settings.ReloadPage();
            Toast("外观已恢复默认");
        }

        /// <summary>把一张本地图片设为 Windows 壁纸；首次换图前记住原始路径，供恢复使用。</summary>
        public static bool SetWallpaperImage(string path)
        {
            if (Config == null || string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
            try
            {
                if (string.IsNullOrEmpty(Config.WallpaperOrig))
                {
                    Config.WallpaperOrig = Wallpaper.CurrentPath();
                    int tile = 0;
                    string raw = Wallpaper.GetStyleRaw(out tile);
                    int style = 10;
                    int.TryParse(raw, out style);
                    Config.WallpaperOrigStyle = Util.Clamp(style, 0, 100);
                    Config.WallpaperOrigTile = Util.Clamp(tile, 0, 1);
                }
                if (!Wallpaper.Set(path, 10, 0))
                {
                    Toast("壁纸应用失败");
                    return false;
                }
                Config.Save();
                ApplyVisualSettings(true);
                Toast("壁纸已切换：" + Path.GetFileName(path));
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn("wall", "切换壁纸失败 " + ex.Message);
                Toast("壁纸应用失败");
                return false;
            }
        }

        /// <summary>回到首次换图前记录的系统壁纸。</summary>
        public static bool RestoreSystemWallpaper()
        {
            if (Config == null) return false;
            if (string.IsNullOrEmpty(Config.WallpaperOrig) || !File.Exists(Config.WallpaperOrig))
            {
                Toast("没有保存的原始壁纸");
                return false;
            }
            try
            {
                if (!Wallpaper.Set(Config.WallpaperOrig, Config.WallpaperOrigStyle, Config.WallpaperOrigTile))
                {
                    Toast("恢复壁纸失败");
                    return false;
                }
                Config.WallpaperOrig = "";
                Config.Save();
                ApplyVisualSettings(true);
                Toast("已恢复系统壁纸");
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn("wall", "恢复壁纸失败 " + ex.Message);
                Toast("恢复壁纸失败");
                return false;
            }
        }

        static void RebuildPanels()
        {
            for (int i = 0; i < Panels.Count; i++)
            {
                try { if (Panels[i] != null) Panels[i].Close(); } catch { }
            }
            Panels.Clear();
            for (int i = 0; i < Config.Panels.Count; i++)
            {
                PanelForm pf = new PanelForm(Config.Panels[i]);
                Panels.Add(pf);
                pf.Show();
                if (allHidden) pf.Hide();
            }
        }

        static void ApplyDockRuntime()
        {
            if (Config.DockOn)
            {
                if (Dock == null) { Dock = new DockForm(); Dock.Show(); }
                else { Dock.Show(); Dock.RelayoutPublic(); Dock.RefreshAutoHide(); }
                OpenExtraDocks();
                if (allHidden) HideAllDocks();
            }
            else if (Dock != null)
            {
                try { CloseExtraDocks(); } catch { }
                try { Dock.Close(); } catch { }
                Dock = null;
            }
        }

        static void ApplyIslandRuntime()
        {
            if (Config.IslandOn)
            {
                if (Island == null) { Island = new IslandForm(); Island.Show(); }
                else { Island.Show(); Island.ApplyConfig(); }
                if (allHidden && Island != null) Island.Hide();
            }
            else if (Island != null)
            {
                try { Island.Close(); } catch { }
                Island = null;
            }
        }

        public static void ToggleTaskbarAutoHide()
        {
            bool on = !Taskbar.AutoHide();
            bool ok = Taskbar.SetAutoHide(on);
            if (ok)
            {
                Config.TaskbarAutoHide = on;
                Config.Save();
                foreach (DockForm d in LiveDocks()) d.TickPositionPublic();
                Toast(on ? "任务栏已设为自动隐藏" : "任务栏已恢复常显");
            }
            else Toast("任务栏设置失败，请检查系统权限");
        }

        public static void RestoreTaskbar()
        {
            bool target = Config != null ? Config.TaskbarOriginalAutoHide : true;
            bool ok = Taskbar.SetAutoHide(target);
            if (Config != null) { Config.TaskbarAutoHide = target; Config.Save(); }
            if (ok) foreach (DockForm d in LiveDocks()) d.TickPositionPublic();
            Toast(ok ? (target ? "任务栏已恢复为自动隐藏" : "任务栏已恢复为常显") : "任务栏设置失败");
        }

        public static void ReloadWallpaperTheme()
        {
            try
            {
                Backdrop.Rebuild();
                Config.ApplyTheme();
                RelayoutDocks();
                if (Island != null) Island.RefreshIsland();
                RefreshPanels();
                Toast("已按当前壁纸重新取色");
            }
            catch (Exception ex) { Log.Warn("wall", ex.Message); Toast("取色失败，已保留原配色"); }
        }

        public static void ResetDock()
        {
            Config.DockItems = ShellConfig.DefaultDock();
            Config.DockProfiles.Clear();
            Config.Save();
            if (Dock == null && Config.DockOn) { Dock = new DockForm(); Dock.Show(); OpenExtraDocks(); }
            else if (Dock != null) { Dock.RelayoutPublic(); OpenExtraDocks(); }
            Toast("Dock 已恢复默认组合");
        }

        public static string DockScreenKey(Screen screen)
        {
            if (screen == null) return "";
            return screen.DeviceName == null ? "" : screen.DeviceName;
        }

        static DockProfile FindDockProfile(string key)
        {
            if (Config == null || key.Length == 0) return null;
            for (int i = 0; i < Config.DockProfiles.Count; i++)
                if (Config.DockProfiles[i] != null && string.Equals(Config.DockProfiles[i].Screen, key, StringComparison.OrdinalIgnoreCase))
                    return Config.DockProfiles[i];
            return null;
        }

        static DockItem CloneDockItem(DockItem it)
        {
            if (it == null) return null;
            DockItem copy = new DockItem();
            copy.Kind = it.Kind;
            copy.Id = it.Id;
            copy.Path = it.Path;
            copy.Label = it.Label;
            return copy;
        }

        /// <summary>该屏当前使用的 Dock 列表：关闭“每屏独立”时全部共享 dockItems；主屏始终用共享列表。</summary>
        public static List<DockItem> DockItemsFor(Screen screen)
        {
            if (Config == null) return new List<DockItem>();
            if (!Config.DockPerScreen || screen == null || screen.Primary) return Config.DockItems;
            DockProfile p = FindDockProfile(DockScreenKey(screen));
            return p == null ? Config.DockItems : p.Items;
        }

        /// <summary>第一次在副屏排序/移除/固定时，把共享列表深拷贝成该屏的独立布局。</summary>
        static List<DockItem> EnsureDockItems(Screen screen)
        {
            if (Config == null) return new List<DockItem>();
            if (!Config.DockPerScreen || screen == null || screen.Primary) return Config.DockItems;
            string key = DockScreenKey(screen);
            DockProfile p = FindDockProfile(key);
            if (p != null) return p.Items;
            if (Config.DockProfiles.Count >= 4)
            {
                Log.Warn("dock", "每屏布局已达 4 个上限，忽略新屏 " + key);
                return null;
            }
            p = new DockProfile();
            p.Screen = key;
            for (int i = 0; i < Config.DockItems.Count; i++) p.Items.Add(CloneDockItem(Config.DockItems[i]));
            Config.DockProfiles.Add(p);
            Log.Info("dock", "建立副屏独立布局 " + key + " n=" + p.Items.Count);
            return p.Items;
        }

        public static int PinPaths(string[] files)
        {
            return PinPaths(files, Screen.PrimaryScreen);
        }

        public static int PinPaths(string[] files, Screen screen)
        {
            if (files == null || Config == null) return 0;
            List<DockItem> target = EnsureDockItems(screen);
            if (target == null) return 0;
            int added = 0;
            for (int i = 0; i < files.Length && target.Count < 16; i++)
            {
                string p = files[i];
                if (p == null || p.Trim().Length == 0) continue;
                if (!File.Exists(p) && !Directory.Exists(p))
                {
                    Log.Warn("dock", "拖入项不存在，跳过：" + p);
                    continue;
                }
                DockItem item = new DockItem();
                item.Kind = "app";
                item.Path = p;
                item.Label = SafeLabel(p);
                bool has = false;
                for (int k = 0; k < target.Count; k++)
                    if (target[k].SameAs(item)) { has = true; break; }
                if (has) continue;
                target.Add(item);
                added++;
            }
            if (added > 0) Config.Save();
            if (added > 0) RelayoutDocks();
            return added;
        }

        static string SafeLabel(string path)
        {
            try
            {
                if (Directory.Exists(path)) return new DirectoryInfo(path).Name;
                string name = Path.GetFileNameWithoutExtension(path);
                return name.Length > 0 ? name : Path.GetFileName(path);
            }
            catch { return "应用"; }
        }

        public static void RemoveDockItem(int index)
        {
            RemoveDockItem(index, Screen.PrimaryScreen);
        }

        public static void RemoveDockItem(int index, Screen screen)
        {
            if (Config == null) return;
            List<DockItem> items = EnsureDockItems(screen);
            if (items == null) return;
            if (index < 0 || index >= items.Count) return;
            items.RemoveAt(index);
            Config.Save();
            Toast("已从 Dock 移除");
            RelayoutDocks();
        }

        public static int MoveDockItem(int from, int before)
        {
            return MoveDockItem(from, before, Screen.PrimaryScreen);
        }

        public static int MoveDockItem(int from, int before, Screen screen)
        {
            if (Config == null) return 0;
            List<DockItem> items = EnsureDockItems(screen);
            if (items == null) return 0;
            if (from < 0 || from >= items.Count) return 0;
            if (before < 0) before = 0;
            if (before > items.Count) before = items.Count;
            int target = before;
            if (target > from) target--;
            if (target == from) return 0;
            DockItem item = items[from];
            items.RemoveAt(from);
            items.Insert(target, item);
            Config.Save();
            return 1;
        }

        public static void SetDockPerScreen(bool on)
        {
            if (Config == null) return;
            Config.DockPerScreen = on;
            Config.Save();
            RelayoutDocks();
            Toast(on ? "已开启每屏独立 Dock 布局" : "已恢复共享 Dock 布局，各屏布局仍保留");
            Log.Info("dock", "每屏独立布局=" + on + " profiles=" + Config.DockProfiles.Count + " kept=1");
        }

        // ---------- 生命周期 / 工具 ----------
        public static void UI(Action a)
        {
            if (a == null) return;
            try
            {
                if (Host != null && Host.IsHandleCreated) Host.BeginInvoke(a);
                else a();
            }
            catch { try { a(); } catch { } }
        }

        static string DesktopMarkerPath()
        {
            return Path.Combine(Paths.Data, "desktop-icons.runtime");
        }

        /// <summary>上次强退留下的运行期隐藏：下次启动先按标记恢复，再重新校准。</summary>
        static void RecoverDesktopIcons()
        {
            try
            {
                string p = DesktopMarkerPath();
                if (!File.Exists(p)) return;
                string s = File.ReadAllText(p).Trim();
                bool visible = s != "0";
                Native.SetDesktopIconsVisible(visible);
                File.Delete(p);
                Log.Info("icons", "上次异常退出，已校准桌面图标 visible=" + visible);
            }
            catch (Exception ex) { Log.Warn("icons", "校准桌面图标失败 " + ex.Message); }
        }

        static void SaveDesktopMarker(bool originalVisible)
        {
            try
            {
                Directory.CreateDirectory(Paths.Data);
                File.WriteAllText(DesktopMarkerPath(), originalVisible ? "1" : "0");
            }
            catch (Exception ex) { Log.Warn("icons", "写桌面图标标记失败 " + ex.Message); }
        }

        static void DeleteDesktopMarker()
        {
            try { if (File.Exists(DesktopMarkerPath())) File.Delete(DesktopMarkerPath()); } catch { }
        }

        /// <summary>应用桌面页配置：全部图标运行期隐藏 + 五个系统图标注册表开关。</summary>
        public static bool ApplyDesktopSettings(bool announce)
        {
            if (Config == null) return false;
            bool ok = true;
            bool wantHidden = Config.DesktopIconsHidden;
            bool visible = Native.DesktopIconsVisible();
            if (visible == wantHidden)
            {
                if (!Native.SetDesktopIconsVisible(!wantHidden))
                {
                    ok = false;
                    Log.Warn("icons", "桌面图标显隐失败 visible=" + visible + " wantHidden=" + wantHidden);
                }
                else if (wantHidden) desktopIconsHiddenByUs = visible;
                else desktopIconsHiddenByUs = false;
            }
            else if (!wantHidden) desktopIconsHiddenByUs = false;

            if (desktopIconsHiddenByUs && desktopIconOriginalVisible) SaveDesktopMarker(true);
            else DeleteDesktopMarker();

            if (!DesktopState.ApplyShellIcons(Config)) ok = false;
            DesktopState.Refresh();
            Log.Info("icons", "桌面 hidden=" + wantHidden + " visible=" + Native.DesktopIconsVisible() +
                " ok=" + ok + " originalVisible=" + desktopIconOriginalVisible);
            if (announce) Toast(ok ? (wantHidden ? "桌面图标已隐藏（运行期）" : "桌面图标已显示") : "桌面图标切换失败");
            return ok;
        }

        public static void Toast(string text)
        {
            UI(delegate { try { ToastForm.Show(text); } catch { } });
        }

        public static void Quit()
        {
            try
            {
                if (Config != null && Config.TaskbarFullHide)
                {
                    int n = Taskbar.ShowAll();
                    Log.Info("taskbar", "退出恢复任务栏 n=" + n);
                    Config.TaskbarFullHide = false;
                    Config.Save();
                }
            }
            catch { }
            try { if (tray != null) { tray.Visible = false; tray.Dispose(); tray = null; } } catch { }
            try { if (statsTimer != null) statsTimer.Stop(); } catch { }
            try { if (wallTimer != null) wallTimer.Stop(); } catch { }
            try { CloseExtraDocks(); } catch { }
            try { if (Hub != null) Hub.Close(); } catch { }
            try { if (Dock != null) Dock.Close(); } catch { }
            try { if (Island != null) Island.Close(); } catch { }
            try { if (Collector != null) Collector.Close(); } catch { }
            try { if (Settings != null) Settings.Close(); } catch { }
            for (int i = 0; i < Panels.Count; i++) { try { if (Panels[i] != null) Panels[i].Close(); } catch { } }
            try
            {
                if (Config != null && Config.DesktopIconsHidden && desktopIconsHiddenByUs)
                    Native.SetDesktopIconsVisible(desktopIconOriginalVisible);
            }
            catch { }
            DeleteDesktopMarker();
            try { if (Host != null) Host.Close(); } catch { }
            Application.Exit();
        }

        public static void DisplayChanged()
        {
            UI(delegate { OnDisplayChanged(); });
        }
    }

    sealed class V2Host : Form
    {
        public V2Host()
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
                V2Shell.ActivateMessage = Native.RegisterWindowMessage(V2Shell.ActivateMessageName);
                RegisterDefaultHotkeys();
                V2Shell.CreateTray();
            }
            catch (Exception ex) { Log.Warn("hotkey", ex.Message); }
        }

        public bool RegisterDefaultHotkeys()
        {
            try
            {
                Native.UnregisterHotKey(Handle, 11);
                Native.UnregisterHotKey(Handle, 12);
                V2Shell.HotkeyToggleOk = Native.RegisterHotKey(Handle, 11, 2 | 1, (uint)Keys.L);
                V2Shell.HotkeySettingsOk = Native.RegisterHotKey(Handle, 12, 2 | 1, (uint)Keys.G);
                Log.Info("hotkey", "toggle=" + (V2Shell.HotkeyToggleOk ? "ok" : "fail") +
                    " settings=" + (V2Shell.HotkeySettingsOk ? "ok" : "fail"));
                return V2Shell.HotkeyToggleOk && V2Shell.HotkeySettingsOk;
            }
            catch (Exception ex)
            {
                V2Shell.HotkeyToggleOk = false;
                V2Shell.HotkeySettingsOk = false;
                Log.Warn("hotkey", ex.Message);
                return false;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312)
            {
                int id = m.WParam.ToInt32();
                if (id == 11) V2Shell.ToggleAll();
                else if (id == 12) V2Shell.OpenSettings();
                return;
            }
            if (V2Shell.ActivateMessage != 0 && m.Msg == V2Shell.ActivateMessage)
            {
                V2Shell.ShowAll();
                V2Shell.ShowMenu(Cursor.Position);
                return;
            }
            if (m.Msg == 0x007E)
            {
                V2Shell.DisplayChanged();
                return;
            }
            base.WndProc(ref m);
        }
    }
}
