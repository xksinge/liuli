// Liuli · 玻璃右键菜单：固定一列动作，点外部/Esc 关闭；不用系统菜单是为了不破观感。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Liuli
{
    sealed class MenuForm : Form
    {
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);

        public sealed class Item
        {
            public string Text;
            public Action Run;
            public Glyph Icon;
            public bool Danger;
            public Item(string text, Action run) { Text = text; Run = run; Icon = Glyph.Dot; }
            public Item(string text, Action run, Glyph icon) { Text = text; Run = run; Icon = icon; }
            public Item(string text, Action run, Glyph icon, bool danger) { Text = text; Run = run; Icon = icon; Danger = danger; }
        }

        readonly List<Item> items;
        int hover = -1;
        int downIndex = -1;
        int rowH;

        public MenuForm(List<Item> items, Size anchor)
        {
            this.items = items ?? new List<Item>();
            float s = Theme.S;
            rowH = Util.RoundI(34 * s);
            int width = Util.RoundI(216 * s);
            foreach (Item it in this.items) width = Math.Max(width, Util.RoundI((it.Text.Length * 13 + 90) * s));
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            DoubleBuffered = true;
            BackColor = Util.Rgb(16, 20, 28);
            ClientSize = new Size(width, this.items.Count * rowH + Util.RoundI(16 * s));
            using (GraphicsPath p = Theme.Round(new Rectangle(0, 0, Width, Height), Util.RoundI(14 * s)))
                Region = new Region(p);
            MouseMove += OnMove;
            MouseDown += OnDown;
            MouseUp += OnUp;
            KeyPreview = true;
        }

        public void ShowAt(Point screen)
        {
            Rectangle work = Screen.FromPoint(screen).WorkingArea;
            int x = screen.X - Util.RoundI(12 * Theme.S);
            int y = screen.Y - Util.RoundI(8 * Theme.S);
            if (x + Width > work.Right - 6) x = work.Right - Width - 6;
            if (y + Height > work.Bottom - 6) y = screen.Y - Height + Util.RoundI(24 * Theme.S);
            if (y < work.Top + 6) y = work.Top + 6;
            if (x < work.X + 6) x = work.X + 6;
            Location = new Point(x, y);
            Show();
            try { SetForegroundWindow(Handle); } catch { }
            Activate();
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Close();
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width, Height);
            using (LinearGradientBrush lb = new LinearGradientBrush(r, Util.Rgb(32, 38, 52), Util.Rgb(14, 17, 24), 90f))
                g.FillRectangle(lb, r);
            using (Pen p = new Pen(Color.FromArgb(80, 255, 255, 255), 1f)) g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
            using (LinearGradientBrush lb = new LinearGradientBrush(new Rectangle(18, 0, Width - 36, 2), Color.FromArgb(100, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 0f))
                g.FillRectangle(lb, 18, 0, Width - 36, 1);

            int s = Util.RoundI(14 * Theme.S);
            for (int i = 0; i < items.Count; i++)
            {
                Rectangle row = new Rectangle(Util.RoundI(6 * Theme.S), s + i * rowH, Width - Util.RoundI(12 * Theme.S), rowH);
                if (hover == i)
                {
                    using (GraphicsPath p = Theme.Round(row, Util.RoundI(9 * Theme.S)))
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(34, 255, 255, 255))) g.FillPath(b, p);
                }
                Color fg = items[i].Danger ? Util.Rgb(255, 138, 138) : Theme.TextMain;
                Theme.Glyph(g, items[i].Icon, new Rectangle(row.X + Util.RoundI(10 * Theme.S), row.Y + (rowH - Util.RoundI(18 * Theme.S)) / 2, Util.RoundI(18 * Theme.S), Util.RoundI(18 * Theme.S)), items[i].Danger ? fg : Theme.TextSub, 1.3f * Theme.S);
                Rectangle tr = new Rectangle(row.X + Util.RoundI(38 * Theme.S), row.Y, row.Width - Util.RoundI(46 * Theme.S), rowH);
                Theme.TextMiddle(g, items[i].Text, Theme.Font(11f, FontStyle.Regular), fg, tr, StringAlignment.Near);
            }
        }

        void OnMove(object s, MouseEventArgs e)
        {
            int s2 = Util.RoundI(14 * Theme.S);
            int idx = (e.Y - s2) / rowH;
            if (idx < 0 || idx >= items.Count) idx = -1;
            if (idx != hover) { hover = idx; Invalidate(); }
            Cursor = idx >= 0 ? Cursors.Hand : Cursors.Default;
        }

        void OnDown(object s, MouseEventArgs e)
        {
            int s2 = Util.RoundI(14 * Theme.S);
            int idx = (e.Y - s2) / rowH;
            if (idx >= 0 && idx < items.Count) downIndex = idx;
            else { downIndex = -1; Close(); }
        }

        void OnUp(object s, MouseEventArgs e)
        {
            int s2 = Util.RoundI(14 * Theme.S);
            int idx = (e.Y - s2) / rowH;
            int chosen = downIndex;
            downIndex = -1;
            if (chosen >= 0 && chosen < items.Count && idx == chosen)
            {
                Item it = items[chosen];
                Close();
                if (it.Run != null) it.Run();
            }
            else Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape) { Close(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
