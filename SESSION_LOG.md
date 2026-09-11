# SESSION LOG — MCG_BracketLibrary

## Session 2026-09-09 (g) — 5c ghost (đã bỏ) + section preview

### Đã làm
- Thử **5c ghost bám con trỏ** (`GhostDragPicker` + InteractionGraphics) → user yêu cầu **BỎ**.
  `BracketLocationPicker` về lại flow 5b: 4 face pick (Web/TopPlate/HP/Flange) + click điểm trên mặt
  TopPlate. `PickLocation(bool flip)`. Xoá `GhostDragPicker.cs`.
- **Section preview** (`BracketLibraryViewModel.PreviewImage` = `DrawingImage`): vẽ bracket outline
  (xanh, filled) + **context Web / Top Plate / Flange / HP-FB** (xám) trong 1 `DrawingGroup` (mỗi lớp
  1 `GeometryDrawing` + Pen riêng) → `<Image Stretch=Uniform>`. Context lấy từ `KneeSolution`
  (S, Huse) + `Params` (MemberHeight, FlangeOverhang). Bỏ `OutlinePoints`/`<Polygon>`.
- Build xanh, deployed. **Chưa test.**

### Bước tiếp theo
- Test: mở palette → khối "4 · Preview" thấy bracket + 4 đường context xám? Đổi Span_S/thickness → đổi theo?
- Guide HTML đã cũ (config-1 + flow cũ) — viết lại khi flow ổn định.

---

## Session 2026-09-08 (f) — Test Inventor + viết lại pick cho CONFIG 2

### Đã làm
- **Test trong Inventor** (log `plugin.log`):
  - ✅ E_INVALIDARG (session c, AddForSolid) **đã hết** — `[ComponentPlacer] Placed OB` thành công.
  - ⚠️ `LipFoldBuilder.TryAdd` lỗi `E_INVALIDARG` tại `WorkAxes.AddByTwoPoints` — bracket flanged
    chưa có lip (được catch, không chặn). Nợ lại.
  - 🐛 Đo tham số sai: log cho thấy mô hình user là **config 2** (Web ∥ HP cách 474mm, bracket ⊥ Web),
    còn code giả định config 1. `Span_S`/`Flange_Overhang` sai (đo dọc trục sai + mặt Flange/HP dài liên tục).
- **Viết lại `BracketLocationPicker` theo config 2** (đã chốt với user):
  - Pick: (1) mặt Web (2) **click điểm** trên cạnh Web∩TopPlate (3) mặt HP (4) mặt Flange — đều bắt buộc.
  - `member` = pháp tuyến mặt Web quay về phía HP; `up` = member × cạnh_dir (hướng +Z); `PlaneNormal` = member × up.
  - Đo: `Span_S` = k/c mp Web↔HP · `Web_Height` = k/c Z gốc↔Flange · `Flange_Overhang` = flange chìa
    xa nhất khỏi mp Web dọc member · `Member_Height` = trải mặt HP theo up.
- **`Interop/PointOnEntityPicker.cs`** (mới): `InteractionEvents` bắt (điểm click + entity) đồng bộ.
  Signature `SelectEvents.OnSelect(ObjectsEnumerator, SelectionDeviceEnum, Point, Point2d, View)`.
- Log chẩn đoán chi tiết từng bước trong `GeometryMeasure.Describe*` + picker.
- `dotnet build -c Debug` → **0 error, 0 warning**, deployed.

### Trạng thái
- Config-2 code xong, build xanh. **Chưa verify số đo config-2 trong Inventor.**
- FaceProxy `.Geometry` trong assembly: log xác nhận đã ở hệ assembly (coords ~49090mm) — OK.

### Bước tiếp theo
- Test config-2: restart Inventor → pick 4 bước → xem log `───── KẾT QUẢ` có ra ~Span 474 / webH 374 /
  overhang ~107 / memberH 120 không.
- Nếu OK: 5b (ghost preview + nút Lật + tách combo Nhóm/Chiều dày).

### Cập nhật sau test config-2 (user "Xong bước 1")
- ✅ Config-2 **đúng hết**: log `span=474 webH=374 overhang=107 memberH=120` → F=92, knee profile OK,
  Placed OK. `PointOnEntityPicker` (InteractionEvents) bắt điểm click chạy tốt.
- **Sửa `LipFoldBuilder`**: bỏ `WorkAxes.AddByTwoPoints` (không nhận transient Point) → dùng
  `WorkPlanes.AddFixed(origin, edgeDir, foldDir, true)` trực tiếp. → ✅ user xác nhận **lip đã hiện**.
- **+ Sniped End**: lip outline 6 đỉnh (2 đầu vát: `rise = LipWidth − 15`, `run = rise·tan60°`,
  chừa 15mm phẳng). Dựng qua `SketchProfileBuilder.Build` (tái dùng). **Chưa test sniped.**

