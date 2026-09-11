# Test & chạy tool trong Inventor

## 0. Yêu cầu

- Autodesk **Inventor 2023** (`C:\Program Files\Autodesk\Inventor 2023`).
- **.NET SDK** (build bằng `dotnet build`) — máy này đã có .NET 10 SDK, build `net48` OK.
- Nếu Inventor cài ở phiên bản/đường dẫn khác → sửa `<InventorPath>` trong `MCG_BracketLibrary.csproj`
  và thư mục Addins trong `Install_AutoLoadInventorAddin.bat` (`Inventor 2023` → `Inventor 2026`…).

## 1. Build

```powershell
cd C:\Users\truonph\Desktop\MCG\Inventor\MCG_BracketLibrary
dotnet build -c Debug
```

Kết quả mong đợi: `Build succeeded. 0 Error(s)`.
MSBuild tự chạy 2 target sau build:
- `bin\Debug\` ← DLL + `.addin`
- `C:\CustomTools\Inventor\MCG_BracketLibrary\` ← DLL + `.addin` (theo chuẩn MCGVN)

## 2. Cài add-in (chỉ làm 1 lần)

```powershell
C:\Users\truonph\Desktop\MCG\Inventor\MCG_BracketLibrary\Install_AutoLoadInventorAddin.bat
```

`xcopy` toàn bộ `C:\CustomTools\Inventor\MCG_BracketLibrary\*` →
`%APPDATA%\Autodesk\Inventor 2023\Addins\MCG_BracketLibrary\`
(giống cách `MCG_CheckListInventor` deploy — `.addin` trỏ DLL cùng folder).

> Lần sau chỉ cần `dotnet build` rồi chạy lại `.bat` để cập nhật DLL (hoặc copy tay
> `bin\Debug\MCG_BracketLibrary.dll` đè vào folder Addins).

## 3. Nạp trong Inventor

1. **Đóng hẳn Inventor** rồi mở lại (add-in `LoadOnStartUp=1`, chỉ nạp lúc khởi động).
2. Kiểm tra: **Tools ▸ Add-Ins** → tab "Add-Ins" → thấy **"MCG Bracket Library"**, Load Behavior = *Loaded*.
   - Nếu *Load Failed*: xem `C:\CustomTools\Inventor\logs\MCG_BracketLibrary.log`.
3. Mở 1 Part hoặc Assembly → ribbon **MCG TOOLS** → panel **Model** → nút **Bracket Library**.
   Click → hiện DockableWindow bên phải.

## 4. Kịch bản test tối thiểu (assembly)

1. Tạo assembly mới, chèn 1 part có 1 mặt phẳng đứng lớn (giả lập Web) — hoặc mở 1 assembly kết cấu thật.
2. Mở palette Bracket Library.
3. **1 · Loại** → chọn `OB — Outer Bracket (t6)`.
4. **2 · Vị trí** → bấm **Pick vị trí** (5 bước, prompt hiện trên status bar):
   - `1/5` Click 1 **mặt phẳng** (Web).
   - `2/5` Click 1 **cạnh thẳng** (Web ∩ TopPlate) — điểm ĐẦU = gốc, hướng = ra phía HP/mép.
   - `3/5`–`5/5` **tuỳ chọn** — mặt Flange / cạnh HP∩TopPlate / mặt HP. Nhấn **Esc** để bỏ qua từng cái.
   - (hướng ngược ý → tick "Đảo hướng member" rồi Pick lại)
5. **3 · Thông số** → nếu đã pick đủ mặt/cạnh ở bước 4, các ô `Span_S` / `Web_Height` /
   `Flange_Overhang` / `Member_Height` **tự điền** (dòng xanh "Đã đo từ hình học…"). Kiểm lại,
   sửa tay các ô còn thiếu. Xem khối **4 · Derived**: `Huse`, `F`, `Dtop`, `free-edge`, `flanged`.
   - Bỏ tick "Tự đo tham số từ hình học pick" nếu muốn nhập tay toàn bộ như trước.
6. Chọn radio **Assembly (component)** → bấm **CHÈN BRACKET**.
7. Kết quả: 1 component `.ipt` mới tên `OB` xuất hiện trong assembly, tại vị trí đã pick.
   File lưu ở `%LOCALAPPDATA%\MCG_BracketLibrary\parts\OB_<timestamp>.ipt`.
8. Double-click component → **Manage ▸ Parameters** → thấy 6 User Parameter (`Span_S`, `Web_Height`…).

### Part context

Mở 1 Part, làm bước 3–6 nhưng chọn radio **Part (feature)** → bracket thêm vào part đang mở dưới
dạng 1 base feature (`OB_bracket`).

## 5. Những chỗ dễ hỏng (checklist khi test)

| Triệu chứng | Nghi ngờ | File |
|---|---|---|
| Palette trống / lỗi khi mở | ctor `BracketLibraryView` / service | `Views/BracketLibraryView.xaml.cs`, log |
| Pick không chọn được mặt/cạnh | SelectionFilter, ngữ cảnh doc | `Interop/BracketLocationPicker.cs` |
| Bracket đặt sai vị trí / xoay lệch | matrix từ face/edge; FaceProxy trong assembly chưa transform | `Interop/LocationMatrix.cs`, `BracketLocationPicker.cs` |
| Auto-đo tham số sai (Span_S/Web_Height/Flange_Overhang/Member_Height) | `.Geometry` proxy chưa ở hệ assembly; đo sai dấu/hướng | `Interop/GeometryMeasure.cs` |
| Extrude lỗi "profile hở" | bulge → arc 3 điểm, đỉnh không khít | `Interop/SketchProfileBuilder.cs` |
| Cung R30 phình ra thay vì lõm | dấu bulge / hướng sagitta | `SketchProfileBuilder.ArcMidPoint` |
| Lip sai (góc vuông, không fillet) | **đã biết** — bản demo chưa làm fillet + sniped-end | `Interop/LipFoldBuilder.cs` |
| `.ipt` sai đơn vị | template mặc định của máy | `BracketPartFactory` (`Documents.Add(...,"",true)`) |

## 6. Debug

- Log runtime: `%APPDATA%\MCG_BracketLibrary\plugin.log` (`FileLogger`).
- Log lỗi Activate/Deactivate: `C:\CustomTools\Inventor\logs\MCG_BracketLibrary.log` (`AddinLogger`).
- `Debug.WriteLine` → DebugView (Sysinternals) hoặc VS "Attach to Process" → `Inventor.exe`.
- Verify hình học **ngoài Inventor**: viết console app nhỏ gọi `KneeParameterSolver.Solve` +
  `KneeProfileGenerator.Generate`, in outline, so với block `OB_type1` trong skill `parametric-profiles`.
