// Liuli v2 · Dock 图标：系统入口用统一矢量绘制，普通应用走 Shell 高清图标缓存。
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Liuli
{
    static class DockGlyphs
    {
        public static void Draw(Graphics g, DockItem item, Rectangle r, float s)
        {
            if (item == null) return;
            if (item.IsSystem)
            {
                DrawSystem(g, item.Id, r, s);
                return;
            }
            if (item.Path != null && item.Path.StartsWith("ms-settings:", StringComparison.OrdinalIgnoreCase))
            {
                Theme.Glyph(g, Glyph.Gear, r, Theme.Accent, 1.25f * s);
                return;
            }
            if (item.Path != null && item.Path.StartsWith("ms-calculator:", StringComparison.OrdinalIgnoreCase))
            {
                DrawCalculator(g, r, s);
                return;
            }
            Icons.Draw(g, item.Path, r);
        }

        static void DrawSystem(Graphics g, string id, Rectangle r, float s)
        {
            int pad = Util.RoundI(3 * s);
            Rectangle box = new Rectangle(r.X + pad, r.Y + pad, r.Width - pad * 2, r.Height - pad * 2);
            Color main = Theme.TextMain;
            Color accent = Theme.Accent;
            using (Pen pen = new Pen(main, 1.35f * s))
            using (Pen soft = new Pen(Color.FromArgb(110, accent), 1f * s))
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(22, accent)))
            {
                pen.LineJoin = LineJoin.Round;
                if (id == "thispc")
                {
                    Rectangle screen = new Rectangle(box.X, box.Y + Util.RoundI(2 * s), box.Width, Util.RoundI(box.Height * 0.62f));
                    using (GraphicsPath p = Theme.Round(screen, Util.RoundI(3 * s)))
                    {
                        g.FillPath(fill, p);
                        g.DrawPath(pen, p);
                    }
                    using (Pen line = new Pen(Color.FromArgb(80, main), 1f * s))
                    {
                        g.DrawLine(line, screen.X + Util.RoundI(5 * s), screen.Bottom - Util.RoundI(5 * s),
                            screen.Right - Util.RoundI(5 * s), screen.Bottom - Util.RoundI(5 * s));
                    }
                    int standY = screen.Bottom + Util.RoundI(2 * s);
                    g.DrawLine(pen, box.X + box.Width / 2, standY, box.X + box.Width / 2, standY + Util.RoundI(5 * s));
                    g.DrawLine(pen, box.X + box.Width / 2 - Util.RoundI(6 * s), box.Bottom - Util.RoundI(1 * s),
                        box.X + box.Width / 2 + Util.RoundI(6 * s), box.Bottom - Util.RoundI(1 * s));
                }
                else if (id == "recycle")
                {
                    int lidY = box.Y + Util.RoundI(5 * s);
                    int left = box.X + Util.RoundI(5 * s), right = box.Right - Util.RoundI(5 * s);
                    g.DrawLine(pen, left, lidY, right, lidY);
                    g.DrawLine(pen, box.X + box.Width / 2 - Util.RoundI(3 * s), lidY - Util.RoundI(3 * s),
                        box.X + box.Width / 2 + Util.RoundI(3 * s), lidY - Util.RoundI(3 * s));
                    using (GraphicsPath p = new GraphicsPath())
                    {
                        p.AddLine(left + Util.RoundI(1 * s), lidY + Util.RoundI(2 * s), right - Util.RoundI(1 * s), lidY + Util.RoundI(2 * s));
                        p.AddLine(right - Util.RoundI(1 * s), lidY + Util.RoundI(2 * s), right - Util.RoundI(4 * s), box.Bottom - Util.RoundI(1 * s));
                        p.AddLine(right - Util.RoundI(4 * s), box.Bottom - Util.RoundI(1 * s), left + Util.RoundI(4 * s), box.Bottom - Util.RoundI(1 * s));
                        p.AddLine(left + Util.RoundI(4 * s), box.Bottom - Util.RoundI(1 * s), left + Util.RoundI(1 * s), lidY + Util.RoundI(2 * s));
                        g.FillPath(fill, p);
                        g.DrawPath(pen, p);
                    }
                    g.DrawLine(soft, box.X + box.Width / 2 - Util.RoundI(3 * s), lidY + Util.RoundI(6 * s),
                        box.X + box.Width / 2 - Util.RoundI(4 * s), box.Bottom - Util.RoundI(4 * s));
                    g.DrawLine(soft, box.X + box.Width / 2 + Util.RoundI(3 * s), lidY + Util.RoundI(6 * s),
                        box.X + box.Width / 2 + Util.RoundI(4 * s), box.Bottom - Util.RoundI(4 * s));
                }
                else if (id == "userfiles")
                {
                    int top = box.Y + Util.RoundI(6 * s);
                    Rectangle folder = new Rectangle(box.X + Util.RoundI(1 * s), top, box.Width - Util.RoundI(2 * s), box.Height - Util.RoundI(8 * s));
                    using (GraphicsPath p = new GraphicsPath())
                    {
                        p.AddLine(folder.X, folder.Y + Util.RoundI(4 * s), folder.X + Util.RoundI(7 * s), folder.Y + Util.RoundI(4 * s));
                        p.AddLine(folder.X + Util.RoundI(7 * s), folder.Y + Util.RoundI(4 * s), folder.X + Util.RoundI(10 * s), folder.Y);
                        p.AddLine(folder.X + Util.RoundI(10 * s), folder.Y, folder.Right - Util.RoundI(3 * s), folder.Y);
                        p.AddLine(folder.Right - Util.RoundI(3 * s), folder.Y, folder.Right, folder.Y + Util.RoundI(3 * s));
                        p.AddLine(folder.Right, folder.Y + Util.RoundI(3 * s), folder.Right, folder.Bottom);
                        p.AddLine(folder.Right, folder.Bottom, folder.X, folder.Bottom);
                        p.AddLine(folder.X, folder.Bottom, folder.X, folder.Y + Util.RoundI(4 * s));
                        g.FillPath(fill, p);
                        g.DrawPath(pen, p);
                    }
                    int cx = box.X + box.Width / 2;
                    int cy = folder.Y + Util.RoundI(7 * s);
                    g.DrawEllipse(pen, cx - Util.RoundI(4 * s), cy, Util.RoundI(8 * s), Util.RoundI(8 * s));
                    using (GraphicsPath p = new GraphicsPath())
                    {
                        p.AddArc(cx - Util.RoundI(8 * s), cy + Util.RoundI(6 * s), Util.RoundI(16 * s), Util.RoundI(12 * s), 180, 180);
                        g.DrawPath(pen, p);
                    }
                }
                else if (id == "network")
                {
                    Rectangle globe = new Rectangle(box.X + Util.RoundI(3 * s), box.Y + Util.RoundI(3 * s), box.Width - Util.RoundI(6 * s), box.Height - Util.RoundI(6 * s));
                    g.DrawEllipse(pen, globe);
                    g.DrawEllipse(soft, globe.X + globe.Width / 2 - Util.RoundI(1 * s), globe.Y + Util.RoundI(2 * s),
                        Util.RoundI(2 * s), globe.Height - Util.RoundI(4 * s));
                    g.DrawArc(soft, globe.X + Util.RoundI(2 * s), globe.Y + Util.RoundI(5 * s),
                        globe.Width - Util.RoundI(4 * s), Util.RoundI(8 * s), 0, 180);
                    g.DrawArc(soft, globe.X + Util.RoundI(2 * s), globe.Bottom - Util.RoundI(13 * s),
                        globe.Width - Util.RoundI(4 * s), Util.RoundI(8 * s), 180, 180);
                }
                else
                {
                    Theme.Glyph(g, Glyph.Gear, box, Theme.Accent, 1.2f * s);
                }
            }
        }

        static void DrawCalculator(Graphics g, Rectangle r, float s)
        {
            int pad = Util.RoundI(4 * s);
            Rectangle box = new Rectangle(r.X + pad, r.Y + pad, r.Width - pad * 2, r.Height - pad * 2);
            using (GraphicsPath p = Theme.Round(box, Util.RoundI(4 * s)))
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(30, Theme.Accent)))
            using (Pen pen = new Pen(Theme.Accent, 1.2f * s))
            {
                g.FillPath(fill, p);
                g.DrawPath(pen, p);
            }
            Rectangle screen = new Rectangle(box.X + Util.RoundI(4 * s), box.Y + Util.RoundI(4 * s),
                box.Width - Util.RoundI(8 * s), Util.RoundI(7 * s));
            using (SolidBrush b = new SolidBrush(Color.FromArgb(70, Theme.TextMain))) g.FillRectangle(b, screen);
            int dot = Util.RoundI(3 * s);
            for (int yy = 0; yy < 3; yy++)
                for (int xx = 0; xx < 3; xx++)
                {
                    int x = box.X + Util.RoundI(5 * s) + xx * (dot + Util.RoundI(3 * s));
                    int y = box.Y + Util.RoundI(15 * s) + yy * (dot + Util.RoundI(3 * s));
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(150, Theme.TextMain))) g.FillEllipse(b, x, y, dot, dot);
                }
        }
    }
}
