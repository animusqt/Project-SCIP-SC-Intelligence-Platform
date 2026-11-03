using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ScipDashboard.Core.Data;
using ScipDashboard.Core.Models;
using ScipDashboard.Core.Services;
using ScipDashboard.WPF.Services;

namespace ScipDashboard.WPF.ViewModels
{
    public class SalvageViewModel : INotifyPropertyChanged
    {
        private readonly IDataService _dataService;
        private string _searchQuery = string.Empty;
        private string _selectedLocation = "All Locations";
        private bool _isLoading;
        private string _statusMessage = "Ready";
        private DataSourceStatus _dataStatus = DataSourceStatus.Sample;
        private string _operationType = "Salvage";
        private string _operationLocation = string.Empty;
        private string _operationResource = string.Empty;
        private int _operationYieldScu;
        private int _operationDurationMinutes = 60;
        private decimal _operationPricePerScu;
        private string _selectedHazardBand = "Low risk";
        private string _selectedTargetType = "Medium hull";
        private SalvageHullOption _selectedSalvageHull;

        public ObservableCollection<SalvageLocationViewModel> Locations { get; } = new();
        public ObservableCollection<string> LocationNames { get; } = new() { "All Locations" };
        public ObservableCollection<string> ActivityTypes { get; } = new() { "Salvage", "Mining" };
        public ObservableCollection<string> HazardBands { get; } = new() { "Low risk", "Moderate risk", "High risk", "Extreme risk" };
        public ObservableCollection<string> TargetTypes { get; } = new()
        {
            "Small hull", "Medium hull", "Large hull", "Capital wreck", "Asteroid deposit", "Quantanium deposit"
        };
        public ObservableCollection<SalvageHullOption> SalvageHulls { get; } = new()
        {
            new("Drake Vulture", 12, "Salvage"),
            new("Drake Golem", 6, "Salvage"),
            new("Aegis Reclaimer", 420, "Salvage"),
            new("Argo SRV", 0, "Support"),
            new("Drake Caterpillar", 576, "Hauling"),
            new("MISC Prospector", 32, "Mining"),
            new("Argo MOLE", 96, "Mining")
        };
        public ObservableCollection<SalvageComponentReference> ComponentReferences { get; } = new()
        {
            new("Recycled Material Composite", "Material", "12 aUEC / SCU", "Price reference; confirm before selling."),
            new("Construction Materials", "Material", "6 aUEC / SCU", "Munching yield depends on ship and target."),
            new("Size 1 shield generator", "Shield", "2,600 aUEC", "Estimated component value."),
            new("Size 2 shield generator", "Shield", "9,500 aUEC", "Estimated component value."),
            new("Size 3 shield generator", "Shield", "34,000 aUEC", "Estimated component value."),
            new("Size 2 cooler", "Cooler", "5,200 aUEC", "Remove before hull scraping."),
            new("Size 2 power plant", "Power", "7,800 aUEC", "Estimated component value."),
            new("Size 2 quantum drive", "Quantum", "14,500 aUEC", "Estimated component value.")
        };
        public ObservableCollection<OperationRunEntry> OperationRuns { get; } =
            LocalFeatureStore.Read<ObservableCollection<OperationRunEntry>>("operations.json");
        public ICommand AddOperationCommand { get; }
        public ICommand RemoveOperationCommand { get; }

        public string SearchQuery
        {
            get => _searchQuery;
            set { if (_searchQuery != value) { _searchQuery = value; OnPropertyChanged(); FilterLocations(); } }
        }

        public sealed record SalvageHullOption(string Name, int CargoCapacityScu, string Role)
        {
            public string DisplayText => CargoCapacityScu > 0
                ? $"{Name} · {CargoCapacityScu:N0} SCU · {Role}"
                : $"{Name} · support hull · {Role}";
        }

        public sealed record SalvageComponentReference(string Name, string Category, string Value, string Notes);

