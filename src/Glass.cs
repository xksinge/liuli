// Liuli · 玻璃材质：烘焙磨砂底 + 低透明色彩层 + 顶边高光 + 1px 描边 + 噪点。
// 观感三件套：同源圆角、顶部 1px 高光、统一描边透明度；所有窗口只走这里，不各画各的。
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Liuli
{
    static class Glass
    {
        public const int MarginBase = 16;      // 窗口外留白（投影区）

        public static int Margin { get { return Util.RoundI(MarginBase * Theme.S); } }

        public static Rectangle Body(Rectangle window)
        {
            int m = Margin;
            return new Rectangle(window.X + m, window.Y + m, Math.Max(1, window.Width - m * 2), Math.Max(1, window.Height - m * 2));
        }

        /// <summary>屏幕坐标里的玻璃主体（用于壁纸切片）。</summary>
        public static Rectangle ScreenBody(Board b)
        {
            int m = Margin;
            return new Rectangle(b.X + m, b.Y + m, Math.Max(1, b.W - m * 2), Math.Max(1, b.H - m * 2));
        }

        public static void Paint(Graphics g, Board b, Rectangle body, Color accent, bool hover, bool dragging, bool focused)
        {
            float s = Theme.S;
            float radius = Theme.Radius * s;
            int glass = Theme.GlassPct;
            bool light = Theme.Light;

            // 1) 外投影：悬浮时略深
            int shadowA = dragging ? 116 : (hover ? 96 : 78);
            Theme.DrawShadow(g, body, (int)radius, (int)(11 * s), shadowA);

            using (GraphicsPath path = Theme.Round(body, radius))
            {
                // 2) 磨砂底（实时玻璃模式下交给 DWM，自己不再画底）
                g.SetClip(path);
                if (!Theme.Live) Backdrop.Draw(g, Util.ToF(body), ScreenBody(b));
                else
                {
                    using (SolidBrush clear = new SolidBrush(Color.FromArgb(0))) g.FillPath(clear, path);
                }

                // 3) 色调层：整体薄纱 + 上下渐变，让文字有对比度又不糊住壁纸
                int veil = (int)(14 + glass * 0.56f);
                Color veilColor = light ? Color.FromArgb(veil, 255, 255, 255) : Color.FromArgb(veil, 10, 13, 22);
                using (SolidBrush vb = new SolidBrush(veilColor)) g.FillPath(vb, path);

                using (LinearGradientBrush top = new LinearGradientBrush(
                    new Rectangle(body.X, body.Y, body.Width, Math.Max(1, body.Height / 3)),
                    Color.FromArgb(light ? 40 : 26, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 90f))
                {
                    g.FillRectangle(top, new Rectangle(body.X, body.Y, body.Width, body.Height / 3));
                }
                using (LinearGradientBrush bottom = new LinearGradientBrush(
                    new Rectangle(body.X, body.Bottom - body.Height / 3, body.Width, Math.Max(1, body.Height / 3)),
                    Color.FromArgb(0, 0, 0, 0), Color.FromArgb(light ? 10 : 34, 0, 0, 0), 90f))
                {
                    g.FillRectangle(bottom, new Rectangle(body.X, body.Bottom - body.Height / 3, body.Width, body.Height / 3));
                }

                // 4) 强调色呼吸：左上角一团极淡的色光
                using (GraphicsPath glowPath = new GraphicsPath())
                {
                    float gw = body.Width * 1.05f, gh = body.Height * 0.95f;
                    glowPath.AddEllipse(body.X - gw * 0.35f, body.Y - gh * 0.55f, gw, gh);
                    using (PathGradientBrush pg = new PathGradientBrush(glowPath))
                    {
                        pg.CenterColor = Color.FromArgb((int)((light ? 26 : 34) * (0.4f + glass / 140f)), accent);
                        pg.SurroundColors = new Color[] { Color.FromArgb(0, accent) };
                        pg.CenterPoint = new PointF(body.X + body.Width * 0.16f, body.Y + body.Height * 0.10f);
                        g.FillPath(pg, glowPath);
                    }
                }

                // 5) 噪点：防止大面积渐变出现色带
                using (TextureBrush nb = new TextureBrush(Theme.NoiseTile(), WrapMode.Tile))
                {
                    nb.TranslateTransform(body.X, body.Y);
                    g.FillRectangle(nb, body);
                }
                g.ResetClip();

                // 6) 描边：上亮下暗的一圈，像玻璃棱
                using (LinearGradientBrush edge = new LinearGradientBrush(
                    new Rectangle(body.X, body.Y, body.Width, Math.Max(1, body.Height)),
                    light ? Color.FromArgb(150, 255, 255, 255) : Color.FromArgb(120, 255, 255, 255),
                    light ? Color.FromArgb(26, 0, 0, 0) : Color.FromArgb(20, 0, 0, 0), 90f))
                using (Pen pen = new Pen(edge, 1.25f * s))
                {
                    pen.LineJoin = LineJoin.Round;
                    g.DrawPath(pen, path);
                }
                if (focused)
                {
                    using (Pen pen = new Pen(Color.FromArgb(110, accent), 1.2f * s)) { g.DrawPath(pen, path); }
                }

                // 7) 顶边高光：只画圆角内的一小段
                RectangleF hr = new RectangleF(body.X + radius, body.Y + 1.1f * s, body.Width - radius * 2f, 1.6f * s);
                if (hr.Width > 2)
                {
                    using (LinearGradientBrush lb = new LinearGradientBrush(hr,
                        Color.FromArgb(0, 255, 255, 255), Color.FromArgb(76, 255, 255, 255), 0f))
                    using (SolidBrush b2 = new SolidBrush(Color.FromArgb(76, 255, 255, 255)))
                    {
                        g.FillRectangle(b2, hr.X, hr.Y, hr.Width * 0.5f, hr.Height);
                    }
                }

                // 8) 底边折射：一圈极淡的强调色
                RectangleF br = new RectangleF(body.X + radius, body.Bottom - 2.4f * s, body.Width - radius * 2f, 1.2f * s);
                if (br.Width > 2)
                {
                    using (SolidBrush b3 = new SolidBrush(Color.FromArgb(46, accent))) g.FillRectangle(b3, br);
                }

                // 9) 左上折射光斑（液态玻璃的高光点）
                using (GraphicsPath hp = new GraphicsPath())
                {
                    float hw = body.Width * 0.55f, hh = body.Height * 0.34f;
                    hp.AddEllipse(body.X + body.Width * 0.06f, body.Y - hh * 0.42f, hw, hh);
                    using (PathGradientBrush pg = new PathGradientBrush(hp))
                    {
                        pg.CenterColor = Color.FromArgb(light ? 34 : 46, 255, 255, 255);
                        pg.SurroundColors = new Color[] { Color.FromArgb(0, 255, 255, 255) };
                        pg.CenterPoint = new PointF(body.X + body.Width * 0.22f, body.Y + body.Height * 0.02f);
                        g.FillPath(pg, hp);
                    }
                }
            }
        }

        /// <summary>圆形玻璃按钮；hover 时底色浮起、图标提亮。</summary>
        public static void Button(Graphics g, Rectangle r, Glyph glyph, bool hover, bool active, Color accent)
        {
            float s = Theme.S;
            int radius = r.Height / 2;
            if (active)
            {
                using (GraphicsPath p = Theme.Round(r, radius))
                using (SolidBrush b = new SolidBrush(Color.FromArgb(70, accent))) g.FillPath(b, p);
            }
            else if (hover)
            {
                using (GraphicsPath p = Theme.Round(r, radius))
                using (SolidBrush b = new SolidBrush(Theme.Hover)) g.FillPath(b, p);
                Theme.StrokeRound(g, Util.ToF(r), radius, Color.FromArgb(40, 255, 255, 255), 1f);
            }
            Color icon = hover ? Theme.TextMain : Theme.TextSub;
            if (active) icon = accent;
            Theme.Glyph(g, glyph, Util.Inflate(r, -(int)(r.Width * 0.28f)), icon, 1.6f * s);
        }

        /// <summary>胶囊标签（模块名 / 计数）。</summary>
        public static void Chip(Graphics g, Rectangle r, string text, Color color)
        {
            using (GraphicsPath p = Theme.Round(r, r.Height / 2))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(26, 255, 255, 255)))
                g.FillPath(b, p);
            Theme.TextMiddle(g, text, Theme.Font(Theme.FxTiny, FontStyle.Regular), color, r, StringAlignment.Center);
        }

        public static void HoverRing(Graphics g, Rectangle body, Color accent)
        {
            float s = Theme.S;
            using (GraphicsPath p = Theme.Round(Util.Inflate(body, 2), Theme.Radius * s))
            using (Pen pen = new Pen(Color.FromArgb(34, accent), 1f * s))
            {
                pen.LineJoin = LineJoin.Round;
                g.DrawPath(pen, p);
            }
        }
    }
}
