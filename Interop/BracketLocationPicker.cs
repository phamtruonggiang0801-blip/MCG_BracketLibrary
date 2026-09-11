using System;
using Inventor;
using BracketLibraryInventorPlugin.Models;
using BracketLibraryInventorPlugin.Utilities;

namespace BracketLibraryInventorPlugin.Interop
{
    /// <summary>
    /// Pick bracket location — CONFIG 2: bracket plate is PERPENDICULAR to the Web face, spanning
    /// from the Web to a parallel HP.
    ///
    ///  1. WEB face       — <c>member</c> = Web normal (toward HP).
    ///  2. Click a point on the TOP PLATE face — bracket station; <c>up</c> = TopPlate normal (away from Flange).
    ///  3. HP face        — Span_S = Web↔HP plane distance; Member_Height.
    ///  4. FLANGE face    — Web_Height (top ↔ flange); Flange_Overhang.
    ///
    /// Local frame: +X = <c>member</c> · +Y = <c>up</c> · +Z = <c>PlaneNormal</c> = member × up.
    /// </summary>
    internal sealed class BracketLocationPicker
    {
        private const string LOG = "[BracketLocationPicker]";
        private readonly global::Inventor.Application _app;
        private readonly TransientGeometry _tg;
        private readonly PointOnEntityPicker _pointPicker;

        public BracketLocationPicker(global::Inventor.Application app)
        {
            _app = app;
            _tg = app.TransientGeometry;
            _pointPicker = new PointOnEntityPicker(app);
        }

