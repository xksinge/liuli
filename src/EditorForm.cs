// Liuli · 文本编辑窗：看板本身是 NOACTIVATE 分层窗，打不了字；
// 输入交给这个短命的普通窗口（真 TextBox => 中文输入法、选择、右键菜单都正常）。
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Liuli
{
    sealed class EditorForm : Form
    {
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);

        readonly bool multiline;
        readonly string original;
        TextBox tb;
        Rectangle saveR, cancelR, closeR;
        int hoverBtn = -1;
        bool dragging;
        Point dragOff;
        string result;
        bool saved;

        public static string AskText(Form owner, string title, string initial, bool multiline)
        {
            EditorForm f = new EditorForm(title, initial, multiline);
            f.PlaceNear(owner);
            string r = null;
            App.HideBar();
            try { f.ShowDialog(owner); r = f.result; }
            finally { f.Dispose(); App.RestoreBar(); }
            return r;
        }

        EditorForm(string title, string initial, bool multiline)
        {
            this.multiline = multiline;
            this.original = initial == null ? "" : initial;
            Text = title;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            KeyPreview = true;
            DoubleBuffered = true;
            BackColor = Util.Rgb(18, 22, 31);
            Font = Theme.Font(10f, FontStyle.Regular);

            int w = Util.RoundI((multiline ? 560 : 470) * Theme.S);
            int h = Util.RoundI((multiline ? 420 : 212) * Theme.S);
            ClientSize = new Size(w, h);
            ApplyRegion();

            Label titleLabel = new Label();
            titleLabel.Text = title;
            titleLabel.AutoSize = false;
            titleLabel.SetBounds(Util.RoundI(22 * Theme.S), Util.RoundI(16 * Theme.S), w - Util.RoundI(100 * Theme.S), Util.RoundI(24 * Theme.S));
            titleLabel.ForeColor = Theme.TextMain;
            titleLabel.Font = Theme.Font(12f, FontStyle.Bold);
            titleLabel.BackColor = Color.Transparent;

            tb = new TextBox();
            tb.Multiline = multiline;
            tb.AcceptsReturn = multiline;
            tb.ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None;
            tb.BackColor = Util.Rgb(30, 36, 48);
            tb.ForeColor = Util.Rgb(238, 243, 250);
            tb.BorderStyle = BorderStyle.None;
            tb.Font = multiline ? Theme.Mono(10.5f, FontStyle.Regular) : Theme.Font(11.5f, FontStyle.Regular);
            tb.SetBounds(Util.RoundI(22 * Theme.S), Util.RoundI(50 * Theme.S),
                w - Util.RoundI(44 * Theme.S), h - Util.RoundI(50 * Theme.S) - Util.RoundI(74 * Theme.S));
            tb.Text = initial;
            tb.ImeMode = ImeMode.NoControl;

            Controls.Add(titleLabel);
            Controls.Add(tb);

            Shown += delegate
            {
                try { SetForegroundWindow(Handle); } catch { }
                Activate();
                BeginInvoke((MethodInvoker)delegate
                {
                    try { tb.Focus(); tb.SelectionStart = tb.TextLength; } catch { }
                });
            };
            KeyDown += delegate (object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape) { result = null; Close(); return; }
                if (e.Control && e.KeyCode == Keys.Enter) { Save(); return; }
                if (!multiline && e.KeyCode == Keys.Enter) { Save(); return; }
            };
            tb.KeyDown += delegate (object s, KeyEventArgs e)
            {
                if (e.Control && e.KeyCode == Keys.Enter) { Save(); e.SuppressKeyPress = true; }
            };
            MouseMove += OnMove;
            MouseDown += OnDown;
            MouseUp += OnUp;
        }

        void PlaceNear(Form owner)
        {
            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            int x, y;
            if (owner != null && owner.Bounds.Width > 0)
            {
                x = owner.Left + (owner.Width - Width) / 2;
                y = owner.Top + Util.RoundI(40 * Theme.S);
            }
            else
            {
                x = work.X + (work.Width - Width) / 2;
                y = work.Y + (work.Height - Height) / 2;
            }
            if (owner != null) { Rectangle s = Screen.FromControl(owner).WorkingArea; work = s; }
            x = Util.Clamp(x, work.X + 8, work.Right - Width - 8);
            y = Util.Clamp(y, work.Y + 8, work.Bottom - Height - 8);
            Location = new Point(x, y);
        }

        void ApplyRegion()
        {
            using (GraphicsPath p = Theme.Round(new Rectangle(0, 0, Width, Height), Util.RoundI(16 * Theme.S)))
            {
                Region = new Region(p);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            Rectangle r = new Rectangle(0, 0, Width, Height);
            using (LinearGradientBrush lb = new LinearGradientBrush(r, Util.Rgb(28, 34, 47), Util.Rgb(14, 17, 24), 90f))
                g.FillRectangle(lb, r);
            using (Pen p = new Pen(Color.FromArgb(70, 255, 255, 255), 1f))
                g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
            // 顶部高光
            using (LinearGradientBrush lb = new LinearGradientBrush(new Rectangle(20, 0, Width - 40, 2), Color.FromArgb(90, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 0f))
                g.FillRectangle(lb, 20, 0, Width - 40, 1);

            // 输入框底
            int tx = tb.Left, ty = tb.Top, tw = tb.Width, th = tb.Height;
            Rectangle box = new Rectangle(tx - 10, ty - 8, tw + 20, th + 16);
            using (GraphicsPath p = Theme.Round(box, Util.RoundI(10 * Theme.S)))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(160, Util.Rgb(30, 36, 48)))) g.FillPath(b, p);
            using (GraphicsPath p = Theme.Round(box, Util.RoundI(10 * Theme.S)))
            using (Pen pen = new Pen(Color.FromArgb(40, 255, 255, 255), 1f)) g.DrawPath(pen, p);

            // 说明
            Rectangle hint = new Rectangle(Util.RoundI(22 * Theme.S), Height - Util.RoundI(64 * Theme.S), Width - Util.RoundI(220 * Theme.S), Util.RoundI(18 * Theme.S));
            Theme.Text(g, multiline ? "Ctrl+Enter 保存 · Esc 取消" : "Enter 保存 · Esc 取消", Theme.Font(8.5f, FontStyle.Regular), Theme.TextFaint, hint, StringAlignment.Near, true);

            int bw = Util.RoundI(78 * Theme.S), bh = Util.RoundI(34 * Theme.S), gap = Util.RoundI(10 * Theme.S);
            cancelR = new Rectangle(Width - Util.RoundI(22 * Theme.S) - bw * 2 - gap, Height - Util.RoundI(58 * Theme.S), bw, bh);
            saveR = new Rectangle(Width - Util.RoundI(22 * Theme.S) - bw, Height - Util.RoundI(58 * Theme.S), bw, bh);
            PaintButton(g, cancelR, "取消", hoverBtn == 1, false);
            PaintButton(g, saveR, "保存", hoverBtn == 0, true);

            closeR = new Rectangle(Width - Util.RoundI(40 * Theme.S), Util.RoundI(12 * Theme.S), Util.RoundI(26 * Theme.S), Util.RoundI(26 * Theme.S));
            Glass.Button(g, closeR, Glyph.Close, hoverBtn == 2, false, Theme.Accent);
        }

        void PaintButton(Graphics g, Rectangle r, string text, bool hover, bool primary)
        {
            using (GraphicsPath p = Theme.Round(r, Util.RoundI(10 * Theme.S)))
            {
                if (primary)
                {
                    using (LinearGradientBrush lb = new LinearGradientBrush(r, Color.FromArgb(hover ? 235 : 210, Theme.Accent),
                        Color.FromArgb(hover ? 200 : 170, Util.Mix(Theme.Accent, Color.Black, 0.25f)), 90f)) g.FillPath(lb, p);
                }
                else
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(hover ? 44 : 26, 255, 255, 255))) g.FillPath(b, p);
                    using (Pen pen = new Pen(Color.FromArgb(50, 255, 255, 255), 1f)) g.DrawPath(pen, p);
                }
            }
            Theme.TextMiddle(g, text, Theme.Font(11f, FontStyle.Bold), primary ? Util.Rgb(12, 16, 24) : Theme.TextMain, r, StringAlignment.Center);
        }

        void OnMove(object s, MouseEventArgs e)
        {
            int hb = -1;
            if (saveR.Contains(e.Location)) hb = 0;
            else if (cancelR.Contains(e.Location)) hb = 1;
            else if (closeR.Contains(e.Location)) hb = 2;
            if (hb != hoverBtn) { hoverBtn = hb; Invalidate(); }
            if (dragging)
            {
                Point p = PointToScreen(e.Location);
                Location = new Point(p.X - dragOff.X, p.Y - dragOff.Y);
            }
            Cursor = (e.Y < Util.RoundI(48 * Theme.S) && hb < 0) ? Cursors.SizeAll : (hb >= 0 ? Cursors.Hand : Cursors.Default);
        }

        void OnDown(object s, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (e.Y < Util.RoundI(48 * Theme.S) && e.X < Width - Util.RoundI(50 * Theme.S))
            {
                dragging = true;
                Point sp = PointToScreen(e.Location);
                dragOff = new Point(sp.X - Left, sp.Y - Top);
                return;
            }
            if (saveR.Contains(e.Location)) { Save(); return; }
            if (cancelR.Contains(e.Location)) { result = null; Close(); return; }
            if (closeR.Contains(e.Location)) { result = null; Close(); return; }
        }

        void OnUp(object s, MouseEventArgs e)
        {
            dragging = false;
        }

        void Save()
        {
            if (saved) return;
            saved = true;
            result = tb.Text;
            Log.Info("editor", "save len=" + (result == null ? -1 : result.Length));
            Close();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
        }
    }
}
