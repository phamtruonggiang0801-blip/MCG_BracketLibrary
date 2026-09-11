using System;
using Inventor;

namespace BracketLibraryInventorPlugin.Interop
{
    /// <summary>
    /// Đo đạc hình học Inventor cho <see cref="BracketLocationPicker"/>. ĐƠN VỊ TRẢ VỀ: mm.
    ///
    /// Geometry của entity pick qua <c>CommandManager.Pick</c> đã ở hệ toạ độ tài liệu đang mở
    /// (assembly hoặc part) — không transform thêm ở đây. (FaceProxy context trong assembly:
    /// xem backlog docs/ARCHITECTURE.md §8.3.)
    /// </summary>
    internal static class GeometryMeasure
    {
        /// <summary>Mặt phẳng của 1 planar Face: điểm gốc + pháp tuyến (Vector, đơn vị nội bộ cm).</summary>
        public static (Point Root, Vector Normal) PlaneOf(Face face)
        {
            var pl = (Plane)face.Geometry;
            return (pl.RootPoint, pl.Normal.AsVector());
        }

        /// <summary>Đường thẳng của 1 linear Edge: điểm đầu, điểm cuối, hướng (cm).</summary>
        public static (Point Start, Point End, Vector Dir) LineOf(Edge edge)
        {
            var ls = (LineSegment)edge.Geometry;
            return (ls.StartPoint, ls.EndPoint, ls.Direction.AsVector());
        }

        /// <summary>Khoảng vuông góc từ điểm <paramref name="p"/> tới mặt phẳng (mm). <paramref name="unitNormal"/> phải đã chuẩn hoá.</summary>
        public static double DistancePointToPlane(Point p, Point planeRoot, Vector unitNormal)
        {
            double d = (p.X - planeRoot.X) * unitNormal.X
                     + (p.Y - planeRoot.Y) * unitNormal.Y
                     + (p.Z - planeRoot.Z) * unitNormal.Z;
            return InventorUnits.Mm(Math.Abs(d));
        }

        /// <summary>Trị tuyệt đối hình chiếu của (to − from) lên <paramref name="unitDir"/> (mm).</summary>
        public static double ProjectedDistance(Point from, Point to, Vector unitDir)
        {
            double d = (to.X - from.X) * unitDir.X + (to.Y - from.Y) * unitDir.Y + (to.Z - from.Z) * unitDir.Z;
            return InventorUnits.Mm(Math.Abs(d));
        }

        /// <summary>Bề rộng trải của mặt theo 1 hướng = max − min hình chiếu đỉnh (mm). null nếu &lt; 1 mm.</summary>
        public static double? FaceSpanAlong(Face face, Vector unitDir)
        {
            double lo = double.MaxValue, hi = double.MinValue;
            foreach (Vertex v in face.Vertices)
            {
                var pt = v.Point;
                double t = pt.X * unitDir.X + pt.Y * unitDir.Y + pt.Z * unitDir.Z;
                if (t < lo) lo = t;
                if (t > hi) hi = t;
            }
            return Positive(hi - lo);
        }

        /// <summary>Reach xa nhất của mặt so với <paramref name="origin"/> theo <paramref name="unitDir"/> (mm) — đo overhang từ chân Web.</summary>
        public static double? MaxReachFromPoint(Face face, Point origin, Vector unitDir)
        {
            double hi = double.MinValue;
            foreach (Vertex v in face.Vertices)
            {
                var pt = v.Point;
                double t = (pt.X - origin.X) * unitDir.X + (pt.Y - origin.Y) * unitDir.Y + (pt.Z - origin.Z) * unitDir.Z;
                if (t > hi) hi = t;
            }
            return hi == double.MinValue ? (double?)null : Positive(hi);
        }

        /// <summary>Đổi cm → mm, trả null nếu ≤ 1 mm (coi như không đo được).</summary>
        private static double? Positive(double cm)
        {
            double mm = InventorUnits.Mm(cm);
            return mm > 1.0 ? mm : (double?)null;
        }

        // ─────────────────────────── CHẨN ĐOÁN (ghi log) ───────────────────────────

