// Liuli v2 · 离屏预览：不显示窗口，把 Dock/灵动岛画到 PNG，供视觉评审与留档。
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Liuli
{
    static class V2Preview
    {
        public static int Run(string[] args)
        {
            try
            {
                ShellConfig c = ShellConfig.Load();
                V2Shell.Config = c;
                Theme.BlurPct = Util.Clamp(c.BlurPct, 10, 100);
                Theme.GlassPct = Util.Clamp(c.GlassPct, 20, 140);
                Backdrop.Start();
                c.ApplyTheme();
                V2Shell.Stats = SysStats.Sample();
                V2Shell.Weather = new WeatherService();
                V2Shell.Weather.LoadCache();

                DockForm dock = new DockForm();
                IntPtr dh = dock.Handle;
                dock.Render();

                IslandForm island = new IslandForm();
                IntPtr ih = island.Handle;
                island.ExpandForPreview();

                Rectangle screen = Screen.PrimaryScreen.Bounds;
                Bitmap shot = new Bitmap(screen.Width, screen.Height, PixelFormat.Format32bppPArgb);
                using (Graphics g = Graphics.FromImage(shot))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    if (Backdrop.Source != null) g.DrawImage(Backdrop.Source, screen);
                    else
                    {
                        using (LinearGradientBrush b = new LinearGradientBrush(screen, Util.Rgb(28, 38, 56), Util.Rgb(10, 14, 22), 90f))
                            g.FillRectangle(b, screen);
                    }

                    Bitmap bmp = dock.Snapshot();
                    if (bmp != null)
                    {
                        g.DrawImage(bmp, dock.Left, dock.Top, bmp.Width, bmp.Height);
                        bmp.Dispose();
                    }
                    bmp = island.Snapshot();
                    if (bmp != null)
                    {
                        g.DrawImage(bmp, island.Left, island.Top, bmp.Width, bmp.Height);
                        bmp.Dispose();
                    }
                }

                string dir = Path.Combine(Paths.Base, "..\\docs\\screens\\v2");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, "m1-preview.png");
                shot.Save(file, ImageFormat.Png);
                shot.Dispose();
                dock.Dispose();
                island.Dispose();
                Console.WriteLine("preview=" + file + " " + new FileInfo(file).Length + "B");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("preview failed: " + ex);
                return 2;
            }
        }
    }
}
