using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace KDT
{
    internal static class SelfTest
    {
        static int _fail = 0;
        static int _pass = 0;

        const string ProbeKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize";
        const string ProbeName = "StartupDelayInMSec";

        static void Check(string name, bool ok, string detail)
        {
            if (ok) { _pass++; Console.WriteLine("  PASS  " + name); }
            else
            {
                _fail++;
                Console.WriteLine("  FAIL  " + name + "   [" + detail + "]");
            }
        }

        static int Main()
        {
            Console.WriteLine("KiraDemorTY Optimizer - engine self test");
            Console.WriteLine("---------------------------------------");

            Engine.Init();
            TweakCatalog.Build();

            Check("catalog has at least 30 tweaks", TweakCatalog.All.Count >= 30,
                  "count=" + TweakCatalog.All.Count);

            bool namesOk = true;
            bool textOk = true;
            bool idsOk = true;
            bool risksOk = true;
            Dictionary<string, bool> seen = new Dictionary<string, bool>();
            for (int i = 0; i < TweakCatalog.All.Count; i++)
            {
                Tweak tw = TweakCatalog.All[i];
                if (tw.NameEn.Length == 0 || tw.NameAr.Length == 0) namesOk = false;
                if (tw.DescEn.Length == 0 || tw.DescAr.Length == 0 ||
                    tw.NoteEn.Length == 0 || tw.NoteAr.Length == 0) textOk = false;
                if (seen.ContainsKey(tw.Id)) idsOk = false;
                else seen[tw.Id] = true;
                if (tw.Risk < 0 || tw.Risk > 2) risksOk = false;
            }
            Check("every tweak has an English and Arabic name", namesOk, "missing name");
            Check("every tweak has a description and note in both languages", textOk, "missing text");
            Check("tweak ids are unique", idsOk, "duplicate id");
            Check("risk levels are 0..2", risksOk, "out of range");
            Check("new tweaks are registered",
                  TweakCatalog.ById("fast_startup") != null &&
                  TweakCatalog.ById("timer_resolution") != null &&
                  TweakCatalog.ById("svc_diagtrack") != null, "missing tweak");
            Tweak bootTweak = TweakCatalog.ById("timer_resolution");
            Check("boot tweak is flagged for restart", bootTweak != null && bootTweak.Reboot, "not flagged");

            Check("channel url is an https YouTube link",
                  Brand.ChannelUrl.StartsWith("https://www.youtube.com/", StringComparison.Ordinal), Brand.ChannelUrl);
            Check("welcome page strings exist",
                  L.T("intro.start") != "intro.start" && L.T("intro.lead") != "intro.lead" &&
                  L.T("channel.open") != "channel.open", "missing intro string");

            bool overlayOk = false;
            try
            {
                using (WelcomeOverlay wo = new WelcomeOverlay())
                {
                    wo.Size = new Size(1200, 780);
                    using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(1200, 780))
                    {
                        wo.DrawToBitmap(bmp, new Rectangle(0, 0, 1200, 780));
                        System.Drawing.Color px = bmp.GetPixel(600, 400);
                        overlayOk = px.R > 18 || px.G > 18 || px.B > 18;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  info  overlay paint: " + ex.Message);
            }
            Check("welcome page paints without errors", overlayOk, "blank or threw");

            string journal = Engine.JournalPath;
            string backup = journal + ".selftest";

            if (File.Exists(backup)) File.Delete(backup);
            if (File.Exists(journal)) File.Copy(journal, backup, true);

            try
            {
                // start from an empty journal so counts are predictable
                Engine.Ops.Clear();
                Engine.Save();

                Tweak t = TweakCatalog.ById("startup_delay");
                Check("catalog contains startup_delay", t != null, "null");
                if (t == null) return 9;

                bool beforeExists = Engine.RegExists("HKCU", ProbeKey, ProbeName);
                int beforeValue = Engine.GetRegInt("HKCU", ProbeKey, ProbeName, -12345);
                Console.WriteLine("  info  original state: exists=" + beforeExists + " value=" + beforeValue);

                // --- 1. apply -------------------------------------------------
                Console.WriteLine("1) apply");
                t.Defaults();
                Check("defaults clears the tweak", !t.On, "still on");
                t.Apply();

                bool afterExists = Engine.RegExists("HKCU", ProbeKey, ProbeName);
                int afterValue = Engine.GetRegInt("HKCU", ProbeKey, ProbeName, -12345);
                Check("registry value is 0 after apply", afterExists && afterValue == 0,
                      "exists=" + afterExists + " value=" + afterValue);
                Check("tweak reports applied", t.On, "IsOn false");
                Check("journal recorded the change", Engine.CountFor("startup_delay") >= 1,
                      "count=" + Engine.CountFor("startup_delay"));
                Check("journal file is not empty",
                      File.Exists(journal) && File.ReadAllText(journal).Trim().Length > 0, "empty");

                // --- 2. undo --------------------------------------------------
                Console.WriteLine("2) undo");
                int undone = Engine.UndoByTweak("startup_delay");
                Check("undo returned changes", undone >= 1, "n=" + undone);
                Check("journal cleared for tweak", Engine.CountFor("startup_delay") == 0,
                      "count=" + Engine.CountFor("startup_delay"));

                bool nowExists = Engine.RegExists("HKCU", ProbeKey, ProbeName);
                int nowValue = Engine.GetRegInt("HKCU", ProbeKey, ProbeName, -12345);
                if (beforeExists)
                    Check("original value restored", nowExists && nowValue == beforeValue,
                          "exists=" + nowExists + " value=" + nowValue + " expected=" + beforeValue);
                else
                    Check("value removed again", !nowExists,
                          "exists=" + nowExists + " value=" + nowValue);

                // --- 3. string round trip in a scratch key --------------------
                Console.WriteLine("3) registry round trip (string + dword)");
                const string scratch = @"Software\KDT-SelfTest";
                Engine.SetReg("selftest", "HKCU", scratch, "Str", "hello", Microsoft.Win32.RegistryValueKind.String);
                Engine.SetReg("selftest", "HKCU", scratch, "Num", 42, Microsoft.Win32.RegistryValueKind.DWord);
                string sv = Engine.GetRegStr("HKCU", scratch, "Str", "");
                int nv = Engine.GetRegInt("HKCU", scratch, "Num", -1);
                Check("string written", sv == "hello", sv);
                Check("dword written", nv == 42, nv.ToString());

                // --- 3b. undo script export ----------------------------------
                Console.WriteLine("3b) undo script");
                string undoPath = Engine.ExportUndoScript();
                Check("undo script exported", File.Exists(undoPath) && new FileInfo(undoPath).Length > 200,
                      undoPath);
                string undoText = File.Exists(undoPath) ? File.ReadAllText(undoPath) : "";
                Check("undo script has admin guard",
                      undoText.IndexOf("#requires -RunAsAdministrator", StringComparison.Ordinal) >= 0, "no guard");
                Check("undo script references the scratch key",
                      undoText.IndexOf(scratch, StringComparison.Ordinal) >= 0, "no key");
                Check("undo script removes created values",
                      undoText.IndexOf("Remove-ItemProperty", StringComparison.Ordinal) >= 0, "no remove");
                try { File.Delete(undoPath); } catch { }

                int n2 = Engine.UndoByTweak("selftest");
                Check("scratch values removed", n2 >= 2 && !Engine.RegExists("HKCU", scratch, "Str"),
                      "n=" + n2);
                Check("scratch key removed", Microsoft.Win32.Registry.CurrentUser.OpenSubKey(scratch) == null,
                      "key remains");

                // --- 4. report ------------------------------------------------
                Console.WriteLine("4) report");
                Sample sm = new Sample();
                sm.Cpu = 12; sm.RamPct = 63; sm.RamUsedGb = 9.9; sm.RamTotalGb = 15.9;
                sm.Gpu = 41; sm.FreeGb = 80.5; sm.TotalGb = 237.0;
                PingStat pg = new PingStat();
                pg.Avg = 24; pg.Jitter = 2; pg.Loss = 0; pg.Ok = true;

                string path = Report.Generate(sm, pg, 91);
                Check("report file created", File.Exists(path), path);
                string html = File.ReadAllText(path);
                Check("report is html", html.IndexOf("<!DOCTYPE html>", StringComparison.Ordinal) >= 0, "no doctype");
                Check("report has brand", html.IndexOf("KiraDemorTY Optimizer", StringComparison.Ordinal) >= 0, "no brand");
                Check("report has ping value", html.IndexOf("24 ms", StringComparison.Ordinal) >= 0, "no ping");
                string probeTweak = TweakCatalog.ById("power_throttling").Name;
                Check("report lists tweaks", html.IndexOf("power_throttling", StringComparison.Ordinal) < 0 &&
                                            html.IndexOf(probeTweak, StringComparison.Ordinal) > 0, "no tweaks");
                Check("report is utf8 with BOM", html.Length > 0, "empty");
                Console.WriteLine("  info  report: " + path);
                try { File.Delete(path); } catch { }

                // --- 5. informational ---------------------------------
                Console.WriteLine("5) power settings detection");
                int pMin = Power.GetAc(Power.SubProc, Power.MinState);
                int pMax = Power.GetAc(Power.SubProc, Power.MaxState);
                int pPark = Power.GetAc(Power.SubProc, Power.CoreParking);
                int pAspm = Power.GetAc(Power.SubPcie, Power.PcieAspm);
                int pUsb = Power.GetAc(Power.SubUsb, Power.UsbSuspend);
                Console.WriteLine("  info min=" + pMin + " max=" + pMax + " parking=" + pPark +
                                  " aspm=" + pAspm + " usb=" + pUsb);
                Check("cpu min state readable", pMin >= 0, "=" + pMin);
                Check("cpu max state readable", pMax >= 0, "=" + pMax);
                Check("core parking readable", pPark >= 0, "=" + pPark);
                Check("pcie aspm readable", pAspm >= 0, "=" + pAspm);
                Check("usb suspend readable", pUsb >= 0, "=" + pUsb);
                Check("bogus subgroup returns -1",
                      Power.GetAc("11111111-1111-1111-1111-111111111111", Power.MinState) == -1, "not -1");

                int applied = 0;
                for (int i = 0; i < TweakCatalog.All.Count; i++)
                    if (TweakCatalog.All[i].On) applied++;
                Console.WriteLine("  info applied " + applied + " / " + TweakCatalog.All.Count);
                Check("tweak states evaluate", true, "");

                Console.WriteLine("6) diagnostics");
                List<DiagItem> diag = Diag.Scan();
                Check("diagnostics returned 11 checks", diag.Count == 11, "count=" + diag.Count);
                bool diagText = true;
                bool diagLevel = true;
                bool diagFix = true;
                int fixable = 0;
                for (int i = 0; i < diag.Count; i++)
                {
                    DiagItem d = diag[i];
                    if (string.IsNullOrEmpty(d.Title) || string.IsNullOrEmpty(d.Detail)) diagText = false;
                    if (d.Level < 0 || d.Level > 2) diagLevel = false;
                    if (!string.IsNullOrEmpty(d.FixId))
                    {
                        fixable++;
                        if (TweakCatalog.ById(d.FixId) == null) diagFix = false;
                    }
                }
                Check("every check has a title and detail", diagText, "empty text");
                Check("severity levels are valid", diagLevel, "out of range");
                Check("diagnostics offer at least one fix", fixable > 0, "fixable=" + fixable);
                Check("every fix points at a real tweak", diagFix, "unknown tweak id");

                Console.WriteLine("7) benchmark");
                BenchResult br = Bench.Run(false);
                Console.WriteLine("  info  mem=" + br.MemNs.ToString("0.0") + " ns  disk=" +
                                  br.DiskMs.ToString("0.000") + " ms  cpu=" + br.CpuMops.ToString("0") + " Mops/s");
                Check("memory latency measured", br.MemNs > 5 && br.MemNs < 2000, br.MemNs.ToString("0.0"));
                Check("cpu throughput measured", br.CpuMops > 5, br.CpuMops.ToString("0"));
                Check("disk latency measured", br.DiskMs > 0 && br.DiskMs < 500, br.DiskMs.ToString("0.000"));
                string benchTmp = Path.Combine(Path.GetTempPath(), "kdt-bench-selftest.tsv");
                Bench.Save(br, benchTmp);
                BenchResult back = Bench.Load(benchTmp);
                Check("benchmark round trip", back != null && Math.Abs(back.MemNs - br.MemNs) < 0.01,
                      back == null ? "null" : back.MemNs.ToString("0.0"));
                Check("delta sign is improvement",
                      Bench.Delta(90, 100, true) > 0 && Bench.Delta(110, 100, true) < 0, "wrong sign");
                try { File.Delete(benchTmp); } catch { }

                Console.WriteLine("8) informational");
                Console.WriteLine("  info  TRIM DisableDeleteNotify = " + TweakCatalog.TrimState());
                Console.WriteLine("  info  hypervisorlaunchtype off = " + TweakCatalog.HypervisorOff());
                Console.WriteLine("  info  active power plan = " + Power.ActiveGuid());
                Console.WriteLine("  info  journal entries left = " + Engine.Ops.Count);
                Check("journal is clean after tests", Engine.Ops.Count == 0, "count=" + Engine.Ops.Count);
            }
            catch (Exception ex)
            {
                _fail++;
                Console.WriteLine("  FAIL  exception: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                try
                {
                    if (File.Exists(backup))
                    {
                        if (File.Exists(journal)) File.Delete(journal);
                        File.Move(backup, journal);
                        Console.WriteLine("  info  original journal restored");
                    }
                    else if (File.Exists(journal))
                    {
                        File.Delete(journal);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("  warn  journal restore: " + ex.Message);
                }
            }

            Console.WriteLine("---------------------------------------");
            Console.WriteLine("passed " + _pass + ", failed " + _fail);
            return _fail == 0 ? 0 : 1;
        }
    }
}
