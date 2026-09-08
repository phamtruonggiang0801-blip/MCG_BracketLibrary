using System.Collections.Generic;

namespace BracketLibraryInventorPlugin.Models
{
    /// <summary>
    /// Bộ thông số ĐẦU VÀO của 1 bracket (đơn vị mm). Mỗi field map 1-1 sang 1 Inventor
    /// User Parameter trong .ipt sinh ra → user chỉnh tiếp bằng hộp thoại Parameters
    /// và bấm "Rebuild" để dựng lại (xem BracketParameterBinder).
    ///
    /// Giá trị dẫn xuất (Huse, F, Dtop, ledge, flanged, under50, squareCorner, freeEdge)
    /// KHÔNG nằm ở đây — <see cref="Geometry.KneeParameterSolver"/> tính ra <see cref="KneeSolution"/>.
    /// Tách vậy để khớp kiến trúc repo MCG_3DPanel (BracketParameters ↔ input; KneeParams ↔ solved).
    /// </summary>
    public sealed class BracketParameters
    {
        /// <summary>S — leg-on-TopPlate = độ vươn của bracket trong plan (334–524 điển hình).</summary>
        public double SpanS { get; set; } = 400;

        /// <summary>Chiều cao Web mà bracket ngồi lên (cạnh đứng của knee). Có thể auto-đo từ mặt pick.</summary>
        public double WebHeight { get; set; } = 374;

        /// <summary>Chiều cao member đầu kia (HP hoặc Flat Bar) — nuôi Dtop. HP120 → 120.</summary>
        public double MemberHeight { get; set; } = 120;

        /// <summary>
        /// Overhang — tấm đỡ (Flange/FacePlate) vươn xa theo hướng member tính từ chân Web.
        /// F = overhang − 15. overhang &lt; 50 → under-50 (F = 15, chân nâng cách flange 30).
        /// </summary>
        public double FlangeOverhang { get; set; } = 90;

        /// <summary>Chiều dày tấm bracket. Mặc định theo mã (OB=6, OB1=10) nhưng cho sửa.</summary>
        public double PlateThickness { get; set; } = 6;

        /// <summary>Hướng gấp lip: +1 hoặc −1 theo local Z của tấm (chiều đổ kết cấu). Chỉ dùng khi flanged.</summary>
        public int LipFoldDirection { get; set; } = 1;

        public BracketParameters Clone() => new BracketParameters
        {
            SpanS = SpanS,
            WebHeight = WebHeight,
            MemberHeight = MemberHeight,
            FlangeOverhang = FlangeOverhang,
            PlateThickness = PlateThickness,
            LipFoldDirection = LipFoldDirection
        };

        /// <summary>Tên Inventor User Parameter ↔ giá trị mm. Bind 2 chiều qua BracketParameterBinder.</summary>
        public IReadOnlyDictionary<string, double> ToParameterMap() => new Dictionary<string, double>
        {
            ["Span_S"]         = SpanS,
            ["Web_Height"]     = WebHeight,
            ["Member_Height"]  = MemberHeight,
            ["Flange_Overhang"]= FlangeOverhang,
            ["Plate_Thickness"]= PlateThickness,
            ["Lip_Fold_Dir"]   = LipFoldDirection
        };

        public static BracketParameters FromParameterMap(IReadOnlyDictionary<string, double> m)
        {
            var p = new BracketParameters();
            if (m.TryGetValue("Span_S", out var s)) p.SpanS = s;
            if (m.TryGetValue("Web_Height", out var h)) p.WebHeight = h;
            if (m.TryGetValue("Member_Height", out var mh)) p.MemberHeight = mh;
            if (m.TryGetValue("Flange_Overhang", out var o)) p.FlangeOverhang = o;
            if (m.TryGetValue("Plate_Thickness", out var t)) p.PlateThickness = t;
            if (m.TryGetValue("Lip_Fold_Dir", out var f)) p.LipFoldDirection = f >= 0 ? 1 : -1;
            return p;
        }
    }
}
