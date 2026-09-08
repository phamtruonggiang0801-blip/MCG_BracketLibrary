# MCG Bracket Library — Inventor Add-in (DEMO)

Thư viện bracket tham số cho Autodesk Inventor. Người dùng:

1. **Chọn loại bracket** (OB / OB1 / IB / IB1 / FB / FB1) từ gallery.
2. **Pick vị trí chèn** — click 1 mặt phẳng Web + 1 cạnh tham chiếu (hướng member).
3. **Chỉnh thông số** dạng Parameter (Span_S, Web_Height, Member_Height, Flange_Overhang,
   Plate_Thickness, Lip_Fold_Dir) — tool hiển thị luôn các giá trị dẫn xuất (Huse, F, Dtop,
   free-edge, có gấp lip hay không…).
4. **Chèn** — vào assembly thành 1 component `.ipt` (đặt tên theo mã), hoặc vào part đang mở
   thành 1 feature.

> Đây là **bản demo để review kiến trúc + luồng**. Hình học tấm knee (outline + toe R30 +
> squareCorner + under-50) được **port 1-1 từ `ProfileService` của repo `MCG_3DPanel`** (đã
> calibrate với panel thật). Phần gấp lip có fillet + sniped-end 2 đầu lip **chưa hoàn thiện** —
> xem [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) §"Việc còn lại".

## Build & chạy

```
dotnet build -c Debug          # cần Inventor 2023 tại C:\Program Files\Autodesk\Inventor 2023
```

Trạng thái: **build OK, 0 error** (đã verify trên máy có Inventor 2023 + .NET 10 SDK).
MSBuild tự deploy DLL + `.addin` vào `C:\CustomTools\Inventor\MCG_BracketLibrary\`.
Chạy `Install_AutoLoadInventorAddin.bat` một lần, rồi **khởi động lại Inventor**.

Nút: tab **MCG TOOLS** → panel **Model** → **Bracket Library** (Part + Assembly ribbon).

👉 Hướng dẫn test chi tiết + checklist chỗ dễ hỏng: **[docs/TESTING.md](docs/TESTING.md)**.

## Cấu trúc

| Thư mục | Nội dung |
|---|---|
| `Core/` | SDK ribbon dùng chung MCG (`MCG.Inventor.Ribbon`) — copy từ `MCG_CheckListInventor` |
| `Models/` | POCO thuần: `BracketType`, `BracketDefinition`, `BracketParameters`, `Profile2D`, `InsertLocation` |
| `Catalog/` | `BracketCatalog` — danh mục loại + thông số mặc định (nguồn: skill trong `MCG_3DPanel`) |
| `Geometry/` | Toán thuần, KHÔNG phụ thuộc Inventor: `KneeParameterSolver` (port `BracketBuilder.Solve`), `KneeProfileGenerator` (port `ProfileService`) |
| `Interop/` | Cầu nối interop: sketch từ outline, `BracketPartFactory`, `ComponentPlacer`, `BracketFeatureBuilder`, `WebFaceLocationPicker` |
| `Services/` | `BracketInsertionService` — điều phối (không chứa hình học) |
| `Views/` | WPF palette + ViewModel |

## Liên hệ với `MCG_3DPanel`

`Geometry/KneeProfileGenerator.cs` và `Geometry/KneeParameterSolver.cs` là **bản port** của
`Services/ProfileService.cs` + `Services/Builders/BracketBuilder.cs` bên repo AutoCAD.
Nếu sửa công thức toe/knee, **phải sửa đồng thời cả 2 nơi** (2 nguồn phải khớp) — hoặc tách
`ProfileService` thành 1 package .NET Standard dùng chung (đề xuất, xem ARCHITECTURE §6).
