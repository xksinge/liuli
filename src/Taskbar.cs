// Liuli v2 · 任务栏：只做受支持的自动隐藏（ABM_SETSTATE），完全隐藏留到 P1 且退出必恢复。
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Liuli
{
    static class Taskbar
    {
        const uint ABM_GETSTATE = 0x00000004;
        const uint ABM_SETSTATE = 0x0000000A;
        const int ABS_AUTOHIDE = 0x00000001;
        const int ABS_ALWAYSONTOP = 0x00000002;

        [StructLayout(LayoutKind.Sequential)]
        struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public Native.RECT rc;
            public int lParam;
        }

        [DllImport("shell32.dll")]
        static extern uint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr FindWindow(string cls, string title);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr h, out Native.RECT r);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr h, int cmd);

        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr h);

        public static bool AutoHide()
        {
            try
            {
                APPBARDATA d = MakeData();
                uint state = SHAppBarMessage(ABM_GETSTATE, ref d);
                return (state & ABS_AUTOHIDE) != 0;
            }
            catch { return false; }
        }

        /// <summary>打开/关闭 Explorer 的自动隐藏。返回是否成功。</summary>
        public static bool SetAutoHide(bool on)
        {
            try
            {
                APPBARDATA d = MakeData();
                d.lParam = on ? ABS_AUTOHIDE : ABS_ALWAYSONTOP;
                SHAppBarMessage(ABM_SETSTATE, ref d);
                return AutoHide() == on;
            }
            catch (Exception ex)
            {
                Log.Warn("taskbar", "设置自动隐藏失败：" + ex.Message);
                return false;
            }
        }

        static APPBARDATA MakeData()
        {
            APPBARDATA d = new APPBARDATA();
            d.cbSize = Marshal.SizeOf(d);
            return d;
        }

        /// <summary>主屏任务栏当前是否露出（自动隐藏滑下时返回 false）。</summary>
        public static bool Visible()
        {
            try
            {
                IntPtr h = FindWindow("Shell_TrayWnd", null);
                if (h == IntPtr.Zero || !IsWindowVisible(h)) return false;
                Native.RECT r;
                if (!GetWindowRect(h, out r)) return false;
                Rectangle screen = Screen.PrimaryScreen.Bounds;
                return r.Top < screen.Bottom - 2 && r.Bottom > screen.Top + 2;
            }
            catch { return false; }
        }

        public static int VisibleHeight()
        {
            try
            {
                IntPtr h = FindWindow("Shell_TrayWnd", null);
                if (h == IntPtr.Zero) return 0;
                Native.RECT r;
                if (!GetWindowRect(h, out r)) return 0;
                int hh = r.Bottom - r.Top;
                return hh > 0 && hh < 200 ? hh : 0;
            }
            catch { return 0; }
        }

        /// <summary>任务栏是否真正从屏幕边缘露出（自动隐藏缩回 2px 时返回 false）。</summary>
        public static bool Revealed()
        {
            try
            {
                IntPtr h = FindWindow("Shell_TrayWnd", null);
                if (h == IntPtr.Zero || !IsWindowVisible(h)) return false;
                Native.RECT r;
                if (!GetWindowRect(h, out r)) return false;
                Rectangle screen = Screen.PrimaryScreen.Bounds;
                bool bottomDocked = r.Bottom >= screen.Bottom - 2;
                return bottomDocked && r.Top <= screen.Bottom - 6;
            }
            catch { return false; }
        }

        public static IntPtr Handle()
        {
            try { return FindWindow("Shell_TrayWnd", null); } catch { return IntPtr.Zero; }
        }

        public static void HideAll()
        {
            IntPtr h = Handle();
            if (h != IntPtr.Zero) ShowWindow(h, 0);
        }

        public static void ShowAll()
        {
            IntPtr h = Handle();
            if (h != IntPtr.Zero) ShowWindow(h, 5);
        }
    }
}
