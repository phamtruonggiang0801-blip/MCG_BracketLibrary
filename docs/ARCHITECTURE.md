# Kiến trúc & Ý tưởng triển khai — MCG Bracket Library

Tài liệu này là **bản để review**. Nó mô tả ý tưởng, các phương án đã cân nhắc, luồng dữ liệu,
và phần việc còn lại. Code trong repo là scaffold hiện thực hoá đúng thiết kế này.

---

## 1. Bài toán

Người dùng muốn 1 **thư viện bracket** trên Inventor:

1. Chọn loại bracket mong muốn (dựa trên các skill bracket đã có: OB/OB1, IB/IB1, FB/FB1, …).
2. Chọn vị trí cần chèn.
3. Chỉnh các thông số **dạng Parameter** để phù hợp vị trí đó.

Điểm mấu chốt: **"thông số dạng Parameter"** = phải là Inventor User Parameter thật, để sau khi
chèn người dùng còn sửa tiếp bằng công cụ có sẵn của Inventor (hộp thoại Parameters, iLogic…).

---

## 2. Phương án bộ máy hình học — đã chọn: **Port công thức parametric sang Inventor API**

| Phương án | Ưu | Nhược | Kết luận |
|---|---|---|---|
| **A. Port công thức → API sinh sketch/feature** | 1 nguồn chân lý (đã calibrate panel thật ở `MCG_3DPanel`); ra Parameter thật; không phải vẽ tay template | Phải viết code interop; công thức toe/lip phức tạp | ✅ **Chọn** |
| B. Thư viện file `.ipt` mẫu + iLogic | Ít code hình học | Vẽ tay 6+ template; dễ lệch công thức skill; khó bảo trì khi rule đổi | ❌ |
| C. Content Center | Chuẩn Inventor, có UI browse sẵn | Setup nặng (library DB); khó nhét công thức toe R30/squareCorner; khó version | ❌ (cân nhắc lại ở Phase 4 nếu cần share nhiều máy) |

→ Toàn bộ toán nằm ở `Geometry/` (thuần .NET, **không** `using Inventor`), port 1-1 từ
`ProfileService` + `BracketBuilder.Solve`. `Interop/` chỉ chuyển outline (list điểm + bulge)
thành SketchLine/SketchArc rồi Extrude.

---

## 3. Luồng

```
        ┌─────────────── WPF palette (Views/) ───────────────┐
        │  1. ComboBox loại   → BracketCatalog.Get(type)      │
        │  2. [Pick vị trí]   → WebFaceLocationPicker         │
        │  3. 6 ô Parameter   → BracketParameters (mm)        │
        │  4. bảng "Derived"  ← KneeSolution (đọc)            │
        │  5. [CHÈN]                                          │
        └───────────────────────┬────────────────────────────┘
                                │  IBracketInsertionService
                                ▼
   KneeParameterSolver.Solve(def, params) ──► KneeSolution
        │   (Under50? SquareCorner? Flanged? Huse/F/Dtop/Ledge/FreeEdge)
        ▼
   KneeProfileGenerator.Generate(solution) ──► Profile2D   (mm, đỉnh + bulge, hệ local elevation)
        ▼
   SketchProfileBuilder.Build(sketch, profile)             (mm → cm; bulge → SketchArc 3 điểm)
        ▼
   ┌── ngữ cảnh ASSEMBLY ──────────────┐   ┌── ngữ cảnh PART ─────────────────────────┐
   │ BracketPartFactory.Create → .ipt  │   │ BracketFeatureBuilder:                    │
   │   + User Parameters (mm)          │   │   CreateUnsaved (part scratch)           │
   │   + Extrude đối xứng theo dày     │   │   → TransientBRep.Copy + Transform        │
   │   + LipFold (nếu flanged)         │   │   → NonParametricBaseFeature vào part đích│
   │   + iProperties Part Number = mã  │   │   + ghi User Parameters vào part đích     │
   │ ComponentPlacer.Place(occ, matrix)│   └──────────────────────────────────────────┘
   └───────────────────────────────────┘
```

Tất cả bọc trong `TransactionManager.StartTransaction` → 1 bước Undo.

---

## 4. Mô hình dữ liệu

