# CLAUDE.md — MCG_BracketLibrary

# Inventor 2023+ | C# | .NET Framework 4.8 | WPF (HwndSource/ElementHost) | VS Code

> File này Claude Code tự đọc mỗi khi mở project. Cả team áp dụng.

---

## 1. Khởi động phiên — đọc theo thứ tự
1. CLAUDE.md
2. CONTEXT.md
3. SESSION_LOG.md
4. docs/ARCHITECTURE.md (thiết kế + việc còn lại)

Sau khi đọc, báo cáo ngắn: đang ở Phase nào, file nào làm tiếp, vấn đề tồn đọng.

---

## 2. Thông tin dự án

```
Tên project    : MCG_BracketLibrary
Mục tiêu       : Thư viện bracket tham số cho Inventor — pick vị trí, chỉnh Parameter, chèn
Runtime        : .NET Framework 4.8, x64
UI             : WPF UserControl nhúng DockableWindow (quản lý trực tiếp, như MCG_CheckListInventor)
API DLLs       : Autodesk.Inventor.Interop (C:\Program Files\Autodesk\Inventor 2023\Bin\Public Assemblies)
Output         : Inventor Add-in (.dll + .addin)
Deploy folder  : C:\CustomTools\Inventor\MCG_BracketLibrary\
Build          : dotnet build -c Debug
```

**GUID Add-in (CỐ ĐỊNH):** `{7f3a9c21-5b84-4e16-a9d2-3c6e8f1b40a7}`

---

## 3. Namespace — BẮT BUỘC

```
BracketLibraryInventorPlugin                       ← Root (StandardAddInServer.cs)
├── .Descriptors      ← IToolDescriptor
├── .Helpers          ← AddinLogger
├── .Utilities        ← FileLogger
├── .Models           ← POCO thuần (không ref Inventor)
├── .Catalog          ← BracketCatalog
├── .Geometry         ← Toán thuần (không ref Inventor) — port từ MCG_3DPanel
├── .Interop          ← Cầu nối interop (folder Interop/ — KHÔNG đặt tên "Inventor" để tránh đụng namespace interop)
├── .Services         ← Điều phối
├── .Views            ← WPF
└── MCG.Inventor.Ribbon   ← SDK ribbon dùng chung (Core/)
```

**Quy tắc phụ thuộc:** `Models` và `Geometry` **KHÔNG** được `using Inventor;`. Chỉ `Interop/`,
`Services/`, `Views/`, `Descriptors/`, `StandardAddInServer` mới đụng interop.

---

## 4. Module hóa (kế thừa skill `module-per-object` của MCG_3DPanel)

- 1 nhóm bracket = 1 `IBracketProfileGenerator` (Geometry/) + 1 dòng trong `BracketCatalog`.
- Thêm nhóm mới (GirderEnd, HpEndBracketB, CollarPlate) **không sửa** generator nhóm khác.
- `BracketInsertionService` chỉ ĐIỀU PHỐI — không chứa công thức hình học.
- File > ~250 dòng hoặc ≥ 2 trách nhiệm → tách.

---

## 5. Nguồn chân lý hình học

`Geometry/KneeProfileGenerator.cs` = port `Services/ProfileService.cs` (repo `MCG_3DPanel`).
`Geometry/KneeParameterSolver.cs` = port `Services/Builders/BracketBuilder.cs` (`Solve` + `KneeDtop`).

⚠️ Sửa công thức toe/knee/squareCorner/under-50 → **sửa đồng thời cả 2 repo**. Xem các skill:
`.claude/skills/bracket-identification`, `.claude/skills/parametric-profiles` bên `MCG_3DPanel`.

---

## 6. Đơn vị

- Inventor model API: **CENTIMET**. Dùng `InventorUnits.Cm(mm)` / `.Mm(cm)` cho MỌI giá trị hình học.
- Thông số người dùng + User Parameter: **mm**.
- Outline generator: **mm**, hệ local (gốc = Web∩TopPlate, +X dọc member, −Y xuống).

---

## 7. Logging

```csharp
private const string LOG = "[ClassName]";
FileLogger.Log(LOG, "message");                       // %APPDATA%\MCG_BracketLibrary\plugin.log
FileLogger.LogException(LOG, "context", ex);
AddinLogger.Log("[phase]", ex);                       // chỉ lỗi Activate/Deactivate → C:\CustomTools\Inventor\logs
```

---

## 8. Quy tắc ngôn ngữ (như MCG_CheckListInventor)

| Nội dung | Ngôn ngữ |
|---|---|
| Class/method/variable, UI text, log | English |
| `/// <summary>` + inline `//` | Tiếng Việt |
| Error message cho user | Tiếng Việt |

---

## 9. Kết thúc task — bắt buộc

Thêm session mới vào **ĐẦU** SESSION_LOG.md (Đã làm / Trạng thái / Bước tiếp theo).
Khi user nhắn "lưu session" / "hết token" / "tạm dừng" → lưu ngay, không hỏi lại.
