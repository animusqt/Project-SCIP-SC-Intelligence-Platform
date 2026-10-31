using System;
using System.Windows;
using System.Windows.Controls;
using ScipDashboard.Core.Data;
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
                var dataService = App.Services.Get<IDataService>();

                // Create view model
                DataContext = new ScanViewModel(dataService);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ScanView] Construction error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace:\n{ex.StackTrace}");
                throw;
            }
        }
    }
}
