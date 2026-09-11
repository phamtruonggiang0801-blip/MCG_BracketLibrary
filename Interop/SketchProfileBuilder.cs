using System;
using Inventor;
using BracketLibraryInventorPlugin.Models;
using BracketLibraryInventorPlugin.Utilities;

namespace BracketLibraryInventorPlugin.Interop
{
    /// <summary>
    /// Dựng biên dạng kín trên 1 <see cref="PlanarSketch"/> từ <see cref="Profile2D"/> (mm, bulge).
    /// Mỗi cạnh: bulge ≈ 0 → SketchLine; ngược lại → SketchArc dựng qua 3 điểm (đầu, điểm-trên-cung, cuối).
    ///
    /// QUY ƯỚC BULGE (chuẩn LWPolyline AutoCAD, khớp ProfileService repo MCG_3DPanel):
    ///   bulge = tan(gócQuét / 4), DƯƠNG khi cung đi CCW từ điểm đầu tới điểm cuối.
    ///   Cung CCW (bulge &gt; 0) PHỒNG SANG PHẢI của hướng đầu→cuối (ví dụ: nửa đường tròn từ
    ///   (0,0) tới (10,0) với bulge +1 đi qua (5,−5) — phía dưới = bên phải khi đi theo +X).
    ///
    /// Để AddForSolid nhận ra 1 vùng kín, các cạnh phải NỐI NHAU qua CÙNG 1 SketchPoint (không chỉ
    /// trùng toạ độ). Vì vậy ta chuyền điểm cuối của cạnh trước làm điểm đầu cạnh sau, và đóng vòng
    /// bằng chính SketchPoint đầu tiên.
    /// </summary>
    internal static class SketchProfileBuilder
    {
        private const string LOG = "[SketchProfileBuilder]";

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

            // Chuyền điểm cuối cạnh trước → điểm đầu cạnh sau để mọi cạnh dùng chung SketchPoint.
            object prevEnd = pts[0];      // Point2d cho cạnh đầu, SketchPoint cho các cạnh sau
            SketchPoint firstStart = null;

            for (int i = 0; i < n; i++)
            {
                var v = outline.Vertices[i];
                bool last = i == n - 1;
                object endArg = last && firstStart != null ? (object)firstStart : pts[(i + 1) % n];

                if (Math.Abs(v.Bulge) < 1e-9)
                {
                    var line = sketch.SketchLines.AddByTwoPoints(prevEnd, endArg);
                    if (i == 0) firstStart = line.StartSketchPoint;
                    prevEnd = line.EndSketchPoint;
                }
                else
                {
                    Point2d mid = ArcMidPoint(pts[i], pts[(i + 1) % n], v.Bulge, tg);
                    var arc = sketch.SketchArcs.AddByThreePoints(prevEnd, mid, endArg);
                    if (i == 0) firstStart = arc.StartSketchPoint;
                    prevEnd = arc.EndSketchPoint;
                }
            }

            try
            {
                return sketch.Profiles.AddForSolid();
            }
            catch (Exception ex)
            {
                FileLogger.LogException(LOG, "AddForSolid — " + DiagnoseOpenLoop(sketch, tg), ex);
                throw;
            }
        }

        /// <summary>Số liệu sketch để chẩn đoán "profile hở" khi AddForSolid lỗi.</summary>
        private static string DiagnoseOpenLoop(PlanarSketch sketch, TransientGeometry tg)
        {
            try
            {
                return $"lines={sketch.SketchLines.Count} arcs={sketch.SketchArcs.Count} " +
                       $"points={sketch.SketchPoints.Count}";
            }
            catch { return "diag n/a"; }
        }

        /// <summary>
        /// Điểm giữa CUNG (để dựng SketchArc qua 3 điểm) từ dây <paramref name="a"/>→<paramref name="b"/>
        /// và bulge. Sagitta = |bulge|·(nửa dây); cung CCW (bulge &gt; 0) phồng sang PHẢI của a→b.
        /// </summary>
        private static Point2d ArcMidPoint(Point2d a, Point2d b, double bulge, TransientGeometry tg)
        {
            double mx = (a.X + b.X) / 2.0;
            double my = (a.Y + b.Y) / 2.0;
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double chord = Math.Sqrt(dx * dx + dy * dy);
            if (chord < 1e-12) return tg.CreatePoint2d(mx, my);

            double sagitta = Math.Abs(bulge) * (chord / 2.0);
            // Pháp tuyến PHẢI của (a→b): (dy, −dx)/chord. Bulge > 0 (CCW) → phồng sang phải.
            double nx = dy / chord;
            double ny = -dx / chord;
            double s = bulge > 0 ? 1.0 : -1.0;
            return tg.CreatePoint2d(mx + s * nx * sagitta, my + s * ny * sagitta);
        }
    }
}
