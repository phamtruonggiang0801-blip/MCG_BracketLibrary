namespace BracketLibraryInventorPlugin.Models
{
    /// <summary>
    /// 1 đỉnh của outline elevation (đơn vị mm, hệ local: gốc = góc Web∩TopPlate,
    /// +X dọc member/Top Plate, −Y xuống theo chiều cao Web).
    ///
    /// <see cref="Bulge"/> = bulge kiểu LWPolyline AutoCAD: tan(góc quét cung / 4).
    /// 0 = đoạn thẳng tới đỉnh KẾ TIẾP. Dương/âm = chiều cung (CCW dương).
    /// Giữ đúng quy ước của ProfileService (repo MCG_3DPanel) để port công thức 1:1.
    /// </summary>
    public struct ProfileVertex
    {
        public double X;
        public double Y;
        public double Bulge;

        public ProfileVertex(double x, double y, double bulge = 0.0)
        {
            X = x; Y = y; Bulge = bulge;
        }

        public override string ToString() =>
            Bulge == 0.0 ? $"({X:0.##},{Y:0.##})" : $"({X:0.##},{Y:0.##} b={Bulge:0.####})";
    }
}
