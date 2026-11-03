using System.Windows;
using System.Windows.Controls;
using ScipDashboard.Core.Data;
using ScipDashboard.Core.Services;
using ScipDashboard.WPF.ViewModels;

namespace ScipDashboard.WPF.Modules
{
    public partial class HaulingView : UserControl
    {
        public HaulingView()
        {
            InitializeComponent();

            // Get services from App
            var dataService = App.Services.Get<IDataService>();
            var marketDataService = App.Services.Get<IMarketDataService>();

            // Create view model
            DataContext = new HaulingViewModel(dataService, marketDataService);
        }

        /// <summary>Loads a suggested multi-stop loop into the builder when an option chip is clicked.</summary>
        private void LoopOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button
                && button.DataContext is LoopOption option
                && DataContext is HaulingViewModel viewModel)
            {
                viewModel.UseLoopOption(option);
            }
        }
    }
}
