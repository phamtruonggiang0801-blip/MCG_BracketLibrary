using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BracketLibraryInventorPlugin.Models
{
    /// <summary>
    /// Outline kín (elevation) của 1 bracket — danh sách <see cref="ProfileVertex"/> + bulge.
    /// Đây là "hợp đồng" giữa generator hình học (thuần toán, không phụ thuộc Inventor)
    /// và <c>SketchProfileBuilder</c> (dựng PlanarSketch từ outline này).
    /// </summary>
    public sealed class Profile2D
    {
        public List<ProfileVertex> Vertices { get; } = new List<ProfileVertex>();

        /// <summary>Cạnh tự do (free-edge) — nơi gấp lip. null nếu bracket không gấp lip.</summary>
        public FreeEdge FreeEdge { get; set; }

        public void Add(double x, double y, double bulge = 0.0) => Vertices.Add(new ProfileVertex(x, y, bulge));
        public void Add(ProfileVertex v) => Vertices.Add(v);

        public int Count => Vertices.Count;

        public (double minX, double minY, double maxX, double maxY) Bounds()
        {
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (var v in Vertices)
            {
                minX = Math.Min(minX, v.X); minY = Math.Min(minY, v.Y);
                maxX = Math.Max(maxX, v.X); maxY = Math.Max(maxY, v.Y);
            }
            return (minX, minY, maxX, maxY);
        }

        public override string ToString()
        {
            var sb = new StringBuilder($"Profile2D n={Count} [");
            sb.Append(string.Join(" ", Vertices.Select(v => v.ToString())));
            sb.Append("]");
            if (FreeEdge != null) sb.Append($" freeEdge={FreeEdge}");
            return sb.ToString();
        }
    }

    /// <summary>Cạnh chéo tự do của bracket (từ toe đầu-Flange tới toe đầu-HP). Dùng để đắp lip gấp.</summary>
    public sealed class FreeEdge
    {
        public double Ax, Ay, Bx, By;
        public double Length => Math.Sqrt((Bx - Ax) * (Bx - Ax) + (By - Ay) * (By - Ay));
        public override string ToString() => $"({Ax:0.#},{Ay:0.#})->({Bx:0.#},{By:0.#}) L={Length:0}";
    }
}
