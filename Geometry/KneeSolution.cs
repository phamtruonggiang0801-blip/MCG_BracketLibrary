namespace BracketLibraryInventorPlugin.Geometry
{
    /// <summary>
    /// Kết quả GIẢI thông số 1 bracket knee (tương ứng <c>BracketBuilder.KneeParams</c> repo MCG_3DPanel).
    /// Chỉ đọc — do <see cref="KneeParameterSolver"/> sinh; UI hiển thị phần "Derived" từ struct này.
    /// </summary>
    public struct KneeSolution
    {
        // Đầu vào đã chuẩn hoá
        public double S;          // leg-on-TopPlate (span)
        public double Thk;        // chiều dày tấm
        public double Overhang;   // flange vươn theo memberDir

        // Dẫn xuất
        public double Huse;       // chiều cao dùng để dựng (WebHeight, hoặc −30 nếu under50)
        public double F;          // leg-on-Flange
        public double Dtop;       // chiều cao mặt đầu span
        public double Ledge;      // đoạn ngang đầu span trước R30 (IB = 15)
        public double FreeEdge;   // dài cạnh chéo tự do (toe-flange → toe-HP)

        public bool Under50;      // overhang < 50
        public bool Flanged;      // free-edge ≥ ngưỡng → gấp lip
        public bool SquareCorner; // flange đỡ dài hơn cả bracket → bỏ R30 chân, hạ thẳng đứng

        // Tham số gấp lip
        public double BendRadius;   // Rb = 2·thk (bán kính TRONG)
        public double LipWidth;     // 70 (thk6) / 100 (thk10)
        public double BendThreshold;// 350 (thk6) / 600 (thk10)
        public int FoldSign;        // +1 / −1

        public override string ToString() =>
            $"S={S:0} thk={Thk:0} overhang={Overhang:0} -> Huse={Huse:0} F={F:0} Dtop={Dtop:0} " +
            $"ledge={Ledge:0} freeEdge={FreeEdge:0} under50={Under50} flanged={Flanged} squareCorner={SquareCorner}";
    }
}
