using System;
using Inventor;
using BracketLibraryInventorPlugin.Models;

namespace BracketLibraryInventorPlugin.Interop
{
    /// <summary>
    /// Dựng biên dạng kín trên 1 <see cref="PlanarSketch"/> từ <see cref="Profile2D"/> (mm, bulge).
    /// Mỗi cạnh: bulge ≈ 0 → SketchLine; ngược lại → SketchArc dựng qua 3 điểm (đầu, điểm-trên-cung, cuối).
    ///
    /// Sagitta của cung = |bulge| · (nửa dây); hướng phồng theo dấu bulge (dương = bên trái đầu→cuối, CCW).
    /// Đây là chuyển đổi 1-1 quy ước bulge LWPolyline (repo MCG_3DPanel) sang sketch Inventor.
    /// </summary>
    internal static class SketchProfileBuilder
    {
        public static Profile Build(PlanarSketch sketch, Profile2D outline, TransientGeometry tg)
        {
            if (outline.Count < 3) throw new ArgumentException("Outline < 3 đỉnh.");

            int n = outline.Count;
            var pts = new Point2d[n];
            for (int i = 0; i < n; i++)
            {
                var v = outline.Vertices[i];
                pts[i] = tg.CreatePoint2d(InventorUnits.Cm(v.X), InventorUnits.Cm(v.Y));
            }

            for (int i = 0; i < n; i++)
            {
                var v = outline.Vertices[i];
                Point2d a = pts[i];
                Point2d b = pts[(i + 1) % n];

                if (Math.Abs(v.Bulge) < 1e-9)
                {
                    sketch.SketchLines.AddByTwoPoints(a, b);
                }
                else
                {
                    Point2d mid = ArcMidPoint(a, b, v.Bulge, tg);
                    sketch.SketchArcs.AddByThreePoints(a, mid, b);
                }
            }

            // Các cạnh kề dùng chung toạ độ đỉnh → Inventor tự tạo ràng buộc coincident (trong dung sai).
            // AddForSolid gộp thành 1 vùng kín. Nếu profile hở, exception được ném lên (đã log ở lớp gọi).
            var profile = sketch.Profiles.AddForSolid();
            return profile;
        }

        private static Point2d ArcMidPoint(Point2d a, Point2d b, double bulge, TransientGeometry tg)
        {
            double mx = (a.X + b.X) / 2.0;
            double my = (a.Y + b.Y) / 2.0;
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double chord = Math.Sqrt(dx * dx + dy * dy);
            if (chord < 1e-12) return tg.CreatePoint2d(mx, my);

            double sagitta = Math.Abs(bulge) * (chord / 2.0);
            // Pháp tuyến trái của (a→b): (-dy, dx)/chord. Bulge > 0 → phồng sang trái (CCW).
            double nx = -dy / chord;
            double ny = dx / chord;
            double s = bulge > 0 ? 1.0 : -1.0;
            return tg.CreatePoint2d(mx + s * nx * sagitta, my + s * ny * sagitta);
        }
    }
}
