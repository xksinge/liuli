// Liuli v2 · Windows 通知摘要：后台只读通知库，向灵动岛提供最新 toast 标题/正文。
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml;

namespace Liuli
{
    sealed class NotifyNow
    {
        public bool Has;
        public string Key = "";
        public string App = "";
        public string Title = "";
        public string Body = "";
        public int Count;
        public long Arrival;
    }

    static class NotificationService
    {
        const int SqliteOk = 0;
        const int SqliteRow = 100;
        const int SqliteDone = 101;
        const int OpenReadOnly = 1;

        static Thread worker;
        static volatile NotifyNow now = new NotifyNow();
        static List<NotifyNow> all = new List<NotifyNow>();
        static HashSet<string> ignored = new HashSet<string>();
        static long cutoff;
        static IntPtr db;
        static long lastWarn;
        static bool reportedMissing;
        static readonly object gate = new object();

        public static NotifyNow Now
        {
            get { return now; }
        }

        public static long ClearedAt
        {
            get { lock (gate) return cutoff; }
        }

        public static void Start(long clearedAt)
        {
            if (worker != null) return;
            SetCutoff(clearedAt);
            worker = new Thread(Loop);
            worker.IsBackground = true;
            worker.Name = "LiuliNotify";
            try { worker.SetApartmentState(ApartmentState.MTA); } catch { }
            worker.Start();
        }

        static void SetCutoff(long v)
        {
            lock (gate) cutoff = v > 0 ? v : 0;
        }

        public static void IgnoreCurrent()
        {
            string key;
            lock (gate)
            {
                key = now.Key;
                if (key.Length == 0) return;
                ignored.Add(key);
            }
            Publish();
        }

        public static void ClearAll()
        {
            long max = 0;
            lock (gate)
            {
                for (int i = 0; i < all.Count; i++)
                    if (all[i].Arrival > max) max = all[i].Arrival;
                if (max < cutoff) max = cutoff;
                cutoff = max;
                now = new NotifyNow();
            }
            try { V2Shell.NotifyNotificationsChanged(); } catch { }
        }

        static void Loop()
        {
            while (true)
            {
                try { Poll(); }
                catch (Exception ex) { Warn("轮询失败 " + ex.Message); }
                Thread.Sleep(2000);
            }
        }

        static string DbPath()
        {
            string over = Environment.GetEnvironmentVariable("LIULI_WPN_DB");
            if (over != null && over.Length > 0) return over;
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "Windows", "Notifications", "wpndatabase.db");
        }

        static void Poll()
        {
            string path = DbPath();
            if (!File.Exists(path))
            {
                CloseDb();
                if (!reportedMissing) { reportedMissing = true; Log.Warn("notify", "通知库不存在 " + path); }
                PublishEmpty();
                return;
            }

            if (db == IntPtr.Zero && !OpenDb(path)) { PublishEmpty(); return; }

            List<NotifyNow> list = Query();
            if (list == null) { PublishEmpty(); return; }

            lock (gate) all = list;
            Publish();
        }

        static void PublishEmpty()
        {
            bool changed;
            lock (gate)
            {
                changed = now.Has;
                all = new List<NotifyNow>();
                now = new NotifyNow();
            }
            if (!changed) return;
            try { V2Shell.NotifyNotificationsChanged(); } catch { }
        }

        /// <summary>按忽略/清除状态从缓存挑出可见摘要；没有变化就不回调 UI。</summary>
        static void Publish()
        {
            NotifyNow pick = null;
            int count = 0;
            lock (gate)
            {
                for (int i = 0; i < all.Count; i++)
                {
                    NotifyNow n = all[i];
                    if (n.Arrival <= cutoff) continue;
                    if (ignored.Contains(n.Key)) continue;
                    count++;
                    if (pick == null) pick = n;
                }
                NotifyNow old = now;
                if (count == 0)
                {
                    now = new NotifyNow();
                    if (!old.Has) return;
                }
                else
                {
                    NotifyNow v = new NotifyNow();
                    v.Has = true;
                    v.Key = pick.Key;
                    v.App = pick.App;
                    v.Title = pick.Title;
                    v.Body = pick.Body;
                    v.Arrival = pick.Arrival;
                    v.Count = count;
                    now = v;
                    if (old.Has && old.Key == v.Key && old.Count == v.Count) return;
                }
            }
            Log.Info("notify", "可见摘要=" + count);
            try { V2Shell.NotifyNotificationsChanged(); } catch { }
        }

        // ---------- SQLite ----------
        static bool OpenDb(string path)
        {
            IntPtr h = IntPtr.Zero;
            try
            {
                int rc = sqlite3_open_v2(path, out h, OpenReadOnly, IntPtr.Zero);
                if (rc != SqliteOk || h == IntPtr.Zero)
                {
                    Warn("打开通知库失败 rc=" + rc);
                    if (h != IntPtr.Zero) sqlite3_close(h);
                    return false;
                }
                sqlite3_busy_timeout(h, 500);
                db = h;
                Log.Info("notify", "通知库只读连接就绪");
                return true;
            }
            catch (Exception ex)
            {
                Warn("加载 winsqlite3 失败 " + ex.Message);
                if (h != IntPtr.Zero) try { sqlite3_close(h); } catch { }
                return false;
            }
        }

        static void CloseDb()
        {
            if (db != IntPtr.Zero)
            {
                try { sqlite3_close(db); } catch { }
                db = IntPtr.Zero;
            }
        }

