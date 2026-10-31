using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScipDashboard.WPF.Modules
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
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

        private static void NavigateTo(ScipDashboard.WPF.DashboardView targetView)
        {
            var mainWindow = Application.Current.MainWindow as Main.MainWindow;
            mainWindow?.NavigateToView(targetView);
        }
    }
}
