using System;
using System.Collections.Generic;
using System.Linq;
using Inventor;
using BracketLibraryInventorPlugin.Catalog;
using BracketLibraryInventorPlugin.Geometry;
using BracketLibraryInventorPlugin.Models;
using BracketLibraryInventorPlugin.Utilities;

namespace BracketLibraryInventorPlugin.Interop
{
    /// <summary>
    /// Sửa lip của 1 bracket ĐÃ CHÈN: xoá các feature lip (`LipFold` / `LipCombine` / `LipSnipe` +
    /// work plane / sketch tên `Lip*`), đảo <c>Lip_Fold_Dir</c>, dựng lại lip. Nền cho "Rebuild" (Phase 3).
    /// </summary>
    internal static class BracketLipEditor
    {
        private const string LOG = "[BracketLipEditor]";

        public static string FlipOnSelected(global::Inventor.Application app, ProfileGeneratorRegistry generators)
        {
            if (!(app.ActiveDocument is AssemblyDocument asm))
                return "Mở assembly rồi chọn 1 bracket component.";
            if (asm.SelectSet.Count == 0)
                return "Chưa chọn component. Click 1 bracket trong assembly rồi bấm lại.";

            var occ = asm.SelectSet[1] as ComponentOccurrence;
            if (occ == null) return "Đối tượng chọn không phải component.";

            PartDocument part;
            PartComponentDefinition partDef;
            try
            {
                partDef = occ.Definition as PartComponentDefinition;
                part = partDef?.Document as PartDocument;
                if (part == null) return "Component không phải part (.ipt).";
            }
            catch (Exception ex) { FileLogger.LogException(LOG, "get part", ex); return "Không lấy được part của component."; }

            try
            {
                var p = BracketParameterBinder.Read(partDef);
                p.LipFoldDirection = -p.LipFoldDirection;
                BracketParameterBinder.Write(partDef, p);

                string code = ReadPartNumber(part);
                var def = BracketCatalog.All.FirstOrDefault(d => d.PartCode == code)
                          ?? BracketCatalog.Get(BracketType.OB);

                var k = KneeParameterSolver.Solve(def, p);
                if (!k.Flanged) return "Bracket này không flanged — không có lip để lật.";

                var outline = generators.For(def.Family).Generate(k);

                int removed = DeleteLipFeatures(partDef);
                try { part.Update(); } catch { }
                string note = LipFoldBuilder.TryAdd(app, partDef, outline, outline.FreeEdge, k);

                try { part.Update(); } catch { }
                part.Save();
                try { asm.Update(); } catch { }
                FileLogger.Log(LOG, $"{code}: flip lip → Lip_Fold_Dir={p.LipFoldDirection}, removed {removed}, {note}");
                return $"Đã lật lip ({code}): {note}";
            }
            catch (Exception ex)
            {
                FileLogger.LogException(LOG, "FlipOnSelected", ex);
                return "Lỗi khi lật lip: " + ex.Message;
            }
        }

        private static string ReadPartNumber(PartDocument part)
        {
            try { return (string)part.PropertySets["Design Tracking Properties"]["Part Number"].Value; }
            catch { return ""; }
        }

        /// <summary>
        /// Xoá MỌI feature tạo SAU "Plate" (= toàn bộ lip: LipFold / LipCombine / LipSnipe) + work
        /// plane / sketch mồ côi tên "Lip*". Nhiều pass — xoá con giải phóng cha. Trả số phần tử đã xoá.
        /// </summary>
        private static int DeleteLipFeatures(PartComponentDefinition partDef)
        {
            int n = 0;

            for (int pass = 0; pass < 5; pass++)
            {
                var feats = partDef.Features.Cast<PartFeature>().ToList();
                int plateIdx = feats.FindIndex(f => Nm(f) == "Plate");
                bool any = false;
                for (int i = feats.Count - 1; i > plateIdx; i--)   // plateIdx = -1 → xoá hết (an toàn: bracket chỉ có Plate + lip)
                {
                    if (plateIdx < 0 && !IsLipName(Nm(feats[i]))) continue;
                    try { feats[i].Delete(); n++; any = true; }
                    catch (Exception ex) { if (pass == 4) FileLogger.LogException(LOG, "del feat", ex); }
                }
                if (!any) break;
            }

            for (int pass = 0; pass < 3; pass++)
            {
                bool any = false;
                foreach (PlanarSketch sk in partDef.Sketches.Cast<PlanarSketch>().ToList())
                    if (IsLipName(Nm(sk)))
                        try { sk.Delete(); n++; any = true; } catch (Exception ex) { if (pass == 2) FileLogger.LogException(LOG, "del sketch", ex); }
                foreach (WorkPlane wp in partDef.WorkPlanes.Cast<WorkPlane>().ToList())
                    if (IsLipName(Nm(wp)))
                        try { wp.Delete(); n++; any = true; } catch (Exception ex) { if (pass == 2) FileLogger.LogException(LOG, "del wp", ex); }
                if (!any) break;
            }

            return n;
        }

        private static bool IsLipName(string name) => name != null && name.StartsWith("Lip");
        private static string Nm(dynamic o) { try { return (string)o.Name; } catch { return null; } }
    }
}
