// Liuli v2 · 面板内容：空容器 / 时钟 / 天气 / 系统读数，与面板容器解耦。
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Liuli
{
    static class PanelContent
    {
        public static void Paint(Graphics g, Rectangle body, string content, bool topMost, float scale)
        {
            int pad = Util.RoundI(18 * scale);
            int header = Util.RoundI(38 * scale);
            Rectangle head = new Rectangle(body.X + pad, body.Y + Util.RoundI(9 * scale),
                body.Width - pad * 2, Util.RoundI(24 * scale));
            string name = PanelConfig.ContentName(content);
            Theme.TextMiddle(g, name, Theme.Font(11f, FontStyle.Bold, scale), Theme.TextMain, head, StringAlignment.Near);
            Rectangle tag = new Rectangle(body.Right - pad - Util.RoundI(54 * scale), head.Y,
                Util.RoundI(54 * scale), head.Height);
            Theme.TextMiddle(g, topMost ? "置顶" : "置底", Theme.Font(9f, FontStyle.Regular, scale),
                Theme.TextFaint, tag, StringAlignment.Far);

            int lineY = body.Y + header;
            using (Pen pen = new Pen(Theme.Hair, 1f))
                g.DrawLine(pen, body.X + pad, lineY, body.Right - pad, lineY);

            Rectangle area = new Rectangle(body.X + pad, lineY + Util.RoundI(10 * scale),
                body.Width - pad * 2, body.Bottom - lineY - Util.RoundI(18 * scale));
            if (area.Width <= 4 || area.Height <= 4) return;

            string mode = PanelConfig.NormalizeContent(content);
            if (mode == "clock") PaintClock(g, area, scale);
            else if (mode == "weather") PaintWeather(g, area, scale);
            else if (mode == "stats") PaintStats(g, area, scale);
            else PaintEmpty(g, area, scale);
        }

        static void PaintEmpty(Graphics g, Rectangle area, float scale)
        {
            int d = Util.RoundI(48 * scale);
            Rectangle icon = new Rectangle(area.X + (area.Width - d) / 2,
                area.Y + Math.Max(0, (area.Height - d) / 2 - Util.RoundI(18 * scale)), d, d);
            if (icon.Bottom < area.Bottom)
            {
                using (SolidBrush b = new SolidBrush(Theme.Hover))
                using (GraphicsPath p = Theme.Round(icon, d / 2))
                    g.FillPath(b, p);
                Theme.Glyph(g, Glyph.Plus, new Rectangle(icon.X + d / 3, icon.Y + d / 3, d / 3, d / 3),
                    Theme.TextSub, 1.4f * scale);
            }
            Rectangle text = new Rectangle(area.X, icon.Bottom + Util.RoundI(8 * scale),
                area.Width, Util.RoundI(22 * scale));
            Theme.TextMiddle(g, "空容器", Theme.Font(10.5f, FontStyle.Regular, scale), Theme.TextSub, text, StringAlignment.Center);
            Rectangle hint = new Rectangle(area.X, text.Bottom + Util.RoundI(2 * scale),
                area.Width, Util.RoundI(18 * scale));
            Theme.TextMiddle(g, "右键切换内容", Theme.Font(9f, FontStyle.Regular, scale), Theme.TextFaint, hint, StringAlignment.Center);
        }

        static void PaintClock(Graphics g, Rectangle area, float scale)
        {
            if (area.Height < 80)
            {
                int small = Math.Max(16, Math.Min(Util.RoundI(30 * scale), area.Height - Util.RoundI(4 * scale)));
                Theme.TextMiddle(g, DateTime.Now.ToString("HH:mm"), Theme.Font(small, FontStyle.Bold, scale),
                    Theme.TextMain, area, StringAlignment.Near);
                return;
            }
            Theme.TextMiddle(g, "本地时间", Theme.Font(9f, FontStyle.Regular, scale), Theme.TextFaint,
                new Rectangle(area.X, area.Y, area.Width, Util.RoundI(18 * scale)), StringAlignment.Near);
            int big = Math.Max(18, Math.Min(Util.RoundI(42 * scale), area.Height - Util.RoundI(42 * scale)));
            Rectangle time = new Rectangle(area.X, area.Y + Util.RoundI(15 * scale), area.Width,
                Math.Min(area.Height - Util.RoundI(20 * scale), big + Util.RoundI(8 * scale)));
            Theme.TextMiddle(g, DateTime.Now.ToString("HH:mm"), Theme.Font(big, FontStyle.Bold, scale),
                Theme.TextMain, time, StringAlignment.Near);
            DateTime d = DateTime.Now;
            string[] wd = { "日", "一", "二", "三", "四", "五", "六" };
            Rectangle date = new Rectangle(area.X, time.Bottom - Util.RoundI(2 * scale), area.Width,
                Util.RoundI(20 * scale));
            Theme.TextMiddle(g, d.Month + "月" + d.Day + "日 星期" + wd[(int)d.DayOfWeek],
                Theme.Font(10.5f, FontStyle.Regular, scale), Theme.TextSub, date, StringAlignment.Near);
        }

        static void PaintWeather(Graphics g, Rectangle area, float scale)
        {
            WeatherNow w = V2Shell.Weather != null ? V2Shell.Weather.Now : new WeatherNow();
            string city = V2Shell.Weather != null ? V2Shell.Weather.City : "道孚";
            if (!w.Has)
            {
                Theme.TextMiddle(g, "天气离线", Theme.Font(10.5f, FontStyle.Regular, scale),
                    Theme.TextSub, area, StringAlignment.Near);
                return;
            }

            int iconSize = Math.Min(Util.RoundI(52 * scale), Math.Max(24, area.Height - Util.RoundI(30 * scale)));
            WeatherService.Glyph(g, w.Code, new Rectangle(area.X, area.Y + Util.RoundI(4 * scale), iconSize, iconSize),
                Theme.Accent);
            int textX = area.X + iconSize + Util.RoundI(12 * scale);
            int big = Math.Max(18, Math.Min(Util.RoundI(34 * scale), area.Height - Util.RoundI(44 * scale)));
            Rectangle temp = new Rectangle(textX, area.Y, area.Right - textX, Math.Min(area.Height, big + Util.RoundI(10 * scale)));
            Theme.TextMiddle(g, w.Temp + "°", Theme.Font(big, FontStyle.Bold, scale), Theme.TextMain, temp, StringAlignment.Near);
            Rectangle cond = new Rectangle(textX, temp.Bottom, area.Right - textX, Util.RoundI(18 * scale));
            Theme.TextMiddle(g, city + " · " + w.Cond, Theme.Font(10f, FontStyle.Regular, scale),
                Theme.TextSub, cond, StringAlignment.Near);
            Rectangle detail = new Rectangle(textX, cond.Bottom + Util.RoundI(2 * scale), area.Right - textX,
                Util.RoundI(18 * scale));
            if (detail.Bottom <= area.Bottom)
            {
                Theme.TextMiddle(g, "湿度 " + w.Humidity + "% · 风 " + w.Wind.ToString("0.0") + "m/s",
                    Theme.Font(9f, FontStyle.Regular, scale), Theme.TextFaint, detail, StringAlignment.Near);
            }
        }

        static void PaintStats(Graphics g, Rectangle area, float scale)
        {
            StatsNow st = V2Shell.Stats;
            int rowH = Math.Max(Util.RoundI(30 * scale), area.Height / 3);
            Stat(g, new Rectangle(area.X, area.Y, area.Width, rowH), "CPU", st.CpuText, st.Cpu / 100f, scale);
            Stat(g, new Rectangle(area.X, area.Y + rowH, area.Width, rowH), "内存", st.MemPct + "%", st.MemPct / 100f, scale);
            Stat(g, new Rectangle(area.X, area.Y + rowH * 2, area.Width, rowH), "磁盘", st.DiskPct + "%", st.DiskPct / 100f, scale);
        }

        static void Stat(Graphics g, Rectangle r, string label, string value, float pct, float scale)
        {
            if (pct < 0f) pct = 0f;
            if (pct > 1f) pct = 1f;
            int lineH = Util.RoundI(4 * scale);
            int barY = r.Y + Util.RoundI(21 * scale);
            Rectangle top = new Rectangle(r.X, r.Y, r.Width, Util.RoundI(18 * scale));
            Theme.TextMiddle(g, label, Theme.Font(9.5f, FontStyle.Regular, scale), Theme.TextSub, top, StringAlignment.Near);
            Theme.TextMiddle(g, value, Theme.Font(9.5f, FontStyle.Regular, scale), Theme.TextMain, top, StringAlignment.Far);
            int barW = r.Width;
            Rectangle track = new Rectangle(r.X, barY, barW, lineH);
            using (SolidBrush b = new SolidBrush(Theme.Hair))
            using (GraphicsPath p = Theme.Round(track, lineH / 2))
                g.FillPath(b, p);
            int fillW = Math.Max(0, (int)(barW * pct));
            if (fillW > 0)
            {
                Rectangle fill = new Rectangle(r.X, barY, fillW, lineH);
                using (SolidBrush b = new SolidBrush(Theme.Accent))
                using (GraphicsPath p = Theme.Round(fill, lineH / 2))
                    g.FillPath(b, p);
            }
        }
    }
}
