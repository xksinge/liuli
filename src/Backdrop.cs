// Liuli · 背景磨砂层：整屏壁纸烘焙一次（低分辨率 + 三遍盒滤），每块看板只做 1:1 切片。
// 逐帧渲染不再做模糊；换壁纸/换主题时重烘一次，闲置时零开销。
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;

namespace Liuli
{
    static class Backdrop
    {
        public static Bitmap Source;         // 已磨砂的壁纸（虚拟屏 × Scale）
        public static Rectangle Virtual = new Rectangle(0, 0, 1920, 1080);
        public static float Scale = 0.25f;
        public static Color Mean = Util.Rgb(28, 34, 48);
        public static Color Accent = Util.Rgb(126, 178, 255);
        public static bool Light;
        public static int Version;
        public static bool Ready;
        public static string LastSignature = "";
        public static double LastBuildMs;

        static readonly object gate = new object();
        static Bitmap pending;
        static int pendingVersion;

        public static void Start()
        {
            Rebuild();
        }

        /// <summary>重烘：同步执行，约 10~30ms；只在换壁纸与启动时发生。</summary>
        public static void Rebuild()
        {
            long t0 = Environment.TickCount;
            string sig = Wallpaper.Signature();
            string custom = null;
            try
            {
                if (App.Ws != null && App.Ws.Set.WallMode == "image" && App.Ws.Set.WallPath.Length > 0 && System.IO.File.Exists(App.Ws.Set.WallPath))
                    custom = App.Ws.Set.WallPath;
            }
            catch { }
            if (custom != null)
            {
                try { sig = "img:" + custom + ":" + System.IO.File.GetLastWriteTimeUtc(custom).Ticks; } catch { }
            }
            Rectangle vb = SystemInformation.VirtualScreen;
            float scale = Math.Min(0.25f, 1600f / Math.Max(1, Math.Max(vb.Width, vb.Height)));
            if (scale < 0.12f) scale = 0.12f;

            Color fallback = Wallpaper.DesktopColor();
            Bitmap raw = null;
            try
            {
                raw = Wallpaper.Render(vb, scale, custom, fallback);
                int radius = 2 + (int)(Theme.BlurPct * 0.10f);
                ImageFx.Frost(raw, 0.80f, 0.95f, 3);
                ImageFx.Blur(raw, radius, 3);
                ImageFx.Blur(raw, Math.Max(1, radius / 2), 1);

                Color mean = ImageFx.Average(raw);
                bool light;
                Color accent;
                Theme.PickTone(mean, out light, out accent);
                lock (gate)
                {
                    Bitmap old = Source;
                    Source = raw;
                    raw = null;
                    Virtual = vb;
                    Scale = scale;
                    Mean = mean;
                    Accent = accent;
                    Light = light;
                    LastSignature = sig;
                    Ready = true;
                    Version++;
                    DisposeSoon(old);
                }
                LastBuildMs = Environment.TickCount - t0;
                Log.Info("wall", "磨砂重烘 " + raw0(vb) + " scale=" + scale.ToString("0.###") + " ms=" + LastBuildMs.ToString("0"));
            }
            catch (Exception ex)
            {
                Log.Error("wall", "磨砂失败：" + ex.Message);
            }
            finally
            {
                // raw 已移交 Source，这里只在失败时释放
            }
        }

        static string raw0(Rectangle r) { return r.Width + "x" + r.Height + "@" + r.X + "," + r.Y; }

        /// <summary>轮询壁纸签名，变了就重烘。返回是否发生变化。</summary>
        public static bool Tick()
        {
            try
            {
                string sig = Wallpaper.Signature();
                if (sig != LastSignature) { Rebuild(); return true; }
            }
            catch { }
            return false;
        }

        /// <summary>动态壁纸/视频帧：外部给一张整屏画面，转成磨砂底。</summary>
        public static void RebuildFromFrame(Bitmap frame)
        {
            if (frame == null) return;
            int radius = 2 + (int)(Theme.BlurPct * 0.10f);
            try
            {
                Bitmap copy = new Bitmap(frame.Width, frame.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                using (Graphics g = Graphics.FromImage(copy)) g.DrawImage(frame, 0, 0, frame.Width, frame.Height);
                ImageFx.Frost(copy, 0.80f, 0.95f, 3);
                ImageFx.Blur(copy, radius, 2);
                Color mean = ImageFx.Average(copy);
                bool light;
                Color accent;
                Theme.PickTone(mean, out light, out accent);
                lock (gate)
                {
                    Source = copy;
                    Virtual = SystemInformation.VirtualScreen;
                    Mean = mean;
                    Accent = accent;
                    Light = light;
                    Ready = true;
                    Version++;
                }
            }
            catch (Exception ex) { Log.Warn("wall", "帧磨砂失败 " + ex.Message); }
        }

        /// <summary>把看板在屏幕上的矩形，从磨砂底里切出来画到本地目标矩形。</summary>
        public static void Draw(Graphics g, RectangleF destLocal, Rectangle boardScreen)
        {
            lock (gate)
            {
                if (Source == null) return;
                float scale = Scale;
                RectangleF s = new RectangleF(
                    (boardScreen.X - Virtual.X) * scale,
                    (boardScreen.Y - Virtual.Y) * scale,
                    boardScreen.Width * scale,
                    boardScreen.Height * scale);
                if (s.Width < 1 || s.Height < 1) return;
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(Source, destLocal, s, GraphicsUnit.Pixel);
            }
        }

        static void DisposeSoon(Bitmap old)
        {
            if (old == null) return;
            ThreadPool.QueueUserWorkItem(delegate
            {
                try { Thread.Sleep(3000); old.Dispose(); } catch { }
            });
        }

        public static void Dispose()
        {
            lock (gate)
            {
                if (Source != null) { Source.Dispose(); Source = null; }
            }
        }
    }
}