        public InsertLocation Pick(bool flipMemberDir)
        {
            var doc = _app.ActiveDocument;
            HostContextKind ctx;
            if (doc is AssemblyDocument) ctx = HostContextKind.Assembly;
            else if (doc is PartDocument) ctx = HostContextKind.Part;
            else { ShowPrompt("Mở 1 Part hoặc Assembly trước khi pick vị trí."); return null; }

            FileLogger.Log(LOG, $"───── PICK (config-2, ctx={ctx}, flip={flipMemberDir}) ─────");

            var webFace = PickFace("1/4 · Pick the WEB face");
            if (webFace == null) { FileLogger.Log(LOG, "Cancelled: Web face."); return null; }
            FileLogger.Log(LOG, "[1] WEB: " + GeometryMeasure.DescribePlaneRaw(webFace));
            var (webRoot, webNormal) = GeometryMeasure.PlaneOf(webFace);

            var p2 = _pointPicker.Pick("2/4 · Click a point on the TOP PLATE face — bracket station",
                                       SelectionFilterEnum.kPartFacePlanarFilter);
            if (p2 == null) { FileLogger.Log(LOG, "Cancelled: point."); return null; }
            var topFace = p2.Entity as Face;
            if (topFace == null)
            {
                FileLogger.Log(LOG, "[2] entity is not a Face: " + (p2.Entity?.GetType().Name ?? "null"));
                ShowPrompt("Click on a planar TOP PLATE face.");
                return null;
            }
            Point clickPt = p2.Point;
            var (topRoot, topNormal) = GeometryMeasure.PlaneOf(topFace);
            FileLogger.Log(LOG, $"[2] click=({InventorUnits.Mm(clickPt.X):0},{InventorUnits.Mm(clickPt.Y):0},{InventorUnits.Mm(clickPt.Z):0}) " +
                                $"topNormal=({topNormal.X:0.00},{topNormal.Y:0.00},{topNormal.Z:0.00})");

            var hpFace = PickFace("3/4 · Pick the HP face");
            if (hpFace == null) { FileLogger.Log(LOG, "Cancelled: HP face."); return null; }
            var (hpRoot, _) = GeometryMeasure.PlaneOf(hpFace);

            var flangeFace = PickFace("4/4 · Pick the FLANGE face");
            if (flangeFace == null) { FileLogger.Log(LOG, "Cancelled: Flange face."); return null; }
            var (flangeRoot, _) = GeometryMeasure.PlaneOf(flangeFace);

            // ─── FRAME ───
            Vector member = webNormal.Copy();
            if (Dot(hpRoot, webRoot, webNormal) < 0.0) member.ScaleBy(-1.0);   // toward the HP plane
            if (flipMemberDir) member.ScaleBy(-1.0);
            member.Normalize();

            Vector up = topNormal.Copy();
            if (Dot(flangeRoot, topRoot, up) > 0.0) up.ScaleBy(-1.0);          // topNormal → flange side → flip
            double proj = up.X * member.X + up.Y * member.Y + up.Z * member.Z;
            up = _tg.CreateVector(up.X - proj * member.X, up.Y - proj * member.Y, up.Z - proj * member.Z);
            if (up.Length < 1e-6) { ShowPrompt("Mặt Top Plate không vuông góc mặt Web — chọn lại."); return null; }
            up.Normalize();

            Vector pn = member.CrossProduct(up);
            pn.Normalize();

            Point a = ProjectToPlane(clickPt, webRoot, webNormal);
            a = ProjectToPlane(a, topRoot, topNormal);

            var loc = new InsertLocation
            {
                Context = ctx,
                Anchor = new[] { InventorUnits.Mm(a.X), InventorUnits.Mm(a.Y), InventorUnits.Mm(a.Z) },
                MemberDir = new[] { member.X, member.Y, member.Z },
                UpDir = new[] { up.X, up.Y, up.Z },
                PlaneNormal = new[] { pn.X, pn.Y, pn.Z },
                MeasuredSpanMm = GeometryMeasure.DistancePointToPlane(hpRoot, webRoot, webNormal),
                MeasuredMemberHeightMm = GeometryMeasure.FaceSpanAlong(hpFace, up),
                MeasuredWebHeightMm = GeometryMeasure.DistancePointToPlane(a, flangeRoot, up),
                MeasuredFlangeOverhangMm = GeometryMeasure.MaxReachFromPoint(flangeFace, webRoot, member),
                Label = $"{(ctx == HostContextKind.Assembly ? "ASM" : "PART")}: @ ({InventorUnits.Mm(a.X):0},{InventorUnits.Mm(a.Y):0},{InventorUnits.Mm(a.Z):0})"
            };
            FileLogger.Log(LOG, $"[frame] anchor(mm)=({loc.Anchor[0]:0},{loc.Anchor[1]:0},{loc.Anchor[2]:0}) " +
                                $"member=({member.X:0.00},{member.Y:0.00},{member.Z:0.00}) up=({up.X:0.00},{up.Y:0.00},{up.Z:0.00})");
            FileLogger.Log(LOG, "[3] HP: " + GeometryMeasure.DescribeFaceExtent(hpFace, a, member, up, webNormal));
            FileLogger.Log(LOG, "[4] FLANGE: " + GeometryMeasure.DescribeFaceExtent(flangeFace, a, member, up, webNormal));
            FileLogger.Log(LOG, $"───── RESULT: span={Fmt(loc.MeasuredSpanMm)} webH={Fmt(loc.MeasuredWebHeightMm)} " +
                                $"overhang={Fmt(loc.MeasuredFlangeOverhangMm)} memberH={Fmt(loc.MeasuredMemberHeightMm)} ─────");
            return loc;
        }

        private static double Dot(Point p, Point from, Vector dir) =>
            (p.X - from.X) * dir.X + (p.Y - from.Y) * dir.Y + (p.Z - from.Z) * dir.Z;

        private Point ProjectToPlane(Point p, Point planeRoot, Vector unitNormal)
        {
            double d = Dot(p, planeRoot, unitNormal);
            return _tg.CreatePoint(p.X - d * unitNormal.X, p.Y - d * unitNormal.Y, p.Z - d * unitNormal.Z);
        }

        private Face PickFace(string prompt) =>
            _app.CommandManager.Pick(SelectionFilterEnum.kPartFacePlanarFilter, prompt) as Face;

        private void ShowPrompt(string msg) { try { _app.StatusBarText = msg; } catch { } }

        private static string Fmt(double? v) => v is double x ? x.ToString("0") : "—";
    }
}
