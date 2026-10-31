using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using ScipDashboard.Core.Data;
using ScipDashboard.Core.Models;
using ScipDashboard.Core.Services;
using ScipDashboard.WPF;

namespace ScipDashboard.WPF.ViewModels
{
    public class MarketViewModel : INotifyPropertyChanged
    {
        private readonly IMarketDataService _marketDataService;
        private readonly IDataService _dataService;
        private string _searchQuery = string.Empty;
        private string _selectedCategory = "All Categories";
        private string _selectedLocation = "All Locations";
        private string _sortBy = "Margin %";
        private bool _sortDescending = true;
        private bool _isLoading;
        private bool _isRefreshing;
        private string _statusMessage = "Ready";
        private DataSourceStatus _dataStatus = DataSourceStatus.Sample;
        private DateTime _lastUpdated = DateTime.MinValue;
        private CommodityViewModel? _selectedCommodity;
        private bool _isFavoritesOnly;
        private bool _hasCommodities;

        // Quick stats
        private string _bestGainName = "—";
        private string _bestGainMargin = "—";
        private string _bestGainVolume = "—";
        private string _bestGainRisk = "—";
        private string _biggestLossName = "—";
        private string _biggestLossMargin = "—";
        private string _biggestLossVolume = "—";
        private string _biggestLossRisk = "—";
        private string _mostTradedName = "—";
        private string _mostTradedMargin = "—";
        private string _mostTradedVolume = "—";
        private string _mostTradedRisk = "—";

        public MarketViewModel(IMarketDataService marketDataService, IDataService dataService)
        {
            _marketDataService = marketDataService;
            _dataService = dataService;

            Commodities = new ObservableCollection<CommodityViewModel>();
            Categories = new ObservableCollection<string>
            {
                "All Categories",
                "Agricultural",
                "Bio / Contraband",
                "Commodity",
                "Gas / Fuel",
                "Metal / Ore",
                "Salvage / Industrial"
            };
            Locations = new ObservableCollection<string> { "All Locations" };
            SortOptions = new ObservableCollection<string>
            {
                "Margin %",
                "Best Trend",
                "Risk",
                "Sell Price",
                "Buy Price",
                "Volume",
                "Commodity A→Z",
                "Category"
            };
            Watchlist = new ObservableCollection<WatchlistItem>();

            SearchCommand = new RelayCommand(_ => FilterCommodities());
            RefreshCommand = new RelayCommand(async _ => await ExecuteRefreshAsync());
            ToggleWatchlistCommand = new RelayCommand<string?>(ExecuteToggleWatchlist);
            ClearFiltersCommand = new RelayCommand(_ => ExecuteClearFilters(null));
            ToggleFavoritesFilterCommand = new RelayCommand(_ => ToggleFavoritesFilter());
            ExpandRowCommand = new RelayCommand<string?>(ExecuteExpandRow);
            FilterByCategoryCommand = new RelayCommand<string>(ExecuteFilterByCategory);
            ToggleSortCommand = new RelayCommand(_ => ToggleSortDirection());

            LoadDataAsync();
        }

