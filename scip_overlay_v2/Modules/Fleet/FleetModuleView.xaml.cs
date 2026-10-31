using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using ScipDashboard.Core.Models;

namespace ScipDashboard.Modules.Fleet
{
    public partial class FleetModuleView : UserControl
    {
        public FleetModuleView()
        {
            InitializeComponent();
            DataContext = new FleetViewModel();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var viewModel = DataContext as FleetViewModel;
            if (viewModel != null)
            {
                viewModel.SearchQuery = SearchBox.Text;
            }
        }
    }
}
