// Liuli · OLE 拖放目标：分层窗口只靠 DragAcceptFiles 在部分场景收不到 WM_DROPFILES，
// 直接注册 IDropTarget，让资源管理器的拖拽稳定落到看板上。
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Liuli
{
    [ComVisible(true)]
    public sealed class DropTarget : IDropTarget
    {
        public const uint COPY = 1, MOVE = 2;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINTL { public int x; public int y; }

        readonly Action<string[]> onDrop;
        readonly string tag;

        public DropTarget(string tag, Action<string[]> onDrop)
        {
            this.tag = tag;
            this.onDrop = onDrop;
        }

        static string[] Files(IDataObject data)
        {
            try
            {
                System.Windows.Forms.IDataObject w = data as System.Windows.Forms.IDataObject;
                if (w == null || !w.GetDataPresent(System.Windows.Forms.DataFormats.FileDrop)) return null;
                return (string[])w.GetData(System.Windows.Forms.DataFormats.FileDrop);
            }
            catch { return null; }
        }

        public void DragEnter(IDataObject pDataObj, uint grfKeyState, POINTL pt, ref uint pdwEffect)
        {
            string[] files = Files(pDataObj);
            if (files != null && files.Length > 0)
            {
                pdwEffect = MOVE;
                Log.Info("drop", "OLE 进入 " + tag + "，" + files.Length + " 项");
            }
            else pdwEffect = 0;
        }

        public void DragOver(uint grfKeyState, POINTL pt, ref uint pdwEffect)
        {
            pdwEffect = MOVE;
        }

        public void DragLeave()
        {
        }

        public void Drop(IDataObject pDataObj, uint grfKeyState, POINTL pt, ref uint pdwEffect)
        {
            string[] files = Files(pDataObj);
            if (files != null && files.Length > 0)
            {
                Log.Info("drop", "OLE 放下 " + tag + "，" + files.Length + " 项");
                onDrop(files);
                pdwEffect = MOVE;
            }
            else pdwEffect = 0;
        }

        [DllImport("ole32.dll")]
        public static extern int RegisterDragDrop(IntPtr hwnd, IDropTarget target);

        [DllImport("ole32.dll")]
        public static extern int RevokeDragDrop(IntPtr hwnd);

        [DllImport("ole32.dll")]
        public static extern int OleInitialize(IntPtr reserved);

        static bool oleReady;

        /// <summary>拖放前确保本线程已做 OLE 初始化，否则 RegisterDragDrop 会失败。</summary>
        public static void EnsureOle()
        {
            if (oleReady) return;
            try { OleInitialize(IntPtr.Zero); } catch { }
            oleReady = true;
        }
    }

    [ComImport, Guid("00000122-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDropTarget
    {
        void DragEnter([MarshalAs(UnmanagedType.Interface)] IDataObject pDataObj, uint grfKeyState, DropTarget.POINTL pt, ref uint pdwEffect);
        void DragOver(uint grfKeyState, DropTarget.POINTL pt, ref uint pdwEffect);
        void DragLeave();
        void Drop([MarshalAs(UnmanagedType.Interface)] IDataObject pDataObj, uint grfKeyState, DropTarget.POINTL pt, ref uint pdwEffect);
    }
}
