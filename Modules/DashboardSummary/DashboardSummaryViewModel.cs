using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using ScipDashboard.Core.Models;
using ScipDashboard.Core.Common;

namespace ScipDashboard.Modules.DashboardSummary
{
    public class DashboardSummaryViewModel : INotifyPropertyChanged
    {
        private int _activeHaulCount;
        public int ActiveHaulCount
        {
            get => _activeHaulCount;
            set { _activeHaulCount = value; OnPropertyChanged(); }
        }

        private int _activeRouteCount;
        public int ActiveRouteCount
        {
            get => _activeRouteCount;
            set { _activeRouteCount = value; OnPropertyChanged(); }
        }

        private int _fleetCount;
        public int FleetCount
        {
            get => _fleetCount;
            set { _fleetCount = value; OnPropertyChanged(); }
        }

        private int _cargoCapacity;
        public int CargoCapacity
        {
            get => _cargoCapacity;
            set { _cargoCapacity = value; OnPropertyChanged(); }
        }

        public DashboardSummaryViewModel()
        {
            // Initialize with mock data for now.
            // In production, this will call a service or the DashboardState to populate values.
            RefreshData();
        }

        public void RefreshData()
        {
            // Logic to aggregate data from Hauling, Fleet, and Cargo modules will go here.
            // This logic would eventually reference the DashboardState or Repository classes.
            
            // Example calculation stubs:
            ActiveHaulCount = 2; 
            ActiveRouteCount = 1;
            FleetCount = 14;
            CargoCapacity = 4500;
            
            OnPropertyChanged(nameof(ActiveHaulCount));
            OnPropertyChanged(nameof(ActiveRouteCount));
            OnPropertyChanged(nameof(FleetCount));
            OnPropertyChanged(nameof(CargoCapacity));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
