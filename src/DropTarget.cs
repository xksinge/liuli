// Liuli · OLE 拖放目标：分层窗口只靠 DragAcceptFiles 在部分场景收不到 WM_DROPFILES，
// 直接注册 IDropTarget，让资源管理器的拖拽稳定落到看板上。
using System;
using System.Collections.Generic;
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
            // 直接按 CF_HDROP 读取：OLE 传进来的 IDataObject 不能可靠地转成 WinForms.IDataObject
            try
            {
                FORMATETC fe = new FORMATETC();
                fe.cfFormat = 15; // CF_HDROP
                fe.dwAspect = DVASPECT.DVASPECT_CONTENT;
                fe.lindex = -1;
                fe.tymed = TYMED.TYMED_HGLOBAL;
                STGMEDIUM stg;
                data.GetData(ref fe, out stg);
                if (stg.unionmember == IntPtr.Zero) return null;
                IntPtr p = GlobalLock(stg.unionmember);
                if (p == IntPtr.Zero) { ReleaseStgMedium(ref stg); return null; }
                try
                {
                    DROPFILES df = (DROPFILES)Marshal.PtrToStructure(p, typeof(DROPFILES));
                    IntPtr s = new IntPtr(p.ToInt64() + (long)df.pFiles);
                    List<string> list = new List<string>();
                    for (int i = 0; i < 64; i++)
                    {
                        string one = df.fWide != 0 ? Marshal.PtrToStringUni(s) : Marshal.PtrToStringAnsi(s);
                        if (one == null || one.Length == 0) break;
                        list.Add(one);
                        int bytes = one.Length + 1;
                        s = new IntPtr(s.ToInt64() + (df.fWide != 0 ? bytes * 2 : bytes));
                    }
                    return list.Count > 0 ? list.ToArray() : null;
                }
                finally
                {
                    GlobalUnlock(stg.unionmember);
                    ReleaseStgMedium(ref stg);
                }
            }
            catch (Exception ex)
            {
                Log.Warn("drop", "读取拖入文件失败 " + ex.Message);
                return null;
            }
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

        [StructLayout(LayoutKind.Sequential)]
        public struct DROPFILES { public uint pFiles; public int x; public int y; public int fNC; public int fWide; }

        [DllImport("kernel32.dll")]
        static extern IntPtr GlobalLock(IntPtr h);
        [DllImport("kernel32.dll")]
        static extern bool GlobalUnlock(IntPtr h);
        [DllImport("ole32.dll")]
        static extern void ReleaseStgMedium(ref STGMEDIUM medium);

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
