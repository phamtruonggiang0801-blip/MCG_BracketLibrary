namespace BracketLibraryInventorPlugin.Models
{
    public enum HostContextKind
    {
        /// <summary>Active document là assembly → chèn bracket thành 1 component (.ipt occurrence).</summary>
        Assembly,

        /// <summary>Active document là part → thêm sketch + extrude + fold ngay vào part đó.</summary>
        Part
    }

    /// <summary>
    /// Vị trí + hướng đặt bracket, do <c>WebFaceLocationPicker</c> tính ra từ (mặt Web) + (cạnh tham chiếu).
    /// Thuần số (mm, đơn vị model) — không giữ tham chiếu COM để Models không phụ thuộc Inventor interop.
    ///
    /// Hệ local outline → world:
    ///   gốc (0,0)  →  <see cref="Anchor"/>            (chân Web ∩ mặt trên Top Plate)
    ///   +X local   →  <see cref="MemberDir"/>          (dọc member, ra phía HP/mép)
    ///   +Y local   →  <see cref="UpDir"/>              (lên; outline dùng −Y nên xuống = −UpDir)
    ///   +Z local   →  <see cref="PlaneNormal"/>        (pháp tuyến tấm bracket = pháp tuyến mặt Web)
    /// </summary>
    public sealed class InsertLocation
    {
        public HostContextKind Context { get; set; }

        public double[] Anchor { get; set; } = { 0, 0, 0 };
        public double[] MemberDir { get; set; } = { 1, 0, 0 };
        public double[] UpDir { get; set; } = { 0, 0, 1 };
        public double[] PlaneNormal { get; set; } = { 0, 1, 0 };

        /// <summary>Chiều cao Web đo được từ mặt pick (mm). null nếu picker không suy ra được.</summary>
        public double? MeasuredWebHeightMm { get; set; }

        /// <summary>Nhãn gợi nhớ (VD "Face of Web@Rib3 + Edge") để hiển thị trên UI.</summary>
        public string Label { get; set; } = "(chưa chọn vị trí)";

        public bool IsValid =>
            Anchor?.Length == 3 && MemberDir?.Length == 3 && UpDir?.Length == 3 && PlaneNormal?.Length == 3;
    }
}
