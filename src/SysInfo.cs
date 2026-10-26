using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace KDT
{
    internal class Sample
    {
        public float Cpu = 0;
        public float RamPct = 0;
        public double RamUsedGb = 0;
        public double RamTotalGb = 0;
        public float Gpu = -1;
        public double FreeGb = 0;
        public double TotalGb = 0;
    }

    internal class PingStat
    {
        public int Avg = -1;
        public int Jitter = -1;
        public int Loss = 0;
        public bool Ok = false;
    }

    internal static class SysInfo
    {
        public static string CpuName = "";
        public static string GpuName = "";
        public static string OsName = "";
        public static string Machine = "";
        public static string Board = "";
        public static int Cores = 0;
        public static int Threads = 0;
        public static double RamTotalGb = 0;
        public static bool HardwareReady = false;

        static bool _gpuOk = true;
        static float _lastGpu = -1;

        [StructLayout(LayoutKind.Sequential)]
        struct FileTime
        {
            public uint Low;
            public uint High;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MemoryStatusEx
        {
            public uint Length;
            public uint MemoryLoad;
            public ulong TotalPhys;
            public ulong AvailPhys;
            public ulong TotalPageFile;
            public ulong AvailPageFile;
            public ulong TotalVirtual;
            public ulong AvailVirtual;
            public ulong AvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetSystemTimes(out FileTime idle, out FileTime kernel, out FileTime user);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

        static ulong _prevIdle = 0;
        static ulong _prevKernel = 0;
        static ulong _prevUser = 0;
        static bool _haveCpuSample = false;

        static ulong ToU(FileTime f)
        {
            return ((ulong)f.High << 32) | (ulong)f.Low;
        }

        public static void LoadHardware()
        {
            try
            {
                using (ManagementObjectSearcher s = new ManagementObjectSearcher("SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor"))
                {
                    foreach (ManagementObject o in s.Get())
                    {
                        CpuName = Convert.ToString(o["Name"]).Trim();
                        Cores = Convert.ToInt32(o["NumberOfCores"]);
                        Threads = Convert.ToInt32(o["NumberOfLogicalProcessors"]);
                        break;
                    }
                }
            }
            catch (Exception ex) { Engine.Log("wmi cpu: " + ex.Message); }

            try
            {
                using (ManagementObjectSearcher s = new ManagementObjectSearcher("SELECT Name, AdapterRAM FROM Win32_VideoController"))
                {
                    long best = -1;
                    foreach (ManagementObject o in s.Get())
                    {
                        string n = Convert.ToString(o["Name"]);
                        long ram = 0;
                        try { ram = Convert.ToInt64(o["AdapterRAM"]); }
                        catch { }
                        if (n != null && (n.IndexOf("NVIDIA", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                          n.IndexOf("AMD", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                          n.IndexOf("Radeon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                          n.IndexOf("Intel", StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            if (ram >= best)
                            {
                                best = ram;
                                GpuName = n.Trim();
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Engine.Log("wmi gpu: " + ex.Message); }

            try
            {
                using (ManagementObjectSearcher s = new ManagementObjectSearcher("SELECT Caption, Version FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject o in s.Get())
                    {
                        OsName = Convert.ToString(o["Caption"]).Trim();
                        break;
                    }
                }
            }
            catch (Exception ex) { Engine.Log("wmi os: " + ex.Message); }

            try
            {
                using (ManagementObjectSearcher s = new ManagementObjectSearcher("SELECT Manufacturer, Product FROM Win32_BaseBoard"))
                {
                    foreach (ManagementObject o in s.Get())
                    {
                        Board = (Convert.ToString(o["Manufacturer"]) + " " + Convert.ToString(o["Product"])).Trim();
                        break;
                    }
                }
            }
            catch { }

            MemoryStatusEx m = new MemoryStatusEx();
            m.Length = (uint)Marshal.SizeOf(typeof(MemoryStatusEx));
            if (GlobalMemoryStatusEx(ref m))
                RamTotalGb = Math.Round(m.TotalPhys / 1073741824.0, 1);

            if (Cores <= 0) Cores = Environment.ProcessorCount;
            if (Threads <= 0) Threads = Environment.ProcessorCount;
            if (RamTotalGb <= 0) RamTotalGb = 0;
            if (string.IsNullOrEmpty(CpuName)) CpuName = "CPU";
            if (string.IsNullOrEmpty(GpuName)) GpuName = "GPU";
            if (string.IsNullOrEmpty(OsName)) OsName = "Windows";
            if (string.IsNullOrEmpty(Board)) Board = "-";
            Machine = Environment.MachineName;
            HardwareReady = true;
        }

        public static float ReadCpu()
        {
            FileTime idle, kernel, user;
            if (!GetSystemTimes(out idle, out kernel, out user)) return 0;
            ulong i = ToU(idle);
            ulong k = ToU(kernel);
            ulong u = ToU(user);
            if (!_haveCpuSample)
            {
                _prevIdle = i; _prevKernel = k; _prevUser = u;
                _haveCpuSample = true;
                return 0;
            }
            ulong total = (k - _prevKernel) + (u - _prevUser);
            ulong idleDelta = i - _prevIdle;
            _prevIdle = i; _prevKernel = k; _prevUser = u;
            if (total == 0) return 0;
            double busy = (double)(total - idleDelta) / (double)total * 100.0;
            if (busy < 0) busy = 0;
            if (busy > 100) busy = 100;
            return (float)busy;
        }

        public static void ReadRam(out double usedGb, out double totalGb, out float pct)
        {
            MemoryStatusEx m = new MemoryStatusEx();
            m.Length = (uint)Marshal.SizeOf(typeof(MemoryStatusEx));
            usedGb = 0; totalGb = 0; pct = 0;
            if (!GlobalMemoryStatusEx(ref m)) return;
            totalGb = m.TotalPhys / 1073741824.0;
            usedGb = (m.TotalPhys - m.AvailPhys) / 1073741824.0;
            if (m.TotalPhys > 0)
                pct = (float)((double)(m.TotalPhys - m.AvailPhys) / (double)m.TotalPhys * 100.0);
            pct = (float)Math.Round(pct, 0);
        }

        public static float ReadGpu()
        {
            if (!_gpuOk) return _lastGpu;
            try
            {
                double sum = 0;
                using (ManagementObjectSearcher s = new ManagementObjectSearcher(
                    "SELECT Name, UtilizationPercentage FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine"))
                {
                    using (ManagementObjectCollection col = s.Get())
                    {
                        foreach (ManagementObject o in col)
                        {
                            try
                            {
                                string name = Convert.ToString(o["Name"]);
                                if (name != null && name.IndexOf("engtype_3D", StringComparison.OrdinalIgnoreCase) >= 0)
                                    sum += Convert.ToDouble(o["UtilizationPercentage"]);
                            }
                            catch { }
                            finally { o.Dispose(); }
                        }
                    }
                }
                if (sum < 0) sum = 0;
                if (sum > 100) sum = 100;
                _lastGpu = (float)Math.Round(sum, 0);
                return _lastGpu;
            }
            catch (Exception ex)
            {
                _gpuOk = false;
                Engine.Log("gpu perf: " + ex.Message);
                return -1;
            }
        }

        public static Sample TakeSample(bool withGpu)
        {
            Sample s = new Sample();
            s.Cpu = ReadCpu();
            double used, total;
            float pct;
            ReadRam(out used, out total, out pct);
            s.RamUsedGb = Math.Round(used, 1);
            s.RamTotalGb = Math.Round(total, 1);
            s.RamPct = pct;
            if (withGpu) s.Gpu = ReadGpu();
            else s.Gpu = _lastGpu;
            try
            {
                DriveInfo d = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory));
                s.FreeGb = Math.Round(d.AvailableFreeSpace / 1073741824.0, 1);
                s.TotalGb = Math.Round(d.TotalSize / 1073741824.0, 1);
            }
            catch { }
            return s;
        }

        public static PingStat PingHost(string host, int samples)
        {
            PingStat st = new PingStat();
            List<int> times = new List<int>();
            int lost = 0;
            try
            {
                using (Ping p = new Ping())
                {
                    for (int i = 0; i < samples; i++)
                    {
                        try
                        {
                            PingReply r = p.Send(host, 1200);
                            if (r != null && r.Status == IPStatus.Success) times.Add((int)r.RoundtripTime);
                            else lost++;
                        }
                        catch { lost++; }
                    }
                }
            }
            catch { lost = samples; }
            if (times.Count == 0)
            {
                st.Loss = 100;
                st.Ok = false;
                return st;
            }
            double sum = 0;
            for (int i = 0; i < times.Count; i++) sum += times[i];
            st.Avg = (int)Math.Round(sum / times.Count);
            double jitter = 0;
            if (times.Count > 1)
            {
                for (int i = 1; i < times.Count; i++) jitter += Math.Abs(times[i] - times[i - 1]);
                jitter = jitter / (times.Count - 1);
            }
            st.Jitter = (int)Math.Round(jitter);
            st.Loss = (int)Math.Round(lost * 100.0 / samples);
            st.Ok = true;
            return st;
        }

        public static string Uptime()
        {
            try
            {
                TimeSpan t = TimeSpan.FromMilliseconds(Environment.TickCount);
                if (t.TotalDays >= 1)
                    return ((int)t.TotalDays) + "d " + t.Hours + "h " + t.Minutes + "m";
                return t.Hours + "h " + t.Minutes + "m";
            }
            catch
            {
                return "-";
            }
        }
    }
}
