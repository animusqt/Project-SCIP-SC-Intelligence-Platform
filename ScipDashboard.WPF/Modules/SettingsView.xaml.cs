using System.Windows;
using System.Windows.Controls;
using ScipDashboard.WPF.ViewModels;

namespace ScipDashboard.WPF.Modules
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();
        }
    }
}
