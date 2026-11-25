// Liuli · 视频壁纸：把 Windows Media Player 控件（COM，GPU 解码）挂进桌面壁纸层。
// 优点：系统自带解码器、不用自带 FFmpeg；缺点：依赖 Media Feature Pack，缺失时上层退回图片模式。
using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Liuli
{
    sealed class WmpAx : AxHost
    {
        public WmpAx() : base("6BF52A52-394A-11D3-B153-00C04F79FAA6") { }
        public object Player { get { return GetOcx(); } }
    }

    static class VideoWall
    {
        static Form host;
        static WmpAx ax;
        static object player;
        static bool on;
        static bool paused;
        static long lastCheck;

        public static bool On { get { return on; } }

        public static bool Start(string path, bool mute)
        {
            if (on) return true;
            try
            {
                host = new Form();
                host.FormBorderStyle = FormBorderStyle.None;
                host.ShowInTaskbar = false;
                host.StartPosition = FormStartPosition.Manual;
                host.BackColor = Color.Black;
                host.Bounds = SystemInformation.VirtualScreen;

                ax = new WmpAx();
                ax.Dock = DockStyle.Fill;
                host.Controls.Add(ax);
                host.Show();

                player = ax.Player;
                if (player == null) throw new Exception("WMP 控件没有初始化");

                Set(player, "uiMode", "none");
                Set(player, "stretchToFit", true);
                object settings = Get(player, "settings");
                if (settings != null)
                {
                    Set(settings, "autoStart", true);
                    Set(settings, "mute", mute);
                    if (mute) Set(settings, "volume", 0);
                    try { settings.GetType().InvokeMember("setMode", BindingFlags.InvokeMethod, null, settings, new object[] { "loop", true }); }
                    catch { }
                }
                Set(player, "URL", path);
                object controls = Get(player, "controls");
                if (controls != null) controls.GetType().InvokeMember("play", BindingFlags.InvokeMethod, null, controls, null);

                IntPtr wallpaperLayer = FindWallpaperLayer();
                if (wallpaperLayer != IntPtr.Zero)
                {
                    Native.SetParent(host.Handle, wallpaperLayer);
                    int style = Native.GetWindowLong(host.Handle, -16);
                    style = (style & 0x7FFFFFFF) | 0x40000000;   // WS_CHILD，去掉 WS_POPUP
                    Native.SetWindowLong(host.Handle, -16, style);
                    Rectangle vb = SystemInformation.VirtualScreen;
                    Native.SetWindowPos(host.Handle, IntPtr.Zero, 0, 0, vb.Width, vb.Height,
                        0x0004 | 0x0010 | 0x0040); // NOZORDER | NOACTIVATE | SHOWWINDOW
                    Log.Info("wall", "视频壁纸挂在桌面壁纸层");
                }
                else
                {
                    // 拿不到壁纸层：至少把窗口压到最底，别挡住看板
                    Native.KeepAtBottom(host.Handle);
                    Log.Warn("wall", "没拿到 WorkerW 壁纸层，视频退化为底层窗口");
                }
                on = true;
                paused = false;
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn("wall", "视频壁纸启动失败 " + ex.Message);
                Stop();
                return false;
            }
        }

        static IntPtr FindWallpaperLayer()
        {
            IntPtr prog = Native.FindWindow("Progman", null);
            IntPtr res;
            if (prog != IntPtr.Zero) Native.SendMessageTimeout(prog, 0x052C, IntPtr.Zero, IntPtr.Zero, 0, 1500, out res);

            IntPtr iconHost = IntPtr.Zero;
            IntPtr w = IntPtr.Zero;
            while (true)
            {
                w = Native.FindWindowEx(IntPtr.Zero, w, "WorkerW", null);
                if (w == IntPtr.Zero) break;
                IntPtr dv = Native.FindWindowEx(w, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (dv != IntPtr.Zero) { iconHost = w; break; }
            }
            if (iconHost == IntPtr.Zero) return IntPtr.Zero;
            // 图标宿主之后的那个 WorkerW 就是壁纸层
            IntPtr layer = Native.FindWindowEx(IntPtr.Zero, iconHost, "WorkerW", null);
            return layer;
        }

        static object Get(object o, string name)
        {
            try { return o.GetType().InvokeMember(name, BindingFlags.GetProperty, null, o, null); }
            catch { return null; }
        }

        static void Set(object o, string name, object v)
        {
            try { o.GetType().InvokeMember(name, BindingFlags.SetProperty, null, o, new object[] { v }); }
            catch { }
        }

        static void Call(object o, string name, params object[] args)
        {
            try { o.GetType().InvokeMember(name, BindingFlags.InvokeMethod, null, o, args); }
            catch { }
        }

        public static void Tick()
        {
            if (!on || player == null) return;
            long now = Environment.TickCount;
            if (now - lastCheck < 2500) return;
            lastCheck = now;
            try
            {
                bool full = FullscreenForeground();
                bool shouldPause = App.Ws.Set.WallPause && full;
                object controls = Get(player, "controls");
                object state = Get(player, "playState");
                int st = state == null ? 0 : Convert.ToInt32(state);
                if (shouldPause && !paused)
                {
                    if (controls != null) Call(controls, "pause");
                    paused = true;
                }
                else if (!shouldPause && paused)
                {
                    if (controls != null) Call(controls, "play");
                    paused = false;
                }
                else if (!paused && st != 3 && st != 2 && st != 6)   // 3 播放 / 2 暂停 / 6 缓冲
                {
                    if (controls != null) Call(controls, "play");
                }
            }
            catch { }
        }

        static bool FullscreenForeground()
        {
            try
            {
                IntPtr fg = Native.GetForegroundWindow();
                if (fg == IntPtr.Zero) return false;
                Native.RECT r;
                if (!Native.GetWindowRect(fg, out r)) return false;
                Screen sc = Screen.FromHandle(fg);
                Rectangle b = sc.Bounds;
                return Math.Abs(r.Left - b.Left) < 4 && Math.Abs(r.Top - b.Top) < 4
                    && Math.Abs(r.Right - b.Right) < 4 && Math.Abs(r.Bottom - b.Bottom) < 4;
            }
            catch { return false; }
        }

        public static void Stop()
        {
            on = false;
            try
            {
                if (player != null)
                {
                    object controls = Get(player, "controls");
                    if (controls != null) Call(controls, "stop");
                }
            }
            catch { }
            try
            {
                if (ax != null) { ax.Dispose(); ax = null; }
                if (host != null) { host.Close(); host.Dispose(); host = null; }
            }
            catch { }
            player = null;
            paused = false;
        }
    }
}
