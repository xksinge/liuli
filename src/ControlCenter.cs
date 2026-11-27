// Liuli v2 · 控制中心数据：音量 / 网络 / 蓝牙 / 亮度，全部按“失败即隐藏”降级。
using System;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace Liuli
{
    sealed class ControlSnapshot
    {
        public bool VolumeOk;
        public int Volume;
        public bool Mute;
        public bool NetworkOk;
        public string Network = "未连接";
        public bool BluetoothOk;
        public string Bluetooth = "";
        public bool BrightnessOk;
        public int Brightness;
    }

    static class ControlCenter
    {
        static IAudioEndpointVolume volume;
        static bool audioTried;
        static bool audioFailed;

        public static ControlSnapshot Sample()
        {
            ControlSnapshot s = new ControlSnapshot();
            SampleAudio(s);
            SampleNetwork(s);
            SampleBluetooth(s);
            SampleBrightness(s);
            return s;
        }

        static bool EnsureAudio()
        {
            if (audioTried && !audioFailed && volume != null) return true;
            if (audioFailed) return false;
            audioTried = true;
            try
            {
                IMMDeviceEnumerator en = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
                IMMDevice dev;
                int hr = en.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out dev);
                if (hr != 0 || dev == null) throw new Exception("没有默认音频输出");
                Guid iid = AudioEndpointVolumeIid;
                object iface;
                hr = dev.Activate(ref iid, 23, IntPtr.Zero, out iface);
                if (hr != 0 || iface == null) throw new Exception("激活音量接口失败");
                volume = (IAudioEndpointVolume)iface;
                return true;
            }
            catch (Exception ex)
            {
                audioFailed = true;
                volume = null;
                Log.Warn("control", "音量接口不可用 " + ex.Message);
                return false;
            }
        }

        static void SampleAudio(ControlSnapshot s)
        {
            try
            {
                if (!EnsureAudio()) return;
                float v = 0f;
                bool mute = false;
                volume.GetMasterVolumeLevelScalar(out v);
                volume.GetMute(out mute);
                s.Volume = Util.Clamp(Util.RoundI(v * 100f), 0, 100);
                s.Mute = mute;
                s.VolumeOk = true;
            }
            catch (Exception ex)
            {
                audioFailed = true;
                volume = null;
                Log.Warn("control", "读取音量失败 " + ex.Message);
            }
        }

        public static bool ToggleMute()
        {
            try
            {
                if (!EnsureAudio()) return false;
                bool mute = false;
                volume.GetMute(out mute);
                Guid ctx = Guid.Empty;
                volume.SetMute(!mute, ref ctx);
                Log.Info("control", "静音=" + (!mute));
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn("control", "切换静音失败 " + ex.Message);
                return false;
            }
        }

        public static bool StepVolume(int delta)
        {
            try
            {
                if (!EnsureAudio()) return false;
                float v = 0f;
                volume.GetMasterVolumeLevelScalar(out v);
                float next = v + delta / 100f;
                if (next < 0f) next = 0f;
                if (next > 1f) next = 1f;
                Guid ctx = Guid.Empty;
                volume.SetMasterVolumeLevelScalar(next, ref ctx);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn("control", "调节音量失败 " + ex.Message);
                return false;
            }
        }

        static void SampleNetwork(ControlSnapshot s)
        {
            try
            {
                if (!NetworkInterface.GetIsNetworkAvailable())
                {
                    s.Network = "未连接";
                    s.NetworkOk = true;
                    return;
                }
                NetworkInterface[] list = NetworkInterface.GetAllNetworkInterfaces();
                for (int i = 0; i < list.Length; i++)
                {
                    NetworkInterface ni = list[i];
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) s.Network = "Wi-Fi 已连接";
                    else if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet) s.Network = "以太网已连接";
                    else s.Network = "已连接";
                    s.NetworkOk = true;
                    return;
                }
                s.Network = "未连接";
                s.NetworkOk = true;
            }
            catch (Exception ex)
            {
                Log.Warn("control", "读取网络失败 " + ex.Message);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct BLUETOOTH_RADIO_INFO
        {
            public uint dwSize;
            public ulong address;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 248)]
            public string szName;
            public uint lmpSubversion;
            public ushort manufacturer;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct BLUETOOTH_FIND_RADIO_PARAMS
        {
            public uint dwSize;
        }

        [DllImport("bthprops.cpl", SetLastError = true)]
        static extern IntPtr BluetoothFindFirstRadio(ref BLUETOOTH_FIND_RADIO_PARAMS p, out IntPtr radio);
        [DllImport("bthprops.cpl", SetLastError = true)]
        static extern bool BluetoothFindRadioClose(IntPtr find);
        [DllImport("bthprops.cpl", SetLastError = true)]
        static extern bool BluetoothGetRadioInfo(IntPtr radio, ref BLUETOOTH_RADIO_INFO info);
        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr h);

        static void SampleBluetooth(ControlSnapshot s)
        {
            IntPtr find = IntPtr.Zero;
            IntPtr radio = IntPtr.Zero;
            try
            {
                BLUETOOTH_FIND_RADIO_PARAMS p = new BLUETOOTH_FIND_RADIO_PARAMS();
                p.dwSize = (uint)Marshal.SizeOf(typeof(BLUETOOTH_FIND_RADIO_PARAMS));
                find = BluetoothFindFirstRadio(ref p, out radio);
                if (find == IntPtr.Zero || radio == IntPtr.Zero) return;
                BLUETOOTH_RADIO_INFO info = new BLUETOOTH_RADIO_INFO();
                info.dwSize = (uint)Marshal.SizeOf(typeof(BLUETOOTH_RADIO_INFO));
                if (BluetoothGetRadioInfo(radio, ref info) && info.szName != null && info.szName.Length > 0)
                    s.Bluetooth = info.szName;
                else s.Bluetooth = "已检测到蓝牙";
                s.BluetoothOk = true;
            }
            catch (Exception ex)
            {
                Log.Warn("control", "读取蓝牙失败 " + ex.Message);
            }
            finally
            {
                if (find != IntPtr.Zero) { try { BluetoothFindRadioClose(find); } catch { } }
                if (radio != IntPtr.Zero) { try { CloseHandle(radio); } catch { } }
            }
        }

        static bool brightnessTried;
        static bool brightnessFailed;
        static int brightnessValue;

        static void SampleBrightness(ControlSnapshot s)
        {
            if (brightnessFailed) return;
            if (brightnessTried)
            {
                s.BrightnessOk = true;
                s.Brightness = brightnessValue;
                return;
            }
            brightnessTried = true;
            ManagementObjectSearcher searcher = null;
            try
            {
                searcher = new ManagementObjectSearcher(
                    "root\\WMI", "SELECT CurrentBrightness FROM WmiMonitorBrightness");
                foreach (ManagementObject mo in searcher.Get())
                {
                    object v = mo["CurrentBrightness"];
                    if (v != null)
                    {
                        brightnessValue = Util.Clamp(Convert.ToInt32(v), 0, 100);
                        s.Brightness = brightnessValue;
                        s.BrightnessOk = true;
                        return;
                    }
                }
                brightnessFailed = true;
            }
            catch (Exception ex)
            {
                brightnessFailed = true;
                Log.Warn("control", "亮度接口不可用 " + ex.Message);
            }
            finally
            {
                if (searcher != null) searcher.Dispose();
            }
        }

        public static bool StepBrightness(int delta)
        {
            if (brightnessFailed) return false;
            ManagementObjectSearcher searcher = null;
            try
            {
                searcher = new ManagementObjectSearcher(
                    "root\\WMI", "SELECT * FROM WmiMonitorBrightnessMethods");
                foreach (ManagementObject mo in searcher.Get())
                {
                    mo.InvokeMethod("WmiSetBrightness", new object[] { (uint)1, (byte)delta });
                    brightnessValue = delta;
                    return true;
                }
                brightnessFailed = true;
            }
            catch (Exception ex)
            {
                brightnessFailed = true;
                Log.Warn("control", "调节亮度失败 " + ex.Message);
            }
            finally
            {
                if (searcher != null) searcher.Dispose();
            }
            return false;
        }

        static readonly Guid AudioEndpointVolumeIid = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");

        enum EDataFlow { eRender = 0, eCapture = 1, eAll = 2 }
        enum ERole { eConsole = 0, eMultimedia = 1, eCommunications = 2 }

        [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        class MMDeviceEnumeratorComObject { }

        [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IMMDeviceEnumerator
        {
            [PreserveSig] int EnumAudioEndpoints(EDataFlow dataFlow, int stateMask, out object devices);
            [PreserveSig] int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice endpoint);
            [PreserveSig] int GetDevice(string id, out IMMDevice device);
            [PreserveSig] int RegisterEndpointNotificationCallback(IntPtr client);
            [PreserveSig] int UnregisterEndpointNotificationCallback(IntPtr client);
        }

        [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IMMDevice
        {
            [PreserveSig] int Activate(ref Guid iid, int clsCtx, IntPtr activationParams,
                [MarshalAs(UnmanagedType.IUnknown)] out object iface);
            [PreserveSig] int OpenPropertyStore(int access, out object store);
            [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
            [PreserveSig] int GetState(out int state);
        }

        [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IAudioEndpointVolume
        {
            [PreserveSig] int RegisterControlChangeNotify(IntPtr notify);
            [PreserveSig] int UnregisterControlChangeNotify(IntPtr notify);
            [PreserveSig] int GetChannelCount(out int count);
            [PreserveSig] int SetMasterVolumeLevel(float levelDb, ref Guid ctx);
            [PreserveSig] int SetMasterVolumeLevelScalar(float level, ref Guid ctx);
            [PreserveSig] int GetMasterVolumeLevel(out float levelDb);
            [PreserveSig] int GetMasterVolumeLevelScalar(out float level);
            [PreserveSig] int SetChannelVolumeLevel(uint channel, float levelDb, ref Guid ctx);
            [PreserveSig] int SetChannelVolumeLevelScalar(uint channel, float level, ref Guid ctx);
            [PreserveSig] int GetChannelVolumeLevel(uint channel, out float levelDb);
            [PreserveSig] int GetChannelVolumeLevelScalar(uint channel, out float level);
            [PreserveSig] int SetMute(bool mute, ref Guid ctx);
            [PreserveSig] int GetMute(out bool mute);
            [PreserveSig] int GetVolumeStepInfo(out uint step, out uint stepCount);
            [PreserveSig] int VolumeStepUp(ref Guid ctx);
            [PreserveSig] int VolumeStepDown(ref Guid ctx);
            [PreserveSig] int QueryHardwareSupport(out uint mask);
            [PreserveSig] int GetVolumeRange(out float min, out float max, out float inc);
        }
    }
}
