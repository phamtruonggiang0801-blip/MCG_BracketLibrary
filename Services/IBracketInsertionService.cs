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
        /// <summary>Pick mặt Web + cạnh tham chiếu. null nếu user huỷ.</summary>
        InsertLocation PickLocation(bool flipMemberDir);

        /// <summary>Giải thông số + cảnh báo hình học (không dựng gì).</summary>
        (KneeSolution solution, string warning) Preview(BracketDefinition def, BracketParameters p);

        /// <summary>Dựng + chèn bracket vào tài liệu đang mở.</summary>
        InsertResult Insert(BracketDefinition def, BracketParameters p, InsertLocation location);
    }
}