### 5b (đợt 1) — Preview outline 2D
- `Geometry/OutlineTessellator.cs` (mới): `Profile2D` (bulge) → list `Vec2` (rải cung, quy ước phải).
- `IBracketInsertionService.Preview` trả thêm `Profile2D`; `BracketInsertionService` giữ `_generators`.
- `BracketLibraryViewModel.OutlinePoints` (`PointCollection`, Y đảo dấu); XAML: `Viewbox` + `Polygon`
  trong khối "4 · Preview + dẫn xuất". Recompute mỗi lần đổi param.
- Build xanh, deployed. **Chưa test.**

### 5b (đợt 2) — tách combo + nút Lật + UI tiếng Anh + pick TopPlate face

**Tách combo "Loại"** → **Family** (`CboFamily`: OB/IB/FB) + **Thickness** (`CboThk`: t6/t10).
`BracketCatalog.Get(family, thicknessMm)` (mới). VM: `Families`/`Thicknesses` (`FamilyChoice`/`ThkChoice`),
`SelectedFamily`/`SelectedThickness` → `ApplyType` (đổi Family = reset param; đổi Thickness = chỉ set
`PlateThickness`). `SelectedDefinition` → get-only.

**Pick #2 = TopPlate FACE thay vì cạnh Web∩TopPlate** (yêu cầu user): `PointOnEntityPicker` giờ pick
`kPartFacePlanarFilter` → click 1 điểm trên mặt TopPlate → `up` = pháp tuyến TopPlate (lật ra xa Flange,
⊥-ised với member). Bỏ giả định `+Z` → **bỏ nút "Lật đứng"**. `BracketLocationPicker` viết lại phần frame.

**Nút "Flip lip"** — `FlipLip()` đảo `Lip_Fold_Dir` cho lần chèn sau.
**Nút "Flip lip on selected bracket"** — `BracketLipEditor.FlipOnSelected` (mới): chọn bracket component
trong assembly → mở part → xoá feature/sketch/workplane tên `Lip*` → đảo `Lip_Fold_Dir` → dựng lại lip
→ Save. `LipCombine` giờ được đặt tên để xoá được.

**UI → tiếng Anh** (theo CLAUDE.md §8): XAML + `BracketLibraryViewModel` string user-facing + prompt
picker. Error/exception message giữ tiếng Việt.

Build xanh, deployed. **Chưa test.**

### Fix: flip-lip-on-selected mất snipe
User test: lip lật OK nhưng **mất sniped end**. Log: `DeleteLipFeatures` xoá không hết (`del sketch
E_INVALIDARG` — sketch bị feature consume), part còn feature lỗi → `TrySnipe.AddFixed` `E_FAIL`
(`snipeFail(plane)`). Sửa: `DeleteLipFeatures` xoá **mọi feature sau "Plate"** (không lọc tên), nhiều
pass (xoá con giải phóng cha), rồi work plane/sketch mồ côi. Thêm `part.Update()` sau khi xoá. **Chưa test.**

### Lip có FILLET — port đúng từ BracketBuilder.cs (MCG_3DPanel)
- User: "chưa có góc bo". v3 lỗi `set_Name E_FAIL` (extrude+Join feature bị sick vì Join chạm tiếp tuyến).
- Đọc **source `Services/Builders/BracketBuilder.cs` `TryAddBendLip` + `SnipLipEnds`** (không chỉ skill):
  tiết diện 6 đỉnh, 2 cung **ĐỒNG TÂM** tại `(−ov, t/2+Rb)` (trong Rb, ngoài Rb+t), tiếp tuyến 2 mặt
  tấm; mirror Y nếu `foldSign<0`.
- `LipFoldBuilder` v4: `TryAdd(app, partDef, outline, fe, k)` trả note string.
  - Tiết diện → extrude `kNewBodyOperation` (KHÔNG Join trực tiếp) → **body riêng**.
  - Thử `CombineFeatures.Add(plate, [lip], kJoinOperation)` trong try/catch — lỗi thì để 2 body.
  - `OvMm = 2.0` (AutoCAD ACIS dùng 0.5, Inventor parametric kén hơn).
  - Sniped End: cut 2 nêm tam giác trên work plane trong mặt lip, `kCutOperation`, defensive try/catch.
  - Mọi `feature.Name =` bọc try/catch (`TrySetName`).
- **v5** (sau test v4): v4 chỉ thấy Sketch, không lên 3D — `AddForSolid` lỗi trên tiết diện
  (`lines=4 arcs=2 points=8` → `AddByThreePoints` KHÔNG bond SketchPoint → vòng hở). Sửa:
  `BuildBendSection` dựng TRỰC TIẾP trên sketch — 6 `SketchPoint` dùng chung + 2 cung
  `AddByCenterStartEndPoint` (đồng tâm, R khớp chính xác) + 4 line. `Tri` snipe cũng dùng SketchPoint.
