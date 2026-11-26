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
                dock.PreviewReveal();
                dock.PreviewHide();
                Console.WriteLine("dockHidden top=" + dock.Top + " screenBottom=" + Screen.PrimaryScreen.Bounds.Bottom);
                dock.PreviewReveal();
                dock.Render();
                Console.WriteLine("dockRunning=" + dock.RunningNamesPublic());

                IslandForm island = new IslandForm();
                IntPtr ih = island.Handle;
                island.ExpandForPreview();

                string dir = Path.Combine(Paths.Base, "..\\docs\\screens\\v2");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, "m1-preview.png");
                SaveShot(dock, island, file);

                dock.PreviewPress(4);
                island.PreviewPressBtn(2);
                string pressedFile = Path.Combine(dir, "m1-preview-pressed.png");
                SaveShot(dock, island, pressedFile);

                dock.Dispose();
                island.Dispose();
                string presetFile = SavePresetStrip(c, dir);
                Console.WriteLine("preview=" + file + " " + new FileInfo(file).Length + "B");
                Console.WriteLine("preview=" + pressedFile + " " + new FileInfo(pressedFile).Length + "B");
                Console.WriteLine("preview=" + presetFile + " " + new FileInfo(presetFile).Length + "B");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("preview failed: " + ex);
                return 2;
            }
        }

        static string SavePresetStrip(ShellConfig c, string dir)
        {
            string[] presets = { "small", "standard", "large" };
            string[] labels = { "小", "标准", "大" };
            Bitmap[] snaps = new Bitmap[3];
            DockForm[] forms = new DockForm[3];
            int maxW = 0, maxH = 0;
            for (int i = 0; i < 3; i++)
            {
                c.DockPreset = presets[i];
                DockForm d = new DockForm();
                IntPtr h = d.Handle;
                d.PreviewReveal();
                d.Render();
                forms[i] = d;
                snaps[i] = d.Snapshot();
                if (snaps[i] != null)
                {
                    maxW = Math.Max(maxW, snaps[i].Width);
                    maxH = Math.Max(maxH, snaps[i].Height);
                }
                Console.WriteLine("dockPreset " + presets[i] + "=" + (snaps[i] == null ? "null" : snaps[i].Width + "x" + snaps[i].Height));
            }
            c.DockPreset = "standard";

            int pad = 24, labelH = 24;
            int width = maxW + pad * 2;
            int height = pad + 3 * (maxH + labelH + pad);
            Bitmap shot = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(shot))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(0, 0, width, height),
                    Util.Rgb(28, 38, 56), Util.Rgb(10, 14, 22), 90f))
                    g.FillRectangle(b, new Rectangle(0, 0, width, height));
                Font font = new Font("Microsoft YaHei UI", 15f, FontStyle.Bold, GraphicsUnit.Pixel);
                using (StringFormat sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Center;
                    using (SolidBrush text = new SolidBrush(Color.FromArgb(225, 255, 255, 255)))
                    {
                        int y = pad;
                        for (int i = 0; i < 3; i++)
                        {
                            g.DrawString(labels[i], font, text, new RectangleF(0, y, width, labelH), sf);
                            if (snaps[i] != null) g.DrawImage(snaps[i], (width - snaps[i].Width) / 2, y + labelH, snaps[i].Width, snaps[i].Height);
                            y += maxH + labelH + pad;
                        }
                    }
                }
                font.Dispose();
            }
            string file = Path.Combine(dir, "m2-dock-presets.png");
            shot.Save(file, ImageFormat.Png);
            shot.Dispose();
            for (int i = 0; i < 3; i++)
            {
                if (forms[i] != null) forms[i].Dispose();
                if (snaps[i] != null) snaps[i].Dispose();
            }
            return file;
        }

        static void SaveShot(DockForm dock, IslandForm island, string file)
        {
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
            shot.Save(file, ImageFormat.Png);
            shot.Dispose();
        }
    }
}
