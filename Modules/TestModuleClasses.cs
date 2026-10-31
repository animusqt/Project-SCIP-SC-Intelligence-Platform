using System;
using System.Windows;
using System.Windows.Controls;

namespace ScipDashboard.Modules
{
    // Test classes that MainViewModel tries to instantiate
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
        }
    }
    
    public partial class MarketView : UserControl
    {
        public MarketView()
        {
            InitializeComponent();
        }
    }
    
    public partial class HaulingView : UserControl
    {
        public HaulingView()
        {
            InitializeComponent();
        }
    }
    
    // Add missing view classes that MainViewModel references
    public partial class FleetView : UserControl
    {
        public FleetView()
        {
            InitializeComponent();
        }
    }
    
    public partial class StarQuesterView : UserControl
    {
        public StarQuesterView()
        {
            InitializeComponent();
        }
    }
    
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }
    }
    
    public partial class RouteBuilderView : UserControl
    {
        public RouteBuilderView()
        {
            InitializeComponent();
        }
    }
    
    public partial class MultiRouteBuilderView : UserControl
    {
        public MultiRouteBuilderView()
        {
            InitializeComponent();
        }
    }
    
    public partial class SingleRunView : UserControl
    {
        public SingleRunView()
        {
            InitializeComponent();
        }
    }
}
