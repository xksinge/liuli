// Liuli · 数据模型：看板、条目、设置；含默认工作区与边界收敛。
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Liuli
{
    enum BoardKind { Note = 0, Todo = 1, Files = 2, Code = 3, Status = 4, Clock = 5 }

    static class Kinds
    {
        public static readonly string[] Names = { "便签", "待办", "文件", "代码", "状态", "时钟" };
        public static readonly string[] Hints = { "随手写下的短句", "今天要做完的事", "常用文件与文件夹", "收藏的代码片段", "天气 · 系统占用", "时间与日期" };

        public static string Name(BoardKind k) { return Names[(int)k]; }
        public static string Hint(BoardKind k) { return Hints[(int)k]; }
        public static Glyph Icon(BoardKind k)
        {
            switch (k)
            {
                case BoardKind.Todo: return Glyph.Todo;
                case BoardKind.Files: return Glyph.Folder;
                case BoardKind.Code: return Glyph.Code;
                case BoardKind.Status: return Glyph.Pulse;
                case BoardKind.Clock: return Glyph.Clock;
                default: return Glyph.Note;
            }
        }

        public static BoardKind FromName(string s)
        {
            for (int i = 0; i < Names.Length; i++) if (Names[i] == s) return (BoardKind)i;
            return BoardKind.Note;
        }
    }

    sealed class Item
    {
        public string Text = "";
        public string Path = "";
        public string Extra = "";
        public bool Done;

        public Item() { }
        public Item(string text) { Text = text; }
        public Item(string text, string path) { Text = text; Path = path; }

        public Item Clone()
        {
            Item it = new Item();
            it.Text = Text; it.Path = Path; it.Extra = Extra; it.Done = Done;
            return it;
        }
    }

    sealed class Board
    {
        public string Id = "";
        public BoardKind Kind = BoardKind.Note;
        public string Title = "便签";
        public int X, Y, W = 330, H = 260;
        public int Accent = -1;            // -1 = 跟随壁纸
        public int Opacity = 100;          // 0..100
        public bool Pinned;
        public bool Locked;
        public bool Collapsed;
        public bool CollapseAllowed = true;
        public int Z;

        public string Text = "";           // Note 正文 / Status 副标题
        public string City = "道孚";
        public double Lat = 30.98, Lon = 101.12;
        public bool ShowWeather = true;
        public bool ShowStats = true;

        public List<Item> Items = new List<Item>();

        // 运行时（不序列化）
        public int Scroll;
        public int AnimStartX, AnimStartY, AnimW, AnimH;

        public Rectangle Bounds { get { return new Rectangle(X, Y, W, H); } set { X = value.X; Y = value.Y; W = value.Width; H = value.Height; } }

        public string NewId()
        {
            Id = "b" + Guid.NewGuid().ToString("N").Substring(0, 9);
            return Id;
        }

        public void Clamp(Rectangle[] screens)
        {
            if (W < 240) W = 240;
            if (H < 140) H = 140;
            bool any = false;
            for (int i = 0; i < screens.Length; i++)
            {
                Rectangle s = screens[i];
                Rectangle inter = Rectangle.Intersect(s, Bounds);
                if (inter.Width >= 80 && inter.Height >= 48) { any = true; break; }
            }
            if (!any && screens.Length > 0)
            {
                Rectangle s = screens[0];
                X = s.X + 40;
                Y = s.Y + 40;
                W = Math.Min(W, s.Width - 80);
                H = Math.Min(H, s.Height - 80);
            }
            Opacity = Util.Clamp(Opacity, 45, 100);
            if (Accent < -1) Accent = -1;
            if (Accent > 5) Accent = 5;
        }

        public Board Clone()
        {
            Board b = new Board();
            b.Id = Id; b.Kind = Kind; b.Title = Title; b.X = X; b.Y = Y; b.W = W; b.H = H;
            b.Accent = Accent; b.Opacity = Opacity; b.Pinned = Pinned; b.Locked = Locked; b.Collapsed = Collapsed;
            b.Text = Text; b.City = City; b.Lat = Lat; b.Lon = Lon; b.ShowWeather = ShowWeather; b.ShowStats = ShowStats;
            b.Z = Z;
            b.Items = new List<Item>();
            for (int i = 0; i < Items.Count; i++) b.Items.Add(Items[i].Clone());
            return b;
        }
    }

    sealed class Settings
    {
        public int GlassPct = 100;
        public int BlurPct = 62;
        public int Radius = 22;
        public bool LiveGlass;
        public string Tone = "auto";        // auto | dark | light
        public bool Snap = true;
        public bool AlwaysTop;
        public bool ShowBar = true;
        public bool HideDesktopIcons;
        public bool AutoStart;
        public bool WeatherOn = true;
        public string WallMode = "system";  // system | image | video
        public string WallPath = "";
        public int WallDim = 30;            // 动态壁纸压暗 0..80
        public bool WallMute = true;
        public bool WallPause = true;       // 全屏/闲置暂停
        public bool FirstRun = true;
        public int BarX = -1, BarY = -1;
        public bool Diagnose;
        public bool HideOnFullscreen = true;
        public bool OrganizeMove;           // 整理时真实移动桌面原件（桌面不再保留副本）

        public int GlassAlphaPct { get { return Util.Clamp(GlassPct, 0, 100); } }
        public int BlurRadius { get { return Util.Clamp(BlurPct, 0, 100); } }
        public int CornerRadius { get { return Util.Clamp(Radius, 10, 30); } }
    }

    sealed class Workspace
    {
        public Settings Set = new Settings();
        public List<Board> Boards = new List<Board>();

        public static Workspace Default(Rectangle[] screens)
        {
            Workspace ws = new Workspace();
            Rectangle s = screens.Length > 0 ? screens[0] : new Rectangle(0, 0, 1920, 1080);
            int left = s.X + 32;
            int top = s.Y + 32;

            Board note = new Board();
            note.NewId(); note.Kind = BoardKind.Note; note.Title = "灵感便签";
            note.X = left; note.Y = top; note.W = 330; note.H = 300; note.Z = 1;
            note.Text = "把桌面变成一块会呼吸的玻璃。\n\n双击这里开始写，Ctrl+Enter 保存。\n拖标题栏可以换位置，拖边缘可以改大小。";
            ws.Boards.Add(note);

            Board todo = new Board();
            todo.NewId(); todo.Kind = BoardKind.Todo; todo.Title = "今日清单";
            todo.X = left + note.W + 16; todo.Y = top; todo.W = 300; todo.H = 300; todo.Z = 2;
            todo.Items.Add(new Item("整理桌面图标，收进看板", ""));
            todo.Items.Add(new Item("给玻璃看板挑一张壁纸"));
            todo.Items.Add(new Item("写一条今天的代码片段"));
            ws.Boards.Add(todo);

            Board status = new Board();
            status.NewId(); status.Kind = BoardKind.Status; status.Title = "桌面状态";
            status.X = left; status.Y = top + 300 + 16; status.W = 330; status.H = 300; status.Z = 3;
            ws.Boards.Add(status);

            Board clock = new Board();
            clock.NewId(); clock.Kind = BoardKind.Clock; clock.Title = "时间";
            clock.X = left + note.W + 16; clock.Y = top + 300 + 16; clock.W = 320; clock.H = 160; clock.Z = 4;
            ws.Boards.Add(clock);

            return ws;
        }

        public Board Find(string id)
        {
            for (int i = 0; i < Boards.Count; i++) if (Boards[i].Id == id) return Boards[i];
            return null;
        }

        public void ClampAll(Rectangle[] screens)
        {
            for (int i = 0; i < Boards.Count; i++) Boards[i].Clamp(screens);
        }

        public Workspace Clone()
        {
            Workspace ws = new Workspace();
            ws.Set = Set;
            for (int i = 0; i < Boards.Count; i++) ws.Boards.Add(Boards[i].Clone());
            return ws;
        }
    }

    static class Palette
    {
        // 与壁纸无关的六套强调色（浅色主题下自动压暗）
        static readonly Color[] dark = {
            Util.Rgb(126, 178, 255),  // 极光蓝
            Util.Rgb(122, 225, 196),  // 薄荷
            Util.Rgb(186, 158, 255),  // 雾紫
            Util.Rgb(255, 198, 120),  // 琥珀
            Util.Rgb(255, 154, 178),  // 玫瑰
            Util.Rgb(176, 190, 208)   // 石墨
        };

        public static Color Get(int index, bool light)
        {
            if (index < 0) return Theme.Accent;
            int i = Util.Clamp(index, 0, dark.Length - 1);
            Color c = dark[i];
            if (!light) return c;
            float h, s, l;
            Util.RgbToHsl(c, out h, out s, out l);
            return Util.Hsl(h, Math.Min(0.66f, s * 0.95f), 0.44f);
        }

        public static int Count { get { return dark.Length; } }
    }
}
