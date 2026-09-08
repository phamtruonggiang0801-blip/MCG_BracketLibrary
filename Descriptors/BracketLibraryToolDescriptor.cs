using System;
using System.Drawing;
using System.Reflection;
using Inventor;
using MCG.Inventor.Ribbon;

namespace BracketLibraryInventorPlugin.Descriptors
{
    /// <summary>
    /// Nút "Bracket Library" trên tab MCG TOOLS → panel Model (hiện trên Part + Assembly ribbon).
    /// Click → bật/tắt DockableWindow.
    /// </summary>
    internal class BracketLibraryToolDescriptor : IToolDescriptor
    {
        private readonly Action _toggleAction;

        public BracketLibraryToolDescriptor(Action toggleAction) => _toggleAction = toggleAction;

        public string Id          => "id.Button.BracketLibrary.Open";
        public string DisplayName => "Bracket\nLibrary";
        public string Tooltip     => "MCG Bracket Library";
        public string Description  =>
            "Thư viện bracket tham số: chọn loại (OB/OB1/IB/IB1/FB/FB1), pick mặt Web + cạnh tham chiếu, " +
            "chỉnh thông số dạng Parameter rồi chèn bracket vào assembly (component) hoặc part (feature).";

        public Bitmap Icon16 => LoadIcon("icon16.png");
        public Bitmap Icon32 => LoadIcon("icon32.png");

        public PanelLocation Panel    => PanelLocation.Model;
        public RibbonContext Contexts => RibbonContext.Model3D;   // Part + Assembly

        public ButtonDisplayEnum ButtonDisplay => ButtonDisplayEnum.kAlwaysDisplayText;
        public CommandTypesEnum  CommandType   => CommandTypesEnum.kShapeEditCmdType;
        public bool              UseLargeIcon  => true;

        public void OnExecute(NameValueMap context) => _toggleAction?.Invoke();

        // Palette quản lý trực tiếp trong StandardAddInServer → trả null để SDK không can thiệp.
        public IDockablePanelDescriptor DockablePanel => null;

        private static Bitmap LoadIcon(string fileName)
        {
            var asm = Assembly.GetExecutingAssembly();
            return PictureDispConverter.LoadBitmapFromResource(
                asm, $"BracketLibraryInventorPlugin.Resources.{fileName}");
            // null → MCGRibbonManager tự tạo placeholder icon xanh MCG
        }
    }
}
