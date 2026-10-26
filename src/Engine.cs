using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;

namespace KDT
{
    internal class Op
    {
        public string Kind = "reg";     // reg | cmd
        public string TweakId = "";
        public string Hive = "";
        public string Path = "";
        public string Name = "";
        public string OldEnc = "";
        public string OldKind = "String";
        public string Undo = "";
        public string Note = "";
        public string Time = "";
        public bool Existed = false;
        public bool CreatedKey = false;
    }

    internal static class Engine
    {
        public static string DataDir = "";
        public static string BackupRoot = "";
        public static string JournalPath = "";
        public static readonly List<Op> Ops = new List<Op>();
        public static readonly List<string> LogLines = new List<string>();
        public static bool RestoreFirst = true;
        public static bool IsAdmin = false;

        // First-run welcome page: shown until the user finishes it once.
        public static bool IntroSeen = false;

        // "Restart Windows to finish" badge. We persist the boot time at the
        // moment a boot-dependent tweak is applied, so the badge clears itself
        // only after Windows has actually restarted.
        public static bool PendingReboot = false;
        static long BootTicks = 0;

        [DllImport("kernel32.dll")]
        static extern ulong GetTickCount64();

        public static DateTime BootTime()
        {
            try { return DateTime.Now.AddMilliseconds(-(double)GetTickCount64()); }
            catch { return DateTime.Now; }
        }

        public static void Init()
        {
            DataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KiraDemorTY Optimizer");
            BackupRoot = Path.Combine(DataDir, "Backups");
            JournalPath = Path.Combine(DataDir, "journal.tsv");
            Directory.CreateDirectory(DataDir);
            Directory.CreateDirectory(BackupRoot);
            IsAdmin = CheckAdmin();
            LoadSettings();
            Load();
        }

        static bool CheckAdmin()
        {
            try
            {
                WindowsIdentity id = WindowsIdentity.GetCurrent();
                WindowsPrincipal p = new WindowsPrincipal(id);
                return p.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        public static void Log(string s)
        {
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + s;
            LogLines.Add(line);
            if (LogLines.Count > 400) LogLines.RemoveAt(0);
        }

        // ---------------- settings ----------------
        static string SettingsPath
        {
            get { return Path.Combine(DataDir, "settings.ini"); }
        }

        // Data folder with %LOCALAPPDATA% kept as a variable: short enough for a
        // card, still valid in Explorer, and it does not expose the account name
        // when a screenshot is shared.
        public static string DataDirDisplay
        {
            get
            {
                string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (local.Length > 0 && DataDir.StartsWith(local, StringComparison.OrdinalIgnoreCase))
                    return "%LOCALAPPDATA%" + DataDir.Substring(local.Length);
                return DataDir;
            }
        }

        public static void LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return;
                string[] lines = File.ReadAllLines(SettingsPath, Encoding.UTF8);
                foreach (string raw in lines)
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    int i = line.IndexOf('=');
                    if (i <= 0) continue;
                    string k = line.Substring(0, i).Trim();
                    string v = line.Substring(i + 1).Trim();
                    if (k == "lang") Theme.Arabic = (v == "ar");
                    if (k == "restore") RestoreFirst = (v != "0");
                    if (k == "intro") IntroSeen = (v == "1");
                    if (k == "reboot") PendingReboot = (v == "1");
                    if (k == "boot") long.TryParse(v, out BootTicks);
                }

                // A restart happened if the machine's real boot time moved.
                if (PendingReboot && BootTicks > 0)
                {
                    long diff = Math.Abs(BootTime().Ticks - BootTicks);
                    if (diff > TimeSpan.FromMinutes(2).Ticks) PendingReboot = false;
                }
            }
            catch (Exception ex)
            {
                Log("settings load: " + ex.Message);
            }
        }

