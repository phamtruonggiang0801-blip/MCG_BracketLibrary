# Tham số bracket knee — tham chiếu

Trích từ các skill của `MCG_3DPanel`: `bracket-identification` (mục 2, 4b, 5), `parametric-profiles`
(mục 5, 5b). Đây là số liệu đã kiểm chứng trên panel thật 7D-09C / 7D-17C.

## 1. Ba nhóm — khác nhau CHỈ ở mặt đầu span

| Nhóm | Mã | Nối | Dtop (mặt đầu span) | ledge | Ghi chú |
|---|---|---|---|---|---|
| Outer | OB / OB1 | Web ↔ **lưng HP** | `Member_Height − 15` | 0 | phổ biến nhất |
| Inner | IB / IB1 | Web ↔ **bụng HP** (né bulb) | `Member_Height − 30` | **15** | ledge ngang bọc bulb trước R30 |
| Flatbar | FB / FB1 | Web ↔ **Flat Bar** | `Member_Height − 15` | 0 | mã part thật = **"BF"** |

Hậu tố **"1"** = tấm dày 10mm (mặc định 6mm). `Rb = 2·thk`, đoạn-dày = `thk`.

## 2. Ba leg của knee (hệ local, gốc = Web∩TopPlate)

| Leg | Local | Công thức |
|---|---|---|
| Leg trên (Top Plate, y=0) | `(0,0)→(S,0)` | **S** = `Span_S` (độ vươn plan, 334–524 điển hình) |
| Cạnh Web (đứng, x=0) | `(0,0)→(0,−H)` | **H** = `Huse` = `Web_Height` (hoặc −30 nếu under-50) |
| Leg dưới (Flange, y=−H) | `(F,−H)→(0,−H)` | **F** = `Flange_Overhang − 15`, kẹp ≤ `S − 30` |

## 3. Các nhánh hình học

| Điều kiện | Kết quả |
|---|---|
| `Flange_Overhang < 50` | **under-50**: `F = 15`, `Huse = Web_Height − 30` (chân thụt 30mm trên Flange) |
| `Flange_Overhang − 15 > Span_S − 30` | **squareCorner**: bỏ R30 chân, hạ thẳng đứng góc vuông (Flange đỡ dài hơn cả bracket) |
| `free-edge ≥ 350` (thk6) / `≥ 600` (thk10), và KHÔNG squareCorner | **flanged**: toe = R30 → 18mm → Rb → đoạn dày ⊥ → gấp lip |
| còn lại | **R30-only**: 2 cung R30 tiếp tuyến chung 1 free-edge thẳng |

`free-edge = √((S − F)² + (Huse − Dtop)²)` — khoảng cách **toe-tới-toe**
(toe-HP ở `(S,−Dtop)`, toe-flange ở `(F,−Huse)`).

## 4. Toe-end R30 (cố định mọi chiều dày — KHÁC Rb)

- Mỗi toe-end = 1 cung **R30** bo **LÕM cắt vào thân** (bulge dương trong outline CW).
- Tâm R30 top = `(S − ledge, −Dtop − 30)`; tâm R30 bot = `(F + 30, −H)`.
- Free-edge = tiếp tuyến ngoài chung của 2 cung R30.
- Kiểm chứng block `OB_type1` (S=594, H=374, F=148.9): bulge top ≈ **+0.132**, bulge bot ≈ **+0.268**.

## 5. Gấp lip (flanged)

| thk | ngưỡng free-edge | bề rộng lip | Rb (trong) | Rb+thk (ngoài) |
|---|---|---|---|---|
| 6 | 350 | 70 | 12 | 18 |
| 10 | 600 | 100 | 20 | 30 |

- Lip = mép bracket gấp vuông góc, chạy dọc free-edge, dày = dày tấm.
- Hướng gấp = về **chiều đổ kết cấu** (`Lip_Fold_Dir` ±1; bản AutoCAD suy từ panel type).
- 2 đầu lip: **Sniped End** — `rise = lipW − 15`, `run = rise·tan60°`, còn 15mm phẳng sát mặt đầu.

## 6. Giới hạn

- `Span_S < 775` (giới hạn bản vẽ; ≥ 775 là GB — chưa thiết kế).
- IB: classification bụng/lưng HP bên `MCG_3DPanel` **chưa chốt**; ở tool này user tự chọn OB vs IB.
- IB Dtop: theo rule kỹ sư `HP − 30`; block `IB_type1` lại vẽ theo chiều cao web HP — chờ panel IB thật.
