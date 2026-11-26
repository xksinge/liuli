// Liuli · Win32 互操作。所有 P/Invoke 集中在这里，业务代码不直接调 user32。
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace Liuli
{
    static class Native
    {
        // ---------- 结构 ----------
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int X, Y; public POINT(int x, int y) { X = x; Y = y; } }

        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE { public int cx, cy; public SIZE(int x, int y) { cx = x; cy = y; } }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BLENDFUNCTION { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        public struct ACCENTPOLICY { public int AccentState, AccentFlags, GradientColor, AnimationId; }

        [StructLayout(LayoutKind.Sequential)]
        public struct WCADATA { public int Attribute; public IntPtr Data; public uint SizeOfData; }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys, ullAvailPhys, ullTotalPageFile, ullAvailPageFile;
            public ulong ullTotalVirtual, ullAvailVirtual, ullAvailExtendedVirtual;
        }

        // ---------- 常量 ----------
        public const int WS_EX_LAYERED = 0x80000;
        public const int WS_EX_TOOLWINDOW = 0x80;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WS_EX_TOPMOST = 0x8;
        public const int WS_EX_TRANSPARENT = 0x20;
        public const int ULW_ALPHA = 2;
        public const byte AC_SRC_OVER = 0, AC_SRC_ALPHA = 1;
        public const int WCA_ACCENT_POLICY = 19;
        public const int ACCENT_DISABLED = 0, ACCENT_GRADIENT = 1, ACCENT_TRANSPARENT_GRADIENT = 2, ACCENT_BLURBEHIND = 3, ACCENT_ACRYLIC = 4;

        public const int WM_SETTINGCHANGE = 0x001A;
        public const int WM_THEMECHANGED = 0x031A;
        public const int WM_DPICHANGED = 0x02E0;
        public const int WM_DROPFILES = 0x0233;
        public const int WM_MOUSEMOVE = 0x0200;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONUP = 0x0202;
        public const int WM_LBUTTONDBLCLK = 0x0203;
        public const int WM_RBUTTONUP = 0x0205;
        public const int WM_MOUSEWHEEL = 0x020A;
        public const int WM_MOUSELEAVE = 0x02A3;
        public const int WM_NCHITTEST = 0x0084;
        public const int WM_ACTIVATEAPP = 0x001C;
        public const int WM_HOTKEY = 0x0312;
        public const int WM_COMMAND = 0x0111;
        public const int HTTRANSPARENT = -1;
        public const int GWL_EXSTYLE = -20;

        public const uint SWP_NOSIZE = 0x0001, SWP_NOMOVE = 0x0002, SWP_NOZORDER = 0x0004, SWP_NOACTIVATE = 0x0010;
        public static readonly IntPtr HWND_BOTTOM = (IntPtr)1;
        public static readonly IntPtr HWND_TOPMOST = (IntPtr)(-1);

        public const int SHGFI_ICON = 0x100, SHGFI_LARGEICON = 0x0, SHGFI_SMALLICON = 0x1, SHGFI_USEFILEATTRIBUTES = 0x10, SHGFI_DISPLAYNAME = 0x200, SHGFI_TYPENAME = 0x400;
        public const uint FILE_ATTRIBUTE_NORMAL = 0x80;

        // ---------- user32 ----------
        [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll")] public static extern IntPtr CreateCompatibleDC(IntPtr hDC);
        [DllImport("gdi32.dll")] public static extern bool DeleteDC(IntPtr hDC);
        [DllImport("gdi32.dll")] public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
        [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr hObject);
        [DllImport("gdi32.dll")] public static extern bool BitBlt(IntPtr hDst, int x, int y, int w, int h, IntPtr hSrc, int x1, int y1, int rop);

        [DllImport("user32.dll")]
        public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);

        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT p);
        [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr h, int idx);
        [DllImport("user32.dll")] public static extern int SetWindowLong(IntPtr h, int idx, int val);
        [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(POINT p);
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string cls, string title);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string title);

        public delegate bool EnumWinProc(IntPtr h, IntPtr l);
        [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWinProc cb, IntPtr l);
        [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr parent, EnumWinProc cb, IntPtr l);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessageTimeout(IntPtr h, int msg, IntPtr w, IntPtr l, int flags, int timeout, out IntPtr result);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetClassName(IntPtr h, StringBuilder sb, int max);

        [DllImport("user32.dll")] public static extern int RegisterWindowMessage(string name);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool SystemParametersInfo(int action, int param, string text, int winIni);

        [DllImport("user32.dll")] public static extern IntPtr SetParent(IntPtr child, IntPtr parent);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool PostMessage(IntPtr h, int msg, IntPtr w, IntPtr l);

        public const int HWND_BROADCAST = 0xFFFF;

        [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr h, int id, uint mods, uint vk);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h, int id);

        [DllImport("user32.dll")] public static extern bool DragAcceptFiles(IntPtr h, bool accept);
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)] public static extern uint DragQueryFile(IntPtr drop, uint index, StringBuilder file, uint len);
        [DllImport("shell32.dll")] public static extern void DragFinish(IntPtr drop);

        [DllImport("user32.dll")] public static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WCADATA data);

        [DllImport("user32.dll")] public static extern IntPtr MonitorFromPoint(POINT pt, int flags);
        [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr h);
        [DllImport("user32.dll")] public static extern uint GetDpiForSystem();
        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")] public static extern IntPtr SetProcessDpiAwarenessContext(IntPtr ctx);
        [DllImport("shcore.dll")] public static extern int SetProcessDpiAwareness(int value);
        [DllImport("shcore.dll")] public static extern int GetDpiForMonitor(IntPtr hmonitor, int type, out uint dpiX, out uint dpiY);

        [DllImport("user32.dll")] public static extern bool SystemParametersInfo(int action, int param, StringBuilder value, int winIni);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr OpenClipboard(IntPtr h);
        [DllImport("user32.dll")] public static extern bool CloseClipboard();
        [DllImport("user32.dll")] public static extern IntPtr GetClipboardData(uint fmt);
        [DllImport("user32.dll")] public static extern bool EmptyClipboard();
        [DllImport("user32.dll")] public static extern IntPtr SetClipboardData(uint fmt, IntPtr data);
        [DllImport("kernel32.dll")] public static extern IntPtr GlobalAlloc(uint flags, IntPtr size);
        [DllImport("kernel32.dll")] public static extern IntPtr GlobalLock(IntPtr h);
        [DllImport("kernel32.dll")] public static extern bool GlobalUnlock(IntPtr h);
        [DllImport("kernel32.dll")] public static extern bool GlobalFree(IntPtr h);

        // ---------- shell32 ----------
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SHGetFileInfo(string path, uint attrs, ref SHFILEINFO info, uint cb, uint flags);
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr ShellExecute(IntPtr hwnd, string op, string file, string args, string dir, int show);
        [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr hIcon);

        // ---------- kernel32 ----------
        [DllImport("kernel32.dll")] public static extern bool GetSystemTimes(out long idle, out long kernel, out long user);
        [DllImport("kernel32.dll")] public static extern void GlobalMemoryStatusEx(ref MEMORYSTATUSEX status);

        // ---------- 封装 ----------

        public static void EnablePerMonitorDpi()
        {
            try { SetProcessDpiAwarenessContext(new IntPtr(-4)); return; } catch { }
            try { SetProcessDpiAwareness(2); return; } catch { }
            try { SetProcessDPIAware(); } catch { }
        }

        public static int DpiOf(IntPtr hwnd) { try { return (int)GetDpiForWindow(hwnd); } catch { return 96; } }

        public static void ApplyAcrylic(IntPtr hwnd, bool on, int tintArgb)
        {
            // tintArgb 用 0xAABBGGRR（DWM 的布局与 Color.FromArgb 的 AARRGGBB 相反）
            Native.ACCENTPOLICY policy = new Native.ACCENTPOLICY();
            policy.AccentState = on ? Native.ACCENT_ACRYLIC : Native.ACCENT_DISABLED;
            policy.AccentFlags = on ? 2 : 0;
            policy.GradientColor = on ? tintArgb : 0;
            policy.AnimationId = 0;
            Native.WCADATA data = new Native.WCADATA();
            data.Attribute = Native.WCA_ACCENT_POLICY;
            data.SizeOfData = (uint)Marshal.SizeOf(policy);
            data.Data = Marshal.AllocHGlobal((int)data.SizeOfData);
            try
            {
                Marshal.StructureToPtr(policy, data.Data, false);
                Native.SetWindowCompositionAttribute(hwnd, ref data);
            }
            catch { }
            finally { Marshal.FreeHGlobal(data.Data); }
        }

        public static void KeepAtBottom(IntPtr hwnd)
        {
            SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        public static void KeepAtTop(IntPtr hwnd)
        {
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        public static void ShellOpen(string path)
        {
            try { ShellExecute(IntPtr.Zero, "open", path, null, null, 5); }
            catch (Exception ex) { Log.Warn("shell open failed: " + path + " " + ex.Message); }
        }

        public static void ShellOpenFolder(string path)
        {
            try
            {
                if (System.IO.File.Exists(path)) ShellExecute(IntPtr.Zero, "explore", "/select,\"" + path + "\"", null, null, 5);
                else ShellExecute(IntPtr.Zero, "open", path, null, null, 5);
            }
            catch { }
        }

        public static string DesktopIconStatePath()
        {
            string defView = null;
            IntPtr prog = FindWindow("Progman", null);
            if (prog != IntPtr.Zero)
            {
                IntPtr child = FindWindowEx(prog, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (child != IntPtr.Zero) defView = "progman";
            }
            if (defView == null)
            {
                IntPtr worker = IntPtr.Zero;
                while (true)
                {
                    worker = FindWindowEx(IntPtr.Zero, worker, "WorkerW", null);
                    if (worker == IntPtr.Zero) break;
                    IntPtr child = FindWindowEx(worker, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (child != IntPtr.Zero) { defView = "worker"; break; }
                }
            }
            return defView;
        }

        /// <summary>桌面图标显隐：0x7402 是资源管理器“显示桌面图标”的命令 ID。</summary>
        /// <remarks>
        /// Win11 26200 上 FindWindow("Progman") 会返回 0，但 EnumWindows 能枚举到，所以先枚举再找子窗；
        /// SysListView32 是 DefView 的孙辈，FindWindowEx 只查直接子窗，必须用 EnumChildWindows 递归找。
        /// </remarks>
        public static bool ToggleDesktopIcons(out bool nowVisible)
        {
            nowVisible = true;
            try
            {
                IntPtr defView = DesktopDefView();
                if (defView == IntPtr.Zero) return false;
                IntPtr list = DesktopIconList(defView);
                bool before = list != IntPtr.Zero && IsWindowVisible(list);
                SendMessage(defView, WM_COMMAND, new IntPtr(0x7402), IntPtr.Zero);
                System.Threading.Thread.Sleep(200);
                list = DesktopIconList(defView);
                if (list != IntPtr.Zero) nowVisible = IsWindowVisible(list);
                else nowVisible = !before;
                return true;
            }
            catch { return false; }
        }

        /// <summary>探测桌面图标当前是否可见（读不到就按可见处理，避免误隐藏）。</summary>
        public static bool DesktopIconsVisible()
        {
            try
            {
                IntPtr defView = DesktopDefView();
                if (defView == IntPtr.Zero) return true;
                IntPtr list = DesktopIconList(defView);
                if (list == IntPtr.Zero) return true;
                return IsWindowVisible(list);
            }
            catch { return true; }
        }

        /// <summary>找到承载桌面图标的 SHELLDLL_DefView：先枚举 Progman / WorkerW，再退回 FindWindow。</summary>
        public static IntPtr DesktopDefView()
        {
            IntPtr progDv = IntPtr.Zero, workerDv = IntPtr.Zero;
            try
            {
                EnumWindows(delegate (IntPtr h, IntPtr l)
                {
                    string c = ClassOf(h);
                    if (c != "Progman" && c != "WorkerW") return true;
                    IntPtr dv = FindWindowEx(h, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (dv == IntPtr.Zero || DesktopIconList(dv) == IntPtr.Zero) return true;
                    // 有些机器上 WorkerW 也挂着 DefView（壁纸宿主），优先用 Progman 下那份真桌面
                    if (c == "Progman") progDv = dv; else workerDv = dv;
                    return true;
                }, IntPtr.Zero);
            }
            catch { }
            if (progDv != IntPtr.Zero) return progDv;
            if (workerDv != IntPtr.Zero) return workerDv;
            IntPtr prog = FindWindow("Progman", null);
            return prog == IntPtr.Zero ? IntPtr.Zero : FindWindowEx(prog, IntPtr.Zero, "SHELLDLL_DefView", null);
        }

        static IntPtr DesktopIconList(IntPtr defView)
        {
            IntPtr list = IntPtr.Zero;
            try
            {
                EnumChildWindows(defView, delegate (IntPtr h, IntPtr l)
                {
                    if (ClassOf(h) == "SysListView32") { list = h; return false; }
                    return true;
                }, IntPtr.Zero);
            }
            catch { }
            return list;
        }

        static string ClassOf(IntPtr h)
        {
            try
            {
                StringBuilder sb = new StringBuilder(128);
                GetClassName(h, sb, sb.Capacity);
                return sb.ToString();
            }
            catch { return ""; }
        }

        /// <summary>读取当前壁纸层窗口句柄（用于动态壁纸时取帧；读不到返回 0）。</summary>
        public static IntPtr WallpaperLayerWindow()
        {
            try
            {
                IntPtr prog = FindWindow("Progman", null);
                IntPtr defView = IntPtr.Zero;
                if (prog != IntPtr.Zero) defView = FindWindowEx(prog, IntPtr.Zero, "SHELLDLL_DefView", null);
                IntPtr host = prog;
                if (defView == IntPtr.Zero)
                {
                    IntPtr worker = IntPtr.Zero;
                    while (true)
                    {
                        worker = FindWindowEx(IntPtr.Zero, worker, "WorkerW", null);
                        if (worker == IntPtr.Zero) break;
                        IntPtr child = FindWindowEx(worker, IntPtr.Zero, "SHELLDLL_DefView", null);
                        if (child != IntPtr.Zero) { defView = child; host = worker; break; }
                    }
                }
                if (defView == IntPtr.Zero) return IntPtr.Zero;
                IntPtr wall = FindWindowEx(IntPtr.Zero, host, "WorkerW", null);
                return wall;
            }
            catch { return IntPtr.Zero; }
        }

        // ---------- 分层窗口推送 ----------
        public static void PushLayered(IntPtr hwnd, Bitmap canvas, Point screenPos, byte constantAlpha)
        {
            IntPtr screenDc = GetDC(IntPtr.Zero);
            IntPtr memDc = CreateCompatibleDC(screenDc);
            IntPtr hBitmap = IntPtr.Zero, old = IntPtr.Zero;
            try
            {
                hBitmap = canvas.GetHbitmap(Color.FromArgb(0));
                old = SelectObject(memDc, hBitmap);
                POINT dest = new POINT(screenPos.X, screenPos.Y);
                SIZE size = new SIZE(canvas.Width, canvas.Height);
                POINT src = new POINT(0, 0);
                BLENDFUNCTION blend = new BLENDFUNCTION();
                blend.BlendOp = AC_SRC_OVER;
                blend.BlendFlags = 0;
                blend.SourceConstantAlpha = constantAlpha;
                blend.AlphaFormat = AC_SRC_ALPHA;
                UpdateLayeredWindow(hwnd, screenDc, ref dest, ref size, memDc, ref src, 0, ref blend, ULW_ALPHA);
            }
            finally
            {
                ReleaseDC(IntPtr.Zero, screenDc);
                if (hBitmap != IntPtr.Zero) { SelectObject(memDc, old); DeleteObject(hBitmap); }
                DeleteDC(memDc);
            }
        }

        public static string ClassPathOf(IntPtr hwnd)
        {
            try
            {
                StringBuilder sb = new StringBuilder(128);
                GetClassName(hwnd, sb, 128);
                return sb.ToString();
            }
            catch { return ""; }
        }
    }
}
