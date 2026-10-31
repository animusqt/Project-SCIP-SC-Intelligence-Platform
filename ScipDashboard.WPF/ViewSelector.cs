using System.Windows;
using System.Windows.Controls;
using ScipDashboard.Core;

namespace ScipDashboard.WPF
{
    public class ViewSelector : DataTemplateSelector
    {
        public DataTemplate? MainTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item is DashboardView view)
            {
                return view switch
                {
                    DashboardView.Main => MainTemplate,
                    _ => null
                };
            }
            return null;
        }
    }
}
