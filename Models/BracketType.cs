namespace BracketLibraryInventorPlugin.Models
{
    /// <summary>
    /// Mã bracket knee cụ thể. Hậu tố "1" = bậc chiều dày 10mm (mặc định 6mm).
    /// Bậc dày khác (15→"2", 20→"3") chưa có bản vẽ chuẩn — thêm khi cần.
    /// </summary>
    public enum BracketType
    {
        OB,   // Outer, thk 6
        OB1,  // Outer, thk 10
        IB,   // Inner, thk 6
        IB1,  // Inner, thk 10
        FB,   // Flatbar, thk 6  (code thật = "BF")
        FB1   // Flatbar, thk 10 (code thật = "BF1")
    }
}
