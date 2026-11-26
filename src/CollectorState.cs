// Liuli v2 · 桌面收纳数据层：只保存原路径，不移动/复制/删除任何桌面文件。
using System;
using System.Collections.Generic;
using System.IO;

namespace Liuli
{
    sealed class CollectorItem
    {
        public string Path = "";
        public string Name = "";
        public string Group = "应用";
        public bool Hidden;
        public bool Manual;      // 手动拖入的桌面外入口；普通桌面条目为 false
        public bool IsFolder;

        public CollectorItem Copy()
        {
            CollectorItem c = new CollectorItem();
            c.Path = Path;
            c.Name = Name;
            c.Group = Group;
            c.Hidden = Hidden;
            c.Manual = Manual;
            c.IsFolder = IsFolder;
            return c;
        }

        public string DisplayName
        {
            get
            {
                if (Name != null && Name.Length > 0) return Name;
                try
                {
                    if (Directory.Exists(Path)) return new DirectoryInfo(Path).Name;
                    string name = System.IO.Path.GetFileNameWithoutExtension(Path);
                    return name.Length > 0 ? name : System.IO.Path.GetFileName(Path);
                }
                catch { return Path; }
            }
        }
    }

    /// <summary>
    /// 桌面入口索引。同步只比较路径与分组，不产生任何文件操作。
    /// 参考 GlassBoard 的 Entry 模型，但按本项目 C#5 / ShellConfig 重写。
    /// </summary>
    static class CollectorState
    {
        public const int MaxItems = 500;
        public const string AllGroup = "全部";

        public static string[] GroupNames()
        {
            string[] names = new string[Icons.GroupOrder.Length + 1];
            names[0] = AllGroup;
            for (int i = 0; i < Icons.GroupOrder.Length; i++) names[i + 1] = Icons.GroupOrder[i];
            return names;
        }

        public static bool IsGroup(string group)
        {
            if (string.IsNullOrEmpty(group)) return false;
            for (int i = 0; i < Icons.GroupOrder.Length; i++)
                if (string.Equals(Icons.GroupOrder[i], group, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static string NormalizeGroup(string group, string path)
        {
            if (IsGroup(group)) return group;
            return Icons.GroupOf(path);
        }

        public static List<CollectorItem> Items()
        {
            return V2Shell.Config == null ? new List<CollectorItem>() : V2Shell.Config.CollectorItems;
        }

        public static CollectorItem Find(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            List<CollectorItem> list = Items();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && string.Equals(list[i].Path, path, StringComparison.OrdinalIgnoreCase))
                    return list[i];
            }
            return null;
        }

        public static int VisibleCount()
        {
            int n = 0;
            List<CollectorItem> list = Items();
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null && !list[i].Hidden) n++;
            return n;
        }

