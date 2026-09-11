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
        │  2. [Pick vị trí]   → BracketLocationPicker         │
        │  3. 6 ô Parameter   → BracketParameters (mm)        │
        │     (auto-đo từ pick nếu chọn thêm Flange/HP)       │
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
| `Interop/BracketLocationPicker.cs` | multi-pick (Web+cạnh bắt buộc; Flange/HP tuỳ chọn) → InsertLocation + số đo | `BracketBuilder.AnchorDir` (thủ công thay vì auto) |
| `Interop/GeometryMeasure.cs` | đo mặt/cạnh Inventor (span, reach, k/c điểm–mặt) → mm | — (Phase 5a) |

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

---

## 10. Đề xuất Phase 5 — Pick hình học nhiều mặt/cạnh → bỏ nhập tham số

**Trạng thái:** 5a code xong 2026-09-08 (chưa verify Inventor). Đọc kèm `docs/BRACKET-PARAMETERS.md`.

### 10.1 Hình học thật — CONFIG 2 (đã chốt với user 2026-09-08)

⚠️ Scaffold Phase 0 giả định **config 1** (bracket NẰM TRONG mặt phẳng Web, vươn dọc cạnh top).
Mô hình thật của user là **config 2**:

- Web và HP là **2 tấm đứng SONG SONG**, cách nhau `Span_S` (~450).
- Tấm bracket nằm trong mặt phẳng **VUÔNG GÓC mặt Web**, bắc từ Web sang HP.
- Hệ local: `+X member` = **pháp tuyến mặt Web** (hướng về HP) · `+Y up` = **+Z thế giới** ·
  `+Z PlaneNormal` = `member × up` (dọc thân tàu, = hướng dày tấm).

| Đo | Cách (config 2) |
|---|---|
| `Span_S` | k/c vuông góc mặt phẳng Web ↔ mặt phẳng HP |
| `Web_Height` | k/c theo `up` từ gốc (mức TopPlate) ↔ mặt phẳng Flange |
| `Flange_Overhang` | đỉnh flange chìa xa nhất khỏi **mặt phẳng Web** dọc `member` (phía HP) |
| `Member_Height` | bề rộng trải mặt HP theo `up` |

### 10.2 Luồng pick (5a — đã code)

```text
PLAN (nhìn từ trên):  Web ∥ HP, cách Span_S
        ┌── click ĐIỂM trên cạnh Web∩TopPlate  (vị trí bracket dọc Y)
        v
   ═════╪═══════════ TOP PLATE ═══════════════
        │  MẶT WEB                    MẶT HP
        │  (pháp tuyến = member →)    (∥ Web)
        │  |<──────── Span_S ────────>|
      x=Xweb                       x=Xhp

ELEVATION (mặt phẳng bracket, chứa member + up):
   (gốc)·────────── Span_S ──────────·   ← Top Plate  (z = z_click)
        │╲                          (toe gần HP)
   Web_ │ ╲____
   Hgt  │ /    ╲____
        ·─┴──────────·──────────────────  ← MẶT FLANGE  (z = z_flange)
        │<── F ──>|
      MẶT FLANGE chìa Flange_Overhang khỏi mặt Web
```

| # | Pick (bắt buộc) | Suy ra |
|---|---|---|
| 1 | **Mặt Web** | `member` = pháp tuyến mặt Web |
| 2 | **Click điểm trên MẶT TOP PLATE** | vị trí bracket (Y); `up` = pháp tuyến TopPlate (lật ra xa Flange, ⊥ member); `PlaneNormal` = member × up. Bỏ giả định +Z → không cần "Lật đứng". |
| 3 | **Mặt HP** | `Span_S` = k/c mp Web↔HP; quay `member` về phía HP; `Member_Height` = trải HP theo `up` |
| 4 | **Mặt Flange** | `Web_Height` = k/c Z gốc↔Flange; `Flange_Overhang` = flange chìa khỏi mp Web dọc `member` |

4 pick, 0 ô gõ. `member` không pick HP thì không định hướng được → HP **bắt buộc**.
Điểm click (bước 2) lấy qua `PointOnEntityPicker` (`InteractionEvents`) — `CommandManager.Pick`
chỉ trả entity, không trả toạ độ.

### 10.3 Tham số còn phải nhập

