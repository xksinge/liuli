// Liuli · 桌面图标系统开关：只写当前用户的 HideDesktopIcons，不创建/删除任何快捷方式。
using Microsoft.Win32;
using System;

namespace Liuli
{
    static class DesktopState
    {
        // 五个系统图标的 CLSID；1=隐藏，0=显示。
        public static readonly string[] ShellClsids =
        {
            "{20D04FE0-3AEA-1069-A2D8-08002B30309D}", // 此电脑
            "{645FF040-5081-101B-9F08-00AA002F954E}", // 回收站
            "{59031a47-3f72-44a7-89c5-5595fe6b30ee}", // 用户文件夹
            "{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}", // 网络
            "{5399E370-B05B-11D1-9F4B-00C04FD91972}"  // 控制面板
        };

        public static readonly string[] ShellNames =
        {
            "此电脑", "回收站", "用户文件夹", "网络", "控制面板"
        };

        const string NewStart = @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel";
        const string Classic = @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\ClassicStartMenu";

        public static bool ApplyShellIcons(ShellConfig c)
        {
            if (c == null) return false;
            bool ok = true;
            ok &= Write(NewStart, ShellClsids[0], c.HideShellThisPc);
            ok &= Write(NewStart, ShellClsids[1], c.HideShellRecycle);
            ok &= Write(NewStart, ShellClsids[2], c.HideShellUserFiles);
            ok &= Write(NewStart, ShellClsids[3], c.HideShellNetwork);
            ok &= Write(NewStart, ShellClsids[4], c.HideShellControlPanel);
            // Win10 的经典开始菜单也会读这一份；两处一起写，避免切换开始菜单样式后失效。
            ok &= Write(Classic, ShellClsids[0], c.HideShellThisPc);
            ok &= Write(Classic, ShellClsids[1], c.HideShellRecycle);
            ok &= Write(Classic, ShellClsids[2], c.HideShellUserFiles);
            ok &= Write(Classic, ShellClsids[3], c.HideShellNetwork);
            ok &= Write(Classic, ShellClsids[4], c.HideShellControlPanel);
            return ok;
        }

        static bool Write(string path, string clsid, bool hidden)
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(path))
                {
                    if (k == null) return false;
                    k.SetValue(clsid, hidden ? 1 : 0, RegistryValueKind.DWord);
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn("icons", "写桌面图标开关失败 " + ex.Message);
                return false;
            }
        }

        /// <summary>通知资源管理器刷新桌面图标；不重启 explorer。</summary>
        public static void Refresh()
        {
            try { Native.SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero); }
            catch { }
        }
    }
}
