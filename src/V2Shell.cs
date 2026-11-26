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
        public static readonly List<PanelForm> Panels = new List<PanelForm>();
        public static V2Host Host;
        public static WeatherService Weather;
        public static StatsNow Stats = new StatsNow();
        public static int ActivateMessage;

        static NotifyIcon tray;
        static Timer statsTimer;
        static Timer wallTimer;
        static bool allHidden;
        static long bootAt;

        public static void Boot()
        {
            bootAt = Environment.TickCount;
            Config = ShellConfig.Load();
            Theme.BlurPct = Util.Clamp(Config.BlurPct, 10, 100);
            Theme.GlassPct = Util.Clamp(Config.GlassPct, 20, 140);

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

            statsTimer = new Timer();
            statsTimer.Interval = 2000;
            statsTimer.Tick += delegate
            {
                Stats = SysStats.Sample();
                if (Island != null && Island.Visible) Island.RefreshIsland();
                if (Dock != null && Dock.Visible) Dock.TickPositionPublic();
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
                    if (Dock != null) { Dock.RelayoutPublic(); }
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
            menu.Items.Add("显示/隐藏桌面层", null, delegate { ToggleAll(); });
            menu.Items.Add("Dock 自动隐藏：开/关", null, delegate { ToggleDockAutoHide(); });
            menu.Items.Add("Dock 尺寸：小/标准/大（点击切换）", null, delegate { CycleDockPreset(); });
            menu.Items.Add("灵动岛设置", null, delegate { OpenIslandSettingsLater(Cursor.Position); });
            menu.Items.Add("新建面板", null, delegate { AddPanel(); });
            menu.Items.Add("任务栏自动隐藏：开/关", null, delegate { ToggleTaskbarAutoHide(); });
            menu.Items.Add("恢复系统任务栏", null, delegate { RestoreTaskbar(); });
            menu.Items.Add("重载 Dock 默认组合", null, delegate { ResetDock(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("重新读取壁纸配色", null, delegate { ReloadWallpaperTheme(); });
            menu.Items.Add("打开数据目录", null, delegate { Native.ShellOpenFolder(Paths.Data); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("退出 Liuli", null, delegate { Quit(); });
            tray.ContextMenuStrip = menu;
            tray.DoubleClick += delegate { ShowMenu(Cursor.Position); };
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
            items.Add(new MenuForm.Item("显示/隐藏桌面层", delegate { ToggleAll(); }));
            items.Add(new MenuForm.Item("Dock 自动隐藏：开/关", delegate { ToggleDockAutoHide(); }, Glyph.Gear));
            items.Add(new MenuForm.Item("Dock 尺寸：" + DockPresetLabel(Config == null ? "standard" : Config.DockPreset) + "（点击切换）", delegate { CycleDockPreset(); }, Glyph.Gear));
            items.Add(new MenuForm.Item("灵动岛设置", delegate { OpenIslandSettingsLater(screen); }, Glyph.Gear));
            items.Add(new MenuForm.Item("新建面板", delegate { AddPanel(); }, Glyph.Plus));
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
            if (Config != null && Config.DockOn)
            {
                if (Dock == null) { Dock = new DockForm(); Dock.Show(); }
                else { Dock.Show(); Dock.RelayoutPublic(); Dock.RefreshAutoHide(); }
            }
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
        }

        public static void HideAll()
        {
            allHidden = true;
            if (Dock != null) Dock.Hide();
            if (Island != null) Island.Hide();
            for (int i = 0; i < Panels.Count; i++)
                if (Panels[i] != null) Panels[i].Hide();
        }

        public static void ToggleDockAutoHide()
        {
            if (Config == null) return;
            Config.DockAutoHide = !Config.DockAutoHide;
            Config.Save();
            if (Dock != null) Dock.RefreshAutoHide();
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
            if (Dock != null) Dock.RelayoutPublic();
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
            Panels.Remove(panel);
            Config.Panels.Remove(panel.Cfg);
            Config.Save();
            try { panel.Close(); } catch { }
            Toast("面板已删除");
        }

        public static void SavePanels()
        {
            if (Config == null) return;
            Config.Save();
        }

        public static void RefreshPanels()
        {
            for (int i = 0; i < Panels.Count; i++)
            {
                if (Panels[i] != null && Panels[i].Visible) Panels[i].Render();
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
                Toast(on ? "任务栏已设为自动隐藏" : "任务栏已恢复常显");
            }
            else Toast("任务栏设置失败，请检查系统权限");
        }

        public static void RestoreTaskbar()
        {
            bool target = Config != null ? Config.TaskbarOriginalAutoHide : true;
            bool ok = Taskbar.SetAutoHide(target);
            if (Config != null) { Config.TaskbarAutoHide = target; Config.Save(); }
            Toast(ok ? (target ? "任务栏已恢复为自动隐藏" : "任务栏已恢复为常显") : "任务栏设置失败");
        }

        public static void ReloadWallpaperTheme()
        {
            try
            {
                Backdrop.Rebuild();
                Config.ApplyTheme();
                if (Dock != null) Dock.RelayoutPublic();
                if (Island != null) Island.RefreshIsland();
                RefreshPanels();
                Toast("已按当前壁纸重新取色");
            }
            catch (Exception ex) { Log.Warn("wall", ex.Message); Toast("取色失败，已保留原配色"); }
        }

        public static void ResetDock()
        {
            Config.DockItems = ShellConfig.DefaultDock();
            Config.Save();
            if (Dock == null && Config.DockOn) { Dock = new DockForm(); Dock.Show(); }
            else if (Dock != null) Dock.RelayoutPublic();
            Toast("Dock 已恢复默认组合");
        }

        public static int PinPaths(string[] files)
        {
            if (files == null || Config == null) return 0;
            int added = 0;
            for (int i = 0; i < files.Length && Config.DockItems.Count < 16; i++)
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
                for (int k = 0; k < Config.DockItems.Count; k++)
                    if (Config.DockItems[k].SameAs(item)) { has = true; break; }
                if (has) continue;
                Config.DockItems.Add(item);
                added++;
            }
            if (added > 0) Config.Save();
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
            if (Config == null || index < 0 || index >= Config.DockItems.Count) return;
            Config.DockItems.RemoveAt(index);
            Config.Save();
            Toast("已从 Dock 移除");
            if (Dock != null) Dock.RelayoutPublic();
        }

        public static int MoveDockItem(int from, int before)
        {
            if (Config == null || from < 0 || from >= Config.DockItems.Count) return 0;
            if (before < 0) before = 0;
            if (before > Config.DockItems.Count) before = Config.DockItems.Count;
            int target = before;
            if (target > from) target--;
            if (target == from) return 0;
            DockItem item = Config.DockItems[from];
            Config.DockItems.RemoveAt(from);
            Config.DockItems.Insert(target, item);
            Config.Save();
            return 1;
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
                    Taskbar.ShowAll();
                    Config.TaskbarFullHide = false;
                    Config.Save();
                }
            }
            catch { }
            try { if (tray != null) { tray.Visible = false; tray.Dispose(); tray = null; } } catch { }
            try { if (statsTimer != null) statsTimer.Stop(); } catch { }
            try { if (wallTimer != null) wallTimer.Stop(); } catch { }
            try { if (Dock != null) Dock.Close(); } catch { }
            try { if (Island != null) Island.Close(); } catch { }
            for (int i = 0; i < Panels.Count; i++) { try { if (Panels[i] != null) Panels[i].Close(); } catch { } }
            try { if (Host != null) Host.Close(); } catch { }
            Application.Exit();
        }

        public static void DisplayChanged()
        {
            UI(delegate
            {
                if (Dock != null) Dock.ApplyPosition();
                if (Island != null) Island.ApplyPosition();
                for (int i = 0; i < Panels.Count; i++)
                    if (Panels[i] != null) Panels[i].ApplyBounds();
            });
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
                Native.RegisterHotKey(Handle, 11, 2 | 1, (uint)Keys.L);
                Native.RegisterHotKey(Handle, 12, 2 | 1, (uint)Keys.G);
                V2Shell.CreateTray();
            }
            catch (Exception ex) { Log.Warn("hotkey", ex.Message); }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312)
            {
                int id = m.WParam.ToInt32();
                if (id == 11) V2Shell.ToggleAll();
                else if (id == 12) V2Shell.ShowMenu(Cursor.Position);
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
