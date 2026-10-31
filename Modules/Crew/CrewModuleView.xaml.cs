using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace ScipDashboard.Modules.Crew
{
    public partial class CrewModuleView : UserControl
    {
        public CrewModuleView()
        {
            InitializeComponent();
            DataContext = new CrewViewModel();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var viewModel = DataContext as CrewViewModel;
            if (viewModel != null)
            {
                viewModel.SearchQuery = CrewView.SearchBox.Text;
            }
        }

        private void CrewListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var viewModel = DataContext as CrewViewModel;
            if (viewModel != null && CrewView.CrewListBox.SelectedItem is CrewMember member)
            {
                viewModel.SelectedMember = member;
            }
        }
    }
}
