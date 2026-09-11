using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using BracketLibraryInventorPlugin.Catalog;
using BracketLibraryInventorPlugin.Geometry;
using BracketLibraryInventorPlugin.Models;
using BracketLibraryInventorPlugin.Services;

namespace BracketLibraryInventorPlugin.Views
{
    /// <summary>
    /// ViewModel cho palette. Giữ <see cref="BracketParameters"/> hiện tại + hiển thị phần "Derived"
    /// (nghiệm <see cref="KneeSolution"/>). Không đụng Inventor interop trực tiếp — qua
    /// <see cref="IBracketInsertionService"/>.
    /// </summary>
    public sealed class BracketLibraryViewModel : INotifyPropertyChanged
    {
        private readonly IBracketInsertionService _service;

        public BracketLibraryViewModel(IBracketInsertionService service)
        {
            _service = service;
            Families = new List<FamilyChoice>
            {
                new FamilyChoice(BracketFamily.OuterBracket,   "OB — Outer (Web ↔ HP back)"),
                new FamilyChoice(BracketFamily.InnerBracket,   "IB — Inner (Web ↔ HP belly)"),
                new FamilyChoice(BracketFamily.FlatbarBracket, "FB — Flatbar (Web ↔ Flat Bar)"),
            };
            Thicknesses = new List<ThkChoice> { new ThkChoice(6.0), new ThkChoice(10.0) };
            _family = Families[0];
            _thk = Thicknesses[0];
            _selected = BracketCatalog.Get(_family.Family, _thk.Mm);
            Params = _selected.CreateDefaultParameters();
        }

        public List<FamilyChoice> Families { get; }
        public List<ThkChoice> Thicknesses { get; }

        private FamilyChoice _family;
        public FamilyChoice SelectedFamily
        {
            get => _family;
            set { if (value != null) { _family = value; ApplyType(resetParams: true); } }
        }

        private ThkChoice _thk;
        public ThkChoice SelectedThickness
        {
            get => _thk;
            set { if (value != null) { _thk = value; ApplyType(resetParams: false); } }
        }

        private BracketDefinition _selected;
        public BracketDefinition SelectedDefinition => _selected;

        /// <summary>Đổi Nhóm → reset param mặc định; đổi Chiều dày → chỉ cập nhật PlateThickness (giữ số đo).</summary>
        private void ApplyType(bool resetParams)
        {
            _selected = BracketCatalog.Get(_family.Family, _thk.Mm);
            if (resetParams) Params = _selected.CreateDefaultParameters();
            else Params.PlateThickness = _thk.Mm;
            OnChanged(nameof(SelectedDefinition));
            OnChanged(nameof(NotesText));
            PushParamsToFields();
            Recompute();
        }

        public string NotesText => _selected?.Notes ?? "";

        // Thông số hiện tại (nguồn sự thật).
        public BracketParameters Params { get; private set; } = new BracketParameters();

        // ─── Field-bound (string để hộp text nhập thoải mái) ───
        public double SpanS { get => Params.SpanS; set { Params.SpanS = value; Recompute(); } }
        public double WebHeight { get => Params.WebHeight; set { Params.WebHeight = value; Recompute(); } }
        public double MemberHeight { get => Params.MemberHeight; set { Params.MemberHeight = value; Recompute(); } }
        public double FlangeOverhang { get => Params.FlangeOverhang; set { Params.FlangeOverhang = value; Recompute(); } }
        public double PlateThickness { get => Params.PlateThickness; set { Params.PlateThickness = value; Recompute(); } }
        public int LipFoldDirection { get => Params.LipFoldDirection; set { Params.LipFoldDirection = value >= 0 ? 1 : -1; Recompute(); } }

        public bool ContextIsAssembly { get; set; } = true;
        public bool FlipMemberDir { get; set; }

        /// <summary>Tự điền Span_S / Web_Height / Flange_Overhang / Member_Height từ các mặt/cạnh pick được.</summary>
        public bool AutoMeasureFromPick { get; set; } = true;

        private InsertLocation _location;
        public InsertLocation Location
        {
            get => _location;
            private set { _location = value; OnChanged(nameof(LocationText)); OnChanged(nameof(CanInsert)); }
        }

        public string LocationText => _location?.Label ?? "(not picked — click \"Pick location\")";
        public bool CanInsert => _location != null && _location.IsValid;

        private string _measured = "";
        public string MeasuredText { get => _measured; private set { _measured = value; OnChanged(); } }

        private string _derived = "";
        public string DerivedText { get => _derived; private set { _derived = value; OnChanged(); } }

        private DrawingImage _preview;
        /// <summary>Section preview: bracket outline (xanh) + Web/TopPlate/Flange/HP (xám). Elevation, mm, Y đảo dấu.</summary>
        public DrawingImage PreviewImage { get => _preview; private set { _preview = value; OnChanged(); } }

        private string _warning = "";
        public string WarningText { get => _warning; private set { _warning = value; OnChanged(); } }

        private string _status = "Ready.";
        public string StatusText { get => _status; private set { _status = value; OnChanged(); } }

        // ─── Actions ───

