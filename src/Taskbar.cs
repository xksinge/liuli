// Liuli v2 · 任务栏：自动隐藏（ABM_SETSTATE）、按屏露出检测、多实例隐藏/恢复。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
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

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int GetClassName(IntPtr h, StringBuilder sb, int max);

        static string ClassOf(IntPtr h)
        {
            StringBuilder sb = new StringBuilder(128);
            GetClassName(h, sb, sb.Capacity);
            return sb.ToString();
        }

        static bool IsTaskbarClass(string name)
        {
            return name == "Shell_TrayWnd" || name == "Shell_SecondaryTrayWnd";
        }

        /// <summary>枚举主任务栏与所有副屏任务栏（副屏多实例均为 Shell_SecondaryTrayWnd）。</summary>
        public static List<IntPtr> Handles()
        {
            List<IntPtr> list = new List<IntPtr>();
            try
            {
                Native.EnumWindows(delegate(IntPtr h, IntPtr l)
                {
                    if (IsTaskbarClass(ClassOf(h))) list.Add(h);
                    return true;
                }, IntPtr.Zero);
            }
            catch { }
            return list;
        }

        static IntPtr HandleFor(Screen screen)
        {
            if (screen == null) return IntPtr.Zero;
            Rectangle bounds = screen.Bounds;
            List<IntPtr> list = Handles();
            for (int i = 0; i < list.Count; i++)
            {
                Native.RECT r;
                if (!GetWindowRect(list[i], out r)) continue;
                int midX = (r.Left + r.Right) / 2;
                if (midX >= bounds.Left && midX < bounds.Right) return list[i];
            }
            return IntPtr.Zero;
        }

        /// <summary>指定屏任务栏当前是否露出（自动隐藏滑下时返回 false）。</summary>
        public static bool Visible(Screen screen)
        {
            try
            {
                IntPtr h = HandleFor(screen);
                if (h == IntPtr.Zero || !IsWindowVisible(h)) return false;
                Native.RECT r;
                if (!GetWindowRect(h, out r)) return false;
                Rectangle bounds = screen.Bounds;
                return r.Top < bounds.Bottom - 2 && r.Bottom > bounds.Top + 2;
            }
            catch { return false; }
        }

        /// <summary>主屏任务栏当前是否露出。</summary>
        public static bool Visible()
        {
            return Visible(Screen.PrimaryScreen);
        }

        public static int VisibleHeight(Screen screen)
        {
            try
            {
                IntPtr h = HandleFor(screen);
                if (h == IntPtr.Zero) return 0;
                Native.RECT r;
                if (!GetWindowRect(h, out r)) return 0;
                int hh = r.Bottom - r.Top;
                return hh > 0 && hh < 200 ? hh : 0;
            }
            catch { return 0; }
        }

        public static int VisibleHeight()
        {
            return VisibleHeight(Screen.PrimaryScreen);
        }

        /// <summary>指定屏任务栏是否真正从该屏边缘露出（自动隐藏缩回 2px 时返回 false）。</summary>
        public static bool Revealed(Screen screen)
        {
            try
            {
                IntPtr h = HandleFor(screen);
                if (h == IntPtr.Zero || !IsWindowVisible(h)) return false;
                Native.RECT r;
                if (!GetWindowRect(h, out r)) return false;
                Rectangle bounds = screen.Bounds;
                bool bottomDocked = r.Bottom >= bounds.Bottom - 2;
                return bottomDocked && r.Top <= bounds.Bottom - 6;
            }
            catch { return false; }
        }

        /// <summary>主屏任务栏是否真正露出。</summary>
        public static bool Revealed()
        {
            return Revealed(Screen.PrimaryScreen);
        }

        public static IntPtr Handle()
        {
            return HandleFor(Screen.PrimaryScreen);
        }

        /// <summary>返回主屏、副屏任务栏的句柄与 rect，供启动/重排日志核对。</summary>
        public static string Describe()
        {
            StringBuilder sb = new StringBuilder();
            List<IntPtr> list = Handles();
            for (int i = 0; i < list.Count; i++)
            {
                Native.RECT r;
                GetWindowRect(list[i], out r);
                if (sb.Length > 0) sb.Append("; ");
                sb.Append(ClassOf(list[i]).Replace("Shell_", ""));
                sb.Append("=").Append(r.Left).Append(",").Append(r.Top).Append(",").Append(r.Right).Append(",").Append(r.Bottom);
            }
            return sb.ToString();
        }

        public static int HideAll()
        {
            List<IntPtr> list = Handles();
            for (int i = 0; i < list.Count; i++) ShowWindow(list[i], 0);
            return list.Count;
        }

        public static int ShowAll()
        {
            List<IntPtr> list = Handles();
            for (int i = 0; i < list.Count; i++) ShowWindow(list[i], 5);
            return list.Count;
        }

        /// <summary>任一屏任务栏当前可见；用于完全隐藏的守护重试。</summary>
        public static bool AnyVisible()
        {
            Screen[] screens = Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
                if (Visible(screens[i])) return true;
            return false;
        }
    }
}
