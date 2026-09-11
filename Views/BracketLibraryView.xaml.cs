using System;
using System.Windows;
using System.Windows.Controls;
using Inventor;
using BracketLibraryInventorPlugin.Services;
using BracketLibraryInventorPlugin.Utilities;

namespace BracketLibraryInventorPlugin.Views
{
    public partial class BracketLibraryView : UserControl
    {
        private const string LOG = "[BracketLibraryView]";
        private readonly BracketLibraryViewModel _vm;

        public BracketLibraryView(global::Inventor.Application app)
        {
            InitializeComponent();
            try
            {
                var service = new BracketInsertionService(app);
                _vm = new BracketLibraryViewModel(service);
                DataContext = _vm;
                _vm.Recompute();
            }
            catch (Exception ex)
            {
                FileLogger.LogException(LOG, "ctor", ex);
                MessageBox.Show("Không khởi tạo được Bracket Library: " + ex.Message);
            }
        }

        private void BtnPick_Click(object sender, RoutedEventArgs e)
        {
            SyncContext();
            try { _vm.PickLocation(); }
            catch (Exception ex) { FileLogger.LogException(LOG, "pick", ex); }
        }

        private void BtnInsert_Click(object sender, RoutedEventArgs e)
        {
            SyncContext();
            try { _vm.Insert(); }
            catch (Exception ex)
            {
                FileLogger.LogException(LOG, "insert", ex);
                MessageBox.Show("Lỗi khi chèn: " + ex.Message);
            }
        }

        private void BtnFlipLip_Click(object sender, RoutedEventArgs e)
        {
            try { _vm?.FlipLip(); }
            catch (Exception ex) { FileLogger.LogException(LOG, "flipLip", ex); }
        }

        private void BtnFlipLipSel_Click(object sender, RoutedEventArgs e)
        {
            try { _vm?.FlipLipOnSelected(); }
            catch (Exception ex) { FileLogger.LogException(LOG, "flipLipSelected", ex); }
        }

        private void SyncContext()
        {
            if (_vm == null) return;
            _vm.ContextIsAssembly = RbAsm.IsChecked == true;
        }
    }
}