        public void PickLocation()
        {
            var loc = _service.PickLocation(FlipMemberDir);
            if (loc == null) { StatusText = "Pick cancelled."; return; }

            if (AutoMeasureFromPick)
            {
                var applied = new List<string>();
                Apply("Span_S", loc.MeasuredSpanMm, 50, v => Params.SpanS = v, applied);
                Apply("Web_Height", loc.MeasuredWebHeightMm, 20, v => Params.WebHeight = v, applied);
                Apply("Flange_Overhang", loc.MeasuredFlangeOverhangMm, 10, v => Params.FlangeOverhang = v, applied);
                Apply("Member_Height", loc.MeasuredMemberHeightMm, 20, v => Params.MemberHeight = v, applied);
                PushParamsToFields();
                MeasuredText = applied.Count > 0
                    ? "Measured from geometry: " + string.Join(", ", applied) + ". Enter the rest manually."
                    : "Nothing measured — enter all parameters manually.";
            }
            else MeasuredText = "";

            Location = loc;
            ContextIsAssembly = loc.Context == HostContextKind.Assembly;
            OnChanged(nameof(ContextIsAssembly));
            StatusText = $"Picked: {loc.Label}";
            Recompute();
        }

        private static void Apply(string name, double? measured, double min, Action<double> set, List<string> applied)
        {
            if (measured is double v && v > min) { set(Math.Round(v, 1)); applied.Add(name); }
        }

        public void Insert()
        {
            if (!CanInsert) { StatusText = "No insert location."; return; }
            _location.Context = ContextIsAssembly ? HostContextKind.Assembly : HostContextKind.Part;
            var r = _service.Insert(SelectedDefinition, Params, _location);
            StatusText = r.Message;
        }

        /// <summary>Flip the lip fold direction (±Z local) for the NEXT insert — no re-pick needed.</summary>
        public void FlipLip()
        {
            Params.LipFoldDirection = -Params.LipFoldDirection;
            OnChanged(nameof(LipFoldDirection));
            StatusText = $"Lip folds toward {(Params.LipFoldDirection >= 0 ? "+Z" : "-Z")}.";
            Recompute();
        }

        /// <summary>Flip the lip on the bracket component currently selected in the assembly (rebuilds it in place).</summary>
        public void FlipLipOnSelected()
        {
            StatusText = _service.FlipLipOnSelected();
        }

        public void Recompute()
        {
            var (k, outline, warn) = _service.Preview(SelectedDefinition, Params);
            DerivedText =
                $"Huse = {k.Huse:0}    F = {k.F:0}    Dtop = {k.Dtop:0}    ledge = {k.Ledge:0}\n" +
                $"free-edge = {k.FreeEdge:0}  (threshold {k.BendThreshold:0})\n" +
                $"under-50 = {k.Under50}    flanged = {k.Flanged}    squareCorner = {k.SquareCorner}\n" +
                $"lip: width {k.LipWidth:0}  Rb {k.BendRadius:0}  dir {(k.FoldSign >= 0 ? "+Z" : "-Z")}";
            WarningText = warn ?? "";
            PreviewImage = BuildPreview(outline, k, Params);
        }

        /// <summary>Section preview (mm, screen Y = −local Y): bracket + Web/TopPlate/Flange/HP context.</summary>
        private static DrawingImage BuildPreview(Profile2D outline, KneeSolution k, BracketParameters p)
        {
            var dg = new DrawingGroup();

            // ── context: Web / Top Plate / Flange / HP-FB ──
            var ctx = new GeometryGroup();
            void L(double x1, double y1, double x2, double y2) =>
                ctx.Children.Add(new LineGeometry(new Point(x1, -y1), new Point(x2, -y2)));
            double S = k.S, H = k.Huse;
            double mh = System.Math.Max(20.0, p.MemberHeight);
            double oh = System.Math.Max(0.0, p.FlangeOverhang);
            const double e = 25.0;
            L(0, e, 0, -(H + e));          // Web (đứng, x=0)
            L(-e, 0, S + e, 0);            // Top Plate (ngang, y=0)
            L(0, -H, oh + e, -H);         // Flange (ngang, y=−Huse)
            L(S, e, S, -mh);              // HP / FB (đứng, x=S)
            ctx.Freeze();
            dg.Children.Add(new GeometryDrawing(null,
                new Pen(new SolidColorBrush(Color.FromArgb(0x99, 0x88, 0x88, 0x88)), 7.0), ctx));

            // ── bracket outline (filled) ──
            if (outline != null && outline.Count >= 3)
            {
                var tp = OutlineTessellator.Tessellate(outline);
                var sg = new StreamGeometry();
                using (var c = sg.Open())
                {
                    c.BeginFigure(new Point(tp[0].X, -tp[0].Y), true, true);
                    for (int i = 1; i < tp.Count; i++)
                        c.LineTo(new Point(tp[i].X, -tp[i].Y), true, false);
                }
                sg.Freeze();
                dg.Children.Add(new GeometryDrawing(
                    new SolidColorBrush(Color.FromArgb(0x24, 0x00, 0x78, 0xD4)),
                    new Pen(new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x78, 0xD4)), 4.0), sg));
            }

            dg.Freeze();
            return new DrawingImage(dg);
        }

        // ─── infra ───
        private void PushParamsToFields()
        {
            OnChanged(nameof(SpanS)); OnChanged(nameof(WebHeight)); OnChanged(nameof(MemberHeight));
            OnChanged(nameof(FlangeOverhang)); OnChanged(nameof(PlateThickness)); OnChanged(nameof(LipFoldDirection));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnChanged([CallerMemberName] string n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    /// <summary>Lựa chọn Nhóm bracket cho combo (Phase 5b).</summary>
    public sealed class FamilyChoice
    {
        public BracketFamily Family { get; }
        public string Label { get; }
        public FamilyChoice(BracketFamily family, string label) { Family = family; Label = label; }
        public override string ToString() => Label;
    }

    /// <summary>Lựa chọn bậc chiều dày cho combo (6 / 10 — bậc có rule ngưỡng/lip-width).</summary>
    public sealed class ThkChoice
    {
        public double Mm { get; }
        public ThkChoice(double mm) { Mm = mm; }
        public override string ToString() => $"t{Mm:0}";
    }
}
