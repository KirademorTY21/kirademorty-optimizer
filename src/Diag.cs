using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace KDT
{
    internal class DiagItem
    {
        public string Id = "";
        public string TitleEn = "";
        public string TitleAr = "";
        public string DetailEn = "";
        public string DetailAr = "";
        public string FixId = "";
        public int Level = 0;      // 0 ok, 1 attention, 2 problem
        public bool Fixable = false;

        public string Title { get { return L.Pick(TitleEn, TitleAr); } }
        public string Detail { get { return L.Pick(DetailEn, DetailAr); } }
    }

    // Read-only health checks. Nothing here changes the system: every finding
    // points at a catalog tweak that the user can apply (and undo) explicitly.
    internal static class Diag
    {
        public const string DevGuard = TweakCatalog.DevGuard;
        public const string Hvci = TweakCatalog.Hvci;
        public const string GameDvrKey = TweakCatalog.GameDvrKey;
        public const string GameConfig = TweakCatalog.GameConfig;
        public const string GameBar = TweakCatalog.GameBar;
        public const string GfxDrivers = TweakCatalog.GfxDrivers;
        public const string SysProfile = TweakCatalog.SysProfile;

        static DiagItem Item(string id, string titleEn, string titleAr, int level,
                             string detail, string fixId)
        {
            DiagItem d = new DiagItem();
            d.Id = id;
            d.TitleEn = titleEn;
            d.TitleAr = titleAr;
            d.Level = level;
            d.DetailEn = detail;
            d.DetailAr = detail;
            d.FixId = fixId == null ? "" : fixId;
            d.Fixable = d.FixId.Length > 0 && TweakCatalog.ById(d.FixId) != null;
            return d;
        }

        public static List<DiagItem> Scan()
        {
            List<DiagItem> list = new List<DiagItem>();

            // 1 - VBS / HVCI
            bool hvOff = TweakCatalog.HypervisorOff();
            int hvci = Engine.GetRegInt("HKLM", Hvci, "Enabled", 1);
            bool vbsOn = !hvOff || hvci == 1;
            if (vbsOn)
                list.Add(Item("vbs", "VBS / memory integrity", "VBS وحماية الذاكرة", 2,
                    L.Pick("Virtualization-based security or HVCI is active. Hypervisor launch is '" +
                           TweakCatalog.BcdHypervisorMode() + "', HVCI is " + (hvci == 1 ? "on" : "off") +
                           ". This costs roughly 3-10% CPU and adds memory latency on many systems. Needs a restart.",
                           "الأمان الافتراضي أو HVCI مفعّل. نوع إقلاع الهايبرفايزر '" + TweakCatalog.BcdHypervisorMode() +
                           "'، وHVCI " + (hvci == 1 ? "مفعّل" : "معطّل") +
                           ". يكلّف نحو 3-10% من المعالج ويضيف تأخيرًا للذاكرة في أجهزة كثيرة. يحتاج إعادة تشغيل."),
                    "vbs"));
            else
                list.Add(Item("vbs", "VBS / memory integrity", "VBS وحماية الذاكرة", 0,
                    L.Pick("Hypervisor is off and HVCI is off.", "الهايبرفايزر معطّل وHVCI معطّل."), ""));

            // 2 - power plan
            string plan = Power.ActiveGuid();
            bool high = plan == Power.HighGuid;
            string planName = high ? L.Pick("High performance", "أداء عالي")
                : (plan == Power.BalancedGuid ? L.Pick("Balanced", "متوازن") : plan);
            if (plan.Length == 0)
                list.Add(Item("power_plan", "Power plan", "خطة الطاقة", 1,
                    L.Pick("Could not read the active power plan.", "تعذّرت قراءة خطة الطاقة النشطة."), ""));
            else if (!high)
                list.Add(Item("power_plan", "Power plan", "خطة الطاقة", 1,
                    string.Format(L.Pick("The active plan is {0}. High performance keeps clocks steady between frames.",
                        "الخطة النشطة هي {0}. خطة الأداء العالي تحافظ على ثبات التردد بين الإطارات."), planName),
                    "power_plan"));
            else
                list.Add(Item("power_plan", "Power plan", "خطة الطاقة", 0,
                    L.Pick("High performance is active.", "خطة الأداء العالي نشطة."), ""));

            // 3 - CPU states
            int cmin = Power.GetAc(Power.SubProc, Power.MinState);
            int cmax = Power.GetAc(Power.SubProc, Power.MaxState);
            bool cpuOk = (cmin == 5 && cmax == 100);
            if (cmin < 0 || cmax < 0)
                list.Add(Item("cpu_state", "CPU frequency limits", "حدود تردد المعالج", 1,
                    L.Pick("Could not read the CPU state limits.", "تعذّرت قراءة حدود حالة المعالج."), ""));
            else if (!cpuOk)
                list.Add(Item("cpu_state", "CPU frequency limits", "حدود تردد المعالج", 1,
                    string.Format(L.Pick("Minimum {0}% / maximum {1}%. The recommended profile is 5% / 100%.",
                        "الحد الأدنى {0}% والأعلى {1}%. الملف الموصى به هو 5% / 100%."), cmin, cmax),
                    "cpu_state"));
            else
                list.Add(Item("cpu_state", "CPU frequency limits", "حدود تردد المعالج", 0,
                    L.Pick("Minimum 5% / maximum 100%.", "الحد الأدنى 5% والأعلى 100%."), ""));

            // 4 - core parking
            int park = Power.GetAc(Power.SubProc, Power.CoreParking);
            if (park < 0)
                list.Add(Item("core_parking", "Core parking", "إيقاف الأنوية", 1,
                    L.Pick("This firmware does not expose core parking.", "هذا الجهاز لا يعرض إعداد إيقاف الأنوية."), ""));
            else if (park != 100)
                list.Add(Item("core_parking", "Core parking", "إيقاف الأنوية", 1,
                    string.Format(L.Pick("{0}% of cores may be parked. Parked cores cause 1-5 ms wake-up spikes.",
                        "يمكن إيقاف {0}% من الأنوية. الأنوية الموقوفة تسبب تقطيعات استيقاظ من 1 إلى 5 مللي ثانية."), 100 - park),
                    "core_parking"));
            else
                list.Add(Item("core_parking", "Core parking", "إيقاف الأنوية", 0,
                    L.Pick("Core parking is disabled.", "إيقاف الأنوية معطّل."), ""));

            // 5 - GameDVR
            int dvr = Engine.GetRegInt("HKCU", GameConfig, "GameDVR_Enabled", 1);
            int cap = Engine.GetRegInt("HKCU", GameDvrKey, "AppCaptureEnabled", 1);
            if (dvr == 0 && cap == 0)
                list.Add(Item("game_dvr", "Background recording", "التسجيل في الخلفية", 0,
                    L.Pick("Game DVR capture is off.", "تسجيل Game DVR معطّل."), ""));
            else
                list.Add(Item("game_dvr", "Background recording", "التسجيل في الخلفية", 1,
                    L.Pick("Game DVR is capturing frames in the background. It costs GPU time and disk writes while you play.",
                        "Game DVR يلتقط إطارات في الخلفية. يستهلك وقت كرت الشاشة ويكتب على القرص أثناء اللعب."),
                    "game_dvr"));

            // 6 - Game Mode
            int gm = Engine.GetRegInt("HKCU", GameBar, "AutoGameModeEnabled", 0);
            if (gm == 1)
                list.Add(Item("game_mode", "Game Mode", "وضع الألعاب", 0,
                    L.Pick("Game Mode is enabled.", "وضع الألعاب مفعّل."), ""));
            else
                list.Add(Item("game_mode", "Game Mode", "وضع الألعاب", 1,
                    L.Pick("Game Mode is off, so Windows may background the game's threads for other work.",
                        "وضع الألعاب معطّل، لذا قد يؤخّر ويندوز خيوط اللعبة لصالح مهام أخرى."), "game_mode"));

            // 7 - HAGS
            int hags = Engine.GetRegInt("HKLM", GfxDrivers, "HwSchMode", 2);
            if (hags == 1)
                list.Add(Item("hags", "Hardware GPU scheduling", "جدولة GPU العتادية", 0,
                    L.Pick("HAGS is off.", "جدولة GPU العتادية معطّلة."), ""));
            else
                list.Add(Item("hags", "Hardware GPU scheduling", "جدولة GPU العتادية", 1,
                    L.Pick("HAGS is on. On some driver/GPU pairs it adds frame-time spikes; on others it helps. Measure both.",
                        "جدولة GPU العتادية مفعّلة. في بعض تركيبات التعريف والكرت تزيد تقطع زمن الإطار، وفي أخرى تساعد. قِس الحالتين."),
                    "hags"));

            // 8 - network throttling
            int nthr = Engine.GetRegInt("HKLM", SysProfile, "NetworkThrottlingIndex", 10);
            if (nthr == -1)
                list.Add(Item("net_throttling", "Multimedia network throttle", "خنق شبكة الوسائط", 0,
                    L.Pick("Network throttling is disabled.", "خنق الشبكة معطّل."), ""));
            else
                list.Add(Item("net_throttling", "Multimedia network throttle", "خنق شبكة الوسائط", 1,
                    string.Format(L.Pick("Windows limits multimedia traffic to {0} packets/ms. In online games this can add latency.",
                        "يحدّ ويندوز حركة الوسائط إلى {0} حزمة/مللي. في الألعاب الأونلاين قد يضيف تأخيرًا."), nthr),
                    "net_throttling"));

            // 9 - TRIM
            int trim = TweakCatalog.TrimState();
            if (trim == 0)
                list.Add(Item("trim", "SSD TRIM", "TRIM للقرص الصلب", 0,
                    L.Pick("TRIM is enabled on this system.", "TRIM مفعّل على هذا النظام."), ""));
            else if (trim == 1)
                list.Add(Item("trim", "SSD TRIM", "TRIM للقرص الصلب", 2,
                    L.Pick("TRIM is disabled. Write speed and SSD endurance degrade over time.",
                        "TRIM معطّل. سرعة الكتابة وعمر القرص تتدهور مع الوقت."), "trim"));
            else
                list.Add(Item("trim", "SSD TRIM", "TRIM للقرص الصلب", 1,
                    L.Pick("Could not read the TRIM state.", "تعذّرت قراءة حالة TRIM."), ""));

            // 10 - free space
            try
            {
                DriveInfo d = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory));
                double free = d.AvailableFreeSpace / 1073741824.0;
                double total = d.TotalSize / 1073741824.0;
                double pct = total > 0 ? free / total * 100.0 : 0;
                if (pct < 10)
                    list.Add(Item("disk_free", "System drive space", "مساحة قرص النظام", 2,
                        string.Format(L.Pick("Only {0:0.0} GB free ({1:0}%). Below 10% Windows slows down and updates fail.",
                            "المتاح {0:0.0} جيجابايت فقط ({1:0}%). أقل من 10% يُبطئ ويندوز ويُفشل التحديثات."), free, pct), ""));
                else if (pct < 20)
                    list.Add(Item("disk_free", "System drive space", "مساحة قرص النظام", 1,
                        string.Format(L.Pick("{0:0.0} GB free ({1:0}%). Aim for at least 20% free.",
                            "المتاح {0:0.0} جيجابايت ({1:0}%). يُفضّل إبقاء 20% على الأقل."), free, pct), ""));
                else
                    list.Add(Item("disk_free", "System drive space", "مساحة قرص النظام", 0,
                        string.Format(L.Pick("{0:0.0} GB free ({1:0}%).", "المتاح {0:0.0} جيجابايت ({1:0}%)."), free, pct), ""));
            }
            catch
            {
                list.Add(Item("disk_free", "System drive space", "مساحة قرص النظام", 1,
                    L.Pick("Could not read the system drive.", "تعذّرت قراءة قرص النظام."), ""));
            }

            // 11 - GPU temperature
            int temp = GpuTemp();
            if (temp < 0)
                list.Add(Item("gpu_temp", "GPU temperature", "حرارة كرت الشاشة", 0,
                    L.Pick("nvidia-smi is not available, so temperature is not monitored.",
                        "nvidia-smi غير متاح، لذا لا تُراقب الحرارة."), ""));
            else if (temp >= 83)
                list.Add(Item("gpu_temp", "GPU temperature", "حرارة كرت الشاشة", 2,
                    string.Format(L.Pick("{0} C. This GPU throttles at 83 C, so you are losing clocks right now. Raise the fan curve.",
                        "{0} درجة. هذا الكرت يخنق عند 83 درجة، أي أنك تخسر ترددًا الآن. ارفع منحنى المروحة."), temp), ""));
            else if (temp >= 78)
                list.Add(Item("gpu_temp", "GPU temperature", "حرارة كرت الشاشة", 1,
                    string.Format(L.Pick("{0} C. Close to the 83 C throttle point; consider a stronger fan curve.",
                        "{0} درجة. قريب من حد الخنق 83 درجة؛ فكّر بمنحنى مروحة أقوى."), temp), ""));
            else
                list.Add(Item("gpu_temp", "GPU temperature", "حرارة كرت الشاشة", 0,
                    string.Format(L.Pick("{0} C.", "{0} درجة."), temp), ""));

            return list;
        }

        public static int GpuTemp()
        {
            try
            {
                string o;
                int code = Engine.Run("nvidia-smi.exe",
                    "--query-gpu=temperature.gpu --format=csv,noheader,nounits", out o);
                if (code != 0) return -1;
                Match m = Regex.Match(o, @"(\d{1,3})");
                if (!m.Success) return -1;
                int v;
                if (int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
                    return v;
            }
            catch { }
            return -1;
        }

        public static int IssueCount(List<DiagItem> items)
        {
            int n = 0;
            for (int i = 0; i < items.Count; i++) if (items[i].Level > 0) n++;
            return n;
        }
    }
}
