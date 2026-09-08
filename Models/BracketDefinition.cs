using System;

namespace BracketLibraryInventorPlugin.Models
{
    /// <summary>
    /// Metadata catalog cho 1 loại bracket: hiển thị trên gallery + thông số mặc định
    /// + nhóm hình học. Nạp trong <see cref="Catalog.BracketCatalog"/>.
    /// </summary>
    public sealed class BracketDefinition
    {
        public BracketType Type { get; }
        public BracketFamily Family { get; }

        /// <summary>Tên hiển thị trên UI (VD "OB — Outer Bracket (t6)").</summary>
        public string DisplayName { get; }

        /// <summary>Mã đóng dấu lên component/feature. Flatbar dùng "BF"/"BF1" (không phải "FB").</summary>
        public string PartCode { get; }

        /// <summary>Chiều dày danh nghĩa (mm) — mặc định của <see cref="BracketParameters.PlateThickness"/>.</summary>
        public double NominalThicknessMm { get; }

        /// <summary>Ngưỡng free-edge để gấp lip (mm). thk6 → 350, thk10 → 600.</summary>
        public double BendThresholdMm { get; }

        /// <summary>Bề rộng lip khi gấp (mm). thk6 → 70, thk10 → 100.</summary>
        public double LipWidthMm { get; }

        public string Notes { get; }

        private readonly Func<BracketParameters> _defaults;

        public BracketDefinition(
            BracketType type, BracketFamily family, string displayName, string partCode,
            double nominalThicknessMm, double bendThresholdMm, double lipWidthMm,
            Func<BracketParameters> defaults, string notes)
        {
            Type = type;
            Family = family;
            DisplayName = displayName;
            PartCode = partCode;
            NominalThicknessMm = nominalThicknessMm;
            BendThresholdMm = bendThresholdMm;
            LipWidthMm = lipWidthMm;
            _defaults = defaults;
            Notes = notes;
        }

        /// <summary>Bản sao thông số mặc định (đã set PlateThickness = <see cref="NominalThicknessMm"/>).</summary>
        public BracketParameters CreateDefaultParameters()
        {
            var p = _defaults();
            p.PlateThickness = NominalThicknessMm;
            return p;
        }

        public override string ToString() => $"{Type} [{PartCode}] {DisplayName}";
    }
}
