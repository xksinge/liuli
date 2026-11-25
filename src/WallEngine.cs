// Liuli · 壁纸引擎：
//   图片模式 = 走 Windows 自己的壁纸通道（零常驻开销，退出时还原原壁纸）；
//   视频模式 = 把系统媒体控件挂进桌面壁纸层（GPU 解码，闲置自动暂停）；不含解码器时自动退回图片模式。
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Liuli
{
    static class WallEngine
    {
        static bool imageChrome;          // 是否由我们改过系统壁纸
        static string origPath = "";
        static string origStyle = "10";
        static int origTile;
        static long lastTick;

        public static void Apply()
        {
            string mode = App.Ws.Set.WallMode;
            string path = App.Ws.Set.WallPath;
            Stop();

            if (mode == "image" && File.Exists(path))
            {
                RememberOriginal();
                if (Wallpaper.Set(path, 10, 0))
                {
                    imageChrome = true;
                    Log.Info("wall", "图片壁纸已设置 " + Path.GetFileName(path));
                }
                else ToastAfter("换成图片壁纸失败，先沿用系统壁纸");
            }
            else if (mode == "video" && File.Exists(path))
            {
                if (VideoWall.Start(path, App.Ws.Set.WallMute))
                {
                    Log.Info("wall", "视频壁纸已启动 " + Path.GetFileName(path));
                }
                else
                {
                    App.Ws.Set.WallMode = "system";
                    App.Ws.Set.WallPath = "";
                    App.Save();
                    ToastAfter("这台机器没有可用的视频解码组件，已退回系统壁纸");
                }
            }
        }

        static void ToastAfter(string s)
        {
            App.UI(delegate { App.Toast(s); });
        }

        public static void Tick()
        {
            long now = Environment.TickCount;
            if (now - lastTick < 700) return;
            lastTick = now;
            VideoWall.Tick();
            if (imageChrome && App.Ws.Set.WallMode != "image") Stop();
        }

        public static void Stop()
        {
            VideoWall.Stop();
            if (imageChrome)
            {
                RestoreOriginal();
                imageChrome = false;
            }
        }

        static void RememberOriginal()
        {
            if (imageChrome) return;
            origPath = Wallpaper.CurrentPath();
            origStyle = Wallpaper.GetStyleRaw(out origTile);
            try
            {
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\Liuli"))
                {
                    if (k != null)
                    {
                        k.SetValue("origWall", origPath);
                        k.SetValue("origStyle", origStyle);
                        k.SetValue("origTile", origTile.ToString());
                    }
                }
            }
            catch { }
        }

        static void RestoreOriginal()
        {
            try
            {
                string p = origPath;
                string st = origStyle;
                int tile = origTile;
                if (p.Length == 0 || !File.Exists(p))
                {
                    using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Liuli"))
                    {
                        if (k != null)
                        {
                            p = (k.GetValue("origWall") as string) ?? "";
                            st = (k.GetValue("origStyle") as string) ?? "10";
                            int.TryParse((k.GetValue("origTile") as string) ?? "0", out tile);
                        }
                    }
                }
                if (p.Length > 0 && File.Exists(p))
                {
                    int style = 10;
                    int.TryParse(st, out style);
                    Wallpaper.Set(p, style, tile);
                }
            }
            catch (Exception ex) { Log.Warn("wall", "还原壁纸失败 " + ex.Message); }
        }
    }
}
