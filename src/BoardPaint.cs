// Liuli · 看板内容绘制：便签 / 待办 / 文件 / 代码 / 状态 / 时钟。
// 所有命中区在这里顺手登记，输入只查 hits，不再维护第二套坐标。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Liuli
{
    partial class BoardWindow
    {
        void PaintContent(Rectangle body, Color accent, float s)
        {
            int pad = Util.RoundI(14 * s);
            int headerH = Util.RoundI(46 * s);
            contentRect = new Rectangle(body.X + pad, body.Y + headerH,
                Math.Max(1, body.Width - pad * 2), Math.Max(1, body.Height - headerH - pad));

            using (Pen hair = new Pen(Theme.Hair, 1f))
                g.DrawLine(hair, body.X + pad, body.Y + headerH - 1, body.Right - pad, body.Y + headerH - 1);

            hits.Add(new Zone(contentRect, "content", 0));

            Rectangle clip = new Rectangle(body.X + 2, body.Y + headerH + 1, body.Width - 4, Math.Max(1, body.Bottom - body.Y - headerH - 2));
            GraphicsState st = g.Save();
            g.SetClip(clip);

            switch (B.Kind)
            {
                case BoardKind.Todo: PaintTodo(accent, s); break;
                case BoardKind.Files: PaintFiles(accent, s); break;
                case BoardKind.Code: PaintCode(accent, s); break;
                case BoardKind.Status: PaintStatus(accent, s); break;
                case BoardKind.Clock: PaintClock(accent, s); break;
                default: PaintNote(accent, s); break;
            }
            g.Restore(st);

            PaintScroll(accent, s);
        }

        // ---------- 便签 ----------
        void PaintNote(Color accent, float s)
        {
            Font f = Theme.Font(Theme.FxBody, FontStyle.Regular);
            int lineH = Util.RoundI(19.5f * s);
            int width = contentRect.Width - Util.RoundI(4 * s);
            string text = B.Text;
            if (string.IsNullOrEmpty(text) || text.Trim().Length == 0)
            {
                contentHeight = 0;
                DrawEmpty("双击这里开始写", "也可以把文件拖进来，路径会追加成文字", accent, s);
                return;
            }
            List<string> lines = Util.Wrap(g, text, f, width, 0);
            contentHeight = lines.Count * lineH + Util.RoundI(4 * s);
            using (SolidBrush b = new SolidBrush(Theme.TextMain))
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    int y = contentRect.Y + i * lineH - Util.RoundI(scrollPx);
                    if (y + lineH < contentRect.Y - 2 || y > contentRect.Bottom) continue;
                    g.DrawString(lines[i], f, b, contentRect.X, y);
                }
            }
        }

        void DrawEmpty(string main, string sub, Color accent, float s)
        {
            int cy = contentRect.Y + Math.Max(20, contentRect.Height / 2 - Util.RoundI(24 * s));
            Rectangle r1 = new Rectangle(contentRect.X, cy, contentRect.Width, Util.RoundI(22 * s));
            int ic = Util.RoundI(30 * s);
            using (GraphicsPath p = Theme.Round(new Rectangle(contentRect.X + contentRect.Width / 2 - ic / 2, cy - ic - Util.RoundI(6 * s), ic, ic), ic / 2))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(28, accent))) g.FillPath(b, p);
            Theme.Glyph(g, Kinds.Icon(B.Kind), new Rectangle(contentRect.X + contentRect.Width / 2 - ic / 2, cy - ic - Util.RoundI(6 * s), ic, ic), accent, 1.5f * s);
            Theme.TextMiddle(g, main, Theme.Font(11f, FontStyle.Regular), Theme.TextSub, r1, StringAlignment.Center);
            Rectangle r2 = new Rectangle(contentRect.X, cy + Util.RoundI(22 * s), contentRect.Width, Util.RoundI(18 * s));
            Theme.TextMiddle(g, sub, Theme.Font(8.5f, FontStyle.Regular), Theme.TextFaint, r2, StringAlignment.Center);
        }

        // ---------- 待办 ----------
        void PaintTodo(Color accent, float s)
        {
            int rowH = Util.RoundI(35 * s);
            int check = Util.RoundI(16 * s);
            Font f = Theme.Font(Theme.FxBody, FontStyle.Regular);
            Font fs = Theme.Font(Theme.FxBody, FontStyle.Strikeout);
            int rows = B.Items.Count;
            contentHeight = rows * rowH + Util.RoundI(34 * s);

            for (int i = 0; i < rows; i++)
            {
                Item it = B.Items[i];
                int y = contentRect.Y + i * rowH - Util.RoundI(scrollPx);
                Rectangle row = new Rectangle(contentRect.X, y, contentRect.Width, rowH);
                if (row.Bottom < contentRect.Y || row.Y > contentRect.Bottom) continue;

                if (hoverRow == i)
                    Glass.Chip(g, new Rectangle(row.X, row.Y + 2, row.Width, row.Height - 4), "", Color.Transparent);

                Rectangle box = new Rectangle(row.X + Util.RoundI(2 * s), row.Y + (rowH - check) / 2, check, check);
                using (GraphicsPath p = Theme.Round(box, Util.RoundI(4 * s)))
                {
                    if (it.Done)
                    {
                        using (SolidBrush b = new SolidBrush(Color.FromArgb(200, accent))) g.FillPath(b, p);
                        Theme.Glyph(g, Glyph.Check, Util.Inflate(box, -Util.RoundI(3 * s)), Theme.Light ? Color.White : Util.Rgb(12, 16, 24), 1.6f * s);
                    }
                    else
                    {
                        using (SolidBrush b = new SolidBrush(Color.FromArgb(20, 255, 255, 255))) g.FillPath(b, p);
                        using (Pen pen = new Pen(Color.FromArgb(110, Theme.TextSub), 1f * s)) g.DrawPath(pen, p);
                    }
                }
                hits.Add(new Zone(box, "check", i));

                Rectangle tr = new Rectangle(box.Right + Util.RoundI(9 * s), row.Y, row.Width - check - Util.RoundI(14 * s), rowH);
                Color tc = it.Done ? Theme.TextFaint : Theme.TextMain;
                Theme.TextMiddle(g, it.Text, it.Done ? fs : f, tc, tr, StringAlignment.Near);
                hits.Add(new Zone(row, "item", i));
            }

            int addY = contentRect.Y + rows * rowH - Util.RoundI(scrollPx);
            Rectangle add = new Rectangle(contentRect.X, addY, contentRect.Width, Util.RoundI(34 * s));
            if (add.Y < contentRect.Bottom + 20)
            {
                Theme.Glyph(g, Glyph.Plus, new Rectangle(add.X + Util.RoundI(2 * s), add.Y + Util.RoundI(7 * s), Util.RoundI(18 * s), Util.RoundI(18 * s)), Color.FromArgb(190, accent), 1.4f * s);
                Theme.TextMiddle(g, "添加一条", Theme.Font(10f, FontStyle.Regular), Theme.TextSub,
                    new Rectangle(add.X + Util.RoundI(26 * s), add.Y, add.Width, add.Height), StringAlignment.Near);
                hits.Add(new Zone(add, "add", 0));
            }

            if (rows == 0) DrawEmpty("今天还没有安排", "点下面的「添加一条」写第一件事", accent, s);
        }

        // ---------- 文件 ----------
        void PaintFiles(Color accent, float s)
        {
            int gap = Util.RoundI(8 * s);
            int tileH = Util.RoundI(58 * s);
            int cols = contentRect.Width >= Util.RoundI(250 * s) ? 2 : 1;
            int tileW = (contentRect.Width - gap * (cols - 1)) / cols;
            int iconSize = Util.RoundI(30 * s);
            Font f = Theme.Font(10.5f, FontStyle.Regular);
            Font fsub = Theme.Font(8f, FontStyle.Regular);

            int count = B.Items.Count;
            int total = count + 1;
            int rowsTotal = (total + cols - 1) / cols;
            contentHeight = rowsTotal * (tileH + gap);

            for (int i = 0; i < total; i++)
            {
                int row = i / cols, col = i % cols;
                int x = contentRect.X + col * (tileW + gap);
                int y = contentRect.Y + row * (tileH + gap) - Util.RoundI(scrollPx);
                Rectangle tile = new Rectangle(x, y, tileW, tileH);
                if (tile.Bottom < contentRect.Y || tile.Y > contentRect.Bottom) continue;

                if (i < count)
                {
                    Item it = B.Items[i];
                    if (hoverTile == i)
                    {
                        using (GraphicsPath p = Theme.Round(tile, Util.RoundI(12 * s)))
                        using (SolidBrush b = new SolidBrush(Theme.Hover)) g.FillPath(b, p);
                    }
                    Rectangle ic = new Rectangle(tile.X + Util.RoundI(10 * s), tile.Y + (tileH - iconSize) / 2, iconSize, iconSize);
                    if (!string.IsNullOrEmpty(it.Path) && System.IO.File.Exists(it.Path) || (!string.IsNullOrEmpty(it.Path) && System.IO.Directory.Exists(it.Path)))
                        Icons.Draw(g, it.Path, ic);
                    else
                        Theme.Glyph(g, Glyph.Folder, ic, Theme.TextFaint, 1.3f * s);

                    int labelW = tile.Width - iconSize - Util.RoundI(26 * s);
                    Rectangle lr = new Rectangle(ic.Right + Util.RoundI(8 * s), tile.Y + Util.RoundI(12 * s), labelW, Util.RoundI(19 * s));
                    Theme.Text(g, it.Text.Length > 0 ? it.Text : System.IO.Path.GetFileNameWithoutExtension(it.Path), f, Theme.TextMain, lr, StringAlignment.Near, true);
                    string sub = it.Path.Length > 0 ? System.IO.Path.GetExtension(it.Path).TrimStart('.').ToUpperInvariant() : "快捷入口";
                    if (sub.Length == 0) sub = System.IO.Directory.Exists(it.Path) ? "文件夹" : "文件";
                    Rectangle sr = new Rectangle(lr.X, tile.Y + Util.RoundI(31 * s), labelW, Util.RoundI(16 * s));
                    Theme.Text(g, sub, fsub, Theme.TextFaint, sr, StringAlignment.Near, true);
                    hits.Add(new Zone(tile, "tile", i));
                }
                else
                {
                    using (GraphicsPath p = Theme.Round(tile, Util.RoundI(12 * s)))
                    using (Pen pen = new Pen(Color.FromArgb(46, accent), 1f * s)) g.DrawPath(pen, p);
                    Rectangle plus = new Rectangle(tile.X + Util.RoundI(10 * s), tile.Y + (tileH - iconSize) / 2, iconSize, iconSize);
                    Theme.Glyph(g, Glyph.Plus, plus, Color.FromArgb(170, accent), 1.5f * s);
                    Rectangle lr = new Rectangle(plus.Right + Util.RoundI(8 * s), tile.Y, tile.Width - iconSize - Util.RoundI(26 * s), tileH);
                    Theme.TextMiddle(g, "收进文件", f, Theme.TextSub, lr, StringAlignment.Near);
                    hits.Add(new Zone(tile, "add", 0));
                }
            }

            if (count == 0) DrawEmpty("还没有文件入口", "点「收进文件」或把文件拖到看板上", accent, s);
        }

        // ---------- 代码 ----------
        void PaintCode(Color accent, float s)
        {
            int cardH = Util.RoundI(74 * s);
            int gap = Util.RoundI(8 * s);
            Font fm = Theme.Mono(9.5f, FontStyle.Regular);
            Font fl = Theme.Font(8.5f, FontStyle.Regular);

            contentHeight = B.Items.Count * (cardH + gap) + Util.RoundI(36 * s);
            for (int i = 0; i < B.Items.Count; i++)
            {
                int y = contentRect.Y + i * (cardH + gap) - Util.RoundI(scrollPx);
                Rectangle card = new Rectangle(contentRect.X, y, contentRect.Width, cardH);
                if (card.Bottom < contentRect.Y || card.Y > contentRect.Bottom) continue;

                Rectangle panel = card;
                using (GraphicsPath p = Theme.Round(panel, Util.RoundI(11 * s)))
                using (SolidBrush b = new SolidBrush(Theme.Light ? Color.FromArgb(26, 255, 255, 255) : Color.FromArgb(36, 0, 0, 0))) g.FillPath(b, p);
                using (GraphicsPath p = Theme.Round(panel, Util.RoundI(11 * s)))
                using (Pen pen = new Pen(Color.FromArgb(hoverTile == i ? 80 : 26, accent), 1f * s)) g.DrawPath(pen, p);

                string label = B.Items[i].Extra;
                if (string.IsNullOrEmpty(label)) label = "片段 " + (i + 1);
                Rectangle lr = new Rectangle(card.X + Util.RoundI(10 * s), card.Y + Util.RoundI(6 * s), card.Width - Util.RoundI(46 * s), Util.RoundI(15 * s));
                Theme.Text(g, label, fl, Color.FromArgb(210, accent), lr, StringAlignment.Near, true);

                Rectangle cr = new Rectangle(card.X + Util.RoundI(10 * s), card.Y + Util.RoundI(23 * s), card.Width - Util.RoundI(20 * s), card.Height - Util.RoundI(29 * s));
                List<string> lines = Util.Wrap(g, B.Items[i].Text.Replace("\r", ""), fm, cr.Width, 3);
                for (int k = 0; k < lines.Count && k < 3; k++)
                {
                    Rectangle lineR = new Rectangle(cr.X, cr.Y + k * Util.RoundI(15 * s), cr.Width, Util.RoundI(15 * s));
                    Theme.Text(g, lines[k], fm, Theme.TextSub, lineR, StringAlignment.Near, true);
                }

                Rectangle copy = new Rectangle(card.Right - Util.RoundI(30 * s), card.Y + Util.RoundI(6 * s), Util.RoundI(20 * s), Util.RoundI(20 * s));
                Theme.Glyph(g, Glyph.Copy, copy, Color.FromArgb(hoverTile == i ? 230 : 110, Theme.TextSub), 1.3f * s);
                hits.Add(new Zone(card, "copy", i));
                hits.Add(new Zone(copy, "copy", i));
            }

            int addY = contentRect.Y + B.Items.Count * (cardH + gap) - Util.RoundI(scrollPx);
            Rectangle add = new Rectangle(contentRect.X, addY, contentRect.Width, Util.RoundI(32 * s));
            if (add.Y < contentRect.Bottom + 20)
            {
                Theme.Glyph(g, Glyph.Plus, new Rectangle(add.X + 2, add.Y + 6, Util.RoundI(18 * s), Util.RoundI(18 * s)), Color.FromArgb(190, accent), 1.4f * s);
                Theme.TextMiddle(g, "收藏一段代码", Theme.Font(10f, FontStyle.Regular), Theme.TextSub,
                    new Rectangle(add.X + Util.RoundI(26 * s), add.Y, add.Width, add.Height), StringAlignment.Near);
                hits.Add(new Zone(add, "add", 0));
            }

            if (B.Items.Count == 0) DrawEmpty("代码夹是空的", "把常用的命令、配置片段收进来", accent, s);
        }

        // ---------- 状态 ----------
        void PaintStatus(Color accent, float s)
        {
            contentHeight = 0;
            int y = contentRect.Y;
            var wx = App.Weather == null ? null : App.Weather.Now;

            if (B.ShowWeather)
            {
                Rectangle wcard = new Rectangle(contentRect.X, y, contentRect.Width, Util.RoundI(96 * s));
                using (GraphicsPath p = Theme.Round(wcard, Util.RoundI(14 * s)))
                using (SolidBrush b = new SolidBrush(Color.FromArgb(Theme.Light ? 18 : 26, 255, 255, 255))) g.FillPath(b, p);

                Rectangle cityR = new Rectangle(wcard.X + Util.RoundI(12 * s), wcard.Y + Util.RoundI(8 * s), wcard.Width - Util.RoundI(60 * s), Util.RoundI(18 * s));
                Theme.Text(g, B.City + "  ›", Theme.Font(9.5f, FontStyle.Regular), Color.FromArgb(220, accent), cityR, StringAlignment.Near, false);
                hits.Add(new Zone(cityR, "city", 0));

                if (wx != null && wx.Has)
                {
                    string temp = wx.Temp.ToString() + "°";
                    Rectangle tr = new Rectangle(wcard.X + Util.RoundI(12 * s), wcard.Y + Util.RoundI(24 * s), Util.RoundI(120 * s), Util.RoundI(46 * s));
                    using (SolidBrush b = new SolidBrush(Theme.TextMain))
                        g.DrawString(temp, Theme.Font(30f, FontStyle.Regular), b, tr.X, tr.Y - Util.RoundI(6 * s));
                    WeatherService.Glyph(g, wx.Code, new Rectangle(wcard.Right - Util.RoundI(70 * s), wcard.Y + Util.RoundI(22 * s), Util.RoundI(46 * s), Util.RoundI(46 * s)), Color.FromArgb(230, accent));

                    Rectangle cr = new Rectangle(wcard.X + Util.RoundI(12 * s), wcard.Y + Util.RoundI(70 * s), wcard.Width - Util.RoundI(24 * s), Util.RoundI(18 * s));
                    Theme.Text(g, wx.Cond + " · 湿度 " + wx.Humidity + "% · 风 " + wx.Wind.ToString("0.0") + "m/s", Theme.Font(8.5f, FontStyle.Regular), Theme.TextSub, cr, StringAlignment.Near, true);
                }
                else
                {
                    Rectangle tr = new Rectangle(wcard.X + Util.RoundI(12 * s), wcard.Y + Util.RoundI(26 * s), wcard.Width - Util.RoundI(24 * s), Util.RoundI(40 * s));
                    Theme.Text(g, "天气未连接", Theme.Font(13f, FontStyle.Regular), Theme.TextSub, tr, StringAlignment.Near, false);
                    Rectangle hr = new Rectangle(wcard.X + Util.RoundI(12 * s), wcard.Y + Util.RoundI(64 * s), wcard.Width - Util.RoundI(24 * s), Util.RoundI(18 * s));
                    Theme.Text(g, App.Weather != null && App.Weather.Offline ? "离线也能用，联网后自动补上" : "点城市名换位置", Theme.Font(8.5f, FontStyle.Regular), Theme.TextFaint, hr, StringAlignment.Near, true);
                }
                y += Util.RoundI(106 * s);
                hits.Add(new Zone(wcard, "wcard", 0));
            }

            if (B.ShowStats)
            {
                StatsNow st = App.StatsNow;
                if (st == null) st = new StatsNow();
                int rowH = Util.RoundI(34 * s);
                DrawStat("CPU", (int)st.Cpu, st.CpuText, accent, ref y, rowH, s);
                DrawStat("内存", (int)st.MemPct, st.MemText, accent, ref y, rowH, s);
                DrawStat("磁盘", (int)st.DiskPct, st.DiskText, accent, ref y, rowH, s);
            }

            Rectangle foot = new Rectangle(contentRect.X, contentRect.Bottom - Util.RoundI(16 * s), contentRect.Width, Util.RoundI(14 * s));
            Theme.Text(g, "系统状态每 2 秒刷新" + (B.ShowWeather ? " · 天气每 30 分钟" : ""), Theme.Font(7.5f, FontStyle.Regular), Theme.TextFaint, foot, StringAlignment.Near, true);
        }

        void DrawStat(string label, int pct, string detail, Color accent, ref int y, int rowH, float s)
        {
            int lw = Util.RoundI(38 * s);
            Rectangle lr = new Rectangle(contentRect.X, y, lw, Util.RoundI(18 * s));
            Theme.Text(g, label, Theme.Font(9f, FontStyle.Regular), Theme.TextSub, lr, StringAlignment.Near, false);
            Rectangle dr = new Rectangle(contentRect.Right - Util.RoundI(110 * s), y, Util.RoundI(110 * s), Util.RoundI(18 * s));
            Theme.Text(g, detail, Theme.Font(9f, FontStyle.Regular), Theme.TextFaint, dr, StringAlignment.Far, true);

            Rectangle track = new Rectangle(contentRect.X, y + Util.RoundI(22 * s), contentRect.Width, Util.RoundI(4 * s));
            using (GraphicsPath p = Theme.Round(track, Util.RoundI(2 * s)))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(24, 255, 255, 255))) g.FillPath(b, p);
            int w = Math.Max(2, track.Width * Util.Clamp(pct, 0, 100) / 100);
            Rectangle fill = new Rectangle(track.X, track.Y, w, track.Height);
            using (GraphicsPath p = Theme.Round(fill, Util.RoundI(2 * s)))
            using (LinearGradientBrush lb = new LinearGradientBrush(new Rectangle(fill.X, fill.Y, Math.Max(2, fill.Width), Math.Max(1, fill.Height)),
                Color.FromArgb(210, accent), Color.FromArgb(235, Util.Mix(accent, Color.White, 0.35f)), 0f))
                g.FillPath(lb, p);
            y += rowH;
        }

        // ---------- 时钟 ----------
        void PaintClock(Color accent, float s)
        {
            contentHeight = 0;
            DateTime now = DateTime.Now;
            string hm = now.ToString("HH:mm");
            Font fbig = Theme.Font(Theme.FxClock, FontStyle.Regular);
            Font fsec = Theme.Font(15f, FontStyle.Regular);

            SizeF sz = g.MeasureString(hm, fbig, int.MaxValue, StringFormat.GenericTypographic);
            int x = contentRect.X + Util.RoundI(2 * s);
            int y = contentRect.Y + Util.RoundI(2 * s);
            using (SolidBrush b = new SolidBrush(Theme.TextMain)) g.DrawString(hm, fbig, b, x, y - Util.RoundI(8 * s));

            string sec = now.ToString("ss");
            Rectangle sr = new Rectangle(x + (int)sz.Width + Util.RoundI(2 * s), y + Util.RoundI(18 * s), Util.RoundI(40 * s), Util.RoundI(24 * s));
            Theme.Text(g, sec, fsec, Theme.TextSub, sr, StringAlignment.Near, false);

            string date = now.ToString("M月d日") + " · " + WeekName(now.DayOfWeek);
            int dateY = y + (int)sz.Height - Util.RoundI(18 * s);
            Rectangle dr = new Rectangle(x, dateY, contentRect.Width - Util.RoundI(70 * s), Util.RoundI(20 * s));
            Theme.Text(g, date, Theme.Font(10.5f, FontStyle.Regular), Theme.TextSub, dr, StringAlignment.Near, true);
            Rectangle wr = new Rectangle(x, dateY + Util.RoundI(20 * s), contentRect.Width - Util.RoundI(70 * s), Util.RoundI(16 * s));
            Theme.Text(g, "第 " + WeekOfYear(now) + " 周 · " + (now.Hour < 12 ? "上午" : now.Hour < 18 ? "下午" : "晚上"),
                Theme.Font(8.5f, FontStyle.Regular), Theme.TextFaint, wr, StringAlignment.Near, true);

            // 秒环
            int ring = Util.RoundI(52 * s);
            Rectangle rr = new Rectangle(contentRect.Right - ring - Util.RoundI(4 * s), contentRect.Y + Math.Max(0, (contentRect.Height - ring) / 2), ring, ring);
            using (Pen pen = new Pen(Color.FromArgb(20, 255, 255, 255), Util.RoundI(3 * s))) g.DrawEllipse(pen, rr);
            float sweep = now.Second / 60f * 360f;
            using (Pen pen = new Pen(Color.FromArgb(220, accent), Util.RoundI(3 * s)))
            {
                pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                g.DrawArc(pen, rr, -90, sweep);
            }
            Rectangle ir = new Rectangle(rr.X, rr.Y + ring / 2 - Util.RoundI(10 * s), ring, Util.RoundI(20 * s));
            Theme.TextMiddle(g, now.ToString("ss"), Theme.Font(12f, FontStyle.Regular), Theme.TextMain, ir, StringAlignment.Center);
        }

        static string WeekName(DayOfWeek d)
        {
            string[] n = { "星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" };
            return n[(int)d];
        }

        static int WeekOfYear(DateTime d)
        {
            System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.CurrentCulture;
            return ci.Calendar.GetWeekOfYear(d, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        // ---------- 滚动条 ----------
        void PaintScroll(Color accent, float s)
        {
            if (contentHeight <= contentRect.Height + 1) return;
            int trackW = Util.RoundI(3 * s);
            Rectangle track = new Rectangle(contentRect.Right - trackW, contentRect.Y, trackW, contentRect.Height);
            using (GraphicsPath p = Theme.Round(track, trackW / 2))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(14, 255, 255, 255))) g.FillPath(b, p);
            float view = contentRect.Height;
            int thumbH = Math.Max(Util.RoundI(26 * s), (int)(view * view / contentHeight));
            float maxScroll = Math.Max(1f, contentHeight - view);
            float k = Util.ClampF(scrollPx / maxScroll, 0f, 1f);
            int ty = contentRect.Y + (int)((view - thumbH) * k);
            Rectangle thumb = new Rectangle(track.X - Util.RoundI(1 * s), ty, trackW + Util.RoundI(2 * s), thumbH);
            using (GraphicsPath p = Theme.Round(thumb, thumb.Width / 2))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(150, accent))) g.FillPath(b, p);
        }
    }
}
