// Liuli · 配置存取与一键备份：单文件 boards.json（原子替换），备份走 zip。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Liuli
{
    static class Store
    {
        public static Workspace Load(Rectangle[] screens)
        {
            string text = Util.ReadAllTextSafe(Paths.BoardsFile);
            if (string.IsNullOrEmpty(text))
            {
                Workspace def = Workspace.Default(screens);
                def.Set.FirstRun = true;
                return def;
            }
            try
            {
                Dictionary<string, object> root = Json.Obj(Json.Parse(text));
                if (root == null) throw new Exception("root is not object");
                Workspace ws = new Workspace();
                ReadSettings(ws.Set, Json.Obj(Get(root, "settings")));
                List<object> arr = Json.Arr(Get(root, "boards"));
                if (arr != null)
                {
                    for (int i = 0; i < arr.Count; i++)
                    {
                        Board b = ReadBoard(Json.Obj(arr[i]));
                        if (b != null) ws.Boards.Add(b);
                    }
                }
                if (ws.Boards.Count == 0)
                {
                    Workspace def = Workspace.Default(screens);
                    def.Set = ws.Set;
                    ws = def;
                }
                ws.ClampAll(screens);
                ws.Set.FirstRun = false;
                return ws;
            }
            catch (Exception ex)
            {
                Log.Error("store", "boards.json 解析失败，回退默认：" + ex.Message);
                try
                {
                    string broken = Paths.BoardsFile + ".broken-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                    File.Copy(Paths.BoardsFile, broken, true);
                }
                catch { }
                Workspace def = Workspace.Default(screens);
                return def;
            }
        }

        static object Get(Dictionary<string, object> d, string key)
        {
            object v;
            if (d != null && d.TryGetValue(key, out v)) return v;
            return null;
        }

        public static bool Save(Workspace ws)
        {
            try
            {
                Paths.Ensure();
                Dictionary<string, object> root = new Dictionary<string, object>();
                root["app"] = "Liuli";
                root["version"] = V.Version;
                root["savedAt"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                root["settings"] = WriteSettings(ws.Set);
                List<object> boards = new List<object>();
                for (int i = 0; i < ws.Boards.Count; i++) boards.Add(WriteBoard(ws.Boards[i]));
                root["boards"] = boards;
                return Util.WriteAllTextAtomic(Paths.BoardsFile, Json.Write(root, true));
            }
            catch (Exception ex)
            {
                Log.Error("store", "保存失败：" + ex.Message);
                return false;
            }
        }

        // ---------- 设置 ----------
        static Dictionary<string, object> WriteSettings(Settings s)
        {
            Dictionary<string, object> d = new Dictionary<string, object>();
            d["glass"] = s.GlassPct;
            d["blur"] = s.BlurPct;
            d["radius"] = s.Radius;
            d["liveGlass"] = s.LiveGlass;
            d["tone"] = s.Tone;
            d["snap"] = s.Snap;
            d["alwaysTop"] = s.AlwaysTop;
            d["showBar"] = s.ShowBar;
            d["hideDesktopIcons"] = s.HideDesktopIcons;
            d["autoStart"] = s.AutoStart;
            d["weatherOn"] = s.WeatherOn;
            d["wallMode"] = s.WallMode;
            d["wallPath"] = s.WallPath;
            d["wallDim"] = s.WallDim;
            d["wallMute"] = s.WallMute;
            d["wallPause"] = s.WallPause;
            d["firstRun"] = s.FirstRun;
            d["barX"] = s.BarX;
            d["barY"] = s.BarY;
            d["diagnose"] = s.Diagnose;
            d["hideOnFullscreen"] = s.HideOnFullscreen;
            d["organizeMove"] = s.OrganizeMove;
            d["storeRoot"] = s.StoreRoot;
            return d;
        }

        static void ReadSettings(Settings s, Dictionary<string, object> d)
        {
            if (d == null) return;
            s.GlassPct = Util.Clamp(Json.Int(d, "glass", s.GlassPct), 0, 100);
            s.BlurPct = Util.Clamp(Json.Int(d, "blur", s.BlurPct), 0, 100);
            s.Radius = Util.Clamp(Json.Int(d, "radius", s.Radius), 10, 30);
            s.LiveGlass = Json.Bool(d, "liveGlass", s.LiveGlass);
            s.Tone = Json.Str(d, "tone", s.Tone);
            s.Snap = Json.Bool(d, "snap", s.Snap);
            s.AlwaysTop = Json.Bool(d, "alwaysTop", s.AlwaysTop);
            s.ShowBar = Json.Bool(d, "showBar", s.ShowBar);
            s.HideDesktopIcons = Json.Bool(d, "hideDesktopIcons", s.HideDesktopIcons);
            s.AutoStart = Json.Bool(d, "autoStart", s.AutoStart);
            s.WeatherOn = Json.Bool(d, "weatherOn", s.WeatherOn);
            s.WallMode = Json.Str(d, "wallMode", s.WallMode);
            s.WallPath = Json.Str(d, "wallPath", s.WallPath);
            s.WallDim = Util.Clamp(Json.Int(d, "wallDim", s.WallDim), 0, 80);
            s.WallMute = Json.Bool(d, "wallMute", s.WallMute);
            s.WallPause = Json.Bool(d, "wallPause", s.WallPause);
            s.FirstRun = Json.Bool(d, "firstRun", s.FirstRun);
            s.BarX = Json.Int(d, "barX", s.BarX);
            s.BarY = Json.Int(d, "barY", s.BarY);
            s.Diagnose = Json.Bool(d, "diagnose", s.Diagnose);
            s.HideOnFullscreen = Json.Bool(d, "hideOnFullscreen", s.HideOnFullscreen);
            s.OrganizeMove = Json.Bool(d, "organizeMove", s.OrganizeMove);
            s.StoreRoot = Json.Str(d, "storeRoot", s.StoreRoot);
        }

        // ---------- 看板 ----------
        static Dictionary<string, object> WriteBoard(Board b)
        {
            Dictionary<string, object> d = new Dictionary<string, object>();
            d["id"] = b.Id;
            d["kind"] = (int)b.Kind;
            d["title"] = b.Title;
            d["x"] = b.X; d["y"] = b.Y; d["w"] = b.W; d["h"] = b.H;
            d["accent"] = b.Accent;
            d["opacity"] = b.Opacity;
            d["pinned"] = b.Pinned;
            d["locked"] = b.Locked;
            d["collapsed"] = b.Collapsed;
            d["z"] = b.Z;
            d["text"] = b.Text;
            if (b.Kind == BoardKind.Status)
            {
                d["city"] = b.City;
                d["lat"] = b.Lat;
                d["lon"] = b.Lon;
                d["showWeather"] = b.ShowWeather;
                d["showStats"] = b.ShowStats;
            }
            if (b.Items.Count > 0)
            {
                List<object> items = new List<object>();
                for (int i = 0; i < b.Items.Count; i++)
                {
                    Item it = b.Items[i];
                    if (b.Kind == BoardKind.Todo)
                    {
                        Dictionary<string, object> o = new Dictionary<string, object>();
                        o["text"] = it.Text; o["done"] = it.Done;
                        items.Add(o);
                    }
                    else if (b.Kind == BoardKind.Files)
                    {
                        Dictionary<string, object> o = new Dictionary<string, object>();
                        o["text"] = it.Text; o["path"] = it.Path;
                        items.Add(o);
                    }
                    else
                    {
                        Dictionary<string, object> o = new Dictionary<string, object>();
                        o["text"] = it.Text; o["extra"] = it.Extra;
                        items.Add(o);
                    }
                }
                d["items"] = items;
            }
            return d;
        }

        static Board ReadBoard(Dictionary<string, object> d)
        {
            if (d == null) return null;
            Board b = new Board();
            b.Id = Json.Str(d, "id", "");
            if (b.Id.Length == 0) b.NewId();
            int kind = Json.Int(d, "kind", 0);
            b.Kind = (BoardKind)Util.Clamp(kind, 0, 5);
            b.Title = Json.Str(d, "title", Kinds.Name(b.Kind));
            b.X = Json.Int(d, "x", 40); b.Y = Json.Int(d, "y", 40);
            b.W = Json.Int(d, "w", 320); b.H = Json.Int(d, "h", 260);
            b.Accent = Json.Int(d, "accent", -1);
            b.Opacity = Json.Int(d, "opacity", 100);
            b.Pinned = Json.Bool(d, "pinned", false);
            b.Locked = Json.Bool(d, "locked", false);
            b.Collapsed = Json.Bool(d, "collapsed", false);
            b.Z = Json.Int(d, "z", 0);
            b.Text = Json.Str(d, "text", "");
            b.City = Json.Str(d, "city", "道孚");
            b.Lat = Json.Num(d, "lat", 30.98);
            b.Lon = Json.Num(d, "lon", 101.12);
            b.ShowWeather = Json.Bool(d, "showWeather", true);
            b.ShowStats = Json.Bool(d, "showStats", true);
            List<object> items = Json.Arr(Get(d, "items"));
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    Dictionary<string, object> o = Json.Obj(items[i]);
                    if (o == null) continue;
                    Item it = new Item();
                    it.Text = Json.Str(o, "text", "");
                    it.Path = Json.Str(o, "path", "");
                    it.Extra = Json.Str(o, "extra", "");
                    it.Done = Json.Bool(o, "done", false);
                    b.Items.Add(it);
                }
            }
            return b;
        }

        // ---------- 备份 ----------
        public static string BackupPath(string prefix)
        {
            Paths.Ensure();
            string name = prefix + "-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".zip";
            return Path.Combine(Paths.Backups, name);
        }

        public static string Export(Workspace ws)
        {
            try
            {
                Save(ws);
                string path = BackupPath("Liuli-backup");
                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    AddFile(zip, Paths.BoardsFile, "boards.json");
                    if (File.Exists(Paths.WeatherFile)) AddFile(zip, Paths.WeatherFile, "weather.json");
                    ZipArchiveEntry readme = zip.CreateEntry("README.txt");
                    using (StreamWriter w = new StreamWriter(readme.Open(), Encoding.UTF8))
                    {
                        w.WriteLine("Liuli 看板备份 · " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                        w.WriteLine("源： " + Paths.BoardsFile);
                        w.WriteLine("恢复：设置 → 数据 → 导入备份，选择本 zip。");
                        w.WriteLine("版本： " + V.Version);
                    }
                }
                Log.Info("store", "备份完成 " + path);
                return path;
            }
            catch (Exception ex)
            {
                Log.Error("store", "备份失败：" + ex.Message);
                return null;
            }
        }

        static void AddFile(ZipArchive zip, string file, string entryName)
        {
            if (!File.Exists(file)) return;
            ZipArchiveEntry e = zip.CreateEntry(entryName, CompressionLevel.Optimal);
            using (Stream s = e.Open())
            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.CopyTo(s);
            }
        }

        /// <summary>导入备份：先给当前状态留一份，再替换 boards.json 并返回解析结果。</summary>
        public static Workspace Import(string zipPath, Rectangle[] screens, out string message)
        {
            message = "";
            try
            {
                if (!File.Exists(zipPath)) { message = "备份文件不存在"; return null; }
                string text = null;
                using (FileStream fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read))
                using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    ZipArchiveEntry e = zip.GetEntry("boards.json");
                    if (e == null) { message = "备份里没有 boards.json"; return null; }
                    using (StreamReader r = new StreamReader(e.Open(), Encoding.UTF8))
                        text = r.ReadToEnd();
                }
                if (string.IsNullOrEmpty(text) || Json.Obj(Json.Parse(text)) == null)
                {
                    message = "备份内容不是有效配置";
                    return null;
                }
                try { Export(Load(screens)); } catch { }
                string tmp = Paths.BoardsFile + ".import";
                File.WriteAllText(tmp, text, new UTF8Encoding(true));
                if (File.Exists(Paths.BoardsFile)) File.Delete(Paths.BoardsFile);
                File.Move(tmp, Paths.BoardsFile);
                Workspace ws = Load(screens);
                message = "已导入 " + ws.Boards.Count + " 块看板";
                Log.Info("store", "导入备份 " + zipPath);
                return ws;
            }
            catch (Exception ex)
            {
                message = "导入失败：" + ex.Message;
                Log.Error("store", ex);
                return null;
            }
        }
    }
}
