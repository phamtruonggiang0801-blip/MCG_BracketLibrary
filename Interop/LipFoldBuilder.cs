using System;
using Inventor;
using BracketLibraryInventorPlugin.Geometry;
using BracketLibraryInventorPlugin.Models;
using BracketLibraryInventorPlugin.Utilities;

namespace BracketLibraryInventorPlugin.Interop
{
    /// <summary>
    /// Gấp lip (mép gia cường) dọc free-edge khi <c>k.Flanged</c> — 1 tấm kim loại GẤP LẠI, dày đều
    /// <c>Thk</c>, FILLET tại đường gập (bán kính TRONG <c>Rb = 2·thk</c>, NGOÀI <c>Rb + thk</c>),
    /// 2 đầu Sniped (<c>rise = LipWidth − 15</c>, <c>run = rise·tan60°</c>).
    ///
    /// Port <c>BracketBuilder.TryAddBendLip</c> + <c>SnipLipEnds</c> (repo MCG_3DPanel). Khác:
    /// AutoCAD union ACIS (chấp nhận chạm tiếp tuyến); Inventor tạo body riêng (<c>kNewBodyOperation</c>)
    /// rồi thử Combine(Join) — nếu Join lỗi thì để 2 body (vẫn thấy lip).
    /// </summary>
    internal static class LipFoldBuilder
    {
        private const string LOG = "[LipFoldBuilder]";
        private const double EndFlatMm = 15.0;
        private const double OvMm = 2.0;                           // chồng mép vào thân để Combine chắc (AutoCAD ACIS dùng 0.5)
        private static readonly double Tan60 = Math.Tan(Math.PI / 3.0);

        public static string TryAdd(global::Inventor.Application app, PartComponentDefinition partDef,
                                    Profile2D outline, FreeEdge fe, KneeSolution k)
        {
            if (fe == null || fe.Length < 2.0) return "no-freeEdge";
            double t = k.Thk, Rb = k.BendRadius, lipW = k.LipWidth, len = fe.Length;
            if (t < 0.1 || lipW < 1.0) return "bad-params";

            var tg = app.TransientGeometry;

            // ── hướng free-edge + pháp tuyến trong mặt tấm (ra NGOÀI thân) ──
            double dx = fe.Bx - fe.Ax, dy = fe.By - fe.Ay;
            double L2 = Math.Sqrt(dx * dx + dy * dy);
            if (L2 < 1e-6) return "degenerate";
            double ex = dx / L2, ey = dy / L2;
            double nx = ey, ny = -ex;
            var (cx, cy) = Centroid(outline);
            double midX = (fe.Ax + fe.Bx) / 2.0, midY = (fe.Ay + fe.By) / 2.0;
            if (nx * (midX - cx) + ny * (midY - cy) < 0.0) { nx = -nx; ny = -ny; } // trỏ ra ngoài
            int fs = k.FoldSign >= 0 ? 1 : -1;

            var midPt = tg.CreatePoint(InventorUnits.Cm(midX), InventorUnits.Cm(midY), 0.0);
            var nAxis = tg.CreateUnitVector(nx, ny, 0.0);
            var zAxis = tg.CreateUnitVector(0.0, 0.0, 1.0);

            // ── 1. Tiết diện gấp → body riêng ──
            WorkPlane secPlane = partDef.WorkPlanes.AddFixed(midPt, nAxis, zAxis, true);
            secPlane.Visible = false;
            var secSketch = partDef.Sketches.Add(secPlane);
            secSketch.Name = "LipBendSection";

            Profile secProf;
            try { secProf = BuildBendSection(secSketch, tg, t, Rb, lipW, fs); }
            catch (Exception ex2) { FileLogger.LogException(LOG, "section profile", ex2); return "sectionFail"; }

            ExtrudeFeature lipFeat;
            try
            {
                var d = partDef.Features.ExtrudeFeatures.CreateExtrudeDefinition(secProf, PartFeatureOperationEnum.kNewBodyOperation);
                d.SetDistanceExtent(InventorUnits.Cm(len), PartFeatureExtentDirectionEnum.kSymmetricExtentDirection);
                lipFeat = partDef.Features.ExtrudeFeatures.Add(d);
            }
            catch (Exception ex2) { FileLogger.LogException(LOG, "section extrude", ex2); return "extrudeFail"; }
            TrySetName(lipFeat, "LipFold");

            // ── 2. Combine (Join) — dung thứ lỗi ──
            string joinNote = "sep-body";
            try
            {
                if (partDef.SurfaceBodies.Count >= 2)
                {
                    var tools = app.TransientObjects.CreateObjectCollection();
                    tools.Add(partDef.SurfaceBodies[2]);
                    var cmb = partDef.Features.CombineFeatures.Add(partDef.SurfaceBodies[1], tools, PartFeatureOperationEnum.kJoinOperation, false);
                    try { cmb.Name = "LipCombine"; } catch { }
                    joinNote = "joined";
                }
            }
            catch (Exception ex2) { FileLogger.LogException(LOG, "combine (để 2 body)", ex2); }

            // ── 3. Sniped End 2 đầu ──
            string snipeNote = TrySnipe(app, partDef, fe, t, Rb, lipW, len, ex, ey, nx, ny, fs, midX, midY);

            return $"lip(lipW={lipW:0} Rb={Rb:0} len={len:0} {joinNote} {snipeNote})";
        }

