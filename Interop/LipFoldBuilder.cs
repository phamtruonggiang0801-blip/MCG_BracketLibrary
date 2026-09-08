using System;
using Inventor;
using BracketLibraryInventorPlugin.Geometry;
using BracketLibraryInventorPlugin.Models;

namespace BracketLibraryInventorPlugin.Interop
{
    /// <summary>
    /// Gấp lip dọc cạnh chéo tự do khi <c>solution.Flanged</c>. Bản DEMO: dựng 1 flange chữ nhật
    /// (rộng <c>LipWidth</c> × dài free-edge, dày <c>Thk</c>) vuông góc tấm, lệch ra ngoài 1 khoảng
    /// <c>BendRadius</c>, rồi Join.
    ///
    /// ⚠️ CHƯA đủ so với bản AutoCAD (BracketBuilder.TryAddBendLip):
    ///   - thiếu fillet gấp (arc trong Rb / ngoài Rb+thk) — hiện là góc vuông.
    ///   - thiếu Sniped End 2 đầu lip (rise = lipW−15, run = rise·tan60°).
    ///   Xem docs/ARCHITECTURE.md §"Việc còn lại" + skill parametric-profiles mục 5b.
    /// </summary>
    internal static class LipFoldBuilder
    {
        public static void TryAdd(
            PartComponentDefinition partDef, WorkPlane platePlane,
            FreeEdge fe, KneeSolution k, TransientGeometry tg)
        {
            if (fe == null || fe.Length < 1e-3) return;

            var pA = tg.CreatePoint(InventorUnits.Cm(fe.Ax), InventorUnits.Cm(fe.Ay), 0);
            var pB = tg.CreatePoint(InventorUnits.Cm(fe.Bx), InventorUnits.Cm(fe.By), 0);
            WorkAxis freeAxis = partDef.WorkAxes.AddByTwoPoints(pA, pB, true);
            freeAxis.Visible = false;

            double angle = k.FoldSign >= 0 ? Math.PI / 2.0 : -Math.PI / 2.0;
            WorkPlane foldPlane;
            try
            {
                foldPlane = partDef.WorkPlanes.AddByLinePlaneAndAngle(freeAxis, platePlane, angle, true);
                foldPlane.Visible = false;
            }
            catch
            {
                try { freeAxis.Delete(); } catch { }
                return;
            }

            var lipSketch = partDef.Sketches.Add(foldPlane);
            lipSketch.Name = "LipFold";

            double len = InventorUnits.Cm(fe.Length);
            double w0 = InventorUnits.Cm(k.BendRadius);
            double w1 = InventorUnits.Cm(k.BendRadius + k.LipWidth);

            var q0 = tg.CreatePoint2d(0, w0);
            var q1 = tg.CreatePoint2d(len, w0);
            var q2 = tg.CreatePoint2d(len, w1);
            var q3 = tg.CreatePoint2d(0, w1);
            lipSketch.SketchLines.AddByTwoPoints(q0, q1);
            lipSketch.SketchLines.AddByTwoPoints(q1, q2);
            lipSketch.SketchLines.AddByTwoPoints(q2, q3);
            lipSketch.SketchLines.AddByTwoPoints(q3, q0);

            var prof = lipSketch.Profiles.AddForSolid();
            var def = partDef.Features.ExtrudeFeatures.CreateExtrudeDefinition(
                prof, PartFeatureOperationEnum.kJoinOperation);
            def.SetDistanceExtent(InventorUnits.Cm(k.Thk), PartFeatureExtentDirectionEnum.kSymmetricExtentDirection);
            partDef.Features.ExtrudeFeatures.Add(def);
        }
    }
}
