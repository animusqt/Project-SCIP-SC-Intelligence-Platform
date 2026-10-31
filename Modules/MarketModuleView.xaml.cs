using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScipDashboard.Modules
{
    public partial class MarketModuleView : UserControl
    {
        public MarketModuleView()
        {
            InitializeComponent();
        }
    }
    
    public class SampleCommodity
    {
        public string Icon { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public double Buy { get; set; } = 0;
        public double Sell { get; set; } = 0;
        public double Margin { get; set; } = 0;
        public SolidColorBrush MarginColor { get; set; } = Brushes.Green;
        public string Location { get; set; } = "";
    }
}