        /// <summary>
        /// Dựng tiết diện kim loại gấp TRỰC TIẾP trên sketch (X = ⊥ free-edge ra ngoài, Y = +Z).
        /// 6 SketchPoint dùng chung + 2 cung `AddByCenterStartEndPoint` (đồng tâm, bán kính KHỚP CHÍNH XÁC)
        /// + 4 line → vòng khép kín chắc chắn. Mirror Y nếu fs&lt;0.
        /// </summary>
        private static Profile BuildBendSection(PlanarSketch sk, TransientGeometry tg, double t, double Rb, double lipW, int fs)
        {
            double S = fs;   // dấu Y
            Point2d P(double u, double w) => tg.CreatePoint2d(InventorUnits.Cm(u), InventorUnits.Cm(S * w));

            var center = P(-OvMm, t / 2.0 + Rb);
            var sp = new SketchPoint[6];
            sp[0] = sk.SketchPoints.Add(P(-OvMm, t / 2.0), false);              // tiếp tuyến mặt trên
            sp[1] = sk.SketchPoints.Add(P(Rb - OvMm, t / 2.0 + Rb), false);
            sp[2] = sk.SketchPoints.Add(P(Rb - OvMm, t / 2.0 + Rb + lipW), false);
            sp[3] = sk.SketchPoints.Add(P(Rb + t - OvMm, t / 2.0 + Rb + lipW), false);
            sp[4] = sk.SketchPoints.Add(P(Rb + t - OvMm, t / 2.0 + Rb), false);
            sp[5] = sk.SketchPoints.Add(P(-OvMm, -t / 2.0), false);             // tiếp tuyến mặt dưới

            bool innerCCW = fs > 0;
            sk.SketchArcs.AddByCenterStartEndPoint(center, sp[0], sp[1], innerCCW);   // cung TRONG (Rb)
            sk.SketchLines.AddByTwoPoints(sp[1], sp[2]);
            sk.SketchLines.AddByTwoPoints(sp[2], sp[3]);
            sk.SketchLines.AddByTwoPoints(sp[3], sp[4]);
            sk.SketchArcs.AddByCenterStartEndPoint(center, sp[4], sp[5], !innerCCW); // cung NGOÀI (Rb + t)
            sk.SketchLines.AddByTwoPoints(sp[5], sp[0]);

            return sk.Profiles.AddForSolid();
        }

        private static string TrySnipe(global::Inventor.Application app, PartComponentDefinition partDef,
            FreeEdge fe, double t, double Rb, double lipW, double len,
            double ex, double ey, double nx, double ny, int fs, double midX, double midY)
        {
            double rise = lipW - EndFlatMm;
            double run = rise * Tan60;
            if (rise <= 0.5) return "no-snipe(rise)";
            if (2.0 * run >= len - 0.5) run = (len - 1.0) / 2.0;
            if (run < 1.0) return "no-snipe(run)";

            var tg = app.TransientGeometry;
            // gốc = mép ngoài lip (mid-thk) tại giữa free-edge; X = dọc free-edge, Y = ngang lip (từ gốc lip).
            double lipMidN = Rb + t / 2.0;
            double lipRootW = (t / 2.0 + Rb) * fs;
            var origin = tg.CreatePoint(
                InventorUnits.Cm(midX) + InventorUnits.Cm(lipMidN) * nx,
                InventorUnits.Cm(midY) + InventorUnits.Cm(lipMidN) * ny,
                InventorUnits.Cm(lipRootW));
            var eAxis = tg.CreateUnitVector(ex, ey, 0.0);
            var wAxis = tg.CreateUnitVector(0.0, 0.0, fs);   // +Y = từ gốc lip ra mép ngoài

            WorkPlane cutPlane;
            PlanarSketch sk;
            try
            {
                cutPlane = partDef.WorkPlanes.AddFixed(origin, eAxis, wAxis, true);
                cutPlane.Visible = false;
                sk = partDef.Sketches.Add(cutPlane);
                sk.Name = "LipSnipe";
            }
            catch (Exception ex2) { FileLogger.LogException(LOG, "snipe plane", ex2); return "snipeFail(plane)"; }

            double half = len / 2.0;
            Tri(sk, tg, -half, -half + run, lipW);   // đầu A (length=0)
            Tri(sk, tg, half, half - run, lipW);      // đầu B (length=len)

            try
            {
                var prof = sk.Profiles.AddForSolid();
                var d = partDef.Features.ExtrudeFeatures.CreateExtrudeDefinition(prof, PartFeatureOperationEnum.kCutOperation);
                d.SetDistanceExtent(InventorUnits.Cm(t + 4.0), PartFeatureExtentDirectionEnum.kSymmetricExtentDirection);
                TrySetName(partDef.Features.ExtrudeFeatures.Add(d), "LipSnipe");
                return "sniped";
            }
            catch (Exception ex2) { FileLogger.LogException(LOG, "snipe cut", ex2); return "snipeFail(cut)"; }
        }

        private static void Tri(PlanarSketch sk, TransientGeometry tg, double xEnd, double xInner, double lipW)
        {
            var a = sk.SketchPoints.Add(tg.CreatePoint2d(InventorUnits.Cm(xEnd), InventorUnits.Cm(EndFlatMm)), false);
            var b = sk.SketchPoints.Add(tg.CreatePoint2d(InventorUnits.Cm(xEnd), InventorUnits.Cm(lipW)), false);
            var d = sk.SketchPoints.Add(tg.CreatePoint2d(InventorUnits.Cm(xInner), InventorUnits.Cm(lipW)), false);
            sk.SketchLines.AddByTwoPoints(a, b);
            sk.SketchLines.AddByTwoPoints(b, d);
            sk.SketchLines.AddByTwoPoints(d, a);
        }

        private static (double, double) Centroid(Profile2D o)
        {
            double x = 0, y = 0;
            foreach (var v in o.Vertices) { x += v.X; y += v.Y; }
            int n = Math.Max(1, o.Count);
            return (x / n, y / n);
        }

        private static void TrySetName(ExtrudeFeature f, string name)
        {
            try { f.Name = name; } catch { }
        }
    }
}
