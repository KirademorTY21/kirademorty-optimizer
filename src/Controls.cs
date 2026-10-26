using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KDT
{
    internal enum BtnKind
    {
        Primary,
        Ghost,
        Danger,
        Subtle
    }

    internal class KButton : Control
    {
        public BtnKind Kind = BtnKind.Ghost;
        public Color Tint = Color.Empty;
        bool _hover;
        bool _down;

        public KButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Height = 38;
            Font = Theme.FBold;
            Cursor = Cursors.Hand;
            BackColor = Theme.Bg;
        }

        public KButton(string text)
            : this()
        {
            Text = text;
        }

        public void Fit()
        {
            int w = TextRenderer.MeasureText(Text, Font).Width + 34;
            Width = Math.Max(w, 90);
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color fill = Color.Empty;
            Color border = Color.Empty;
            Color text = Theme.Text;
            Color tint = Kind == BtnKind.Primary ? Theme.Accent : (Kind == BtnKind.Danger ? Theme.Danger : Theme.Text);
            if (Tint != Color.Empty) tint = Tint;

            if (Kind == BtnKind.Primary)
            {
                fill = _down ? Theme.AccentDeep : (_hover ? Theme.Mix(Theme.Accent, Color.White, 0.12) : tint);
                text = Theme.OnAccent;
            }
            else if (Kind == BtnKind.Ghost)
            {
                fill = _down ? Theme.A(tint, 26) : (_hover ? Theme.A(tint, 18) : Theme.A(tint, 8));
                border = _hover ? Theme.A(tint, 110) : Theme.Line;
                text = tint;
            }
            else if (Kind == BtnKind.Danger)
            {
                fill = _down ? Theme.A(tint, 34) : (_hover ? Theme.A(tint, 26) : Theme.A(tint, 16));
                border = Theme.A(tint, 90);
                text = tint;
            }
            else
            {
                if (_hover) fill = Theme.A(Theme.Text, 10);
                text = Theme.Muted;
            }

            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            if (fill != Color.Empty) Theme.Fill(g, r, 9, fill);
            if (border != Color.Empty) Theme.Stroke(g, r, 9, border);

            if (!Enabled) text = Theme.Dim;
            Theme.DrawC(g, Text, Font, text, new Rectangle(0, 0, Width, Height - 1));
        }
    }

    internal class ToggleSwitch : Control
    {
        bool _on;
        bool _hover;
        float _anim;
        Timer _timer;

        public event EventHandler Changed;

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(46, 24);
            Cursor = Cursors.Hand;
            BackColor = Theme.Card;
            _timer = new Timer();
            _timer.Interval = 15;
            _timer.Tick += delegate
            {
                float target = _on ? 1f : 0f;
                if (Math.Abs(_anim - target) < 0.06f)
                {
                    _anim = target;
                    _timer.Stop();
                }
                else
                {
                    _anim += (_on ? 0.18f : -0.18f);
                }
                Invalidate();
            };
        }

        public bool On
        {
            get { return _on; }
            set
            {
                if (_on == value) return;
                _on = value;
                _anim = _on ? 1f : 0f;
                _timer.Start();
                Invalidate();
                if (Changed != null) Changed(this, EventArgs.Empty);
            }
        }

        public void SetSilent(bool value)
        {
            _on = value;
            _anim = value ? 1f : 0f;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            On = !_on;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Theme.Card);

            Color track = Theme.Mix(Theme.Line, Theme.Accent, _anim);
            if (_hover) track = Theme.Mix(track, Color.White, 0.06);
            Rectangle r = new Rectangle(0, 2, Width - 1, Height - 5);
            Theme.Fill(g, r, r.Height / 2, track);

            int knob = Height - 8;
            int x = (int)(2 + (Width - knob - 4) * _anim);
            using (SolidBrush b = new SolidBrush(_on ? Theme.OnAccent : Color.FromArgb(200, 208, 220)))
            {
                g.FillEllipse(b, x, 4, knob, knob);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _timer != null) _timer.Dispose();
            base.Dispose(disposing);
        }
    }

    internal class NavItem : Control
    {
        public int Icon = 0;
        bool _sel;
        bool _hover;

        public NavItem()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Height = 46;
            Font = Theme.FBody;
            Cursor = Cursors.Hand;
            BackColor = Theme.Side;
        }

        public bool Selected
        {
            get { return _sel; }
            set { _sel = value; Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Theme.Side);

            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            if (_sel) Theme.Fill(g, r, 10, Theme.A(Theme.Accent, 20));
            else if (_hover) Theme.Fill(g, r, 10, Theme.A(Theme.Text, 8));

            Color fg = _sel ? Theme.Accent : Theme.Muted;
            Color ic = _sel ? Theme.Accent : Theme.Dim;

            int barX = Theme.Arabic ? Width - 3 : 0;
            if (_sel)
            {
                Rectangle bar = new Rectangle(barX, (Height - 20) / 2, 3, 20);
                Theme.Fill(g, bar, 2, Theme.Accent);
            }

            int ix = Theme.Arabic ? Width - 34 : 16;
            DrawIcon(g, new Rectangle(ix, (Height - 18) / 2, 18, 18), ic);

            Rectangle tr = Theme.Arabic
                ? new Rectangle(12, 0, Width - 50, Height)
                : new Rectangle(46, 0, Width - 58, Height);
            Theme.Draw(g, Text, _sel ? Theme.FBold : Theme.FBody, fg, tr,
                TextFormatFlags.VerticalCenter | Theme.FL(Theme.Arabic));
        }

        void DrawIcon(Graphics g, Rectangle r, Color c)
        {
            using (Pen p = new Pen(c, 1.7f))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                p.LineJoin = LineJoin.Round;
                if (Icon == 0)
                {
                    int s = 6;
                    g.DrawRectangle(p, r.X, r.Y, s, s);
                    g.DrawRectangle(p, r.X + 9, r.Y, s, s);
                    g.DrawRectangle(p, r.X, r.Y + 9, s, s);
                    g.DrawRectangle(p, r.X + 9, r.Y + 9, s, s);
                }
                else if (Icon == 1)
                {
                    g.DrawLine(p, r.X, r.Y + 4, r.Right, r.Y + 4);
                    g.DrawLine(p, r.X, r.Y + 13, r.Right, r.Y + 13);
                    using (SolidBrush b = new SolidBrush(c))
                    {
                        g.FillEllipse(b, r.X + 3, r.Y + 1, 6, 6);
                        g.FillEllipse(b, r.X + 9, r.Y + 10, 6, 6);
                    }
                }
                else if (Icon == 2)
                {
                    g.DrawArc(p, r.X + 1, r.Y + 1, 15, 15, 40, 280);
                    g.DrawLine(p, r.X + 2, r.Y + 2, r.X + 7, r.Y + 3);
                    g.DrawLine(p, r.X + 2, r.Y + 2, r.X + 3, r.Y + 8);
                }
                else if (Icon == 3)
                {
                    g.DrawRectangle(p, r.X + 2, r.Y, 13, 17);
                    g.DrawLine(p, r.X + 6, r.Y + 5, r.Right - 3, r.Y + 5);
                    g.DrawLine(p, r.X + 6, r.Y + 9, r.Right - 3, r.Y + 9);
                    g.DrawLine(p, r.X + 6, r.Y + 13, r.X + 11, r.Y + 13);
                }
                else if (Icon == 4)
                {
                    g.DrawEllipse(p, r.X + 5, r.Y + 5, 8, 8);
                    g.DrawEllipse(p, r.X + 2, r.Y + 2, 14, 14);
                    g.DrawLine(p, r.X + 9, r.Y - 1, r.X + 9, r.Y + 2);
                    g.DrawLine(p, r.X + 9, r.Bottom - 2, r.X + 9, r.Bottom + 1);
                    g.DrawLine(p, r.X - 1, r.Y + 9, r.X + 2, r.Y + 9);
                    g.DrawLine(p, r.Right - 2, r.Y + 9, r.Right + 1, r.Y + 9);
                }
                else if (Icon == 5)
                {
                    // heartbeat / diagnostics
                    g.DrawLine(p, r.X, r.Y + 10, r.X + 4, r.Y + 10);
                    g.DrawLine(p, r.X + 4, r.Y + 10, r.X + 6, r.Y + 3);
                    g.DrawLine(p, r.X + 6, r.Y + 3, r.X + 9, r.Y + 15);
                    g.DrawLine(p, r.X + 9, r.Y + 15, r.X + 11, r.Y + 10);
                    g.DrawLine(p, r.X + 11, r.Y + 10, r.Right, r.Y + 10);
                }
                else
                {
                    // stopwatch / measurement
                    g.DrawEllipse(p, r.X + 2, r.Y + 3, 14, 14);
                    g.DrawLine(p, r.X + 9, r.Y + 10, r.X + 9, r.Y + 6);
                    g.DrawLine(p, r.X + 9, r.Y + 10, r.X + 13, r.Y + 12);
                    g.DrawLine(p, r.X + 7, r.Y, r.X + 11, r.Y);
                }
            }
        }
    }

    internal class Card : Panel
    {
        public string Title = "";
        public string Desc = "";

        public Card()
        {
            BackColor = Theme.Card;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            Padding = new Padding(20, 16, 20, 16);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Theme.SetRegion(this, 12);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            Theme.Stroke(g, new Rectangle(0, 0, Width - 1, Height - 1), 12, Theme.Line);

            bool rtl = Theme.Arabic;
            int pad = Padding.Left;
            if (Title.Length > 0)
            {
                Theme.Draw(g, Title, Theme.FH2, Theme.Text,
                    new Rectangle(pad, 15, Width - pad * 2, 24), Theme.FL(rtl));
            }
            if (Desc.Length > 0)
            {
                Theme.Draw(g, Desc, Theme.FSmall, Theme.Muted,
                    new Rectangle(pad, 43, Width - pad * 2, Math.Min(Height - 50, 74)), Theme.FL(rtl, true));
            }
        }
    }

    internal class StatCard : Control
    {
        public string Title = "";
        public string Value = "--";
        public string Sub = "";
        public Color Accent = Theme.Accent;
        public float Pct = -1;
        public List<float> Trend;

        public StatCard()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Card;
            Font = Theme.FBody;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Theme.SetRegion(this, 12);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            Theme.Stroke(g, new Rectangle(0, 0, Width - 1, Height - 1), 12, Theme.Line);

            bool rtl = Theme.Arabic;
            int pad = 18;
            Rectangle tr = new Rectangle(pad, 14, Width - pad * 2 - 12, 18);
            Theme.Draw(g, Title, Theme.FSmall, Theme.Muted, tr, Theme.FL(rtl));

            using (SolidBrush b = new SolidBrush(Accent))
            {
                g.FillEllipse(b, Width - pad - 6, 20, 6, 6);
            }

            Theme.Draw(g, Value, Theme.FValue, Theme.Text,
                new Rectangle(pad, 36, Width - pad * 2, 46),
                rtl ? (TextFormatFlags.Right | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix) :
                      (TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix));

            Theme.Draw(g, Sub, Theme.FSmall, Theme.Dim,
                new Rectangle(pad, 86, Width - pad * 2, 20), Theme.FL(rtl));

            int barY = Height - 24;
            if (Trend != null && Trend.Count > 1)
            {
                int h = barY - 112;
                if (h > 14) DrawTrend(g, new Rectangle(pad, 112, Width - pad * 2, h), barY);
            }

            if (Pct >= 0)
            {
                Rectangle track = new Rectangle(pad, barY, Width - pad * 2, 5);
                Theme.Fill(g, track, 3, Theme.A(Theme.Text, 14));
                int w = (int)(track.Width * Math.Max(0, Math.Min(100, Pct)) / 100f);
                if (w > 2) Theme.Fill(g, new Rectangle(track.X, track.Y, w, 5), 3, Accent);
            }
        }

        void DrawTrend(Graphics g, Rectangle r, int barY)
        {
            float max = 1;
            for (int i = 0; i < Trend.Count; i++) if (Trend[i] > max) max = Trend[i];
            max = Math.Max(max, 10);
            PointF[] pts = new PointF[Trend.Count];
            for (int i = 0; i < Trend.Count; i++)
            {
                float x = r.X + (float)i / (Trend.Count - 1) * r.Width;
                float y = r.Bottom - (Trend[i] / max) * (r.Height - 2);
                pts[i] = new PointF(x, y);
            }
            using (GraphicsPath fill = new GraphicsPath())
            {
                fill.AddLines(pts);
                fill.AddLine(r.Right, r.Bottom, r.X, r.Bottom);
                fill.CloseFigure();
                using (LinearGradientBrush b = new LinearGradientBrush(
                    new Rectangle(r.X, r.Y, r.Width, r.Height + 4), Theme.A(Accent, 46), Theme.A(Accent, 2), 90f))
                {
                    g.FillPath(b, fill);
                }
            }
            using (Pen p = new Pen(Accent, 1.8f))
            {
                p.LineJoin = LineJoin.Round;
                g.DrawLines(p, pts);
            }
        }
    }

    internal class HealthCard : Control
    {
        public int Score = 0;
        public int Applied = 0;
        public int Total = 0;

        public HealthCard()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Card;
            Font = Theme.FBody;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Theme.SetRegion(this, 12);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            Theme.Stroke(g, new Rectangle(0, 0, Width - 1, Height - 1), 12, Theme.Line);

            bool rtl = Theme.Arabic;
            int d = Math.Min(124, Height - 44);
            if (d < 24) d = 24;
            int cx = 22;
            Rectangle ring = new Rectangle(cx, (Height - d) / 2, d, d);

            Color col = Score >= 75 ? Theme.Accent : (Score >= 50 ? Theme.Warn : Theme.Danger);
            using (Pen bg = new Pen(Theme.A(Theme.Text, 16), 9f))
            {
                g.DrawArc(bg, ring, 0, 359.9f);
            }
            using (Pen fg = new Pen(col, 9f))
            {
                fg.StartCap = LineCap.Round;
                fg.EndCap = LineCap.Round;
                float sweep = 359.9f * Math.Max(0, Math.Min(100, Score)) / 100f;
                if (sweep > 0.5f) g.DrawArc(fg, ring, -90f, sweep);
            }
            Theme.DrawC(g, Score.ToString(), Theme.FValue, Theme.Text,
                new Rectangle(ring.X, ring.Y + 24, ring.Width, 52));
            Theme.DrawC(g, "/ 100", Theme.FTiny, Theme.Dim,
                new Rectangle(ring.X, ring.Y + 72, ring.Width, 18));

            int tx = cx + d + 16;
            int tw = Width - tx - 20;
            Rectangle titleR = new Rectangle(tx, 18, tw, 22);
            Theme.Draw(g, L.T("dash.health"), Theme.FH2, Theme.Text, titleR, Theme.FL(rtl));

            Theme.Draw(g, L.T("dash.healthNote"), Theme.FSmall, Theme.Muted,
                new Rectangle(tx, 44, tw, 48), Theme.FL(rtl, true));

            string ap = L.T("dash.applied") + "  " + Applied + " / " + Total;
            Theme.Draw(g, ap, Theme.FSmall, Theme.Dim,
                new Rectangle(tx, Height - 48, tw, 18), Theme.FL(rtl));

            Rectangle bar = new Rectangle(tx, Height - 26, tw, 5);
            Theme.Fill(g, bar, 3, Theme.A(Theme.Text, 14));
            if (Total > 0)
            {
                int w = (int)(bar.Width * (float)Applied / Total);
                if (w > 2) Theme.Fill(g, new Rectangle(bar.X, bar.Y, w, 5), 3, Theme.Accent);
            }
        }
    }

    internal class InfoCard : Control
    {
        public string Title = "";
        public List<string[]> Rows = new List<string[]>();

        public InfoCard()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Card;
            Font = Theme.FBody;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Theme.SetRegion(this, 12);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            Theme.Stroke(g, new Rectangle(0, 0, Width - 1, Height - 1), 12, Theme.Line);

            bool rtl = Theme.Arabic;
            Theme.Draw(g, Title, Theme.FH2, Theme.Text, new Rectangle(18, 14, Width - 36, 22), Theme.FL(rtl));

            int cols = 2;
            int colW = (Width - 36) / cols;
            int startY = 48;
            int rowH = 38;
            for (int i = 0; i < Rows.Count && i < 8; i++)
            {
                int c = i % cols;
                int r = i / cols;
                int x = 18 + c * colW;
                if (rtl) x = Width - 18 - colW * (c + 1) + 12;
                int y = startY + r * rowH;
                Theme.Draw(g, Rows[i][0], Theme.FTiny, Theme.Dim,
                    new Rectangle(x, y, colW - 24, 16), Theme.FL(rtl));
                Theme.Draw(g, Rows[i][1], Theme.FBody, Theme.Text,
                    new Rectangle(x, y + 15, colW - 24, 20), Theme.FL(rtl));
            }
        }
    }

    internal class TweakRow : Control
    {
        public Tweak Tweak;
        ToggleSwitch _toggle;
        bool _hover;

        public event EventHandler Toggled;

        public TweakRow(Tweak t, bool on)
        {
            Tweak = t;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
            Height = 108;
            Font = Theme.FBody;

            _toggle = new ToggleSwitch();
            _toggle.BackColor = Theme.Card;
            _toggle.SetSilent(on);
            _toggle.Changed += delegate
            {
                Invalidate();
                if (Toggled != null) Toggled(this, EventArgs.Empty);
            };
            Controls.Add(_toggle);
        }

        public bool On
        {
            get { return _toggle.On; }
        }

        public void RefreshState(bool on)
        {
            _toggle.SetSilent(on);
            Invalidate();
        }

        public void SetOnSilent(bool v)
        {
            _toggle.SetSilent(v);
            Invalidate();
        }

        public void SetBusy(bool busy)
        {
            _toggle.Enabled = !busy;
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            int ch = Height - 8;
            if (Width < 8 || ch < 8)
            {
                if (_toggle != null) _toggle.Location = new Point(Math.Max(0, Width - 64), 2);
                return;
            }
            using (GraphicsPath p = Theme.Round(new Rectangle(0, 0, Width, ch), 12))
            {
                Region old = Region;
                Region = new Region(p);
                if (old != null) old.Dispose();
            }
            if (_toggle != null)
                _toggle.Location = new Point(Width - 64, (ch - _toggle.Height) / 2);
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            int ch = Height - 8;
            Color bg = _hover ? Theme.Card2 : Theme.Card;
            Theme.Fill(g, new Rectangle(0, 0, Width - 1, ch - 1), 12, bg);
            Theme.Stroke(g, new Rectangle(0, 0, Width - 1, ch - 1), 12, Theme.Line);

            bool rtl = Theme.Arabic;
            int pad = 18;
            int rightReserve = 150;
            Rectangle titleR = new Rectangle(pad, 12, Width - pad - rightReserve, 22);
            Theme.Draw(g, Tweak.Name, Theme.FBold, Theme.Text, titleR, Theme.FL(rtl));

            Rectangle descR = new Rectangle(pad, 35, Width - pad * 2 - 90, 34);
            Theme.Draw(g, Tweak.Desc, Theme.FSmall, Theme.Muted, descR, Theme.FL(rtl, true));

            Color rc = Tweak.Risk == 0 ? Theme.Accent : (Tweak.Risk == 1 ? Theme.Warn : Theme.Danger);
            string rt = Tweak.Risk == 0 ? L.T("tw.safe") : (Tweak.Risk == 1 ? L.T("tw.moderate") : L.T("tw.advanced"));
            int rw = TextRenderer.MeasureText(rt, Theme.FTiny).Width + 18;
            Rectangle risk = new Rectangle(pad, ch - 28, rw, 17);
            Theme.Pill(g, risk, rc, rt, Theme.FTiny);

            if (Tweak.Reboot)
            {
                string rb = L.T("tw.restart");
                int bw = TextRenderer.MeasureText(rb, Theme.FTiny).Width + 18;
                Rectangle boot = new Rectangle(pad + rw + 8, ch - 28, bw, 17);
                Theme.Pill(g, boot, Theme.Info, rb, Theme.FTiny);
            }

            string st = Tweak.On ? L.T("tw.applied") : L.T("tw.notApplied");
            Rectangle stR = new Rectangle(Width - 178, ch - 30, 106, 18);
            Theme.Draw(g, st, Theme.FTiny, Tweak.On ? Theme.Accent : Theme.Dim, stR, Theme.FL(true));
        }
    }

    // One read-only diagnostic finding. The fix button applies the matching
    // catalog tweak through the exact same journal/undo path as the Tweaks page.
    internal class DiagRow : Control
    {
        public DiagItem Item;
        KButton _fix;
        bool _hover;

        public event EventHandler Fix;

        public DiagRow(DiagItem item)
        {
            Item = item;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
            Height = 106;
            Font = Theme.FBody;

            if (item.Fixable)
            {
                _fix = new KButton(L.T("diag.fix"));
                _fix.Kind = BtnKind.Ghost;
                _fix.Font = Theme.FSmall;
                _fix.Tint = item.Level == 2 ? Theme.Danger : Theme.Accent;
                _fix.Size = new Size(112, 34);
                _fix.BackColor = Theme.Card;
                _fix.Click += delegate { if (Fix != null) Fix(this, EventArgs.Empty); };
                Controls.Add(_fix);
            }
        }

        public void SetBusy(bool busy)
        {
            if (_fix != null) _fix.Enabled = !busy;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            int ch = Height - 8;
            if (Width < 8 || ch < 8)
            {
                if (_fix != null) _fix.Location = new Point(Math.Max(0, Width - 128), 2);
                return;
            }
            using (GraphicsPath p = Theme.Round(new Rectangle(0, 0, Width, ch), 12))
            {
                Region old = Region;
                Region = new Region(p);
                if (old != null) old.Dispose();
            }
            if (_fix != null)
                _fix.Location = new Point(Theme.Arabic ? 16 : Width - 16 - _fix.Width, (ch - _fix.Height) / 2);
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            int ch = Height - 8;
            Color bg = _hover ? Theme.Card2 : Theme.Card;
            Theme.Fill(g, new Rectangle(0, 0, Width - 1, ch - 1), 12, bg);
            Theme.Stroke(g, new Rectangle(0, 0, Width - 1, ch - 1), 12, Theme.Line);

            bool rtl = Theme.Arabic;
            int pad = 18;
            int reserve = _fix != null ? 152 : 18;
            Color lc = Item.Level == 2 ? Theme.Danger : (Item.Level == 1 ? Theme.Warn : Theme.Accent);

            Rectangle titleR = new Rectangle(pad, 13, Width - pad - reserve, 22);
            Theme.Draw(g, Item.Title, Theme.FBold, Theme.Text, titleR, Theme.FL(rtl));

            Rectangle descR = new Rectangle(pad, 37, Width - pad - reserve, 36);
            Theme.Draw(g, Item.Detail, Theme.FSmall, Theme.Muted, descR, Theme.FL(rtl, true));

            string lv = Item.Level == 2 ? L.T("diag.problem") : (Item.Level == 1 ? L.T("diag.attention") : L.T("diag.ok"));
            int pw = TextRenderer.MeasureText(lv, Theme.FTiny).Width + 20;
            int px = rtl ? Width - pad - pw : pad;
            Rectangle pill = new Rectangle(px, ch - 28, pw, 17);
            Theme.Pill(g, pill, lc, lv, Theme.FTiny);

            if (!string.IsNullOrEmpty(Item.FixId))
            {
                string via = L.T("diag.via") + " " + Item.FixId;
                Rectangle vr = new Rectangle(pad + (rtl ? 0 : pw + 10), ch - 28, 220, 17);
                if (rtl) vr = new Rectangle(Width - pad - pw - 10 - 220, ch - 28, 220, 17);
                Theme.Draw(g, via, Theme.FTiny, Theme.Dim, vr, Theme.FL(rtl));
            }
        }
    }

    // Baseline vs current comparison for the local benchmark. Every number is
    // measured on this machine; nothing is estimated or simulated.
    internal class BenchTable : Control
    {
        public BenchResult Before;
        public BenchResult Now;
        public bool Busy;

        public BenchTable()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Card;
            Font = Theme.FBody;
        }

        public void SetData(BenchResult before, BenchResult now)
        {
            Before = before;
            Now = now;
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Theme.SetRegion(this, 12);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            Theme.Stroke(g, new Rectangle(0, 0, Width - 1, Height - 1), 12, Theme.Line);

            bool rtl = Theme.Arabic;
            int pad = 20;
            Theme.Draw(g, L.T("bench.title"), Theme.FH2, Theme.Text,
                new Rectangle(pad, 14, Width - pad * 2, 22), Theme.FL(rtl));

            string stamp = L.T("bench.baseline");
            if (Before != null && !string.IsNullOrEmpty(Before.Stamp)) stamp += ": " + Before.Stamp;
            else stamp += ": " + L.T("bench.none");
            Theme.Draw(g, stamp, Theme.FTiny, Theme.Dim,
                new Rectangle(pad, 37, Width - pad * 2, 17), Theme.FL(rtl));

            int top = 66;
            int bottom = Height - 24;
            if (Now == null && !Busy)
            {
                Theme.Draw(g, L.T("bench.empty"), Theme.FSmall, Theme.Muted,
                    new Rectangle(pad, top, Width - pad * 2, 44), Theme.FL(rtl, true));
                return;
            }
            if (Busy)
            {
                Theme.Draw(g, L.T("bench.running"), Theme.FSmall, Theme.Accent,
                    new Rectangle(pad, top, Width - pad * 2, 44), Theme.FL(rtl, true));
                return;
            }
            if (Before == null) Before = Now;

            int labelW = (int)((Width - pad * 2) * 0.40);
            int colW = (Width - pad * 2 - labelW) / 3;

            // Keep the table optically centred instead of leaving a large void
            // under the rows on tall windows. The note travels with the rows.
            int rowH = 46;
            int avail = Math.Max(120, bottom - top);
            if (avail < 26 + rowH * 5 + 30) rowH = Math.Max(24, (avail - 56) / 5);
            int blockH = 26 + rowH * 5 + 30;
            int headY = top + Math.Max(0, (avail - blockH) / 2);
            int rowY = headY + 28;

            string[] labels = new string[] { L.T("bench.mem"), L.T("bench.disk"), L.T("bench.cpu"), L.T("bench.ping"), L.T("bench.jitter") };
            double[] b = new double[] { Before.MemNs, Before.DiskMs, Before.CpuMops, Before.PingMs, Before.JitterMs };
            double[] n = new double[] { Now.MemNs, Now.DiskMs, Now.CpuMops, Now.PingMs, Now.JitterMs };
            bool[] lower = new bool[] { true, true, false, true, true };
            string[] fmt = new string[] { "0.0", "0.000", "0", "0", "0" };

            string[] heads = new string[] { L.T("bench.metric"), L.T("bench.beforeCol"), L.T("bench.nowCol"), L.T("bench.change") };
            for (int c = 0; c < 4; c++)
            {
                int x = ColX(c, pad, labelW, colW, rtl, Width);
                int w = c == 0 ? labelW : colW;
                Rectangle hr = new Rectangle(x, headY, w, 18);
                if (c == 0)
                    Theme.Draw(g, heads[c], Theme.FTiny, Theme.Dim, hr, Theme.FL(rtl));
                else
                    Theme.DrawC(g, heads[c], Theme.FTiny, Theme.Dim, hr);
            }

            using (Pen rule = new Pen(Theme.Mix(Theme.Card, Theme.Line, 0.75f)))
            {
                g.DrawLine(rule, pad, headY + 23, Width - pad, headY + 23);
            }

            for (int i = 0; i < 5; i++)
            {
                int y = rowY + i * rowH;
                if (i > 0)
                {
                    using (Pen rule = new Pen(Theme.Mix(Theme.Card, Theme.Line, 0.45f)))
                    {
                        g.DrawLine(rule, pad, y - 7, Width - pad, y - 7);
                    }
                }

                int lx = ColX(0, pad, labelW, colW, rtl, Width);
                Theme.Draw(g, labels[i], Theme.FSmall, Theme.Text,
                    new Rectangle(lx, y + 3, labelW, 20), Theme.FL(rtl));

                DrawNum(g, b[i], fmt[i], ColX(1, pad, labelW, colW, rtl, Width), y, colW, Theme.Muted);
                DrawNum(g, n[i], fmt[i], ColX(2, pad, labelW, colW, rtl, Width), y, colW, Theme.Text);

                double d = Bench.Delta(n[i], b[i], lower[i]);
                string ds;
                Color dc;
                if (b[i] <= 0 || n[i] < 0) { ds = "--"; dc = Theme.Dim; }
                else if (Math.Abs(d) < 0.5) { ds = "0%"; dc = Theme.Muted; }
                else
                {
                    ds = (d > 0 ? "+" : "") + d.ToString("0.0") + "%";
                    dc = d > 0 ? Theme.Accent : Theme.Danger;
                }
                Theme.DrawC(g, ds, Theme.FBold, dc,
                    new Rectangle(ColX(3, pad, labelW, colW, rtl, Width), y + 3, colW, 20));
            }

            Theme.Draw(g, L.T("bench.note"), Theme.FTiny, Theme.Dim,
                new Rectangle(pad, rowY + rowH * 5 + 8, Width - pad * 2, 20), Theme.FL(rtl));
        }

        static int ColX(int c, int pad, int labelW, int colW, bool rtl, int width)
        {
            if (!rtl) return pad + (c == 0 ? 0 : labelW + (c - 1) * colW);
            // mirror: metric column on the right, numbers flowing left
            if (c == 0) return width - pad - labelW;
            return width - pad - labelW - c * colW;
        }

        static void DrawNum(Graphics g, double v, string fmt, int x, int y, int w, Color c)
        {
            string s = v < 0 ? "--" : v.ToString(fmt);
            Theme.DrawC(g, s, Theme.FBody, c, new Rectangle(x, y + 6, w, 20));
        }
    }

    // First-run welcome page. It covers the whole window, introduces the safety
    // model in three lines, and links straight to the channel. Shown once; can be
    // reopened from Settings or with F1.
    internal class WelcomeOverlay : Panel
    {
        public event EventHandler Start;
        public event EventHandler Channel;
        public event EventHandler Lang;

        KButton _start;
        KButton _chan;
        KButton _lang;

        public WelcomeOverlay()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
            Font = Theme.FBody;

            _start = new KButton();
            _start.Kind = BtnKind.Primary;
            _start.Size = new Size(190, 44);
            _start.Click += delegate { if (Start != null) Start(this, EventArgs.Empty); };
            Controls.Add(_start);

            _chan = new KButton();
            _chan.Kind = BtnKind.Ghost;
            _chan.Tint = Theme.Ember;
            _chan.Size = new Size(190, 44);
            _chan.Click += delegate { if (Channel != null) Channel(this, EventArgs.Empty); };
            Controls.Add(_chan);

            _lang = new KButton();
            _lang.Kind = BtnKind.Ghost;
            _lang.Tint = Theme.Muted;
            _lang.Font = Theme.FSmall;
            _lang.Size = new Size(120, 34);
            _lang.Click += delegate { if (Lang != null) Lang(this, EventArgs.Empty); };
            Controls.Add(_lang);

            ApplyLang();
        }

        public void ApplyLang()
        {
            _start.Text = L.T("intro.start");
            _chan.Text = L.T("channel.open");
            _lang.Text = Theme.Arabic ? "English" : "العربية";
            LayoutButtons();
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutButtons();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && _start != null && _start.CanFocus) _start.Focus();
        }

        Rectangle CardRect()
        {
            int w = Math.Min(840, Width - 64);
            int h = Math.Min(560, Height - 56);
            if (w < 380) w = Math.Max(340, Width - 16);
            if (h < 420) h = Math.Max(400, Height - 16);
            return new Rectangle((Width - w) / 2, (Height - h) / 2, w, h);
        }

        void LayoutButtons()
        {
            if (_start == null || _chan == null || _lang == null) return;
            Rectangle card = CardRect();
            int pad = 44;
            int btnY = card.Bottom - 88;
            if (Theme.Arabic)
            {
                _start.Location = new Point(card.Right - pad - _start.Width, btnY);
                _chan.Location = new Point(_start.Left - 12 - _chan.Width, btnY);
                _lang.Location = new Point(card.X + pad, btnY + 5);
            }
            else
            {
                _start.Location = new Point(card.X + pad, btnY);
                _chan.Location = new Point(_start.Right + 12, btnY);
                _lang.Location = new Point(card.Right - pad - _lang.Width, btnY + 5);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Theme.Bg);

            // Warm halo so the page does not look like a flat dialog.
            using (GraphicsPath halo = new GraphicsPath())
            {
                Rectangle hr = new Rectangle(Width / 2 - 420, -260, 840, 700);
                halo.AddEllipse(hr);
                using (PathGradientBrush pb = new PathGradientBrush(halo))
                {
                    pb.CenterColor = Theme.A(Theme.Accent, 30);
                    pb.SurroundColors = new Color[] { Theme.A(Theme.Accent, 0) };
                    g.FillPath(pb, halo);
                }
            }

            Rectangle card = CardRect();
            Theme.Fill(g, card, 18, Theme.Card);
            Theme.Stroke(g, card, 18, Theme.Line);
            // Thin orange brand line along the top edge.
            Theme.Fill(g, new Rectangle(card.X + 1, card.Y + 1, card.Width - 2, 3), 2, Theme.A(Theme.Accent, 150));

            bool rtl = Theme.Arabic;
            int pad = 44;
            int x = card.X + pad;
            int w = card.Width - pad * 2;

            Rectangle logo = new Rectangle(rtl ? card.Right - pad - 72 : x, card.Y + 38, 72, 72);
            Theme.DrawMark(g, logo);

            int tx = rtl ? card.X + pad : logo.Right + 18;
            int tw = w - 90;
            Theme.Draw(g, L.T("intro.hello").ToUpperInvariant(), Theme.FTiny, Theme.Accent,
                new Rectangle(tx, card.Y + 40, tw, 16), Theme.FL(rtl));
            Theme.Draw(g, "KiraDemorTY Optimizer", Theme.FTitle, Theme.Text,
                new Rectangle(tx, card.Y + 56, tw, 34), Theme.FL(rtl));
            Theme.Draw(g, L.T("app.tagline"), Theme.FSmall, Theme.Muted,
                new Rectangle(tx, card.Y + 94, tw, 18), Theme.FL(rtl));

            using (Pen line = new Pen(Theme.Line))
                g.DrawLine(line, card.X + pad, card.Y + 132, card.Right - pad, card.Y + 132);

            Theme.Draw(g, L.T("intro.lead"), Theme.FBody, Theme.Muted,
                new Rectangle(x, card.Y + 148, w, 44), Theme.FL(rtl, true));

            int btnY = card.Bottom - 88;
            int noteY = btnY - 26;
            int stripH = 58;
            int stripY = noteY - 12 - stripH;
            Rectangle strip = new Rectangle(x, stripY, w, stripH);

            int y0 = card.Y + 202;
            int rowH = Math.Min(56, Math.Max(42, (stripY - 14 - y0) / 3));
            string[] titles = new string[] { L.T("intro.p1"), L.T("intro.p2"), L.T("intro.p3") };
            string[] descs = new string[] { L.T("intro.d1"), L.T("intro.d2"), L.T("intro.d3") };
            int y = y0;
            for (int i = 0; i < 3; i++)
            {
                Rectangle dot = new Rectangle(rtl ? card.Right - pad - 26 : x, y + 2, 26, 26);
                Theme.Fill(g, dot, 13, Theme.A(Theme.Accent, 24));
                Theme.DrawC(g, (i + 1).ToString(), Theme.FTiny, Theme.Accent, dot);

                int fx = rtl ? card.X + pad : x + 38;
                int fw = w - 38;
                Theme.Draw(g, titles[i], Theme.FBold, Theme.Text,
                    new Rectangle(fx, y, fw, 20), Theme.FL(rtl));
                Theme.Draw(g, descs[i], Theme.FSmall, Theme.Muted,
                    new Rectangle(fx, y + 20, fw, rowH - 20), Theme.FL(rtl, true));
                y += rowH;
            }

            Theme.Fill(g, strip, 12, Theme.Card2);
            Theme.Stroke(g, strip, 12, Theme.A(Theme.Ember, 55));

            Rectangle yt = new Rectangle(rtl ? strip.Right - 18 - 36 : strip.X + 18,
                                         strip.Y + (strip.Height - 26) / 2, 36, 26);
            Theme.Fill(g, yt, 7, Theme.Ember);
            using (SolidBrush white = new SolidBrush(Color.FromArgb(255, 249, 244)))
            {
                Point[] tri = new Point[]
                {
                    new Point(yt.X + 14, yt.Y + 8),
                    new Point(yt.X + 14, yt.Y + 18),
                    new Point(yt.X + 23, yt.Y + 13)
                };
                g.FillPolygon(white, tri);
            }

            int sx = rtl ? strip.X + 18 : yt.Right + 12;
            int sw = strip.Width - 36 - 48;
            Theme.Draw(g, L.T("channel.madeBy") + " " + Brand.ChannelName, Theme.FBold, Theme.Text,
                new Rectangle(sx, strip.Y + 11, sw, 20), Theme.FL(rtl));
            Theme.Draw(g, Brand.ChannelLabel, Theme.FTiny, Theme.Muted,
                new Rectangle(sx, strip.Y + 32, sw, 16), Theme.FL(rtl));

            Theme.Draw(g, L.T("intro.note"), Theme.FTiny, Theme.Dim,
                new Rectangle(x, noteY, w, 18), Theme.FL(rtl));
        }
    }
}
