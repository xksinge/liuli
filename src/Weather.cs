// Liuli · 天气：Open-Meteo（无需 key），结果落盘缓存；断网时用缓存，核心功能不依赖联网。
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace Liuli
{
    sealed class WeatherNow
    {
        public bool Has;
        public int Temp;
        public string Cond = "";
        public int Code;
        public int Humidity;
        public double Wind;
        public DateTime At = DateTime.MinValue;
    }

    sealed class WeatherService
    {
        public WeatherNow Now = new WeatherNow();
        public bool Offline;
        public bool Busy;
        string city = "道孚";
        double lat = 30.98, lon = 101.12;
        long lastTry;
        long lastOk;

        public string City { get { return city; } }
        public double Lat { get { return lat; } }
        public double Lon { get { return lon; } }

        static readonly Dictionary<string, double[]> cities = BuildCities();

        static Dictionary<string, double[]> BuildCities()
        {
            Dictionary<string, double[]> d = new Dictionary<string, double[]>();
            Add(d, "道孚", 30.98, 101.12); Add(d, "康定", 30.05, 101.96); Add(d, "成都", 30.67, 104.06);
            Add(d, "拉萨", 29.65, 91.14); Add(d, "北京", 39.90, 116.41); Add(d, "上海", 31.23, 121.47);
            Add(d, "广州", 23.13, 113.26); Add(d, "杭州", 30.27, 120.15); Add(d, "重庆", 29.56, 106.55);
            Add(d, "西安", 34.34, 108.94); Add(d, "昆明", 25.04, 102.71); Add(d, "乌鲁木齐", 43.83, 87.62);
            Add(d, "哈尔滨", 45.80, 126.53); Add(d, "武汉", 30.59, 114.31); Add(d, "深圳", 22.54, 114.06);
            return d;
        }

        static void Add(Dictionary<string, double[]> d, string name, double la, double lo)
        {
            d[name] = new double[] { la, lo };
        }

        public static bool Lookup(string name, out double la, out double lo)
        {
            la = 0; lo = 0;
            if (string.IsNullOrEmpty(name)) return false;
            string key = name.Trim();
            double[] v;
            if (cities.TryGetValue(key, out v)) { la = v[0]; lo = v[1]; return true; }
            foreach (KeyValuePair<string, double[]> kv in cities)
            {
                if (kv.Key.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf(kv.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    la = kv.Value[0]; lo = kv.Value[1]; return true;
                }
            }
            return false;
        }

        public void Configure(string name, double la, double lo)
        {
            city = name; lat = la; lon = lo;
        }

        public void LoadCache()
        {
            string text = Util.ReadAllTextSafe(Paths.WeatherFile);
            if (text == null) return;
            try
            {
                Dictionary<string, object> d = Json.Obj(Json.Parse(text));
                if (d == null) return;
                Now.Temp = Json.Int(d, "temp", 0);
                Now.Code = Json.Int(d, "code", 0);
                Now.Humidity = Json.Int(d, "humidity", 0);
                Now.Wind = Json.Num(d, "wind", 0);
                Now.Cond = Json.Str(d, "cond", "");
                Now.Has = Now.Cond.Length > 0;
                city = Json.Str(d, "city", city);
                lat = Json.Num(d, "lat", lat);
                lon = Json.Num(d, "lon", lon);
                string at = Json.Str(d, "at", "");
                DateTime t;
                if (DateTime.TryParse(at, out t)) Now.At = t;
            }
            catch { }
        }

        void SaveCache()
        {
            try
            {
                Dictionary<string, object> d = new Dictionary<string, object>();
                d["temp"] = Now.Temp; d["code"] = Now.Code; d["humidity"] = Now.Humidity; d["wind"] = Now.Wind;
                d["cond"] = Now.Cond; d["city"] = city; d["lat"] = lat; d["lon"] = lon;
                d["at"] = Now.At.ToString("yyyy-MM-dd HH:mm:ss");
                Paths.Ensure();
                Util.WriteAllTextAtomic(Paths.WeatherFile, Json.Write(d, true));
            }
            catch { }
        }

        public void Refresh(bool force)
        {
            long now = Environment.TickCount;
            if (Busy) return;
            if (!force && now - lastTry < 30 * 60 * 1000) return;
            Busy = true;
            lastTry = now;
            ThreadPool.QueueUserWorkItem(delegate { DoFetch(force); });
        }

        void DoFetch(bool force)
        {
            string url = string.Format("https://api.open-meteo.com/v1/forecast?latitude={0:0.####}&longitude={1:0.####}&current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m&timezone=auto",
                lat, lon);
            HttpWebRequest req = null;
            try
            {
                try { ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; } catch { }
                req = (HttpWebRequest)WebRequest.Create(url);
                req.Timeout = 9000;
                req.ReadWriteTimeout = 9000;
                req.UserAgent = "Liuli/1.0 (desktop widget)";
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (Stream s = resp.GetResponseStream())
                using (StreamReader r = new StreamReader(s, Encoding.UTF8))
                {
                    string text = r.ReadToEnd();
                    Dictionary<string, object> root = Json.Obj(Json.Parse(text));
                    Dictionary<string, object> cur = Json.Obj(root == null ? null : root["current"]);
                    if (cur == null) throw new Exception("no current block");
                    Now.Temp = (int)Math.Round(Json.Num(cur, "temperature_2m", 0));
                    Now.Humidity = Json.Int(cur, "relative_humidity_2m", 0);
                    Now.Code = Json.Int(cur, "weather_code", 0);
                    Now.Wind = Json.Num(cur, "wind_speed_10m", 0) / 3.6;   // km/h -> m/s
                    Now.Cond = Text(Now.Code);
                    Now.Has = true;
                    Now.At = DateTime.Now;
                    Offline = false;
                    lastOk = Environment.TickCount;
                    SaveCache();
                    Log.Info("weather", "OK " + city + " " + Now.Temp + "° " + Now.Cond);
                }
            }
            catch (Exception ex)
            {
                Offline = true;
                Log.Warn("weather", "拉取失败（保留缓存）：" + ex.Message);
            }
            finally
            {
                Busy = false;
                App.UI(delegate
                {
                    App.MarkStatusDirty();
                    App.RequestFrames();
                });
            }
        }

        static string Text(int code)
        {
            if (code == 0) return "晴";
            if (code <= 2) return "少云";
            if (code == 3) return "阴";
            if (code == 45 || code == 48) return "雾";
            if (code >= 51 && code <= 57) return "毛毛雨";
            if (code >= 61 && code <= 67) return "雨";
            if (code >= 71 && code <= 77) return "雪";
            if (code >= 80 && code <= 82) return "阵雨";
            if (code >= 85 && code <= 86) return "阵雪";
            if (code >= 95) return "雷雨";
            return "多云";
        }

        public static void Glyph(Graphics g, int code, Rectangle r, Color c)
        {
            float s = Math.Min(r.Width, r.Height);
            float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
            using (Pen pen = new Pen(c, Math.Max(1.4f, s * 0.055f)))
            {
                pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                bool sun = code == 0 || code == 1;
                bool rain = (code >= 51 && code <= 67) || (code >= 80 && code <= 82) || code >= 95;
                bool snow = (code >= 71 && code <= 77) || (code >= 85 && code <= 86);
                bool fog = code == 45 || code == 48;
                if (sun)
                {
                    g.DrawEllipse(pen, cx - s * 0.20f, cy - s * 0.20f, s * 0.40f, s * 0.40f);
                    for (int i = 0; i < 8; i++)
                    {
                        float a = (float)(Math.PI / 4 * i);
                        g.DrawLine(pen, cx + (float)Math.Cos(a) * s * 0.30f, cy + (float)Math.Sin(a) * s * 0.30f,
                            cx + (float)Math.Cos(a) * s * 0.42f, cy + (float)Math.Sin(a) * s * 0.42f);
                    }
                    return;
                }
                // 云
                using (SolidBrush b = new SolidBrush(Color.FromArgb(c.A, c)))
                {
                    g.FillEllipse(b, cx - s * 0.30f, cy - s * 0.10f, s * 0.34f, s * 0.26f);
                    g.FillEllipse(b, cx - s * 0.10f, cy - s * 0.24f, s * 0.34f, s * 0.34f);
                    g.FillEllipse(b, cx + s * 0.06f, cy - s * 0.08f, s * 0.28f, s * 0.24f);
                    g.FillRectangle(b, cx - s * 0.26f, cy + s * 0.04f, s * 0.56f, s * 0.10f);
                }
                if (rain)
                {
                    for (int i = 0; i < 3; i++)
                        g.DrawLine(pen, cx - s * 0.18f + i * s * 0.17f, cy + s * 0.20f, cx - s * 0.12f + i * s * 0.17f, cy + s * 0.38f);
                }
                else if (snow)
                {
                    for (int i = 0; i < 3; i++)
                        g.DrawLine(pen, cx - s * 0.18f + i * s * 0.17f, cy + s * 0.22f, cx - s * 0.18f + i * s * 0.17f, cy + s * 0.36f);
                }
                else if (fog)
                {
                    for (int i = 0; i < 3; i++)
                        g.DrawLine(pen, cx - s * 0.26f, cy + s * 0.14f + i * s * 0.08f, cx + s * 0.26f, cy + s * 0.14f + i * s * 0.08f);
                }
                if (code >= 95)
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(230, c))) { }
                    g.DrawLine(pen, cx + s * 0.02f, cy + s * 0.16f, cx - s * 0.04f, cy + s * 0.30f);
                    g.DrawLine(pen, cx - s * 0.04f, cy + s * 0.30f, cx + s * 0.06f, cy + s * 0.30f);
                }
            }
        }
    }
}