| Param | Xử lý |
|---|---|
| `Member_Height` | **pick thêm mặt HP/FB** (pick E) → đo chiều sâu profile ⊥ TopPlate. Member không model → dropdown chuẩn (HP100/120/140/160/180/200… · FB…). |
| `Lip_Fold_Dir` | Hướng gập mép gia cường dọc free-edge — xem **10.3.1**. Không suy từ hình học → **nút "Lật lip"** ở preview. |
| `Plate_Thickness` | Người dùng chọn **Nhóm** (OB/IB/FB) + **Chiều dày** (dropdown 6/10) — xem **10.3.2**. |
| 6 ô số cũ | GIỮ LẠI ở vai trò **override** — auto-fill từ pick, cho sửa tay; ai không pick đủ thì gõ phần thiếu. |

#### 10.3.1 `Lip_Fold_Dir` là gì

Khi free-edge (cạnh chéo tự do) đủ dài (`flanged = true`), tool **không để cạnh trần** mà **gập một
dải mép hẹp (lip / face plate, rộng `LipWidth` ≈ 70–100 mm) vuông góc tấm, chạy dọc free-edge** — để
chống oằn cạnh tự do, giống thanh thép góc.

Dải này gập được về **một trong hai mặt** của tấm bracket:
`Lip_Fold_Dir = +1` → về phía local `+Z` · `−1` → phía `−Z`.

Chọn phía nào tuỳ bối cảnh kết cấu (phía có khoảng hở / tránh chi tiết bên cạnh / theo chiều dựng).
Bản AutoCAD `MCG_3DPanel` suy từ panel type; ở tool này không có panel nên **người dùng chọn** —
Phase 5 chuyển thành **nút "Lật lip"** trên preview 3D. Chỉ có tác dụng khi `flanged = true`.

> Cân nhắc đổi tên cho dễ hiểu: `Lip_Fold_Dir` → `Edge_Flange_Side`.

#### 10.3.2 Tách catalog: 6 type → 3 nhóm × bậc dày

- **Hiện:** `BracketType` = {OB, OB1, IB, IB1, FB, FB1} — 1 combo dài, chiều dày dính trong loại.
- **Đề xuất:** 2 combo — **Nhóm** {OB, IB, FB} + **Chiều dày**.
- **CHỐT (tạm thời):** chiều dày chỉ **2 bậc chuẩn** — không cho gõ tự do, không mở t8/t15/t20:

  | thk | ngưỡng free-edge (`BendThreshold`) | bề rộng lip (`LipWidth`) | Rb = 2·thk | hậu tố mã |
  |---|---|---|---|---|
  | **6**  | 350 | 70  | 12 | "OB" / "IB" / "BF" |
  | **10** | 600 | 100 | 20 | "OB1" / "IB1" / "BF1" |

- `BracketDefinition` bỏ thickness rời rạc → tra bảng trên theo bậc (`PartCode` / `LipWidth` /
  `BendThreshold`). `BracketType` enum có thể bỏ, chỉ giữ `BracketFamily`.
- Mở thêm bậc t15→"2", t20→"3" **chỉ khi có bản vẽ chuẩn** cho ngưỡng + bề rộng lip.

### 10.4 Bước preview + chốt hướng (bước 3 mới của UI)

Sau khi đo xong: hiện outline + cho người dùng khử nhập nhằng (Web có 2 mặt; lật lên/xuống; hướng lip).

- **v1 (5b):** vẽ outline **2D trong canvas WPF** của palette (đã có `Profile2D`) + bảng số đo được,
  kèm nút **Lật mặt / Lật member / Lật lip**. Rẻ, làm ngay.
- **v2 (5c):** **transient graphics 3D** trong viewport Inventor (`ClientGraphics` /
  `TransientBRep` + `GraphicsDataSets`); preview bám con trỏ, click để chốt (như Place Component).

### 10.5 Module bổ sung / đổi

