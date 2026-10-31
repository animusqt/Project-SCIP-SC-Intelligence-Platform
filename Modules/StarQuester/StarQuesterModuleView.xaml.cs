using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace ScipDashboard.Modules.StarQuester
{
    public partial class StarQuesterModuleView : UserControl
    {
        public StarQuesterModuleView()
        {
            InitializeComponent();
            DataContext = new StarQuesterViewModel();
        }

        private void QuestListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var viewModel = DataContext as StarQuesterViewModel;
            if (viewModel != null && QuestListBox.SelectedItem is Quest selectedQuest)
            {
                viewModel.SelectedQuest = selectedQuest;
            }
        }
    }
}
