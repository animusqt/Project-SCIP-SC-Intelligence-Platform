using System;
using System.Windows;
using System.Windows.Controls;
using ScipDashboard.WPF.Services;
using ScipDashboard.WPF.ViewModels;

namespace ScipDashboard.WPF.Modules
{
    public partial class ScanView : UserControl
    {
        public ScanView()
        {
            try
            {
                InitializeComponent();

                // Get services from App
                DataContext = App.Services.Get<ScanViewModel>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ScanView] Construction error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace:\n{ex.StackTrace}");
                throw;
            }
        }

        private void CalibrateRegion_Click(object sender, RoutedEventArgs e)
        {
            var scanner = App.Services.Get<ScreenScannerService>();
            var overlay = new RegionCalibrationWindow();
            var owner = Window.GetWindow(this);
            if (owner != null)
                overlay.Owner = owner;

            if (overlay.ShowDialog() != true || overlay.SelectedRegion == null)
                return;

            scanner.SaveRegion(overlay.SelectedRegion);
            if (DataContext is ScanViewModel viewModel)
            {
                viewModel.NotifyScannerRegionChanged();
                viewModel.StatusMessage = "Game capture region saved. Press F9 to stage a scan.";
            }
        }
    }
}