| File | Việc | TT |
|---|---|---|
| `Interop/BracketLocationPicker.cs` | Viết lại theo **config 2**: Web + click-điểm + HP + Flange (đều bắt buộc); frame member = pháp tuyến Web về phía HP | ✅ 5a |
| `Interop/PointOnEntityPicker.cs` *(mới)* | `InteractionEvents` — bắt (điểm click + entity); nền cho preview bám con trỏ (5c) | ✅ 5a |
| `Interop/GeometryMeasure.cs` *(mới)* | Đo span mặt, reach từ mặt phẳng, k/c điểm–mặt → mm; + `Describe*` ghi log chẩn đoán | ✅ 5a |
| `Models/InsertLocation.cs` | Thêm `MeasuredSpanMm` / `MeasuredFlangeOverhangMm` / `MeasuredMemberHeightMm` | ✅ 5a |
| `Views/BracketLibraryViewModel.cs` | `PickLocation` auto-fill 4 tham số; `MeasuredText` | ✅ 5a |
| **FaceProxy transform** (§8.3) | `GeometryMeasure` hiện tin `.Geometry` của proxy đã ở hệ assembly — log 5a xác nhận OK (coords ~49090mm) | ✅ 5a |
| `Geometry/OutlineTessellator.cs` *(mới)* | `Profile2D` (bulge) → list điểm phẳng | ✅ 5b |
| `BracketLibraryViewModel.PreviewImage` (`DrawingImage`) + XAML `<Image>` | **Section preview**: bracket outline + Web/TopPlate/Flange/HP context (2 `GeometryDrawing`, Pen riêng) | ✅ 5b |
| `LipFoldBuilder.cs` | `WorkPlanes.AddFixed` + tiết diện fillet đồng tâm (`AddByCenterStartEndPoint`) + Sniped End + Combine(Join) dung thứ lỗi | ✅ 5b |
| `Interop/BracketLipEditor.cs` *(mới)* | "Flip lip on selected bracket": xoá feature sau "Plate" → đảo `Lip_Fold_Dir` → dựng lại | ✅ 5b |
| ViewModel + XAML | Tách combo **Family + Thickness**; nút "Flip lip" / "Flip lip on selected"; UI tiếng Anh | ✅ 5b |
| Pick #2 = **mặt TopPlate** (không phải cạnh) | `up` = pháp tuyến TopPlate (lật ra xa Flange) → bỏ nút "Lật đứng" | ✅ 5b |
| ~~Ghost bám con trỏ (5c)~~ | Thử `GhostDragPicker` (InteractionEvents + OverlayClientGraphics) — **user yêu cầu bỏ**. | ❌ bỏ |
| `BracketDefinition` refactor | 6 type → 3 nhóm; `PartCode/LipWidth/BendThreshold` theo bậc dày (§10.3.2) — hiện vẫn 6-entry, `Get(family, thk)` tra | ⬜ |

### 10.6 Rủi ro

- **FaceProxy trong assembly** (§8.3) — càng nhiều entity pick càng nhiều chỗ phải nhân
  `occurrence.Transformation`. Gom hết vào `GeometryMeasure`.
- **Validate quan hệ hình học** — Flange ⊥ Web? cạnh // TopPlate? coplanar? → báo lỗi rõ ràng
  ("Mặt Flange không vuông góc mặt Web — chọn lại").
- **Member không model đúng profile** → luôn giữ đường override thủ công.
- **Span_S** đo tới mặt HP ≠ độ vươn thật của bracket (thường trừ khe hở) → cho 1 ô offset.
- `Dtop`/toe span-end phụ thuộc `Member_Height` — đo sai chiều cao HP thì toe lệch.

### 10.7 Phân nhỏ

- **5a** ✅ *(2026-09-08)* — pick config-2 (Web + click-điểm + HP + Flange) + đo 4 tham số, auto-fill.
  Chèn plate OK (E_INVALIDARG session c đã hết). **Chưa verify số đo config-2 trong Inventor.**
- **5b** ✅ *(2026-09-08/09)* — section preview (bracket + context Web/TopPlate/Flange/HP), tách combo
  Family/Thickness, nút Flip lip (+ on selected), pick #2 = mặt TopPlate, UI tiếng Anh, lip fillet.
- **5c** ❌ — ghost bám con trỏ: đã thử, **user yêu cầu bỏ**.

**Còn nợ:**

- ✅ **Lip fold** (2026-09-08): `LipFoldBuilder` v5 — tiết diện gấp có fillet (`AddByCenterStartEndPoint`
  đồng tâm) → body riêng → Combine(Join) → sniped 2 đầu. Log `joined sniped`, OK.
- ✅ Verify số đo config-2: `span=474 webH=374 overhang=107 memberH=120` — đúng.
- Preview 5b (canvas 2D xong, chưa test) · nút Lật · tách combo Nhóm/Chiều dày · ghost 5c.
