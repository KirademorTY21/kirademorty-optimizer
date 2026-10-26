using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace KDT
{
    internal static class Report
    {
        public static string LastPath = "";

        static string H(string s)
        {
            if (s == null) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        // Inline SVG of the app mark: dark tile, warm glow, cream-to-orange K.
        static string Mark()
        {
            StringBuilder s = new StringBuilder();
            s.Append("<svg viewBox=\"0 0 100 100\" xmlns=\"http://www.w3.org/2000/svg\" role=\"img\" aria-label=\"KiraDemorTY\">");
            s.Append("<defs>");
            s.Append("<linearGradient id=\"kg\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\">");
            s.Append("<stop offset=\"0\" stop-color=\"#fff7ec\"/><stop offset=\".46\" stop-color=\"#ffc476\"/><stop offset=\"1\" stop-color=\"#ff760e\"/>");
            s.Append("</linearGradient>");
            s.Append("<radialGradient id=\"gg\" cx=\"0.48\" cy=\"0.5\" r=\"0.55\">");
            s.Append("<stop offset=\"0\" stop-color=\"#ff8a1e\" stop-opacity=\"0.30\"/><stop offset=\"1\" stop-color=\"#ff8a1e\" stop-opacity=\"0\"/>");
            s.Append("</radialGradient>");
            s.Append("<linearGradient id=\"tg\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\">");
            s.Append("<stop offset=\"0\" stop-color=\"#29232b\"/><stop offset=\"1\" stop-color=\"#0c0b0e\"/></linearGradient>");
            s.Append("</defs>");
            s.Append("<rect x=\"2\" y=\"2\" width=\"96\" height=\"96\" rx=\"22\" fill=\"url(#tg)\" stroke=\"#ff8a1e\" stroke-opacity=\".55\"/>");
            s.Append("<rect x=\"2\" y=\"2\" width=\"96\" height=\"96\" rx=\"22\" fill=\"url(#gg)\"/>");
            s.Append("<path d=\"M30 24 L30 76 M72 23 L35 50 L72 77\" fill=\"none\" stroke=\"url(#kg)\" stroke-width=\"11\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>");
            s.Append("</svg>");
            return s.ToString();
        }

        public static string Build(Sample sm, PingStat ping, int score)
        {
            bool ar = Theme.Arabic;
            string dir = ar ? "rtl" : "ltr";
            StringBuilder s = new StringBuilder();
            s.AppendLine("<!DOCTYPE html>");
            s.AppendLine("<html dir=\"" + dir + "\" lang=\"" + (ar ? "ar" : "en") + "\"><head>");
            s.AppendLine("<meta charset=\"utf-8\">");
            s.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
            s.AppendLine("<title>KiraDemorTY Optimizer - Report</title>");
            s.AppendLine("<style>");
            s.AppendLine(":root{--bg:#0c0c0e;--card:#16161a;--line:#2c282a;--text:#f2eee9;--muted:#a29a94;--accent:#ff8a1e;--warn:#ffc24b;--danger:#ff4d6d;}");
            s.AppendLine("*{box-sizing:border-box}");
            s.AppendLine("body{margin:0;background:var(--bg);color:var(--text);font-family:'Segoe UI',Tahoma,Arial,sans-serif;line-height:1.5}");
            s.AppendLine(".wrap{max-width:980px;margin:0 auto;padding:36px 22px 60px}");
            s.AppendLine(".brand{display:flex;align-items:center;gap:14px;margin-bottom:6px}");
            s.AppendLine(".mark{width:46px;height:46px;flex:0 0 46px}");
            s.AppendLine(".mark svg{display:block;width:46px;height:46px}");
            s.AppendLine("h1{font-size:26px;margin:0}");
            s.AppendLine("h2{font-size:16px;margin:30px 0 12px;color:var(--text)}");
            s.AppendLine(".sub{color:var(--muted);font-size:13px}");
            s.AppendLine(".grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:12px}");
            s.AppendLine(".card{background:var(--card);border:1px solid var(--line);border-radius:14px;padding:16px}");
            s.AppendLine(".k{color:var(--muted);font-size:12px;margin-bottom:4px}");
            s.AppendLine(".v{font-size:19px;font-weight:600}");
            s.AppendLine("table{width:100%;border-collapse:collapse;background:var(--card);border:1px solid var(--line);border-radius:14px;overflow:hidden;font-size:13px}");
            s.AppendLine("th,td{padding:10px 13px;text-align:start;border-bottom:1px solid var(--line)}");
            s.AppendLine("th{color:var(--muted);font-weight:600;font-size:12px}");
            s.AppendLine("tr:last-child td{border-bottom:none}");
            s.AppendLine(".on{color:var(--accent);font-weight:600}.off{color:var(--muted)}");
            s.AppendLine(".pill{display:inline-block;padding:2px 9px;border-radius:20px;font-size:11px;border:1px solid var(--line)}");
            s.AppendLine(".safe{color:var(--accent)}.moderate{color:var(--warn)}.advanced{color:var(--danger)}");
            s.AppendLine(".good{color:var(--accent);font-weight:600}.bad{color:var(--danger);font-weight:600}.flat{color:var(--muted)}");
            s.AppendLine("code{font-family:Consolas,monospace;font-size:12px;color:#cdbfb4;word-break:break-all}");
            s.AppendLine(".note{color:var(--muted);font-size:12px;margin-top:18px}");
            s.AppendLine("footer{margin-top:34px;color:var(--muted);font-size:12px;border-top:1px solid var(--line);padding-top:16px}");
            s.AppendLine("footer a{color:var(--accent);text-decoration:none}");
            s.AppendLine("</style></head><body><div class=\"wrap\">");

            s.AppendLine("<div class=\"brand\"><div class=\"mark\">" + Mark() + "</div><div><h1>KiraDemorTY Optimizer</h1>");
            s.AppendLine("<div class=\"sub\">" + (ar ? "تقرير التشخيص" : "Diagnostic report") + " &middot; " + Brand.VersionTag + "</div></div></div>");
            // Deliberately no computer name here: reports are meant to be shared and
            // posted publicly, and the hostname identifies the machine.
            s.AppendLine("<div class=\"sub\">" + (ar ? "أُنشئ في " : "Generated ") + DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + "</div>");

            s.AppendLine("<h2>" + (ar ? "الملخص" : "Summary") + "</h2>");
            s.AppendLine("<div class=\"grid\">");
            Card(s, ar ? "صحة النظام" : "System health", score + " / 100");
            Card(s, ar ? "تحسينات مطبقة" : "Optimizations applied", TweakCatalog.AppliedCount() + " / " + TweakCatalog.All.Count);
            Card(s, ar ? "البينغ" : "Ping", ping != null && ping.Ok ? ping.Avg + " ms" : "n/a");
            Card(s, ar ? "التذبذب / الفقد" : "Jitter / loss", ping != null && ping.Ok ? ping.Jitter + " ms / " + ping.Loss + "%" : "n/a");
            s.AppendLine("</div>");

            s.AppendLine("<h2>" + (ar ? "النظام" : "System") + "</h2>");
            s.AppendLine("<div class=\"grid\">");
            Card(s, "CPU", SysInfo.CpuName);
            Card(s, "GPU", SysInfo.GpuName);
            Card(s, "RAM", SysInfo.RamTotalGb.ToString("0.0", CultureInfo.InvariantCulture) + " GB");
            Card(s, ar ? "ويندوز" : "Windows", SysInfo.OsName);
            Card(s, ar ? "اللوحة" : "Motherboard", SysInfo.Board);
            Card(s, ar ? "قرص النظام" : "System drive", sm != null ? sm.FreeGb.ToString("0.0", CultureInfo.InvariantCulture) + " / " + sm.TotalGb.ToString("0.0", CultureInfo.InvariantCulture) + " GB" : "n/a");
            s.AppendLine("</div>");

            s.AppendLine("<h2>" + (ar ? "القراءات المباشرة" : "Live readings") + "</h2>");
            s.AppendLine("<div class=\"grid\">");
            Card(s, "CPU", sm != null ? sm.Cpu.ToString("0") + "%" : "n/a");
            Card(s, "RAM", sm != null ? sm.RamUsedGb.ToString("0.0", CultureInfo.InvariantCulture) + " / " + sm.RamTotalGb.ToString("0.0", CultureInfo.InvariantCulture) + " GB (" + sm.RamPct.ToString("0") + "%)" : "n/a");
            Card(s, "GPU", sm != null && sm.Gpu >= 0 ? sm.Gpu.ToString("0") + "%" : "n/a");
            Card(s, ar ? "مدة التشغيل" : "Uptime", SysInfo.Uptime());
            s.AppendLine("</div>");

            BenchResult bBefore = Bench.Load(Bench.BaselinePath);
            BenchResult bNow = Bench.Load(Bench.LastPath);
            if (bNow != null)
            {
                s.AppendLine("<h2>" + (ar ? "القياس: قبل / بعد" : "Benchmark: before / after") + "</h2>");
                s.AppendLine("<table><tr><th>" + (ar ? "المقياس" : "Metric") + "</th><th>" + (ar ? "قبل" : "Before") +
                             "</th><th>" + (ar ? "الآن" : "Now") + "</th><th>" + (ar ? "التغيّر" : "Change") + "</th></tr>");
                BenchRow(s, ar ? "تأخير الذاكرة" : "Memory latency", bBefore == null ? -1 : bBefore.MemNs, bNow.MemNs, true, "0.0", " ns");
                BenchRow(s, ar ? "قراءة عشوائية 4K" : "4K random read", bBefore == null ? -1 : bBefore.DiskMs, bNow.DiskMs, true, "0.000", " ms");
                BenchRow(s, ar ? "عمليات المعالج" : "CPU integer", bBefore == null ? -1 : bBefore.CpuMops, bNow.CpuMops, false, "0", " Mops/s");
                BenchRow(s, ar ? "البينغ" : "Ping", bBefore == null ? -1 : bBefore.PingMs, bNow.PingMs, true, "0", " ms");
                BenchRow(s, ar ? "التذبذب" : "Jitter", bBefore == null ? -1 : bBefore.JitterMs, bNow.JitterMs, true, "0", " ms");
                s.AppendLine("</table>");
                if (bBefore != null) s.AppendLine("<div class=\"note\">" + (ar ? "خط الأساس" : "Baseline") +
                    ": " + H(bBefore.Stamp) + " &middot; " + (ar ? "آخر قياس" : "latest") + ": " + H(bNow.Stamp) + "</div>");
            }

            s.AppendLine("<h2>" + (ar ? "التحسينات" : "Optimizations") + "</h2>");
            s.AppendLine("<table><tr><th>" + (ar ? "التحسين" : "Tweak") + "</th><th>" + (ar ? "الحالة" : "State") + "</th><th>" + (ar ? "المستوى" : "Risk") + "</th></tr>");
            for (int i = 0; i < TweakCatalog.All.Count; i++)
            {
                Tweak t = TweakCatalog.All[i];
                string r = t.Risk == 0 ? "safe" : (t.Risk == 1 ? "moderate" : "advanced");
                string rn = ar ? (t.Risk == 0 ? "آمن" : (t.Risk == 1 ? "متوسط" : "متقدم")) : (t.Risk == 0 ? "safe" : (t.Risk == 1 ? "moderate" : "advanced"));
                s.AppendLine("<tr><td>" + H(ar ? t.NameAr : t.NameEn) + "</td><td class=\"" + (t.On ? "on" : "off") + "\">" +
                             (t.On ? (ar ? "مطبّق" : "applied") : (ar ? "غير مطبّق" : "not applied")) +
                             "</td><td><span class=\"pill " + r + "\">" + rn + "</span></td></tr>");
            }
            s.AppendLine("</table>");

            s.AppendLine("<h2>" + (ar ? "التغييرات التي نفّذها البرنامج" : "Changes applied by KDT") + "</h2>");
            if (Engine.Ops.Count == 0)
            {
                s.AppendLine("<div class=\"card\">" + (ar ? "لا توجد تغييرات مسجلة." : "No changes recorded.") + "</div>");
            }
            else
            {
                s.AppendLine("<table><tr><th>" + (ar ? "التحسين" : "Tweak") + "</th><th>" + (ar ? "الهدف" : "Target") + "</th><th>" + (ar ? "القيمة السابقة" : "Previous value") + "</th><th>" + (ar ? "الوقت" : "Time") + "</th></tr>");
                for (int i = Engine.Ops.Count - 1; i >= 0; i--)
                {
                    Op o = Engine.Ops[i];
                    Tweak t = TweakCatalog.ById(o.TweakId);
                    string name = t == null ? o.TweakId : (ar ? t.NameAr : t.NameEn);
                    string target = o.Kind == "cmd" ? o.Undo : o.Note;
                    string prev = o.Kind == "cmd" ? "-" : (o.Existed ? o.OldEnc : (ar ? "لم تكن موجودة" : "did not exist"));
                    s.AppendLine("<tr><td>" + H(name) + "</td><td><code>" + H(target) + "</code></td><td><code>" + H(prev) + "</code></td><td>" + H(o.Time) + "</td></tr>");
                }
                s.AppendLine("</table>");
            }

            s.AppendLine("<h2>" + (ar ? "فحوصات إضافية" : "Extra checks") + "</h2>");
            s.AppendLine("<div class=\"grid\">");
            int trim = TweakCatalog.TrimState();
            Card(s, "TRIM (SSD)", trim == 0 ? (ar ? "مفعّل" : "enabled") : (trim == 1 ? (ar ? "معطّل" : "disabled") : "n/a"));
            Card(s, "VBS / HVCI", TweakCatalog.HypervisorOff() ? (ar ? "معطّل" : "off") : (ar ? "مفعّل" : "on"));
            Card(s, ar ? "نوع التقرير" : "Report type", ar ? "يدوي" : "manual");
            Card(s, ar ? "الخصوصية" : "Privacy", ar ? "كل البيانات محلية" : "all data local");
            s.AppendLine("</div>");

            s.AppendLine("<div class=\"note\">" + (ar
                ? "هذا التقرير يعرض الحالة الفعلية للجهاز. البرنامج لا يرسل أي بيانات إلى أي خادم. لا تعتمد على درجة الصحة كقياس دقيق؛ استخدم قياسات اللعب الفعلية (متوسط FPS وزمن الإطار) للحكم."
                : "This report shows the actual state of the machine. The tool never uploads data anywhere. Treat the health score as an indicator, not a laboratory measurement - judge results with real in-game FPS and frame-time numbers.") + "</div>");

            s.AppendLine("<footer>KiraDemorTY Optimizer " + Brand.VersionTag + " &middot; " + (ar ? "مفتوح المصدر" : "open source") + " &middot; " +
                         (ar ? "بدون تتبّع" : "no telemetry") + "<br>" +
                         (ar ? "قناة يوتيوب: " : "YouTube: ") +
                         "<a href=\"" + Brand.ChannelUrl + "\">" + H(Brand.ChannelName) + "</a> &middot; " +
                         "<a href=\"" + Brand.GithubUrl + "\">" + (ar ? "الكود المصدري" : "source code") + "</a></footer>");
            s.AppendLine("</div></body></html>");
            return s.ToString();
        }

        static void BenchRow(StringBuilder s, string name, double before, double now, bool lower, string fmt, string unit)
        {
            string b = before > 0 ? before.ToString(fmt, CultureInfo.InvariantCulture) + unit : "--";
            string n = now >= 0 ? now.ToString(fmt, CultureInfo.InvariantCulture) + unit : "--";
            string d = "--";
            string cls = "flat";
            if (before > 0 && now >= 0)
            {
                double p = Bench.Delta(now, before, lower);
                if (Math.Abs(p) < 0.5) { d = "0%"; cls = "flat"; }
                else
                {
                    d = (p > 0 ? "+" : "") + p.ToString("0.0", CultureInfo.InvariantCulture) + "%";
                    cls = p > 0 ? "good" : "bad";
                }
            }
            s.AppendLine("<tr><td>" + H(name) + "</td><td>" + H(b) + "</td><td>" + H(n) +
                         "</td><td class=\"" + cls + "\">" + H(d) + "</td></tr>");
        }

        static void Card(StringBuilder s, string k, string v)
        {
            s.AppendLine("<div class=\"card\"><div class=\"k\">" + H(k) + "</div><div class=\"v\">" + H(v) + "</div></div>");
        }

        public static string Generate(Sample sm, PingStat ping, int score)
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "KiraDemorTY Reports");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "KDT-Report-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".html");
            File.WriteAllText(path, Build(sm, ping, score), new UTF8Encoding(true));
            LastPath = path;
            return path;
        }

        public static string Latest()
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "KiraDemorTY Reports");
                if (!Directory.Exists(dir)) return "";
                string[] files = Directory.GetFiles(dir, "KDT-Report-*.html");
                if (files.Length == 0) return "";
                Array.Sort(files);
                return files[files.Length - 1];
            }
            catch
            {
                return "";
            }
        }

        public static void Open(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
                Process.Start(path);
            }
            catch { }
        }
    }
}
