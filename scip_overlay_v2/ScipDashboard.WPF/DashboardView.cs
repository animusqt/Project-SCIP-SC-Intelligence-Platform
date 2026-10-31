namespace ScipDashboard.WPF
{
    public enum DashboardView
    {
        Main,
        Market,
        Hauling,
        RouteBuilder,
        MultiRouteBuilder,
        SingleRun,
        Fleet,
        StarQuest,
        Salvage,
        Settings
    }

    public sealed class DashboardState
    {
        private static readonly DashboardState _instance = new();
        public static DashboardState Instance => _instance;

        public DashboardView CurrentView { get; set; } = DashboardView.Main;
    }
}
