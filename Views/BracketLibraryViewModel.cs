using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
            Definitions = new List<BracketDefinition>(BracketCatalog.All);
            SelectedDefinition = Definitions[0];
        }

        public List<BracketDefinition> Definitions { get; }

        private BracketDefinition _selected;
        public BracketDefinition SelectedDefinition
        {
            get => _selected;
            set
            {
                _selected = value;
                Params = value.CreateDefaultParameters();
                OnChanged();
                OnChanged(nameof(NotesText));
                PushParamsToFields();
                Recompute();
            }
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
        public bool AutoWebHeightFromPick { get; set; } = true;

        private InsertLocation _location;
        public InsertLocation Location
        {
            get => _location;
            private set { _location = value; OnChanged(nameof(LocationText)); OnChanged(nameof(CanInsert)); }
        }

        public string LocationText => _location?.Label ?? "(chưa pick — bấm \"Pick vị trí\")";
        public bool CanInsert => _location != null && _location.IsValid;

        private string _derived = "";
        public string DerivedText { get => _derived; private set { _derived = value; OnChanged(); } }

        private string _warning = "";
        public string WarningText { get => _warning; private set { _warning = value; OnChanged(); } }

        private string _status = "Sẵn sàng.";
        public string StatusText { get => _status; private set { _status = value; OnChanged(); } }

        // ─── Actions ───

        public void PickLocation()
        {
            var loc = _service.PickLocation(FlipMemberDir);
            if (loc == null) { StatusText = "Đã huỷ pick vị trí."; return; }

            if (AutoWebHeightFromPick && loc.MeasuredWebHeightMm is double h && h > 20)
            {
                Params.WebHeight = Math.Round(h, 1);
                PushParamsToFields();
            }
            Location = loc;
            ContextIsAssembly = loc.Context == HostContextKind.Assembly;
            OnChanged(nameof(ContextIsAssembly));
            StatusText = $"Đã pick: {loc.Label}";
            Recompute();
        }

        public void Insert()
        {
            if (!CanInsert) { StatusText = "Chưa có vị trí chèn."; return; }
            _location.Context = ContextIsAssembly ? HostContextKind.Assembly : HostContextKind.Part;
            var r = _service.Insert(SelectedDefinition, Params, _location);
            StatusText = r.Message;
        }

        public void Recompute()
        {
            var (k, warn) = _service.Preview(SelectedDefinition, Params);
            DerivedText =
                $"Huse = {k.Huse:0}    F = {k.F:0}    Dtop = {k.Dtop:0}    ledge = {k.Ledge:0}\n" +
                $"free-edge = {k.FreeEdge:0}  (ngưỡng {k.BendThreshold:0})\n" +
                $"under-50 = {k.Under50}    flanged (gấp lip) = {k.Flanged}    squareCorner = {k.SquareCorner}\n" +
                $"lip: rộng {k.LipWidth:0}  Rb {k.BendRadius:0}  hướng {(k.FoldSign >= 0 ? "+Z" : "-Z")}";
            WarningText = warn ?? "";
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
}
