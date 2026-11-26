// Liuli · 入口：单实例、DPI、异常兜底、启动/退出。
using System;
using System.Threading;
using System.Windows.Forms;

namespace Liuli
{
    static class Program
    {
        const string MutexName = "Liuli.Desktop.Shell.v2";

        [STAThread]
        static int Main(string[] args)
        {
            Paths.Ensure();
            Native.EnablePerMonitorDpi();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate (object s, ThreadExceptionEventArgs e)
            {
                Log.Error("crash", e.Exception.ToString());
                try { V2Shell.UI(delegate { V2Shell.Toast("出了点问题，已记到日志"); }); } catch { }
            };
            AppDomain.CurrentDomain.UnhandledException += delegate (object s, UnhandledExceptionEventArgs e)
            {
                Log.Error("crash", (e.ExceptionObject as Exception) != null ? e.ExceptionObject.ToString() : "未知异常");
            };

            bool first = true;
            Mutex mutex = new Mutex(true, MutexName, out first);
            if (!first)
            {
                int msg = Native.RegisterWindowMessage(V2Shell.ActivateMessageName);
                Native.PostMessage(new IntPtr(Native.HWND_BROADCAST), msg, IntPtr.Zero, IntPtr.Zero);
                return 0;
            }

            try
            {
                V2Shell.Boot();
                Application.Run(V2Shell.Host);
            }
            catch (Exception ex)
            {
                Log.Error("crash", ex.ToString());
                return 1;
            }
            finally
            {
                try { mutex.ReleaseMutex(); } catch { }
            }
            return 0;
        }
    }
}
