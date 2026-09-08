using System.Collections.Generic;
using System.Linq;
using BracketLibraryInventorPlugin.Models;

namespace BracketLibraryInventorPlugin.Geometry
{
    /// <summary>
    /// Sổ đăng ký generator biên dạng theo nhóm bracket. Thêm nhóm mới (GirderEnd, HpEndBracketB…)
    /// = thêm 1 <see cref="IBracketProfileGenerator"/> vào đây — không sửa chỗ khác.
    /// </summary>
    public sealed class ProfileGeneratorRegistry
    {
        private readonly List<IBracketProfileGenerator> _generators;

        public ProfileGeneratorRegistry(IEnumerable<IBracketProfileGenerator> generators = null)
        {
            _generators = (generators ?? Default()).ToList();
        }

        private static IEnumerable<IBracketProfileGenerator> Default()
        {
            yield return new KneeProfileGenerator();
        }

        public IBracketProfileGenerator For(BracketFamily family)
        {
            var g = _generators.FirstOrDefault(x => x.Supports(family));
            if (g == null) throw new KeyNotFoundException($"Không có generator cho nhóm {family}.");
            return g;
        }
    }
}