        public static int GroupCount(string group)
        {
            int n = 0;
            List<CollectorItem> list = Items();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null || list[i].Hidden) continue;
                if (string.IsNullOrEmpty(group) || group == AllGroup ||
                    string.Equals(list[i].Group, group, StringComparison.OrdinalIgnoreCase)) n++;
            }
            return n;
        }

        /// <summary>
        /// 扫描用户桌面 + 公共桌面，合并到 CollectorItems。
        /// 只更新入口与分组；文件消失就移除入口，绝不删除磁盘文件。
        /// </summary>
        public static bool Sync(bool save)
        {
            if (V2Shell.Config == null) return false;
            List<DeskItem> scan;
            try { scan = Icons.ScanDesktop(); }
            catch (Exception ex)
            {
                Log.Warn("collector", "扫描桌面失败 " + ex.Message);
                return false;
            }

            List<CollectorItem> old = V2Shell.Config.CollectorItems;
            if (old == null) old = new List<CollectorItem>();
            List<CollectorItem> next = new List<CollectorItem>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < scan.Count; i++)
            {
                DeskItem d = scan[i];
                if (d == null || string.IsNullOrEmpty(d.Path)) continue;
                if (!File.Exists(d.Path) && !Directory.Exists(d.Path)) continue;
                if (seen.Contains(d.Path)) continue;
                seen.Add(d.Path);

                CollectorItem item = FindIn(old, d.Path);
                if (item == null)
                {
                    item = new CollectorItem();
                    item.Path = d.Path;
                    item.Group = Icons.GroupOf(d.Path);
                }
                else
                {
                    item.Path = d.Path;
                    item.Group = NormalizeGroup(item.Group, d.Path);
                }
                item.Name = d.Name;
                item.IsFolder = Directory.Exists(d.Path);
                next.Add(item);
            }

            // 手动拖入的桌面外入口：只要原件还在就保留。
            for (int i = 0; i < old.Count; i++)
            {
                CollectorItem item = old[i];
                if (item == null || string.IsNullOrEmpty(item.Path)) continue;
                if (seen.Contains(item.Path)) continue;
                if (item.Manual && (File.Exists(item.Path) || Directory.Exists(item.Path)))
                {
                    item.Group = NormalizeGroup(item.Group, item.Path);
                    if (item.Name == null || item.Name.Length == 0)
                        item.Name = DisplayName(item.Path);
                    item.IsFolder = Directory.Exists(item.Path);
                    next.Add(item);
                    seen.Add(item.Path);
                }
            }

            bool changed = !SameList(old, next);
            if (next.Count > MaxItems) next.RemoveRange(MaxItems, next.Count - MaxItems);
            V2Shell.Config.CollectorItems = next;
            if (changed && save) V2Shell.Config.Save();
            if (changed)
                Log.Info("collector", "桌面入口同步 " + next.Count + " 项" + (save ? "，已写入 shell.json" : ""));
            return changed;
        }

        public static List<CollectorItem> ReadItems(List<object> arr)
        {
            List<CollectorItem> list = new List<CollectorItem>();
            if (arr == null) return list;
            for (int i = 0; i < arr.Count && list.Count < MaxItems; i++)
            {
                Dictionary<string, object> d = Json.Obj(arr[i]);
                if (d == null) continue;
                string path = Json.Str(d, "path", "");
                if (path.Length == 0) continue;
                CollectorItem item = new CollectorItem();
                item.Path = path;
                item.Name = Json.Str(d, "name", "");
                item.Group = Json.Str(d, "group", "");
                item.Hidden = Json.Bool(d, "hidden", false);
                item.Manual = Json.Bool(d, "manual", false);
                item.IsFolder = Json.Bool(d, "folder", false);
                item.Group = NormalizeGroup(item.Group, item.Path);
                list.Add(item);
            }
            return list;
        }

        public static List<object> WriteItems(List<CollectorItem> items)
        {
            List<object> arr = new List<object>();
            if (items == null) return arr;
            for (int i = 0; i < items.Count; i++)
            {
                CollectorItem item = items[i];
                if (item == null || string.IsNullOrEmpty(item.Path)) continue;
                Dictionary<string, object> d = new Dictionary<string, object>();
                d["path"] = item.Path;
                d["name"] = item.Name == null ? "" : item.Name;
                d["group"] = NormalizeGroup(item.Group, item.Path);
                d["hidden"] = item.Hidden;
                d["manual"] = item.Manual;
                d["folder"] = item.IsFolder;
                arr.Add(d);
            }
            return arr;
        }

        public static void Normalize(ShellConfig c)
        {
            if (c == null) return;
            if (c.CollectorItems == null) c.CollectorItems = new List<CollectorItem>();
            if (c.CollectorItems.Count > MaxItems)
                c.CollectorItems.RemoveRange(MaxItems, c.CollectorItems.Count - MaxItems);
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < c.CollectorItems.Count; i++)
            {
                CollectorItem item = c.CollectorItems[i];
                if (item == null || string.IsNullOrEmpty(item.Path))
                {
                    c.CollectorItems.RemoveAt(i);
                    i--;
                    continue;
                }
                item.Path = item.Path.Trim();
                if (seen.Contains(item.Path))
                {
                    c.CollectorItems.RemoveAt(i);
                    i--;
                    continue;
                }
                seen.Add(item.Path);
                item.Group = NormalizeGroup(item.Group, item.Path);
                if (item.Name == null) item.Name = "";
                item.IsFolder = Directory.Exists(item.Path);
            }
            if (c.CollectorW < 420) c.CollectorW = 420;
            if (c.CollectorW > 900) c.CollectorW = 900;
            if (c.CollectorH < 300) c.CollectorH = 300;
            if (c.CollectorH > 720) c.CollectorH = 720;
            if (c.CollectorGroup == null || (c.CollectorGroup != AllGroup && !IsGroup(c.CollectorGroup)))
                c.CollectorGroup = AllGroup;
        }

        public static int AddPaths(string[] paths)
        {
            if (V2Shell.Config == null || paths == null) return 0;
            int added = 0;
            List<CollectorItem> list = V2Shell.Config.CollectorItems;
            for (int i = 0; i < paths.Length && list.Count < MaxItems; i++)
            {
                string path = paths[i] == null ? "" : paths[i].Trim();
                if (path.Length == 0) continue;
                if (!File.Exists(path) && !Directory.Exists(path)) continue;
                CollectorItem item = Find(path);
                if (item != null)
                {
                    if (item.Hidden) { item.Hidden = false; added++; }
                    item.Manual = true;
                    item.Group = NormalizeGroup(item.Group, path);
                    continue;
                }
                item = new CollectorItem();
                item.Path = path;
                item.Name = DisplayName(path);
                item.Group = Icons.GroupOf(path);
                item.IsFolder = Directory.Exists(path);
                item.Manual = true;
                list.Add(item);
                added++;
            }
            if (added > 0) V2Shell.Config.Save();
            Log.Info("collector", "拖入 " + added + " 项（只加入口）");
            return added;
        }

        public static bool MoveToGroup(string path, string group)
        {
            if (V2Shell.Config == null || string.IsNullOrEmpty(path) || !IsGroup(group)) return false;
            CollectorItem item = Find(path);
            if (item == null || string.Equals(item.Group, group, StringComparison.OrdinalIgnoreCase)) return false;
            item.Group = group;
            V2Shell.Config.Save();
            Log.Info("collector", "换组 " + item.DisplayName + " -> " + group + "（未移动文件）");
            return true;
        }

        public static bool Hide(string path)
        {
            if (V2Shell.Config == null) return false;
            CollectorItem item = Find(path);
            if (item == null) return false;
            item.Hidden = true;
            V2Shell.Config.Save();
            Log.Info("collector", "从面板移出 " + item.DisplayName + "（未删除文件）");
            return true;
        }

        public static int RestoreHidden()
        {
            if (V2Shell.Config == null) return 0;
            int n = 0;
            List<CollectorItem> list = V2Shell.Config.CollectorItems;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].Hidden) { list[i].Hidden = false; n++; }
            }
            if (n > 0) V2Shell.Config.Save();
            return n;
        }

        static CollectorItem FindIn(List<CollectorItem> list, string path)
        {
            if (list == null) return null;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && string.Equals(list[i].Path, path, StringComparison.OrdinalIgnoreCase))
                    return list[i];
            }
            return null;
        }

        static string DisplayName(string path)
        {
            try
            {
                if (Directory.Exists(path)) return new DirectoryInfo(path).Name;
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                return name.Length > 0 ? name : System.IO.Path.GetFileName(path);
            }
            catch { return path; }
        }

        static bool SameList(List<CollectorItem> old, List<CollectorItem> next)
        {
            if (old == null) return next.Count == 0;
            if (old.Count != next.Count) return false;
            for (int i = 0; i < old.Count; i++)
            {
                CollectorItem a = old[i], b = next[i];
                if (a == null || b == null) return false;
                if (!string.Equals(a.Path, b.Path, StringComparison.OrdinalIgnoreCase)) return false;
                if (!string.Equals(a.Name, b.Name, StringComparison.Ordinal)) return false;
                if (!string.Equals(a.Group, b.Group, StringComparison.OrdinalIgnoreCase)) return false;
                if (a.Hidden != b.Hidden || a.Manual != b.Manual || a.IsFolder != b.IsFolder) return false;
            }
            return true;
        }
    }
}
