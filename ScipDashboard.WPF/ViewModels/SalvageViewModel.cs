using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using ScipDashboard.Core.Data;
using ScipDashboard.Core.Models;
using ScipDashboard.Core.Services;

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

        public ObservableCollection<SalvageLocationViewModel> Locations { get; } = new();
        public ObservableCollection<string> LocationNames { get; } = new() { "All Locations" };

        public string SearchQuery
        {
            get => _searchQuery;
            set { if (_searchQuery != value) { _searchQuery = value; OnPropertyChanged(); FilterLocations(); } }
        }

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
            Locations.CollectionChanged += (s, e) => OnPropertyChanged(nameof(IsEmpty));
            LoadDataAsync();
        }

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
                           c.Category.Equals("Salvage / Industrial", StringComparison.OrdinalIgnoreCase))
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
                    LastUpdated = salvageObs.Max(o => o.Timestamp)
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

    public class SalvageLocationViewModel : INotifyPropertyChanged
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

        public string RiskText => $"{Risk}/100";
        public string YieldText => $"{TotalYield:#,##0} SCU";
        public string DemandText => $"{TotalDemand:#,##0} SCU";
        public string ProfitText => $"+{AverageProfitPercent:0.##}%";
        public string CargoFillText => $"{CargoFillPercent:0.##}%";

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
