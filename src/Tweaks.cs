using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace KDT
{
    internal static class Power
    {
        public const string HighGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
        public const string BalancedGuid = "381b4222-f694-41f0-9685-ff5bb260df2e";

        public const string SubProc = "54533251-82be-4824-96c1-47b60b740d00";
        public const string SubPcie = "501a4d13-42af-4429-9fd1-a8218c268e20";
        public const string SubUsb = "2a737441-1930-4402-8d77-b2bebba308a3";
        public const string SubDisk = "0012ee47-9041-4b5d-9b77-535fba8b1442";
        public const string MinState = "893dee8e-2bef-41e0-89c6-b55d0929964c";
        public const string MaxState = "bc5038f7-23e0-4960-96da-33abaf5935ec";
        public const string CoreParking = "0cc5b647-c1df-4637-891a-dec35c318583";
        public const string PcieAspm = "ee12f906-d277-404b-b6da-e5fa1a576df5";
        public const string UsbSuspend = "48e6b7a6-50f5-4782-a5d4-53bb8f07e226";
        public const string DiskIdle = "6738e2c4-e8a5-4a42-b16a-e040e769756e";

        public static string ActiveGuid()
        {
            string o;
            Engine.Run("powercfg.exe", "/getactivescheme", out o);
            Match m = Regex.Match(o, "([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})");
            if (m.Success) return m.Groups[1].Value.ToLowerInvariant();
            return "";
        }

        public static void Activate(string guid)
        {
            string o;
            int code = Engine.Run("powercfg.exe", "/setactive " + guid, out o);
            if (code != 0)
                throw new InvalidOperationException("powercfg /setactive failed: " + o);
        }

        public static int GetAc(string sub, string setting)
        {
            // The registry is authoritative, instant and language independent.
            // (Parsing "powercfg /q" text used to pick up the "possible setting"
            // range instead of the current value on some builds.)
            try
            {
                string root = @"SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes";
                string active = Engine.GetRegStr("HKLM", root, "ActivePowerScheme", "");
                if (active.Length > 0)
                {
                    string path = root + "\\" + active + "\\" + sub + "\\" + setting;
                    if (Engine.RegExists("HKLM", path, "ACSettingIndex"))
                        return Engine.GetRegInt("HKLM", path, "ACSettingIndex", -1);
                }
            }
            catch { }

            string o;
            Engine.Run("powercfg.exe", "/q SCHEME_CURRENT " + sub + " " + setting, out o);
            string[] lines = o.Replace("\r", "").Split('\n');

            // Preferred: the line that reports the current AC index.
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.IndexOf("Current", StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (line.IndexOf("AC", StringComparison.Ordinal) < 0) continue;
                Match m = Regex.Match(line, "0x([0-9a-fA-F]{1,8})");
                if (m.Success)
                {
                    int v;
                    if (int.TryParse(m.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v)) return v;
                }
            }

            // Fallback: the AC value is the second to last hex number in the block.
            MatchCollection all = Regex.Matches(o, "0x([0-9a-fA-F]{1,8})");
            if (all.Count >= 2)
            {
                int v;
                if (int.TryParse(all[all.Count - 2].Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v)) return v;
            }
            return -1;
        }

        public static void SetAc(string sub, string setting, int value)
        {
            string o;
            int code = Engine.Run("powercfg.exe", "/setacvalueindex SCHEME_CURRENT " + sub + " " + setting + " " + value, out o);
            if (code != 0)
                throw new InvalidOperationException("powercfg /setacvalueindex failed: " + o);
            string g = ActiveGuid();
            if (g.Length > 0) Engine.Run("powercfg.exe", "/setactive " + g, out o);
        }

        public static string UndoCommand(string sub, string setting, int oldValue)
        {
            string g = ActiveGuid();
            string cmd = "powercfg /setacvalueindex SCHEME_CURRENT " + sub + " " + setting + " " + oldValue;
            if (g.Length > 0) cmd += " && powercfg /setactive " + g;
            return cmd;
        }
    }

    internal class Tweak
    {
        public string Id = "";
        public string NameEn = "";
        public string NameAr = "";
        public string DescEn = "";
        public string DescAr = "";
        public string NoteEn = "";
        public string NoteAr = "";
        public int Risk = 0;       // 0 safe, 1 moderate, 2 advanced
        public bool Reboot = false;
        public Action Apply;
        public Action Defaults;
        public Func<bool> IsOn;

        public string Name { get { return L.Pick(NameEn, NameAr); } }
        public string Desc { get { return L.Pick(DescEn, DescAr); } }
        public string Note { get { return L.Pick(NoteEn, NoteAr); } }

        public bool On
        {
            get
            {
                try
                {
                    if (IsOn == null) return Engine.CountFor(Id) > 0;
                    return IsOn();
                }
                catch
                {
                    return false;
                }
            }
        }
    }

    internal static class TweakCatalog
    {
        public static readonly List<Tweak> All = new List<Tweak>();

        public const string DevGuard = @"SYSTEM\CurrentControlSet\Control\DeviceGuard";
        public const string Hvci = @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity";
        public const string SysProfile = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile";
        public const string GamesTask = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games";
        public const string GameBar = @"Software\Microsoft\GameBar";
        public const string GameDvrKey = @"Software\Microsoft\Windows\CurrentVersion\GameDVR";
        public const string GameConfig = @"System\GameConfigStore";
        public const string Win32Prio = @"SYSTEM\CurrentControlSet\Control\PriorityControl";
        public const string GfxDrivers = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";
        public const string PowerKey = @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling";
        public const string TcpIfaces = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";

        static Tweak New(string id, string ne, string na, string de, string da, string noe, string noa,
                         int risk, bool reboot, Action apply, Action defaults, Func<bool> isOn)
        {
            Tweak t = new Tweak();
            t.Id = id;
            t.NameEn = ne; t.NameAr = na;
            t.DescEn = de; t.DescAr = da;
            t.NoteEn = noe; t.NoteAr = noa;
            t.Risk = risk;
            t.Reboot = reboot;
            t.Apply = apply;
            t.Defaults = defaults;
            t.IsOn = isOn;
            return t;
        }

        static void Add(Tweak t)
        {
            All.Add(t);
        }

        public static Tweak ById(string id)
        {
            for (int i = 0; i < All.Count; i++) if (All[i].Id == id) return All[i];
            return null;
        }

        public static int AppliedCount()
        {
            int n = 0;
            for (int i = 0; i < All.Count; i++) if (All[i].On) n++;
            return n;
        }

        public static void Build()
        {
            All.Clear();

            // 1 - power plan
            Add(New("power_plan",
                "High performance power plan", "خطة الطاقة عالية الأداء",
                "Switches to the built-in High Performance plan so the CPU stops dropping clocks between frames.",
                "ينشّط خطة الأداء العالي المدمجة حتى يتوقف المعالج عن خفض تردده بين الإطارات.",
                "Instant. Recommended while plugged in.", "فوري. مستحسن أثناء التوصيل بالكهرباء.",
                0, false,
                delegate
                {
                    string old = Power.ActiveGuid();
                    Power.Activate(Power.HighGuid);
                    if (old.Length > 0 && old != Power.HighGuid)
                        Engine.AddCmdUndo("power_plan", "powercfg /setactive " + old, "previous power plan " + old);
                },
                delegate { Power.Activate(Power.BalancedGuid); },
                delegate { return Power.ActiveGuid() == Power.HighGuid; }));

            // 2 - processor states
            Add(New("cpu_state",
                "CPU minimum state 5% / maximum 100%", "حد المعالج الأدنى 5% والأعلى 100%",
                "Keeps the CPU ready to boost instantly instead of parking at a low frequency.",
                "يبقي المعالج مستعدًا للرفع فورًا بدل البقاء على تردد منخفض.",
                "Applies to the active power plan.", "يُطبّق على خطة الطاقة النشطة.",
                1, false,
                delegate
                {
                    int oldMin = Power.GetAc(Power.SubProc, Power.MinState);
                    int oldMax = Power.GetAc(Power.SubProc, Power.MaxState);
                    Power.SetAc(Power.SubProc, Power.MinState, 5);
                    Power.SetAc(Power.SubProc, Power.MaxState, 100);
                    if (oldMin >= 0)
                        Engine.AddCmdUndo("cpu_state", Power.UndoCommand(Power.SubProc, Power.MinState, oldMin), "previous CPU min state");
                    if (oldMax >= 0)
                        Engine.AddCmdUndo("cpu_state", Power.UndoCommand(Power.SubProc, Power.MaxState, oldMax), "previous CPU max state");
                },
                delegate
                {
                    Power.SetAc(Power.SubProc, Power.MinState, 5);
                    Power.SetAc(Power.SubProc, Power.MaxState, 100);
                },
                delegate
                {
                    return Power.GetAc(Power.SubProc, Power.MinState) == 5 &&
                           Power.GetAc(Power.SubProc, Power.MaxState) == 100;
                }));

            // 3 - core parking
            Add(New("core_parking",
                "Disable CPU core parking", "تعطيل إيقاف أنوية المعالج",
                "Stops Windows from parking cores, which removes a common source of 1-5 ms frame spikes.",
                "يمنع ويندوز من إيقاف الأنوية، وهو سبب شائع لتقطيعات من 1 إلى 5 مللي ثانية.",
                "Useful on 6-core and 8-core CPUs.", "مفيد مع معالجات 6 و8 أنوية.",
                1, false,
                delegate
                {
                    int old = Power.GetAc(Power.SubProc, Power.CoreParking);
                    Power.SetAc(Power.SubProc, Power.CoreParking, 100);
                    if (old >= 0)
                        Engine.AddCmdUndo("core_parking", Power.UndoCommand(Power.SubProc, Power.CoreParking, old), "previous core parking");
                },
                delegate { Power.SetAc(Power.SubProc, Power.CoreParking, 100); },
                delegate { return Power.GetAc(Power.SubProc, Power.CoreParking) == 100; }));

            // 4 - PCIe ASPM
            Add(New("pcie_aspm",
                "Disable PCIe link power saving", "تعطيل توفير طاقة منفذ PCIe",
                "Keeps the graphics card link at full speed instead of entering a low-power state.",
                "يبقي وصلة كرت الشاشة بسرعتها الكاملة بدل دخولها حالة طاقة منخفضة.",
                "Laptops will use slightly more battery.", "اللابتوبات ستستهلك بطارية أكثر قليلًا.",
                1, false,
                delegate
                {
                    int old = Power.GetAc(Power.SubPcie, Power.PcieAspm);
                    Power.SetAc(Power.SubPcie, Power.PcieAspm, 0);
                    if (old >= 0)
                        Engine.AddCmdUndo("pcie_aspm", Power.UndoCommand(Power.SubPcie, Power.PcieAspm, old), "previous PCIe ASPM");
                },
                delegate { Power.SetAc(Power.SubPcie, Power.PcieAspm, 0); },
                delegate { return Power.GetAc(Power.SubPcie, Power.PcieAspm) == 0; }));

            // 5 - USB selective suspend
            Add(New("usb_suspend",
                "Disable USB selective suspend", "تعطيل تعليق USB الانتقائي",
                "Prevents Windows from suspending USB devices mid-game, a known cause of input stalls.",
                "يمنع ويندوز من تعليق أجهزة USB أثناء اللعب، وهو سبب معروف لتقطيع الإدخال.",
                "Especially useful for USB mice and keyboards.", "مفيد خصوصًا لماوسات ولوحات USB.",
                0, false,
                delegate
                {
                    int old = Power.GetAc(Power.SubUsb, Power.UsbSuspend);
                    Power.SetAc(Power.SubUsb, Power.UsbSuspend, 0);
                    if (old >= 0)
                        Engine.AddCmdUndo("usb_suspend", Power.UndoCommand(Power.SubUsb, Power.UsbSuspend, old), "previous USB suspend setting");
                },
                delegate { Power.SetAc(Power.SubUsb, Power.UsbSuspend, 0); },
                delegate { return Power.GetAc(Power.SubUsb, Power.UsbSuspend) == 0; }));

            // 6 - power throttling
            Add(New("power_throttling",
                "Disable Power Throttling", "تعطيل خنق الطاقة",
                "Stops Windows from limiting background work when it thinks a process is not important.",
                "يمنع ويندوز من تقييد العمل في الخلفية عندما يظن أن العملية غير مهمة.",
                "Best on desktops and plugged-in laptops.", "الأفضل على المكتبي واللابتوب الموصول بالكهرباء.",
                1, false,
                delegate
                {
                    Engine.SetReg("power_throttling", "HKLM", PowerKey, "PowerThrottlingOff", 1, RegistryValueKind.DWord);
                },
                delegate
                {
                    Engine.SetReg("power_throttling", "HKLM", PowerKey, "PowerThrottlingOff", 0, RegistryValueKind.DWord);
                },
                delegate { return Engine.GetRegInt("HKLM", PowerKey, "PowerThrottlingOff", 0) == 1; }));

            // 7 - HAGS
            Add(New("hags",
                "Disable hardware GPU scheduling (HAGS)", "تعطيل جدولة GPU العتادية",
                "Turns HAGS off. On some driver/GPU combinations this reduces frame-time spikes and stutter.",
                "يعطّل HAGS. في بعض تركيبات التعريف وكرت الشاشة يقلل تقطع زمن الإطار والتقطيع.",
                "Needs a Windows restart. Test both settings and keep what is smoother.",
                "يحتاج إعادة تشغيل. جرّب الحالتين وأبقِ الأسلَس.",
                2, true,
                delegate
                {
                    Engine.SetReg("hags", "HKLM", GfxDrivers, "HwSchMode", 1, RegistryValueKind.DWord);
                },
                delegate
                {
                    Engine.SetReg("hags", "HKLM", GfxDrivers, "HwSchMode", 2, RegistryValueKind.DWord);
                },
                delegate { return Engine.GetRegInt("HKLM", GfxDrivers, "HwSchMode", 2) == 1; }));

            // 8 - game mode
            Add(New("game_mode",
                "Enable Game Mode", "تفعيل وضع الألعاب",
                "Tells Windows to prioritise the running game over background tasks.",
                "يخبر ويندوز بإعطاء اللعبة أولوية على مهام الخلفية.",
                "Safe and instant.", "آمن وفوري.",
                0, false,
                delegate
                {
                    Engine.SetReg("game_mode", "HKCU", GameBar, "AllowAutoGameMode", 1, RegistryValueKind.DWord);
                    Engine.SetReg("game_mode", "HKCU", GameBar, "AutoGameModeEnabled", 1, RegistryValueKind.DWord);
                },
                delegate
                {
                    Engine.SetReg("game_mode", "HKCU", GameBar, "AutoGameModeEnabled", 0, RegistryValueKind.DWord);
                },
                delegate { return Engine.GetRegInt("HKCU", GameBar, "AutoGameModeEnabled", 0) == 1; }));

            // 9 - game dvr
            Add(New("game_dvr",
                "Disable Game DVR background recording", "تعطيل تسجيل Game DVR في الخلفية",
                "Stops the Xbox recording service from capturing frames while you play.",
                "يوقف خدمة تسجيل Xbox عن التقاط الإطارات أثناء اللعب.",
                "You can still record with OBS or NVIDIA Share.", "ما زال بإمكانك التسجيل عبر OBS أو NVIDIA.",
                0, false,
                delegate
                {
                    Engine.SetReg("game_dvr", "HKCU", GameConfig, "GameDVR_Enabled", 0, RegistryValueKind.DWord);
                    Engine.SetReg("game_dvr", "HKCU", GameDvrKey, "AppCaptureEnabled", 0, RegistryValueKind.DWord);
                    Engine.SetReg("game_dvr", "HKCU", GameDvrKey, "HistoricalCaptureEnabled", 0, RegistryValueKind.DWord);
                },
                delegate
                {
                    Engine.SetReg("game_dvr", "HKCU", GameConfig, "GameDVR_Enabled", 1, RegistryValueKind.DWord);
                    Engine.SetReg("game_dvr", "HKCU", GameDvrKey, "AppCaptureEnabled", 1, RegistryValueKind.DWord);
                },
                delegate { return Engine.GetRegInt("HKCU", GameConfig, "GameDVR_Enabled", 1) == 0; }));

            // 10 - fullscreen optimizations
            Add(New("fse",
                "Disable fullscreen optimizations", "تعطيل تحسينات ملء الشاشة",
                "Forces true exclusive fullscreen for games, reducing composition latency and stutter.",
                "يفرض ملء الشاشة الحصري الحقيقي للألعاب، ما يقلل تأخير التركيب والتقطيع.",
                "Restart the game after applying.", "أعد تشغيل اللعبة بعد التطبيق.",
                0, false,
                delegate
                {
                    Engine.SetReg("fse", "HKCU", GameConfig, "GameDVR_FSEBehaviorMode", 2, RegistryValueKind.DWord);
                    Engine.SetReg("fse", "HKCU", GameConfig, "GameDVR_HonorUserFSEBehaviorMode", 1, RegistryValueKind.DWord);
                    Engine.SetReg("fse", "HKCU", GameConfig, "GameDVR_DXGIHonorFSEWindowsCompatible", 1, RegistryValueKind.DWord);
                    Engine.SetReg("fse", "HKCU", GameConfig, "GameDVR_EFSEFeatureFlags", 0, RegistryValueKind.DWord);
                },
                delegate
                {
                    Engine.SetReg("fse", "HKCU", GameConfig, "GameDVR_FSEBehaviorMode", 0, RegistryValueKind.DWord);
                    Engine.SetReg("fse", "HKCU", GameConfig, "GameDVR_HonorUserFSEBehaviorMode", 0, RegistryValueKind.DWord);
                    Engine.SetReg("fse", "HKCU", GameConfig, "GameDVR_DXGIHonorFSEWindowsCompatible", 0, RegistryValueKind.DWord);
                    Engine.SetReg("fse", "HKCU", GameConfig, "GameDVR_EFSEFeatureFlags", 0, RegistryValueKind.DWord);
                },
                delegate { return Engine.GetRegInt("HKCU", GameConfig, "GameDVR_FSEBehaviorMode", 0) == 2; }));

            // 11 - MMCSS
            Add(New("mmcss",
                "Multimedia responsiveness and game task priority", "استجابة الوسائط وأولوية مهمة اللعبة",
                "Lowers background multimedia reservation and raises the priority of the Games task profile.",
                "يقلل حجز الوسائط في الخلفية ويرفع أولوية ملف مهمة الألعاب.",
                "Helps audio/video threads and game scheduling.", "يساعد خيوط الصوت والفيديو وجدولة اللعبة.",
                1, false,
                delegate
                {
                    Engine.SetReg("mmcss", "HKLM", SysProfile, "SystemResponsiveness", 10, RegistryValueKind.DWord);
                    Engine.SetReg("mmcss", "HKLM", GamesTask, "GPU Priority", 8, RegistryValueKind.DWord);
                    Engine.SetReg("mmcss", "HKLM", GamesTask, "Priority", 6, RegistryValueKind.DWord);
                    Engine.SetReg("mmcss", "HKLM", GamesTask, "Scheduling Category", "High", RegistryValueKind.String);
                    Engine.SetReg("mmcss", "HKLM", GamesTask, "SFIO Priority", "High", RegistryValueKind.String);
                },
                delegate
                {
                    Engine.SetReg("mmcss", "HKLM", SysProfile, "SystemResponsiveness", 20, RegistryValueKind.DWord);
                    Engine.SetReg("mmcss", "HKLM", GamesTask, "Priority", 2, RegistryValueKind.DWord);
                    Engine.SetReg("mmcss", "HKLM", GamesTask, "Scheduling Category", "Medium", RegistryValueKind.String);
                    Engine.SetReg("mmcss", "HKLM", GamesTask, "SFIO Priority", "Normal", RegistryValueKind.String);
                },
                delegate
                {
                    return Engine.GetRegInt("HKLM", SysProfile, "SystemResponsiveness", 20) == 10 &&
                           Engine.GetRegInt("HKLM", GamesTask, "Priority", 2) == 6;
                }));

            // 12 - network throttling
            Add(New("net_throttling",
                "Disable network throttling", "تعطيل خنق الشبكة",
                "Removes the 10 packets-per-millisecond multimedia throttle that can add latency in online games.",
                "يزيل خنق الوسائط عند 10 حزم لكل مللي ثانية الذي قد يضيف تأخيرًا في الألعاب الأونلاين.",
                "Instant. Does not change your bandwidth.", "فوري. لا يغيّر سرعة اتصالك.",
                0, false,
                delegate
                {
                    Engine.SetReg("net_throttling", "HKLM", SysProfile, "NetworkThrottlingIndex", -1, RegistryValueKind.DWord);
                },
                delegate
                {
                    Engine.SetReg("net_throttling", "HKLM", SysProfile, "NetworkThrottlingIndex", 10, RegistryValueKind.DWord);
                },
                delegate { return Engine.GetRegInt("HKLM", SysProfile, "NetworkThrottlingIndex", 10) == -1; }));

            // 13 - priority separation
            Add(New("prio_sep",
                "Foreground CPU priority (Win32PrioritySeparation)", "أولوية المعالج للبرنامج الأمامي",
                "Uses short, fixed quanta for foreground apps so the game gets scheduled more predictably.",
                "يستخدم شرائح زمنية قصيرة وثابتة للتطبيقات الأمامية لجدولة اللعبة بشكل أكثر ثباتًا.",
                "If your PC feels jerky in multitasking, turn this off.",
                "إذا صار الجهاز متقطعًا مع تعدد المهام، عطّل هذا الخيار.",
                1, false,
                delegate
                {
                    Engine.SetReg("prio_sep", "HKLM", Win32Prio, "Win32PrioritySeparation", 38, RegistryValueKind.DWord);
                },
                delegate
                {
                    Engine.SetReg("prio_sep", "HKLM", Win32Prio, "Win32PrioritySeparation", 2, RegistryValueKind.DWord);
                },
                delegate { return Engine.GetRegInt("HKLM", Win32Prio, "Win32PrioritySeparation", 2) == 38; }));

            // 14 - startup delay
            Add(New("startup_delay",
                "Remove startup app delay", "إزالة تأخير برامج البدء",
                "Skips the artificial 10 second delay Windows adds before launching startup apps.",
                "يتخطى التأخير الاصطناعي (10 ثوانٍ) الذي يضيفه ويندوز قبل تشغيل برامج البدء.",
                "Makes your desktop usable sooner after login.", "يجعل سطح المكتب جاهزًا أسرع بعد تسجيل الدخول.",
                0, false,
                delegate
                {
                    Engine.SetReg("startup_delay", "HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize",
                        "StartupDelayInMSec", 0, RegistryValueKind.DWord);
                },
                delegate
                {
                    Engine.DeleteReg("startup_delay", "HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize",
                        "StartupDelayInMSec");
                },
                delegate
                {
                    return Engine.RegExists("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize", "StartupDelayInMSec") &&
                           Engine.GetRegInt("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize", "StartupDelayInMSec", 1) == 0;
                }));

            // 15 - Nagle
            Add(New("nagle",
                "Lower network latency (disable Nagle)", "تقليل تأخير الشبكة (تعطيل Nagle)",
                "Sets TcpAckFrequency and TCPNoDelay on every network interface to cut small-packet delay.",
                "يضبط TcpAckFrequency وTCPNoDelay على كل كرت شبكة لتقليل تأخير الحزم الصغيرة.",
                "Applies after reconnecting or restarting. Most useful for online games.",
                "يُطبّق بعد إعادة الاتصال أو التشغيل. الأنفع في الألعاب الأونلاين.",
                1, false,
                delegate
                {
                    RegistryKey ifs = Registry.LocalMachine.OpenSubKey(TcpIfaces);
                    if (ifs == null) throw new InvalidOperationException("TCP interfaces key not found");
                    try
                    {
                        foreach (string sub in ifs.GetSubKeyNames())
                        {
                            string path = TcpIfaces + "\\" + sub;
                            Engine.SetReg("nagle", "HKLM", path, "TcpAckFrequency", 1, RegistryValueKind.DWord);
                            Engine.SetReg("nagle", "HKLM", path, "TCPNoDelay", 1, RegistryValueKind.DWord);
                        }
                    }
                    finally { ifs.Close(); }
                },
                delegate
                {
                    RegistryKey ifs = Registry.LocalMachine.OpenSubKey(TcpIfaces);
                    if (ifs == null) return;
                    try
                    {
                        foreach (string sub in ifs.GetSubKeyNames())
                        {
                            string path = TcpIfaces + "\\" + sub;
                            Engine.DeleteReg("nagle", "HKLM", path, "TcpAckFrequency");
                            Engine.DeleteReg("nagle", "HKLM", path, "TCPNoDelay");
                        }
                    }
                    finally { ifs.Close(); }
                },
                delegate
                {
                    RegistryKey ifs = Registry.LocalMachine.OpenSubKey(TcpIfaces);
                    if (ifs == null) return false;
                    try
                    {
                        foreach (string sub in ifs.GetSubKeyNames())
                        {
                            string path = TcpIfaces + "\\" + sub;
                            if (Engine.GetRegInt("HKLM", path, "TcpAckFrequency", 0) == 1) return true;
                        }
                    }
                    finally { ifs.Close(); }
                    return false;
                }));

            // 16 - VBS / HVCI
            Add(New("vbs",
                "Disable VBS and memory integrity (HVCI)", "تعطيل VBS وحماية الذاكرة HVCI",
                "Turns off virtualization-based security, which costs 3-10% CPU and adds latency on many systems.",
                "يعطّل الأمان القائم على المحاكاة الافتراضية، الذي يستهلك 3-10% من المعالج ويضيف تأخيرًا في أجهزة كثيرة.",
                "Needs a restart. Do not use if you run Hyper-V, WSL2, or virtual machines.",
                "يحتاج إعادة تشغيل. لا تستخدمه إذا كنت تشغّل Hyper-V أو WSL2 أو أجهزة افتراضية.",
                2, true,
                delegate
                {
                    string oldBcd = BcdHypervisorMode();
                    Engine.SetReg("vbs", "HKLM", DevGuard, "EnableVirtualizationBasedSecurity", 0, RegistryValueKind.DWord);
                    Engine.SetReg("vbs", "HKLM", Hvci, "Enabled", 0, RegistryValueKind.DWord);
                    string o;
                    Engine.Run("bcdedit.exe", "/set hypervisorlaunchtype off", out o);
                    Engine.AddCmdUndo("vbs", "bcdedit /set hypervisorlaunchtype " + oldBcd, "previous hypervisor launch type");
                },
                delegate
                {
                    Engine.SetReg("vbs", "HKLM", DevGuard, "EnableVirtualizationBasedSecurity", 1, RegistryValueKind.DWord);
                    Engine.SetReg("vbs", "HKLM", Hvci, "Enabled", 1, RegistryValueKind.DWord);
                    string o;
                    Engine.Run("bcdedit.exe", "/set hypervisorlaunchtype auto", out o);
                },
                delegate
                {
                    return HypervisorOff() && Engine.GetRegInt("HKLM", Hvci, "Enabled", 1) == 0;
                }));

            // 17 - mouse acceleration
            Add(New("mouse_accel",
                "Disable mouse acceleration", "تعطيل تسريع الماوس",
                "Removes Windows pointer acceleration so the crosshair moves 1:1 with your hand.",
                "يزيل تسريع مؤشر ويندوز ليتحرك التصويب بنسبة 1:1 مع حركة يدك.",
                "Sign out or restart Explorer to apply. Purely personal preference.",
                "سجّل الخروج أو أعد تشغيل Explorer للتطبيق. مسألة تفضيل شخصي.",
                0, false,
                delegate
                {
                    Engine.SetReg("mouse_accel", "HKCU", @"Control Panel\Mouse", "MouseSpeed", "0", RegistryValueKind.String);
                    Engine.SetReg("mouse_accel", "HKCU", @"Control Panel\Mouse", "MouseThreshold1", "0", RegistryValueKind.String);
                    Engine.SetReg("mouse_accel", "HKCU", @"Control Panel\Mouse", "MouseThreshold2", "0", RegistryValueKind.String);
                },
                delegate
                {
                    Engine.SetReg("mouse_accel", "HKCU", @"Control Panel\Mouse", "MouseSpeed", "1", RegistryValueKind.String);
                    Engine.SetReg("mouse_accel", "HKCU", @"Control Panel\Mouse", "MouseThreshold1", "6", RegistryValueKind.String);
                    Engine.SetReg("mouse_accel", "HKCU", @"Control Panel\Mouse", "MouseThreshold2", "10", RegistryValueKind.String);
                },
                delegate { return Engine.GetRegStr("HKCU", @"Control Panel\Mouse", "MouseSpeed", "1") == "0"; }));

            // 18 - visual effects
            Add(New("visual_fx",
                "Visual effects: best performance", "المؤثرات البصرية: أفضل أداء",
                "Turns off window animations, shadows and fade effects, freeing a little GPU composition time.",
                "يعطّل حركات النوافذ والظلال وتأثيرات التلاشي، محرّرًا القليل من وقت تركيب كرت الشاشة.",
                "Windows will look flatter. Cosmetic only, fully reversible.",
                "سيبدو ويندوز أبسط. تغيير شكلي فقط وقابل للتراجع.",
                1, false,
                delegate
                {
                    Engine.SetReg("visual_fx", "HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
                        "VisualFXSetting", 2, RegistryValueKind.DWord);
                },
                delegate
                {
                    Engine.SetReg("visual_fx", "HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
                        "VisualFXSetting", 0, RegistryValueKind.DWord);
                },
                delegate
                {
                    return Engine.GetRegInt("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
                        "VisualFXSetting", 0) == 2;
                }));

            // 19 - transparency
            Add(New("transparency",
                "Disable Windows transparency", "تعطيل شفافية ويندوز",
                "Stops the compositor from blurring translucent surfaces behind windows.",
                "يمنع مركّب الرسوم من تمويه الأسطح الشفافة خلف النوافذ.",
                "A small but real saving on integrated GPUs. Cosmetic only.",
                "توفير صغير لكنه حقيقي على كروت الرسوم المدمجة. تغيير شكلي فقط.",
                0, false,
                delegate
                {
                    Engine.SetReg("transparency", "HKCU", @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                        "EnableTransparency", 0, RegistryValueKind.DWord);
                },
                delegate
                {
                    Engine.SetReg("transparency", "HKCU", @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                        "EnableTransparency", 1, RegistryValueKind.DWord);
                },
                delegate
                {
                    return Engine.GetRegInt("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                        "EnableTransparency", 1) == 0;
                }));

            // 20 - background apps
            Add(New("background_apps",
                "Stop background Store apps", "إيقاف تطبيقات المتجر في الخلفية",
                "Blocks packaged apps from running background tasks and notifications when closed.",
                "يمنع تطبيقات المتجر من تنفيذ مهام في الخلفية وإرسال إشعارات وهي مغلقة.",
                "Some apps stop syncing until you open them. Turn off if you rely on them.",
                "بعض التطبيقات تتوقف عن المزامنة حتى تفتحها. عطّله إذا كنت تعتمد عليها.",
                1, false,
                delegate
                {
                    Engine.SetReg("background_apps", "HKCU", @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications",
                        "GlobalUserDisabled", 1, RegistryValueKind.DWord);
                },
                delegate
                {
                    Engine.SetReg("background_apps", "HKCU", @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications",
                        "GlobalUserDisabled", 0, RegistryValueKind.DWord);
                },
                delegate
                {
                    return Engine.GetRegInt("HKCU", @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications",
                        "GlobalUserDisabled", 0) == 1;
                }));

            // 21 - menu delay
            Add(New("menu_delay",
                "Instant menus (no 400 ms delay)", "قوائم فورية (بدون تأخير 400 مللي)",
                "Removes the default delay before submenus open in Explorer and most apps.",
                "يزيل التأخير الافتراضي قبل فتح القوائم الفرعية في المستكشف ومعظم البرامج.",
                "Restart Explorer to apply. Feels faster, changes nothing under load.",
                "أعد تشغيل Explorer للتطبيق. يبدو أسرع، ولا يغيّر شيئًا تحت الحمل.",
                0, false,
                delegate
                {
                    Engine.SetReg("menu_delay", "HKCU", @"Control Panel\Desktop", "MenuShowDelay", "0", RegistryValueKind.String);
                },
                delegate
                {
                    Engine.SetReg("menu_delay", "HKCU", @"Control Panel\Desktop", "MenuShowDelay", "400", RegistryValueKind.String);
                },
                delegate { return Engine.GetRegStr("HKCU", @"Control Panel\Desktop", "MenuShowDelay", "400") == "0"; }));

            // 22 - search suggestions
            Add(New("search_suggestions",
                "Disable web search suggestions", "تعطيل اقتراحات البحث من الويب",
                "Stops Start menu search from contacting Bing and keeps results local.",
                "يمنع بحث قائمة ابدأ من الاتصال بـ Bing ويبقي النتائج محلية.",
                "Search becomes instant and private; you lose web suggestions.",
                "يصبح البحث فوريًا وخاصًا؛ لكنك تخسر اقتراحات الويب.",
                0, false,
                delegate
                {
                    Engine.SetReg("search_suggestions", "HKCU", @"Software\Microsoft\Windows\CurrentVersion\Search",
                        "BingSearchEnabled", 0, RegistryValueKind.DWord);
                    Engine.SetReg("search_suggestions", "HKCU", @"Software\Microsoft\Windows\CurrentVersion\Search",
                        "CortanaConsent", 0, RegistryValueKind.DWord);
                },
                delegate
                {
                    Engine.SetReg("search_suggestions", "HKCU", @"Software\Microsoft\Windows\CurrentVersion\Search",
                        "BingSearchEnabled", 1, RegistryValueKind.DWord);
                    Engine.SetReg("search_suggestions", "HKCU", @"Software\Microsoft\Windows\CurrentVersion\Search",
                        "CortanaConsent", 1, RegistryValueKind.DWord);
                },
                delegate
                {
                    return Engine.GetRegInt("HKCU", @"Software\Microsoft\Windows\CurrentVersion\Search",
                        "BingSearchEnabled", 1) == 0;
                }));

            // 23 - TRIM
            Add(New("trim",
                "Enable SSD TRIM", "تفعيل TRIM للقرص الصلب",
                "Re-enables the TRIM command so the SSD can reclaim deleted blocks and keep write speed.",
                "يعيد تفعيل أمر TRIM ليستعيد القرص الصلب الكتل المحذوفة ويحافظ على سرعة الكتابة.",
                "Instant. Only needed if TRIM was disabled by another tool.",
                "فوري. لا يلزم إلا إذا عطّل برنامج آخر TRIM.",
                0, false,
                delegate
                {
                    int old = TrimState();
                    string o;
                    Engine.Run("fsutil.exe", "behavior set DisableDeleteNotify 0", out o);
                    if (old == 1)
                        Engine.AddCmdUndo("trim", "fsutil behavior set DisableDeleteNotify 1", "previous TRIM setting");
                },
                delegate
                {
                    string o;
                    Engine.Run("fsutil.exe", "behavior set DisableDeleteNotify 0", out o);
                },
                delegate { return TrimState() == 0; }));

            // 24 - fast startup
            Add(New("fast_startup",
                "Disable Fast Startup", "تعطيل بدء التشغيل السريع",
                "Makes every shutdown a full shutdown, so drivers and devices start clean instead of resuming from a hibernation image.",
                "يجعل كل إيقاف تشغيل كاملًا، لتبدأ التعريفات والأجهزة بحالة نظيفة بدلًا من صورة السكون.",
                "Fixes 'works after reboot, broken after shutdown' issues. Boot takes a few seconds longer.",
                "يحل مشاكل «تعمل بعد إعادة التشغيل وتتعطل بعد الإغلاق». يصبح الإقلاع أبطأ بثوانٍ.",
                0, false,
                delegate
                {
                    Engine.SetReg("fast_startup", "HKLM", @"SYSTEM\CurrentControlSet\Control\Session Manager\Power",
                        "HiberbootEnabled", 0, RegistryValueKind.DWord);
                },
                delegate
                {
                    Engine.SetReg("fast_startup", "HKLM", @"SYSTEM\CurrentControlSet\Control\Session Manager\Power",
                        "HiberbootEnabled", 1, RegistryValueKind.DWord);
                },
                delegate
                {
                    return Engine.GetRegInt("HKLM", @"SYSTEM\CurrentControlSet\Control\Session Manager\Power",
                        "HiberbootEnabled", 1) == 0;
                }));

            // 25 - delivery optimization
            Add(New("delivery_opt",
                "Stop peer-to-peer update sharing", "إيقاف مشاركة التحديثات بين الأجهزة",
                "Stops Delivery Optimization from downloading and seeding Windows updates from other PCs while you play.",
                "يمنع Delivery Optimization من تنزيل ومشاركة تحديثات ويندوز مع أجهزة أخرى أثناء اللعب.",
                "Updates still come straight from Microsoft, just without P2P upload traffic.",
                "تظل التحديثات تأتي من مايكروسوفت مباشرة، لكن بدون حركة رفع P2P.",
                0, false,
                delegate
                {
                    Engine.SetReg("delivery_opt", "HKLM",
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config",
                        "DODownloadMode", 0, RegistryValueKind.DWord);
                },
                delegate
                {
                    Engine.SetReg("delivery_opt", "HKLM",
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config",
                        "DODownloadMode", 1, RegistryValueKind.DWord);
                },
                delegate
                {
                    return Engine.GetRegInt("HKLM",
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config",
                        "DODownloadMode", 1) == 0;
                }));

            // 26 - error reporting
            Add(New("wer",
                "Disable Windows Error Reporting", "تعطيل تقارير أخطاء ويندوز",
                "Stops the crash reporter from writing dumps and contacting Microsoft after an application crashes.",
                "يوقف جامع الأعطال عن كتابة الملفات والاتصال بمايكروسوفت بعد انهيار أي برنامج.",
                "You lose automatic crash reports. Re-enable it while troubleshooting a specific app.",
                "ستفقد تقارير الأعطال التلقائية. أعد تفعيله عند فحص برنامج معيّن.",
                1, false,
                delegate
                {
                    Engine.SetReg("wer", "HKLM", @"SOFTWARE\Microsoft\Windows\Windows Error Reporting",
                        "Disabled", 1, RegistryValueKind.DWord);
                    Engine.SetReg("wer", "HKCU", @"Software\Microsoft\Windows\Windows Error Reporting",
                        "Disabled", 1, RegistryValueKind.DWord);
                },
                delegate
                {
                    Engine.SetReg("wer", "HKLM", @"SOFTWARE\Microsoft\Windows\Windows Error Reporting",
                        "Disabled", 0, RegistryValueKind.DWord);
                    Engine.SetReg("wer", "HKCU", @"Software\Microsoft\Windows\Windows Error Reporting",
                        "Disabled", 0, RegistryValueKind.DWord);
                },
                delegate
                {
                    return Engine.GetRegInt("HKLM", @"SOFTWARE\Microsoft\Windows\Windows Error Reporting",
                        "Disabled", 0) == 1;
                }));

            // 27 - game bar panel
            Add(New("gamebar_tips",
                "Hide Game Bar startup panel", "إخفاء لوحة Game Bar عند البدء",
                "Stops the Game Bar panel and tips from popping up over the game when it launches.",
                "يمنع ظهور لوحة Game Bar والتنبيهات فوق اللعبة عند تشغيلها.",
                "Game Bar still opens with Win+G whenever you need it.",
                "تظل لوحة Game Bar تُفتح بـ Win+G عند الحاجة.",
                0, false,
                delegate
                {
                    Engine.SetReg("gamebar_tips", "HKCU", GameBar, "ShowStartupPanel", 0, RegistryValueKind.DWord);
                    Engine.SetReg("gamebar_tips", "HKCU", GameBar, "UseNexusForGameBarEnabled", 0, RegistryValueKind.DWord);
                },
                delegate
                {
                    Engine.SetReg("gamebar_tips", "HKCU", GameBar, "ShowStartupPanel", 1, RegistryValueKind.DWord);
                },
                delegate { return Engine.GetRegInt("HKCU", GameBar, "ShowStartupPanel", 1) == 0; }));

            // 28 - timer resolution
            Add(New("timer_resolution",
                "High-resolution system timer", "مؤقت النظام عالي الدقة",
                "Lets games request a 1 ms timer instead of the default 15.6 ms tick, which can smooth frame pacing.",
                "يسمح للألعاب بطلب مؤقت 1 مللي بدلًا من 15.6 مللي الافتراضي، ما قد يحسّن انتظام الإطارات.",
                "Takes effect after a restart. Can raise idle power by a few watts; helps in some games, not all.",
                "يُطبّق بعد إعادة التشغيل. قد يرفع استهلاك الخمول بضع واط؛ مفيد في بعض الألعاب وليس كلها.",
                1, true,
                delegate
                {
                    Engine.SetReg("timer_resolution", "HKLM",
                        @"SYSTEM\CurrentControlSet\Control\Session Manager\kernel",
                        "GlobalTimerResolutionRequests", 1, RegistryValueKind.DWord);
                },
                delegate
                {
                    Engine.SetReg("timer_resolution", "HKLM",
                        @"SYSTEM\CurrentControlSet\Control\Session Manager\kernel",
                        "GlobalTimerResolutionRequests", 0, RegistryValueKind.DWord);
                },
                delegate
                {
                    return Engine.GetRegInt("HKLM",
                        @"SYSTEM\CurrentControlSet\Control\Session Manager\kernel",
                        "GlobalTimerResolutionRequests", 0) == 1;
                }));

            // 29 - disk idle timeout
            Add(New("disk_idle",
                "Keep the SSD awake", "إبقاء القرص الصلب مستيقظًا",
                "Sets the disk idle timeout to Never so the drive never drops into a low-power state between loads.",
                "يجعل مهلة خمول القرص بلا نهاية حتى لا يدخل القرص في حالة طاقة منخفضة بين التحميلات.",
                "Removes wake-up spikes when a game streams assets. Slightly higher idle power.",
                "يزيل قفزات الاستيقاظ عند تحميل اللعبة للأصول. استهلاك خمول أعلى قليلًا.",
                0, false,
                delegate
                {
                    int old = Power.GetAc(Power.SubDisk, Power.DiskIdle);
                    Power.SetAc(Power.SubDisk, Power.DiskIdle, 0);
                    if (old >= 0)
                        Engine.AddCmdUndo("disk_idle",
                            Power.UndoCommand(Power.SubDisk, Power.DiskIdle, old),
                            "disk idle timeout was " + old + " s");
                },
                delegate { Power.SetAc(Power.SubDisk, Power.DiskIdle, 1200); },
                delegate { return Power.GetAc(Power.SubDisk, Power.DiskIdle) == 0; }));

            // 30 - automatic maintenance
            Add(New("maintenance",
                "Pause Automatic Maintenance", "إيقاف الصيانة التلقائية",
                "Stops Windows from running defrag, update and telemetry jobs in the background at its scheduled time.",
                "يمنع ويندوز من تشغيل إلغاء التجزئة والتحديثات ومهام التتبّع في الخلفية في وقتها المجدول.",
                "Run maintenance manually now and then from Security and Maintenance.",
                "شغّل الصيانة يدويًا من حين لآخر من «الأمان والصيانة».",
                1, false,
                delegate
                {
                    Engine.SetReg("maintenance", "HKLM",
                        @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance",
                        "MaintenanceDisabled", 1, RegistryValueKind.DWord);
                },
                delegate
                {
                    Engine.SetReg("maintenance", "HKLM",
                        @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance",
                        "MaintenanceDisabled", 0, RegistryValueKind.DWord);
                },
                delegate
                {
                    return Engine.GetRegInt("HKLM",
                        @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance",
                        "MaintenanceDisabled", 0) == 1;
                }));

            // 31 - tips and suggestions
            Add(New("windows_tips", "Disable Windows tips and suggestions", "تعطيل نصائح ويندوز واقتراحاته",
                "Turns off suggested apps, tips and promotional content in the Start menu and Settings.",
                "يوقف التطبيقات المقترحة والنصائح والمحتوى الترويجي في قائمة ابدأ والإعدادات.",
                "No effect on games; it only stops background content downloads.",
                "لا تأثير على الألعاب؛ يوقف فقط تنزيل المحتوى في الخلفية.",
                0, false,
                delegate
                {
                    string key = @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";
                    Engine.SetReg("windows_tips", "HKCU", key, "SoftLandingEnabled", 0, RegistryValueKind.DWord);
                    Engine.SetReg("windows_tips", "HKCU", key, "SubscribedContent-338389Enabled", 0, RegistryValueKind.DWord);
                    Engine.SetReg("windows_tips", "HKCU", key, "SystemPaneSuggestionsEnabled", 0, RegistryValueKind.DWord);
                },
                delegate
                {
                    string key = @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";
                    Engine.SetReg("windows_tips", "HKCU", key, "SoftLandingEnabled", 1, RegistryValueKind.DWord);
                    Engine.SetReg("windows_tips", "HKCU", key, "SubscribedContent-338389Enabled", 1, RegistryValueKind.DWord);
                    Engine.SetReg("windows_tips", "HKCU", key, "SystemPaneSuggestionsEnabled", 1, RegistryValueKind.DWord);
                },
                delegate
                {
                    string key = @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";
                    return Engine.GetRegInt("HKCU", key, "SoftLandingEnabled", 1) == 0;
                }));

            // 32 - QoS reservation
            Add(New("qos",
                "Release reserved network bandwidth", "تحرير نطاق الشبكة المحجوز",
                "Sets the QoS packet scheduler reservation to 0% so no bandwidth is held back for system traffic.",
                "يجعل حجز مجدول حزم QoS صفرًا حتى لا يُحجز نطاق لحركة النظام.",
                "Modern Windows already reserves 0% by default, so expect little or no change.",
                "ويندوز الحديث يحجز 0% افتراضيًا، لذا قد لا تلاحظ فرقًا.",
                0, false,
                delegate
                {
                    Engine.SetReg("qos", "HKLM", @"SOFTWARE\Policies\Microsoft\Windows\Psched",
                        "NonBestEffortLimit", 0, RegistryValueKind.DWord);
                },
                delegate
                {
                    Engine.DeleteReg("qos", "HKLM", @"SOFTWARE\Policies\Microsoft\Windows\Psched",
                        "NonBestEffortLimit");
                },
                delegate
                {
                    return Engine.GetRegInt("HKLM", @"SOFTWARE\Policies\Microsoft\Windows\Psched",
                        "NonBestEffortLimit", 20) == 0;
                }));

            // 33 - telemetry service
            Add(New("svc_diagtrack",
                "Disable the telemetry service", "تعطيل خدمة التتبّع",
                "Stops the Connected User Experiences and Telemetry service (DiagTrack) and sets it to Disabled.",
                "يوقف خدمة «تجارب المستخدم المتصلة والتتبّع» (DiagTrack) ويجعلها معطّلة.",
                "Privacy plus a little background CPU and disk. Some managed work PCs need this service.",
                "خصوصية وتوفير بسيط للمعالج والقرص. بعض أجهزة العمل المُدارة تحتاج هذه الخدمة.",
                1, false,
                delegate
                {
                    int old = ServiceStart("DiagTrack");
                    if (old < 0) throw new InvalidOperationException("The DiagTrack service is not installed on this PC.");
                    string o;
                    Engine.Run("sc.exe", "stop DiagTrack", out o);
                    Engine.SetReg("svc_diagtrack", "HKLM", @"SYSTEM\CurrentControlSet\Services\DiagTrack",
                        "Start", 4, RegistryValueKind.DWord);
                    if (old != 4)
                        Engine.AddCmdUndo("svc_diagtrack", "sc start DiagTrack", "restart the telemetry service");
                },
                delegate
                {
                    string o;
                    Engine.SetReg("svc_diagtrack", "HKLM", @"SYSTEM\CurrentControlSet\Services\DiagTrack",
                        "Start", 2, RegistryValueKind.DWord);
                    Engine.Run("sc.exe", "start DiagTrack", out o);
                },
                delegate
                {
                    return Engine.GetRegInt("HKLM", @"SYSTEM\CurrentControlSet\Services\DiagTrack", "Start", 3) == 4;
                }));
        }

        // 0 boot, 1 system, 2 auto, 3 demand, 4 disabled. -1 = missing service.
        public static int ServiceStart(string name)
        {
            return Engine.GetRegInt("HKLM", @"SYSTEM\CurrentControlSet\Services\" + name, "Start", -1);
        }

        public static string BcdHypervisorMode()
        {
            string o;
            Engine.Run("bcdedit.exe", "/enum {current}", out o);
            string[] lines = o.Replace("\r", "").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("hypervisorlaunchtype", StringComparison.OrdinalIgnoreCase))
                {
                    string[] parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2) return parts[parts.Length - 1];
                }
            }
            return "auto";
        }

        public static bool HypervisorOff()
        {
            string o;
            Engine.Run("bcdedit.exe", "/enum {current}", out o);
            string[] lines = o.Replace("\r", "").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("hypervisorlaunchtype", StringComparison.OrdinalIgnoreCase))
                    return line.ToLowerInvariant().EndsWith("off");
            }
            return false;
        }

        public static int TrimState()
        {
            string o;
            Engine.Run("fsutil.exe", "behavior query DisableDeleteNotify", out o);
            Match m = Regex.Match(o, @"DisableDeleteNotify\s*=\s*(\d)");
            if (m.Success)
            {
                int v;
                if (int.TryParse(m.Groups[1].Value, out v)) return v;
            }
            return -1;
        }

        public static string AppliedNames(bool arabic)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < All.Count; i++)
            {
                if (!All[i].On) continue;
                sb.Append(arabic ? All[i].NameAr : All[i].NameEn);
                sb.Append(", ");
            }
            string s = sb.ToString();
            if (s.Length > 2) s = s.Substring(0, s.Length - 2);
            return s;
        }
    }
}
