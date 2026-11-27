// Liuli v2 · 应用面板数据：从用户/公共开始菜单扫描快捷方式，名称去重后按名称排序。
using System;
using System.Collections.Generic;
using System.IO;

namespace Liuli
{
    sealed class AppEntry
    {
        public string Name = "";
        public string Path = "";
    }

    static class AppCatalog
    {
        static List<AppEntry> all = new List<AppEntry>();
        static bool loaded;
        static DateTime loadedAt;

        public static int Count
        {
            get { Ensure(); return all.Count; }
        }

        public static void Reload()
        {
            List<AppEntry> list = new List<AppEntry>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                ScanDir(Environment.GetFolderPath(Environment.SpecialFolder.Programs), 0, list, seen);
                ScanDir(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms), 0, list, seen);
            }
            catch (Exception ex) { Log.Warn("apps", "扫描开始菜单失败 " + ex.Message); }
            list.Sort(delegate(AppEntry a, AppEntry b)
            {
                return string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase);
            });
            all = list;
            loaded = true;
            loadedAt = DateTime.Now;
            Log.Info("apps", "开始菜单入口 " + all.Count + " 项");
        }

        static void Ensure()
        {
            if (!loaded || (DateTime.Now - loadedAt).TotalSeconds > 300) Reload();
        }

        static void ScanDir(string dir, int depth, List<AppEntry> list, HashSet<string> seen)
        {
            if (depth > 3 || string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;
            try
            {
                string[] files = Directory.GetFiles(dir, "*.lnk");
                for (int i = 0; i < files.Length && list.Count < 500; i++)
                {
                    string name = Path.GetFileNameWithoutExtension(files[i]);
                    if (Skip(name, files[i])) continue;
                    string key = name.Trim().ToLowerInvariant();
                    if (key.Length == 0 || seen.Contains(key)) continue;
                    seen.Add(key);
                    AppEntry it = new AppEntry();
                    it.Name = name.Trim();
                    it.Path = files[i];
                    list.Add(it);
                }
                string[] dirs = Directory.GetDirectories(dir);
                for (int i = 0; i < dirs.Length; i++) ScanDir(dirs[i], depth + 1, list, seen);
            }
            catch (Exception ex) { Log.Warn("apps", "跳过目录 " + dir + "：" + ex.Message); }
        }

        static bool Skip(string name, string path)
        {
            string n = name == null ? "" : name.ToLowerInvariant();
            string p = path == null ? "" : path.ToLowerInvariant();
            if (n.Length == 0) return true;
            if (n.IndexOf("uninstall", StringComparison.Ordinal) >= 0) return true;
            if (n.IndexOf("卸载", StringComparison.Ordinal) >= 0) return true;
            if (n.IndexOf("帮助", StringComparison.Ordinal) >= 0) return true;
            if (n.IndexOf("help", StringComparison.Ordinal) >= 0) return true;
            if (n.IndexOf("readme", StringComparison.Ordinal) >= 0) return true;
            if (n.IndexOf("website", StringComparison.Ordinal) >= 0) return true;
            if (n.IndexOf("网址", StringComparison.Ordinal) >= 0) return true;
            if (p.IndexOf("\\startup\\", StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        public static List<AppEntry> Query(string keyword, int max)
        {
            Ensure();
            List<AppEntry> result = new List<AppEntry>();
            string q = keyword == null ? "" : keyword.Trim().ToLowerInvariant();
            for (int i = 0; i < all.Count && result.Count < max; i++)
            {
                if (q.Length == 0 || all[i].Name.ToLowerInvariant().IndexOf(q, StringComparison.Ordinal) >= 0)
                    result.Add(all[i]);
            }
            return result;
        }
    }
}
