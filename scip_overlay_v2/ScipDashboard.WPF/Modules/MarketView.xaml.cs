using System;
using System.Windows;
using System.Windows.Controls;
using ScipDashboard.Core.Data;
using ScipDashboard.Core.Services;
using ScipDashboard.WPF.ViewModels;

namespace ScipDashboard.WPF.Modules
{
    public partial class MarketView : UserControl
    {
        public MarketView()
        {
            try
            {
                InitializeComponent();

                // Get services from App
                var marketService = App.Services.Get<IMarketDataService>();
                var dataService = App.Services.Get<IDataService>();

                // Create view model
                DataContext = new MarketViewModel(marketService, dataService);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MarketView] Construction error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace:\n{ex.StackTrace}");
                throw;
            }
        }
    }
}
