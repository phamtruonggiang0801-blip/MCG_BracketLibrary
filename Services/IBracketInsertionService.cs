using BracketLibraryInventorPlugin.Geometry;
using BracketLibraryInventorPlugin.Models;

namespace BracketLibraryInventorPlugin.Services
{
    public sealed class InsertResult
    {
        public bool Success;
        public string Message;
        public string PartFilePath;   // set khi ngữ cảnh Assembly
        public KneeSolution Solution;
    }

    /// <summary>Điều phối: pick vị trí → giải thông số → dựng hình → chèn (component / feature).</summary>
    public interface IBracketInsertionService
    {
        /// <summary>Pick 4 mặt (Web/TopPlate/HP/Flange) + click điểm đặt vị trí. null nếu user huỷ.</summary>
        InsertLocation PickLocation(bool flipMemberDir);

        /// <summary>Giải thông số + outline (để preview) + cảnh báo hình học (không dựng gì trong Inventor).</summary>
        (KneeSolution solution, Profile2D outline, string warning) Preview(BracketDefinition def, BracketParameters p);

        /// <summary>Dựng + chèn bracket vào tài liệu đang mở.</summary>
        InsertResult Insert(BracketDefinition def, BracketParameters p, InsertLocation location);

        /// <summary>Lật lip của bracket component đang được chọn trong assembly (rebuild lip tại chỗ).</summary>
        string FlipLipOnSelected();
    }
}
