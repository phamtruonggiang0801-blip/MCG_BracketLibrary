using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms.Integration;
using Inventor;
using BracketLibraryInventorPlugin.Descriptors;
using BracketLibraryInventorPlugin.Helpers;
using BracketLibraryInventorPlugin.Views;
using MCG.Inventor.Ribbon;

namespace BracketLibraryInventorPlugin
{
    /// <summary>
    /// Điểm vào add-in. Vòng đời giống hệt MCG_CheckListInventor:
    ///  - Activate: tạo WPF palette → ElementHost → DockableWindow (quản lý TRỰC TIẾP,
    ///    không qua DockableWindowHost SDK để giữ resize tự do) → ribbon button MCG TOOLS.
    ///  - Deactivate: gỡ ribbon, xoá DockableWindow, ReleaseComObject.
    /// </summary>
    [Guid("7f3a9c21-5b84-4e16-a9d2-3c6e8f1b40a7")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    public class StandardAddInServer : ApplicationAddInServer
    {
        private const string LOG_PREFIX = "[StandardAddInServer]";
        private const string ADDIN_GUID = "{7f3a9c21-5b84-4e16-a9d2-3c6e8f1b40a7}";

        private Inventor.Application _app;
        private MCGRibbonManager     _ribbon;

        private DockableWindow    _dockableWindow;
        private BracketLibraryView _paletteControl;
        private ElementHost       _elementHost;

        public void Activate(ApplicationAddInSite addInSiteObject, bool firstTime)
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            try
            {
                ActivateInternal(addInSiteObject, firstTime);
                AddinLogger.DeleteLogFile();
            }
            catch (Exception ex)
            {
                AddinLogger.Log(LOG_PREFIX, ex);
                // KHÔNG re-throw — exception leak ra Inventor sẽ chặn các add-in khác load.
            }
        }

        private static System.Reflection.Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            string dir = System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location);
            string name = new System.Reflection.AssemblyName(args.Name).Name;
            string path = System.IO.Path.Combine(dir ?? "", name + ".dll");
            return System.IO.File.Exists(path)
                ? System.Reflection.Assembly.LoadFrom(path)
                : null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ActivateInternal(ApplicationAddInSite site, bool firstTime)
        {
            _app = site.Application;
            Debug.WriteLine($"{LOG_PREFIX} Activate (firstTime={firstTime}).");

            // 1. WPF palette + ElementHost
            _paletteControl = new BracketLibraryView(_app);
            _elementHost = new ElementHost
            {
                Dock  = System.Windows.Forms.DockStyle.Fill,
                Child = _paletteControl
            };

            // 2. DockableWindow — không SetMinimumSize để user kéo flex tự do.
            _dockableWindow = _app.UserInterfaceManager.DockableWindows.Add(
                ADDIN_GUID,
                "InternalName_BracketLibrary",
                "MCG Bracket Library");
            _dockableWindow.AddChild(_elementHost.Handle);
            try { _dockableWindow.DockingState = DockingStateEnum.kDockRight; } catch { }
            _dockableWindow.Visible = false;

            // 3. Fix bàn phím Unicode cho WPF popup nhúng trong COM host
            ElementHost.EnableModelessKeyboardInterop(new System.Windows.Window());

            // 4. Ribbon button qua MCG SDK — tab "MCG TOOLS" → panel Model (Part + Assembly)
            var toolDescriptor = new BracketLibraryToolDescriptor(TogglePalette);
            _ribbon = new MCGRibbonManager(_app, ADDIN_GUID);
            _ribbon.RegisterTool(toolDescriptor);
            _ribbon.Build(firstTime);

            Debug.WriteLine($"{LOG_PREFIX} Activate THÀNH CÔNG.");
        }

        private void TogglePalette()
        {
            if (_dockableWindow == null) return;
            _dockableWindow.Visible = !_dockableWindow.Visible;
        }

        public void Deactivate()
        {
            try
            {
                _ribbon?.Cleanup();
                _ribbon = null;

                try { _dockableWindow?.Delete(); } catch { }
                _dockableWindow = null;

                _elementHost?.Dispose();
                _elementHost = null;
                _paletteControl = null;
            }
            catch (Exception ex)
            {
                AddinLogger.Log(LOG_PREFIX, ex);
            }
            finally
            {
                if (_app != null)
                {
                    try { Marshal.ReleaseComObject(_app); } catch { }
                    _app = null;
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        public void ExecuteCommand(int commandID) { }
        public object Automation => null;
    }
}
