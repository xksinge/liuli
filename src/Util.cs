// Liuli · 通用工具：JSON、原子写入、缓动、颜色、文字度量、几何。
// 目标框架 .NET Framework 4.8 / C# 5（csc 4.0），不要用字符串插值、?.、nameof 等新语法。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Text;

namespace Liuli
{
    static class Util
    {
        // ---------- 数值 ----------
        public static int Clamp(int v, int lo, int hi) { return v < lo ? lo : (v > hi ? hi : v); }
        public static float ClampF(float v, float lo, float hi) { return v < lo ? lo : (v > hi ? hi : v); }
        public static double ClampD(double v, double lo, double hi) { return v < lo ? lo : (v > hi ? hi : v); }
        public static int RoundI(float v) { return (int)Math.Round(v, MidpointRounding.AwayFromZero); }
        public static float Lerp(float a, float b, float t) { return a + (b - a) * t; }
        public static int LerpI(int a, int b, float t) { return RoundI(a + (b - a) * t); }

        /// <summary>0..1 平滑曲线，动效统一用它，避免每帧乘 0.2 的指数逼近。</summary>
        public static float Smooth(float t)
        {
            t = ClampF(t, 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        public static float OutCubic(float t)
        {
            t = ClampF(t, 0f, 1f);
            float u = 1f - t;
            return 1f - u * u * u;
        }

        /// <summary>轻微回弹：0..1 区间内先过冲再落位，用于面板展开。</summary>
        public static float OutBack(float t)
        {
            t = ClampF(t, 0f, 1f);
            const float c = 1.24f;
            float u = t - 1f;
            return 1f + u * u * ((c + 1f) * u + c);
        }

        /// <summary>由 UTC tick 驱动的动画进度；dur<=0 视为立刻完成。</summary>
        public static float Progress(long startTick, int durMs)
        {
            if (durMs <= 0) return 1f;
            return ClampF((float)(Environment.TickCount - startTick) / durMs, 0f, 1f);
        }

        // ---------- 颜色 ----------
        public static Color Rgb(int r, int g, int b) { return Color.FromArgb(255, Byte(r), Byte(g), Byte(b)); }

        public static Color Rgba(int a, int r, int g, int b) { return Color.FromArgb(Byte(a), Byte(r), Byte(g), Byte(b)); }

        static int Byte(int v) { return v < 0 ? 0 : (v > 255 ? 255 : v); }

        public static Color Alpha(Color c, int a) { return Color.FromArgb(Byte(a), c); }

        public static Color Mix(Color a, Color b, float t)
        {
            t = ClampF(t, 0f, 1f);
            return Color.FromArgb(
                LerpI(a.A, b.A, t), LerpI(a.R, b.R, t), LerpI(a.G, b.G, t), LerpI(a.B, b.B, t));
        }

        public static Color Scale(Color c, float k)
        {
            return Color.FromArgb(c.A, Byte(RoundI(c.R * k)), Byte(RoundI(c.G * k)), Byte(RoundI(c.B * k)));
        }

        public static int Luma(Color c) { return (299 * c.R + 587 * c.G + 114 * c.B) / 1000; }

        public static Color Hsl(float h, float s, float l)
        {
            h = ((h % 360f) + 360f) % 360f / 60f;
            float c = (1f - Math.Abs(2f * l - 1f)) * s;
            float x = c * (1f - Math.Abs((h % 2f) - 1f));
            float r = 0, g = 0, b = 0;
            if (h < 1f) { r = c; g = x; }
            else if (h < 2f) { r = x; g = c; }
            else if (h < 3f) { g = c; b = x; }
            else if (h < 4f) { g = x; b = c; }
            else if (h < 5f) { r = x; b = c; }
            else { r = c; b = x; }
            float m = l - c / 2f;
            return Rgb(RoundI((r + m) * 255f), RoundI((g + m) * 255f), RoundI((b + m) * 255f));
        }

        public static void RgbToHsl(Color c, out float h, out float s, out float l)
        {
            float r = c.R / 255f, g = c.G / 255f, b = c.B / 255f;
            float mx = Math.Max(r, Math.Max(g, b)), mn = Math.Min(r, Math.Min(g, b));
            l = (mx + mn) / 2f;
            h = 0f; s = 0f;
            if (mx - mn < 0.0001f) return;
            float d = mx - mn;
            if (mx == r) h = ((g - b) / d) % 6f;
            else if (mx == g) h = (b - r) / d + 2f;
            else h = (r - g) / d + 4f;
            h *= 60f;
            if (h < 0f) h += 360f;
            s = l > 0.5f ? d / (2f - mx - mn) : d / (mx + mn);
        }

        // ---------- 几何 ----------
        public static Rectangle Inflate(Rectangle r, int d) { r.Inflate(d, d); return r; }

        public static RectangleF ToF(Rectangle r) { return new RectangleF(r.X, r.Y, r.Width, r.Height); }

        public static bool Contains(Rectangle r, Point p) { return p.X >= r.X && p.Y >= r.Y && p.X < r.Right && p.Y < r.Bottom; }

        public static int Snap(int value, int[] targets, int range)
        {
            int best = value, dist = range + 1;
            for (int i = 0; i < targets.Length; i++)
            {
                int d = Math.Abs(targets[i] - value);
                if (d < dist) { dist = d; best = targets[i]; }
            }
            return dist <= range ? best : value;
        }

        // ---------- 文字 ----------
        public static string Ellipsis(Graphics g, string text, Font font, int width)
        {
            if (string.IsNullOrEmpty(text) || width <= 0) return "";
            if (g.MeasureString(text, font, int.MaxValue, StringFormat.GenericTypographic).Width <= width) return text;
            string tail = "…";
            float tailW = g.MeasureString(tail, font, int.MaxValue, StringFormat.GenericTypographic).Width;
            int lo = 0, hi = text.Length;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                float w = g.MeasureString(text.Substring(0, mid), font, int.MaxValue, StringFormat.GenericTypographic).Width + tailW;
                if (w <= width) lo = mid; else hi = mid - 1;
            }
            return lo <= 0 ? "" : text.Substring(0, lo) + tail;
        }

        /// <summary>按像素宽度折行（中英文混排直接按字符切，够用且稳定）。</summary>
        public static List<string> Wrap(Graphics g, string text, Font font, int width, int maxLines)
        {
            List<string> lines = new List<string>();
            if (string.IsNullOrEmpty(text)) return lines;
            string[] paras = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int p = 0; p < paras.Length; p++)
            {
                string para = paras[p];
                if (para.Length == 0) { lines.Add(""); continue; }
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < para.Length; i++)
                {
                    sb.Append(para[i]);
                    if (g.MeasureString(sb.ToString(), font, int.MaxValue, StringFormat.GenericTypographic).Width > width && sb.Length > 1)
                    {
                        sb.Length -= 1;
                        lines.Add(sb.ToString());
                        sb.Length = 0;
                        sb.Append(para[i]);
                    }
                    if (maxLines > 0 && lines.Count >= maxLines) break;
                }
                if (sb.Length > 0) lines.Add(sb.ToString());
                if (maxLines > 0 && lines.Count >= maxLines) break;
            }
            return lines;
        }

