// Liuli · 图像处理：盒式模糊（三遍近似高斯）、缩放、提色。一次性烘焙用，不进绘制帧。
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Liuli
{
    static class ImageFx
    {
        public static Bitmap Downscale(Bitmap src, int w, int h)
        {
            Bitmap dst = new Bitmap(Math.Max(1, w), Math.Max(1, h), PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(dst))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                using (ImageAttributes ia = new ImageAttributes())
                {
                    ia.SetWrapMode(WrapMode.TileFlipXY);
                    g.DrawImage(src, new Rectangle(0, 0, dst.Width, dst.Height), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, ia);
                }
            }
            return dst;
        }

        public static Bitmap Cover(Bitmap src, int w, int h)
        {
            Bitmap dst = new Bitmap(Math.Max(1, w), Math.Max(1, h), PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(dst))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                float scale = Math.Max((float)w / src.Width, (float)h / src.Height);
                float sw = src.Width * scale, sh = src.Height * scale;
                using (ImageAttributes ia = new ImageAttributes())
                {
                    ia.SetWrapMode(WrapMode.TileFlipXY);
                    g.DrawImage(src, new Rectangle((int)((w - sw) / 2f), (int)((h - sh) / 2f), (int)sw, (int)sh),
                        0, 0, src.Width, src.Height, GraphicsUnit.Pixel, ia);
                }
            }
            return dst;
        }

        /// <summary>三遍盒滤 ≈ 高斯；radius 以像素计。就地返回新图。</summary>
        public static Bitmap Blur(Bitmap src, int radius, int passes)
        {
            if (radius <= 0) return Clone(src);
            int w = src.Width, h = src.Height;
            int n = w * h;
            int[] buf = new int[n];
            int[] tmp = new int[n];
            BitmapData bd = null;
            try
            {
                bd = src.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppPArgb);
                Marshal.Copy(bd.Scan0, buf, 0, n);
                for (int p = 0; p < passes; p++)
                {
                    BlurLine(buf, tmp, w, h, radius, true);
                    BlurLine(tmp, buf, w, h, radius, false);
                }
                Marshal.Copy(buf, 0, bd.Scan0, n);
            }
            finally
            {
                if (bd != null) src.UnlockBits(bd);
            }
            return src;
        }

        static void BlurLine(int[] src, int[] dst, int w, int h, int r, bool horizontal)
        {
            int len = horizontal ? w : h;
            int lines = horizontal ? h : w;
            if (r < 1) r = 1;
            int div = r * 2 + 1;
            for (int line = 0; line < lines; line++)
            {
                int a = 0, rr = 0, gg = 0, bb = 0;
                // 初始窗口（边缘按 clamp 采样，保证角落不偏暗）
                for (int k = -r; k <= r; k++)
                {
                    int idx = Idx(line, Clamp(k, 0, len - 1), w, horizontal);
                    int c = src[idx];
                    a += (c >> 24) & 0xFF; rr += (c >> 16) & 0xFF; gg += (c >> 8) & 0xFF; bb += c & 0xFF;
                }
                for (int x = 0; x < len; x++)
                {
                    int idxOut = Idx(line, x, w, horizontal);
                    dst[idxOut] = ((a / div) << 24) | ((rr / div) << 16) | ((gg / div) << 8) | (bb / div);
                    int remove = src[Idx(line, Clamp(x - r, 0, len - 1), w, horizontal)];
                    int add = src[Idx(line, Clamp(x + r + 1, 0, len - 1), w, horizontal)];
                    a += ((add >> 24) & 0xFF) - ((remove >> 24) & 0xFF);
                    rr += ((add >> 16) & 0xFF) - ((remove >> 16) & 0xFF);
                    gg += ((add >> 8) & 0xFF) - ((remove >> 8) & 0xFF);
                    bb += (add & 0xFF) - (remove & 0xFF);
                }
            }
        }

        static int Idx(int line, int pos, int w, bool horizontal)
        {
            return horizontal ? line * w + pos : pos * w + line;
        }

        static int Clamp(int v, int lo, int hi) { return v < lo ? lo : (v > hi ? hi : v); }

        public static Bitmap Clone(Bitmap src)
        {
            return src.Clone(new Rectangle(0, 0, src.Width, src.Height), PixelFormat.Format32bppPArgb);
        }

        /// <summary>轻微去饱和 + 压暗，让玻璃下的壁纸更“雾”。</summary>
        public static void Frost(Bitmap bmp, float saturation, float brightness, int lift)
        {
            BitmapData bd = null;
            try
            {
                bd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppPArgb);
                int n = bmp.Width * bmp.Height;
                int[] px = new int[n];
                Marshal.Copy(bd.Scan0, px, 0, n);
                for (int i = 0; i < n; i++)
                {
                    int c = px[i];
                    int a = (c >> 24) & 0xFF, r = (c >> 16) & 0xFF, g = (c >> 8) & 0xFF, b = c & 0xFF;
                    int lum = (299 * r + 587 * g + 114 * b) / 1000;
                    r = (int)(lum + (r - lum) * saturation);
                    g = (int)(lum + (g - lum) * saturation);
                    b = (int)(lum + (b - lum) * saturation);
                    r = (int)((r * brightness) + lift);
                    g = (int)((g * brightness) + lift);
                    b = (int)((b * brightness) + lift);
                    if (r > 255) r = 255; if (r < 0) r = 0;
                    if (g > 255) g = 255; if (g < 0) g = 0;
                    if (b > 255) b = 255; if (b < 0) b = 0;
                    px[i] = (a << 24) | (r << 16) | (g << 8) | b;
                }
                Marshal.Copy(px, 0, bd.Scan0, n);
            }
            finally
            {
                if (bd != null) bmp.UnlockBits(bd);
            }
        }

        public static Color Average(Bitmap bmp)
        {
            try
            {
                long r = 0, g = 0, b = 0, n = 0;
                BitmapData bd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
                try
                {
                    int stride = bd.Stride;
                    byte[] row = new byte[stride];
                    for (int y = 0; y < bmp.Height; y += 2)
                    {
                        Marshal.Copy(bd.Scan0 + y * stride, row, 0, stride);
                        for (int x = 0; x < bmp.Width; x += 2)
                        {
                            int off = x * 4;
                            b += row[off]; g += row[off + 1]; r += row[off + 2]; n++;
                        }
                    }
                }
                finally { bmp.UnlockBits(bd); }
                if (n == 0) return Color.Gray;
                return Color.FromArgb((int)(r / n), (int)(g / n), (int)(b / n));
            }
            catch { return Color.Gray; }
        }
    }
}
