// Liuli · 轻提示：底部居中的玻璃胶囊，2 秒自动消失；不弹对话框。
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Liuli
{
    sealed class ToastForm : Form
    {
        static ToastForm current;
        static readonly object gate = new object();

        readonly string text;
        Bitmap canvas;
        long born;
        const int InMs = 170, HoldMs = 1500, OutMs = 220;
        readonly Timer timer;

        public static void Show(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            lock (gate)
            {
                if (current != null) { try { current.CloseNow(); } catch { } current = null; }
                current = new ToastForm(text);
                current.Show();
            }
        }

        ToastForm(string text)
        {
            this.text = text;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = Color.Black;
            Font = Theme.Font(11f, FontStyle.Regular);

            Size size = Measure();
            ClientSize = size;
            canvas = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(work.X + (work.Width - Width) / 2, work.Bottom - Height - Util.RoundI(28 * Theme.S));
            born = Environment.TickCount;
            Render(255);
            timer = new Timer();
            timer.Interval = 15;
            timer.Tick += OnTick;
            timer.Start();
        }

        Size Measure()
        {
            using (Bitmap b = new Bitmap(1, 1))
            using (Graphics g = Graphics.FromImage(b))
            {
                SizeF sz = g.MeasureString(text, Theme.Font(11f, FontStyle.Regular), int.MaxValue, StringFormat.GenericTypographic);
                int w = Math.Max(Util.RoundI(140 * Theme.S), (int)sz.Width + Util.RoundI(64 * Theme.S));
                int maxW = Util.RoundI(520 * Theme.S);
                if (w > maxW) w = maxW;
                return new Size(w, Util.RoundI(42 * Theme.S));
            }
        }

        void OnTick(object s, EventArgs e)
        {
            long age = Environment.TickCount - born;
            int alpha;
            if (age < InMs) alpha = (int)(age * 255f / InMs);
            else if (age < InMs + HoldMs) alpha = 255;
            else if (age < InMs + HoldMs + OutMs) alpha = (int)(255f * (1f - (age - InMs - HoldMs) / (float)OutMs));
            else { alpha = 0; CloseNow(); return; }
            Render(alpha);
        }

        void Render(int alpha)
        {
            if (canvas == null) return;
            using (Graphics g = Graphics.FromImage(canvas))
            {
                g.Clear(Color.FromArgb(0));
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                Rectangle body = new Rectangle(Util.RoundI(6 * Theme.S), Util.RoundI(6 * Theme.S), Width - Util.RoundI(12 * Theme.S), Height - Util.RoundI(12 * Theme.S));
                Theme.DrawShadow(g, body, body.Height / 2, Util.RoundI(8 * Theme.S), 90);
                using (GraphicsPath p = Theme.Round(body, body.Height / 2))
                {
                    g.SetClip(p);
                    Backdrop.Draw(g, Util.ToF(body), new Rectangle(Left + body.X, Top + body.Y, body.Width, body.Height));
                    g.ResetClip();
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(120, 12, 15, 22))) g.FillPath(b, p);
                    using (Pen pen = new Pen(Color.FromArgb(90, 255, 255, 255), 1f)) g.DrawPath(pen, p);
                }
                int dot = Util.RoundI(8 * Theme.S);
                using (SolidBrush b = new SolidBrush(Color.FromArgb(220, Theme.Accent)))
                    g.FillEllipse(b, body.X + Util.RoundI(20 * Theme.S), body.Y + (body.Height - dot) / 2, dot, dot);
                Rectangle tr = new Rectangle(body.X + Util.RoundI(38 * Theme.S), body.Y, body.Width - Util.RoundI(52 * Theme.S), body.Height);
                Theme.TextMiddle(g, text, Theme.Font(11f, FontStyle.Regular), Theme.TextMain, tr, StringAlignment.Near);
            }
            byte a = (byte)Util.Clamp(alpha, 0, 255);
            Native.PushLayered(Handle, canvas, Location, a);
        }

        void CloseNow()
        {
            try
            {
                timer.Stop();
                timer.Dispose();
            }
            catch { }
            Close();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= Native.WS_EX_LAYERED | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE | Native.WS_EX_TOPMOST;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation { get { return true; } }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e) { }
    }
}