        public string SelectedLocation
        {
            get => _selectedLocation;
            set { if (_selectedLocation != value) { _selectedLocation = value; OnPropertyChanged(); FilterLocations(); } }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { if (_isLoading != value) { _isLoading = value; OnPropertyChanged(); } }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(); } }
        }

        public DataSourceStatus DataStatus
        {
            get => _dataStatus;
            set { if (_dataStatus != value) { _dataStatus = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusColor)); } }
        }

        public bool IsEmpty => Locations.Count == 0;
        public int RecordedRuns => OperationRuns.Count;
        public decimal TotalEstimatedProfit => OperationRuns.Sum(run => run.EstimatedProfit);
        public decimal AverageEstimatedProfitPerHour => OperationRuns.Count == 0
            ? 0m
            : OperationRuns.Average(run => run.EstimatedProfitPerHour);

        public string OperationType
        {
            get => _operationType;
            set { if (_operationType != value) { _operationType = value; OnPropertyChanged(); } }
        }

        public string OperationLocation
        {
            get => _operationLocation;
            set { if (_operationLocation != value) { _operationLocation = value; OnPropertyChanged(); } }
        }

        public string OperationResource
        {
            get => _operationResource;
            set { if (_operationResource != value) { _operationResource = value; OnPropertyChanged(); } }
        }

        public int OperationYieldScu
        {
            get => _operationYieldScu;
            set
            {
                if (_operationYieldScu != value)
                {
                    _operationYieldScu = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(OperationEstimatedProfit));
                    OnPropertyChanged(nameof(PlannedCargoTrips));
                }
            }
        }

        public int OperationDurationMinutes
        {
            get => _operationDurationMinutes;
            set { if (_operationDurationMinutes != value) { _operationDurationMinutes = value; OnPropertyChanged(); OnPropertyChanged(nameof(OperationEstimatedProfitPerHour)); } }
        }

        public decimal OperationPricePerScu
        {
            get => _operationPricePerScu;
            set { if (_operationPricePerScu != value) { _operationPricePerScu = value; OnPropertyChanged(); OnPropertyChanged(nameof(OperationEstimatedProfit)); OnPropertyChanged(nameof(OperationEstimatedProfitPerHour)); } }
        }

        public decimal OperationEstimatedProfit => Math.Max(0, OperationYieldScu) * Math.Max(0, OperationPricePerScu);
        public decimal OperationEstimatedProfitPerHour => OperationDurationMinutes <= 0
            ? 0m
            : OperationEstimatedProfit * 60m / OperationDurationMinutes;

        public System.Windows.Media.SolidColorBrush StatusColor => DataStatus switch
        {
            DataSourceStatus.Live => new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#22C55E")),
            DataSourceStatus.Cached => new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F5B32F")),
            DataSourceStatus.Sample => new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#94A3B8")),
            _ => new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#EF4444"))
        };

        public SalvageViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _selectedSalvageHull = SalvageHulls[0];
            Locations.CollectionChanged += (s, e) => OnPropertyChanged(nameof(IsEmpty));
            AddOperationCommand = new RelayCommand(AddOperation);
            RemoveOperationCommand = new RelayCommand<OperationRunEntry?>(RemoveOperation);
            LoadDataAsync();
        }

        public string SelectedHazardBand
        {
            get => _selectedHazardBand;
            set { if (_selectedHazardBand != value) { _selectedHazardBand = value; OnPropertyChanged(); } }
        }

        public string SelectedTargetType
        {
            get => _selectedTargetType;
            set { if (_selectedTargetType != value) { _selectedTargetType = value; OnPropertyChanged(); } }
        }

        public SalvageHullOption SelectedSalvageHull
        {
            get => _selectedSalvageHull;
            set
            {
                if (_selectedSalvageHull == value) return;
                _selectedSalvageHull = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlannedCargoTrips));
                OnPropertyChanged(nameof(SelectedHullCapacityText));
            }
        }

        public int PlannedCargoTrips => SelectedSalvageHull.CargoCapacityScu <= 0
            ? 0
            : Math.Max(1, (int)Math.Ceiling((double)Math.Max(0, OperationYieldScu) / SelectedSalvageHull.CargoCapacityScu));
        public string SelectedHullCapacityText => SelectedSalvageHull.CargoCapacityScu <= 0
            ? "No cargo hold listed"
            : $"{SelectedSalvageHull.CargoCapacityScu:N0} SCU hold";

        private async void LoadDataAsync()
        {
            IsLoading = true;
            StatusMessage = "Loading salvage data...";

            try
            {
                var marketData = await _dataService.GetMarketDataAsync();
                MapSalvageData(marketData);
                UpdateLocationList(marketData);
                StatusMessage = $"Salvage data loaded ({Locations.Count} locations)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                DataStatus = DataSourceStatus.Unavailable;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void MapSalvageData(MarketData marketData)
        {
            Locations.Clear();

            // Salvage locations are those with scrap/salvage commodities
            var salvageCommodities = marketData.Commodities
                .Where(c => c.Category.Equals("Salvage", StringComparison.OrdinalIgnoreCase) ||
                           c.Category.Equals("Salvage / Industrial", StringComparison.OrdinalIgnoreCase) ||
                           c.Category.Contains("Mineral", StringComparison.OrdinalIgnoreCase) ||
                           c.Category.Contains("Ore", StringComparison.OrdinalIgnoreCase) ||
                           c.Category.Contains("Mining", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var location in marketData.Locations.Where(l => l.HasCommoditiesMarket))
            {
                var locationObservations = marketData.PriceObservations
                    .Where(o => o.LocationId == location.Id)
                    .ToList();

                var salvageObs = locationObservations
                    .Where(o => salvageCommodities.Any(c => c.Id == o.CommodityId))
                    .ToList();

                if (salvageObs.Count == 0) continue;

                var totalYield = salvageObs.Sum(o => o.Volume);
                var totalDemand = salvageObs.Sum(o => o.DemandVolume);
                var avgProfit = salvageObs.Any() 
                    ? Math.Round(salvageObs.Average(o => CalculateMargin(o.BuyPrice, o.SellPrice)), 2)
                    : 0m;

                Locations.Add(new SalvageLocationViewModel
                {
                    LocationId = location.Id,
                    LocationName = location.Name,
                    SystemName = location.SystemName,
                    SalvageItems = salvageObs.Count,
                    TotalYield = totalYield,
                    TotalDemand = totalDemand,
                    AverageProfitPercent = avgProfit,
                    CargoFillPercent = totalDemand > 0 ? Math.Min(100, (double)(totalYield * 100.0 / totalDemand)) : 0,
                    Risk = CalculateRisk(avgProfit, totalYield),
                    LastUpdated = salvageObs.Max(o => o.Timestamp),
                    ActivityType = salvageObs.Any(observation =>
                        marketData.Commodities.Any(commodity => commodity.Id == observation.CommodityId &&
                            (commodity.Category.Contains("Mineral", StringComparison.OrdinalIgnoreCase) ||
                             commodity.Category.Contains("Ore", StringComparison.OrdinalIgnoreCase) ||
                             commodity.Category.Contains("Mining", StringComparison.OrdinalIgnoreCase))))
                        ? "Mining / Salvage"
                        : "Salvage",
                    TopResource = salvageObs.OrderByDescending(observation => observation.Volume)
                        .Select(observation => observation.CommodityName).FirstOrDefault() ?? "Unlisted"
                });
            }
        }

        private void UpdateLocationList(MarketData marketData)
        {
            var names = marketData.Locations
                .Select(l => l.Name)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            var existing = LocationNames.Where(n => n != "All Locations").ToList();
            foreach (var n in existing) LocationNames.Remove(n);
            foreach (var n in names)
            {
                if (!LocationNames.Contains(n)) LocationNames.Add(n);
            }
        }

        private void FilterLocations()
        {
            // Filtering handled by CollectionView in the view
        }

        private static decimal CalculateMargin(decimal buyPrice, decimal sellPrice)
        {
            if (buyPrice <= 0) return 0;
            return Math.Round(((sellPrice - buyPrice) / buyPrice) * 100, 2);
        }

        private void AddOperation()
        {
            if (string.IsNullOrWhiteSpace(OperationLocation) || string.IsNullOrWhiteSpace(OperationResource) ||
                OperationYieldScu <= 0 || OperationDurationMinutes <= 0 || OperationPricePerScu <= 0)
            {
                StatusMessage = "Enter a location, resource, positive SCU yield, duration, and unit sell estimate.";
                return;
            }

            var operation = new OperationRunEntry
            {
                ActivityType = OperationType,
                Location = OperationLocation.Trim(),
                Resource = OperationResource.Trim(),
                YieldScu = OperationYieldScu,
                DurationMinutes = OperationDurationMinutes,
                PricePerScu = OperationPricePerScu,
                RecordedAt = DateTime.UtcNow
            };
            OperationRuns.Insert(0, operation);
            OperationLocation = string.Empty;
            OperationResource = string.Empty;
            OperationYieldScu = 0;
            OperationPricePerScu = 0m;
            if (SaveOperations())
                StatusMessage = $"{operation.ActivityType} run recorded: {operation.EstimatedProfit:N0} aUEC estimated gross value.";
        }

        private void RemoveOperation(OperationRunEntry? operation)
        {
            if (operation == null || !OperationRuns.Remove(operation)) return;
            if (SaveOperations())
                StatusMessage = "Operation record removed.";
        }

        private bool SaveOperations()
        {
            try
            {
                LocalFeatureStore.Write("operations.json", OperationRuns);
                OnPropertyChanged(nameof(RecordedRuns));
                OnPropertyChanged(nameof(TotalEstimatedProfit));
                OnPropertyChanged(nameof(AverageEstimatedProfitPerHour));
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                StatusMessage = $"Run data changed in memory but could not be saved locally: {ex.Message}";
                return false;
            }
        }

        private static int CalculateRisk(decimal margin, int volume)
        {
            var risk = 50;
            if (margin > 50) risk -= 20;
            else if (margin > 25) risk -= 10;
            else if (margin > 0) risk += 10;
            else risk += 30;

            if (volume > 10000) risk -= 10;
            else if (volume < 1000) risk += 10;

            return Math.Clamp(risk, 0, 100);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SalvageLocationViewModel
    {
        public string LocationId { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string SystemName { get; set; } = string.Empty;
        public int SalvageItems { get; set; }
        public int TotalYield { get; set; }
        public int TotalDemand { get; set; }
        public decimal AverageProfitPercent { get; set; }
        public double CargoFillPercent { get; set; }
        public int Risk { get; set; }
        public DateTime LastUpdated { get; set; }
        public string ActivityType { get; set; } = "Salvage";
        public string TopResource { get; set; } = string.Empty;

        public string RiskText => $"{Risk}/100";
        public string YieldText => $"{TotalYield:#,##0} SCU";
        public string DemandText => $"{TotalDemand:#,##0} SCU";
        public string ProfitText => $"+{AverageProfitPercent:0.##}%";
        public string CargoFillText => $"{CargoFillPercent:0.##}%";
    }

    public sealed class OperationRunEntry
    {
        public string ActivityType { get; set; } = "Salvage";
        public string Location { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public int YieldScu { get; set; }
        public int DurationMinutes { get; set; }
        public decimal PricePerScu { get; set; }
        public DateTime RecordedAt { get; set; }
        public decimal EstimatedProfit => YieldScu * PricePerScu;
        public decimal EstimatedProfitPerHour => DurationMinutes > 0 ? EstimatedProfit * 60m / DurationMinutes : 0m;
    }
}
