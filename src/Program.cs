using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace KDT
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            bool created = false;
            try
            {
                using (Mutex mutex = new Mutex(true, "KiraDemorTY.Optimizer.SingleInstance", out created))
                {
                    if (!created)
                    {
                        MessageBox.Show("KiraDemorTY Optimizer is already running.", "KiraDemorTY Optimizer",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.ThreadException += delegate(object s, ThreadExceptionEventArgs e)
                    {
                        Crash(e.Exception);
                    };
                    AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
                    {
                        Crash(e.ExceptionObject as Exception);
                    };

                    Engine.Init();
                    try { SysInfo.LoadHardware(); }
                    catch (Exception ex) { Engine.Log("hardware: " + ex.Message); }
                    TweakCatalog.Build();

                    Application.Run(new MainForm());
                }
            }
            catch (Exception ex)
            {
                Crash(ex);
            }
        }

        static void Crash(Exception ex)
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KiraDemorTY Optimizer");
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "crash.log"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                    (ex == null ? "unknown" : ex.ToString()) + Environment.NewLine + Environment.NewLine);
            }
            catch { }
            try
            {
                MessageBox.Show(ex == null ? "Unknown error" : ex.Message, "KiraDemorTY Optimizer",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { }
        }
    }
}
