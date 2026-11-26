// Liuli · 入口：单实例、DPI、异常兜底、启动/退出。
using System;
using System.Drawing;
using System.IO;
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
            if (args.Length > 0 && string.Equals(args[0], "--selftest", StringComparison.OrdinalIgnoreCase))
                return SelfTest();
            if (args.Length > 0 && string.Equals(args[0], "--preview", StringComparison.OrdinalIgnoreCase))
                return V2Preview.Run(args);
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

        /// <summary>无界面自检：只验证 shell.json、默认 Dock 与任务栏状态，不启动窗口。</summary>
        static int SelfTest()
        {
            try
            {
                ShellConfig c = ShellConfig.Load();
                Console.WriteLine("shell.json=" + ShellConfig.FilePath);
                Console.WriteLine("dockOn=" + c.DockOn + " dockAutoHide=" + c.DockAutoHide + " dockPreset=" + c.DockPreset + " islandOn=" + c.IslandOn + " taskbarAutoHide=" + c.TaskbarAutoHide);
                c.NormalizeIsland();
                Console.WriteLine("island.width=" + c.IslandWidth + " height=" + c.IslandHeight + " top=" + c.IslandTop + " animMs=" + c.IslandAnimMs + " quick=" + c.IslandQuick + " weather=" + c.IslandWeather + " stats=" + c.IslandStats);
                Console.WriteLine("panels=" + c.Panels.Count);
                for (int i = 0; i < c.Panels.Count; i++)
                {
                    PanelConfig p = c.Panels[i];
                    Console.WriteLine("  panel." + (i + 1) + " content=" + p.Content + " x=" + p.X + " y=" + p.Y +
                        " w=" + p.W + " h=" + p.H + " topMost=" + p.TopMostPanel);
                }
                Console.WriteLine("themes=" + c.Themes.Count + " active=" + c.ActiveTheme);
                Console.WriteLine("collectorOn=" + c.CollectorOn + " autoScan=" + c.CollectorAutoScan +
                    " items=" + c.CollectorItems.Count + " group=" + c.CollectorGroup +
                    " x=" + c.CollectorX + " y=" + c.CollectorY + " w=" + c.CollectorW + " h=" + c.CollectorH);
                Console.WriteLine("clock24=" + c.Clock24 + " clockSeconds=" + c.ClockSeconds);
                Console.WriteLine("autoStart=" + c.AutoStart + " run=" + AutoStart.ReadValue());
                Console.WriteLine("accent=" + c.AccentMode + " preset=" + c.AccentPreset + " custom=" + c.AccentCustom);
                for (int i = 0; i < c.Themes.Count; i++)
                {
                    ThemeSnapshot t = c.Themes[i];
                    Console.WriteLine("  theme." + (i + 1) + " name=" + t.Name + " glass=" + t.GlassPct + " blur=" + t.BlurPct +
                        " radius=" + t.Radius + " accent=" + t.AccentMode + "/" + t.AccentPreset + " panels=" + t.Panels.Count + " wallpaper=" + t.WallpaperPath);
                }
                Console.WriteLine("dockItems=" + c.DockItems.Count);
                for (int i = 0; i < c.DockItems.Count; i++)
                {
                    DockItem it = c.DockItems[i];
                    Console.WriteLine("  " + (i + 1) + ". " + it.Kind + " " + it.Id + " " + it.DisplayName + " proc=" + string.Join("|", it.ProcessNames()));
                }
                Console.WriteLine("taskbar.autoHide=" + Taskbar.AutoHide() + " visible=" + Taskbar.Visible() + " height=" + Taskbar.VisibleHeight());
                Bitmap probe = Icons.Get(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"), 256);
                Console.WriteLine("iconProbe=explorer " + (probe == null ? "null" : probe.Width + "x" + probe.Height));
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("selftest failed: " + ex);
                return 2;
            }
        }
    }
}
