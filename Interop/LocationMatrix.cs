using Inventor;
using BracketLibraryInventorPlugin.Models;

namespace BracketLibraryInventorPlugin.Interop
{
    /// <summary>Ma trận đặt outline local (mm) → model space (cm) từ <see cref="InsertLocation"/>.</summary>
    internal static class LocationMatrix
    {
        public static Matrix Build(global::Inventor.Application app, InsertLocation loc)
        {
            var tg = app.TransientGeometry;
            var m = tg.CreateMatrix();

            var origin = tg.CreatePoint(
                InventorUnits.Cm(loc.Anchor[0]),
                InventorUnits.Cm(loc.Anchor[1]),
                InventorUnits.Cm(loc.Anchor[2]));

            var x = tg.CreateVector(loc.MemberDir[0], loc.MemberDir[1], loc.MemberDir[2]);
            var y = tg.CreateVector(loc.UpDir[0], loc.UpDir[1], loc.UpDir[2]);
            var z = tg.CreateVector(loc.PlaneNormal[0], loc.PlaneNormal[1], loc.PlaneNormal[2]);
            x.Normalize(); y.Normalize(); z.Normalize();

            m.SetCoordinateSystem(origin, x, y, z);
            return m;
        }
    }
}