### 4.1 Đầu vào — `BracketParameters` (mm, = Inventor User Parameter)

| Field | Parameter name | Ý nghĩa | Mặc định |
|---|---|---|---|
| `SpanS` | `Span_S` | leg-on-TopPlate = độ vươn plan | 450 |
| `WebHeight` | `Web_Height` | chiều cao Web bracket ngồi lên (auto-đo được từ mặt pick) | 374 |
| `MemberHeight` | `Member_Height` | chiều cao HP/FlatBar đầu kia → nuôi Dtop | 120 |
| `FlangeOverhang` | `Flange_Overhang` | flange vươn theo memberDir; F = overhang−15 | 90 |
| `PlateThickness` | `Plate_Thickness` | dày tấm (mặc định theo mã) | 6 / 10 |
| `LipFoldDirection` | `Lip_Fold_Dir` | ±1 hướng gấp lip (chiều đổ kết cấu) | 1 |

### 4.2 Dẫn xuất — `KneeSolution` (chỉ đọc, port `BracketBuilder.KneeParams`)

`Under50 = overhang < 50` · `rawF = overhang − 15` · `SquareCorner = !Under50 && rawF > S−30` ·
`F = Under50 ? 15 : min(rawF, S−30)` · `Huse = Under50 ? WebHeight−30 : WebHeight` ·
`Dtop/Ledge` theo nhóm (§ BRACKET-PARAMETERS) ·
`FreeEdge = √((S−F)² + (Huse−Dtop)²)` · `Flanged = !SquareCorner && FreeEdge ≥ (thk≥8 ? 600 : 350)`.

### 4.3 Vị trí — `InsertLocation` (mm, thuần số, không giữ COM)

`Anchor` (gốc = Web∩TopPlate) · `MemberDir` (+X local) · `UpDir` (+Y local) ·
`PlaneNormal` (+Z local = pháp tuyến tấm) · `Context` (Assembly/Part) · `MeasuredWebHeightMm`.

Hệ local outline: gốc (0,0) → Anchor; +X → MemberDir; −Y (outline đi xuống) → −UpDir; +Z → PlaneNormal.

---

## 5. Map module (kế thừa skill `module-per-object`)

| File | Trách nhiệm | Port từ |
|---|---|---|
| `Geometry/KneeParameterSolver.cs` | Giải Under50/SquareCorner/Flanged/Huse/F/Dtop | `BracketBuilder.Solve` + `KneeDtop` |
| `Geometry/KneeProfileGenerator.cs` | Outline elevation (FlangedToe / R30Only / squareCorner / under50) | `ProfileService.BracketOutline` |
| `Geometry/ArcMath.cs` | bulge, tiếp tuyến điểm-tới-đường-tròn | `ProfileService.ArcBulge` / `TangentPointFromExternal` |
| `Interop/SketchProfileBuilder.cs` | outline (bulge) → SketchLine/SketchArc → Profile | `GeometryUtil.RegionFrom` (khác nền) |
| `Interop/LipFoldBuilder.cs` | gấp lip dọc free-edge | `BracketBuilder.TryAddBendLip` (CHƯA đủ) |
| `Interop/BracketPartFactory.cs` | .ipt: params + sketch + extrude + lip + iProps | `BracketBuilder.Build` (phần dựng solid) |
| `Interop/ComponentPlacer.cs` | place occurrence theo matrix | `BracketBuilder.BracketPlacement` (Matrix3d) |
| `Interop/WebFaceLocationPicker.cs` | pick mặt Web + cạnh → InsertLocation | `BracketBuilder.AnchorDir` (thủ công thay vì auto) |

**Thêm nhóm bracket mới** = thêm 1 `IBracketProfileGenerator` + 1 dòng `BracketCatalog`. Không sửa nhóm khác.

---

## 6. Chia sẻ code hình học với `MCG_3DPanel` (đề xuất)

Hiện `Geometry/KneeProfileGenerator.cs` là **bản chép tay** của `ProfileService.cs`. Rủi ro lệch
khi 1 bên sửa. 2 hướng khắc phục (chọn sau khi review):

