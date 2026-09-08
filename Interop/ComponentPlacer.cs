using Inventor;
using BracketLibraryInventorPlugin.Models;
using BracketLibraryInventorPlugin.Utilities;

namespace BracketLibraryInventorPlugin.Interop
{
    /// <summary>
    /// Chèn file .ipt bracket vào ASSEMBLY đang mở tại transform của <see cref="InsertLocation"/>.
    /// Tên occurrence = mã bracket + số thứ tự (Inventor tự tăng).
    /// </summary>
    internal static class ComponentPlacer
    {
        private const string LOG = "[ComponentPlacer]";

        public static ComponentOccurrence Place(
            global::Inventor.Application app, AssemblyDocument asm, string iptPath, InsertLocation loc, string code)
        {
            var asmDef = asm.ComponentDefinition;
            var matrix = LocationMatrix.Build(app, loc);
            var occ = asmDef.Occurrences.Add(iptPath, matrix);
            try { occ.Name = $"{code}:1"; } catch { }
            FileLogger.Log(LOG, $"Placed {code} <- {iptPath} at anchor=({loc.Anchor[0]:0},{loc.Anchor[1]:0},{loc.Anchor[2]:0})");
            return occ;
        }
    }
}
