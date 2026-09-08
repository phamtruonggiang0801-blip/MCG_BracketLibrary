using System;
using BracketLibraryInventorPlugin.Models;

namespace BracketLibraryInventorPlugin.Geometry
{
    /// <summary>
    /// Giải thông số bracket knee — port của <c>BracketBuilder.Solve</c> + <c>KneeDtop</c> (repo MCG_3DPanel).
    /// Khác biệt DUY NHẤT so với bản AutoCAD: overhang / web-height / member-height ở đây là
    /// THÔNG SỐ NGƯỜI DÙNG NHẬP (hoặc auto-đo từ pick), không phải suy từ hình học panel 2D.
    /// </summary>
    public static class KneeParameterSolver
    {
        public static KneeSolution Solve(BracketDefinition def, BracketParameters p)
        {
            var k = new KneeSolution
            {
                S = p.SpanS,
                Thk = p.PlateThickness,
                Overhang = p.FlangeOverhang,
                FoldSign = p.LipFoldDirection >= 0 ? 1 : -1
            };

            // leg-on-Flange: F = overhang − 15; kẹp ≤ S − R30. overhang < 50 → under-50 (F = 15).
            k.Under50 = k.Overhang < 50.0;
            double rawF = k.Overhang - 15.0;
            k.SquareCorner = !k.Under50 && rawF > k.S - 30.0;
            k.F = k.Under50 ? 15.0 : Math.Min(rawF, k.S - 30.0);
            k.Huse = k.Under50 ? p.WebHeight - 30.0 : p.WebHeight;

            // Dtop + ledge theo nhóm (bracket-identification mục 4b).
            switch (def.Family)
            {
                case BracketFamily.InnerBracket:
                    k.Dtop = p.MemberHeight - 30.0; k.Ledge = 15.0; break;
                case BracketFamily.FlatbarBracket:
                    k.Dtop = p.MemberHeight - 15.0; k.Ledge = 0.0; break;
                default: // OuterBracket
                    k.Dtop = p.MemberHeight - 15.0; k.Ledge = 0.0; break;
            }

            // free-edge (toe-flange → toe-HP); flanged → gấp lip.
            k.FreeEdge = Math.Sqrt((k.S - k.F) * (k.S - k.F) + (k.Huse - k.Dtop) * (k.Huse - k.Dtop));
            k.BendThreshold = def.BendThresholdMm;               // 350 (t6) / 600 (t10)
            k.LipWidth = def.LipWidthMm;                          // 70 / 100
            k.BendRadius = 2.0 * k.Thk;                           // Rb TRONG
            k.Flanged = !k.SquareCorner && k.FreeEdge >= k.BendThreshold;

            return k;
        }

        /// <summary>Cảnh báo hình học để hiển thị trên UI (không chặn insert — chỉ nhắc user).</summary>
        public static string Validate(KneeSolution k)
        {
            if (k.S < 50) return "S quá nhỏ (< 50mm).";
            if (k.Huse < 50) return "Chiều cao Web quá nhỏ.";
            if (k.Dtop <= 0) return "Member_Height quá nhỏ so với Dtop.";
            if (k.Dtop >= k.Huse) return "Dtop ≥ chiều cao Web — mặt đầu span cao hơn cả Web.";
            if (k.F >= k.S) return "F ≥ S — overhang quá lớn so với span.";
            if (k.S >= 775) return "S ≥ 775mm — vượt giới hạn bản vẽ (L < 775).";
            return null;
        }
    }
}
