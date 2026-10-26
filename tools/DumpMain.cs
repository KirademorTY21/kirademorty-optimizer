using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace KDT
{
    // Layout dump harness. Runs a real message loop for the same reason as
    // CaptureMain: async diagnostics need the WinForms synchronization context.
    internal static class DumpMain
    {
        static MainForm _form;
        static MethodInfo _setPage;
        static StringBuilder _sb;
        static int _page;
        static int _wait;
        static Timer _timer;
        static readonly int[] Waits = new int[] { 1500, 2600, 900, 900, 900, 900, 900 };

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Engine.Init();
            try { SysInfo.LoadHardware(); } catch { }
            TweakCatalog.Build();

            bool ar = args.Length > 0 && args[0] == "ar";
            Theme.Arabic = ar;

            _form = new MainForm();
            _setPage = typeof(MainForm).GetMethod("SetPage", BindingFlags.NonPublic | BindingFlags.Instance);
            _sb = new StringBuilder();
            _form.Shown += delegate { Start(); };
            Application.Run(_form);

            Theme.Arabic = ar ? false : Theme.Arabic;

            string path = Path.Combine(Path.GetTempPath(), ar ? "kdt-layout-ar.txt" : "kdt-layout.txt");
            File.WriteAllText(path, _sb.ToString(), Encoding.UTF8);
            Console.WriteLine(path);
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

            _sb.AppendLine("========== PAGE " + _page + " ==========");
            Walk(_form, _sb, 0);

            _page++;
            if (_page >= Waits.Length)
            {
                _timer.Stop();
                _form.Close();
                return;
            }

            _setPage.Invoke(_form, new object[] { _page });
            _wait = Waits[_page];
        }

        static void Walk(Control c, StringBuilder sb, int depth)
        {
            string t = c.Text;
            sb.Append(new string(' ', depth * 2));
            sb.Append(c.GetType().Name);
            sb.Append(" [").Append(c.Left).Append(",").Append(c.Top).Append(" ")
              .Append(c.Width).Append("x").Append(c.Height).Append("]");
            sb.Append(" vis=").Append(c.Visible);
            if (!string.IsNullOrEmpty(t))
                sb.Append(" text=\"").Append(t.Replace("\r", "").Replace("\n", "\\n")).Append("\"");
            sb.AppendLine();
            if (depth > 6) return;
            foreach (Control ch in c.Controls) Walk(ch, sb, depth + 1);
        }
    }
}
