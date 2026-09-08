using System;

namespace BracketLibraryInventorPlugin.Geometry
{
    /// <summary>Vector 2D tối giản (mm) — thay Autodesk.AutoCAD.Geometry.Vector2d/Point2d khi port ProfileService.</summary>
    public struct Vec2
    {
        public double X, Y;
        public Vec2(double x, double y) { X = x; Y = y; }

        public double Length => Math.Sqrt(X * X + Y * Y);

        public Vec2 Normal()
        {
            double l = Length;
            return l < 1e-12 ? new Vec2(0, 0) : new Vec2(X / l, Y / l);
        }

        public double Dot(Vec2 o) => X * o.X + Y * o.Y;
        public Vec2 Negate() => new Vec2(-X, -Y);

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);
        public static Vec2 operator *(Vec2 a, double s) => new Vec2(a.X * s, a.Y * s);

        public override string ToString() => $"({X:0.###},{Y:0.###})";
    }

    /// <summary>Toán cung tròn dùng chung cho generator biên dạng (bulge, tiếp tuyến).</summary>
    public static class ArcMath
    {
        /// <summary>
        /// Bulge (chuẩn LWPolyline) của cung từ <paramref name="ps"/> → <paramref name="pe"/> quanh tâm
        /// <paramref name="c"/>: lấy cung nhỏ, dấu theo chiều quét. Port 1-1 từ ProfileService.ArcBulge.
        /// </summary>
        public static double Bulge(Vec2 c, Vec2 ps, Vec2 pe)
        {
            double a0 = Math.Atan2(ps.Y - c.Y, ps.X - c.X);
            double a1 = Math.Atan2(pe.Y - c.Y, pe.X - c.X);
            double sweep = a1 - a0;
            while (sweep <= -Math.PI) sweep += 2.0 * Math.PI;
            while (sweep > Math.PI) sweep -= 2.0 * Math.PI;
            return Math.Tan(sweep / 4.0);
        }

        /// <summary>
        /// Tiếp điểm trên đường tròn (tâm <paramref name="c"/>, bán kính <paramref name="r"/>) của tiếp
        /// tuyến đi qua ĐIỂM NGOÀI <paramref name="p"/>. Chọn nghiệm cùng hướng <paramref name="refN"/>.
        /// Port 1-1 từ ProfileService.TangentPointFromExternal.
        /// </summary>
        public static Vec2 TangentPointFromExternal(Vec2 c, double r, Vec2 p, Vec2 refN)
        {
            Vec2 e = p - c;
            double d2 = e.X * e.X + e.Y * e.Y;
            if (d2 <= r * r) return p;
            double rd2 = (r * r) / d2;
            double k = r * Math.Sqrt(d2 - r * r) / d2;
            var perp = new Vec2(-e.Y, e.X);
            Vec2 t1 = c + e * rd2 + perp * k;
            Vec2 t2 = c + e * rd2 - perp * k;
            double dot1 = (t1 - c).Dot(refN), dot2 = (t2 - c).Dot(refN);
            return dot1 >= dot2 ? t1 : t2;
        }

        /// <summary>Bán kính cung suy từ dây (ps→pe) và bulge — tiện cho chẩn đoán / dựng sketch.</summary>
        public static double RadiusFromBulge(Vec2 ps, Vec2 pe, double bulge)
        {
            if (Math.Abs(bulge) < 1e-12) return 0.0;
            double chord = (pe - ps).Length;
            double theta = 4.0 * Math.Atan(Math.Abs(bulge)); // góc quét
            return chord / (2.0 * Math.Sin(theta / 2.0));
        }
    }
}
