using System;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ScipDashboard.WPF
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private DashboardView _currentView;
        public DashboardView CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                DashboardState.Instance.CurrentView = value;
                OnPropertyChanged(nameof(CurrentView));
                OnPropertyChanged(nameof(CurrentViewContent));
            }
        }

        public object? CurrentViewContent
        {
            get
            {
                switch (CurrentView)
                {
                    case DashboardView.Main:
                        return new Modules.DashboardView();
                    case DashboardView.Market:
                        return new Modules.MarketView();
                    case DashboardView.Hauling:
                        return new Modules.HaulingView();
                    case DashboardView.RouteBuilder:
                        return new Modules.RouteBuilderView();
                    case DashboardView.MultiRouteBuilder:
                        return new Modules.MultiRouteBuilderView();
                    case DashboardView.SingleRun:
                        return new Modules.SingleRunView();
                    case DashboardView.Fleet:
                        return new Modules.FleetView();
                    case DashboardView.StarQuest:
                        return new Modules.StarQuesterView();
                    case DashboardView.Salvage:
                        return new Modules.SalvageView();
                    case DashboardView.Settings:
                        return new Modules.SettingsView();
                    default:
                        return new Modules.DashboardView();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainViewModel()
        {
            CurrentView = DashboardView.Main;
        }
    }

}