        static List<NotifyNow> Query()
        {
            string sql =
                "SELECT n.HandlerId AS HandlerId, CAST(n.Payload AS TEXT) AS Payload, " +
                "n.ArrivalTime AS ArrivalTime, n.ExpiryTime AS ExpiryTime, h.PrimaryId AS PrimaryId " +
                "FROM Notification n LEFT JOIN NotificationHandler h ON h.RecordId = n.HandlerId " +
                "WHERE n.Type = 'toast' ORDER BY n.ArrivalTime DESC LIMIT 12";

            IntPtr stmt = IntPtr.Zero;
            try
            {
                int rc = sqlite3_prepare_v2(db, sql, -1, out stmt, IntPtr.Zero);
                if (rc != SqliteOk)
                {
                    Warn("查询通知库失败 rc=" + rc);
                    return null;
                }

                List<NotifyNow> list = new List<NotifyNow>();
                Dictionary<string, bool> seen = new Dictionary<string, bool>();
                long nowFile = DateTime.UtcNow.ToFileTimeUtc();
                while ((rc = sqlite3_step(stmt)) == SqliteRow)
                {
                    long expiry = sqlite3_column_int64(stmt, 3);
                    if (expiry > 0 && expiry < nowFile) continue;

                    NotifyNow n = Parse(ColText(stmt, 1), ColText(stmt, 4));
                    if (n == null) continue;
                    n.Arrival = sqlite3_column_int64(stmt, 2);
                    if (seen.ContainsKey(n.Key)) continue;
                    seen[n.Key] = true;
                    list.Add(n);
                }
                if (rc != SqliteDone) Warn("读取通知行失败 rc=" + rc);
                return list;
            }
            catch (Exception ex)
            {
                Warn("解析通知库失败 " + ex.Message);
                return null;
            }
            finally
            {
                if (stmt != IntPtr.Zero) try { sqlite3_finalize(stmt); } catch { }
            }
        }

        static NotifyNow Parse(string payload, string primaryId)
        {
            if (payload == null || payload.Trim().Length == 0) return null;
            List<string> texts = new List<string>();
            try
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.DtdProcessing = DtdProcessing.Prohibit;
                settings.XmlResolver = null;
                using (StringReader sr = new StringReader(payload))
                using (XmlReader xr = XmlReader.Create(sr, settings))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.XmlResolver = null;
                    doc.Load(xr);
                    XmlNodeList nodes = doc.SelectNodes("//*[local-name()='text']");
                    if (nodes != null)
                    {
                        for (int i = 0; i < nodes.Count && texts.Count < 4; i++)
                        {
                            string t = nodes[i].InnerText;
                            if (t == null) continue;
                            t = CleanText(t);
                            if (t.Length > 0) texts.Add(t);
                        }
                    }
                }
            }
            catch { return null; }

            if (texts.Count == 0) return null;
            string title = texts[0];
            if (title.Length > 80) title = title.Substring(0, 80) + "…";
            StringBuilder body = new StringBuilder();
            for (int i = 1; i < texts.Count; i++)
            {
                if (body.Length > 0) body.Append(" · ");
                body.Append(texts[i]);
            }
            string bodyText = body.ToString();
            if (bodyText.Length > 120) bodyText = bodyText.Substring(0, 120) + "…";
            string app = AppName(primaryId);

            NotifyNow n = new NotifyNow();
            n.Has = true;
            n.App = app;
            n.Title = title;
            n.Body = bodyText;
            // 同应用、同标题、同正文视为同一条，避免系统重复写入的时间线副本反复出现。
            n.Key = app + "\n" + title + "\n" + bodyText;
            return n;
        }

        static string CleanText(string s)
        {
            StringBuilder b = new StringBuilder(s.Length);
            bool space = false;
            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
                if (ch == '\r' || ch == '\n' || ch == '\t' || ch == ' ')
                {
                    space = true;
                    continue;
                }
                if (space && b.Length > 0) b.Append(' ');
                space = false;
                b.Append(ch);
            }
            return b.ToString().Trim();
        }

        static string AppName(string primaryId)
        {
            if (primaryId == null || primaryId.Trim().Length == 0) return "系统通知";
            string id = primaryId.Trim();
            int bang = id.IndexOf('!');
            if (bang > 0) id = id.Substring(0, bang);
            int under = id.IndexOf('_');
            if (under > 0) id = id.Substring(0, under);
            int dot = id.LastIndexOf('.');
            if (dot >= 0 && dot + 1 < id.Length) id = id.Substring(dot + 1);
            if (id.Length == 0) return "系统通知";
            return id;
        }

        static void Warn(string msg)
        {
            long t = Environment.TickCount;
            if (t - lastWarn < 60000 && t >= lastWarn) return;
            lastWarn = t;
            Log.Warn("notify", msg);
        }

        static string ColText(IntPtr stmt, int col)
        {
            IntPtr p = sqlite3_column_text16(stmt, col);
            return p == IntPtr.Zero ? "" : Marshal.PtrToStringUni(p);
        }

        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int sqlite3_open_v2([MarshalAs(UnmanagedType.LPUTF8Str)] string filename, out IntPtr db, int flags, IntPtr vfs);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int sqlite3_close(IntPtr db);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int sqlite3_busy_timeout(IntPtr db, int ms);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int sqlite3_prepare_v2(IntPtr db, [MarshalAs(UnmanagedType.LPUTF8Str)] string sql, int nByte, out IntPtr stmt, IntPtr tail);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int sqlite3_step(IntPtr stmt);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "sqlite3_column_text16")]
        static extern IntPtr sqlite3_column_text16(IntPtr stmt, int col);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern long sqlite3_column_int64(IntPtr stmt, int col);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int sqlite3_finalize(IntPtr stmt);
    }
}
