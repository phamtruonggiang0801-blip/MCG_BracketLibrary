using System;
using Inventor;
using BracketLibraryInventorPlugin.Models;
using BracketLibraryInventorPlugin.Utilities;

namespace BracketLibraryInventorPlugin.Interop
{
    /// <summary>
    /// Pick vị trí chèn: (1) MẶT phẳng Web → mặt phẳng tấm bracket + pháp tuyến;
    /// (2) CẠNH thẳng tham chiếu → hướng member (điểm đầu cạnh = gốc bracket, hướng cạnh = ra phía HP/mép).
    /// Suy ra <see cref="InsertLocation"/> (mm). up = n × memberDir, lật để hướng ~ +Z thế giới.
    /// </summary>
    internal sealed class WebFaceLocationPicker
    {
        private const string LOG = "[WebFaceLocationPicker]";
        private readonly global::Inventor.Application _app;

        public WebFaceLocationPicker(global::Inventor.Application app) => _app = app;

        public InsertLocation Pick(bool flipMemberDir)
        {
            var doc = _app.ActiveDocument;
            HostContextKind ctx;
            if (doc is AssemblyDocument) ctx = HostContextKind.Assembly;
            else if (doc is PartDocument) ctx = HostContextKind.Part;
            else { ShowPrompt("Mở 1 Part hoặc Assembly trước khi pick vị trí."); return null; }

            var face = _app.CommandManager.Pick(
                SelectionFilterEnum.kPartFacePlanarFilter,
                "Chọn MẶT phẳng Web (tấm bracket sẽ nằm trên mặt phẳng này)") as Face;
            if (face == null) return null;

            var edge = _app.CommandManager.Pick(
                SelectionFilterEnum.kPartEdgeLinearFilter,
                "Chọn CẠNH tham chiếu: điểm đầu = gốc bracket, hướng cạnh = ra phía HP/mép") as Edge;
            if (edge == null) return null;

            var tg = _app.TransientGeometry;
            var plane = (Plane)face.Geometry;
            var line = (LineSegment)edge.Geometry;

            Vector n = plane.Normal.AsVector();
            Vector d = line.Direction.AsVector();
            Point a = line.StartPoint;   // cm
            if (flipMemberDir) { d.ScaleBy(-1); a = line.EndPoint; }

            // memberDir = d chiếu lên mặt phẳng Web.
            var member = d.Copy();
            var nProj = n.Copy(); nProj.ScaleBy(d.DotProduct(n));
            member.SubtractVector(nProj);
            if (member.Length < 1e-6) { ShowPrompt("Cạnh gần vuông góc mặt Web — chọn cạnh khác."); return null; }
            member.Normalize();

            // up = n × member, lật để +Z.
            var up = n.CrossProduct(member);
            up.Normalize();
            if (up.Z < 0) { up.ScaleBy(-1); n.ScaleBy(-1); }

            var loc = new InsertLocation
            {
                Context = ctx,
                Anchor = new[] { InventorUnits.Mm(a.X), InventorUnits.Mm(a.Y), InventorUnits.Mm(a.Z) },
                MemberDir = new[] { member.X, member.Y, member.Z },
                UpDir = new[] { up.X, up.Y, up.Z },
                PlaneNormal = new[] { n.X, n.Y, n.Z },
                MeasuredWebHeightMm = MeasureAlong(face, up),
                Label = $"{(ctx == HostContextKind.Assembly ? "ASM" : "PART")}: face + edge @ ({InventorUnits.Mm(a.X):0},{InventorUnits.Mm(a.Y):0},{InventorUnits.Mm(a.Z):0})"
            };
            FileLogger.Log(LOG, loc.Label);
            return loc;
        }

        /// <summary>Khoảng trải của mặt theo hướng <paramref name="dir"/> (mm) — gợi ý Web_Height.</summary>
        private static double? MeasureAlong(Face face, Vector dir)
        {
            try
            {
                double lo = double.MaxValue, hi = double.MinValue;
                foreach (Vertex v in face.Vertices)
                {
                    var pt = v.Point;
                    double t = pt.X * dir.X + pt.Y * dir.Y + pt.Z * dir.Z;
                    lo = Math.Min(lo, t); hi = Math.Max(hi, t);
                }
                if (hi <= lo) return null;
                return InventorUnits.Mm(hi - lo);
            }
            catch { return null; }
        }

        private void ShowPrompt(string msg)
        {
            try { _app.StatusBarText = msg; } catch { }
        }
    }
}
