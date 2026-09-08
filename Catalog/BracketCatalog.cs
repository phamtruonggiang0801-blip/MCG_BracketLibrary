using System.Collections.Generic;
using System.Linq;
using BracketLibraryInventorPlugin.Models;

namespace BracketLibraryInventorPlugin.Catalog
{
    /// <summary>
    /// Danh mục các loại bracket + thông số mặc định. Nguồn số liệu: các skill trong repo
    /// MCG_3DPanel (bracket-identification, parametric-profiles). Thêm loại mới = thêm 1 dòng ở đây
    /// + (nếu nhóm hình học mới) 1 <c>IBracketProfileGenerator</c>.
    /// </summary>
    public static class BracketCatalog
    {
        private static readonly List<BracketDefinition> _all = Build();

        public static IReadOnlyList<BracketDefinition> All => _all;

        public static BracketDefinition Get(BracketType type) => _all.First(d => d.Type == type);

        public static IEnumerable<BracketDefinition> ByFamily(BracketFamily family) =>
            _all.Where(d => d.Family == family);

        private static List<BracketDefinition> Build()
        {
            // Mặc định lấy từ block tham chiếu OB_type1 (S≈594/H≈374) + panel thật (HP120, flange ~120).
            BracketParameters KneeDefaults() => new BracketParameters
            {
                SpanS = 450,
                WebHeight = 374,
                MemberHeight = 120,   // HP120 / FlatBar120
                FlangeOverhang = 90,  // → F = 75 (over-50)
                PlateThickness = 6,
                LipFoldDirection = 1
            };

            return new List<BracketDefinition>
            {
                new BracketDefinition(BracketType.OB,  BracketFamily.OuterBracket,  "OB — Outer Bracket (t6)",  "OB",  6,  350, 70,
                    KneeDefaults,
                    "Web ↔ LƯNG HP. Dtop = Member_Height − 15, ledge 0. Gấp lip khi free-edge ≥ 350."),
                new BracketDefinition(BracketType.OB1, BracketFamily.OuterBracket,  "OB1 — Outer Bracket (t10)", "OB1", 10, 600, 100,
                    KneeDefaults,
                    "Như OB, tấm dày 10. Rb = 20, đoạn dày = 10. Gấp lip khi free-edge ≥ 600."),

                new BracketDefinition(BracketType.IB,  BracketFamily.InnerBracket,  "IB — Inner Bracket (t6)",  "IB",  6,  350, 70,
                    KneeDefaults,
                    "Web ↔ BỤNG HP (né bulb). Dtop = Member_Height − 30, ledge 15. (Classification IB thật chờ panel mẫu.)"),
                new BracketDefinition(BracketType.IB1, BracketFamily.InnerBracket,  "IB1 — Inner Bracket (t10)", "IB1", 10, 600, 100,
                    KneeDefaults,
                    "Như IB, tấm dày 10."),

                new BracketDefinition(BracketType.FB,  BracketFamily.FlatbarBracket, "FB — Flatbar Bracket (t6)",  "BF",  6,  350, 70,
                    KneeDefaults,
                    "Web ↔ Flat Bar. Dtop = Member_Height − 15, ledge 0. Mã part thật = \"BF\"."),
                new BracketDefinition(BracketType.FB1, BracketFamily.FlatbarBracket, "FB1 — Flatbar Bracket (t10)", "BF1", 10, 600, 100,
                    KneeDefaults,
                    "Như FB, tấm dày 10. Mã part thật = \"BF1\"."),
            };
        }
    }
}
