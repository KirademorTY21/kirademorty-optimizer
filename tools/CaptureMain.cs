using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace KDT
{
    // Screenshot harness. Uses a real message loop (Application.Run) so the
    // app sees the same WindowsFormsSynchronizationContext it has in production;
    // pumping with DoEvents would push async continuations onto the thread pool.
    internal static class CaptureMain
    {
        static MainForm _form;
        static MethodInfo _setPage;
        static string _dir;
        static int _page;
        static int _wait;
        static Timer _timer;
        static bool _bench;
        static bool _welcomeShot;
        static readonly int[] Waits = new int[] { 1600, 2600, 900, 900, 900, 900, 900 };

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Engine.Init();
            try { SysInfo.LoadHardware(); } catch { }
            TweakCatalog.Build();
            bool plain = args.Length > 2 && args[2] == "plain";
            Engine.IntroSeen = plain;

            bool ar = args.Length > 0 && args[0] == "ar";
            _bench = args.Length > 1 && args[1] == "bench";
            bool wasArabic = Theme.Arabic;
            Theme.Arabic = ar;
            _dir = Path.Combine(Path.GetTempPath(), ar ? "kdt-shots-ar" : "kdt-shots");
            Directory.CreateDirectory(_dir);

            _form = new MainForm();
            _setPage = typeof(MainForm).GetMethod("SetPage", BindingFlags.NonPublic | BindingFlags.Instance);
            _form.Shown += delegate { Start(); };
            AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs ue)
            {
                LogError(ue.ExceptionObject.ToString());
            };
            Application.ThreadException += delegate(object s, System.Threading.ThreadExceptionEventArgs te)
            {
                LogError(te.Exception.ToString());
            };
            Application.Run(_form);

            Theme.Arabic = wasArabic;
            Engine.SaveSettings();

            int n = Engine.LogLines.Count;
            for (int i = Math.Max(0, n - 6); i < n; i++)
                Console.WriteLine("log: " + Engine.LogLines[i]);
        }

        static void Start()
        {
            _page = 0;
            _setPage.Invoke(_form, new object[] { 0 });
            _wait = Waits[0];
            _timer = new Timer();
            _timer.Interval = 100;
            _timer.Tick += Tick;
            _timer.Start();
        }

        static void Tick(object sender, EventArgs e)
        {
            _wait -= 100;
            if (_wait > 0) return;

            if (!_welcomeShot)
            {
                _welcomeShot = true;
                FieldInfo inf = typeof(MainForm).GetField("_intro", BindingFlags.NonPublic | BindingFlags.Instance);
                Control ov = inf == null ? null : inf.GetValue(_form) as Control;
                if (ov != null && ov.Visible)
                {
                    using (Bitmap bmp = new Bitmap(_form.Width, _form.Height))
                    {
                        _form.DrawToBitmap(bmp, new Rectangle(0, 0, _form.Width, _form.Height));
                        string wp = Path.Combine(_dir, "welcome.png");
                        bmp.Save(wp, ImageFormat.Png);
                        Console.WriteLine(wp + "  " + bmp.Width + "x" + bmp.Height);
                    }
                    MethodInfo close = typeof(MainForm).GetMethod("CloseIntro", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (close != null) close.Invoke(_form, null);
                    _wait = 500;
                    return;
                }
            }

            FieldInfo pf = typeof(MainForm).GetField("_page", BindingFlags.NonPublic | BindingFlags.Instance);
            Console.WriteLine("harness page=" + _page + "  form page=" + pf.GetValue(_form));

            if (_page == 0)
            {
                FieldInfo rbf = typeof(MainForm).GetField("_rebootPill", BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo rbs = typeof(MainForm).GetField("_rebootShow", BindingFlags.NonPublic | BindingFlags.Instance);
                Console.WriteLine("rebootShow=" + rbs.GetValue(_form) + "  pill=" + rbf.GetValue(_form) +
                                  "  measure=" + TextRenderer.MeasureText(L.T("tw.pendingReboot"), Theme.FTiny));
            }

            if (_page == 0 || _page == 2 || _page == 6)
            {
                StringBuilder ls = new StringBuilder();
                ls.AppendLine("== page " + _page + " ==");
                Dump(_form, 0, ls);
                File.WriteAllText(Path.Combine(_dir, "layout" + _page + ".txt"), ls.ToString());
            }

            using (Bitmap bmp = new Bitmap(_form.Width, _form.Height))
            {
                _form.DrawToBitmap(bmp, new Rectangle(0, 0, _form.Width, _form.Height));
                string path = Path.Combine(_dir, "page" + _page + ".png");
                bmp.Save(path, ImageFormat.Png);
                Console.WriteLine(path + "  " + bmp.Width + "x" + bmp.Height + "  state=" + _form.WindowState);
            }

            // Settings is taller than the window: also capture it scrolled to the end
            // so the channel card and the "never touch" list can be checked.
            if (_page == 6)
            {
                FieldInfo pgf = typeof(MainForm).GetField("_pages", BindingFlags.NonPublic | BindingFlags.Instance);
                Panel[] pages = pgf == null ? null : pgf.GetValue(_form) as Panel[];
                ScrollableControl sc = (pages == null || pages.Length < 7) ? null : FindScroll(pages[6]);
                if (sc != null)
                {
                    sc.VerticalScroll.Value = sc.VerticalScroll.Maximum;
                    using (Bitmap bmp = new Bitmap(_form.Width, _form.Height))
                    {
                        _form.DrawToBitmap(bmp, new Rectangle(0, 0, _form.Width, _form.Height));
                        string path = Path.Combine(_dir, "page6-bottom.png");
                        bmp.Save(path, ImageFormat.Png);
                        Console.WriteLine(path + "  max=" + sc.VerticalScroll.Maximum);
                    }
                    sc.VerticalScroll.Value = 0;
                }
            }

            if (_form.WindowState != FormWindowState.Normal)
            {
                Console.WriteLine("window was not normal; restoring");
                _form.WindowState = FormWindowState.Normal;
                _form.Activate();
                _wait = 600;
                return;
            }

            _page++;
            if (_page >= Waits.Length)
            {
                _timer.Stop();
                _form.Close();
                return;
            }

            _setPage.Invoke(_form, new object[] { _page });
            _wait = Waits[_page];
            if (_bench && _page == 3)
            {
                MethodInfo run = typeof(MainForm).GetMethod("RunBench", BindingFlags.NonPublic | BindingFlags.Instance);
                run.Invoke(_form, new object[] { null, EventArgs.Empty });
                _wait = 16000;
            }
        }

        static void LogError(string text)
        {
            try
            {
                File.AppendAllText(Path.Combine(Path.GetTempPath(), "kdt-capture-error.txt"),
                    DateTime.Now.ToString("HH:mm:ss") + "  " + text + "\r\n");
            }
            catch { }
        }

        // First AutoScroll container in the tree (the settings page uses one).
        static ScrollableControl FindScroll(Control root)
        {
            ScrollableControl s = root as ScrollableControl;
            if (s != null && s.AutoScroll) return s;
            for (int i = 0; i < root.Controls.Count; i++)
            {
                ScrollableControl hit = FindScroll(root.Controls[i]);
                if (hit != null) return hit;
            }
            return null;
        }

        // Diagnostics only: dump the live control tree so the screenshots can be
        // checked against real geometry instead of guessed pixel offsets.
        static void Dump(Control c, int depth, StringBuilder sb)
        {
            if (depth > 6) return;
            string ind = new string(' ', depth * 2);
            string txt = "";
            try { txt = c.Text; } catch { }
            if (txt != null && txt.Length > 40) txt = txt.Substring(0, 40);
            txt = txt.Replace("\r", " ").Replace("\n", " ");
            sb.AppendLine(ind + c.GetType().Name + "  " + c.Bounds.X + "," + c.Bounds.Y + " " +
                          c.Width + "x" + c.Height + (c.Visible ? "" : " [hidden]") +
                          (txt.Length > 0 ? "  \"" + txt + "\"" : ""));
            for (int i = 0; i < c.Controls.Count; i++)
                Dump(c.Controls[i], depth + 1, sb);
        }
    }
}