- ✅ **v5 OK** (user "Xong V5"): log `OB: lip(lipW=70 Rb=12 len=365 joined sniped)` — fillet + Combine
  1 body + sniped 2 đầu, không lỗi.

---

## Session 2026-09-08 (e) — Phase 5a: multi-pick + auto-đo tham số

### Đã làm
- **`Interop/WebFaceLocationPicker.cs` → `BracketLocationPicker.cs`** (git rm + file mới): pick 5 bước
  tuần tự — Web + cạnh Web∩TopPlate (bắt buộc), mặt Flange + cạnh HP∩TopPlate + mặt HP (tuỳ chọn,
  Esc = bỏ qua). Prompt "n/5" trên status bar.
- **`Interop/GeometryMeasure.cs`** (mới): `PlaneOf` / `LineOf` / `DistancePointToPlane` /
  `ProjectedDistance` / `FaceSpanAlong` / `MaxReachFromPoint` — trả về mm, guard < 1mm → null.
  Đo: Web_Height = k/c gốc→mặt Flange (fallback: trải mặt Web); Flange_Overhang = reach mặt Flange
  từ gốc dọc MemberDir; Span_S = |(điểm HP−gốc)·MemberDir|; Member_Height = trải mặt HP theo UpDir.
- **`Models/InsertLocation.cs`**: + `MeasuredSpanMm` / `MeasuredFlangeOverhangMm` / `MeasuredMemberHeightMm`.
- **`Views/BracketLibraryViewModel.cs`**: `PickLocation` auto-fill 4 ô số (guard min), `MeasuredText`
  ("Đã đo từ hình học: …"). `AutoWebHeightFromPick` → `AutoMeasureFromPick`.
- **`Views/BracketLibraryView.xaml`**: nút Pick + hint 5 bước; checkbox "Tự đo tham số"; dòng MeasuredText.
- `Services/BracketInsertionService.cs`: đổi tên field/kiểu picker.
- Docs: ARCHITECTURE §5/§10.5/§10.7, README, TESTING §4+§5, CONTEXT flow.
- **Log chẩn đoán** (`GeometryMeasure.Describe*` + `BracketLocationPicker` ghi từng bước):
  mặt Web (root/normal/verts), cạnh Web∩TopPlate (2 đầu mm + len), frame (gốc/member/up/webN),
  mặt Flange (định hướng: dot normal·member/up/webN → NGANG/∥Web/⊥member; trải đỉnh theo 3 trục
  so với gốc), Span_S/Web_Height/Flange_Overhang/Member_Height + công thức. → `%APPDATA%\MCG_BracketLibrary\plugin.log`.
- `dotnet build -c Debug` → **0 error, 0 warning**, đã deploy.

### Trạng thái
- Phase 5a code xong + build xanh. **CHƯA test trong Inventor** (cùng với fix session c).
- Chưa làm: preview 2D, tách combo Nhóm+Chiều dày, catalog refactor (đó là 5b).

### Bước tiếp theo
- Test trong Inventor: (1) fix AddForSolid session c; (2) 5a — verify `.Geometry` FaceProxy đã ở hệ
  assembly, Span_S/overhang đúng dấu, Member_Height mặt HP bulb.
- Nếu proxy chưa transform → nhân `ContainingOccurrence.Transformation` trong `GeometryMeasure`.

---

## Session 2026-09-08 (d) — Đề xuất Phase 5 (pick hình học) + trang hướng dẫn HTML

### Đã làm
- `docs/ARCHITECTURE.md §10` (mới): đề xuất **Phase 5 — pick nhiều mặt/cạnh → bỏ nhập tham số**.
  Pick Web + Flange + mặt HP + cạnh Web∩TopPlate (+cạnh HP∩TopPlate) → đo `Span_S` / `Web_Height` /
  `Flange_Overhang` / `Member_Height`; preview outline; người dùng chỉ chốt hướng. 6 ô gõ → 0–1.
  Kèm sơ đồ pick (plan+elevation), bảng pick→tham số, module bổ sung (`BracketLocationPicker`,
  `GeometryMeasure`, `LocationSolver`, `OutlinePreviewControl`), rủi ro, phân nhỏ 5a/5b/5c.
- `CONTEXT.md`: thêm 1 dòng Phase 5 vào danh sách phases.
- Trang hướng dẫn HTML (artifact): giải thích mặt Web / cạnh tham chiếu, hệ toạ độ suy ra, outline +
  6 tham số, quy trình 5 bước, Assembly vs Part, xử lý sự cố; thêm **mục 07** trình bày Phase 5.
  URL: https://claude.ai/code/artifact/6d5b2c3f-085e-415b-b701-90e0385e891a
