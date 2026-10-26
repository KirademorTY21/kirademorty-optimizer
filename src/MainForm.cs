using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KDT
{
    internal class MainForm : Form
    {
        Panel _side;
        Panel _top;
        Panel _host;
        Panel _status;
        Label _topTitle;
        Label _topSub;
        Label _statusLabel;
        NavItem[] _nav;
        KButton _langBtn;
        Panel[] _pages;
        int _page = 0;

        readonly string[] _ids = new string[] { "dash", "diag", "tw", "bench", "bk", "rp", "st" };

        StatCard _cpuCard;
        StatCard _ramCard;
        StatCard _gpuCard;
        StatCard _diskCard;
        StatCard _netCard;
        HealthCard _health;
        InfoCard _info;
        Label _restoreInfo;
        Label _journalCount;
        TextBox _preview;
        KButton _openReport;
        readonly List<TweakRow> _rows = new List<TweakRow>();

        WelcomeOverlay _intro;
        KButton _ytBtn;
        KButton[] _twChips;
        Label _twCount;
        int _twFilter = 0;
        Rectangle _rebootPill;
        bool _rebootShow;

        Panel _diagList;
        Label _diagSummary;
        KButton _diagScan;
        readonly List<DiagRow> _diagRows = new List<DiagRow>();
        bool _diagBusy;
        int _diagIssues = -1;

        BenchTable _benchTable;
        KButton _benchRun;
        KButton _benchBaseline;
        bool _benchBusy;
        BenchResult _benchBefore;
        BenchResult _benchNow;

        Timer _timer;
        Timer _pingTimer;
        bool _sampling;
        bool _pingBusy;
        bool _busy;
        bool _safetyDone;
        int _ticks;
        Sample _last;
        PingStat _ping;
        int _score = 0;
        int _applied = 0;
        int _trim = -1;
        string _restoreMsg = "";

        readonly Dictionary<string, bool> _state = new Dictionary<string, bool>();
        readonly List<float> _cpuTrend = new List<float>();
        readonly List<float> _ramTrend = new List<float>();
        readonly List<float> _gpuTrend = new List<float>();
        readonly List<float> _netTrend = new List<float>();
        readonly ToolTip _tips = new ToolTip();

        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        public MainForm()
        {
            DoubleBuffered = true;
            Text = "KiraDemorTY Optimizer";
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            Font = Theme.FBody;
            ClientSize = new Size(1200, 780);
            MinimumSize = new Size(1080, 700);
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96f, 96f);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { }

            ScanStates();
            _trim = TweakCatalog.TrimState();

            BuildShell();
            RebuildUi();
            StartTimers();

            _rebootShow = Engine.RebootStillPending();
            if (!Engine.IntroSeen) ShowIntro();
        }

        void ScanStates()
        {
            _state.Clear();
            for (int i = 0; i < TweakCatalog.All.Count; i++)
            {
                Tweak t = TweakCatalog.All[i];
                _state[t.Id] = t.On;
            }
            _applied = CountStates();
        }

        int CountStates()
        {
            int n = 0;
            foreach (KeyValuePair<string, bool> kv in _state) if (kv.Value) n++;
            return n;
        }

        bool IsOn(Tweak t)
        {
            bool v;
            if (_state.TryGetValue(t.Id, out v)) return v;
            return false;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int v = 1;
                DwmSetWindowAttribute(Handle, 20, ref v, 4);
                DwmSetWindowAttribute(Handle, 19, ref v, 4);
            }
            catch { }
        }

        // ------------------------------------------------------------ shell

        void BuildShell()
        {
            _side = new Panel();
            _side.BackColor = Theme.Side;
            _side.Paint += PaintSide;

            _status = new Panel();
            _status.BackColor = Theme.Side;
            _status.Paint += PaintStatus;

            _top = new Panel();
            _top.BackColor = Theme.Bg;
            _top.Paint += PaintTop;

            _host = new Panel();
            _host.BackColor = Theme.Bg;

            // Absolute shell geometry. Dock layout puts a Fill panel in front of
            // later-docked siblings, which lets page content slide under the top
            // bar; explicit bounds keep the three bands and the page area exact.
            Controls.Add(_host);
            Controls.Add(_top);
            Controls.Add(_status);
            Controls.Add(_side);
            Resize += delegate { LayoutShell(); };
            LayoutShell();

            _topTitle = new Label();
            _topTitle.AutoSize = false;
            _topTitle.Font = Theme.FTitle;
            _topTitle.ForeColor = Theme.Text;
            _topTitle.BackColor = Theme.Bg;
            _topTitle.TextAlign = ContentAlignment.MiddleLeft;
            _top.Controls.Add(_topTitle);

            _topSub = new Label();
            _topSub.AutoSize = false;
            _topSub.Font = Theme.FSmall;
            _topSub.ForeColor = Theme.Muted;
            _topSub.BackColor = Theme.Bg;
            _topSub.TextAlign = ContentAlignment.MiddleLeft;
            _top.Controls.Add(_topSub);

            _statusLabel = new Label();
            _statusLabel.AutoSize = false;
            _statusLabel.Font = Theme.FSmall;
            _statusLabel.ForeColor = Theme.Muted;
            _statusLabel.BackColor = Theme.Side;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            _status.Controls.Add(_statusLabel);
            SetStatus(L.T("status.ready"));

            _nav = new NavItem[7];
            for (int i = 0; i < 7; i++)
            {
                NavItem n = new NavItem();
                n.Icon = i;
                n.Tag = i;
                n.Click += NavClick;
                _side.Controls.Add(n);
                _nav[i] = n;
            }

            _langBtn = new KButton("AR / EN");
            _langBtn.Kind = BtnKind.Ghost;
            _langBtn.Font = Theme.FSmall;
            _langBtn.BackColor = Theme.Side;
            _langBtn.Click += ToggleLanguage;
            _side.Controls.Add(_langBtn);

            _ytBtn = new KButton(L.T("channel.short"));
            _ytBtn.Kind = BtnKind.Ghost;
            _ytBtn.Tint = Theme.Ember;
            _ytBtn.Font = Theme.FSmall;
            _ytBtn.BackColor = Theme.Side;
            _ytBtn.Click += delegate { Engine.OpenUrl(Brand.ChannelUrl); };
            _tips.SetToolTip(_ytBtn, L.T("channel.tip") + "\n" + Brand.ChannelUrl);
            _side.Controls.Add(_ytBtn);

            _top.Resize += delegate { LayoutTop(); };
            _top.MouseClick += delegate(object s, MouseEventArgs me)
            {
                if (_rebootShow && _rebootPill.Contains(me.Location)) SetPage(2);
            };
            _top.MouseMove += delegate(object s, MouseEventArgs me)
            {
                Cursor wanted = (_rebootShow && _rebootPill.Contains(me.Location)) ? Cursors.Hand : Cursors.Default;
                if (_top.Cursor != wanted) _top.Cursor = wanted;
            };
            _status.Resize += delegate
            {
                _statusLabel.Bounds = new Rectangle(20, 0, _status.Width - 160, _status.Height);
            };
            _side.Resize += delegate { LayoutSide(); };

            LayoutTop();
            LayoutSide();
        }

        void PaintSide(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Theme.Side);

            using (Pen line = new Pen(Theme.Line))
                g.DrawLine(line, _side.Width - 1, 0, _side.Width - 1, _side.Height);

            bool rtl = Theme.Arabic;
            int m = 20;
            Rectangle logo = rtl
                ? new Rectangle(_side.Width - m - 42, 22, 42, 42)
                : new Rectangle(m, 22, 42, 42);

            // Soft orange glow behind the logo mark.
            using (GraphicsPath glow = new GraphicsPath())
            {
                Rectangle gr = new Rectangle(logo.X - 18, logo.Y - 18, logo.Width + 36, logo.Height + 36);
                glow.AddEllipse(gr);
                using (PathGradientBrush pb = new PathGradientBrush(glow))
                {
                    pb.CenterColor = Theme.A(Theme.Accent, 60);
                    pb.SurroundColors = new Color[] { Theme.A(Theme.Accent, 0) };
                    g.FillPath(pb, glow);
                }
            }

            Theme.DrawMark(g, logo);

            int tx = rtl ? _side.Width - m - 42 - 10 - 150 : m + 54;
            Rectangle nameR = new Rectangle(tx, 20, 168, 24);
            Theme.Draw(g, "KiraDemorTY", Theme.FBold, Theme.Text, nameR, Theme.FL(rtl));
            Rectangle tagR = new Rectangle(tx, 43, 168, 18);
            Theme.Draw(g, "OPTIMIZER " + Brand.VersionTag, Theme.FTiny, Theme.Accent, tagR, Theme.FL(rtl));

            using (Pen line = new Pen(Theme.Line))
                g.DrawLine(line, 16, 100, _side.Width - 16, 100);

            string ver = Theme.Arabic ? "الإصدار " + Brand.Version + " · مفتوح المصدر" : Brand.VersionTag + " · open source";
            Theme.Draw(g, ver, Theme.FTiny, Theme.Dim,
                new Rectangle(16, _side.Height - 134, _side.Width - 32, 18), Theme.FL(rtl));
        }

        void PaintStatus(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Theme.Side);
            using (Pen line = new Pen(Theme.Line))
                g.DrawLine(line, 0, 0, _status.Width, 0);
            Theme.Draw(g, "KiraDemorTY Optimizer", Theme.FTiny, Theme.Dim,
                new Rectangle(_status.Width - 170, 0, 156, _status.Height),
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        void PaintTop(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Theme.Bg);
            using (Pen line = new Pen(Theme.Line))
                g.DrawLine(line, 0, _top.Height - 1, _top.Width, _top.Height - 1);

            bool admin = Engine.IsAdmin;
            string txt = admin ? L.T("nav.admin") : L.T("nav.limited");
            Color c = admin ? Theme.Accent : Theme.Warn;
            int w = TextRenderer.MeasureText(txt, Theme.FTiny).Width + 32;
            Rectangle r = Theme.Arabic
                ? new Rectangle(20, 26, w, 24)
                : new Rectangle(_top.Width - w - 24, 26, w, 24);
            Theme.Fill(g, r, 12, Theme.A(c, 20));
            using (SolidBrush b = new SolidBrush(c))
                g.FillEllipse(b, r.X + 10, r.Y + 9, 6, 6);
            Theme.Draw(g, txt, Theme.FTiny, c, new Rectangle(r.X + 21, r.Y, r.Width - 25, r.Height),
                TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            _rebootShow = Engine.RebootStillPending();
            if (_rebootShow)
            {
                string rb = L.T("tw.pendingReboot");
                int rw = TextRenderer.MeasureText(rb, Theme.FTiny).Width + 34;
                Rectangle rr = Theme.Arabic
                    ? new Rectangle(r.Right + 10, 26, rw, 24)
                    : new Rectangle(r.X - 10 - rw, 26, rw, 24);
                _rebootPill = rr;
                Theme.Fill(g, rr, 12, Theme.A(Theme.Warn, 22));
                Theme.Stroke(g, rr, 12, Theme.A(Theme.Warn, 70));
                using (SolidBrush b = new SolidBrush(Theme.Warn))
                    g.FillEllipse(b, rr.X + 10, rr.Y + 9, 6, 6);
                Theme.Draw(g, rb, Theme.FTiny, Theme.Warn,
                    new Rectangle(rr.X + 21, rr.Y, rr.Width - 25, rr.Height),
                    TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
            else
            {
                _rebootPill = Rectangle.Empty;
            }
        }

        void LayoutShell()
        {
            if (_side == null || _top == null || _status == null || _host == null) return;
            int w = ClientSize.Width;
            int h = ClientSize.Height;
            if (w < 320 || h < 240) return;

            const int sideW = 238;
            const int topH = 80;
            const int statusH = 32;

            _side.Bounds = new Rectangle(0, 0, sideW, h);
            _top.Bounds = new Rectangle(sideW, 0, w - sideW, topH);
            _status.Bounds = new Rectangle(sideW, h - statusH, w - sideW, statusH);
            _host.Bounds = new Rectangle(sideW, topH, w - sideW, h - topH - statusH);

            if (_intro != null && _intro.Visible) _intro.Bounds = ClientRectangle;
        }

        // Width of the status pills painted at the end of PaintTop. Headings have
        // to stop before them; when the reboot notice appears the block grows and
        // the heading has to reflow, or the pill gets painted underneath the
        // title label.
        int TopPillBlock()
        {
            string badge = Engine.IsAdmin ? L.T("nav.admin") : L.T("nav.limited");
            int block = TextRenderer.MeasureText(badge, Theme.FTiny).Width + 32 + 24;
            if (Engine.RebootStillPending())
                block += 10 + TextRenderer.MeasureText(L.T("tw.pendingReboot"), Theme.FTiny).Width + 34;
            return block;
        }

        void LayoutTop()
        {
            if (_top == null || _topTitle == null) return;
            bool rtl = Theme.Arabic;
            int block = TopPillBlock() + 18;
            int x;
            int w;
            if (rtl)
            {
                // Pills sit at the left edge in RTL, headings are right aligned.
                x = 26 + block;
                w = _top.Width - x - 26;
            }
            else
            {
                x = 26;
                w = _top.Width - block - 26;
            }
            if (w < 180) w = 180;
            _topTitle.Bounds = new Rectangle(x, 12, w, 34);
            _topSub.Bounds = new Rectangle(x + (rtl ? 2 : 2), 47, w, 20);
            _topTitle.TextAlign = rtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
            _topSub.TextAlign = rtl ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
            // Keep base direction LTR so TextAlign is not mirrored by WinForms;
            // the bidi algorithm still renders Arabic correctly.
            _topTitle.RightToLeft = RightToLeft.No;
            _topSub.RightToLeft = RightToLeft.No;
        }

        void LayoutSide()
        {
            if (_side == null) return;
            int y = 116;
            for (int i = 0; i < _nav.Length; i++)
            {
                if (_nav[i] == null) continue;
                _nav[i].Bounds = new Rectangle(14, y, _side.Width - 28, 46);
                y += 52;
            }
            if (_langBtn != null)
                _langBtn.Bounds = new Rectangle(14, _side.Height - 62, _side.Width - 28, 36);
            if (_ytBtn != null)
                _ytBtn.Bounds = new Rectangle(14, _side.Height - 106, _side.Width - 28, 36);
            _side.Invalidate();
        }

        void NavClick(object sender, EventArgs e)
        {
            NavItem n = sender as NavItem;
            if (n == null) return;
            SetPage((int)n.Tag);
        }

        void SetPage(int i)
        {
            if (_pages == null || i < 0 || i >= _pages.Length) return;
            _page = i;
            for (int k = 0; k < _pages.Length; k++)
            {
                if (_pages[k] == null) continue;
                _pages[k].Visible = (k == i);
            }
            if (_pages[i] != null)
            {
                _pages[i].BringToFront();
                _host.PerformLayout();
            }
            for (int k = 0; k < _nav.Length; k++)
                if (_nav[k] != null) _nav[k].Selected = (k == i);

            string id = _ids[i];
            _topTitle.Text = L.T(id + ".title");
            _topSub.Text = L.T(id + ".sub");
            LayoutTop();

            if (id == "diag") ScanDiag();
            if (id == "tw") RefreshRows();
            if (id == "bench") RefreshBench();
            if (id == "bk") RefreshBackup();
            if (id == "rp") RefreshPreview();
        }

        void RebuildUi()
        {
            int keep = _page;
            _host.SuspendLayout();
            while (_host.Controls.Count > 0)
            {
                Control c = _host.Controls[0];
                _host.Controls.RemoveAt(0);
                c.Dispose();
            }
            _rows.Clear();
            _diagRows.Clear();
            _cpuCard = null; _ramCard = null; _gpuCard = null; _diskCard = null; _netCard = null;
            _health = null; _info = null; _restoreInfo = null; _journalCount = null; _preview = null;
            _openReport = null; _diagList = null; _diagSummary = null; _diagScan = null;
            _benchTable = null; _benchRun = null; _benchBaseline = null;
            _twChips = null; _twCount = null;

            _pages = new Panel[7];
            _pages[0] = BuildDashboard();
            _pages[1] = BuildDiag();
            _pages[2] = BuildTweaks();
            _pages[3] = BuildBench();
            _pages[4] = BuildBackup();
            _pages[5] = BuildReport();
            _pages[6] = BuildSettings();
            for (int i = 0; i < _pages.Length; i++)
            {
                _pages[i].Visible = false;
                _host.Controls.Add(_pages[i]);
            }

            for (int i = 0; i < _nav.Length; i++)
                _nav[i].Text = L.T(NavKey(_ids[i]));
            _langBtn.Text = Theme.Arabic ? "English" : "العربية";
            _langBtn.Kind = BtnKind.Ghost;
            if (_ytBtn != null) _ytBtn.Text = L.T("channel.short");

            LayoutTop();
            LayoutSide();
            SetPage(keep);
            if (_last != null) ApplySample(_last);
            _host.ResumeLayout();
        }

        static string NavKey(string id)
        {
            if (id == "dash") return "nav.dashboard";
            if (id == "diag") return "nav.diag";
            if (id == "tw") return "nav.tweaks";
            if (id == "bench") return "nav.bench";
            if (id == "bk") return "nav.backup";
            if (id == "rp") return "nav.report";
            return "nav.settings";
        }

        // ------------------------------------------------------------ pages

        Panel NewPage()
        {
            Panel p = new Panel();
            p.Dock = DockStyle.Fill;
            p.BackColor = Theme.Bg;
            p.Padding = new Padding(22, 14, 22, 16);
            p.Visible = false;
            return p;
        }

        static void Fill(Control c, Padding margin)
        {
            c.Dock = DockStyle.Fill;
            c.Margin = margin;
        }

        Panel BuildDashboard()
        {
            Panel p = NewPage();
            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Fill;
            t.BackColor = Theme.Bg;
            t.ColumnCount = 3;
            t.RowCount = 3;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 190f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 190f));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            _health = new HealthCard();
            _health.Score = _score;
            _health.Applied = _applied;
            _health.Total = TweakCatalog.All.Count;
            Fill(_health, new Padding(0, 0, 14, 14));

            _cpuCard = new StatCard();
            _cpuCard.Title = L.T("dash.cpu");
            _cpuCard.Accent = Theme.Accent;
            _cpuCard.Trend = _cpuTrend;
            Fill(_cpuCard, new Padding(0, 0, 14, 14));

            _ramCard = new StatCard();
            _ramCard.Title = L.T("dash.ram");
            _ramCard.Accent = Theme.Ember;
            _ramCard.Trend = _ramTrend;
            Fill(_ramCard, new Padding(0, 0, 0, 14));

            _gpuCard = new StatCard();
            _gpuCard.Title = L.T("dash.gpu");
            _gpuCard.Accent = Theme.Info;
            _gpuCard.Trend = _gpuTrend;
            Fill(_gpuCard, new Padding(0, 0, 14, 14));

            _diskCard = new StatCard();
            _diskCard.Title = L.T("dash.disk");
            _diskCard.Accent = Theme.Warn;
            Fill(_diskCard, new Padding(0, 0, 14, 14));

            _netCard = new StatCard();
            _netCard.Title = L.T("dash.net") + " · 1.1.1.1";
            _netCard.Accent = Theme.Accent;
            _netCard.Trend = _netTrend;
            Fill(_netCard, new Padding(0, 0, 0, 14));

            _info = new InfoCard();
            _info.Title = L.T("dash.sysinfo");
            Fill(_info, new Padding(0, 0, 0, 0));

            t.Controls.Add(_health, 0, 0);
            t.Controls.Add(_cpuCard, 1, 0);
            t.Controls.Add(_ramCard, 2, 0);
            t.Controls.Add(_gpuCard, 0, 1);
            t.Controls.Add(_diskCard, 1, 1);
            t.Controls.Add(_netCard, 2, 1);
            t.Controls.Add(_info, 0, 2);
            t.SetColumnSpan(_info, 3);

            p.Controls.Add(t);
            RefreshInfo();
            return p;
        }

        Panel BuildDiag()
        {
            Panel p = NewPage();

            Panel head = new Panel();
            head.Dock = DockStyle.Top;
            head.Height = 58;
            head.BackColor = Theme.Bg;

            _diagSummary = new Label();
            _diagSummary.AutoSize = false;
            _diagSummary.AutoEllipsis = true;
            _diagSummary.Font = Theme.FBody;
            _diagSummary.ForeColor = Theme.Muted;
            _diagSummary.BackColor = Theme.Bg;
            _diagSummary.Bounds = new Rectangle(0, 8, 420, 24);
            _diagSummary.TextAlign = ContentAlignment.MiddleLeft;
            _diagSummary.Text = L.T("diag.readonly");
            head.Controls.Add(_diagSummary);

            _diagScan = new KButton(L.T("diag.scan"));
            _diagScan.Kind = BtnKind.Primary;
            _diagScan.Size = new Size(154, 40);
            _diagScan.Click += delegate { ScanDiag(); };
            head.Controls.Add(_diagScan);

            head.Resize += delegate
            {
                int x = head.Width - _diagScan.Width;
                _diagScan.Location = new Point(x, 4);
                _diagSummary.Bounds = new Rectangle(Theme.Arabic ? 12 : 0, 8,
                    Math.Max(140, x - 20), 24);
                _diagSummary.TextAlign = Theme.Arabic ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
            };

            _diagList = new Panel();
            _diagList.Dock = DockStyle.Fill;
            _diagList.AutoScroll = true;
            _diagList.BackColor = Theme.Bg;
            Theme.DarkScroll(_diagList);

            p.Controls.Add(_diagList);
            p.Controls.Add(head);
            return p;
        }

        Panel BuildBench()
        {
            Panel p = NewPage();

            Panel head = new Panel();
            head.Dock = DockStyle.Top;
            head.Height = 58;
            head.BackColor = Theme.Bg;

            Label hint = new Label();
            hint.Text = L.T("bench.sub");
            hint.Font = Theme.FSmall;
            hint.ForeColor = Theme.Muted;
            hint.BackColor = Theme.Bg;
            hint.AutoSize = false;
            hint.Bounds = new Rectangle(0, 10, 300, 20);
            hint.TextAlign = ContentAlignment.MiddleLeft;
            head.Controls.Add(hint);

            _benchRun = new KButton(L.T("bench.run"));
            _benchRun.Kind = BtnKind.Primary;
            _benchRun.Size = new Size(180, 40);
            _benchRun.Click += RunBench;
            head.Controls.Add(_benchRun);

            _benchBaseline = new KButton(L.T("bench.setBase"));
            _benchBaseline.Kind = BtnKind.Ghost;
            _benchBaseline.Size = new Size(200, 40);
            _benchBaseline.Click += SetBenchBaseline;
            head.Controls.Add(_benchBaseline);

            head.Resize += delegate
            {
                int x = head.Width - _benchRun.Width;
                _benchRun.Location = new Point(x, 4);
                _benchBaseline.Location = new Point(x - 12 - _benchBaseline.Width, 4);
                hint.Bounds = new Rectangle(Theme.Arabic ? 12 : 0, 10,
                    Math.Max(120, x - _benchBaseline.Width - 40), 20);
                hint.TextAlign = Theme.Arabic ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
            };

            _benchTable = new BenchTable();
            _benchTable.Dock = DockStyle.Fill;
            _benchTable.Margin = new Padding(0);

            p.Controls.Add(_benchTable);
            p.Controls.Add(head);
            RefreshBench();
            return p;
        }

        Panel BuildTweaks()
        {
            Panel p = NewPage();

            Panel head = new Panel();
            head.Dock = DockStyle.Top;
            head.Height = 96;
            head.BackColor = Theme.Bg;

            Label hint = new Label();
            hint.Text = L.T("tw.recommendedHint");
            hint.Font = Theme.FSmall;
            hint.ForeColor = Theme.Muted;
            hint.BackColor = Theme.Bg;
            hint.AutoSize = false;
            hint.Bounds = new Rectangle(0, 10, 380, 20);
            hint.TextAlign = ContentAlignment.MiddleLeft;
            head.Controls.Add(hint);

            KButton rec = new KButton(L.T("tw.recommended"));
            rec.Kind = BtnKind.Primary;
            rec.Size = new Size(190, 40);
            rec.Click += ApplyRecommended;
            head.Controls.Add(rec);

            KButton undo = new KButton(L.T("bk.undoAll"));
            undo.Kind = BtnKind.Ghost;
            undo.Size = new Size(155, 40);
            undo.Click += UndoAllClick;
            head.Controls.Add(undo);

            string[] chipNames = new string[]
            {
                L.T("tw.filterAll"), L.T("tw.filterSafe"),
                L.T("tw.filterModerate"), L.T("tw.filterAdvanced")
            };
            _twChips = new KButton[4];
            for (int i = 0; i < 4; i++)
            {
                KButton chip = new KButton(chipNames[i]);
                chip.Font = Theme.FSmall;
                chip.Kind = i == 0 ? BtnKind.Primary : BtnKind.Ghost;
                chip.Height = 30;
                chip.Width = Math.Max(76, TextRenderer.MeasureText(chipNames[i], Theme.FSmall).Width + 30);
                int idx = i;
                chip.Click += delegate { ApplyTweakFilter(idx); };
                head.Controls.Add(chip);
                _twChips[i] = chip;
            }

            _twCount = new Label();
            _twCount.AutoSize = false;
            _twCount.Font = Theme.FSmall;
            _twCount.ForeColor = Theme.Muted;
            _twCount.BackColor = Theme.Bg;
            _twCount.Bounds = new Rectangle(0, 54, 300, 26);
            head.Controls.Add(_twCount);

            head.Resize += delegate
            {
                int x = head.Width - rec.Width;
                rec.Location = new Point(x, 2);
                undo.Location = new Point(x - 12 - undo.Width, 2);
                hint.Bounds = new Rectangle(Theme.Arabic ? 12 : 0, 10,
                    Math.Max(120, x - undo.Width - 30), 20);
                hint.TextAlign = Theme.Arabic ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;

                if (Theme.Arabic)
                {
                    int cx = head.Width - 12;
                    for (int i = 0; i < _twChips.Length; i++)
                    {
                        cx -= _twChips[i].Width;
                        _twChips[i].Location = new Point(cx, 54);
                        cx -= 8;
                    }
                }
                else
                {
                    int cx = 0;
                    for (int i = 0; i < _twChips.Length; i++)
                    {
                        _twChips[i].Location = new Point(cx, 54);
                        cx += _twChips[i].Width + 8;
                    }
                }
                _twCount.Bounds = new Rectangle(Theme.Arabic ? 0 : Math.Max(0, head.Width - 300), 54, 300, 26);
                _twCount.TextAlign = Theme.Arabic ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleRight;
            };

            Panel list = new Panel();
            list.Dock = DockStyle.Fill;
            list.AutoScroll = true;
            list.BackColor = Theme.Bg;
            Theme.DarkScroll(list);

            for (int i = TweakCatalog.All.Count - 1; i >= 0; i--)
            {
                Tweak tw = TweakCatalog.All[i];
                TweakRow row = new TweakRow(tw, IsOn(tw));
                row.Dock = DockStyle.Top;
                row.Toggled += RowToggled;
                _tips.SetToolTip(row, tw.Note);
                list.Controls.Add(row);
                _rows.Add(row);
            }

            p.Controls.Add(list);
            p.Controls.Add(head);
            ApplyTweakFilter(0);
            return p;
        }

        void ApplyTweakFilter(int f)
        {
            _twFilter = f;
            for (int i = 0; i < _rows.Count; i++)
            {
                TweakRow r = _rows[i];
                r.Visible = (f == 0 || r.Tweak.Risk == f - 1);
            }
            if (_twChips != null)
            {
                for (int i = 0; i < _twChips.Length; i++)
                {
                    _twChips[i].Kind = (i == f) ? BtnKind.Primary : BtnKind.Ghost;
                    _twChips[i].Invalidate();
                }
            }
            RefreshTweakCount();
        }

        void RefreshTweakCount()
        {
            if (_twCount == null) return;
            _twCount.Text = string.Format(L.T("tw.counter"), _applied, TweakCatalog.All.Count);
            _twCount.ForeColor = _applied == TweakCatalog.All.Count ? Theme.Accent : Theme.Muted;
        }

        Card ActionCard(string title, string desc, string button, EventHandler onClick, int height)
        {
            Card c = new Card();
            c.Title = title;
            c.Desc = desc;
            c.Dock = DockStyle.Fill;
            c.Margin = new Padding(0, 0, 0, 12);
            c.Height = height;

            KButton b = new KButton(button);
            b.Kind = BtnKind.Ghost;
            b.Size = new Size(210, 38);
            b.Location = new Point(20, height - 56);
            b.BackColor = Theme.Card;
            b.Click += onClick;
            c.Controls.Add(b);
            return c;
        }

        Panel BuildBackup()
        {
            Panel p = NewPage();
            Panel scroll = new Panel();
            scroll.Dock = DockStyle.Fill;
            scroll.AutoScroll = true;
            scroll.BackColor = Theme.Bg;
            Theme.DarkScroll(scroll);

            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Top;
            t.AutoSize = true;
            t.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            t.BackColor = Theme.Bg;
            t.ColumnCount = 1;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            Card c1 = ActionCard(L.T("bk.restore"), L.T("bk.restoreDesc"), L.T("bk.create"), CreateRestorePoint, 178);
            _restoreInfo = new Label();
            _restoreInfo.AutoSize = false;
            _restoreInfo.AutoEllipsis = true;
            _restoreInfo.Font = Theme.FSmall;
            _restoreInfo.ForeColor = Theme.Dim;
            _restoreInfo.BackColor = Theme.Card;
            _restoreInfo.Bounds = new Rectangle(242, 120, 300, 36);
            _restoreInfo.TextAlign = ContentAlignment.MiddleLeft;
            _restoreInfo.Text = "-";
            _restoreInfo.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            c1.Controls.Add(_restoreInfo);
            c1.Resize += delegate
            {
                _restoreInfo.Width = Math.Max(60, c1.Width - 262);
            };

            Card c2 = ActionCard(L.T("bk.undo"), L.T("bk.undoDesc"), L.T("bk.undoAll"), UndoAllClick, 178);
            _journalCount = new Label();
            _journalCount.AutoSize = false;
            _journalCount.Font = Theme.FSmall;
            _journalCount.ForeColor = Theme.Dim;
            _journalCount.BackColor = Theme.Card;
            _journalCount.Bounds = new Rectangle(242, 118, 300, 40);
            _journalCount.TextAlign = ContentAlignment.MiddleLeft;
            c2.Controls.Add(_journalCount);

            Card c3 = new Card();
            c3.Title = L.T("bk.folder");
            c3.Desc = Engine.DataDirDisplay;
            c3.Dock = DockStyle.Fill;
            c3.Margin = new Padding(0, 0, 0, 12);
            c3.Height = 140;
            KButton open = new KButton(L.T("bk.open"));
            open.Kind = BtnKind.Ghost;
            open.Size = new Size(170, 38);
            open.Location = new Point(20, 88);
            open.BackColor = Theme.Card;
            open.Click += delegate { Engine.OpenFolder(Engine.DataDir); };
            c3.Controls.Add(open);

            Card c4 = new Card();
            c4.Title = L.T("bk.safety");
            c4.Desc = L.T("bk.safetyDesc");
            c4.Dock = DockStyle.Fill;
            c4.Margin = new Padding(0, 0, 0, 12);
            c4.Height = 220;

            Card c5 = ActionCard(L.T("bk.exportTitle"), L.T("bk.exportDesc"), L.T("bk.export"), ExportUndo, 178);

            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 190f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 190f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 152f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 232f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 190f));
            t.Controls.Add(c1, 0, 0);
            t.Controls.Add(c2, 0, 1);
            t.Controls.Add(c3, 0, 2);
            t.Controls.Add(c4, 0, 3);
            t.Controls.Add(c5, 0, 4);

            scroll.Controls.Add(t);
            p.Controls.Add(scroll);
            RefreshBackup();
            return p;
        }

        Panel BuildReport()
        {
            Panel p = NewPage();
            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Fill;
            t.BackColor = Theme.Bg;
            t.ColumnCount = 1;
            t.RowCount = 2;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 196f));
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Card c = new Card();
            c.Title = L.T("rp.title");
            c.Desc = L.T("rp.desc");
            c.Dock = DockStyle.Fill;
            c.Margin = new Padding(0, 0, 0, 14);

            KButton gen = new KButton(L.T("rp.generate"));
            gen.Kind = BtnKind.Primary;
            gen.Size = new Size(220, 40);
            gen.Location = new Point(20, 138);
            gen.BackColor = Theme.Card;
            gen.Click += GenerateReport;
            c.Controls.Add(gen);

            _openReport = new KButton(L.T("rp.open"));
            _openReport.Kind = BtnKind.Ghost;
            _openReport.Size = new Size(190, 40);
            _openReport.Location = new Point(252, 138);
            _openReport.BackColor = Theme.Card;
            _openReport.Click += OpenLastReport;
            c.Controls.Add(_openReport);

            Card pv = new Card();
            pv.Title = L.T("rp.preview");
            pv.Dock = DockStyle.Fill;
            pv.Margin = new Padding(0, 0, 0, 0);

            Panel host = new Panel();
            host.BackColor = Theme.Card;
            host.Padding = new Padding(12, 8, 12, 8);
            host.Bounds = new Rectangle(20, 48, pv.Width - 40, pv.Height - 64);
            pv.Controls.Add(host);

            _preview = new TextBox();
            _preview.Multiline = true;
            _preview.ReadOnly = true;
            _preview.BorderStyle = BorderStyle.None;
            _preview.BackColor = Theme.Card2;
            _preview.ForeColor = Theme.Text;
            _preview.Font = new Font("Consolas", 9.2f);
            _preview.ScrollBars = ScrollBars.Vertical;
            Theme.DarkScroll(_preview);
            _preview.Dock = DockStyle.Fill;
            host.Controls.Add(_preview);
            pv.Resize += delegate
            {
                host.Bounds = new Rectangle(20, 48, Math.Max(40, pv.Width - 40), Math.Max(10, pv.Height - 66));
            };

            t.Controls.Add(c, 0, 0);
            t.Controls.Add(pv, 0, 1);
            p.Controls.Add(t);
            RefreshPreview();
            return p;
        }

        Panel BuildSettings()
        {
            Panel p = NewPage();
            Panel scroll = new Panel();
            scroll.Dock = DockStyle.Fill;
            scroll.AutoScroll = true;
            scroll.BackColor = Theme.Bg;
            Theme.DarkScroll(scroll);

            TableLayoutPanel t = new TableLayoutPanel();
            t.Dock = DockStyle.Top;
            t.AutoSize = true;
            t.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            t.BackColor = Theme.Bg;
            t.ColumnCount = 1;
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            Card lang = new Card();
            lang.Title = L.T("st.lang");
            lang.Desc = L.T("st.langDesc");
            lang.Dock = DockStyle.Fill;
            lang.Margin = new Padding(0, 0, 0, 12);
            lang.Height = 150;

            KButton arBtn = new KButton("العربية");
            arBtn.Kind = Theme.Arabic ? BtnKind.Primary : BtnKind.Ghost;
            arBtn.Size = new Size(130, 38);
            arBtn.Location = new Point(20, 98);
            arBtn.BackColor = Theme.Card;
            arBtn.Click += delegate { SetLanguage(true); };
            lang.Controls.Add(arBtn);

            KButton enBtn = new KButton("English");
            enBtn.Kind = Theme.Arabic ? BtnKind.Ghost : BtnKind.Primary;
            enBtn.Size = new Size(130, 38);
            enBtn.Location = new Point(160, 98);
            enBtn.BackColor = Theme.Card;
            enBtn.Click += delegate { SetLanguage(false); };
            lang.Controls.Add(enBtn);

            Card rest = new Card();
            rest.Title = L.T("st.restore");
            rest.Desc = L.T("st.restoreDesc");
            rest.Dock = DockStyle.Fill;
            rest.Margin = new Padding(0, 0, 0, 12);
            rest.Height = 150;

            ToggleSwitch tg = new ToggleSwitch();
            tg.BackColor = Theme.Card;
            tg.SetSilent(Engine.RestoreFirst);
            tg.Location = new Point(rest.Width - 70, 96);
            tg.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            tg.Changed += delegate
            {
                Engine.RestoreFirst = tg.On;
                Engine.SaveSettings();
            };
            rest.Controls.Add(tg);

            Card about = new Card();
            about.Title = L.T("st.about");
            about.Desc = L.T("st.aboutDesc") + "\n\n" + L.T("st.github") + ": " + Brand.GithubLabel;
            about.Dock = DockStyle.Fill;
            about.Margin = new Padding(0, 0, 0, 12);
            about.Height = 190;

            KButton data = new KButton(L.T("st.data"));
            data.Kind = BtnKind.Ghost;
            data.Size = new Size(200, 38);
            data.Location = new Point(20, 118);
            data.BackColor = Theme.Card;
            data.Click += delegate { Engine.OpenFolder(Engine.DataDir); };
            about.Controls.Add(data);

            KButton replay = new KButton(L.T("st.replay"));
            replay.Kind = BtnKind.Ghost;
            replay.Size = new Size(200, 38);
            replay.Location = new Point(232, 118);
            replay.BackColor = Theme.Card;
            replay.Click += delegate { ShowIntro(); };
            about.Controls.Add(replay);

            Card channel = new Card();
            channel.Title = L.T("st.channel");
            channel.Desc = L.T("st.channelDesc") + "\n" + Brand.ChannelName + " · " + Brand.ChannelLabel;
            channel.Dock = DockStyle.Fill;
            channel.Margin = new Padding(0, 0, 0, 12);
            channel.Height = 170;

            KButton yt = new KButton(L.T("channel.open"));
            yt.Kind = BtnKind.Ghost;
            yt.Tint = Theme.Ember;
            yt.Size = new Size(200, 38);
            yt.Location = new Point(20, 118);
            yt.BackColor = Theme.Card;
            yt.Click += delegate { Engine.OpenUrl(Brand.ChannelUrl); };
            channel.Controls.Add(yt);

            Card never = new Card();
            never.Title = L.T("st.diagnostics");
            never.Desc = L.T("st.neverDesc");
            never.Dock = DockStyle.Fill;
            never.Margin = new Padding(0, 0, 0, 12);
            never.Height = 196;

            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 174f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 174f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 202f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 182f));
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 220f));
            t.Controls.Add(lang, 0, 0);
            t.Controls.Add(rest, 0, 1);
            t.Controls.Add(about, 0, 2);
            t.Controls.Add(channel, 0, 3);
            t.Controls.Add(never, 0, 4);

            scroll.Controls.Add(t);
            p.Controls.Add(scroll);
            return p;
        }

        // ------------------------------------------------------------ status & language

        void SetStatus(string s)
        {
            if (_statusLabel != null) _statusLabel.Text = "  " + s;
            Engine.Log(s);
        }

        void ToggleLanguage(object sender, EventArgs e)
        {
            SetLanguage(!Theme.Arabic);
        }

        void SetLanguage(bool arabic)
        {
            if (Theme.Arabic == arabic) return;
            Theme.Arabic = arabic;
            Engine.SaveSettings();
            RebuildUi();
            if (_intro != null) _intro.ApplyLang();
        }

        // ------------------------------------------------------------ welcome

        void ShowIntro()
        {
            if (_intro == null)
            {
                _intro = new WelcomeOverlay();
                _intro.Start += delegate { CloseIntro(); };
                _intro.Channel += delegate { Engine.OpenUrl(Brand.ChannelUrl); };
                _intro.Lang += delegate { SetLanguage(!Theme.Arabic); };
                _intro.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                _intro.Bounds = ClientRectangle;
                Controls.Add(_intro);
            }
            // Hide the shell while the welcome page is open: one opaque child
            // means no z-order surprises and no clicks leaking to the app.
            _side.Visible = false;
            _top.Visible = false;
            _status.Visible = false;
            _host.Visible = false;
            _intro.Bounds = ClientRectangle;
            _intro.Visible = true;
            _intro.BringToFront();
            _intro.ApplyLang();
        }

        void CloseIntro()
        {
            if (_intro != null) _intro.Visible = false;
            _side.Visible = true;
            _top.Visible = true;
            _status.Visible = true;
            _host.Visible = true;
            if (_pages != null && _page >= 0 && _page < _pages.Length && _pages[_page] != null)
            {
                _pages[_page].Visible = true;
                _pages[_page].BringToFront();
            }
            Engine.IntroSeen = true;
            Engine.SaveSettings();
            try { _host.Focus(); }
            catch { }
        }

        // ------------------------------------------------------------ live data

        void StartTimers()
        {
            _timer = new Timer();
            _timer.Interval = 1000;
            _timer.Tick += TickSample;
            _timer.Start();

            _pingTimer = new Timer();
            _pingTimer.Interval = 30000;
            _pingTimer.Tick += PingTick;
            _pingTimer.Start();
        }

        bool _shownOnce;

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (_shownOnce) return;
            _shownOnce = true;
            PingTick(null, EventArgs.Empty);
        }

        async void TickSample(object sender, EventArgs e)
        {
            // The pending-reboot pill changes the width reserved in the top bar.
            if (_top != null)
            {
                bool pending = Engine.RebootStillPending();
                if (pending != _rebootShow)
                {
                    _rebootShow = pending;
                    LayoutTop();
                    _top.Invalidate();
                }
            }
            if (_sampling || IsDisposed) return;
            _sampling = true;
            try
            {
                bool withGpu = (_ticks % 3 == 0);
                _ticks++;
                Sample sm = await Task.Run(delegate { return SysInfo.TakeSample(withGpu); });
                if (IsDisposed) return;
                ApplySample(sm);
            }
            catch { }
            finally { _sampling = false; }
        }

        static void Trend(List<float> list, float v)
        {
            list.Add(v);
            while (list.Count > 50) list.RemoveAt(0);
        }

        void ApplySample(Sample sm)
        {
            _last = sm;
            Trend(_cpuTrend, sm.Cpu);
            Trend(_ramTrend, sm.RamPct);
            if (sm.Gpu >= 0) Trend(_gpuTrend, sm.Gpu);

            if (_cpuCard != null)
            {
                _cpuCard.Value = sm.Cpu.ToString("0") + "%";
                _cpuCard.Sub = SysInfo.Cores + " " + L.T("dash.cores") + " · " + SysInfo.Threads + " " + L.T("dash.threads");
                _cpuCard.Pct = sm.Cpu;
                _cpuCard.Invalidate();
            }
            if (_ramCard != null)
            {
                _ramCard.Value = sm.RamUsedGb.ToString("0.0") + " / " + sm.RamTotalGb.ToString("0.0") + " GB";
                double free = sm.RamTotalGb - sm.RamUsedGb;
                if (free < 0) free = 0;
                _ramCard.Sub = free.ToString("0.0") + " GB " + L.T("dash.available");
                _ramCard.Pct = sm.RamPct;
                _ramCard.Invalidate();
            }
            if (_gpuCard != null)
            {
                _gpuCard.Value = sm.Gpu >= 0 ? sm.Gpu.ToString("0") + "%" : L.T("dash.na");
                _gpuCard.Sub = Short(SysInfo.GpuName, 34);
                _gpuCard.Pct = sm.Gpu;
                _gpuCard.Invalidate();
            }
            if (_diskCard != null)
            {
                _diskCard.Value = sm.FreeGb.ToString("0.0") + " GB";
                _diskCard.Sub = L.T("dash.free") + " · " + sm.TotalGb.ToString("0.0") + " GB";
                _diskCard.Pct = sm.TotalGb > 0 ? (float)(sm.FreeGb / sm.TotalGb * 100.0) : -1;
                _diskCard.Invalidate();
            }

            _score = ComputeScore();
            UpdateHealthCard();
            RefreshInfo();
        }

        void UpdateNetCard()
        {
            if (_netCard == null) return;
            PingStat p = _ping;
            if (p == null || !p.Ok)
            {
                _netCard.Value = L.T("dash.na");
                _netCard.Sub = L.T("dash.ping") + " 1.1.1.1";
                _netCard.Pct = -1;
            }
            else
            {
                _netCard.Value = p.Avg + " " + L.T("dash.ms");
                _netCard.Sub = L.T("dash.jitter") + " " + p.Jitter + " " + L.T("dash.ms") +
                                " · " + L.T("dash.loss") + " " + p.Loss + "%";
                _netCard.Pct = Math.Min(100, p.Avg);
            }
            _netCard.Invalidate();
        }

        async void PingTick(object sender, EventArgs e)
        {
            if (_pingBusy || IsDisposed) return;
            _pingBusy = true;
            SetStatus(L.T("status.ping"));
            try
            {
                PingStat p = await Task.Run(delegate { return SysInfo.PingHost("1.1.1.1", 3); });
                if (IsDisposed) return;
                _ping = p;
                if (p.Ok) Trend(_netTrend, p.Avg);
                UpdateNetCard();
                _score = ComputeScore();
                UpdateHealthCard();
                if (_page == 3) RefreshPreview();
                SetStatus(L.T("status.ready"));
            }
            catch { }
            finally { _pingBusy = false; }
        }

        int ComputeScore()
        {
            int s = 100;
            if (_last != null)
            {
                if (_last.RamPct > 88) s -= 22;
                else if (_last.RamPct > 72) s -= 10;
                if (_last.FreeGb < 8) s -= 20;
                else if (_last.FreeGb < 20) s -= 8;
            }
            if (_ping != null && _ping.Ok)
            {
                if (_ping.Avg > 120) s -= 14;
                else if (_ping.Avg > 70) s -= 7;
                if (_ping.Jitter > 30) s -= 10;
                else if (_ping.Jitter > 12) s -= 4;
                if (_ping.Loss > 0) s -= 8;
            }
            if (s < 5) s = 5;
            if (s > 100) s = 100;
            return s;
        }

        void UpdateHealthCard()
        {
            if (_health == null) return;
            _health.Score = _score;
            _health.Applied = _applied;
            _health.Total = TweakCatalog.All.Count;
            _health.Invalidate();
        }

        void RefreshInfo()
        {
            if (_info == null) return;
            _info.Rows.Clear();
            _info.Rows.Add(new string[] { L.T("dash.cpuName"), Short(SysInfo.CpuName, 42) });
            _info.Rows.Add(new string[] { L.T("dash.gpuName"), Short(SysInfo.GpuName, 42) });
            _info.Rows.Add(new string[] { L.T("dash.ramTotal"), SysInfo.RamTotalGb.ToString("0.0") + " GB" });
            _info.Rows.Add(new string[] { L.T("dash.os"), Short(SysInfo.OsName, 42) });
            _info.Rows.Add(new string[] { L.T("dash.machine"), SysInfo.Machine });
            _info.Rows.Add(new string[] { "Motherboard", Short(SysInfo.Board, 42) });
            _info.Rows.Add(new string[] { L.T("dash.uptime"), SysInfo.Uptime() });
            string trim = _trim == 0 ? "TRIM: enabled" : (_trim == 1 ? "TRIM: disabled" : "TRIM: n/a");
            _info.Rows.Add(new string[] { "Storage", trim });
            _info.Invalidate();
        }

        static string Short(string s, int max)
        {
            if (s == null) return "";
            if (s.Length <= max) return s;
            return s.Substring(0, max - 1) + "…";
        }

        // ------------------------------------------------------------ tweak actions

        void RefreshRows()
        {
            for (int i = 0; i < _rows.Count; i++)
                _rows[i].RefreshState(IsOn(_rows[i].Tweak));
            RefreshTweakCount();
        }

        void RowToggled(object sender, EventArgs e)
        {
            TweakRow row = sender as TweakRow;
            if (row == null) return;
            Tweak t = row.Tweak;

            if (_busy)
            {
                row.SetOnSilent(IsOn(t));
                return;
            }

            bool target = row.On;
            if (target && t.Risk == 2)
            {
                string body = L.T("risk.advanced") + "\n\n" + t.Note;
                if (Msg(string.Format(L.T("tw.confirmAdvanced"), t.Name, body), L.T("msg.title"),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    row.SetOnSilent(false);
                    return;
                }
            }

            if (!EnsureSafety())
            {
                row.SetOnSilent(IsOn(t));
                return;
            }

            RunChange(t, target);
            RefreshRows();
        }

        void RunChange(Tweak t, bool on)
        {
            _busy = true;
            Cursor = Cursors.WaitCursor;
            for (int i = 0; i < _rows.Count; i++) _rows[i].SetBusy(true);
            try
            {
                if (on)
                {
                    t.Apply();
                    _state[t.Id] = true;
                    SetStatus(string.Format(L.T("status.applied"), t.Name));
                }
                else
                {
                    int n = Engine.UndoByTweak(t.Id);
                    if (n == 0 && t.Defaults != null) t.Defaults();
                    _state[t.Id] = t.On;
                    SetStatus(string.Format(L.T("status.reverted"), t.Name));
                }
                _applied = CountStates();
                UpdateHealthCard();
                if (t.Reboot)
                {
                    if (on) Engine.SetPendingReboot();
                    _rebootShow = Engine.RebootStillPending();
                    if (_top != null) _top.Invalidate();
                    SetStatus(L.T("msg.needReboot"));
                }
                RefreshTweakCount();
            }
            catch (Exception ex)
            {
                Msg(ex.Message, L.T("msg.fail"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                _state[t.Id] = t.On;
            }
            finally
            {
                _busy = false;
                Cursor = Cursors.Default;
                for (int i = 0; i < _rows.Count; i++) _rows[i].SetBusy(false);
                _applied = CountStates();
                UpdateHealthCard();
            }
        }

        void ApplyRecommended(object sender, EventArgs e)
        {
            if (_busy) return;
            List<Tweak> todo = new List<Tweak>();
            for (int i = 0; i < TweakCatalog.All.Count; i++)
            {
                Tweak t = TweakCatalog.All[i];
                if (t.Risk <= 1 && !IsOn(t)) todo.Add(t);
            }
            if (todo.Count == 0)
            {
                SetStatus(L.T("tw.noneSelected"));
                return;
            }
            if (Msg(string.Format(L.T("tw.confirmProfile"), todo.Count), L.T("msg.title"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            if (!EnsureSafety()) return;

            _busy = true;
            Cursor = Cursors.WaitCursor;
            for (int i = 0; i < _rows.Count; i++) _rows[i].SetBusy(true);
            int ok = 0;
            int fail = 0;
            bool reboot = false;
            try
            {
                for (int i = 0; i < todo.Count; i++)
                {
                    Tweak t = todo[i];
                    try
                    {
                        SetStatus(string.Format(L.T("status.applied"), t.Name));
                        Application.DoEvents();
                        t.Apply();
                        _state[t.Id] = true;
                        ok++;
                        if (t.Reboot) reboot = true;
                    }
                    catch (Exception ex)
                    {
                        fail++;
                        Engine.Log("apply " + t.Id + ": " + ex.Message);
                    }
                }
            }
            finally
            {
                _busy = false;
                Cursor = Cursors.Default;
                for (int i = 0; i < _rows.Count; i++) _rows[i].SetBusy(false);
                _applied = CountStates();
                UpdateHealthCard();
                RefreshRows();
            }

            string msg = L.T("msg.done") + ": " + ok;
            if (fail > 0) msg += "   " + L.T("msg.fail") + ": " + fail;
            if (reboot) msg += "\n\n" + L.T("msg.needReboot");
            if (reboot) Engine.SetPendingReboot();
            _rebootShow = Engine.RebootStillPending();
            if (_top != null) _top.Invalidate();
            Msg(msg, L.T("msg.title"), MessageBoxButtons.OK,
                fail > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            SetStatus(L.T("status.ready"));
        }

        void UndoAllClick(object sender, EventArgs e)
        {
            if (_busy) return;
            if (Engine.Ops.Count == 0)
            {
                Msg(L.T("bk.none"), L.T("msg.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (Msg(L.T("bk.confirmUndo"), L.T("msg.title"), MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes) return;

            _busy = true;
            Cursor = Cursors.WaitCursor;
            try
            {
                int n = Engine.UndoAll();
                ScanStates();
                Engine.ClearPendingReboot();
                _rebootShow = false;
                if (_top != null) _top.Invalidate();
                UpdateHealthCard();
                RefreshRows();
                RefreshBackup();
                Msg(L.T("msg.undoDone") + " (" + n + ")", L.T("msg.title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetStatus(L.T("msg.undoDone"));
            }
            finally
            {
                Cursor = Cursors.Default;
                _busy = false;
            }
        }

        bool EnsureSafety()
        {
            if (!Engine.RestoreFirst || _safetyDone) return true;
            SetStatus(L.T("status.restore"));
            Cursor = Cursors.WaitCursor;
            Application.DoEvents();
            string msg;
            bool ok = Engine.CreateRestorePoint(out msg);
            Cursor = Cursors.Default;
            _safetyDone = true;
            if (!ok)
            {
                string show = msg == null ? "" : msg;
                if (show.Length > 220) show = show.Substring(0, 220);
                if (Msg(string.Format(L.T("msg.safetyFail"), show), L.T("msg.title"),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return false;
            }
            else
            {
                SetStatus(L.T("status.restoreOk"));
            }
            return true;
        }

        async void CreateRestorePoint(object sender, EventArgs e)
        {
            KButton b = sender as KButton;
            if (b != null) b.Enabled = false;
            SetStatus(L.T("status.restore"));
            _restoreMsg = "";
            bool ok = false;
            try
            {
                ok = await Task.Run(delegate
                {
                    string m;
                    bool r = Engine.CreateRestorePoint(out m);
                    _restoreMsg = m;
                    return r;
                });
            }
            catch (Exception ex)
            {
                _restoreMsg = ex.Message;
            }
            if (IsDisposed) return;
            if (b != null) b.Enabled = true;
            _safetyDone = ok;
            string show = ok ? L.T("status.restoreOk") : _restoreMsg;
            if (string.IsNullOrEmpty(show)) show = ok ? L.T("status.restoreOk") : L.T("msg.fail");
            if (_restoreInfo != null) _restoreInfo.Text = show;
            SetStatus(show);
            withMsg(show, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        void withMsg(string text, MessageBoxIcon icon)
        {
            string show = text == null ? "" : text;
            if (show.Length > 400) show = show.Substring(0, 400) + "...";
            Msg(show, L.T("msg.title"), MessageBoxButtons.OK, icon);
        }

        void RefreshBackup()
        {
            if (_journalCount != null)
                _journalCount.Text = Engine.Ops.Count + " " + L.T("bk.entries");
        }

        void ExportUndo(object sender, EventArgs e)
        {
            if (_busy) return;
            if (Engine.Ops.Count == 0)
            {
                Msg(L.T("bk.none"), L.T("msg.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                SetStatus(L.T("status.export"));
                string p = Engine.ExportUndoScript();
                SetStatus(string.Format(L.T("bk.exported"), p));
                Msg(p, L.T("msg.done"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Msg(ex.Message, L.T("msg.fail"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ------------------------------------------------------------ diagnostics

        async void ScanDiag()
        {
            if (_diagBusy || IsDisposed) return;
            _diagBusy = true;
            SetStatus(L.T("status.diag"));
            if (_diagScan != null) _diagScan.Enabled = false;
            for (int i = 0; i < _diagRows.Count; i++) _diagRows[i].SetBusy(true);
            if (_diagSummary != null)
            {
                _diagSummary.Text = L.T("diag.scanning");
                _diagSummary.ForeColor = Theme.Accent;
            }
            try
            {
                List<DiagItem> items = await Task.Run(delegate { return Diag.Scan(); });
                if (IsDisposed) return;
                PopulateDiag(items);
            }
            catch (Exception ex)
            {
                Engine.Log("diag: " + ex.Message);
            }
            finally
            {
                _diagBusy = false;
                if (!IsDisposed)
                {
                    if (_diagScan != null) _diagScan.Enabled = true;
                    SetStatus(L.T("status.ready"));
                }
            }
        }

        void PopulateDiag(List<DiagItem> items)
        {
            if (_diagList == null) return;
            _diagList.SuspendLayout();
            while (_diagList.Controls.Count > 0)
            {
                Control c = _diagList.Controls[0];
                _diagList.Controls.RemoveAt(0);
                c.Dispose();
            }
            _diagRows.Clear();

            if (items == null)
            {
                if (_diagSummary != null)
                {
                    _diagSummary.Text = L.T("diag.readonly");
                    _diagSummary.ForeColor = Theme.Muted;
                }
                _diagList.ResumeLayout();
                return;
            }

            int issues = Diag.IssueCount(items);
            _diagIssues = issues;
            bool problem = false;
            for (int i = 0; i < items.Count; i++) if (items[i].Level == 2) problem = true;

            if (_diagSummary != null)
            {
                _diagSummary.Text = issues == 0
                    ? string.Format(L.T("diag.allGood"), items.Count)
                    : string.Format(L.T("diag.summary"), issues, items.Count);
                _diagSummary.ForeColor = issues == 0 ? Theme.Accent : (problem ? Theme.Danger : Theme.Warn);
            }

            // Problems first, then attention, then healthy checks.
            List<DiagItem> sorted = new List<DiagItem>(items);
            for (int i = 1; i < sorted.Count; i++)
            {
                DiagItem key = sorted[i];
                int j = i - 1;
                while (j >= 0 && sorted[j].Level < key.Level) { sorted[j + 1] = sorted[j]; j--; }
                sorted[j + 1] = key;
            }

            for (int i = sorted.Count - 1; i >= 0; i--)
            {
                DiagRow row = new DiagRow(sorted[i]);
                row.Dock = DockStyle.Top;
                row.Fix += DiagFix;
                _diagList.Controls.Add(row);
                _diagRows.Add(row);
            }
            _diagList.ResumeLayout();
        }

        void DiagFix(object sender, EventArgs e)
        {
            DiagRow row = sender as DiagRow;
            if (row == null || row.Item == null || _busy || _diagBusy) return;
            Tweak t = TweakCatalog.ById(row.Item.FixId);
            if (t == null) return;

            if (t.Risk == 2)
            {
                string body = L.T("risk.advanced") + "\n\n" + t.Note;
                if (Msg(string.Format(L.T("tw.confirmAdvanced"), t.Name, body), L.T("msg.title"),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;
            }
            if (!EnsureSafety()) return;

            SetStatus(string.Format(L.T("diag.fixed"), t.Name));
            RunChange(t, true);
            RefreshRows();
            if (_page == 1) ScanDiag();
        }

        // ------------------------------------------------------------ benchmark

        void RefreshBench()
        {
            if (_benchTable == null) return;
            if (_benchBefore == null) _benchBefore = Bench.Load(Bench.BaselinePath);
            if (_benchNow == null) _benchNow = Bench.Load(Bench.LastPath);
            _benchTable.Busy = false;
            _benchTable.SetData(_benchBefore, _benchNow);
        }

        async void RunBench(object sender, EventArgs e)
        {
            if (_benchBusy || IsDisposed) return;
            if (_benchRun != null) _benchRun.Enabled = false;
            if (_benchBaseline != null) _benchBaseline.Enabled = false;

            BenchResult before = _benchBefore != null ? _benchBefore : Bench.Load(Bench.BaselinePath);
            bool hasBaseline = before != null;
            _benchNow = null;
            if (_benchTable != null)
            {
                _benchTable.Busy = true;
                _benchTable.SetData(_benchBefore, null);
            }
            SetStatus(L.T("status.bench"));

            try
            {
                BenchResult r = await Task.Run(delegate { return Bench.Run(true); });
                if (IsDisposed) return;

                _benchNow = r;
                Bench.Save(r, Bench.LastPath);
                if (!hasBaseline)
                {
                    Bench.Save(r, Bench.BaselinePath);
                    before = r;
                }
                _benchBefore = Bench.Load(Bench.BaselinePath);
                if (_benchBefore == null) _benchBefore = before;

                if (_benchTable != null)
                {
                    _benchTable.Busy = false;
                    _benchTable.SetData(_benchBefore, _benchNow);
                }
                SetStatus(hasBaseline ? string.Format(L.T("bench.done"), Bench.LastPath)
                                      : L.T("bench.firstRun"));
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                {
                    if (_benchTable != null)
                    {
                        _benchTable.Busy = false;
                        _benchTable.SetData(_benchBefore, _benchNow);
                    }
                    Msg(ex.Message, L.T("msg.fail"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                _benchBusy = false;
                if (!IsDisposed)
                {
                    if (_benchRun != null) _benchRun.Enabled = true;
                    if (_benchBaseline != null) _benchBaseline.Enabled = true;
                }
            }
        }

        void SetBenchBaseline(object sender, EventArgs e)
        {
            if (_benchBusy) return;
            BenchResult now = _benchNow;
            if (now == null) now = Bench.Load(Bench.LastPath);
            if (now == null)
            {
                Msg(L.T("bench.noData"), L.T("msg.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Bench.Save(now, Bench.BaselinePath);
            _benchBefore = now;
            _benchNow = now;
            if (_benchTable != null) _benchTable.SetData(_benchBefore, _benchNow);
            SetStatus(L.T("bench.baselineSaved"));
        }

        // ------------------------------------------------------------ report

        void GenerateReport(object sender, EventArgs e)
        {
            Sample sm = _last;
            if (sm == null) sm = SysInfo.TakeSample(true);
            try
            {
                string path = Report.Generate(sm, _ping, _score);
                SetStatus(string.Format(L.T("status.report"), path));
                if (_openReport != null) _openReport.Enabled = true;
                RefreshPreview();
                Report.Open(path);
            }
            catch (Exception ex)
            {
                Msg(ex.Message, L.T("msg.fail"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void OpenLastReport(object sender, EventArgs e)
        {
            string p = Report.LastPath;
            if (string.IsNullOrEmpty(p) || !System.IO.File.Exists(p)) p = Report.Latest();
            if (string.IsNullOrEmpty(p))
            {
                Msg(L.T("rp.none"), L.T("msg.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Report.Open(p);
        }

        void RefreshPreview()
        {
            if (_preview == null) return;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("KiraDemorTY Optimizer  " + Brand.VersionTag);
            sb.AppendLine("------------------------------------------");
            sb.AppendLine("Machine      : " + SysInfo.Machine);
            sb.AppendLine("CPU          : " + SysInfo.CpuName);
            sb.AppendLine("GPU          : " + SysInfo.GpuName);
            sb.AppendLine("RAM          : " + SysInfo.RamTotalGb.ToString("0.0") + " GB");
            sb.AppendLine("Windows      : " + SysInfo.OsName);
            sb.AppendLine("Uptime       : " + SysInfo.Uptime());
            sb.AppendLine();
            sb.AppendLine("Health       : " + _score + " / 100");
            sb.AppendLine("Optimizations: " + _applied + " / " + TweakCatalog.All.Count);
            if (Engine.RebootStillPending()) sb.AppendLine("Restart      : required to finish");
            if (_diagIssues >= 0)
                sb.AppendLine("Diagnostics  : " + _diagIssues + " issue(s) found");
            if (_last != null)
            {
                sb.AppendLine("CPU load     : " + _last.Cpu.ToString("0") + "%");
                sb.AppendLine("RAM used     : " + _last.RamUsedGb.ToString("0.0") + " / " + _last.RamTotalGb.ToString("0.0") + " GB");
                sb.AppendLine("Disk free    : " + _last.FreeGb.ToString("0.0") + " GB");
            }
            if (_ping != null && _ping.Ok)
                sb.AppendLine("Ping         : " + _ping.Avg + " ms   jitter " + _ping.Jitter + " ms   loss " + _ping.Loss + "%");
            else
                sb.AppendLine("Ping         : n/a");
            sb.AppendLine("TRIM         : " + (_trim == 0 ? "enabled" : (_trim == 1 ? "disabled" : "n/a")));
            sb.AppendLine("VBS / HVCI   : " + (TweakCatalog.HypervisorOff() ? "off" : "on"));
            sb.AppendLine();
            string last = Report.LastPath;
            if (string.IsNullOrEmpty(last)) last = Report.Latest();
            sb.AppendLine("Last report  : " + (string.IsNullOrEmpty(last) ? L.T("rp.none") : last));
            sb.AppendLine("Channel      : " + Brand.ChannelUrl);
            _preview.Text = sb.ToString();
            _preview.SelectionStart = 0;
            _preview.SelectionLength = 0;
        }

        // ------------------------------------------------------------ helpers

        DialogResult Msg(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            MessageBoxOptions opt = Theme.Arabic
                ? (MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign)
                : 0;
            return MessageBox.Show(this, text, caption, buttons, icon, MessageBoxDefaultButton.Button1, opt);
        }

        // Ctrl+1..7 jump straight to a page; F1 reopens the welcome page.
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F1)
            {
                ShowIntro();
                return true;
            }
            if (keyData == Keys.Escape && _intro != null && _intro.Visible)
            {
                CloseIntro();
                return true;
            }
            if ((keyData & Keys.Control) == Keys.Control)
            {
                Keys k = keyData & Keys.KeyCode;
                if (k >= Keys.D1 && k <= Keys.D7)
                {
                    SetPage((int)k - (int)Keys.D1);
                    return true;
                }
                if (k >= Keys.NumPad1 && k <= Keys.NumPad7)
                {
                    SetPage((int)k - (int)Keys.NumPad1);
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try { _tips.Dispose(); }
            catch { }
            Engine.SaveSettings();
            base.OnFormClosed(e);
        }
    }
}
