using System;
using System.Collections.Generic;
using BracketLibraryInventorPlugin.Models;

namespace BracketLibraryInventorPlugin.Geometry
{
    /// <summary>
    /// Rải outline (đỉnh + bulge) thành list điểm phẳng (mm) để vẽ preview 2D trong palette.
    /// Quy ước bulge khớp <c>SketchProfileBuilder</c>: cung CCW (bulge &gt; 0) phồng sang PHẢI của đầu→cuối.
    /// Bulge của outline &lt; 1 (cung nhỏ) nên chỉ xử lý cung &lt; 180°.
    /// </summary>
    public static class OutlineTessellator
    {
        public static List<Vec2> Tessellate(Profile2D outline, int segPerArc = 10)
        {
            var pts = new List<Vec2>();
            if (outline == null) return pts;
            int n = outline.Count;
            if (n < 2) return pts;

            for (int i = 0; i < n; i++)
            {
                var v = outline.Vertices[i];
                var nv = outline.Vertices[(i + 1) % n];
                var a = new Vec2(v.X, v.Y);
                var b = new Vec2(nv.X, nv.Y);
                pts.Add(a);

                if (Math.Abs(v.Bulge) < 1e-9) continue;

                double dx = b.X - a.X, dy = b.Y - a.Y;
                double chord = Math.Sqrt(dx * dx + dy * dy);
                if (chord < 1e-9) continue;

                double sag = Math.Abs(v.Bulge) * (chord / 2.0);
                double s = v.Bulge > 0 ? 1.0 : -1.0;      // phải của a→b
                double nx = dy / chord, ny = -dx / chord;  // pháp tuyến phải
                var mid = new Vec2((a.X + b.X) / 2.0 + s * nx * sag,
                                   (a.Y + b.Y) / 2.0 + s * ny * sag);

                if (!CircleFrom3(a, mid, b, out var c, out double r)) continue;
                double a0 = Math.Atan2(a.Y - c.Y, a.X - c.X);
                double am = Math.Atan2(mid.Y - c.Y, mid.X - c.X);
                double a1 = Math.Atan2(b.Y - c.Y, b.X - c.X);

                double sweep = Norm(a1 - a0);
                double midOff = Norm(am - a0);
                if (Math.Sign(midOff) != Math.Sign(sweep))
                    sweep += sweep > 0 ? -2.0 * Math.PI : 2.0 * Math.PI;

                for (int j = 1; j < segPerArc; j++)
                {
                    double t = a0 + sweep * (j / (double)segPerArc);
                    pts.Add(new Vec2(c.X + r * Math.Cos(t), c.Y + r * Math.Sin(t)));
                }
            }
            return pts;
        }

        private static double Norm(double x)
        {
            while (x <= -Math.PI) x += 2.0 * Math.PI;
            while (x > Math.PI) x -= 2.0 * Math.PI;
            return x;
        }

        private static bool CircleFrom3(Vec2 a, Vec2 b, Vec2 c, out Vec2 center, out double radius)
        {
            center = new Vec2(0, 0);
            radius = 0;
            double d = 2.0 * (a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y));
            if (Math.Abs(d) < 1e-9) return false;
            double aa = a.X * a.X + a.Y * a.Y, bb = b.X * b.X + b.Y * b.Y, cc = c.X * c.X + c.Y * c.Y;
            double ux = (aa * (b.Y - c.Y) + bb * (c.Y - a.Y) + cc * (a.Y - b.Y)) / d;
            double uy = (aa * (c.X - b.X) + bb * (a.X - c.X) + cc * (b.X - a.X)) / d;
            center = new Vec2(ux, uy);
            radius = Math.Sqrt((a.X - ux) * (a.X - ux) + (a.Y - uy) * (a.Y - uy));
            return true;
        }
    }
}
