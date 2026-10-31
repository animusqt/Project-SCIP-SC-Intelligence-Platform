using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace ScipDashboard.Modules.Scrap
{
    public partial class ScrapModuleView : UserControl
    {
        public ScrapModuleView()
        {
            InitializeComponent();
            DataContext = new ScrapViewModel();
        }

        private void ScrapListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is ScrapViewModel vm && ScrapListBox.SelectedItem is ScrapItem selected)
            {
                vm.UpdateDetails(selected);
            }
        }
    }
}
