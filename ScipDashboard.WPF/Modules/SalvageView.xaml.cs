using System.Windows.Controls;
using ScipDashboard.Core.Data;
using ScipDashboard.WPF.ViewModels;

namespace ScipDashboard.WPF.Modules
{
    public partial class SalvageView : UserControl
    {
        public SalvageView()
        {
            InitializeComponent();
            DataContext = new SalvageViewModel(App.Services.Get<IDataService>());
        }
    }
}
