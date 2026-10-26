using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace KDT
{
    internal class BenchResult
    {
        public double MemNs = -1;
        public double DiskMs = -1;
        public double CpuMops = -1;
        public double PingMs = -1;
        public double JitterMs = -1;
        public string Stamp = "";

        public bool Any
        {
            get { return MemNs > 0 || DiskMs > 0 || CpuMops > 0 || PingMs >= 0; }
        }
    }

    // Local, repeatable measurements. No FPS promises: these numbers move when
    // the system actually changes, and every result is stored on this machine.
    internal static class Bench
    {
        public static string BaselinePath
        {
            get { return Path.Combine(Engine.DataDir, "bench-baseline.tsv"); }
        }

        public static string LastPath
        {
            get { return Path.Combine(Engine.DataDir, "bench-last.tsv"); }
        }

        // ---------------------------------------------------------------- run

        public static BenchResult Run(bool withNetwork)
        {
            BenchResult r = new BenchResult();
            r.Stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            try { r.MemNs = MemoryNs(); }
            catch (Exception ex) { Engine.Log("bench memory: " + ex.Message); r.MemNs = -1; }
            try { r.DiskMs = DiskRandomMs(); }
            catch (Exception ex) { Engine.Log("bench disk: " + ex.Message); r.DiskMs = -1; }
            try { r.CpuMops = CpuMops(); }
            catch (Exception ex) { Engine.Log("bench cpu: " + ex.Message); r.CpuMops = -1; }
            if (withNetwork)
            {
                try
                {
                    PingStat p = SysInfo.PingHost("1.1.1.1", 8);
                    if (p != null && p.Ok)
                    {
                        r.PingMs = p.Avg;
                        r.JitterMs = p.Jitter;
                    }
                }
                catch (Exception ex) { Engine.Log("bench ping: " + ex.Message); }
            }
            return r;
        }

        // 8M-entry pointer chase over 32 MB: larger than the Ryzen 5 3600 L3,
        // so the number reflects real memory latency rather than cache speed.
        static double MemoryNs()
        {
            const int n = 8 * 1024 * 1024;
            int[] a = new int[n];
            int[] order = new int[n];
            for (int i = 0; i < n; i++) order[i] = i;

            Random rnd = new Random(12345);
            for (int i = n - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                int t = order[i]; order[i] = order[j]; order[j] = t;
            }
            for (int i = 0; i < n; i++) a[order[i]] = order[(i + 1) % n];
            order = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();

            int idx = 0;
            for (int i = 0; i < 200000; i++) idx = a[idx];

            long sink = 0;
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < n; i++)
            {
                idx = a[idx];
                sink += idx;
            }
            sw.Stop();
            _sink += sink;
            return sw.Elapsed.TotalMilliseconds * 1000000.0 / n;
        }

        static long _sink;

        static double CpuMops()
        {
            const int iters = 60000000;
            double best = 0;
            for (int run = 0; run < 3; run++)
            {
                uint x = (uint)(0x9E3779B9 + run);
                Stopwatch sw = Stopwatch.StartNew();
                for (int i = 0; i < iters; i++) x = x * 1664525u + 1013904223u;
                sw.Stop();
                _sink += x;
                double mops = iters / sw.Elapsed.TotalSeconds / 1000000.0;
                if (mops > best) best = mops;
            }
            return best;
        }

        // Uncached 4K random reads through CreateFile with FILE_FLAG_NO_BUFFERING
        // so Windows' cache cannot make the disk look faster than it is.
        static double DiskRandomMs()
        {
            string path = Path.Combine(Engine.DataDir, "bench-disk.bin");
            const int size = 64 * 1024 * 1024;
            const int block = 4096;
            if (!File.Exists(path) || new FileInfo(path).Length != size)
            {
                byte[] buf = new byte[1024 * 1024];
                new Random(99).NextBytes(buf);
                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    for (int i = 0; i < size / buf.Length; i++) fs.Write(buf, 0, buf.Length);
                    fs.Flush(true);
                }
            }

            IntPtr h = CreateFileW(path, 0x80000000, 0x00000001, IntPtr.Zero, 3,
                0x20000000 | 0x10000000, IntPtr.Zero); // NO_BUFFERING | RANDOM_ACCESS
            if (h == new IntPtr(-1)) return -1;
            IntPtr mem = VirtualAlloc(IntPtr.Zero, new UIntPtr((uint)block), 0x1000 | 0x2000, 0x04);
            if (mem == IntPtr.Zero)
            {
                CloseHandle(h);
                return -1;
            }

            try
            {
                int blocks = size / block;
                Random rnd = new Random(4242);
                long pos;
                uint read;
                for (int i = 0; i < 24; i++)
                {
                    SetFilePointerEx(h, (long)rnd.Next(blocks) * block, out pos, 0);
                    ReadFile(h, mem, (uint)block, out read, IntPtr.Zero);
                }
                const int reads = 800;
                Stopwatch sw = Stopwatch.StartNew();
                for (int i = 0; i < reads; i++)
                {
                    SetFilePointerEx(h, (long)rnd.Next(blocks) * block, out pos, 0);
                    ReadFile(h, mem, (uint)block, out read, IntPtr.Zero);
                }
                sw.Stop();
                return sw.Elapsed.TotalMilliseconds / reads;
            }
            finally
            {
                VirtualFree(mem, UIntPtr.Zero, 0x8000);
                CloseHandle(h);
                try { File.Delete(path); } catch { }
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern IntPtr CreateFileW(string name, uint access, uint share, IntPtr sec,
            uint disposition, uint flags, IntPtr template);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadFile(IntPtr h, IntPtr buf, uint toRead, out uint read, IntPtr overlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetFilePointerEx(IntPtr h, long distance, out long newPos, uint method);

        [DllImport("kernel32.dll")]
        static extern IntPtr VirtualAlloc(IntPtr addr, UIntPtr size, uint type, uint protect);

        [DllImport("kernel32.dll")]
        static extern bool VirtualFree(IntPtr addr, UIntPtr size, uint type);

        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr h);

        // ------------------------------------------------------------ storage

        public static void Save(BenchResult r, string path)
        {
            try
            {
                string[] lines = new string[]
                {
                    "# KiraDemorTY Optimizer benchmark",
                    "stamp=" + r.Stamp,
                    "mem_ns=" + r.MemNs.ToString("0.###", CultureInfo.InvariantCulture),
                    "disk_ms=" + r.DiskMs.ToString("0.####", CultureInfo.InvariantCulture),
                    "cpu_mops=" + r.CpuMops.ToString("0.##", CultureInfo.InvariantCulture),
                    "ping_ms=" + r.PingMs.ToString("0.##", CultureInfo.InvariantCulture),
                    "jitter_ms=" + r.JitterMs.ToString("0.##", CultureInfo.InvariantCulture)
                };
                File.WriteAllLines(path, lines, new System.Text.UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                Engine.Log("bench save: " + ex.Message);
            }
        }

        public static BenchResult Load(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                BenchResult r = new BenchResult();
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string k = line.Substring(0, eq);
                    string v = line.Substring(eq + 1);
                    double d;
                    double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out d);
                    if (k == "stamp") r.Stamp = v;
                    else if (k == "mem_ns") r.MemNs = d;
                    else if (k == "disk_ms") r.DiskMs = d;
                    else if (k == "cpu_mops") r.CpuMops = d;
                    else if (k == "ping_ms") r.PingMs = d;
                    else if (k == "jitter_ms") r.JitterMs = d;
                }
                return r.Any ? r : null;
            }
            catch
            {
                return null;
            }
        }

        // Returns the improvement in percent where positive means better.
        public static double Delta(double now, double before, bool lowerIsBetter)
        {
            if (before <= 0 || now < 0) return 0;
            double pct = (now - before) / before * 100.0;
            return lowerIsBetter ? -pct : pct;
        }
    }
}
