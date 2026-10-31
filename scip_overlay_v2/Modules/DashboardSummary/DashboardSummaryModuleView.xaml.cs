using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Threading.Tasks;

namespace ScipDashboard.Modules.DashboardSummary
{
    public partial class DashboardSummaryModuleView : System.Windows.Controls.UserControl
    {
        public DashboardSummaryModuleView()
        {
            InitializeComponent();
            DataContext = new DashboardSummaryViewModel();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Simple fade-in animation
            await Task.Delay(100);
            
            DoubleAnimation fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5)
            };
            
            StatsGrid.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }
    }
}
