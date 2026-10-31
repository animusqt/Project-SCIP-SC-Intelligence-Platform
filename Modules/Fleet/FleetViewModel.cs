using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using ScipDashboard.Core.Models;

namespace ScipDashboard.Modules.Fleet
{
    public class FleetViewModel : INotifyPropertyChanged
    {
        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                OnPropertyChanged();
                UpdateFilteredShips();
            }
        }

        private ObservableCollection<Ship> _allShips = new ObservableCollection<Ship>();
        public ObservableCollection<Ship> AllShips
        {
            get => _allShips;
            set { _allShips = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Ship> FilteredShips { get; set; } = new ObservableCollection<Ship>();

        public FleetViewModel()
        {
            // Mock ship data
            AllShips = new ObservableCollection<Ship>
            {
                new Ship { Id = "1", Name = "Cutlass", Manufacturer = "Origin", Role = ShipRole.Transport, Variant = "Standard", CargoCapacity = 500, Status = "Available" },
                new Ship { Id = "2", Name = "Drake", Manufacturer = "Origin", Role = ShipRole.Industrial, Variant = "Standard", CargoCapacity = 600, Status = "Available" },
                new Ship { Id = "3", Name = "Mercury", Manufacturer = "Origin", Role = ShipRole.Transport, Variant = "Standard", CargoCapacity = 2000, Status = "In Transit" },
                new Ship { Id = "4", Name = "Horizon", Manufacturer = "Origin", Role = ShipRole.Military, Variant = "Standard", CargoCapacity = 5000, Status = "Available" }
            };

            UpdateFilteredShips();
        }

        private void UpdateFilteredShips()
        {
            FilteredShips.Clear();
            var filtered = AllShips.Where(s => s.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) || 
                                               s.Manufacturer.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                                               s.Role.ToString().Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)).ToList();
            
            foreach (var ship in filtered)
            {
                FilteredShips.Add(ship);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
