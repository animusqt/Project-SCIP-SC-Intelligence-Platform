using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ScipDashboard.WPF.Services;

namespace ScipDashboard.WPF.ViewModels
{
    public sealed class FleetViewModel : INotifyPropertyChanged
    {
        private const string StoreFile = "fleet.json";
        private string _searchQuery = string.Empty;
        private string _statusMessage = "Fleet saved locally on this PC.";

        public FleetViewModel()
        {
            Catalog = LoadCatalog();
            VisibleCatalog = new ObservableCollection<FleetCatalogShip>(Catalog);
            OwnedShips = LocalFeatureStore.Read<ObservableCollection<FleetShipEntry>>(StoreFile);
            foreach (var ship in OwnedShips)
                ship.PropertyChanged += OnFleetShipChanged;

            AddShipCommand = new RelayCommand<string?>(AddShip);
            RemoveShipCommand = new RelayCommand<FleetShipEntry?>(RemoveShip);
            FilterCatalog();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<FleetCatalogShip> Catalog { get; }
        public ObservableCollection<FleetCatalogShip> VisibleCatalog { get; }
        public ObservableCollection<FleetShipEntry> OwnedShips { get; }
        public int CatalogCount => Catalog.Count;
        public RelayCommand<string?> AddShipCommand { get; }
        public RelayCommand<FleetShipEntry?> RemoveShipCommand { get; }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (_searchQuery == value) return;
                _searchQuery = value;
                OnPropertyChanged();
                FilterCatalog();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set { _statusMessage = value; OnPropertyChanged(); }
        }

        public int FleetShipCount => OwnedShips.Sum(ship => ship.Quantity);
        public int FleetCapacity => OwnedShips.Sum(ship => ship.CargoCapacityScu * ship.Quantity);
        public int CargoLoaded => OwnedShips.Sum(ship => ship.CargoLoadedScu * ship.Quantity);

        private void AddShip(string? id)
        {
            var catalogShip = Catalog.FirstOrDefault(ship => ship.Id == id);
            if (catalogShip == null) return;

            var existing = OwnedShips.FirstOrDefault(ship => ship.Name == catalogShip.Name);
            if (existing == null)
            {
                existing = new FleetShipEntry
                {
                    Name = catalogShip.Name,
                    Manufacturer = catalogShip.Manufacturer,
                    Role = catalogShip.Role,
                    CargoCapacityScu = catalogShip.CargoCapacityScu,
                    CurrentLocation = "Unassigned"
                };
                existing.PropertyChanged += OnFleetShipChanged;
                OwnedShips.Add(existing);
            }
            else
            {
                existing.Quantity++;
            }

            if (SaveFleet())
                StatusMessage = $"{catalogShip.Name} added to your fleet.";
        }

        private void RemoveShip(FleetShipEntry? ship)
        {
            if (ship == null || !OwnedShips.Remove(ship)) return;
            ship.PropertyChanged -= OnFleetShipChanged;
            if (SaveFleet())
                StatusMessage = $"{ship.Name} removed from your fleet.";
        }

        private void FilterCatalog()
        {
            var query = SearchQuery.Trim();
            var matches = Catalog.Where(ship =>
                query.Length == 0 ||
                ship.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                ship.GameName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                ship.Manufacturer.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                ship.Role.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                ship.AvailabilityStatus.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                ship.PledgePrice.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                ship.AuecPrice.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                ship.CrewRange.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                ship.MaxBoxSize.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                ship.LoadingSupport.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                ship.BestForLoadout.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

            VisibleCatalog.Clear();
            foreach (var ship in matches)
                VisibleCatalog.Add(ship);
        }

        private static ObservableCollection<FleetCatalogShip> LoadCatalog()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "ship-catalog.json");
            if (!File.Exists(path))
                throw new FileNotFoundException("The spacecraft catalog was not packaged with the dashboard.", path);

            var catalog = JsonSerializer.Deserialize<List<FleetCatalogShip>>(
                File.ReadAllText(path),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (catalog == null || catalog.Count == 0)
                throw new InvalidDataException($"The spacecraft catalog is empty or invalid: {path}");

            return new ObservableCollection<FleetCatalogShip>(catalog);
        }

        private void OnFleetShipChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(FleetShipCount));
            OnPropertyChanged(nameof(FleetCapacity));
            OnPropertyChanged(nameof(CargoLoaded));
            SaveFleet();
        }

        private bool SaveFleet()
        {
            OnPropertyChanged(nameof(FleetShipCount));
            OnPropertyChanged(nameof(FleetCapacity));
            OnPropertyChanged(nameof(CargoLoaded));
            try
            {
                LocalFeatureStore.Write(StoreFile, OwnedShips);
                return true;
            }
            catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
            {
                StatusMessage = $"Fleet changed for this session but could not be saved locally: {ex.Message}";
                return false;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public sealed class FleetCatalogShip
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string GameName { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int CargoCapacityScu { get; set; }
        public string AvailabilityStatus { get; set; } = string.Empty;
        public string PledgePrice { get; set; } = "Not listed in bundled source";
        public string AuecPrice { get; set; } = "Not listed in bundled source";
        public string CrewRange { get; set; } = "Not listed";
        public string MaxBoxSize { get; set; } = "Not listed";
        public string LoadingSupport { get; set; } = "Not listed";
        public string BestForLoadout { get; set; } = string.Empty;
        public string RsiSpecsUrl { get; set; } = string.Empty;
        public Uri RsiSpecsUri => Uri.TryCreate(RsiSpecsUrl, UriKind.Absolute, out var uri) &&
                                  uri.Scheme == Uri.UriSchemeHttps &&
                                  uri.Host.Equals("robertsspaceindustries.com", StringComparison.OrdinalIgnoreCase)
            ? uri
            : new Uri($"https://robertsspaceindustries.com/pledge/ships?search={Uri.EscapeDataString(GameName)}");
    }

    public sealed class FleetShipEntry : INotifyPropertyChanged
    {
        private int _quantity = 1;
        private int _cargoLoadedScu;
        private string _currentLocation = "Unassigned";
        private string _assignment = "Available";

        public string Name { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int CargoCapacityScu { get; set; }
        public int Quantity { get => _quantity; set => Set(ref _quantity, Math.Max(1, value)); }
        public int CargoLoadedScu
        {
            get => _cargoLoadedScu;
            set => Set(ref _cargoLoadedScu, Math.Clamp(value, 0, CargoCapacityScu));
        }
        public string CurrentLocation { get => _currentLocation; set => Set(ref _currentLocation, value); }
        public string Assignment { get => _assignment; set => Set(ref _assignment, value); }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