        // ---------- 文件 ----------
        public static bool WriteAllTextAtomic(string path, string text)
        {
            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, text, new UTF8Encoding(true));
                if (File.Exists(path))
                {
                    try { File.Replace(tmp, path, null); return true; }
                    catch { File.Delete(path); }
                }
                File.Move(tmp, path);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn("atomic write failed: " + path + " " + ex.Message);
                return false;
            }
        }

        public static string ReadAllTextSafe(string path)
        {
            try { return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : null; }
            catch { return null; }
        }

        public static long DirSize(string dir)
        {
            long total = 0;
            try
            {
                foreach (string f in Directory.GetFiles(dir)) { try { total += new FileInfo(f).Length; } catch { } }
                foreach (string d in Directory.GetDirectories(dir)) total += DirSize(d);
            }
            catch { }
            return total;
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("0.0") + " KB";
            if (bytes < 1024L * 1024 * 1024) return (bytes / 1048576.0).ToString("0.0") + " MB";
            return (bytes / 1073741824.0).ToString("0.0") + " GB";
        }
    }

    /// <summary>极简 JSON：只服务于本程序的配置/备份，键序保持插入顺序。</summary>
    static class Json
    {
        public static object Parse(string text)
        {
            if (text == null) return null;
            int i = 0;
            object v = ParseValue(text, ref i);
            return v;
        }

        static void SkipWs(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }

        static object ParseValue(string s, ref int i)
        {
            SkipWs(s, ref i);
            if (i >= s.Length) return null;
            char c = s[i];
            if (c == '{') return ParseObject(s, ref i);
            if (c == '[') return ParseArray(s, ref i);
            if (c == '"') return ParseString(s, ref i);
            if (c == 't') { i += 4; return true; }
            if (c == 'f') { i += 5; return false; }
            if (c == 'n') { i += 4; return null; }
            return ParseNumber(s, ref i);
        }

        static Dictionary<string, object> ParseObject(string s, ref int i)
        {
            Dictionary<string, object> d = new Dictionary<string, object>();
            i++;
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return d; }
            while (i < s.Length)
            {
                SkipWs(s, ref i);
                if (i >= s.Length || s[i] != '"') break;
                string key = ParseString(s, ref i);
                SkipWs(s, ref i);
                if (i < s.Length && s[i] == ':') i++;
                object v = ParseValue(s, ref i);
                d[key] = v;
                SkipWs(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == '}') { i++; break; }
                break;
            }
            return d;
        }

        static List<object> ParseArray(string s, ref int i)
        {
            List<object> list = new List<object>();
            i++;
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return list; }
            while (i < s.Length)
            {
                list.Add(ParseValue(s, ref i));
                SkipWs(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == ']') { i++; break; }
                break;
            }
            return list;
        }

        static string ParseString(string s, ref int i)
        {
            StringBuilder sb = new StringBuilder();
            i++;
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') break;
                if (c != '\\') { sb.Append(c); continue; }
                if (i >= s.Length) break;
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 3 < s.Length)
                        {
                            int code;
                            if (int.TryParse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code))
                            {
                                sb.Append((char)code);
                                i += 4;
                            }
                        }
                        break;
                    default: sb.Append(e); break;
                }
            }
            return sb.ToString();
        }

        static double ParseNumber(string s, ref int i)
        {
            int start = i;
            if (i < s.Length && (s[i] == '-' || s[i] == '+')) i++;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.' || s[i] == 'e' || s[i] == 'E' || s[i] == '-' || s[i] == '+')) i++;
            double d;
            double.TryParse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out d);
            return d;
        }

        public static string Write(object value, bool pretty)
        {
            StringBuilder sb = new StringBuilder();
            WriteValue(sb, value, pretty, 0);
            return sb.ToString();
        }

        static void Indent(StringBuilder sb, int depth)
        {
            for (int i = 0; i < depth; i++) sb.Append("  ");
        }

        static void WriteValue(StringBuilder sb, object v, bool pretty, int depth)
        {
            if (v == null) { sb.Append("null"); return; }
            if (v is string) { WriteString(sb, (string)v); return; }
            if (v is bool) { sb.Append(((bool)v) ? "true" : "false"); return; }
            if (v is int || v is long) { sb.Append(Convert.ToInt64(v).ToString(CultureInfo.InvariantCulture)); return; }
            if (v is float || v is double) { sb.Append(Convert.ToDouble(v).ToString("0.####", CultureInfo.InvariantCulture)); return; }
            Dictionary<string, object> od = v as Dictionary<string, object>;
            if (od != null)
            {
                sb.Append('{');
                bool first = true;
                foreach (KeyValuePair<string, object> kv in od)
                {
                    if (!first) sb.Append(',');
                    if (pretty) { sb.Append('\n'); Indent(sb, depth + 1); }
                    WriteString(sb, kv.Key);
                    sb.Append(pretty ? ": " : ":");
                    WriteValue(sb, kv.Value, pretty, depth + 1);
                    first = false;
                }
                if (pretty && !first) { sb.Append('\n'); Indent(sb, depth); }
                sb.Append('}');
                return;
            }
            List<object> arr = v as List<object>;
            if (arr != null)
            {
                sb.Append('[');
                for (int i = 0; i < arr.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    if (pretty) { sb.Append('\n'); Indent(sb, depth + 1); }
                    WriteValue(sb, arr[i], pretty, depth + 1);
                }
                if (pretty && arr.Count > 0) { sb.Append('\n'); Indent(sb, depth); }
                sb.Append(']');
                return;
            }
            sb.Append("null");
        }

        static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        // ---------- 取值助手 ----------
        public static Dictionary<string, object> Obj(object v) { return v as Dictionary<string, object>; }

        public static List<object> Arr(object v) { return v as List<object>; }

        public static string Str(Dictionary<string, object> d, string key, string def)
        {
            object v;
            if (d != null && d.TryGetValue(key, out v) && v is string) return (string)v;
            return def;
        }

        public static int Int(Dictionary<string, object> d, string key, int def)
        {
            object v;
            if (d != null && d.TryGetValue(key, out v))
            {
                if (v is double) return (int)Math.Round((double)v);
                if (v is int) return (int)v;
                if (v is string) { int i; if (int.TryParse((string)v, out i)) return i; }
            }
            return def;
        }

        public static bool Bool(Dictionary<string, object> d, string key, bool def)
        {
            object v;
            if (d != null && d.TryGetValue(key, out v))
            {
                if (v is bool) return (bool)v;
                if (v is double) return (double)v != 0;
            }
            return def;
        }

        public static double Num(Dictionary<string, object> d, string key, double def)
        {
            object v;
            if (d != null && d.TryGetValue(key, out v) && v is double) return (double)v;
            return def;
        }

        public static object Set(string key, object value)
        {
            // 仅用于构造小对象时的可读性
            return value;
        }

        public static Dictionary<string, object> NewObj()
        {
            return new Dictionary<string, object>();
        }
    }
}
