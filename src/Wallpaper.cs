// Liuli · 壁纸获取与渲染：只读壁纸文件/系统转码文件，不截屏（避免把窗口、图标、桌宠烤进玻璃）。
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Liuli
{
    static class Wallpaper
    {
        /// <summary>最近一次 Render 是否按虚拟屏连续（span）绘制；多屏时为 true。</summary>
        public static bool LastSpan;

        public static string CurrentPath()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop"))
                {
                    if (k != null)
                    {
                        string p = k.GetValue("WallPaper") as string;
                        if (!string.IsNullOrEmpty(p) && File.Exists(p)) return p;
                    }
                }
            }
            catch { }
            try
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder(1024);
                if (Native.SystemParametersInfo(0x0073, sb.Capacity, sb, 0))
                {
                    string p = sb.ToString();
                    if (File.Exists(p)) return p;
                }
            }
            catch { }
            string transcoded = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Microsoft\Windows\Themes\TranscodedWallpaper");
            if (File.Exists(transcoded)) return transcoded;
            return "";
        }

        public static int Style()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop"))
                {
                    if (k != null)
                    {
                        object s = k.GetValue("WallpaperStyle");
                        object t = k.GetValue("TileWallpaper");
                        int style = 0;
                        if (s != null) int.TryParse(Text(s), out style);
                        int tile = 0;
                        if (t != null) int.TryParse(Text(t), out tile);
                        if (tile == 1) return 100;      // 平铺
                        return style;                    // 0 居中 / 2 拉伸 / 6 适应 / 10 填充 / 22 跨屏
                    }
                }
            }
            catch { }
            return 10;
        }

        static string Text(object o) { return o == null ? "" : o.ToString(); }

        /// <summary>换系统壁纸（图片模式直接用 Windows 自己的壁纸通道，零常驻开销）。</summary>
        public static bool Set(string path, int style, int tile)
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
                {
                    if (k != null)
                    {
                        k.SetValue("WallpaperStyle", style.ToString());
                        k.SetValue("TileWallpaper", tile == 1 ? "1" : "0");
                    }
                }
                return Native.SystemParametersInfo(0x0014, 0, path, 0x01 | 0x02);
            }
            catch (Exception ex) { Log.Warn("wall", "设置壁纸失败 " + ex.Message); return false; }
        }

        public static string GetStyleRaw(out int tile)
        {
            tile = 0;
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop"))
                {
                    if (k == null) return "10";
                    object s = k.GetValue("WallpaperStyle");
                    object t = k.GetValue("TileWallpaper");
                    if (t != null) int.TryParse(Text(t), out tile);
                    return s == null ? "10" : Text(s);
                }
            }
            catch { }
            return "10";
        }

        public static Color DesktopColor()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Control Panel\Colors"))
                {
                    if (k != null)
                    {
                        string v = k.GetValue("Background") as string;
                        if (!string.IsNullOrEmpty(v))
                        {
                            string[] parts = v.Split(' ');
                            if (parts.Length >= 3)
                                return Util.Rgb(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
                        }
                    }
                }
            }
            catch { }
            return Util.Rgb(16, 20, 30);
        }

        public static string Signature()
        {
            string p = CurrentPath();
            string stamp = "";
            try { if (p.Length > 0) stamp = File.GetLastWriteTimeUtc(p).Ticks.ToString(); } catch { }
            string t = "";
            try
            {
                string tr = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Microsoft\Windows\Themes\TranscodedWallpaper");
                if (File.Exists(tr)) t = File.GetLastWriteTimeUtc(tr).Ticks.ToString();
            } catch { }
            Rectangle v = SystemInformation.VirtualScreen;
            return p + "|" + p.Length + "|" + stamp + "|" + t + "|" + Style() + "|" + v.Width + "x" + v.Height + "@" + v.X + "," + v.Y;
        }

        /// <summary>
        /// 把当前壁纸按系统样式渲染到指定尺寸（默认虚拟屏 1/4）。返回的图已铺满、不透明。
        /// </summary>
        public static Bitmap Render(Rectangle virtualBounds, float scale, string overridePath, Color fallback)
        {
            int w = Math.Max(2, (int)Math.Round(virtualBounds.Width * scale));
            int h = Math.Max(2, (int)Math.Round(virtualBounds.Height * scale));
            Bitmap bmp = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(fallback);
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;

                string path = overridePath;
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) path = CurrentPath();
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return bmp;

                using (Image img = LoadImage(path))
                {
                    if (img == null) return bmp;
                    int style = overridePath != null && overridePath.Length > 0 ? 10 : Style();
                    RectangleF vb = new RectangleF(0, 0, w, h);
                    // 多屏玻璃底必须按虚拟屏连续铺一张图，否则“填充/平铺”会在每块屏重新起算，
                    // 跨屏窗口的玻璃在拼接处出现重复纹理。单屏仍按系统样式。
                    Screen[] screens = Screen.AllScreens;
                    bool span = style == 22 || screens.Length > 1;
                    LastSpan = span;
                    if (span)
                    {
                        DrawSpan(g, img, vb);
                    }
                    else
                    {
                        for (int i = 0; i < screens.Length; i++)
                        {
                            Rectangle s = screens[i].Bounds;
                            RectangleF r = new RectangleF((s.X - virtualBounds.X) * scale, (s.Y - virtualBounds.Y) * scale,
                                s.Width * scale, s.Height * scale);
                            DrawStyle(g, img, r, style, scale);
                        }
                    }
                }
            }
            return bmp;
        }

        static Image LoadImage(string path)
        {
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    // 复制到内存流，避免壁纸文件被系统占用
                    byte[] buf = new byte[fs.Length];
                    fs.Read(buf, 0, buf.Length);
                    MemoryStream ms = new MemoryStream(buf);
                    return Image.FromStream(ms);
                }
            }
            catch (Exception ex)
            {
                Log.Warn("wall", "读取壁纸失败 " + path + " " + ex.Message);
                return null;
            }
        }

        static void DrawStyle(Graphics g, Image img, RectangleF r, int style, float scale)
        {
            float iw = img.Width * scale, ih = img.Height * scale;
            switch (style)
            {
                case 0: // 居中
                    g.DrawImage(img, new RectangleF(r.X + (r.Width - iw) / 2f, r.Y + (r.Height - ih) / 2f, iw, ih));
                    break;
                case 2: // 拉伸
                    g.DrawImage(img, r);
                    break;
                case 6: // 适应
                {
                    float k = Math.Min(r.Width / iw, r.Height / ih);
                    float dw = iw * k, dh = ih * k;
                    g.DrawImage(img, new RectangleF(r.X + (r.Width - dw) / 2f, r.Y + (r.Height - dh) / 2f, dw, dh));
                    break;
                }
                case 100: // 平铺
                    for (float y = r.Y; y < r.Bottom; y += ih)
                        for (float x = r.X; x < r.Right; x += iw)
                            g.DrawImage(img, new RectangleF(x, y, iw, ih));
                    break;
                default: // 填充（Cover）
                    DrawCover(g, img, r, scale);
                    break;
            }
        }

        static void DrawCover(Graphics g, Image img, RectangleF r, float scale)
        {
            float k = Math.Max(r.Width / (img.Width * scale), r.Height / (img.Height * scale));
            float dw = img.Width * scale * k, dh = img.Height * scale * k;
            g.DrawImage(img, new RectangleF(r.X + (r.Width - dw) / 2f, r.Y + (r.Height - dh) / 2f, dw, dh));
        }

        static void DrawSpan(Graphics g, Image img, RectangleF r)
        {
            float k = Math.Max(r.Width / (img.Width), r.Height / (img.Height));
            float dw = img.Width * k, dh = img.Height * k;
            g.DrawImage(img, new RectangleF(r.X + (r.Width - dw) / 2f, r.Y + (r.Height - dh) / 2f, dw, dh));
        }
    }
}