        public static void SaveSettings()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# KiraDemorTY Optimizer settings");
                sb.AppendLine("lang=" + (Theme.Arabic ? "ar" : "en"));
                sb.AppendLine("restore=" + (RestoreFirst ? "1" : "0"));
                sb.AppendLine("intro=" + (IntroSeen ? "1" : "0"));
                sb.AppendLine("reboot=" + (PendingReboot ? "1" : "0"));
                sb.AppendLine("boot=" + BootTicks.ToString(CultureInfo.InvariantCulture));
                File.WriteAllText(SettingsPath, sb.ToString(), new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                Log("settings save: " + ex.Message);
            }
        }

        public static void SetPendingReboot()
        {
            PendingReboot = true;
            BootTicks = BootTime().Ticks;
            SaveSettings();
        }

        public static void ClearPendingReboot()
        {
            if (!PendingReboot) return;
            PendingReboot = false;
            SaveSettings();
        }

        // True only while the machine has not been restarted since the tweak.
        public static bool RebootStillPending()
        {
            if (!PendingReboot) return false;
            if (BootTicks > 0 && Math.Abs(BootTime().Ticks - BootTicks) > TimeSpan.FromMinutes(2).Ticks)
            {
                PendingReboot = false;
                return false;
            }
            return true;
        }

        // ---------------- journal ----------------
        static string E(string s)
        {
            if (s == null) s = "";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
        }

        static string D(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            try { return Encoding.UTF8.GetString(Convert.FromBase64String(s)); }
            catch { return ""; }
        }

        public static void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < Ops.Count; i++)
                {
                    Op o = Ops[i];
                    sb.Append(E(o.Kind)).Append('\t');
                    sb.Append(E(o.TweakId)).Append('\t');
                    sb.Append(E(o.Hive)).Append('\t');
                    sb.Append(E(o.Path)).Append('\t');
                    sb.Append(E(o.Name)).Append('\t');
                    sb.Append(E(o.OldEnc)).Append('\t');
                    sb.Append(E(o.OldKind)).Append('\t');
                    sb.Append(o.Existed ? "1" : "0").Append('\t');
                    sb.Append(o.CreatedKey ? "1" : "0").Append('\t');
                    sb.Append(E(o.Undo)).Append('\t');
                    sb.Append(E(o.Note)).Append('\t');
                    sb.Append(E(o.Time));
                    sb.AppendLine();
                }
                File.WriteAllText(JournalPath, sb.ToString(), new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                Log("journal save: " + ex.Message);
            }
        }

        public static void Load()
        {
            Ops.Clear();
            try
            {
                if (!File.Exists(JournalPath)) return;
                string[] lines = File.ReadAllLines(JournalPath, Encoding.UTF8);
                foreach (string line in lines)
                {
                    if (line.Trim().Length == 0) continue;
                    string[] f = line.Split('\t');
                    if (f.Length < 12) continue;
                    Op o = new Op();
                    o.Kind = D(f[0]);
                    o.TweakId = D(f[1]);
                    o.Hive = D(f[2]);
                    o.Path = D(f[3]);
                    o.Name = D(f[4]);
                    o.OldEnc = D(f[5]);
                    o.OldKind = D(f[6]);
                    o.Existed = (f[7] == "1");
                    o.CreatedKey = (f[8] == "1");
                    o.Undo = D(f[9]);
                    o.Note = D(f[10]);
                    o.Time = D(f[11]);
                    Ops.Add(o);
                }
            }
            catch (Exception ex)
            {
                Log("journal load: " + ex.Message);
            }
        }

        public static int CountFor(string tweakId)
        {
            int n = 0;
            for (int i = 0; i < Ops.Count; i++) if (Ops[i].TweakId == tweakId) n++;
            return n;
        }

        // ---------------- registry ----------------
        public static RegistryKey BaseKey(string hive)
        {
            if (hive == "HKCU") return Registry.CurrentUser;
            if (hive == "HKLM") return Registry.LocalMachine;
            if (hive == "HKCR") return Registry.ClassesRoot;
            return Registry.CurrentUser;
        }

        public static object GetReg(string hive, string path, string name)
        {
            try
            {
                using (RegistryKey k = BaseKey(hive).OpenSubKey(path, false))
                {
                    if (k == null) return null;
                    return k.GetValue(name, null);
                }
            }
            catch
            {
                return null;
            }
        }

        public static int GetRegInt(string hive, string path, string name, int def)
        {
            object v = GetReg(hive, path, name);
            if (v == null) return def;
            try { return Convert.ToInt32(v, CultureInfo.InvariantCulture); }
            catch { return def; }
        }

        public static string GetRegStr(string hive, string path, string name, string def)
        {
            object v = GetReg(hive, path, name);
            if (v == null) return def;
            return Convert.ToString(v);
        }

        public static bool RegExists(string hive, string path, string name)
        {
            try
            {
                using (RegistryKey k = BaseKey(hive).OpenSubKey(path, false))
                {
                    if (k == null) return false;
                    return k.GetValue(name, null) != null;
                }
            }
            catch
            {
                return false;
            }
        }

        static string EncValue(object v, RegistryValueKind kind)
        {
            if (v == null) return "";
            if (kind == RegistryValueKind.DWord)
                return "D:" + Convert.ToInt32(v, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            if (kind == RegistryValueKind.QWord)
                return "Q:" + Convert.ToInt64(v, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            if (kind == RegistryValueKind.Binary)
                return "B:" + Convert.ToBase64String((byte[])v);
            if (kind == RegistryValueKind.MultiString)
                return "M:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join("\n", (string[])v)));
            return "S:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(Convert.ToString(v)));
        }

        static object DecValue(string enc, string kindName, out RegistryValueKind kind)
        {
            kind = RegistryValueKind.String;
            try { kind = (RegistryValueKind)Enum.Parse(typeof(RegistryValueKind), kindName, true); }
            catch { kind = RegistryValueKind.String; }
            if (string.IsNullOrEmpty(enc)) return null;
            char t = enc[0];
            string body = enc.Length > 2 ? enc.Substring(2) : "";
            if (t == 'D')
            {
                kind = RegistryValueKind.DWord;
                return int.Parse(body, CultureInfo.InvariantCulture);
            }
            if (t == 'Q')
            {
                kind = RegistryValueKind.QWord;
                return long.Parse(body, CultureInfo.InvariantCulture);
            }
            if (t == 'B')
            {
                kind = RegistryValueKind.Binary;
                return Convert.FromBase64String(body);
            }
            if (t == 'M')
            {
                kind = RegistryValueKind.MultiString;
                string s = Encoding.UTF8.GetString(Convert.FromBase64String(body));
                return s.Split('\n');
            }
            kind = RegistryValueKind.String;
            return Encoding.UTF8.GetString(Convert.FromBase64String(body));
        }

        public static void SetReg(string tweakId, string hive, string path, string name, object value, RegistryValueKind kind)
        {
            RegistryKey root = BaseKey(hive);
            RegistryKey key = root.OpenSubKey(path, true);
            bool created = false;
            if (key == null)
            {
                key = root.CreateSubKey(path);
                created = true;
            }
            if (key == null) throw new InvalidOperationException("Cannot open registry key " + hive + "\\" + path);
            try
            {
                object old = null;
                RegistryValueKind oldKind = RegistryValueKind.String;
                bool existed = false;
                if (!created)
                {
                    old = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                    existed = (old != null);
                    if (existed)
                    {
                        try { oldKind = key.GetValueKind(name); }
                        catch { oldKind = RegistryValueKind.String; }
                    }
                }

                string newEnc = EncValue(value, kind);
                string oldEnc = existed ? EncValue(old, oldKind) : "";
                if (existed && oldEnc == newEnc) return; // already identical, nothing to journal

                key.SetValue(name, value, kind);

                Op o = new Op();
                o.Kind = "reg";
                o.TweakId = tweakId;
                o.Hive = hive;
                o.Path = path;
                o.Name = name;
                o.OldEnc = oldEnc;
                o.OldKind = oldKind.ToString();
                o.Existed = existed;
                o.CreatedKey = created;
                o.Note = hive + "\\" + path + "\\" + name;
                o.Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                Ops.Add(o);
                Save();
                Log("SET " + o.Note + " -> " + newEnc);
            }
            finally
            {
                key.Close();
            }
        }

        public static void DeleteReg(string tweakId, string hive, string path, string name)
        {
            RegistryKey root = BaseKey(hive);
            RegistryKey key = root.OpenSubKey(path, true);
            if (key == null) return;
            try
            {
                object old = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                if (old == null) return;
                RegistryValueKind oldKind;
                try { oldKind = key.GetValueKind(name); }
                catch { oldKind = RegistryValueKind.String; }

                Op o = new Op();
                o.Kind = "reg";
                o.TweakId = tweakId;
                o.Hive = hive;
                o.Path = path;
                o.Name = name;
                o.OldEnc = EncValue(old, oldKind);
                o.OldKind = oldKind.ToString();
                o.Existed = true;
                o.Note = hive + "\\" + path + "\\" + name;
                o.Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                key.DeleteValue(name, false);
                Ops.Add(o);
                Save();
                Log("DEL " + o.Note);
            }
            finally
            {
                key.Close();
            }
        }

        static bool KeyEmpty(RegistryKey root, string path)
        {
            try
            {
                using (RegistryKey k = root.OpenSubKey(path, false))
                {
                    if (k == null) return true;
                    return k.ValueCount == 0 && k.SubKeyCount == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        static void UndoReg(Op o)
        {
            RegistryKey root = BaseKey(o.Hive);
            RegistryKey key = root.OpenSubKey(o.Path, true);
            if (key == null) return;
            try
            {
                if (o.Existed)
                {
                    RegistryValueKind kind;
                    object val = DecValue(o.OldEnc, o.OldKind, out kind);
                    key.SetValue(o.Name, val, kind);
                }
                else
                {
                    key.DeleteValue(o.Name, false);
                }
            }
            finally
            {
                key.Close();
            }
            if (o.CreatedKey && KeyEmpty(root, o.Path))
            {
                try { root.DeleteSubKeyTree(o.Path, false); }
                catch { }
            }
        }

        // ---------------- commands ----------------
        public static int Run(string exe, string args, out string output)
        {
            output = "";
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(exe, args);
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                using (Process p = Process.Start(psi))
                {
                    string so = p.StandardOutput.ReadToEnd();
                    string se = p.StandardError.ReadToEnd();
                    p.WaitForExit(30000);
                    output = (so + "\n" + se).Trim();
                    return p.HasExited ? p.ExitCode : -1;
                }
            }
            catch (Exception ex)
            {
                output = ex.Message;
                return -1;
            }
        }

        public static void AddCmdUndo(string tweakId, string undoCommand, string note)
        {
            Op o = new Op();
            o.Kind = "cmd";
            o.TweakId = tweakId;
            o.Undo = undoCommand;
            o.Note = note;
            o.Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Ops.Add(o);
            Save();
            Log("CMD-UNDO " + note + " :: " + undoCommand);
        }

        static void UndoCmd(Op o)
        {
            string output;
            Run("cmd.exe", "/c " + o.Undo, out output);
            Log("UNDO CMD " + o.Undo + " => " + output);
        }

        // ---------------- undo ----------------
        public static int UndoByTweak(string tweakId)
        {
            int n = 0;
            for (int i = Ops.Count - 1; i >= 0; i--)
            {
                if (Ops[i].TweakId != tweakId) continue;
                Op o = Ops[i];
                try
                {
                    if (o.Kind == "reg") UndoReg(o);
                    else if (o.Kind == "cmd") UndoCmd(o);
                    Ops.RemoveAt(i);
                    n++;
                }
                catch (Exception ex)
                {
                    Log("undo failed: " + ex.Message);
                }
            }
            if (n > 0) Save();
            return n;
        }

        public static int UndoAll()
        {
            int n = 0;
            for (int i = Ops.Count - 1; i >= 0; i--)
            {
                Op o = Ops[i];
                try
                {
                    if (o.Kind == "reg") UndoReg(o);
                    else if (o.Kind == "cmd") UndoCmd(o);
                    Ops.RemoveAt(i);
                    n++;
                }
                catch (Exception ex)
                {
                    Log("undo failed: " + ex.Message);
                }
            }
            Save();
            return n;
        }

        // ---------------- undo script export ----------------

        static string PsQuote(string s)
        {
            if (s == null) return "''";
            return "'" + s.Replace("'", "''") + "'";
        }

        static string PsHive(string hive)
        {
            if (hive == "HKLM") return "HKLM:";
            if (hive == "HKCR") return "HKCR:";
            return "HKCU:";
        }

        static string PsValue(object v, RegistryValueKind kind)
        {
            if (kind == RegistryValueKind.DWord)
                return Convert.ToInt32(v, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            if (kind == RegistryValueKind.QWord)
                return "[long]" + Convert.ToInt64(v, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            if (kind == RegistryValueKind.Binary)
            {
                byte[] b = (byte[])v;
                StringBuilder sb = new StringBuilder("[byte[]](");
                for (int i = 0; i < b.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(b[i].ToString(CultureInfo.InvariantCulture));
                }
                return sb.Append(')').ToString();
            }
            if (kind == RegistryValueKind.MultiString)
            {
                string[] parts = (string[])v;
                StringBuilder sb = new StringBuilder("@(");
                for (int i = 0; i < parts.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(PsQuote(parts[i]));
                }
                return sb.Append(')').ToString();
            }
            return PsQuote(Convert.ToString(v, CultureInfo.InvariantCulture));
        }

        public static string ExportUndoScript()
        {
            string path = Path.Combine(DataDir, "KDT-undo-" +
                DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".ps1");
            File.WriteAllText(path, BuildUndoScript(), new UTF8Encoding(true));
            Log("undo script exported: " + path);
            return path;
        }

        public static string BuildUndoScript()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("#requires -RunAsAdministrator");
            sb.AppendLine("# KiraDemorTY Optimizer - undo script");
            sb.AppendLine("# Generated " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) +
                          " on " + Environment.MachineName);
            sb.AppendLine("# Reverts " + Ops.Count + " journaled change(s), newest first.");
            sb.AppendLine("# Review it, then run:  powershell -ExecutionPolicy Bypass -File .\\this-file.ps1");
            sb.AppendLine("$ErrorActionPreference = 'Continue'");
            sb.AppendLine("$done = 0; $failed = 0");
            sb.AppendLine("function Step($text) { Write-Host ('  ' + $text) }");
            sb.AppendLine("function Restore-Key($path) { if (-not (Test-Path -LiteralPath $path)) { New-Item -Path $path -Force | Out-Null } }");
            sb.AppendLine("");
            sb.AppendLine("Write-Host 'KiraDemorTY Optimizer - undo' -ForegroundColor Yellow");

            for (int i = Ops.Count - 1; i >= 0; i--)
            {
                Op o = Ops[i];
                sb.AppendLine("");
                sb.AppendLine("# " + o.Time + "  " + (o.TweakId.Length > 0 ? o.TweakId : "?") + "  " + o.Kind);
                if (o.Kind == "cmd")
                {
                    string cmd = (o.Undo == null ? "" : o.Undo).Replace("\r", " ").Replace("\n", " ").Replace("\"", "\\\"");
                    sb.AppendLine("Step " + PsQuote("cmd: " + o.Undo));
                    sb.AppendLine("& cmd.exe /c \"" + cmd + "\"");
                    sb.AppendLine("if ($LASTEXITCODE -eq 0) { $done++ } else { $failed++ }");
                }
                else
                {
                    string key = PsHive(o.Hive) + "\\" + o.Path;
                    if (o.Existed)
                    {
                        RegistryValueKind kind;
                        object val = DecValue(o.OldEnc, o.OldKind, out kind);
                        sb.AppendLine("Step " + PsQuote("restore " + o.Note));
                        sb.AppendLine("Restore-Key " + PsQuote(key));
                        sb.AppendLine("Set-ItemProperty -LiteralPath " + PsQuote(key) + " -Name " + PsQuote(o.Name) +
                                      " -Value " + PsValue(val, kind) + " -Type " + kind.ToString());
                        sb.AppendLine("$done++");
                    }
                    else
                    {
                        sb.AppendLine("Step " + PsQuote("remove " + o.Note));
                        sb.AppendLine("Remove-ItemProperty -LiteralPath " + PsQuote(key) + " -Name " + PsQuote(o.Name) +
                                      " -ErrorAction SilentlyContinue");
                        sb.AppendLine("$done++");
                    }
                    if (o.CreatedKey)
                    {
                        sb.AppendLine("$k = Get-Item -LiteralPath " + PsQuote(key) + " -ErrorAction SilentlyContinue");
                        sb.AppendLine("if ($k -and $k.GetValueNames().Count -eq 0 -and $k.GetSubKeyNames().Count -eq 0) { Remove-Item -LiteralPath " + PsQuote(key) + " -Force -ErrorAction SilentlyContinue }");
                    }
                }
            }

            sb.AppendLine("");
            sb.AppendLine("Write-Host ('Undo finished. steps=' + $done + ' failed=' + $failed) -ForegroundColor Green");
            sb.AppendLine("Write-Host 'Restart Windows if a boot, power or GPU setting was reverted.'");
            return sb.ToString();
        }

        // ---------------- restore point ----------------
        public static bool CreateRestorePoint(out string message)
        {
            message = "";
            string desc = "KDT Optimizer " + DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            string cmd = "Checkpoint-Computer -Description '" + desc + "' -RestorePointType 'MODIFY_SETTINGS'";
            string outp;
            int code = Run("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -Command \"" + cmd + "\"", out outp);
            message = outp;
            Log("restore point exit=" + code + " " + outp);
            if (code == 0)
            {
                if (message.Length == 0) message = L.T("status.restoreOk");
                return true;
            }
            if (message.Length == 0) message = "exit code " + code;
            return false;
        }

        public static void OpenFolder(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                Process.Start("explorer.exe", "\"" + path + "\"");
            }
            catch (Exception ex)
            {
                Log("open folder: " + ex.Message);
            }
        }

        public static void OpenUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(url);
                psi.UseShellExecute = true;
                Process.Start(psi);
                Log("open url: " + url);
            }
            catch (Exception ex)
            {
                Log("open url: " + ex.Message);
            }
        }
    }
}
