// Liuli v2 · SMTC 媒体会话：后台轮询当前播放器，供灵动岛显示与播放控制。
using System;
using System.Threading;
using Windows.Foundation;
using Windows.Media.Control;

namespace Liuli
{
    sealed class MediaNow
    {
        public bool Has;
        public string Title = "";
        public string Artist = "";
        public string App = "";
        public bool Playing;
        public double Position;
        public double Duration;

        public string MetaKey()
        {
            return (Has ? "1" : "0") + "|" + (Playing ? "1" : "0") + "|" + Title + "|" + Artist + "|" + App;
        }
    }

    static class MediaService
    {
        static Thread worker;
        static volatile MediaNow now = new MediaNow();
        static GlobalSystemMediaTransportControlsSessionManager manager;
        static GlobalSystemMediaTransportControlsSession session;
        static long lastWarn;
        static bool warned;

        public static MediaNow Now
        {
            get { return now; }
        }

        public static void Start()
        {
            if (worker != null) return;
            worker = new Thread(Loop);
            worker.IsBackground = true;
            worker.Name = "LiuliMedia";
            try { worker.SetApartmentState(ApartmentState.MTA); } catch { }
            worker.Start();
        }

        public static void TogglePlayPause()
        {
            GlobalSystemMediaTransportControlsSession s = session;
            if (s == null) return;
            try { s.TryTogglePlayPauseAsync(); }
            catch (Exception ex) { Warn("播放/暂停失败 " + ex.Message); }
        }

        public static void SkipNext()
        {
            GlobalSystemMediaTransportControlsSession s = session;
            if (s == null) return;
            try { s.TrySkipNextAsync(); }
            catch (Exception ex) { Warn("下一首失败 " + ex.Message); }
        }

        public static void SkipPrevious()
        {
            GlobalSystemMediaTransportControlsSession s = session;
            if (s == null) return;
            try { s.TrySkipPreviousAsync(); }
            catch (Exception ex) { Warn("上一首失败 " + ex.Message); }
        }

        static void Loop()
        {
            while (true)
            {
                try { Poll(); }
                catch (Exception ex) { Warn("轮询失败 " + ex.Message); }
                Thread.Sleep(900);
            }
        }

        static void Poll()
        {
            if (manager == null) manager = RequestManager();
            if (manager == null)
            {
                PublishEmpty();
                return;
            }

            GlobalSystemMediaTransportControlsSession s = manager.GetCurrentSession();
            if (s == null)
            {
                session = null;
                PublishEmpty();
                return;
            }

            GlobalSystemMediaTransportControlsSessionMediaProperties p = RequestProps(s);
            if (p == null) return;

            bool playing = false;
            double pos = 0;
            double dur = 0;
            try
            {
                GlobalSystemMediaTransportControlsSessionPlaybackInfo info = s.GetPlaybackInfo();
                if (info != null && info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                    playing = true;
                GlobalSystemMediaTransportControlsSessionTimelineProperties tl = s.GetTimelineProperties();
                if (tl != null)
                {
                    pos = tl.Position.TotalSeconds;
                    dur = (tl.EndTime - tl.StartTime).TotalSeconds;
                }
            }
            catch { }

            session = s;

            MediaNow n = new MediaNow();
            n.Has = true;
            n.Title = Clean(p.Title);
            n.Artist = Clean(p.Artist);
            if (n.Artist.Length == 0) n.Artist = Clean(p.AlbumArtist);
            n.App = AppName(s.SourceAppUserModelId);
            if (n.Title.Length == 0) n.Title = n.App.Length > 0 ? n.App : "正在播放";
            n.Playing = playing;
            n.Position = pos < 0 ? 0 : pos;
            n.Duration = dur < 0 ? 0 : dur;

            string oldKey = now.MetaKey();
            string newKey = n.MetaKey();
            now = n;
            if (oldKey != newKey) V2Shell.NotifyMediaChanged();
        }

        static GlobalSystemMediaTransportControlsSessionManager RequestManager()
        {
            try
            {
                IAsyncOperation<GlobalSystemMediaTransportControlsSessionManager> op =
                    GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                ManualResetEvent done = new ManualResetEvent(false);
                op.Completed = delegate(IAsyncOperation<GlobalSystemMediaTransportControlsSessionManager> o, AsyncStatus st)
                {
                    done.Set();
                };
                if (!done.WaitOne(1500)) return null;
                return op.GetResults();
            }
            catch (Exception ex)
            {
                Warn("SMTC 不可用 " + ex.Message);
                return null;
            }
        }

        static GlobalSystemMediaTransportControlsSessionMediaProperties RequestProps(
            GlobalSystemMediaTransportControlsSession s)
        {
            try
            {
                IAsyncOperation<GlobalSystemMediaTransportControlsSessionMediaProperties> op =
                    s.TryGetMediaPropertiesAsync();
                ManualResetEvent done = new ManualResetEvent(false);
                op.Completed = delegate(IAsyncOperation<GlobalSystemMediaTransportControlsSessionMediaProperties> o, AsyncStatus st)
                {
                    done.Set();
                };
                if (!done.WaitOne(800)) return null;
                return op.GetResults();
            }
            catch { return null; }
        }

        static void PublishEmpty()
        {
            if (now.Has)
            {
                now = new MediaNow();
                V2Shell.NotifyMediaChanged();
            }
        }

        static string Clean(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            string v = s.Replace("\r", " ").Replace("\n", " ").Trim();
            return v.Length > 80 ? v.Substring(0, 80) + "…" : v;
        }

        static string AppName(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            string v = id;
            int bang = v.LastIndexOf('!');
            if (bang >= 0 && bang + 1 < v.Length) v = v.Substring(bang + 1);
            if (v.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) v = v.Substring(0, v.Length - 4);
            return v;
        }

        static void Warn(string msg)
        {
            long t = Environment.TickCount;
            if (!warned || t - lastWarn > 10000)
            {
                warned = true;
                lastWarn = t;
                Log.Warn("media", msg);
            }
        }
    }
}
