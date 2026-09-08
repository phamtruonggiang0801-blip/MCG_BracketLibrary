using System;
using System.IO;
using Inventor;
using BracketLibraryInventorPlugin.Geometry;
using BracketLibraryInventorPlugin.Interop;
using BracketLibraryInventorPlugin.Models;
using BracketLibraryInventorPlugin.Utilities;

namespace BracketLibraryInventorPlugin.Services
{
    /// <summary>
    /// Điều phối thuần — không chứa hình học (theo skill module-per-object của MCG_3DPanel).
    /// Hình học nằm ở Geometry/* + Interop/*.
    /// </summary>
    public sealed class BracketInsertionService : IBracketInsertionService
    {
        private const string LOG = "[BracketInsertionService]";

        private readonly global::Inventor.Application _app;
        private readonly WebFaceLocationPicker _picker;
        private readonly BracketPartFactory _factory;
        private readonly BracketFeatureBuilder _featureBuilder;

        private static readonly string LibraryDir = System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "MCG_BracketLibrary", "parts");

        public BracketInsertionService(global::Inventor.Application app)
        {
            _app = app;
            var generators = new ProfileGeneratorRegistry();
            _picker = new WebFaceLocationPicker(app);
            _factory = new BracketPartFactory(app, generators);
            _featureBuilder = new BracketFeatureBuilder(app, _factory);
        }

        public InsertLocation PickLocation(bool flipMemberDir)
        {
            try { return _picker.Pick(flipMemberDir); }
            catch (Exception ex) { FileLogger.LogException(LOG, "PickLocation", ex); return null; }
        }

        public (KneeSolution, string) Preview(BracketDefinition def, BracketParameters p)
        {
            var k = KneeParameterSolver.Solve(def, p);
            return (k, KneeParameterSolver.Validate(k));
        }

        public InsertResult Insert(BracketDefinition def, BracketParameters p, InsertLocation location)
        {
            if (location == null || !location.IsValid)
                return Fail("Chưa chọn vị trí chèn hợp lệ.");

            var doc = _app.ActiveDocument;
            Transaction tx = null;
            try
            {
                tx = _app.TransactionManager.StartTransaction((_Document)doc, $"Insert bracket {def.PartCode}");

                if (location.Context == HostContextKind.Assembly)
                {
                    if (!(doc is AssemblyDocument asm))
                        return Fail("Ngữ cảnh Assembly nhưng tài liệu đang mở không phải assembly.");

                    var r = _factory.Create(def, p, LibraryDir);
                    // .ipt bracket đã Save; đóng cửa sổ part độc lập, chỉ giữ occurrence trong assembly.
                    try { r.Document.Close(true); } catch { }

                    ComponentPlacer.Place(_app, asm, r.FilePath, location, def.PartCode);
                    tx.End();
                    return Ok(r.Solution, r.FilePath, $"Đã chèn {def.PartCode} vào assembly.");
                }
                else
                {
                    if (!(doc is PartDocument part))
                        return Fail("Ngữ cảnh Part nhưng tài liệu đang mở không phải part.");

                    var (k, _) = Preview(def, p);
                    _featureBuilder.Build(part, def, p, location);
                    tx.End();
                    return Ok(k, null, $"Đã thêm feature bracket {def.PartCode} vào part.");
                }
            }
            catch (Exception ex)
            {
                FileLogger.LogException(LOG, "Insert", ex);
                try { tx?.Abort(); } catch { }
                return Fail("Lỗi khi chèn bracket: " + ex.Message);
            }
        }

        private static InsertResult Ok(KneeSolution k, string path, string msg) =>
            new InsertResult { Success = true, Message = msg, PartFilePath = path, Solution = k };

        private static InsertResult Fail(string msg) =>
            new InsertResult { Success = false, Message = msg };
    }
}