- **6a. Tách package chung** `MCG.Bracket.Geometry` (.NET Standard 2.0): chứa `ProfileService`,
  `KneeParameterSolver`, toán bulge. Cả `MCG_3DPanel` (AutoCAD) lẫn `MCG_BracketLibrary` (Inventor)
  tham chiếu. Điểm vướng: `ProfileService` hiện dùng `Autodesk.AutoCAD.Geometry.Point2d/Vector2d` →
  phải đổi sang kiểu trung tính (`Vec2` như repo này đã làm).
- **6b. Giữ 2 bản, thêm test đối chiếu**: 1 bộ golden-value (outline OB_type1/OB_type2 từ block
  explode) chạy ở cả 2 repo, fail nếu lệch > 0.01mm.

→ Khuyến nghị **6a** khi có ≥ 2 nhóm được port (knee + GE).

---

## 7. Parametric rebuild (Phase 3)

Sau khi chèn, user sửa `Span_S` = 520 trong hộp thoại Parameters của Inventor.

- **Assembly / .ipt**: thêm nút "Rebuild từ Parameters" trong palette →
  `BracketParameterBinder.Read(partDef)` → `Solve` → `Generate` → xoá sketch/extrude cũ
  (theo tên `BracketProfile` / `Plate` / `LipFold`) → dựng lại. .ipt vẫn 1 file, occurrence tự cập nhật.
- **Part**: `NonParametricBaseFeature` là hình học đông cứng → Rebuild = xoá base feature + dựng lại
  từ params mới. Muốn feature parametric đầy đủ: đổi sang `Sketches.AddWithOrientation` trên work
  plane tại InsertLocation (cần WorkAxis cho X, work point cho origin) rồi Extrude — khi đó sketch
  bị điều khiển bởi cùng bộ Parameter, không cần rebuild thủ công.

Có thể thêm iLogic rule nhúng vào .ipt để tự regen khi param đổi (gọi lại DLL qua `AddIn`).

---

## 8. Việc còn lại (ưu tiên từ trên xuống)

1. **Build + chạy trong Inventor 2023** — sửa lỗi biên dịch/interop. Chưa chạy lần nào.
2. **Lip fold hoàn chỉnh** (`LipFoldBuilder`): hiện là flange chữ nhật góc vuông. Cần:
   - Tiết diện kim loại gấp có fillet: arc trong `Rb = 2·thk`, arc ngoài `Rb + thk`, dày đều `thk`.
   - Sniped End 2 đầu lip: `rise = lipW − 15`, `run = rise·tan60°`, giữ 15mm sát mặt đầu.
   - Port `BracketBuilder.TryAddBendLip` + `SnipLipEnds` (repo `MCG_3DPanel`).
3. **FaceProxy trong assembly**: `face.Geometry` có thể ở hệ occurrence — nhân thêm
   `occurrence.Transformation` để về assembly space.
4. **Verify arc bulge**: cung R30 lõm (bulge dương, cắt vào thân) — check sagitta + hướng phồng
   với block `OB_type1` (bulge v1 ≈ +0.132, v9 ≈ +0.265 theo skill).
5. **squareCorner / under-50 flanged**: mới port công thức, chưa render kiểm mắt.
6. **Nút Rebuild** (Phase 3).
7. **Mở rộng loại** (Phase 4): GE1–6 (`GeProfileService`), HP-End Bracket "B"
   (`HpEndBracketBuilder`), Collar Plate (`WebCollarPlateBuilder`).
8. **Icon** ribbon (`Resources/icon16.png`, `icon32.png`) — hiện dùng placeholder xanh MCG.
9. **Chia sẻ code hình học** với `MCG_3DPanel` (§6).

---

## 9. Deploy

Giống `MCG_CheckListInventor`:
- `dotnet build -c Debug` → MSBuild copy DLL + `.addin` vào `C:\CustomTools\Inventor\MCG_BracketLibrary\`.
- `Install_AutoLoadInventorAddin.bat` copy `.addin` vào `%APPDATA%\Autodesk\Inventor 2023\Addins\...`.
- File `.ipt` bracket sinh ra lưu ở `%LOCALAPPDATA%\MCG_BracketLibrary\parts\<mã>_<timestamp>.ipt`
  (Phase 4: cho user chọn thư mục / theo project Vault).
