using System;
using Inventor;
using BracketLibraryInventorPlugin.Models;
using BracketLibraryInventorPlugin.Utilities;

namespace BracketLibraryInventorPlugin.Interop
{
    /// <summary>
    /// Ngữ cảnh PART: thêm bracket vào chính part đang mở.
    /// Cách làm (demo): dựng hình trong 1 part scratch (BracketPartFactory.CreateUnsaved) → copy
    /// SurfaceBody qua TransientBRep → transform về <see cref="InsertLocation"/> → thêm bằng
    /// NonParametricBaseFeature. Thông số vẫn được ghi vào User Parameters của part đích để "Rebuild".
    ///
    /// ⚠️ Base feature là hình học "đông cứng" — sửa thông số = xoá feature + dựng lại
    /// (BracketInsertionService.Rebuild). Muốn feature parametric đầy đủ trong part-context cần
    /// AddWithOrientation sketch — xem docs/ARCHITECTURE.md §"Việc còn lại".
    /// </summary>
    internal sealed class BracketFeatureBuilder
    {
        private const string LOG = "[BracketFeatureBuilder]";
        private readonly global::Inventor.Application _app;
        private readonly BracketPartFactory _factory;

        public BracketFeatureBuilder(global::Inventor.Application app, BracketPartFactory factory)
        {
            _app = app;
            _factory = factory;
        }

        public NonParametricBaseFeature Build(
            PartDocument targetDoc, BracketDefinition def, BracketParameters p, InsertLocation loc)
        {
            var scratch = _factory.CreateUnsaved(def, p);
            try
            {
                var srcBody = scratch.Document.ComponentDefinition.SurfaceBodies[1];
                var brep = _app.TransientBRep;
                var copy = brep.Copy(srcBody);

                var matrix = LocationMatrix.Build(_app, loc);
                brep.Transform(copy, matrix);

                var targetDef = targetDoc.ComponentDefinition;

                // Ghi thông số vào part đích (để user chỉnh + Rebuild).
                BracketParameterBinder.Write(targetDef, p);

                var coll = _app.TransientObjects.CreateObjectCollection();
                coll.Add(copy);

                var bfDef = targetDef.Features.NonParametricBaseFeatures.CreateDefinition();
                bfDef.BRepEntities = coll;
                bfDef.OutputType = BaseFeatureOutputTypeEnum.kSolidOutputType;
                bfDef.IsAssociative = false;
                var bf = targetDef.Features.NonParametricBaseFeatures.AddByDefinition(bfDef);
                try { bf.Name = $"{def.PartCode}_bracket"; } catch { }

                FileLogger.Log(LOG, $"Base feature {def.PartCode} added to {targetDoc.DisplayName}.");
                return bf;
            }
            finally
            {
                try { scratch.Document.Close(true); } catch (Exception ex) { FileLogger.LogException(LOG, "close scratch", ex); }
            }
        }
    }
}