        private static double Dot(Vector a, Vector b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        private static double Mm(double cm) => cm * InventorUnits.CmToMm;

        /// <summary>Cạnh (chưa có frame): 2 đầu (mm), độ dài, hướng.</summary>
        public static string DescribeEdgeRaw(Edge edge)
        {
            var (s, e, d) = LineOf(edge);
            double len = Math.Sqrt((e.X - s.X) * (e.X - s.X) + (e.Y - s.Y) * (e.Y - s.Y) + (e.Z - s.Z) * (e.Z - s.Z));
            return $"đầu=({Mm(s.X):0},{Mm(s.Y):0},{Mm(s.Z):0}) cuối=({Mm(e.X):0},{Mm(e.Y):0},{Mm(e.Z):0}) " +
                   $"len={Mm(len):0} dir=({d.X:0.00},{d.Y:0.00},{d.Z:0.00})";
        }

        /// <summary>Mặt phẳng (chưa có frame): điểm gốc + pháp tuyến.</summary>
        public static string DescribePlaneRaw(Face face)
        {
            var (r, n) = PlaneOf(face);
            return $"root=({Mm(r.X):0},{Mm(r.Y):0},{Mm(r.Z):0}) normal=({n.X:0.00},{n.Y:0.00},{n.Z:0.00}) verts={CountVertices(face)}";
        }

        /// <summary>Định hướng mặt so với hệ frame: dot pháp tuyến với member / up / webNormal.</summary>
        public static string DescribePlane(Face face, Vector member, Vector up, Vector webNormal)
        {
            var (_, n) = PlaneOf(face);
            n.Normalize();
            double dm = Dot(n, member), du = Dot(n, up), dw = Dot(n, webNormal);
            string ori =
                Math.Abs(du) > 0.9 ? "NGANG (∥ TopPlate)" :
                Math.Abs(dw) > 0.9 ? "∥ mặt Web (cùng mp bracket)" :
                Math.Abs(dm) > 0.9 ? "⊥ member (mặt đầu)" : "nghiêng";
            return $"pháp tuyến·member={dm:0.00} ·up={du:0.00} ·webN={dw:0.00} → {ori}";
        }

        /// <summary>Hình chiếu đỉnh mặt lên 3 trục frame, so với <paramref name="origin"/> (mm) — thấy mặt trải cỡ nào, phía nào.</summary>
        public static string DescribeFaceExtent(Face face, Point origin, Vector member, Vector up, Vector webNormal)
        {
            double mLo = double.MaxValue, mHi = double.MinValue;
            double uLo = double.MaxValue, uHi = double.MinValue;
            double wLo = double.MaxValue, wHi = double.MinValue;
            int nv = 0;
            foreach (Vertex v in face.Vertices)
            {
                nv++;
                var p = v.Point;
                double m = (p.X - origin.X) * member.X + (p.Y - origin.Y) * member.Y + (p.Z - origin.Z) * member.Z;
                double u = (p.X - origin.X) * up.X + (p.Y - origin.Y) * up.Y + (p.Z - origin.Z) * up.Z;
                double w = (p.X - origin.X) * webNormal.X + (p.Y - origin.Y) * webNormal.Y + (p.Z - origin.Z) * webNormal.Z;
                if (m < mLo) mLo = m; if (m > mHi) mHi = m;
                if (u < uLo) uLo = u; if (u > uHi) uHi = u;
                if (w < wLo) wLo = w; if (w > wHi) wHi = w;
            }
            return $"{nv} đỉnh | so với gốc (mm): member[{Mm(mLo):0}..{Mm(mHi):0}] " +
                   $"up[{Mm(uLo):0}..{Mm(uHi):0}] webN[{Mm(wLo):0}..{Mm(wHi):0}]";
        }

        /// <summary>Cạnh so với frame: độ dài + vị trí 2 đầu chiếu lên member (so với gốc).</summary>
        public static string DescribeEdge(Edge edge, Point origin, Vector member)
        {
            var (s, e, _) = LineOf(edge);
            double len = Math.Sqrt((e.X - s.X) * (e.X - s.X) + (e.Y - s.Y) * (e.Y - s.Y) + (e.Z - s.Z) * (e.Z - s.Z));
            double sM = (s.X - origin.X) * member.X + (s.Y - origin.Y) * member.Y + (s.Z - origin.Z) * member.Z;
            double eM = (e.X - origin.X) * member.X + (e.Y - origin.Y) * member.Y + (e.Z - origin.Z) * member.Z;
            return $"len={Mm(len):0} | đầu@member={Mm(sM):0} cuối@member={Mm(eM):0}";
        }

        private static int CountVertices(Face face)
        {
            int n = 0;
            foreach (Vertex _ in face.Vertices) n++;
            return n;
        }
    }
}
