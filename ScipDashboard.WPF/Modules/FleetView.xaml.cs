using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using ScipDashboard.WPF.ViewModels;

namespace ScipDashboard.WPF.Modules
{
    public partial class FleetView : UserControl
    {
        public FleetView()
        {
            InitializeComponent();
            DataContext = new FleetViewModel();
        }

        private void OnRsiSpecsNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (e.Uri.Scheme == Uri.UriSchemeHttps &&
                e.Uri.Host.Equals("robertsspaceindustries.com", StringComparison.OrdinalIgnoreCase))
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
        }
    }
}
