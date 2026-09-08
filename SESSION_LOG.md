# SESSION LOG — MCG_BracketLibrary

## Session 2026-09-08 — Scaffold bản demo (Phase 0)

### Đã làm
- Khởi tạo repo `C:\Users\truonph\Desktop\MCG\Inventor\MCG_BracketLibrary`.
- `MCG_BracketLibrary.csproj` / `.addin` / `Install_AutoLoadInventorAddin.bat` — GUID `{7f3a9c21-...a7}`.
- `StandardAddInServer.cs` + `Descriptors/BracketLibraryToolDescriptor.cs` — nút MCG TOOLS ▸ Model
  (Part + Assembly ribbon), DockableWindow quản lý trực tiếp.
- `Core/` — copy SDK `MCG.Inventor.Ribbon` từ `MCG_CheckListInventor` (7 file, không sửa).
- `Models/` — `BracketFamily`, `BracketType`, `BracketDefinition`, `BracketParameters`,
  `ProfileVertex`, `Profile2D`, `InsertLocation`.
- `Catalog/BracketCatalog.cs` — 6 loại (OB/OB1/IB/IB1/FB/FB1) + thông số mặc định.
- `Geometry/` (toán thuần, không ref Inventor):
  - `ArcMath.cs` (Vec2 + bulge + tangent) — port từ `ProfileService`.
  - `KneeSolution.cs` + `KneeParameterSolver.cs` — port `BracketBuilder.Solve` + `KneeDtop`.
  - `KneeProfileGenerator.cs` — port `ProfileService.BracketOutline` (FlangedToe / R30Only /
    squareCorner / under-50 / TemplateUnder50 / Sharp).
  - `IBracketProfileGenerator.cs` + `ProfileGeneratorRegistry.cs`.
- `Interop/` — `InventorUnits`, `SketchProfileBuilder` (bulge→SketchArc 3 điểm),
  `BracketParameterBinder` (BracketParameters ↔ User Parameters mm), `LipFoldBuilder` (demo, chưa fillet),
  `BracketPartFactory` (Create / CreateUnsaved), `LocationMatrix`, `ComponentPlacer`,
  `BracketFeatureBuilder` (transient BRep base feature), `WebFaceLocationPicker` (pick face + edge).
- `Services/` — `IBracketInsertionService` + `BracketInsertionService` (điều phối, transaction).
- `Views/` — `BracketLibraryView.xaml(.cs)` + `BracketLibraryViewModel.cs`.
- Docs: `README.md`, `CLAUDE.md`, `CONTEXT.md`, `docs/ARCHITECTURE.md`, `docs/BRACKET-PARAMETERS.md`.

### Trạng thái
- Phase 0 (scaffold) xong. **Chưa build/test trong Inventor** — code interop viết theo API 2023,
  cần verify khi có máy có Inventor.

### Bước tiếp theo
- File: build toàn project → sửa lỗi biên dịch interop (nếu có).
- File: `Interop/WebFaceLocationPicker.cs` — xử lý FaceProxy context trong assembly.
- File: `Interop/SketchProfileBuilder.cs` — verify arc R30 lõm dựng đúng (so với block OB_type1).
- Mục tiêu: chèn thử 1 OB vào assembly rỗng, kiểm hình knee + vị trí.
