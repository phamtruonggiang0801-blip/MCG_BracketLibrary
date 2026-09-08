namespace BracketLibraryInventorPlugin.Interop
{
    /// <summary>
    /// Inventor lưu độ dài nội bộ bằng CENTIMET. Mọi giá trị hình học truyền vào model API
    /// phải nhân <see cref="MmToCm"/>. Thông số hiển thị / User Parameter ta giữ mm.
    /// </summary>
    internal static class InventorUnits
    {
        public const double MmToCm = 0.1;
        public const double CmToMm = 10.0;

        public static double Cm(double mm) => mm * MmToCm;
        public static double Mm(double cm) => cm * CmToMm;
    }
}
