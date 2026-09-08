namespace BracketLibraryInventorPlugin.Models
{
    /// <summary>
    /// Nhóm bracket knee. Ba nhóm khác nhau CHỈ ở mặt đầu span (Dtop) + ledge — toe/bend dùng chung.
    /// Xem skill bracket-identification (mục 4b) trong repo MCG_3DPanel.
    ///
    /// (Giai đoạn sau có thể mở rộng: GirderEnd, HpEndBracketB, CollarPlate — mỗi nhóm 1 generator riêng.)
    /// </summary>
    public enum BracketFamily
    {
        /// <summary>OB / OB1 — nối Web ↔ LƯNG HP. Dtop = HP − 15, ledge 0.</summary>
        OuterBracket,

        /// <summary>IB / IB1 — nối Web ↔ BỤNG HP (né bulb). Dtop = HP − 30, ledge 15.</summary>
        InnerBracket,

        /// <summary>FB / FB1 — nối Web ↔ Flat Bar. Dtop = FlatBar − 15, ledge 0. (Mã thật trên bản vẽ = "BF".)</summary>
        FlatbarBracket
    }
}
