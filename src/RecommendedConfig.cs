// Liuli v2 · 推荐配置导入：先全量校验，再备份 shell.json，最后一次性应用。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace Liuli
{
    sealed class ImportResult
    {
        public bool Ok;
        public string Name = "";
        public string Backup = "";
        public string Error = "";
        public int Fields;
    }

    static class RecommendedConfig
    {
        const int MaxItems = 16;
        const int MaxPanels = 6;

        static readonly string[] RootKeys = new string[] { "version", "name", "description", "theme", "dock", "island", "panels" };
        static readonly string[] ThemeKeys = new string[] { "mode", "accent", "accentMode", "accentPreset", "glassPct", "blurPct", "radius" };
        static readonly string[] DockKeys = new string[] { "preset", "autoHide", "align", "bottomGap", "locked", "items" };
        static readonly string[] IslandKeys = new string[] { "on", "width", "height", "top", "animMs", "weather", "stats", "quick", "media", "notify" };
        static readonly string[] PanelKeys = new string[] { "id", "content", "x", "y", "w", "h", "topMost" };
        static readonly string[] ItemKeys = new string[] { "kind", "id", "path", "label" };

        public static ImportResult Import(string path)
        {
            ImportResult r = new ImportResult();
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    r.Error = "文件不存在：" + (path == null ? "" : path);
                    return r;
                }
                FileInfo fi = new FileInfo(path);
                if (fi.Length > 1024 * 1024)
                {
                    r.Error = "文件超过 1 MB，拒绝导入";
                    return r;
                }
                string text = File.ReadAllText(path, Encoding.UTF8);
                object rootObj = Json.Parse(text);
                Dictionary<string, object> root = Json.Obj(rootObj);
                if (root == null)
                {
                    r.Error = "根节点必须是 JSON 对象";
                    return r;
                }

                List<string> errors = new List<string>();
                Dictionary<string, object> spec = new Dictionary<string, object>();
                CheckKeys(root, "", RootKeys, errors);

                if (root.ContainsKey("name"))
                {
                    string name;
                    if (ReadString(root, "name", 1, 24, errors, "name", out name)) spec["name"] = name;
                }
                if (root.ContainsKey("version"))
                {
                    int ver;
                    if (ReadInt(root, "version", 1, 3, errors, "version", out ver)) spec["version"] = ver;
                }
                if (root.ContainsKey("description"))
                {
                    string desc;
                    if (ReadString(root, "description", 0, 120, errors, "description", out desc)) spec["description"] = desc;
                }

                if (root.ContainsKey("theme"))
                {
                    Dictionary<string, object> d = Json.Obj(root["theme"]);
                    if (d == null) errors.Add("字段 theme 必须是对象");
                    else ReadTheme(d, spec, errors);
                }
                if (root.ContainsKey("dock"))
                {
                    Dictionary<string, object> d = Json.Obj(root["dock"]);
                    if (d == null) errors.Add("字段 dock 必须是对象");
                    else ReadDock(d, spec, errors);
                }
                if (root.ContainsKey("island"))
                {
                    Dictionary<string, object> d = Json.Obj(root["island"]);
                    if (d == null) errors.Add("字段 island 必须是对象");
                    else ReadIsland(d, spec, errors);
                }
                if (root.ContainsKey("panels"))
                {
                    List<object> arr = Json.Arr(root["panels"]);
                    if (arr == null) errors.Add("字段 panels 必须是数组");
                    else ReadPanels(arr, spec, errors);
                }

                if (errors.Count > 0)
                {
                    r.Error = string.Join("\n", errors.ToArray());
                    return r;
                }
                if (spec.Count == 0)
                {
                    r.Error = "没有可导入的字段";
                    return r;
                }

                string backup = Backup();
                if (backup == null)
                {
                    r.Error = "备份 shell.json 失败，已取消导入";
                    return r;
                }
                r.Backup = backup;

                ShellConfig c = V2Shell.Config;
                if (c == null)
                {
                    r.Error = "配置尚未加载，无法导入";
                    return r;
                }
                Apply(c, spec);
                c.Save();
                V2Shell.ApplyRecommendedLayout();

                r.Ok = true;
                r.Name = spec.ContainsKey("name") ? (string)spec["name"] : "推荐配置";
                r.Fields = spec.Count;
                return r;
            }
            catch (Exception ex)
            {
                r.Ok = false;
                r.Error = "导入异常：" + ex.Message;
                return r;
            }
        }

        static void ReadTheme(Dictionary<string, object> d, Dictionary<string, object> spec, List<string> errors)
        {
            CheckKeys(d, "theme.", ThemeKeys, errors);
            if (d.ContainsKey("mode"))
            {
                string mode;
                if (ReadEnum(d, "mode", new string[] { "auto", "dark", "light" }, errors, "theme.mode", out mode))
                    spec["themeMode"] = mode;
            }
            if (d.ContainsKey("accent"))
            {
                string accent;
                if (ReadHex(d, "accent", errors, "theme.accent", out accent))
                {
                    spec["accentMode"] = "custom";
                    spec["accentCustom"] = accent;
                }
            }
            if (d.ContainsKey("accentMode"))
            {
                string mode;
                if (ReadEnum(d, "accentMode", new string[] { "auto", "preset", "custom" }, errors, "theme.accentMode", out mode))
                    spec["accentMode"] = mode;
            }
            if (d.ContainsKey("accentPreset"))
            {
                int preset;
                if (ReadInt(d, "accentPreset", 0, 5, errors, "theme.accentPreset", out preset))
                    spec["accentPreset"] = preset;
            }
            if (d.ContainsKey("glassPct"))
            {
                int v;
                if (ReadInt(d, "glassPct", 20, 140, errors, "theme.glassPct", out v)) spec["glassPct"] = v;
            }
            if (d.ContainsKey("blurPct"))
            {
                int v;
                if (ReadInt(d, "blurPct", 10, 100, errors, "theme.blurPct", out v)) spec["blurPct"] = v;
            }
            if (d.ContainsKey("radius"))
            {
                int v;
                if (ReadInt(d, "radius", 10, 30, errors, "theme.radius", out v)) spec["radius"] = v;
            }
        }

        static void ReadDock(Dictionary<string, object> d, Dictionary<string, object> spec, List<string> errors)
        {
            CheckKeys(d, "dock.", DockKeys, errors);
            if (d.ContainsKey("preset"))
            {
                string preset;
                if (ReadEnum(d, "preset", new string[] { "small", "standard", "large" }, errors, "dock.preset", out preset))
                    spec["dockPreset"] = preset;
            }
            if (d.ContainsKey("autoHide"))
            {
                bool v;
                if (ReadBool(d, "autoHide", errors, "dock.autoHide", out v)) spec["dockAutoHide"] = v;
            }
            if (d.ContainsKey("align"))
            {
                string align;
                if (ReadEnum(d, "align", new string[] { "left", "center", "right" }, errors, "dock.align", out align))
                    spec["dockAlign"] = align;
            }
            if (d.ContainsKey("bottomGap"))
            {
                int v;
                if (ReadInt(d, "bottomGap", 0, 160, errors, "dock.bottomGap", out v)) spec["dockBottomGap"] = v;
            }
            if (d.ContainsKey("locked"))
            {
                bool v;
                if (ReadBool(d, "locked", errors, "dock.locked", out v)) spec["dockLocked"] = v;
            }
            if (d.ContainsKey("items"))
            {
                List<object> arr = Json.Arr(d["items"]);
                if (arr == null) errors.Add("字段 dock.items 必须是数组");
                else
                {
                    List<DockItem> items = ReadItems(arr, errors);
                    if (items != null) spec["dockItems"] = items;
                }
            }
        }

        static List<DockItem> ReadItems(List<object> arr, List<string> errors)
        {
            if (arr.Count > MaxItems)
            {
                errors.Add("字段 dock.items 最多 " + MaxItems + " 项");
                return null;
            }
            List<DockItem> list = new List<DockItem>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < arr.Count; i++)
            {
                string at = "dock.items[" + i + "]";
                Dictionary<string, object> d = Json.Obj(arr[i]);
                if (d == null) { errors.Add(at + " 必须是对象"); continue; }
                CheckKeys(d, at + ".", ItemKeys, errors);

                string kind = "app";
                if (d.ContainsKey("kind"))
                {
                    string k;
                    if (ReadEnum(d, "kind", new string[] { "system", "app" }, errors, at + ".kind", out k)) kind = k;
                    else continue;
                }
                string id = "";
                string path = "";
                string label = "";
                if (d.ContainsKey("id") && !ReadString(d, "id", 1, 64, errors, at + ".id", out id)) continue;
                if (d.ContainsKey("path") && !ReadString(d, "path", 1, 260, errors, at + ".path", out path)) continue;
                if (d.ContainsKey("label") && !ReadString(d, "label", 0, 32, errors, at + ".label", out label)) continue;

                DockItem it = new DockItem();
                if (string.Equals(kind, "system", StringComparison.OrdinalIgnoreCase))
                {
                    if (id.Length == 0) { errors.Add(at + ".id 系统入口不能为空"); continue; }
                    if (!IsSystemId(id)) { errors.Add(at + ".id 不在系统入口白名单：" + id); continue; }
                    it.Kind = "system";
                    it.Id = id;
                }
                else
                {
                    if (path.Length == 0) { errors.Add(at + ".path 应用入口不能为空"); continue; }
                    if (path.IndexOfAny(new char[] { '\r', '\n', '\0' }) >= 0) { errors.Add(at + ".path 含非法字符"); continue; }
                    it.Kind = "app";
                    it.Path = path;
                }
                it.Label = label;
                string key = it.IsSystem ? "system:" + it.Id.ToLowerInvariant() : "app:" + it.Path.ToLowerInvariant();
                if (!seen.Add(key)) { errors.Add(at + " 与前面的 Dock 项重复"); continue; }
                list.Add(it);
            }
            return list;
        }

        static void ReadIsland(Dictionary<string, object> d, Dictionary<string, object> spec, List<string> errors)
        {
            CheckKeys(d, "island.", IslandKeys, errors);
            if (d.ContainsKey("on"))
            {
                bool v;
                if (ReadBool(d, "on", errors, "island.on", out v)) spec["islandOn"] = v;
            }
            if (d.ContainsKey("width"))
            {
                int v;
                if (ReadInt(d, "width", 360, 520, errors, "island.width", out v)) spec["islandWidth"] = v;
            }
            if (d.ContainsKey("height"))
            {
                int v;
                if (ReadInt(d, "height", 100, 180, errors, "island.height", out v)) spec["islandHeight"] = v;
            }
            if (d.ContainsKey("top"))
            {
                int v;
                if (ReadInt(d, "top", 0, 80, errors, "island.top", out v)) spec["islandTop"] = v;
            }
            if (d.ContainsKey("animMs"))
            {
                int v;
                if (ReadInt(d, "animMs", 0, 400, errors, "island.animMs", out v)) spec["islandAnimMs"] = v;
            }
            ReadIslandBool(d, "weather", "islandWeather", spec, errors);
            ReadIslandBool(d, "stats", "islandStats", spec, errors);
            ReadIslandBool(d, "quick", "islandQuick", spec, errors);
            ReadIslandBool(d, "media", "islandMedia", spec, errors);
            ReadIslandBool(d, "notify", "islandNotify", spec, errors);
        }

        static void ReadIslandBool(Dictionary<string, object> d, string key, string target,
            Dictionary<string, object> spec, List<string> errors)
        {
            if (!d.ContainsKey(key)) return;
            bool v;
            if (ReadBool(d, key, errors, "island." + key, out v)) spec[target] = v;
        }

        static void ReadPanels(List<object> arr, Dictionary<string, object> spec, List<string> errors)
        {
            if (arr.Count > MaxPanels)
            {
                errors.Add("字段 panels 最多 " + MaxPanels + " 个");
                return;
            }
            List<PanelConfig> panels = new List<PanelConfig>();
            HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < arr.Count; i++)
            {
                string at = "panels[" + i + "]";
                Dictionary<string, object> d = Json.Obj(arr[i]);
                if (d == null) { errors.Add(at + " 必须是对象"); continue; }
                CheckKeys(d, at + ".", PanelKeys, errors);

                PanelConfig p = new PanelConfig();
                p.Id = "rec-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                p.Content = "clock";
                p.X = 80 + (i % 3) * 380;
                p.Y = 180 + (i / 3) * 240;
                p.W = 340;
                p.H = 200;
                p.TopMostPanel = true;

                if (d.ContainsKey("id"))
                {
                    string id;
                    if (!ReadString(d, "id", 1, 32, errors, at + ".id", out id)) continue;
                    if (!ids.Add(id)) { errors.Add(at + ".id 与其他面板重复：" + id); continue; }
                    p.Id = id;
                }
                if (d.ContainsKey("content"))
                {
                    string content;
                    if (!ReadEnum(d, "content", new string[] { "empty", "clock", "weather", "stats" }, errors, at + ".content", out content)) continue;
                    p.Content = content;
                }
                if (d.ContainsKey("x"))
                {
                    int v;
                    if (ReadInt(d, "x", -10000, 10000, errors, at + ".x", out v)) p.X = v; else continue;
                }
                if (d.ContainsKey("y"))
                {
                    int v;
                    if (ReadInt(d, "y", -10000, 10000, errors, at + ".y", out v)) p.Y = v; else continue;
                }
                if (d.ContainsKey("w"))
                {
                    int v;
                    if (ReadInt(d, "w", 220, 720, errors, at + ".w", out v)) p.W = v; else continue;
                }
                if (d.ContainsKey("h"))
                {
                    int v;
                    if (ReadInt(d, "h", 120, 520, errors, at + ".h", out v)) p.H = v; else continue;
                }
                if (d.ContainsKey("topMost"))
                {
                    bool v;
                    if (ReadBool(d, "topMost", errors, at + ".topMost", out v)) p.TopMostPanel = v; else continue;
                }
                panels.Add(p);
            }
            if (panels.Count == arr.Count) spec["panels"] = panels;
        }

        static void Apply(ShellConfig c, Dictionary<string, object> spec)
        {
            foreach (KeyValuePair<string, object> kv in spec)
            {
                switch (kv.Key)
                {
                    case "themeMode": c.ThemeMode = (string)kv.Value; break;
                    case "accentMode": c.AccentMode = (string)kv.Value; break;
                    case "accentCustom": c.AccentCustom = (string)kv.Value; break;
                    case "accentPreset": c.AccentPreset = (int)kv.Value; break;
                    case "glassPct": c.GlassPct = (int)kv.Value; break;
                    case "blurPct": c.BlurPct = (int)kv.Value; break;
                    case "radius": c.Radius = (int)kv.Value; break;
                    case "dockPreset": c.DockPreset = (string)kv.Value; break;
                    case "dockAutoHide": c.DockAutoHide = (bool)kv.Value; break;
                    case "dockAlign": c.DockAlign = (string)kv.Value; break;
                    case "dockBottomGap": c.DockBottomGap = (int)kv.Value; break;
                    case "dockLocked": c.DockLocked = (bool)kv.Value; break;
                    case "dockItems": c.DockItems = (List<DockItem>)kv.Value; break;
                    case "islandOn": c.IslandOn = (bool)kv.Value; break;
                    case "islandWidth": c.IslandWidth = (int)kv.Value; break;
                    case "islandHeight": c.IslandHeight = (int)kv.Value; break;
                    case "islandTop": c.IslandTop = (int)kv.Value; break;
                    case "islandAnimMs": c.IslandAnimMs = (int)kv.Value; break;
                    case "islandWeather": c.IslandWeather = (bool)kv.Value; break;
                    case "islandStats": c.IslandStats = (bool)kv.Value; break;
                    case "islandQuick": c.IslandQuick = (bool)kv.Value; break;
                    case "islandMedia": c.IslandMedia = (bool)kv.Value; break;
                    case "islandNotify": c.IslandNotify = (bool)kv.Value; break;
                    case "panels": c.Panels = (List<PanelConfig>)kv.Value; break;
                }
            }
            c.NormalizeIsland();
            c.NormalizePanels();
        }

        static string Backup()
        {
            try
            {
                string file = ShellConfig.FilePath;
                if (!File.Exists(file)) { Log.Warn("import", "没有 shell.json 可备份"); return ""; }
                string dir = Path.Combine(Paths.Data, "backups");
                Directory.CreateDirectory(dir);
                string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
                string target = Path.Combine(dir, "shell-" + stamp + ".json");
                int n = 0;
                while (File.Exists(target)) { n++; target = Path.Combine(dir, "shell-" + stamp + "-" + n + ".json"); }
                File.Copy(file, target, false);
                return target;
            }
            catch (Exception ex)
            {
                Log.Warn("import", "备份失败 " + ex.Message);
                return null;
            }
        }

        static void CheckKeys(Dictionary<string, object> d, string prefix, string[] allowed, List<string> errors)
        {
            foreach (string key in d.Keys)
            {
                bool ok = false;
                for (int i = 0; i < allowed.Length; i++) if (string.Equals(allowed[i], key, StringComparison.Ordinal)) { ok = true; break; }
                if (!ok) errors.Add("不支持的字段 " + prefix + key);
            }
        }

        static bool ReadString(Dictionary<string, object> d, string key, int min, int max,
            List<string> errors, string path, out string val)
        {
            val = "";
            object o = d[key];
            string s = o as string;
            if (s == null) { errors.Add("字段 " + path + " 必须是字符串"); return false; }
            s = s.Trim();
            if (s.Length < min) { errors.Add("字段 " + path + " 不能为空"); return false; }
            if (s.Length > max) { errors.Add("字段 " + path + " 最长 " + max + " 个字符"); return false; }
            val = s;
            return true;
        }

        static bool ReadEnum(Dictionary<string, object> d, string key, string[] allowed,
            List<string> errors, string path, out string val)
        {
            val = "";
            string s;
            if (!ReadString(d, key, 1, 32, errors, path, out s)) return false;
            s = s.ToLowerInvariant();
            for (int i = 0; i < allowed.Length; i++)
            {
                if (string.Equals(allowed[i], s, StringComparison.Ordinal)) { val = s; return true; }
            }
            errors.Add("字段 " + path + " 只能是 " + string.Join(" / ", allowed));
            return false;
        }

        static bool ReadHex(Dictionary<string, object> d, string key, List<string> errors, string path, out string val)
        {
            val = "";
            string s;
            if (!ReadString(d, key, 1, 7, errors, path, out s)) return false;
            if (s.Length != 7 || s[0] != '#')
            {
                errors.Add("字段 " + path + " 必须是 #RRGGBB");
                return false;
            }
            for (int i = 1; i < 7; i++)
            {
                char ch = s[i];
                bool hex = (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');
                if (!hex) { errors.Add("字段 " + path + " 必须是 #RRGGBB"); return false; }
            }
            val = s.ToUpperInvariant();
            return true;
        }

        static bool ReadInt(Dictionary<string, object> d, string key, int min, int max,
            List<string> errors, string path, out int val)
        {
            val = 0;
            object o = d[key];
            if (!(o is double))
            {
                errors.Add("字段 " + path + " 必须是整数");
                return false;
            }
            double n = (double)o;
            if (n != Math.Floor(n))
            {
                errors.Add("字段 " + path + " 必须是整数");
                return false;
            }
            if (n < min || n > max)
            {
                errors.Add("字段 " + path + " 范围应为 " + min + "–" + max);
                return false;
            }
            val = (int)n;
            return true;
        }

        static bool ReadBool(Dictionary<string, object> d, string key, List<string> errors, string path, out bool val)
        {
            val = false;
            object o = d[key];
            if (!(o is bool))
            {
                errors.Add("字段 " + path + " 必须是 true 或 false");
                return false;
            }
            val = (bool)o;
            return true;
        }

        static bool IsSystemId(string id)
        {
            string[] ids = new string[] { "thispc", "recycle", "userfiles", "network", "controlpanel" };
            for (int i = 0; i < ids.Length; i++) if (string.Equals(ids[i], id, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
