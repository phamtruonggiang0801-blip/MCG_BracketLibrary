# CONTEXT.md — MCG_BracketLibrary

## Mục tiêu

Add-in Inventor: **thư viện bracket tham số**. Khác `MCG_3DPanel` (tự nhận diện bracket từ bản vẽ
panel 2D rồi extrude), ở đây người dùng **chủ động**: chọn loại → pick vị trí → chỉnh Parameter → chèn.

Kịch bản dùng: kỹ sư đang dựng assembly kết cấu trong Inventor, tới chỗ cần 1 bracket knee giữa
Web và HP → mở palette, chọn OB, pick mặt Web + cạnh ra phía HP, sửa `Member_Height` = 120,
`Flange_Overhang` = 90 → Chèn. Bracket ra đúng hình knee (toe R30, gấp lip nếu free-edge ≥ ngưỡng),
đặt đúng vị trí, và **các thông số là Inventor Parameter thật** → sửa tiếp trong hộp thoại Parameters.

## Nguồn gốc

- **Ribbon/DockableWindow**: copy pattern từ `MCG_CheckListInventor` (`G:\My Drive\4. Code\MCG\Inventor\CheckList`).
- **Hình học bracket**: port từ `MCG_3DPanel` (`C:\Users\truonph\Desktop\MCG\AutoCAD\MCG_3DPanel`):
  - `Services/ProfileService.cs` → `Geometry/KneeProfileGenerator.cs`
  - `Services/Builders/BracketBuilder.cs` (`Solve`/`KneeDtop`) → `Geometry/KneeParameterSolver.cs`
  - Skill: `bracket-identification`, `parametric-profiles`, `sniped-end`.

## Kiến trúc (chi tiết: docs/ARCHITECTURE.md)

```
Pick (WebFaceLocationPicker)  ─┐
                               ├─► BracketInsertionService (điều phối)
Thông số (BracketParameters) ──┤        │
                               │        ├─ KneeParameterSolver.Solve  → KneeSolution
Loại (BracketCatalog)  ────────┘        ├─ KneeProfileGenerator.Generate → Profile2D (mm, bulge)
                                        ├─ SketchProfileBuilder → PlanarSketch + Profile
                                        ├─ [Assembly] BracketPartFactory → .ipt  → ComponentPlacer
                                        └─ [Part]     BracketFeatureBuilder → NonParametricBaseFeature
```

## Phases

- **Phase 0 — Scaffold** ✅ *(bản demo này)*: cấu trúc project, catalog, port solver + generator knee,
  cầu nối Inventor (sketch/extrude/place/feature), palette WPF, luồng pick→param→insert.
  Chưa test trong Inventor thật.
- **Phase 1 — Chạy được trong Inventor**: build, load add-in, verify sketch từ bulge dựng đúng arc,
  extrude ra solid, place occurrence đúng transform. Sửa lỗi interop (FaceProxy context, AddForSolid hở…).
- **Phase 2 — Lip fold hoàn chỉnh**: fillet gấp (Rb trong / Rb+thk ngoài) + sniped-end 2 đầu lip.
  Port nốt `BracketBuilder.TryAddBendLip` + `SnipLipEnds`.
- **Phase 3 — Parametric rebuild**: sửa Parameter trong Inventor → nút "Rebuild" đọc lại
  `BracketParameterBinder.Read` → dựng lại. Part-context: thay `NonParametricBaseFeature` bằng
  sketch `AddWithOrientation` để feature parametric đầy đủ.
- **Phase 4 — Mở rộng loại**: GirderEnd (GE1–6), HP-End Bracket "B", Collar Plate — mỗi loại 1
  generator, port từ `GeProfileService` / `HpEndBracketBuilder` / `WebCollarPlateBuilder`.

## Backlog / rủi ro đã biết

- FaceProxy trong assembly: `face.Geometry` có thể ở hệ toạ độ occurrence, chưa transform về assembly.
- Bulge → SketchArc: dùng 3-điểm; cần verify sagitta/hướng với cung R30 lõm thật.
- `Documents.Add(..., "", true)` phụ thuộc template mặc định của máy (đơn vị, standard).
- IB classification (bụng/lưng HP) — bên `MCG_3DPanel` vẫn chưa chốt; ở đây user tự chọn IB vs OB.
