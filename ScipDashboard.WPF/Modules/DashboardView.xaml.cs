using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using ScipDashboard.Core.Data;
using ScipDashboard.Core.Services;
using ScipDashboard.WPF.ViewModels;

namespace ScipDashboard.WPF.Modules
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            DataContext = new DashboardViewModel(
                App.Services.Get<IDataService>(),
                App.Services.Get<IMarketDataService>());
        }

        private void OnMarketClick(object sender, RoutedEventArgs e)
        {
            NavigateTo(ScipDashboard.WPF.DashboardView.Market);
        }

        private void OnHaulingClick(object sender, RoutedEventArgs e)
        {
            NavigateTo(ScipDashboard.WPF.DashboardView.Hauling);
        }

        private void OnFleetClick(object sender, RoutedEventArgs e)
        {
            NavigateTo(ScipDashboard.WPF.DashboardView.Fleet);
        }

        private void OnSettingsClick(object sender, RoutedEventArgs e)
        {
            NavigateTo(ScipDashboard.WPF.DashboardView.Settings);
        }

        private void OnScanClick(object sender, RoutedEventArgs e)
        {
            NavigateTo(ScipDashboard.WPF.DashboardView.Scan);
        }

        private void OnSalvageClick(object sender, RoutedEventArgs e)
        {
            NavigateTo(ScipDashboard.WPF.DashboardView.Salvage);
        }

        private void OnStarQuesterClick(object sender, RoutedEventArgs e)
        {
            NavigateTo(ScipDashboard.WPF.DashboardView.StarQuest);
        }

        private void OnSpectrumNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (Uri.TryCreate(e.Uri?.ToString(), UriKind.Absolute, out var uri) &&
                uri.Scheme == Uri.UriSchemeHttps &&
                uri.Host.Equals("robertsspaceindustries.com", StringComparison.OrdinalIgnoreCase))
            {
                Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
        }

        private static void NavigateTo(ScipDashboard.WPF.DashboardView targetView)
        {
            var mainWindow = Application.Current.MainWindow as Main.MainWindow;
            mainWindow?.NavigateToView(targetView);
        }
    }
}
