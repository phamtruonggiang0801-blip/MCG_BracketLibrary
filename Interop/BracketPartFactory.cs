using System;
using System.IO;
using Inventor;
using BracketLibraryInventorPlugin.Geometry;
using BracketLibraryInventorPlugin.Models;
using BracketLibraryInventorPlugin.Utilities;

namespace BracketLibraryInventorPlugin.Interop
{
    public sealed class BracketPartResult
    {
        public PartDocument Document;
        public string FilePath;
        public KneeSolution Solution;
        public Profile2D Outline;
    }

    /// <summary>
    /// Dựng hình học 1 bracket knee vào 1 PartDocument: User Parameters (mm) → Sketch (XY) →
    /// Extrude đối xứng theo chiều dày → (nếu flanged) lip → iProperties Part Number = mã.
    ///
    ///  - <see cref="Create"/>        : tạo doc mới + Save .ipt (dùng cho ngữ cảnh Assembly).
    ///  - <see cref="CreateUnsaved"/> : tạo doc mới KHÔNG save (dùng làm nguồn transient BRep cho part-context).
    /// </summary>
    public sealed class BracketPartFactory
    {
        private const string LOG = "[BracketPartFactory]";
        private readonly global::Inventor.Application _app;
        private readonly ProfileGeneratorRegistry _generators;

        public BracketPartFactory(global::Inventor.Application app, ProfileGeneratorRegistry generators)
        {
            _app = app;
            _generators = generators;
        }

        public BracketPartResult CreateUnsaved(BracketDefinition def, BracketParameters p)
        {
            var tg = _app.TransientGeometry;
            var k = KneeParameterSolver.Solve(def, p);
            var outline = _generators.For(def.Family).Generate(k);
            FileLogger.Log(LOG, $"{def.PartCode}: {k}");
            FileLogger.Log(LOG, $"{def.PartCode}: {outline}");

            var partDoc = (PartDocument)_app.Documents.Add(
                DocumentTypeEnum.kPartDocumentObject, "", true);

            partDoc.UnitsOfMeasure.LengthUnits = UnitsTypeEnum.kMillimeterLengthUnits;
            var partDef = partDoc.ComponentDefinition;

            BracketParameterBinder.Write(partDef, p);

            var xyPlane = partDef.WorkPlanes[3]; // XY
            var sketch = partDef.Sketches.Add(xyPlane);
            sketch.Name = "BracketProfile";
            var profile = SketchProfileBuilder.Build(sketch, outline, tg);

            var extDef = partDef.Features.ExtrudeFeatures.CreateExtrudeDefinition(
                profile, PartFeatureOperationEnum.kNewBodyOperation);
            extDef.SetDistanceExtent(
                InventorUnits.Cm(k.Thk), PartFeatureExtentDirectionEnum.kSymmetricExtentDirection);
            var ext = partDef.Features.ExtrudeFeatures.Add(extDef);
            ext.Name = "Plate";

            if (k.Flanged)
            {
                try
                {
                    string note = LipFoldBuilder.TryAdd(_app, partDef, outline, outline.FreeEdge, k);
                    FileLogger.Log(LOG, $"{def.PartCode}: {note}");
                }
                catch (Exception ex) { FileLogger.LogException(LOG, "lip fold", ex); }
            }

            StampProperties(partDoc, def, k);

            return new BracketPartResult { Document = partDoc, Solution = k, Outline = outline };
        }

        public BracketPartResult Create(BracketDefinition def, BracketParameters p, string libraryDir)
        {
            var r = CreateUnsaved(def, p);
            try
            {
                System.IO.Directory.CreateDirectory(libraryDir);
                r.FilePath = System.IO.Path.Combine(libraryDir, $"{def.PartCode}_{DateTime.Now:yyyyMMdd_HHmmss}.ipt");
                r.Document.SaveAs(r.FilePath, false);
                return r;
            }
            catch
            {
                try { r.Document.Close(true); } catch { }
                throw;
            }
        }

        private static void StampProperties(PartDocument doc, BracketDefinition def, KneeSolution k)
        {
            try
            {
                var sets = doc.PropertySets;
                var design = sets["Design Tracking Properties"];
                design["Part Number"].Value = def.PartCode;
                design["Description"].Value =
                    $"{def.DisplayName} | S={k.S:0} Huse={k.Huse:0} F={k.F:0} thk={k.Thk:0} flanged={k.Flanged}";
                sets["Inventor Summary Information"]["Title"].Value = def.PartCode;
            }
            catch (Exception ex) { FileLogger.LogException(LOG, "iProperties", ex); }
        }
    }
}
