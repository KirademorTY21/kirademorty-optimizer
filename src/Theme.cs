using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KDT
{
    internal static class Theme
    {
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        static extern int SetWindowTheme(IntPtr hWnd, string subApp, string subId);

        // Makes native scrollbars and text-box chrome render dark instead of the
        // default light grey. Works on Windows 10 1809+ and Windows 11.
        public static void DarkScroll(Control c)
        {
            if (c == null) return;
            c.HandleCreated += delegate
            {
                try { SetWindowTheme(c.Handle, "DarkMode_Explorer", null); }
                catch { }
            };
            if (c.IsHandleCreated)
            {
                try { SetWindowTheme(c.Handle, "DarkMode_Explorer", null); }
                catch { }
            }
        }

        // Warm dark neutrals so the orange brand colour sits naturally on top.
        public static readonly Color Bg = Color.FromArgb(12, 12, 14);
        public static readonly Color Side = Color.FromArgb(16, 16, 19);
        public static readonly Color Card = Color.FromArgb(22, 22, 26);
        public static readonly Color Card2 = Color.FromArgb(28, 27, 31);
        public static readonly Color Line = Color.FromArgb(44, 40, 42);
        public static readonly Color Text = Color.FromArgb(242, 238, 233);
        public static readonly Color Muted = Color.FromArgb(162, 154, 148);
        public static readonly Color Dim = Color.FromArgb(104, 97, 92);

        // Brand: KiraDemorTY orange.
        public static readonly Color Accent = Color.FromArgb(255, 138, 30);
        public static readonly Color AccentDeep = Color.FromArgb(214, 96, 0);
        public static readonly Color Ember = Color.FromArgb(255, 63, 31);
        public static readonly Color OnAccent = Color.FromArgb(26, 13, 5);

        // Kept clearly outside the brand hue so warnings never read as "on".
        public static readonly Color Warn = Color.FromArgb(255, 194, 75);
        public static readonly Color Danger = Color.FromArgb(255, 77, 109);
        public static readonly Color Info = Color.FromArgb(90, 160, 255);

        public static bool Arabic = false;

        public static readonly Font FTitle = new Font("Segoe UI", 19f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font FH2 = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font FBody = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FBold = new Font("Segoe UI", 9.5f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font FSmall = new Font("Segoe UI", 8.3f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FTiny = new Font("Segoe UI", 7.6f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FValue = new Font("Segoe UI", 26f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font FNum = new Font("Segoe UI", 15f, FontStyle.Bold, GraphicsUnit.Point);

        public static GraphicsPath Round(Rectangle r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            if (r.Width < 2 || r.Height < 2)
            {
                p.AddRectangle(new Rectangle(r.X, r.Y, Math.Max(1, r.Width), Math.Max(1, r.Height)));
                return p;
            }
            if (rad <= 0)
            {
                p.AddRectangle(r);
                return p;
            }
            int d = rad * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static Color A(Color c, int alpha)
        {
            return Color.FromArgb(Math.Max(0, Math.Min(255, alpha)), c.R, c.G, c.B);
        }

        public static Color Mix(Color a, Color b, double t)
        {
            if (t < 0) t = 0;
            if (t > 1) t = 1;
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        public static TextFormatFlags FL(bool right, bool wrap)
        {
            TextFormatFlags f = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
            if (wrap) f |= TextFormatFlags.WordBreak;
            if (right)
            {
                f |= TextFormatFlags.Right;
                f |= TextFormatFlags.RightToLeft;
            }
            return f;
        }

        public static TextFormatFlags FL(bool right)
        {
            return FL(right, false);
        }

        public static void Draw(Graphics g, string s, Font f, Color c, Rectangle r, TextFormatFlags flags)
        {
            TextRenderer.DrawText(g, s == null ? "" : s, f, r, c, flags);
        }

        public static void DrawC(Graphics g, string s, Font f, Color c, Rectangle r)
        {
            TextRenderer.DrawText(g, s == null ? "" : s, f, r, c,
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        public static void Fill(Graphics g, Rectangle r, int rad, Color c)
        {
            using (GraphicsPath p = Round(r, rad))
            using (SolidBrush b = new SolidBrush(c))
            {
                g.FillPath(b, p);
            }
        }

        public static void Stroke(Graphics g, Rectangle r, int rad, Color c)
        {
            using (GraphicsPath p = Round(r, rad))
            using (Pen pen = new Pen(c, 1f))
            {
                g.DrawPath(pen, p);
            }
        }

        public static void SetRegion(Control c, int rad)
        {
            if (c.Width <= 0 || c.Height <= 0) return;
            using (GraphicsPath p = Round(new Rectangle(0, 0, c.Width, c.Height), rad))
            {
                Region old = c.Region;
                c.Region = new Region(p);
                if (old != null) old.Dispose();
            }
        }

        public static int Clamp(int v, int lo, int hi)
        {
            if (v < lo) return lo;
            if (v > hi) return hi;
            return v;
        }

        public static string Pct(double v)
        {
            if (v < 0) return "--";
            return ((int)Math.Round(v)).ToString() + "%";
        }

        public static void Pill(Graphics g, Rectangle r, Color color, string text, Font f)
        {
            Theme.Fill(g, r, r.Height / 2, A(color, 26));
            Theme.DrawC(g, text, f, color, r);
        }

        // Brand mark: dark rounded tile, warm inner glow, cream-to-orange K.
        // Shared by the sidebar logo so the window matches the EXE icon.
        public static void DrawMark(Graphics g, Rectangle r)
        {
            if (r.Width < 6 || r.Height < 6) return;
            int rad = Math.Max(4, r.Width / 5);
            using (GraphicsPath tile = Round(r, rad))
            {
                using (LinearGradientBrush body = new LinearGradientBrush(
                    new Rectangle(r.X, r.Y, r.Width, r.Height),
                    Color.FromArgb(41, 35, 43), Color.FromArgb(12, 11, 14), 90f))
                {
                    g.FillPath(body, tile);
                }

                Rectangle gr = new Rectangle(r.X - r.Width / 8, r.Y, r.Width + r.Width / 4, r.Height + r.Height / 4);
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(gr);
                    using (PathGradientBrush pb = new PathGradientBrush(gp))
                    {
                        pb.CenterColor = A(Accent, 34);
                        pb.SurroundColors = new Color[] { A(Accent, 0) };
                        GraphicsState st = g.Save();
                        g.SetClip(tile);
                        g.FillPath(pb, gp);
                        g.Restore(st);
                    }
                }

                float w = Math.Max(1.7f, r.Width * 0.108f);
                using (GraphicsPath k = new GraphicsPath())
                {
                    float x0 = r.X + r.Width * 0.302f;
                    float xm = r.X + r.Width * 0.352f;
                    float x1 = r.X + r.Width * 0.716f;
                    float y0 = r.Y + r.Height * 0.238f;
                    float y1 = r.Y + r.Height * 0.500f;
                    float y2 = r.Y + r.Height * 0.762f;
                    float ay0 = r.Y + r.Height * 0.226f;
                    float ay1 = r.Y + r.Height * 0.774f;
                    k.AddLine(x0, y0, x0, y2);
                    k.StartFigure();
                    k.AddLine(x1, ay0, xm, y1);
                    k.AddLine(xm, y1, x1, ay1);

                    using (Pen halo = new Pen(A(Accent, 30), w * 1.55f))
                    {
                        halo.StartCap = LineCap.Round;
                        halo.EndCap = LineCap.Round;
                        halo.LineJoin = LineJoin.Round;
                        g.DrawPath(halo, k);
                    }
                    RectangleF kr = new RectangleF(r.X, r.Y + r.Height * 0.18f, r.Width, r.Height * 0.64f);
                    using (LinearGradientBrush kb = new LinearGradientBrush(kr,
                        Color.FromArgb(255, 247, 236), Color.FromArgb(255, 118, 14), 90f))
                    {
                        ColorBlend blend = new ColorBlend(3);
                        blend.Colors = new Color[] { Color.FromArgb(255, 247, 236), Color.FromArgb(255, 196, 118), Color.FromArgb(255, 118, 14) };
                        blend.Positions = new float[] { 0f, 0.46f, 1f };
                        kb.InterpolationColors = blend;
                        using (Pen pen = new Pen(kb, w))
                        {
                            pen.StartCap = LineCap.Round;
                            pen.EndCap = LineCap.Round;
                            pen.LineJoin = LineJoin.Round;
                            g.DrawPath(pen, k);
                        }
                    }
                }

                using (Pen rim = new Pen(A(Accent, 120), 1f))
                    g.DrawPath(rim, tile);
            }
        }
    }
}
