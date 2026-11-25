// Liuli · 视觉 token 与绘制原语：圆角、玻璃边、投影、噪点、矢量小图标、文字排版。
// 所有窗口共用这一套；窗口里不许再自创圆角/描边数值。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Liuli
{
    enum Glyph { Plus, Gear, Broom, Close, Chevron, Pin, Note, Todo, Folder, Code, Pulse, Clock, Copy, More, Check, Image, Video, Restore, Save, ArrowUp, ArrowDown, Dot }

    static class Theme
    {
        // ---------- 运行时 token ----------
        public static int Radius = 22;          // 看板圆角
        public static int RadiusSm = 13;        // 按钮/条目
        public static int GlassPct = 100;       // 玻璃浓度
        public static int BlurPct = 62;         // 磨砂强度
        public static bool Live = false;        // DWM 实时玻璃（GPU）
        public static bool Light = false;       // 壁纸偏亮时自动切浅色文字
        public static bool FollowWallpaperTone = true;
        public static Color Accent = Util.Rgb(126, 178, 255);
        public static Color WallMean = Util.Rgb(32, 40, 56);
        public static int S = 1;                // 界面缩放（按窗口 DPI）

        // 主色板（随 Light 计算）
        public static Color TextMain = Util.Rgb(243, 247, 255);
        public static Color TextSub = Util.Rgb(183, 196, 214);
        public static Color TextFaint = Util.Rgb(136, 152, 172);
        public static Color Hair = Color.FromArgb(26, 255, 255, 255);
        public static Color EdgeHi = Color.FromArgb(92, 255, 255, 255);
        public static Color EdgeLo = Color.FromArgb(24, 255, 255, 255);
        public static Color Hover = Color.FromArgb(28, 255, 255, 255);
        public static Color Press = Color.FromArgb(46, 255, 255, 255);
        public static Color Shadow = Color.FromArgb(255, 0, 0, 0);
        public static Color Danger = Util.Rgb(255, 122, 122);

        public const int AnimFast = 140;
        public const int AnimStd = 220;
        public const int AnimSlow = 340;

        // 字号（px，按 S 缩放）
        public const float FxTitle = 14f;
        public const float FxSub = 10f;
        public const float FxBody = 11.5f;
        public const float FxSmall = 9.5f;
        public const float FxTiny = 8.5f;
        public const float FxClock = 44f;

        public const string Family = "Microsoft YaHei UI";
        public const string FamilyMono = "Consolas";

        public static void ApplyTone()
        {
            if (Light)
            {
                TextMain = Util.Rgb(18, 24, 34);
                TextSub = Util.Rgb(74, 86, 104);
                TextFaint = Util.Rgb(112, 124, 142);
                Hair = Color.FromArgb(20, 0, 0, 0);
                EdgeHi = Color.FromArgb(120, 255, 255, 255);
                EdgeLo = Color.FromArgb(22, 0, 0, 0);
                Hover = Color.FromArgb(26, 0, 0, 0);
                Press = Color.FromArgb(42, 0, 0, 0);
            }
            else
            {
                TextMain = Util.Rgb(243, 247, 255);
                TextSub = Util.Rgb(183, 196, 214);
                TextFaint = Util.Rgb(136, 152, 172);
                Hair = Color.FromArgb(26, 255, 255, 255);
                EdgeHi = Color.FromArgb(92, 255, 255, 255);
                EdgeLo = Color.FromArgb(22, 255, 255, 255);
                Hover = Color.FromArgb(28, 255, 255, 255);
                Press = Color.FromArgb(46, 255, 255, 255);
            }
        }

        /// <summary>由壁纸平均亮度推文字明暗：阈值带 8 的回滞，避免抖。</summary>
        public static void PickTone(Color mean, out bool light, out Color accent)
        {
            int lum = Util.Luma(mean);
            float h, s, l;
            Util.RgbToHsl(mean, out h, out s, out l);
            if (s < 0.16f) { h = 214f; s = 0.52f; }
            // 阈值按“磨砂后的底色”来定：亮底加白纱配深字，暗底加暗纱配白字
            if (lum >= 150) light = true;
            else if (lum <= 132) light = false;
            else light = Light;
            accent = light ? Util.Hsl(h, 0.62f, 0.42f) : Util.Hsl(h, 0.56f, 0.70f);
        }

        // ---------- 字体 ----------
        static readonly Dictionary<string, Font> fonts = new Dictionary<string, Font>();
        static readonly object fontGate = new object();

        public static Font Font(float px, FontStyle style)
        {
            return Get(Family, px * S, style);
        }

        public static Font Mono(float px, FontStyle style)
        {
            return Get(FamilyMono, px * S, style);
        }

        static Font Get(string family, float px, FontStyle style)
        {
            string key = family + "|" + (int)px + "|" + (int)style;
            lock (fontGate)
            {
                Font f;
                if (fonts.TryGetValue(key, out f)) return f;
                try { f = new Font(family, px, style, GraphicsUnit.Pixel); }
                catch { f = new Font("Microsoft YaHei UI", px, style, GraphicsUnit.Pixel); }
                fonts[key] = f;
                return f;
            }
        }

        // ---------- 路径 ----------
        public static GraphicsPath Round(Rectangle r, int radius)
        {
            return Round(Util.ToF(r), radius);
        }

        public static GraphicsPath Round(RectangleF r, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            if (r.Width <= 0 || r.Height <= 0) return path;
            float d = Math.Min(radius * 2f, Math.Min(r.Width, r.Height));
            if (d <= 0.5f) { path.AddRectangle(r); return path; }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void FillRound(Graphics g, Rectangle r, int radius, Color c)
        {
            using (GraphicsPath p = Round(r, radius)) g.FillPath(GetBrush(c), p);
        }

        public static void StrokeRound(Graphics g, RectangleF r, float radius, Color c, float width)
        {
            using (GraphicsPath p = Round(r, radius))
            using (Pen pen = new Pen(c, width))
            {
                pen.LineJoin = LineJoin.Round;
                g.DrawPath(pen, p);
            }
        }

        static readonly Dictionary<int, SolidBrush> brushes = new Dictionary<int, SolidBrush>();

        public static SolidBrush GetBrush(Color c)
        {
            // 只缓存高频色，避免长期持有大量刷子
            return new SolidBrush(c);
        }

        // ---------- 阴影 ----------
        /// <summary>柔和外阴影：多层低透明度圆角描边叠出渐变，不占缓存。</summary>
        public static void DrawShadow(Graphics g, Rectangle body, int radius, int spread, int alpha)
        {
            int steps = 8;
            for (int i = steps; i >= 1; i--)
            {
                float t = (float)i / steps;
                Rectangle r = Util.Inflate(body, i * spread / steps);
                int a = (int)(alpha * (1f - t) * 0.42f) + 4;
                using (GraphicsPath p = Round(r, radius + i))
                using (Pen pen = new Pen(Color.FromArgb(a, 0, 0, 0), 2f))
                {
                    pen.LineJoin = LineJoin.Round;
                    g.DrawPath(pen, p);
                }
            }
        }

        // ---------- 噪点 ----------
        static Bitmap noise;
        static readonly object noiseGate = new object();

        public static Bitmap NoiseTile()
        {
            lock (noiseGate)
            {
                if (noise != null) return noise;
                noise = new Bitmap(128, 128, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                Random rnd = new Random(11);
                using (Graphics g = Graphics.FromImage(noise))
                {
                    g.Clear(Color.Transparent);
                    for (int i = 0; i < 2600; i++)
                    {
                        int x = rnd.Next(128), y = rnd.Next(128);
                        int a = rnd.Next(2, 7);
                        noise.SetPixel(x, y, Color.FromArgb(a, 255, 255, 255));
                    }
                }
                return noise;
            }
        }

        // ---------- 文字 ----------
        public static StringFormat SF(StringAlignment align, bool wrap, bool ellipsis)
        {
            StringFormat sf = new StringFormat(StringFormat.GenericTypographic.Clone() as StringFormat);
            sf.Alignment = align;
            sf.LineAlignment = StringAlignment.Near;
            sf.FormatFlags = StringFormatFlags.NoWrap;
            if (!wrap) sf.FormatFlags |= StringFormatFlags.NoWrap;
            if (ellipsis) sf.Trimming = StringTrimming.EllipsisCharacter;
            return sf;
        }

        public static void Text(Graphics g, string s, Font f, Color c, Rectangle r, StringAlignment align, bool ellipsis)
        {
            if (string.IsNullOrEmpty(s)) return;
            using (StringFormat sf = SF(align, false, ellipsis))
            {
                sf.FormatFlags |= StringFormatFlags.NoWrap;
                using (SolidBrush b = new SolidBrush(c))
                    g.DrawString(s, f, b, Util.ToF(r), sf);
            }
        }

        public static void TextMiddle(Graphics g, string s, Font f, Color c, Rectangle r, StringAlignment align)
        {
            if (string.IsNullOrEmpty(s)) return;
            using (StringFormat sf = SF(align, false, true))
            {
                sf.LineAlignment = StringAlignment.Center;
                using (SolidBrush b = new SolidBrush(c))
                    g.DrawString(s, f, b, Util.ToF(r), sf);
            }
        }

        // ---------- 矢量图标 ----------
        public static void Glyph(Graphics g, Glyph kind, Rectangle box, Color color, float weight)
        {
            float x = box.X, y = box.Y, w = box.Width, h = box.Height;
            float cx = x + w / 2f, cy = y + h / 2f;
            float u = Math.Min(w, h);
            using (Pen pen = new Pen(color, weight))
            {
                pen.LineJoin = LineJoin.Round;
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                switch (kind)
                {
                    case Liuli.Glyph.Plus:
                        g.DrawLine(pen, cx - u * 0.22f, cy, cx + u * 0.22f, cy);
                        g.DrawLine(pen, cx, cy - u * 0.22f, cx, cy + u * 0.22f);
                        break;
                    case Liuli.Glyph.Close:
                        g.DrawLine(pen, cx - u * 0.18f, cy - u * 0.18f, cx + u * 0.18f, cy + u * 0.18f);
                        g.DrawLine(pen, cx + u * 0.18f, cy - u * 0.18f, cx - u * 0.18f, cy + u * 0.18f);
                        break;
                    case Liuli.Glyph.Chevron:
                        g.DrawLine(pen, cx - u * 0.16f, cy - u * 0.07f, cx, cy + u * 0.10f);
                        g.DrawLine(pen, cx + u * 0.16f, cy - u * 0.07f, cx, cy + u * 0.10f);
                        break;
                    case Liuli.Glyph.Pin:
                        g.DrawEllipse(pen, cx - u * 0.12f, cy - u * 0.20f, u * 0.24f, u * 0.24f);
                        g.DrawLine(pen, cx, cy - u * 0.04f, cx, cy + u * 0.22f);
                        break;
                    case Liuli.Glyph.More:
                        using (SolidBrush b = new SolidBrush(color))
                        {
                            float d = u * 0.075f;
                            g.FillEllipse(b, cx - u * 0.18f - d / 2, cy - d / 2, d, d);
                            g.FillEllipse(b, cx - d / 2, cy - d / 2, d, d);
                            g.FillEllipse(b, cx + u * 0.18f - d / 2, cy - d / 2, d, d);
                        }
                        break;
                    case Liuli.Glyph.Check:
                        g.DrawLine(pen, cx - u * 0.20f, cy, cx - u * 0.04f, cy + u * 0.15f);
                        g.DrawLine(pen, cx - u * 0.04f, cy + u * 0.15f, cx + u * 0.21f, cy - u * 0.16f);
                        break;
                    case Liuli.Glyph.Note:
                        g.DrawRectangle(pen, cx - u * 0.20f, cy - u * 0.22f, u * 0.40f, u * 0.44f);
                        g.DrawLine(pen, cx - u * 0.12f, cy - u * 0.08f, cx + u * 0.12f, cy - u * 0.08f);
                        g.DrawLine(pen, cx - u * 0.12f, cy + u * 0.02f, cx + u * 0.12f, cy + u * 0.02f);
                        g.DrawLine(pen, cx - u * 0.12f, cy + u * 0.12f, cx + u * 0.04f, cy + u * 0.12f);
                        break;
                    case Liuli.Glyph.Todo:
                        for (int i = 0; i < 3; i++)
                        {
                            float yy = cy - u * 0.15f + i * u * 0.15f;
                            g.DrawRectangle(pen, cx - u * 0.20f, yy - u * 0.055f, u * 0.11f, u * 0.11f);
                            g.DrawLine(pen, cx - u * 0.02f, yy, cx + u * 0.20f, yy);
                        }
                        break;
                    case Liuli.Glyph.Folder:
                        g.DrawLine(pen, cx - u * 0.22f, cy - u * 0.10f, cx - u * 0.10f, cy - u * 0.10f);
                        g.DrawLine(pen, cx - u * 0.10f, cy - u * 0.10f, cx - u * 0.06f, cy - u * 0.16f);
                        g.DrawLine(pen, cx - u * 0.06f, cy - u * 0.16f, cx + u * 0.10f, cy - u * 0.16f);
                        g.DrawLine(pen, cx + u * 0.10f, cy - u * 0.16f, cx + u * 0.14f, cy - u * 0.10f);
                        g.DrawLine(pen, cx + u * 0.14f, cy - u * 0.10f, cx + u * 0.22f, cy - u * 0.10f);
                        g.DrawLine(pen, cx + u * 0.22f, cy - u * 0.10f, cx + u * 0.22f, cy + u * 0.16f);
                        g.DrawLine(pen, cx + u * 0.22f, cy + u * 0.16f, cx - u * 0.22f, cy + u * 0.16f);
                        g.DrawLine(pen, cx - u * 0.22f, cy + u * 0.16f, cx - u * 0.22f, cy - u * 0.10f);
                        break;
                    case Liuli.Glyph.Code:
                        g.DrawLine(pen, cx - u * 0.10f, cy - u * 0.14f, cx - u * 0.22f, cy);
                        g.DrawLine(pen, cx - u * 0.22f, cy, cx - u * 0.10f, cy + u * 0.14f);
                        g.DrawLine(pen, cx + u * 0.10f, cy - u * 0.14f, cx + u * 0.22f, cy);
                        g.DrawLine(pen, cx + u * 0.22f, cy, cx + u * 0.10f, cy + u * 0.14f);
                        break;
                    case Liuli.Glyph.Pulse:
                        g.DrawLine(pen, cx - u * 0.24f, cy, cx - u * 0.12f, cy);
                        g.DrawLine(pen, cx - u * 0.12f, cy, cx - u * 0.05f, cy - u * 0.14f);
                        g.DrawLine(pen, cx - u * 0.05f, cy - u * 0.14f, cx + u * 0.04f, cy + u * 0.13f);
                        g.DrawLine(pen, cx + u * 0.04f, cy + u * 0.13f, cx + u * 0.10f, cy);
                        g.DrawLine(pen, cx + u * 0.10f, cy, cx + u * 0.24f, cy);
                        break;
                    case Liuli.Glyph.Clock:
                        g.DrawEllipse(pen, cx - u * 0.22f, cy - u * 0.22f, u * 0.44f, u * 0.44f);
                        g.DrawLine(pen, cx, cy, cx, cy - u * 0.13f);
                        g.DrawLine(pen, cx, cy, cx + u * 0.10f, cy + u * 0.05f);
                        break;
                    case Liuli.Glyph.Copy:
                        g.DrawRectangle(pen, cx - u * 0.18f, cy - u * 0.20f, u * 0.26f, u * 0.30f);
                        g.DrawRectangle(pen, cx - u * 0.06f, cy - u * 0.10f, u * 0.26f, u * 0.30f);
                        break;
                    case Liuli.Glyph.Broom:
                        g.DrawLine(pen, cx + u * 0.14f, cy - u * 0.22f, cx - u * 0.02f, cy + u * 0.02f);
                        g.DrawLine(pen, cx - u * 0.16f, cy + u * 0.04f, cx + u * 0.10f, cy + u * 0.04f);
                        g.DrawLine(pen, cx - u * 0.16f, cy + u * 0.04f, cx - u * 0.18f, cy + u * 0.18f);
                        g.DrawLine(pen, cx - u * 0.02f, cy + u * 0.04f, cx - u * 0.02f, cy + u * 0.18f);
                        g.DrawLine(pen, cx + u * 0.10f, cy + u * 0.04f, cx + u * 0.14f, cy + u * 0.18f);
                        break;
                    case Liuli.Glyph.Gear:
                        g.DrawEllipse(pen, cx - u * 0.10f, cy - u * 0.10f, u * 0.20f, u * 0.20f);
                        for (int i = 0; i < 8; i++)
                        {
                            float ang = (float)(Math.PI / 4 * i);
                            float x1 = cx + (float)Math.Cos(ang) * u * 0.14f;
                            float y1 = cy + (float)Math.Sin(ang) * u * 0.14f;
                            float x2 = cx + (float)Math.Cos(ang) * u * 0.24f;
                            float y2 = cy + (float)Math.Sin(ang) * u * 0.24f;
                            g.DrawLine(pen, x1, y1, x2, y2);
                        }
                        break;
                    case Liuli.Glyph.Image:
                        g.DrawRectangle(pen, cx - u * 0.22f, cy - u * 0.18f, u * 0.44f, u * 0.36f);
                        g.DrawLine(pen, cx - u * 0.22f, cy + u * 0.12f, cx - u * 0.06f, cy - u * 0.02f);
                        g.DrawLine(pen, cx - u * 0.06f, cy - u * 0.02f, cx + u * 0.06f, cy + u * 0.10f);
                        g.DrawLine(pen, cx + u * 0.06f, cy + u * 0.10f, cx + u * 0.22f, cy - u * 0.04f);
                        break;
                    case Liuli.Glyph.Video:
                        g.DrawRectangle(pen, cx - u * 0.22f, cy - u * 0.16f, u * 0.34f, u * 0.32f);
                        g.DrawLine(pen, cx + u * 0.12f, cy, cx + u * 0.22f, cy - u * 0.10f);
                        g.DrawLine(pen, cx + u * 0.12f, cy, cx + u * 0.22f, cy + u * 0.10f);
                        break;
                    case Liuli.Glyph.Restore:
                        g.DrawArc(pen, cx - u * 0.18f, cy - u * 0.18f, u * 0.36f, u * 0.36f, 30, 300);
                        g.DrawLine(pen, cx - u * 0.18f, cy - u * 0.06f, cx - u * 0.18f, cy - u * 0.22f);
                        g.DrawLine(pen, cx - u * 0.18f, cy - u * 0.22f, cx - u * 0.02f, cy - u * 0.22f);
                        break;
                    case Liuli.Glyph.Save:
                        g.DrawRectangle(pen, cx - u * 0.18f, cy - u * 0.18f, u * 0.36f, u * 0.36f);
                        g.DrawRectangle(pen, cx - u * 0.09f, cy - u * 0.18f, u * 0.18f, u * 0.12f);
                        g.DrawRectangle(pen, cx - u * 0.09f, cy + u * 0.02f, u * 0.18f, u * 0.16f);
                        break;
                    case Liuli.Glyph.ArrowUp:
                        g.DrawLine(pen, cx, cy + u * 0.16f, cx, cy - u * 0.16f);
                        g.DrawLine(pen, cx, cy - u * 0.16f, cx - u * 0.09f, cy - u * 0.06f);
                        g.DrawLine(pen, cx, cy - u * 0.16f, cx + u * 0.09f, cy - u * 0.06f);
                        break;
                    case Liuli.Glyph.Dot:
                        using (SolidBrush b = new SolidBrush(color)) g.FillEllipse(b, cx - u * 0.10f, cy - u * 0.10f, u * 0.20f, u * 0.20f);
                        break;
                }
            }
        }
    }
}