- §10.3 làm rõ theo phản hồi: `Member_Height` = pick thêm mặt HP/FB (pick E); §10.3.1 giải thích
  `Lip_Fold_Dir` (phía gập mép gia cường free-edge); §10.3.2 tách catalog 6 type → Nhóm{OB/IB/FB} ×
  Chiều dày{6,10}, ngưỡng/lip-width chỉ có rule cho t6/t10.

### Trạng thái
- Chỉ tài liệu — **chưa code Phase 5**. Fix AddForSolid (session c) vẫn chờ test trong Inventor.

### Bước tiếp theo
- Test lại session (c) trong Inventor trước.
- Nếu chốt Phase 5: bắt đầu 5a (`BracketLocationPicker` multi-pick + `LocationSolver`).

---

## Session 2026-09-08 (c) — Fix lỗi chèn bracket: E_INVALIDARG tại AddForSolid

### Đã làm
- Đọc `%APPDATA%\MCG_BracketLibrary\plugin.log`: lỗi `The parameter is incorrect (0x80070057)`
  ném từ `Inventor.Profiles.AddForSolid` trong `SketchProfileBuilder.Build` (ngữ cảnh Assembly, OB).
  Solver + generator chạy OK (outline 13 đỉnh hợp lệ, không tự cắt — verify bằng script F#).
- Sửa `Interop/SketchProfileBuilder.cs`:
  1. **Profile hở** (nguyên nhân E_INVALIDARG): mỗi cạnh trước đây dựng từ `Point2d` transient rời →
     các cạnh không dùng chung `SketchPoint` → AddForSolid không thấy vòng kín. Nay chuyền
     `EndSketchPoint` cạnh trước → điểm đầu cạnh sau, đóng vòng bằng `firstStart`.
  2. **Dấu bulge đảo**: `ArcMidPoint` đặt sagitta sang TRÁI của a→b cho bulge>0. Đúng ra cung CCW
     (bulge>0, chuẩn LWPolyline) phồng sang PHẢI. Đã verify: convention mới tái tạo đúng tâm/bán kính
     R30 + Rb (script `chk5.fsx`). Trước đây → toe lồi thay vì lõm.
  3. Thêm log chẩn đoán (`lines/arcs/points`) khi AddForSolid vẫn lỗi.
- `dotnet build -c Debug` → Build succeeded, 0 error, đã deploy `C:\CustomTools\Inventor\MCG_BracketLibrary`.

### Trạng thái
- Fix xong + build xanh. **CHƯA test lại trong Inventor** (cần restart Inventor để nạp DLL mới).

### Bước tiếp theo
- Restart Inventor → chèn thử OB vào assembly → xác nhận hết E_INVALIDARG, extrude ra solid,
  cung toe R30 lõm (không lồi).
- Nếu vẫn lỗi: xem dòng log `[SketchProfileBuilder] AddForSolid — lines=.. arcs=.. points=..`.
- Kiểm tiếp checklist `docs/TESTING.md §5`: FaceProxy transform, vị trí/xoay occurrence, lip fold.

---

## Session 2026-09-08 (b) — Build xanh + deploy + TESTING.md

### Đã làm
- `dotnet build -c Debug` trên máy có Inventor 2023 + .NET 10 SDK → **Build succeeded, 0 error**.
- Sửa 6 lỗi biên dịch interop:
  - `Path` / `Environment` ambiguous với `Inventor.Path` / `Inventor.Environment`
    → fully-qualify `System.IO.Path` / `System.Environment` (`BracketInsertionService`, `BracketPartFactory`).
  - `BracketParameterBinder`: iterate `UserParameter` (không phải `Parameter`); `Convert.ToDouble(prm.Value)`;
    `up[name]` trả `UserParameter`.
- `Install_AutoLoadInventorAddin.bat` → `xcopy` cả folder (DLL + .addin) sang `%APPDATA%\...\Addins\`
  (giống MCG_CheckListInventor). Đã deploy thử.
- Thêm `docs/TESTING.md` — hướng dẫn build/cài/nạp + kịch bản test tối thiểu + checklist chỗ dễ hỏng.

### Trạng thái
- Phase 0 + build xanh. Add-in đã copy vào Addins folder — **chưa nạp/test trong Inventor**
  (Inventor đang chạy, cần restart để nạp).

### Bước tiếp theo
- Restart Inventor → verify add-in Loaded (Tools ▸ Add-Ins) → mở palette → chèn thử 1 OB.
- Sửa theo checklist `docs/TESTING.md` §5 (arc bulge, FaceProxy, lip fold).

---

## Session 2026-09-08 (a) — Scaffold bản demo (Phase 0)

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
