// Liuli v2 · 新外壳配置（独立于旧 boards.json，首次启动生成默认 shell.json）。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Liuli
{
    sealed class PanelConfig
    {
        public string Id = "";
        public string Content = "clock";
        public int X = -1;
        public int Y = -1;
        public int W = 340;
        public int H = 200;
        public bool TopMostPanel = true;

        public static string NormalizeContent(string s)
        {
            string v = (s == null ? "" : s.Trim().ToLowerInvariant());
            if (v == "empty" || v == "clock" || v == "weather" || v == "stats") return v;
            return "empty";
        }

        public static string ContentName(string content)
        {
            switch (NormalizeContent(content))
            {
                case "clock": return "时钟";
                case "weather": return "天气";
                case "stats": return "系统";
                default: return "空容器";
            }
        }
    }

    sealed class DockItem
    {
        public string Kind = "app";       // system / app
        public string Id = "";            // system: thispc / recycle / userfiles / network / controlpanel
        public string Path = "";          // app: exe / lnk / folder / ms-settings:
        public string Label = "";

        public bool IsSystem { get { return string.Equals(Kind, "system", StringComparison.OrdinalIgnoreCase); } }

        /// <summary>运行指示用进程名；系统入口、URL、.lnk 暂不解析。</summary>
        public string[] ProcessNames()
        {
            if (IsSystem || string.IsNullOrEmpty(Path)) return new string[0];
            if (Path.StartsWith("ms-settings:", StringComparison.OrdinalIgnoreCase))
                return new string[] { "SystemSettings" };
            if (Path.StartsWith("ms-calculator:", StringComparison.OrdinalIgnoreCase))
                return new string[] { "CalculatorApp", "Calculator", "WindowsCalculator" };
            if (Path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                Path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return new string[0];
            try
            {
                if (Path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    string file = System.IO.Path.GetFileNameWithoutExtension(Path);
                    if (string.Equals(file, "calc", StringComparison.OrdinalIgnoreCase))
                        return new string[] { "CalculatorApp", "Calculator", "WindowsCalculator" };
                    return new string[] { file };
                }
            }
            catch { }
            return new string[0];
        }

        public string DisplayName
        {
            get
            {
                if (Label != null && Label.Length > 0) return Label;
                if (IsSystem) return DockItem.SystemName(Id);
                try
                {
                    if (Path.StartsWith("ms-settings:", StringComparison.OrdinalIgnoreCase)) return "设置";
                    if (Path.StartsWith("ms-calculator:", StringComparison.OrdinalIgnoreCase)) return "计算器";
                    if (Directory.Exists(Path)) return new DirectoryInfo(Path).Name;
                    string name = System.IO.Path.GetFileNameWithoutExtension(Path);
                    return name.Length > 0 ? name : Path;
                }
                catch { return Path; }
            }
        }

        public static string SystemName(string id)
        {
            switch (id)
            {
                case "thispc": return "此电脑";
                case "recycle": return "回收站";
                case "userfiles": return "用户文件夹";
                case "network": return "网络";
                case "controlpanel": return "控制面板";
                default: return id;
            }
        }

        public void Launch()
        {
            try
            {
                if (IsSystem)
                {
                    string args = "";
                    switch (Id)
                    {
                        case "thispc": args = "shell:MyComputerFolder"; break;
                        case "recycle": args = "shell:RecycleBinFolder"; break;
                        case "userfiles":
                            args = "\"" + Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\"";
                            break;
                        case "network": args = "shell:NetworkPlacesFolder"; break;
                        case "controlpanel": args = "shell:ControlPanelFolder"; break;
                        default: return;
                    }
                    Process.Start("explorer.exe", args);
                    return;
                }
                if (Path == null || Path.Trim().Length == 0) return;
                if (Path.StartsWith("ms-settings:", StringComparison.OrdinalIgnoreCase) ||
                    Path.StartsWith("ms-calculator:", StringComparison.OrdinalIgnoreCase) ||
                    Path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    Path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start(Path);
                    return;
                }
                if (File.Exists(Path) || Directory.Exists(Path))
                {
                    Native.ShellOpen(Path);
                    return;
                }
                ToastForm.Show("找不到：" + Path);            }
            catch (Exception ex)
            {
                Log.Warn("dock", "打开失败 " + ex.Message);
                ToastForm.Show("打不开：" + DisplayName);
            }
        }

        public bool SameAs(DockItem other)
        {
            if (other == null) return false;
            if (IsSystem || other.IsSystem)
                return IsSystem && other.IsSystem && string.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase);
            return string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase);
        }
    }

    sealed class ShellConfig
    {
        public int Version = 2;

        // 外壳
        public bool DockOn = true;
        public bool DockAutoHide = false;
        public string DockPreset = "standard";   // small / standard / large
        public bool IslandOn = true;
        public bool IslandWeather = true;
        public bool IslandStats = true;
        public bool IslandQuick = true;
        public int IslandWidth = 380;       // 展开态 body 宽度
        public int IslandHeight = 112;      // 展开态 body 高度
        public int IslandTop = 6;           // 顶部偏移
        public int IslandAnimMs = 180;      // 展开/收起时长

        // 任务栏（M1 只做自动隐藏；完全隐藏留到 P1）
        public bool TaskbarAutoHide = true;
        public bool TaskbarOriginalAutoHide = true;
        public bool TaskbarFullHide = false;

        // 主题（M1 只消费壁纸色；预设/自定义留给 M5）
        public string ThemeMode = "auto";
        public string ThemePreset = "mist";
        public int GlassPct = 100;
        public int BlurPct = 62;
        public int Radius = 20;

        // 桌面图标（M5 才执行，先存字段）
        public bool DesktopIconsHidden = true;
        public bool HideShellThisPc = true;
        public bool HideShellRecycle = true;
        public bool HideShellUserFiles = true;
        public bool HideShellNetwork = true;
        public bool HideShellControlPanel = true;

        public List<DockItem> DockItems = new List<DockItem>();
        public List<PanelConfig> Panels = new List<PanelConfig>();

        public static string FilePath { get { return Path.Combine(Paths.Data, "shell.json"); } }

        public static ShellConfig Load()
        {
            ShellConfig c = Defaults();
            try
            {
                if (!File.Exists(FilePath)) { c.Save(); return c; }
                string text = File.ReadAllText(FilePath, Encoding.UTF8);
                object rootObj = Json.Parse(text);
                Dictionary<string, object> root = Json.Obj(rootObj);
                if (root == null) throw new Exception("shell.json 根节点不是对象");

                c.Version = Json.Int(root, "version", 2);
                c.DockOn = Json.Bool(root, "dockOn", true);
                c.DockAutoHide = Json.Bool(root, "dockAutoHide", false);
                string preset = Json.Str(root, "dockPreset", "standard");
                c.DockPreset = (string.Equals(preset, "small", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(preset, "large", StringComparison.OrdinalIgnoreCase)) ? preset.ToLowerInvariant() : "standard";
                c.IslandOn = Json.Bool(root, "islandOn", true);
                c.IslandWeather = Json.Bool(root, "islandWeather", true);
                c.IslandStats = Json.Bool(root, "islandStats", true);
                c.IslandQuick = Json.Bool(root, "islandQuick", true);
                c.IslandWidth = Util.Clamp(Json.Int(root, "islandWidth", 380), 360, 520);
                c.IslandHeight = Util.Clamp(Json.Int(root, "islandHeight", 112), 100, 180);
                c.IslandTop = Util.Clamp(Json.Int(root, "islandTop", 6), 0, 80);
                c.IslandAnimMs = Util.Clamp(Json.Int(root, "islandAnimMs", 180), 0, 400);
                c.TaskbarAutoHide = Json.Bool(root, "taskbarAutoHide", true);
                c.TaskbarOriginalAutoHide = Json.Bool(root, "taskbarOriginalAutoHide", true);
                c.TaskbarFullHide = Json.Bool(root, "taskbarFullHide", false);
                c.ThemeMode = Json.Str(root, "themeMode", "auto");
                c.ThemePreset = Json.Str(root, "themePreset", "mist");
                c.GlassPct = Json.Int(root, "glassPct", 100);
                c.BlurPct = Json.Int(root, "blurPct", 62);
                c.Radius = Json.Int(root, "radius", 20);
                c.DesktopIconsHidden = Json.Bool(root, "desktopIconsHidden", true);
                c.HideShellThisPc = Json.Bool(root, "hideShellThisPc", true);
                c.HideShellRecycle = Json.Bool(root, "hideShellRecycle", true);
                c.HideShellUserFiles = Json.Bool(root, "hideShellUserFiles", true);
                c.HideShellNetwork = Json.Bool(root, "hideShellNetwork", true);
                c.HideShellControlPanel = Json.Bool(root, "hideShellControlPanel", true);

                object itemsObj = root.ContainsKey("dockItems") ? root["dockItems"] : null;
                List<object> items = Json.Arr(itemsObj);
                if (items != null)
                {
                    c.DockItems = new List<DockItem>();
                    for (int i = 0; i < items.Count && c.DockItems.Count < 16; i++)
                    {
                        Dictionary<string, object> d = Json.Obj(items[i]);
                        if (d == null) continue;
                        string kind = Json.Str(d, "kind", "app");
                        string id = Json.Str(d, "id", "");
                        string path = Json.Str(d, "path", "");
                        string label = Json.Str(d, "label", "");
                        if (string.Equals(kind, "system", StringComparison.OrdinalIgnoreCase))
                        {
                            if (id.Length == 0) continue;
                            DockItem it = new DockItem();
                            it.Kind = "system"; it.Id = id; it.Label = label;
                            c.DockItems.Add(it);
                        }
                        else
                        {
                            if (path.Length == 0) continue;
                            DockItem it = new DockItem();
                            it.Kind = "app"; it.Path = path; it.Label = label;
                            c.DockItems.Add(it);
                        }
                    }
                }

                object panelsObj = root.ContainsKey("panels") ? root["panels"] : null;
                List<object> panels = Json.Arr(panelsObj);
                if (panels != null)
                {
                    c.Panels = new List<PanelConfig>();
                    for (int i = 0; i < panels.Count && c.Panels.Count < 6; i++)
                    {
                        Dictionary<string, object> d = Json.Obj(panels[i]);
                        if (d == null) continue;
                        PanelConfig p = new PanelConfig();
                        p.Id = Json.Str(d, "id", "");
                        p.Content = PanelConfig.NormalizeContent(Json.Str(d, "content", "empty"));
                        p.X = Json.Int(d, "x", -1);
                        p.Y = Json.Int(d, "y", -1);
                        p.W = Json.Int(d, "w", 340);
                        p.H = Json.Int(d, "h", 200);
                        p.TopMostPanel = Json.Bool(d, "topMost", true);
                        c.Panels.Add(p);
                    }
                }
                c.NormalizePanels();
                return c;
            }
            catch (Exception ex)
            {
                Log.Warn("config", "shell.json 读取失败，使用默认：" + ex.Message);
                try
                {
                    string bad = FilePath + ".bad-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                    if (File.Exists(FilePath)) File.Copy(FilePath, bad, true);
                }
                catch { }
                ShellConfig d = Defaults();
                try { d.Save(); } catch { }
                return d;
            }
        }

        public static ShellConfig Defaults()
        {
            ShellConfig c = new ShellConfig();
            c.DockItems = DefaultDock();
            return c;
        }

        public static List<DockItem> DefaultDock()
        {
            List<DockItem> list = new List<DockItem>();
            AddSystem(list, "thispc");
            AddSystem(list, "recycle");
            AddSystem(list, "userfiles");

            string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            AddApp(list, Path.Combine(windows, "explorer.exe"), "资源管理器");
            AddApp(list, "ms-settings:", "设置");
            AddApp(list, FindEdge(), "Edge");
            AddApp(list, Path.Combine(windows, "System32", "notepad.exe"), "记事本");
            AddApp(list, FindCalculator(windows), "计算器");
            return list;
        }

        static void AddSystem(List<DockItem> list, string id)
        {
            DockItem it = new DockItem();
            it.Kind = "system"; it.Id = id; it.Label = DockItem.SystemName(id);
            list.Add(it);
        }

        static void AddApp(List<DockItem> list, string path, string label)
        {
            if (path == null || path.Length == 0) return;
            if (!path.StartsWith("ms-", StringComparison.OrdinalIgnoreCase) && !File.Exists(path) && !Directory.Exists(path)) return;
            for (int i = 0; i < list.Count; i++)
                if (string.Equals(list[i].Path, path, StringComparison.OrdinalIgnoreCase)) return;
            DockItem it = new DockItem();
            it.Kind = "app"; it.Path = path; it.Label = label;
            list.Add(it);
        }

        static string FindEdge()
        {
            string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string[] paths = {
                Path.Combine(pf86, "Microsoft\\Edge\\Application\\msedge.exe"),
                Path.Combine(pf, "Microsoft\\Edge\\Application\\msedge.exe")
            };
            for (int i = 0; i < paths.Length; i++) if (File.Exists(paths[i])) return paths[i];
            return "";
        }

        static string FindCalculator(string windows)
        {
            string exe = Path.Combine(windows, "System32", "calc.exe");
            if (File.Exists(exe)) return exe;
            return "ms-calculator:";
        }

        public void Save()
        {
            try
            {
                NormalizeIsland();
                NormalizePanels();
                Dictionary<string, object> root = new Dictionary<string, object>();
                root["version"] = Version;
                root["dockOn"] = DockOn;
                root["dockAutoHide"] = DockAutoHide;
                root["dockPreset"] = DockPreset;
                root["islandOn"] = IslandOn;
                root["islandWeather"] = IslandWeather;
                root["islandStats"] = IslandStats;
                root["islandQuick"] = IslandQuick;
                root["islandWidth"] = IslandWidth;
                root["islandHeight"] = IslandHeight;
                root["islandTop"] = IslandTop;
                root["islandAnimMs"] = IslandAnimMs;
                root["taskbarAutoHide"] = TaskbarAutoHide;
                root["taskbarOriginalAutoHide"] = TaskbarOriginalAutoHide;
                root["taskbarFullHide"] = TaskbarFullHide;
                root["themeMode"] = ThemeMode;
                root["themePreset"] = ThemePreset;
                root["glassPct"] = GlassPct;
                root["blurPct"] = BlurPct;
                root["radius"] = Radius;
                root["desktopIconsHidden"] = DesktopIconsHidden;
                root["hideShellThisPc"] = HideShellThisPc;
                root["hideShellRecycle"] = HideShellRecycle;
                root["hideShellUserFiles"] = HideShellUserFiles;
                root["hideShellNetwork"] = HideShellNetwork;
                root["hideShellControlPanel"] = HideShellControlPanel;

                List<object> arr = new List<object>();
                for (int i = 0; i < DockItems.Count; i++)
                {
                    DockItem it = DockItems[i];
                    if (it == null) continue;
                    Dictionary<string, object> d = new Dictionary<string, object>();
                    d["kind"] = it.Kind;
                    if (it.IsSystem) d["id"] = it.Id; else d["path"] = it.Path;
                    d["label"] = it.Label == null ? "" : it.Label;
                    arr.Add(d);
                }
                root["dockItems"] = arr;

                List<object> panelArr = new List<object>();
                for (int i = 0; i < Panels.Count; i++)
                {
                    PanelConfig p = Panels[i];
                    if (p == null) continue;
                    Dictionary<string, object> d = new Dictionary<string, object>();
                    d["id"] = p.Id;
                    d["content"] = PanelConfig.NormalizeContent(p.Content);
                    d["x"] = p.X;
                    d["y"] = p.Y;
                    d["w"] = p.W;
                    d["h"] = p.H;
                    d["topMost"] = p.TopMostPanel;
                    panelArr.Add(d);
                }
                root["panels"] = panelArr;
                Util.WriteAllTextAtomic(FilePath, Json.Write(root, true));
            }
            catch (Exception ex) { Log.Warn("config", "shell.json 保存失败：" + ex.Message); }
        }

        public void NormalizeIsland()
        {
            IslandWidth = Util.Clamp(IslandWidth, 360, 520);
            IslandHeight = Util.Clamp(IslandHeight, 100, 180);
            IslandTop = Util.Clamp(IslandTop, 0, 80);
            IslandAnimMs = Util.Clamp(IslandAnimMs, 0, 400);
        }

        public void NormalizePanels()
        {
            if (Panels == null) Panels = new List<PanelConfig>();
            if (Panels.Count > 6) Panels.RemoveRange(6, Panels.Count - 6);
            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < Panels.Count; i++)
            {
                PanelConfig p = Panels[i];
                if (p == null) { Panels.RemoveAt(i); i--; continue; }
                p.Id = (p.Id == null ? "" : p.Id.Trim());
                if (p.Id.Length == 0 || ids.Contains(p.Id))
                    p.Id = "panel-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                ids.Add(p.Id);
                p.Content = PanelConfig.NormalizeContent(p.Content);
                p.W = Util.Clamp(p.W, 220, 720);
                p.H = Util.Clamp(p.H, 120, 520);
                if (p.W > work.Width - 40) p.W = Math.Max(220, work.Width - 40);
                if (p.H > work.Height - 40) p.H = Math.Max(120, work.Height - 40);
                if (p.X == -1 || p.Y == -1 || p.X < work.Left || p.Y < work.Top ||
                    p.X > work.Right - 80 || p.Y > work.Bottom - 60)
                {
                    p.X = work.Right - p.W - 40 - i * 24;
                    p.Y = work.Top + 160 + i * 28;
                }
                if (p.X > work.Right - 80) p.X = work.Right - 80;
                if (p.Y > work.Bottom - 60) p.Y = work.Bottom - 60;
            }
        }

        public PanelConfig NewPanel(int index)
        {
            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            PanelConfig p = new PanelConfig();
            p.Id = "panel-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            p.Content = "clock";
            p.W = 340;
            p.H = 200;
            p.X = work.Right - p.W - 40 - index * 24;
            p.Y = work.Top + 160 + index * 28;
            p.TopMostPanel = true;
            NormalizePanelsAfterAdd(p, work);
            return p;
        }

        static void NormalizePanelsAfterAdd(PanelConfig p, Rectangle work)
        {
            p.W = Util.Clamp(p.W, 220, 720);
            p.H = Util.Clamp(p.H, 120, 520);
            if (p.X < work.Left) p.X = work.Left;
            if (p.Y < work.Top) p.Y = work.Top;
            if (p.X > work.Right - 80) p.X = work.Right - 80;
            if (p.Y > work.Bottom - 60) p.Y = work.Bottom - 60;
        }

        public void ApplyTheme()
        {
            Theme.GlassPct = Util.Clamp(GlassPct, 20, 140);
            Theme.BlurPct = Util.Clamp(BlurPct, 10, 100);
            Theme.Radius = Radius;
            Theme.Light = Backdrop.Light;
            Theme.Accent = Backdrop.Accent;
            if (!Backdrop.Mean.IsEmpty) Theme.WallMean = Backdrop.Mean;
            Theme.ApplyTone();
        }
    }
}
