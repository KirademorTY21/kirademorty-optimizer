using System;
using System.Collections.Generic;

namespace KDT
{
    internal static class L
    {
        static readonly Dictionary<string, string> En = new Dictionary<string, string>();
        static readonly Dictionary<string, string> Ar = new Dictionary<string, string>();

        static void A(string k, string en, string ar)
        {
            En[k] = en;
            Ar[k] = ar;
        }

        public static string T(string k)
        {
            Dictionary<string, string> d = Theme.Arabic ? Ar : En;
            string v;
            if (d.TryGetValue(k, out v)) return v;
            return k;
        }

        public static string Pick(string en, string ar)
        {
            return Theme.Arabic ? ar : en;
        }

        static L()
        {
            A("app.name", "KiraDemorTY Optimizer", "كيرا ديمورتي أوبتيميزر");
            A("app.tagline", "Measure. Tune. Prove it.", "قِس. اضبط. أثبت النتيجة.");
            A("app.version", "Version 1.2", "الإصدار 1.2");

            A("nav.dashboard", "Dashboard", "لوحة التحكم");
            A("nav.diag", "Diagnostics", "التشخيص");
            A("nav.tweaks", "Tweaks", "التحسينات");
            A("nav.bench", "Benchmark", "القياس");
            A("nav.backup", "Backup & Undo", "النسخ والتراجع");
            A("nav.report", "Report", "التقرير");
            A("nav.settings", "Settings", "الإعدادات");
            A("nav.admin", "Administrator", "مسؤول النظام");
            A("nav.limited", "Limited mode", "وضع محدود");

            A("dash.title", "System overview", "نظرة عامة على النظام");
            A("dash.sub", "Live readings from this machine", "قراءات مباشرة من هذا الجهاز");
            A("dash.health", "System health", "صحة النظام");
            A("dash.healthNote", "Indicative score based on memory, disk and network pressure.", "مؤشر تقريبي مبني على ضغط الذاكرة والقرص والشبكة.");
            A("dash.cpu", "Processor", "المعالج");
            A("dash.ram", "Memory", "الذاكرة");
            A("dash.gpu", "Graphics", "كرت الشاشة");
            A("dash.disk", "System drive", "قرص النظام");
            A("dash.net", "Network", "الشبكة");
            A("dash.applied", "Optimizations applied", "تحسينات مطبّقة");
            A("dash.of", "of", "من");
            A("dash.free", "free", "متاح");
            A("dash.used", "used", "مستخدم");
            A("dash.available", "available", "متاح");
            A("dash.ping", "Ping", "البينغ");
            A("dash.jitter", "Jitter", "التذبذب");
            A("dash.loss", "Loss", "الفقد");
            A("dash.na", "n/a", "غير متاح");
            A("dash.cores", "cores", "أنوية");
            A("dash.threads", "threads", "خيوط");
            A("dash.uptime", "Uptime", "مدة التشغيل");
            A("dash.sysinfo", "System information", "معلومات النظام");
            A("dash.machine", "Machine", "الجهاز");
            A("dash.os", "Windows", "ويندوز");
            A("dash.gpuName", "GPU", "كرت الشاشة");
            A("dash.cpuName", "CPU", "المعالج");
            A("dash.ramTotal", "Installed RAM", "الرام المثبتة");
            A("dash.scanning", "Scanning hardware...", "جارٍ فحص العتاد...");
            A("dash.ms", "ms", "مللي");
            A("dash.host", "1.1.1.1", "1.1.1.1");

            A("tw.title", "Performance tweaks", "تحسينات الأداء");
            A("tw.sub", "Every change is journaled and reversible. A restore point is created first.", "كل تغيير مسجّل وقابل للتراجع، مع إنشاء نقطة استعادة قبل التطبيق.");
            A("tw.recommended", "Apply Recommended", "تطبيق الموصى به");
            A("tw.recommendedHint", "Applies all safe and moderate tweaks. Advanced tweaks are left for you.", "يطبّق كل التحسينات الآمنة والمتوسطة، ويترك المتقدمة لك.");
            A("tw.safe", "SAFE", "آمن");
            A("tw.moderate", "MODERATE", "متوسط");
            A("tw.advanced", "ADVANCED", "متقدم");
            A("tw.restart", "Restart required", "يحتاج إعادة تشغيل");
            A("tw.applied", "Applied", "مطبّق");
            A("tw.notApplied", "Not applied", "غير مطبّق");
            A("tw.applying", "Working...", "جارٍ العمل...");
            A("tw.confirmAdvanced", "This is an advanced tweak:\n\n{0}\n\n{1}\n\nContinue?", "هذا تحسين متقدم:\n\n{0}\n\n{1}\n\nهل تريد المتابعة؟");
            A("tw.confirmProfile", "Apply {0} recommended tweaks now?", "هل تريد تطبيق {0} تحسينًا موصى به الآن؟");
            A("tw.rebootBanner", "Some applied tweaks need a Windows restart to take full effect.", "بعض التحسينات المطبقة تحتاج إعادة تشغيل ويندوز لتأخذ مفعولها كاملًا.");
            A("tw.noneSelected", "Nothing to apply.", "لا يوجد ما يُطبّق.");

            A("diag.title", "Diagnostics", "التشخيص");
            A("diag.sub", "Read-only checks, each with a one-click fix", "فحوصات للقراءة فقط، ولكل منها إصلاح بضغطة");
            A("diag.scan", "Run scan", "إعادة الفحص");
            A("diag.scanning", "Scanning...", "جارٍ الفحص...");
            A("diag.summary", "{0} of {1} checks need attention", "{0} من {1} فحصًا يحتاج انتباهًا");
            A("diag.allGood", "All {0} checks passed", "نجحت كل الفحوصات ({0})");
            A("diag.ok", "OK", "سليم");
            A("diag.attention", "ATTENTION", "انتباه");
            A("diag.problem", "PROBLEM", "مشكلة");
            A("diag.fix", "Fix", "إصلاح");
            A("diag.via", "tweak:", "التحسين:");
            A("diag.fixed", "Applied {0}. Re-checking...", "تم تطبيق {0}. جارٍ إعادة الفحص...");
            A("diag.readonly", "These checks never change your PC by themselves. Every fix is a normal journaled tweak you can undo.", "هذه الفحوصات لا تغيّر جهازك من تلقاء نفسها. كل إصلاح هو تحسين عادي مسجّل ويمكن التراجع عنه.");

            A("bench.title", "Before / after", "قبل / بعد");
            A("bench.sub", "Local, repeatable measurements", "قياسات محلية قابلة للتكرار");
            A("bench.empty", "Run a measurement to record your baseline.", "نفّذ قياسًا لتسجيل خط الأساس.");
            A("bench.running", "Measuring... this takes about 10 seconds.", "جارٍ القياس... يستغرق نحو 10 ثوانٍ.");
            A("bench.run", "Run measurement", "تنفيذ القياس");
            A("bench.setBase", "Set baseline to current", "تعيين خط الأساس للحالي");
            A("bench.baseline", "Baseline", "خط الأساس");
            A("bench.none", "not set yet", "غير محدد");
            A("bench.metric", "Metric", "المقياس");
            A("bench.beforeCol", "Before", "قبل");
            A("bench.nowCol", "Now", "الآن");
            A("bench.change", "Change", "التغيّر");
            A("bench.mem", "Memory latency (ns)", "تأخير الذاكرة (نانوثانية)");
            A("bench.disk", "4K random read (ms)", "قراءة عشوائية 4K (مللي)");
            A("bench.cpu", "CPU integer (Mops/s)", "عمليات المعالج (مليون/ث)");
            A("bench.ping", "Ping 1.1.1.1 (ms)", "بينغ 1.1.1.1 (مللي)");
            A("bench.jitter", "Jitter (ms)", "التذبذب (مللي)");
            A("bench.note", "Orange means better. Lower is better for latency, disk, ping and jitter.", "البرتقالي يعني تحسّنًا. الأقل أفضل للتأخير والقرص والبينغ والتذبذب.");
            A("bench.baselineSaved", "Baseline updated.", "تم تحديث خط الأساس.");
            A("bench.done", "Measurement saved to {0}", "تم حفظ القياس في {0}");
            A("bench.noData", "Run a measurement first.", "نفّذ قياسًا أولًا.");
            A("bench.firstRun", "First run stored as your baseline.", "أول قياس حُفظ كخط أساس.");

            A("bk.title", "Backup & undo", "النسخ الاحتياطي والتراجع");
            A("bk.sub", "Restore points, a full change journal, and one-click rollback.", "نقاط استعادة، وسجل كامل للتغييرات، وتراجع بضغطة واحدة.");
            A("bk.restore", "System Restore", "استعادة النظام");
            A("bk.restoreDesc", "Creates a Windows restore point before changes are made. Windows may skip creation if a point was made in the last 24 hours.", "ينشئ نقطة استعادة ويندوز قبل إجراء التغييرات. قد يتخطاه ويندوز إذا أُنشئت نقطة خلال آخر 24 ساعة.");
            A("bk.create", "Create restore point", "إنشاء نقطة استعادة");
            A("bk.undo", "Undo changes", "التراجع عن التغييرات");
            A("bk.undoDesc", "Reverts every change made by this tool, newest first. Registry values return to their exact previous data.", "يعكس كل تغيير نفّذه البرنامج من الأحدث للأقدم، وتعود قيم الريجستري إلى بياناتها السابقة بالضبط.");
            A("bk.undoAll", "Undo all changes", "التراجع عن كل التغييرات");
            A("bk.journal", "Change journal", "سجل التغييرات");
            A("bk.entries", "active change(s)", "تغيير نشط");
            A("bk.folder", "Data folder", "مجلد البيانات");
            A("bk.open", "Open folder", "فتح المجلد");
            A("bk.safety", "Safety model", "نموذج الأمان");
            A("bk.safetyDesc", "1) Restore point before the first change.\n2) Every registry value is saved with its exact type and data.\n3) Power and boot settings store the previous value in the undo command.\n4) Nothing touches personal files, and no data leaves this machine.", "١) نقطة استعادة قبل أول تغيير.\n٢) كل قيمة ريجستري تُحفظ بنوعها وبياناتها بالضبط.\n٣) إعدادات الطاقة والإقلاع تحفظ القيمة السابقة داخل أمر التراجع.\n٤) لا يلمس أي ملف شخصي، ولا تُرسل أي بيانات خارج الجهاز.");
            A("bk.none", "No changes recorded yet.", "لا توجد تغييرات مسجلة بعد.");
            A("bk.confirmUndo", "Undo every change made by KDT?\n\nThis restores the exact previous values.", "هل تريد التراجع عن كل تغييرات البرنامج؟\n\nستعود القيم السابقة بالضبط.");
            A("bk.openFailed", "Could not open the folder.", "تعذر فتح المجلد.");
            A("bk.exportTitle", "Undo script", "سكربت التراجع");
            A("bk.exportDesc", "Export a PowerShell script that reverts every journaled change. Keep it next to the tool so you can restore this PC even without the app.", "صدّر سكربت PowerShell يعكس كل تغيير مسجّل. احتفظ به بجانب البرنامج لتستعيد جهازك حتى بدون التطبيق.");
            A("bk.export", "Export undo script", "تصدير سكربت التراجع");
            A("bk.exported", "Undo script saved to {0}", "تم حفظ سكربت التراجع في {0}");

            A("rp.title", "Diagnostic report", "تقرير التشخيص");
            A("rp.sub", "Share real numbers, not marketing claims.", "شارك أرقامًا حقيقية، لا وعودًا تسويقية.");
            A("rp.generate", "Generate HTML report", "إنشاء تقرير HTML");
            A("rp.open", "Open last report", "فتح آخر تقرير");
            A("rp.desc", "The report contains your hardware, live readings, the state of every tweak, and the exact list of changes KDT made. Open it in any browser, print it to PDF, or attach it to a support post.", "يحتوي التقرير على عتادك، والقراءات المباشرة، وحالة كل تحسين، وقائمة التغييرات التي نفّذها البرنامج. افتحه في أي متصفح، أو اطبعه PDF، أو أرفقه في منشور الدعم.");
            A("rp.preview", "Summary", "الملخص");
            A("rp.none", "No report generated yet.", "لم يُنشأ أي تقرير بعد.");

            A("st.title", "Settings", "الإعدادات");
            A("st.sub", "Preferences and transparency", "التفضيلات والشفافية");
            A("st.lang", "Interface language", "لغة الواجهة");
            A("st.langDesc", "Switch between Arabic and English instantly.", "بدّل بين العربية والإنجليزية فورًا.");
            A("st.restore", "Restore point before changes", "نقطة استعادة قبل التغييرات");
            A("st.restoreDesc", "Strongly recommended. Disable only if System Restore is unavailable on this PC.", "مستحسن بشدة. لا تُعطّله إلا إذا كانت استعادة النظام غير متاحة على هذا الجهاز.");
            A("st.about", "About", "حول البرنامج");
            A("st.aboutDesc", "KDT v1.2 - open source, no telemetry, no bundled offers, no fake FPS claims.", "KDT الإصدار 1.2 — مفتوح المصدر، بدون تتبّع، بدون إعلانات، وبدون ادعاءات FPS وهمية.");
            A("st.github", "Source code on GitHub", "الكود المصدري على GitHub");
            A("st.channel", "YouTube channel", "قناة يوتيوب");
            A("st.channelDesc", "Gameplay, guides and real before/after tests. New videos appear there first.", "ألعاب، شروحات، واختبارات حقيقية قبل/بعد. الفيديوهات الجديدة تُنشر هناك أولًا.");
            A("st.replay", "Show welcome page", "إظهار صفحة الترحيب");
            A("st.data", "Open data folder", "فتح مجلد البيانات");
            A("st.diagnostics", "What the tool never does", "ما لا يفعله البرنامج أبدًا");
            A("st.neverDesc", "- No registry cleaning\n- No fake RAM boosters\n- No disabling Defender or Windows Update\n- No driver installers or bundled software\n- No deleting personal files", "- لا تنظيف للريجستري\n- لا مقويات رام وهمية\n- لا تعطيل للدفيندر أو تحديثات ويندوز\n- لا مثبّتات تعاريف أو برامج مرفقة\n- لا حذف للملفات الشخصية");

            A("tw.pendingReboot", "Restart to finish", "أعد التشغيل للإكمال");
            A("tw.filterAll", "All", "الكل");
            A("tw.filterSafe", "Safe", "آمن");
            A("tw.filterModerate", "Moderate", "متوسط");
            A("tw.filterAdvanced", "Advanced", "متقدم");
            A("tw.counter", "{0} of {1} applied", "{0} من {1} مطبّق");
            A("tw.noMatch", "No tweaks in this group.", "لا توجد تحسينات في هذه المجموعة.");

            A("channel.title", "YouTube channel", "قناة يوتيوب");
            A("channel.open", "Open channel", "فتح القناة");
            A("channel.madeBy", "Made by", "صنعها");
            A("channel.short", "YouTube · KiraDemorty", "يوتيوب · KiraDemorty");
            A("channel.desc", "Gameplay, guides and real performance tests.", "ألعاب، شروحات، واختبارات أداء حقيقية.");
            A("channel.tip", "Opens the channel in your browser.", "تفتح القناة في المتصفح.");

            A("intro.hello", "Welcome to", "أهلًا بك في");
            A("intro.lead", "A safe, honest Windows tuner. Every change is journaled, reversible, and measured before and after.", "أداة ويندوز آمنة وصادقة. كل تغيير يُسجَّل، ويمكن التراجع عنه، ويُقاس قبل وبعد.");
            A("intro.p1", "Safe by design", "آمن بالتصميم");
            A("intro.d1", "A restore point before the first change, and a full journal of every value.", "نقطة استعادة قبل أول تغيير، وسجل كامل لكل قيمة.");
            A("intro.p2", "Honest numbers", "أرقام صادقة");
            A("intro.d2", "Real memory, disk, CPU and ping measurements - no fake FPS claims.", "قياس حقيقي للذاكرة والقرص والمعالج والبينغ — بدون ادعاءات FPS وهمية.");
            A("intro.p3", "One-click undo", "تراجع بضغطة");
            A("intro.d3", "Restore the exact previous values, even without the app.", "أعِد القيم السابقة بالضبط، حتى بدون البرنامج.");
            A("intro.start", "Start tuning", "ابدأ الآن");
            A("intro.note", "This page appears once. Press F1 to open it any time.", "تظهر هذه الصفحة مرة واحدة فقط. اضغط F1 لإظهارها في أي وقت.");
            A("intro.admin", "Running as Administrator", "يعمل كمسؤول");

            A("common.cancel", "Cancel", "إلغاء");
            A("common.close", "Close", "إغلاق");
            A("common.ok", "OK", "موافق");
            A("common.yes", "Yes", "نعم");
            A("common.no", "No", "لا");
            A("common.refresh", "Refresh", "تحديث");
            A("common.ar", "AR", "ع");
            A("common.en", "EN", "EN");

            A("msg.title", "KiraDemorTY Optimizer", "كيرا ديمورتي أوبتيميزر");
            A("msg.done", "Done", "تم");
            A("msg.fail", "Failed", "فشل");
            A("msg.needReboot", "Restart Windows to finish applying these changes.", "أعد تشغيل ويندوز لإكمال تطبيق هذه التغييرات.");
            A("msg.undoDone", "All changes were undone.", "تم التراجع عن كل التغييرات.");
            A("msg.safetyFail", "Could not create a restore point:\n{0}\n\nContinue without one?", "تعذر إنشاء نقطة استعادة:\n{0}\n\nهل تريد المتابعة بدونها؟");
            A("msg.admin", "This tool must run as Administrator to change system settings.", "يجب تشغيل البرنامج كمسؤول لتغيير إعدادات النظام.");

            A("status.ready", "Ready", "جاهز");
            A("status.restore", "Creating a restore point...", "جارٍ إنشاء نقطة استعادة...");
            A("status.restoreOk", "Restore point requested successfully.", "تم طلب نقطة الاستعادة بنجاح.");
            A("status.applied", "Applied: {0}", "تم تطبيق: {0}");
            A("status.reverted", "Reverted: {0}", "تم التراجع عن: {0}");
            A("status.skipped", "Skipped: {0}", "تم التخطي: {0}");
            A("status.report", "Report saved to {0}", "تم حفظ التقرير في {0}");
            A("status.ping", "Checking network...", "جارٍ فحص الشبكة...");
            A("status.scan", "Reading hardware...", "جارٍ قراءة العتاد...");
            A("status.diag", "Scanning system...", "جارٍ فحص النظام...");
            A("status.bench", "Measuring memory, disk and CPU...", "جارٍ قياس الذاكرة والقرص والمعالج...");
            A("status.export", "Exporting undo script...", "جارٍ تصدير سكربت التراجع...");

            A("risk.safe", "Safe. Standard Windows settings with no side effects.", "آمن. إعدادات ويندوز قياسية بلا آثار جانبية.");
            A("risk.moderate", "Moderate. Changes power or scheduler behaviour; reversible.", "متوسط. يغيّر سلوك الطاقة أو الجدولة، وقابل للتراجع.");
            A("risk.advanced", "Advanced. Requires a restart and can affect security or compatibility.", "متقدم. يحتاج إعادة تشغيل وقد يؤثر على الأمان أو التوافق.");
        }
    }
}
