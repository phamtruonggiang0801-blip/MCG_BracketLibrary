using BracketLibraryInventorPlugin.Models;

namespace BracketLibraryInventorPlugin.Geometry
{
    /// <summary>
    /// Sinh outline elevation (thuần toán, không phụ thuộc Inventor) cho 1 nhóm bracket.
    /// Mỗi nhóm (knee / girder-end / hp-end-B …) 1 implementation — pattern module-per-object.
    /// </summary>
    public interface IBracketProfileGenerator
    {
        bool Supports(BracketFamily family);

        /// <summary>Dựng outline kín từ nghiệm đã giải. Đơn vị mm, hệ local elevation.</summary>
        Profile2D Generate(KneeSolution solution);
    }
}
