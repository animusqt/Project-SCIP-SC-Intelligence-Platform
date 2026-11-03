using System.Windows;
using System.Windows.Controls;
using ScipDashboard.WPF.ViewModels;

namespace ScipDashboard.WPF.Modules
{
    public partial class StarQuesterView : UserControl
    {
        public StarQuesterView()
        {
            InitializeComponent();
            DataContext = new StarQuesterViewModel();
        }
    }
}
