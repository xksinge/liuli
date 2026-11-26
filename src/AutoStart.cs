// Liuli v2 · 当前用户开机自启：只写 HKCU Run，不写 HKLM。
using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Liuli
{
    static class AutoStart
    {
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string ValueName = "Liuli";

        public static string Command()
        {
            return "\"" + Application.ExecutablePath + "\"";
        }

        public static string ReadValue()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey, false))
                {
                    if (k == null) return "";
                    object v = k.GetValue(ValueName);
                    return v == null ? "" : v.ToString();
                }
            }
            catch { return ""; }
        }

        public static bool Apply(bool enabled)
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (k == null)
                    {
                        Log.Warn("autostart", "无法打开 HKCU Run");
                        return false;
                    }
                    if (enabled)
                    {
                        k.SetValue(ValueName, Command(), RegistryValueKind.String);
                        Log.Info("autostart", "已写入 " + Command());
                    }
                    else
                    {
                        k.DeleteValue(ValueName, false);
                        Log.Info("autostart", "已删除当前用户启动项");
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Warn("autostart", "写入失败：" + ex.Message);
                return false;
            }
        }
    }
}