        public ObservableCollection<CommodityViewModel> Commodities { get; }
        public bool HasCommodities
        {
            get => _hasCommodities;
            set { if (_hasCommodities != value) { _hasCommodities = value; OnPropertyChanged(); } }
        }
        public ObservableCollection<string> Categories { get; }
        public ObservableCollection<string> Locations { get; }
        public ObservableCollection<string> SortOptions { get; }
        public ObservableCollection<Core.Models.WatchlistItem> Watchlist { get; }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (_searchQuery != value)
                {
                    _searchQuery = value;
                    OnPropertyChanged();
                    FilterCommodities();
                }
            }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory != value)
                {
                    _selectedCategory = value;
                    OnPropertyChanged();
                    FilterCommodities();
                }
            }
        }

        public string SelectedLocation
        {
            get => _selectedLocation;
            set
            {
                if (_selectedLocation != value)
                {
                    _selectedLocation = value;
                    OnPropertyChanged();
                    FilterCommodities();
                }
            }
        }

        public string SortBy
        {
            get => _sortBy;
            set
            {
                if (_sortBy != value)
                {
                    _sortBy = value;
                    OnPropertyChanged();
                    SortCommodities();
                }
            }
        }

        public bool SortDescending
        {
            get => _sortDescending;
            set
            {
                if (_sortDescending != value)
                {
                    _sortDescending = value;
                    OnPropertyChanged();
                    SortCommodities();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { if (_isLoading != value) { _isLoading = value; OnPropertyChanged(); } }
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set { if (_isRefreshing != value) { _isRefreshing = value; OnPropertyChanged(); } }
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

        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set { if (_lastUpdated != value) { _lastUpdated = value; OnPropertyChanged(); } }
        }

        public SolidColorBrush StatusColor => DataStatus switch
        {
            DataSourceStatus.Live => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E")),
            DataSourceStatus.Cached => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5B32F")),
            DataSourceStatus.Sample => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
            _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"))
        };

        public CommodityViewModel? SelectedCommodity
        {
            get => _selectedCommodity;
            set
            {
                if (_selectedCommodity != value)
                {
                    _selectedCommodity = value;
                    OnPropertyChanged();
                    if (value != null)
                    {
                        LoadStationDetail(value.Id);
                    }
                }
            }
        }

        public bool IsFavoritesOnly
        {
            get => _isFavoritesOnly;
            set { if (_isFavoritesOnly != value) { _isFavoritesOnly = value; OnPropertyChanged(); FilterCommodities(); } }
        }

        // Quick stats
        public string BestGainName { get => _bestGainName; set { _bestGainName = value; OnPropertyChanged(); } }
        public string BestGainMargin { get => _bestGainMargin; set { _bestGainMargin = value; OnPropertyChanged(); } }
        public string BestGainVolume { get => _bestGainVolume; set { _bestGainVolume = value; OnPropertyChanged(); } }
        public string BestGainRisk { get => _bestGainRisk; set { _bestGainRisk = value; OnPropertyChanged(); } }
        public string BiggestLossName { get => _biggestLossName; set { _biggestLossName = value; OnPropertyChanged(); } }
        public string BiggestLossMargin { get => _biggestLossMargin; set { _biggestLossMargin = value; OnPropertyChanged(); } }
        public string BiggestLossVolume { get => _biggestLossVolume; set { _biggestLossVolume = value; OnPropertyChanged(); } }
        public string BiggestLossRisk { get => _biggestLossRisk; set { _biggestLossRisk = value; OnPropertyChanged(); } }
        public string MostTradedName { get => _mostTradedName; set { _mostTradedName = value; OnPropertyChanged(); } }
        public string MostTradedMargin { get => _mostTradedMargin; set { _mostTradedMargin = value; OnPropertyChanged(); } }
        public string MostTradedVolume { get => _mostTradedVolume; set { _mostTradedVolume = value; OnPropertyChanged(); } }
        public string MostTradedRisk { get => _mostTradedRisk; set { _mostTradedRisk = value; OnPropertyChanged(); } }

        public RelayCommand SearchCommand { get; }
        public RelayCommand RefreshCommand { get; }
        public RelayCommand<string?> ToggleWatchlistCommand { get; }
        public RelayCommand ClearFiltersCommand { get; }
        public RelayCommand ToggleFavoritesFilterCommand { get; }
        public RelayCommand<string?> ExpandRowCommand { get; }
        public RelayCommand<string> FilterByCategoryCommand { get; }
        public RelayCommand ToggleSortCommand { get; }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            StatusMessage = "Loading market data...";

            try
            {
                // Load immediately from local data service (seeded sample data)
                var marketData = await _dataService.GetMarketDataAsync();

                DataStatus = DataSourceStatus.Sample;
                LastUpdated = DateTime.UtcNow;

                MapMarketDataToViewModels(marketData);
                HasCommodities = Commodities.Count > 0;
                UpdateCategoriesAndLocations(marketData);
                UpdateQuickStats();

                StatusMessage = marketData.Commodities.Count > 0
                    ? $"{marketData.Commodities.Count} commodities loaded"
                    : "No commodities found";

                // Background refresh from UEXCorp (non-blocking)
                try
                {
                    await _marketDataService.RefreshFromUEXCorpAsync();
                    // Reload with live data
                    var liveData = await _dataService.GetMarketDataAsync();
                    MapMarketDataToViewModels(liveData);
                    HasCommodities = Commodities.Count > 0;
                    DataStatus = _marketDataService.CurrentStatus;
                    LastUpdated = _marketDataService.LastRefresh;
                    UpdateCategoriesAndLocations(liveData);
                    UpdateQuickStats();
                    StatusMessage = $"Market data refreshed ({liveData.Commodities.Count} commodities)";
                }
                catch
                {
                    // Keep sample data if live refresh fails
                }
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

        private async Task ExecuteRefreshAsync()
        {
            IsRefreshing = true;
            StatusMessage = "Refreshing from UEXCorp...";

            try
            {
                await _marketDataService.RefreshFromUEXCorpAsync();
                await LoadDataAsync();
                StatusMessage = "Market data refreshed";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Refresh failed: {ex.Message}";
                DataStatus = DataSourceStatus.Unavailable;
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private void MapMarketDataToViewModels(MarketData marketData)
        {
            Commodities.Clear();

            var locations = marketData.Locations.ToDictionary(l => l.Id, l => l.Name);

            foreach (var commodity in marketData.Commodities)
            {
                var observations = marketData.PriceObservations
                    .Where(o => o.CommodityId == commodity.Id)
                    .ToList();

                if (observations.Count == 0)
                {
                    continue;
                }

                var bestBuy = observations.OrderBy(o => o.BuyPrice).FirstOrDefault();
                var bestSell = observations.OrderByDescending(o => o.SellPrice).FirstOrDefault();

                if (bestBuy == null || bestSell == null) continue;

                var margin = CalculateMargin(bestBuy.BuyPrice, bestSell.SellPrice);
                var volume = observations.Sum(o => o.Volume);
                var demandVolume = observations.Sum(o => o.DemandVolume);
                var buyStock = observations.Sum(o => o.Volume);
                var sellDemand = observations.Sum(o => o.DemandVolume);

                var buyLoc = locations.TryGetValue(bestBuy.LocationId, out var buyLocName) ? buyLocName : bestBuy.LocationName;
                var sellLoc = locations.TryGetValue(bestSell.LocationId, out var sellLocName) ? sellLocName : bestSell.LocationName;

                var risk = CalculateRisk(margin, volume, demandVolume);
                var trend = CalculateTrend(margin);

                Commodities.Add(new CommodityViewModel
                {
                    Id = commodity.Id,
                    Name = commodity.Name,
                    DisplayName = commodity.DisplayName,
                    Category = commodity.Category,
                    Icon = GetCommodityIcon(commodity.Name),
                    IsIllegal = commodity.IsIllegal,
                    IsOnWatchlist = Watchlist.Any(w => w.CommodityId == commodity.Id),
                    Status = commodity.SourceStatus,
                    BuyPrice = bestBuy.BuyPrice,
                    BuyLocation = buyLoc,
                    SellPrice = bestSell.SellPrice,
                    SellLocation = sellLoc,
                    Margin = margin,
                    Volume = volume,
                    DemandVolume = demandVolume,
                    BuyStock = buyStock,
                    SellDemand = sellDemand,
                    LastUpdated = observations.Max(o => o.Timestamp),
                    Risk = risk,
                    Trend = trend,
                    StationObservations = observations
                        .OrderByDescending(o => o.SellPrice - o.BuyPrice)
                        .Select(o => new StationObservation
                        {
                            LocationName = o.LocationName,
                            SystemName = o.SystemName,
                            BuyPrice = o.BuyPrice,
                            SellPrice = o.SellPrice,
                            Margin = CalculateMargin(o.BuyPrice, o.SellPrice),
                            Volume = o.Volume,
                            DemandVolume = o.DemandVolume,
                            Timestamp = o.Timestamp,
                            SourceName = o.SourceName
                        }).ToList()
                });
            }

            SortCommodities();
        }

        private static int CalculateRisk(decimal margin, int volume, int demand)
        {
            var risk = 50;
            if (margin > 50) risk -= 20;
            else if (margin > 25) risk -= 10;
            else if (margin > 0) risk += 10;
            else risk += 30;

            if (volume > 10000) risk -= 10;
            else if (volume < 1000) risk += 10;

            if (demand > 10000) risk -= 10;
            else if (demand < 100) risk += 10;

            return Math.Clamp(risk, 0, 100);
        }

        private static string CalculateTrend(decimal margin)
        {
            return margin switch
            {
                > 50 => "Strong Buy",
                > 20 => "Weak Buy",
                > 0 => "Marginal",
                _ => "Avoid"
            };
        }

        private static string RiskLabel(int risk)
        {
            return risk switch
            {
                < 25 => "Low",
                < 50 => "Moderate",
                < 75 => "High",
                _ => "Extreme"
            };
        }

        private void UpdateCategoriesAndLocations(MarketData marketData)
        {
            var categories = marketData.Commodities
                .Select(c => c.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            var existingCats = Categories.Where(c => c != "All Categories").ToList();
            foreach (var cat in existingCats)
                Categories.Remove(cat);
            foreach (var cat in categories)
            {
                if (!Categories.Contains(cat))
                    Categories.Add(cat);
            }

            var locationNames = marketData.Locations
                .Select(l => l.Name)
                .Distinct()
                .OrderBy(l => l)
                .ToList();

            var existingLocs = Locations.Where(l => l != "All Locations").ToList();
            foreach (var loc in existingLocs)
                Locations.Remove(loc);
            foreach (var loc in locationNames)
            {
                if (!Locations.Contains(loc))
                    Locations.Add(loc);
            }
        }

        private void UpdateQuickStats()
        {
            var withMargin = Commodities.Where(c => c.Margin > 0).OrderByDescending(c => c.Margin).ToList();
            var withLoss = Commodities.Where(c => c.Margin < 0).OrderBy(c => c.Margin).ToList();
            var mostTraded = Commodities.OrderByDescending(c => c.Volume).FirstOrDefault();

            if (withMargin.Count > 0)
            {
                var best = withMargin.First();
                BestGainName = best.Name;
                BestGainMargin = $"+{best.Margin:0.##}%";
                BestGainVolume = best.Volume.ToString("#,##0");
                BestGainRisk = $"{best.Risk}/100 ({RiskLabel(best.Risk)})";
            }

            if (withLoss.Count > 0)
            {
                var worst = withLoss.First();
                BiggestLossName = worst.Name;
                BiggestLossMargin = $"{worst.Margin:0.##}%";
                BiggestLossVolume = worst.Volume.ToString("#,##0");
                BiggestLossRisk = $"{worst.Risk}/100 ({RiskLabel(worst.Risk)})";
            }

            if (mostTraded != null)
            {
                MostTradedName = mostTraded.Name;
                MostTradedMargin = $"{mostTraded.Margin:0.##}%";
                MostTradedVolume = mostTraded.Volume.ToString("#,##0");
                MostTradedRisk = $"{mostTraded.Risk}/100 ({RiskLabel(mostTraded.Risk)})";
            }
        }

        private async Task LoadWatchlistAsync()
        {
            var watchlist = await _dataService.GetWatchlistAsync();
            Watchlist.Clear();
            foreach (var item in watchlist)
                Watchlist.Add(item);
        }

        private async Task LoadStationDetail(string commodityId)
        {
            // Station detail is already loaded in StationObservations; just refresh watchlist state
            var commodity = Commodities.FirstOrDefault(c => c.Id == commodityId);
            if (commodity != null)
                commodity.IsOnWatchlist = Watchlist.Any(w => w.CommodityId == commodityId);
        }

        private void FilterCommodities()
        {
            var view = CollectionViewSource.GetDefaultView(Commodities);
            if (view == null) return;

            view.Filter = item =>
            {
                var c = (CommodityViewModel)item;

                if (!string.IsNullOrWhiteSpace(SearchQuery) &&
                    !c.Name.ToLower().Contains(SearchQuery.ToLower()) &&
                    !c.Category.ToLower().Contains(SearchQuery.ToLower()))
                    return false;

                if (SelectedCategory != "All Categories" && c.Category != SelectedCategory)
                    return false;

                if (SelectedLocation != "All Locations" && c.BuyLocation != SelectedLocation)
                    return false;

                if (IsFavoritesOnly && !c.IsOnWatchlist)
                    return false;

                return true;
            };

            view.Refresh();
            OnPropertyChanged(nameof(HasCommodities));
        }

        private void SortCommodities()
        {
            var view = CollectionViewSource.GetDefaultView(Commodities);
            if (view == null) return;

            view.SortDescriptions.Clear();

            var direction = _sortDescending ? ListSortDirection.Descending : ListSortDirection.Ascending;

            switch (_sortBy)
            {
                case "Margin %":
                    view.SortDescriptions.Add(new SortDescription(nameof(CommodityViewModel.Margin), direction));
                    break;
                case "Best Trend":
                    view.SortDescriptions.Add(new SortDescription(nameof(CommodityViewModel.Margin), direction));
                    break;
                case "Risk":
                    view.SortDescriptions.Add(new SortDescription(nameof(CommodityViewModel.Risk), direction));
                    break;
                case "Sell Price":
                    view.SortDescriptions.Add(new SortDescription(nameof(CommodityViewModel.SellPrice), direction));
                    break;
                case "Buy Price":
                    view.SortDescriptions.Add(new SortDescription(nameof(CommodityViewModel.BuyPrice), direction));
                    break;
                case "Volume":
                    view.SortDescriptions.Add(new SortDescription(nameof(CommodityViewModel.Volume), direction));
                    break;
                case "Commodity A→Z":
                    view.SortDescriptions.Add(new SortDescription(nameof(CommodityViewModel.Name), direction));
                    break;
                case "Category":
                    view.SortDescriptions.Add(new SortDescription(nameof(CommodityViewModel.Category), direction));
                    break;
            }

            view.Refresh();
        }

        private void ToggleFavoritesFilter() => IsFavoritesOnly = !IsFavoritesOnly;

        private void ExecuteFilterByCategory(string? category)
        {
            if (category == null) return;
            SelectedCategory = category;
        }

        private void ToggleSortDirection() => SortDescending = !SortDescending;

        private void ExecuteClearFilters(object? parameter)
        {
            SearchQuery = string.Empty;
            SelectedCategory = "All Categories";
            SelectedLocation = "All Locations";
            IsFavoritesOnly = false;
        }

        private async void ExecuteToggleWatchlist(string? commodityId)
        {
            if (string.IsNullOrEmpty(commodityId)) return;

            var commodity = Commodities.FirstOrDefault(c => c.Id == commodityId);
            if (commodity == null) return;

            var existing = Watchlist.FirstOrDefault(w => w.CommodityId == commodityId);

            if (existing != null)
            {
                await _dataService.RemoveFromWatchlistAsync(existing.Id);
                Watchlist.Remove(existing);
                commodity.IsOnWatchlist = false;
                StatusMessage = $"{commodity.Name} removed from watchlist";
            }
            else
            {
                var newItem = new Core.Models.WatchlistItem
                {
                    CommodityId = commodityId,
                    TargetBuyPrice = commodity.BuyPrice,
                    TargetSellPrice = commodity.SellPrice,
                    CreatedAt = DateTime.UtcNow,
                    NotifyOnPriceChange = true,
                    Type = Core.Models.WatchlistItem.ItemType.Commodity
                };

                await _dataService.AddToWatchlistAsync(newItem);
                Watchlist.Add(newItem);
                commodity.IsOnWatchlist = true;
                StatusMessage = $"{commodity.Name} added to watchlist";
            }
        }

        private void ExecuteExpandRow(string? commodityId)
        {
            if (commodityId == null) return;
            SelectedCommodity = Commodities.FirstOrDefault(c => c.Id == commodityId);
        }

        private string GetCommodityIcon(string name)
        {
            return name.ToLower() switch
            {
                "gold" => "⛏️",
                "titanium" => "⚙️",
                "diamonds" or "diamond" => "💎",
                "quantanium" or "quantium" => "⚗️",
                "platinum" or "palladium" => "🪨",
                "aluminum" or "copper" or "tungsten" => "🔧",
                "laranite" or "bexalite" or "borase" => "✨",
                _ when name.ToLower().Contains("agri") => "🌾",
                _ when name.ToLower().Contains("scrap") => "🗑️",
                _ when name.ToLower().Contains("stim") => "💉",
                _ when name.ToLower().Contains("wine") => "🍷",
                _ when name.ToLower().Contains("whiskey") => "🥃",
                _ when name.ToLower().Contains("coffee") => "☕",
                _ => "📦"
            };
        }

        private static decimal CalculateMargin(decimal buyPrice, decimal sellPrice)
        {
            if (buyPrice <= 0) return 0;
            return Math.Round(((sellPrice - buyPrice) / buyPrice) * 100, 2);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CommodityViewModel : INotifyPropertyChanged
    {
        private bool _isOnWatchlist;
        private bool _isExpanded;

        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public decimal BuyPrice { get; set; } = 0m;
        public decimal SellPrice { get; set; } = 0m;
        public decimal Margin { get; set; } = 0m;
        public decimal MarginPercent => BuyPrice > 0 ? Math.Round(((SellPrice - BuyPrice) / BuyPrice) * 100, 2) : 0m;
        public string BuyLocation { get; set; } = string.Empty;
        public string SellLocation { get; set; } = string.Empty;
        public int Volume { get; set; } = 0;
        public int DemandVolume { get; set; } = 0;
        public int BuyStock { get; set; } = 0;
        public int SellDemand { get; set; } = 0;
        public DateTime LastUpdated { get; set; } = DateTime.MinValue;
        public bool IsIllegal { get; set; } = false;
        public DataSourceStatus Status { get; set; } = DataSourceStatus.Sample;
        public int Risk { get; set; } = 50;
        public string Trend { get; set; } = "Marginal";
        public List<StationObservation> StationObservations { get; set; } = new();

        public bool IsOnWatchlist
        {
            get => _isOnWatchlist;
            set { if (_isOnWatchlist != value) { _isOnWatchlist = value; OnPropertyChanged(); } }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set { if (_isExpanded != value) { _isExpanded = value; OnPropertyChanged(); } }
        }

        public string SupplyDemandText => $"S{BuyStock:#,##0}D{SellDemand:#,##0}";
        public string RiskText => $"{Risk}/100";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class StationObservation
    {
        public string LocationName { get; set; } = string.Empty;
        public string SystemName { get; set; } = string.Empty;
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
        public decimal Margin { get; set; }
        public int Volume { get; set; }
        public int DemandVolume { get; set; }
        public DateTime Timestamp { get; set; }
        public string SourceName { get; set; } = string.Empty;
        public string MarginText => $"{Margin:0.##}%";
    }
}
