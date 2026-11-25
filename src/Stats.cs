// Liuli · 系统读数：CPU / 内存 / 磁盘，2 秒采样一次，只做增量，不枚举进程。
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Liuli
{
    sealed class StatsNow
    {
        public float Cpu;
        public int MemPct;
        public int DiskPct;
        public string CpuText = "—";
        public string MemText = "—";
        public string DiskText = "—";
    }

    static class SysStats
    {
        static long prevIdle, prevKernel, prevUser;
        static bool inited;

        public static StatsNow Sample()
        {
            StatsNow n = new StatsNow();
            try
            {
                long idle, kernel, user;
                if (Native.GetSystemTimes(out idle, out kernel, out user))
                {
                    if (!inited) { prevIdle = idle; prevKernel = kernel; prevUser = user; inited = true; }
                    long di = idle - prevIdle, dk = kernel - prevKernel, du = user - prevUser;
                    prevIdle = idle; prevKernel = kernel; prevUser = user;
                    long total = dk + du;
                    if (total > 0)
                    {
                        int busy = (int)Math.Round(100.0 * (total - di) / total);
                        n.Cpu = Util.Clamp(busy, 0, 100);
                        n.CpuText = busy + "%";
                    }
                    else { n.Cpu = 0; n.CpuText = "0%"; }
                }
            }
            catch { }

            try
            {
                Native.MEMORYSTATUSEX m = new Native.MEMORYSTATUSEX();
                m.dwLength = (uint)Marshal.SizeOf(m);
                Native.GlobalMemoryStatusEx(ref m);
                n.MemPct = (int)m.dwMemoryLoad;
                double used = (m.ullTotalPhys - m.ullAvailPhys) / 1073741824.0;
                double total = m.ullTotalPhys / 1073741824.0;
                n.MemText = used.ToString("0.0") + " / " + total.ToString("0") + " GB";
            }
            catch { }

            try
            {
                DriveInfo d = new DriveInfo("C:");
                long total = d.TotalSize, free = d.TotalFreeSpace;
                n.DiskPct = total > 0 ? (int)(100 - free * 100 / total) : 0;
                n.DiskText = "空闲 " + Util.FormatBytes(free);
            }
            catch { }

            return n;
        }
    }
}
