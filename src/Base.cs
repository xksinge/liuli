// Liuli · 根路径、日志、版本。
using System;
using System.IO;
using System.Text;

namespace Liuli
{
    static class Paths
    {
        static string baseDir;
        public static string Base
        {
            get
            {
                if (baseDir == null)
                {
                    baseDir = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory).TrimEnd('\\');
                    // 开发时 exe 可能在 bin\，数据跟 exe 同级；允许 liuli.root 覆盖
                    string cfg = Path.Combine(baseDir, "liuli.root");
                    if (File.Exists(cfg))
                    {
                        string v = File.ReadAllText(cfg).Trim().Trim('"');
                        if (v.Length > 0) baseDir = Path.GetFullPath(v).TrimEnd('\\');
                    }
                }
                return baseDir;
            }
        }

        public static string Data { get { return Path.Combine(Base, "data"); } }
        public static string BoardsFile { get { return Path.Combine(Data, "boards.json"); } }
        public static string WeatherFile { get { return Path.Combine(Data, "weather.json"); } }
        public static string Logs { get { return Path.Combine(Data, "logs"); } }
        public static string Backups { get { return Path.Combine(Data, "backups"); } }
        public static string IconCache { get { return Path.Combine(Data, "icons"); } }
        public static string ShotDir { get { return Path.Combine(Data, "shots"); } }
        public static string UndoFile { get { return Path.Combine(Data, "undo-organize.json"); } }
        public static string MoveManifestFile { get { return Path.Combine(Data, "move-manifest.json"); } }

        public static void Ensure()
        {
            try
            {
                if (!Directory.Exists(Data)) Directory.CreateDirectory(Data);
                if (!Directory.Exists(Logs)) Directory.CreateDirectory(Logs);
                if (!Directory.Exists(Backups)) Directory.CreateDirectory(Backups);
            }
            catch { }
        }
    }

    static class V
    {
        public const string Version = "1.4.1";
        public const string Name = "Liuli 琉璃";
        public const string Tagline = "把桌面变成一块会呼吸的玻璃看板";
    }

    static class Log
    {
        static readonly object gate = new object();
        static string file;

        public static string FilePath
        {
            get
            {
                if (file == null)
                {
                    try
                    {
                        Paths.Ensure();
                        file = Path.Combine(Paths.Logs, "liuli.log");
                    }
                    catch { file = Path.Combine(Path.GetTempPath(), "liuli.log"); }
                }
                return file;
            }
        }

        public static void Info(string tag, string msg) { Write("I", tag, msg); }
        public static void Warn(string msg) { Write("W", "app", msg); }
        public static void Warn(string tag, string msg) { Write("W", tag, msg); }

        public static void Error(string tag, Exception ex)
        {
            Write("E", tag, ex == null ? "null" : ex.ToString());
        }

        public static void Error(string tag, string msg) { Write("E", tag, msg); }

        static void Write(string level, string tag, string msg)
        {
            try
            {
                lock (gate)
                {
                    Rotate();
                    string line = string.Format("{0} {1} [{2}] {3}\r\n", DateTime.Now.ToString("HH:mm:ss.fff"), level, tag, msg);
                    System.IO.File.AppendAllText(FilePath, line, Encoding.UTF8);
                }
            }
            catch { }
        }

        static void Rotate()
        {
            try
            {
                if (!System.IO.File.Exists(FilePath)) return;
                FileInfo fi = new FileInfo(FilePath);
                if (fi.Length < 512 * 1024) return;
                string old = FilePath + ".1";
                if (System.IO.File.Exists(old)) System.IO.File.Delete(old);
                System.IO.File.Move(FilePath, old);
            }
            catch { }
        }

        public static string Tail(int lines)
        {
            try
            {
                if (!System.IO.File.Exists(FilePath)) return "(无日志)";
                string[] all = System.IO.File.ReadAllLines(FilePath);
                int start = Math.Max(0, all.Length - lines);
                StringBuilder sb = new StringBuilder();
                for (int i = start; i < all.Length; i++) sb.AppendLine(all[i]);
                return sb.ToString();
            }
            catch { return "(日志读取失败)"; }
        }
    }
}
