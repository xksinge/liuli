// Liuli · 图标：桌面扫描 + Shell 图标提取（按路径+mtime 缓存，绘制帧不碰磁盘）。
// 图标整理只创建“入口”，不移动、不删除用户的桌面文件。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Liuli
{
    sealed class DeskItem
    {
        public string Path = "";
        public string Name = "";
        public string Ext = "";
        public string Group = "";
    }

    static class Icons
    {
        static readonly Dictionary<string, Bitmap> cache = new Dictionary<string, Bitmap>();
        static readonly object gate = new object();

        public static readonly string[] GroupOrder = { "应用", "文件夹", "文档", "图片", "影音", "压缩包", "代码", "其他" };

        static readonly Dictionary<string, string> groups = BuildGroups();

        static Dictionary<string, string> BuildGroups()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            Put(d, "应用", "lnk", "url", "exe", "msi", "bat", "cmd", "appref-ms");
            Put(d, "文档", "txt", "md", "doc", "docx", "pdf", "rtf", "odt", "wps", "ppt", "pptx", "xls", "xlsx", "csv", "et", "dps");
            Put(d, "图片", "png", "jpg", "jpeg", "gif", "bmp", "webp", "svg", "ico", "tif", "tiff", "heic", "psd");
            Put(d, "影音", "mp4", "mkv", "avi", "mov", "wmv", "flv", "webm", "m4v", "mp3", "wav", "flac", "m4a", "aac", "ogg", "wma");
            Put(d, "压缩包", "zip", "rar", "7z", "tar", "gz", "bz2", "xz");
            Put(d, "代码", "cs", "py", "ps1", "js", "ts", "json", "xml", "html", "css", "sql", "java", "c", "cpp", "h", "lua", "sh", "ini", "yml", "yaml");
            LoadCustom(d);
            return d;
        }

        /// <summary>data\groups.txt 自定义扩展名分组，行格式 ext=分组；只接受 GroupOrder 里已有的分组。</summary>
        static void LoadCustom(Dictionary<string, string> d)
        {
            try
            {
                string f = Path.Combine(Paths.Data, "groups.txt");
                if (!File.Exists(f)) return;
                string[] lines = File.ReadAllLines(f, Encoding.UTF8);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("//")) continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string ext = line.Substring(0, eq).Trim().TrimStart('.').ToLowerInvariant();
                    string g = line.Substring(eq + 1).Trim();
                    if (ext.Length == 0) continue;
                    if (Array.IndexOf(GroupOrder, g) < 0) { Log.Warn("icons", "groups.txt 分组无效：" + line); continue; }
                    d[ext] = g;
                }
                Log.Info("icons", "groups.txt 自定义规则已加载");
            }
            catch (Exception ex) { Log.Warn("icons", "groups.txt 解析失败 " + ex.Message); }
        }

        static void Put(Dictionary<string, string> d, string group, params string[] exts)
        {
            for (int i = 0; i < exts.Length; i++) d[exts[i]] = group;
        }

        public static string GroupOf(string path)
        {
            try
            {
                if (Directory.Exists(path)) return "文件夹";
                string ext = System.IO.Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
                string g;
                if (groups.TryGetValue(ext, out g)) return g;
            }
            catch { }
            return "其他";
        }

        public static List<DeskItem> ScanDesktop()
        {
            List<DeskItem> list = new List<DeskItem>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] dirs = {
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
            };
            for (int d = 0; d < dirs.Length; d++)
            {
                string dir = dirs[d];
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
                try
                {
                    foreach (string p in Directory.GetFileSystemEntries(dir))
                    {
                        try
                        {
                            FileAttributes at = File.GetAttributes(p);
                            if ((at & FileAttributes.Hidden) != 0) continue;
                            if (seen.Contains(System.IO.Path.GetFileName(p))) continue;
                            seen.Add(System.IO.Path.GetFileName(p));
                            DeskItem it = new DeskItem();
                            it.Path = p;
                            it.Name = Directory.Exists(p) ? new DirectoryInfo(p).Name : System.IO.Path.GetFileNameWithoutExtension(p);
                            it.Ext = Directory.Exists(p) ? "" : System.IO.Path.GetExtension(p).TrimStart('.').ToLowerInvariant();
                            it.Group = GroupOf(p);
                            list.Add(it);
                        }
                        catch { }
                    }
                }
                catch (Exception ex) { Log.Warn("icons", "扫描桌面失败 " + ex.Message); }
            }
            list.Sort(delegate (DeskItem a, DeskItem b)
            {
                int g1 = Array.IndexOf(GroupOrder, a.Group), g2 = Array.IndexOf(GroupOrder, b.Group);
                if (g1 != g2) return g1.CompareTo(g2);
                return string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase);
            });
            return list;
        }

        public static void Draw(Graphics g, string path, Rectangle r)
        {
            Bitmap bmp = Get(path, Math.Max(16, r.Width));
            if (bmp == null)
            {
                Theme.Glyph(g, Glyph.Folder, r, Theme.TextFaint, 1.3f * Theme.S);
                return;
            }
            InterpolationMode old = g.InterpolationMode;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(bmp, r);
            g.InterpolationMode = old;
        }

        public static Bitmap Get(string path, int size)
        {
            if (string.IsNullOrEmpty(path)) return null;
            string key = path + "|" + SafeStamp(path);
            lock (gate)
            {
                Bitmap b;
                if (cache.TryGetValue(key, out b)) return b;
            }
            Bitmap made = Load(path, size);
            if (made == null) return null;
            lock (gate)
            {
                if (cache.Count > 320) ClearLocked();
                cache[key] = made;
            }
            return made;
        }

        static string SafeStamp(string path)
        {
            try
            {
                if (Directory.Exists(path)) return "d";
                return File.GetLastWriteTimeUtc(path).Ticks.ToString();
            }
            catch { return "0"; }
        }

        static Bitmap Load(string path, int size)
        {
            IntPtr hIcon = IntPtr.Zero;
            try
            {
                Native.SHFILEINFO info = new Native.SHFILEINFO();
                uint flags = Native.SHGFI_ICON | Native.SHGFI_LARGEICON;
                uint attrs = 0;
                if (Directory.Exists(path))
                {
                    attrs = 0x10; // FILE_ATTRIBUTE_DIRECTORY
                    flags |= Native.SHGFI_USEFILEATTRIBUTES;
                }
                Native.SHGetFileInfo(path, attrs, ref info, (uint)Marshal.SizeOf(info), flags);
                hIcon = info.hIcon;
                if (hIcon == IntPtr.Zero) return null;
                using (Icon ic = Icon.FromHandle(hIcon))
                {
                    Bitmap src = ic.ToBitmap();
                    Bitmap dst = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                    using (Graphics g = Graphics.FromImage(dst))
                    {
                        g.Clear(Color.Transparent);
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.DrawImage(src, new Rectangle(0, 0, size, size));
                    }
                    src.Dispose();
                    return dst;
                }
            }
            catch (Exception ex)
            {
                Log.Warn("icons", "提取图标失败 " + path + " " + ex.Message);
                return null;
            }
            finally
            {
                if (hIcon != IntPtr.Zero) Native.DestroyIcon(hIcon);
            }
        }

        static void ClearLocked()
        {
            foreach (KeyValuePair<string, Bitmap> kv in cache) { try { kv.Value.Dispose(); } catch { } }
            cache.Clear();
        }

        public static void Clear()
        {
            lock (gate) ClearLocked();
        }

        /// <summary>一键整理：按分组生成/更新看板（不移动桌面文件），返回新建的看板数。</summary>
        public static int OrganizeToBoards(List<DeskItem> items, Rectangle work)
        {
            if (items == null || items.Count == 0) return 0;
            try
            {
                Paths.Ensure();
                if (File.Exists(Paths.BoardsFile)) File.Copy(Paths.BoardsFile, Paths.UndoFile, true);
                Log.Info("icons", "整理前快照 " + Paths.UndoFile);
            }
            catch (Exception ex) { Log.Warn("icons", "整理前快照失败 " + ex.Message); }
            int created = 0;
            Dictionary<string, List<DeskItem>> byGroup = new Dictionary<string, List<DeskItem>>();
            for (int k = 0; k < items.Count; k++)
            {
                string g = items[k].Group;
                List<DeskItem> list;
                if (!byGroup.TryGetValue(g, out list)) { list = new List<DeskItem>(); byGroup[g] = list; }
                list.Add(items[k]);
            }

            for (int gi = 0; gi < GroupOrder.Length; gi++)
            {
                List<DeskItem> list;
                if (!byGroup.TryGetValue(GroupOrder[gi], out list) || list.Count == 0) continue;

                // 同名看板就原地更新，不重复长出新板
                Board same = null;
                for (int b = 0; b < App.Ws.Boards.Count; b++)
                {
                    Board x = App.Ws.Boards[b];
                    if (x.Kind == BoardKind.Files && x.Title == GroupOrder[gi]) { same = x; break; }
                }
                if (same != null)
                {
                    same.Items.Clear();
                    for (int k = 0; k < list.Count && k < 40; k++) same.Items.Add(new Item(list[k].Name, list[k].Path));
                    App.RefreshBoard(same);
                    continue;
                }

                Board nb = new Board();
                nb.NewId();
                nb.Kind = BoardKind.Files;
                nb.Title = GroupOrder[gi];
                nb.W = 330; nb.H = 330;
                // 靠主屏右侧两列排，避开左侧默认看板
                int col = created / 4;
                int row = created % 4;
                nb.X = work.Right - Util.RoundI(356 * Theme.S) - col * Util.RoundI(346 * Theme.S);
                nb.Y = work.Y + Util.RoundI(40 * Theme.S) + row * Util.RoundI(346 * Theme.S);
                for (int k = 0; k < list.Count && k < 40; k++) nb.Items.Add(new Item(list[k].Name, list[k].Path));
                nb.Z = App.Ws.Boards.Count + 1;
                App.Ws.Boards.Add(nb);
                App.CreateWindow(nb, true);
                created++;
            }
            App.Save();
            Log.Info("icons", "整理 " + items.Count + " 个图标，新建 " + created + " 块文件看板");
            return created;
        }
    }
}
