// Liuli · 图标：桌面扫描 + Shell 图标提取（按路径+mtime 缓存，绘制帧不碰磁盘）。
// 图标整理默认只创建“入口”，不移动、不删除用户的桌面文件；开启“整理时移动原件”后才真实移动（可撤销）。
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
        public bool Public;   // 来自公共桌面（C:\Users\Public\Desktop），真实整理时不移动
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
                            if (string.Equals(p, LiuliRoot, StringComparison.OrdinalIgnoreCase)) continue;
                            FileAttributes at = File.GetAttributes(p);
                            if ((at & FileAttributes.Hidden) != 0) continue;
                            if (seen.Contains(System.IO.Path.GetFileName(p))) continue;
                            seen.Add(System.IO.Path.GetFileName(p));
                            DeskItem it = new DeskItem();
                            it.Path = p;
                            it.Name = Directory.Exists(p) ? new DirectoryInfo(p).Name : System.IO.Path.GetFileNameWithoutExtension(p);
                            it.Ext = Directory.Exists(p) ? "" : System.IO.Path.GetExtension(p).TrimStart('.').ToLowerInvariant();
                            it.Public = (d == 1);
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

        /// <summary>桌面上的 Liuli 收纳根目录：桌面\Liuli\&lt;分组&gt;。只存原件，不放副本。</summary>
        public static string LiuliRoot
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Liuli"); }
        }

        /// <summary>把桌面上的文件/文件夹移进 桌面\Liuli\&lt;group&gt;，返回新路径；失败返回 null。</summary>
        public static string MoveIntoGroup(string path, string group)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path) && !Directory.Exists(path)) return null;
                string dir = Path.Combine(LiuliRoot, string.IsNullOrEmpty(group) ? "其他" : group);
                Directory.CreateDirectory(dir);
                string dest = UniqueDest(dir, Path.GetFileName(path));
                if (Directory.Exists(path)) Directory.Move(path, dest);
                else File.Move(path, dest);
                Log.Info("icons", "移入 " + Path.GetFileName(path) + " -> " + dest);
                return dest;
            }
            catch (Exception ex)
            {
                Log.Warn("icons", "移入失败 " + path + " " + ex.Message);
                return null;
            }
        }

        static string UniqueDest(string dir, string name)
        {
            string dest = Path.Combine(dir, name);
            if (!File.Exists(dest) && !Directory.Exists(dest)) return dest;
            string stem = Path.GetFileNameWithoutExtension(name);
            string ext = Path.GetExtension(name);
            for (int i = 2; i < 999; i++)
            {
                dest = Path.Combine(dir, stem + " (" + i + ")" + ext);
                if (!File.Exists(dest) && !Directory.Exists(dest)) return dest;
            }
            return Path.Combine(dir, stem + " (" + DateTime.Now.ToString("HHmmss") + ")" + ext);
        }

        /// <summary>真实整理：把桌面条目移到 桌面\Liuli\分组，并把“原路径→新路径”写进 manifest 供撤销。</summary>
        static int MoveAll(List<DeskItem> items)
        {
            List<object> moves = new List<object>();
            int moved = 0;
            for (int i = 0; i < items.Count; i++)
            {
                DeskItem it = items[i];
                if (it.Public) continue; // 公共桌面条目只收入口，不动文件（避免权限问题）
                string dest = MoveIntoGroup(it.Path, it.Group);
                if (dest == null) continue;
                Dictionary<string, object> m = new Dictionary<string, object>();
                m["from"] = it.Path;
                m["to"] = dest;
                moves.Add(m);
                it.Path = dest;
                moved++;
                SaveManifest(moves);
            }
            return moved;
        }

        static void SaveManifest(List<object> moves)
        {
            try
            {
                Dictionary<string, object> root = new Dictionary<string, object>();
                root["moves"] = moves;
                Util.WriteAllTextAtomic(Paths.MoveManifestFile, Json.Write(root, true));
            }
            catch (Exception ex) { Log.Warn("icons", "写移动清单失败 " + ex.Message); }
        }

        /// <summary>把一次拖入移动追加到清单，保证「撤销上次整理」也能把它移回。</summary>
        public static void RecordMove(string from, string to)
        {
            try
            {
                List<object> moves = new List<object>();
                if (File.Exists(Paths.MoveManifestFile))
                {
                    Dictionary<string, object> root = Json.Obj(Json.Parse(Util.ReadAllTextSafe(Paths.MoveManifestFile)));
                    List<object> old = Json.Arr(root == null ? null : root["moves"]);
                    if (old != null) for (int i = 0; i < old.Count; i++) moves.Add(old[i]);
                }
                Dictionary<string, object> m = new Dictionary<string, object>();
                m["from"] = from;
                m["to"] = to;
                moves.Add(m);
                SaveManifest(moves);
            }
            catch (Exception ex) { Log.Warn("icons", "追加移动清单失败 " + ex.Message); }
        }

        /// <summary>撤销真实整理：把 manifest 里的文件按“新→原”移回桌面。</summary>
        public static void MoveBackAll()
        {
            try
            {
                if (!File.Exists(Paths.MoveManifestFile)) return;
                string text = Util.ReadAllTextSafe(Paths.MoveManifestFile);
                Dictionary<string, object> root = Json.Obj(Json.Parse(text));
                List<object> moves = Json.Arr(root == null ? null : root["moves"]);
                int back = 0, failed = 0;
                if (moves != null)
                {
                    for (int i = 0; i < moves.Count; i++)
                    {
                        Dictionary<string, object> m = Json.Obj(moves[i]);
                        if (m == null) continue;
                        string from = Json.Str(m, "from", "");
                        string to = Json.Str(m, "to", "");
                        if (from.Length == 0 || to.Length == 0) continue;
                        if (!File.Exists(to) && !Directory.Exists(to)) continue;
                        if (File.Exists(from) || Directory.Exists(from)) continue;
                        try
                        {
                            string dir = Path.GetDirectoryName(from);
                            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                            if (Directory.Exists(to)) Directory.Move(to, from);
                            else File.Move(to, from);
                            back++;
                        }
                        catch (Exception ex)
                        {
                            // 目标目录不可写（如公共桌面）时，退回用户桌面，至少不丢原件
                            try
                            {
                                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                                string alt = UniqueDest(desktop, Path.GetFileName(to));
                                if (Directory.Exists(to)) Directory.Move(to, alt);
                                else File.Move(to, alt);
                                Log.Warn("icons", "撤销时无法回到原路径，已放回桌面 " + alt + "（" + ex.Message + "）");
                                back++;
                            }
                            catch (Exception ex2)
                            {
                                failed++;
                                Log.Error("icons", "撤销移回失败 " + to + " " + ex2.Message);
                            }
                        }
                    }
                }
                Log.Info("icons", "撤销移动 " + back + " 项" + (failed > 0 ? "，失败 " + failed + " 项（清单保留，可重试）" : ""));
                if (failed == 0) File.Delete(Paths.MoveManifestFile);
                CleanupRoot();
            }
            catch (Exception ex) { Log.Warn("icons", "撤销移动失败 " + ex.Message); }
        }

        static void CleanupRoot()
        {
            try
            {
                if (!Directory.Exists(LiuliRoot)) return;
                // 撤销后只剩空分组目录，整棵树一起清掉（有文件则保留，绝不递归删用户文件）
                if (Directory.GetFiles(LiuliRoot, "*", SearchOption.AllDirectories).Length == 0)
                    Directory.Delete(LiuliRoot, true);
            }
            catch { }
        }

        /// <summary>一键整理：按分组生成/更新看板（不移动桌面文件），返回新建的看板数。</summary>
        public static int OrganizeToBoards(List<DeskItem> items, Rectangle work, bool move)
        {
            if (items == null || items.Count == 0) return 0;
            try
            {
                Paths.Ensure();
                if (File.Exists(Paths.BoardsFile)) File.Copy(Paths.BoardsFile, Paths.UndoFile, true);
                Log.Info("icons", "整理前快照 " + Paths.UndoFile);
            }
            catch (Exception ex) { Log.Warn("icons", "整理前快照失败 " + ex.Message); }
            if (File.Exists(Paths.MoveManifestFile)) try { File.Delete(Paths.MoveManifestFile); } catch { }
            int moved = move ? MoveAll(items) : 0;
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
            Log.Info("icons", "整理 " + items.Count + " 个图标，新建 " + created + " 块文件看板" + (moved > 0 ? "，移动原件 " + moved + " 项" : ""));
            return created;
        }
    }
}
