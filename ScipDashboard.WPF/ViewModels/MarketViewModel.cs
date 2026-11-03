using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;
using ScipDashboard.Core.Data;
using ScipDashboard.Core.Models;
using ScipDashboard.Core.Services;
using ScipDashboard.WPF;
using ScipDashboard.WPF.Services;

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
        private int _tradeQuantityScu = 1;
        private int _cargoCapacityScu = 100;
        private int _currentCargoScu;
        private string _currentCargoCommodity = string.Empty;
        private decimal _tradeCostBasis;
        private readonly List<MarketPriceHistoryEntry> _marketHistory;
        private string? _marketHistoryError;

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
                "24h Change",
                "Risk",
                "Sell Price",
                "Buy Price",
                "Volume",
                "Commodity A→Z",
                "Category"
            };
            Watchlist = new ObservableCollection<WatchlistItem>();
            var tradeState = LocalFeatureStore.Read<MarketTradeState>("market-trades.json");
            TradeHistory = tradeState.History;
            _currentCargoScu = tradeState.CargoScu;
            _currentCargoCommodity = tradeState.CargoCommodity;
            _tradeCostBasis = tradeState.CostBasisPerScu;
            try
            {
                _marketHistory = LocalFeatureStore.Read<List<MarketPriceHistoryEntry>>("market-history.json");
            }
            catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
            {
                _marketHistory = new List<MarketPriceHistoryEntry>();
                _marketHistoryError = $"Market history could not be loaded: {ex.Message}";
            }

            SearchCommand = new RelayCommand(_ => FilterCommodities());
            RefreshCommand = new RelayCommand(async _ => await ExecuteRefreshAsync());
            ToggleWatchlistCommand = new RelayCommand<string?>(ExecuteToggleWatchlist);
            ClearFiltersCommand = new RelayCommand(_ => ExecuteClearFilters(null));
            ToggleFavoritesFilterCommand = new RelayCommand(_ => ToggleFavoritesFilter());
            ExpandRowCommand = new RelayCommand<string?>(ExecuteExpandRow);
            FilterByCategoryCommand = new RelayCommand<string>(ExecuteFilterByCategory);
            ToggleSortCommand = new RelayCommand(_ => ToggleSortDirection());
            SelectCommodityCommand = new RelayCommand<CommodityViewModel?>(commodity => SelectedCommodity = commodity);
            BuyCommand = new RelayCommand(BuyCargo);
            SellCommand = new RelayCommand(SellCargo);

            Commodities.CollectionChanged += (s, e) => OnPropertyChanged(nameof(IsEmpty));

            _ = LoadDataAsync();
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
        public ObservableCollection<MarketTradeEntry> TradeHistory { get; }
        public RelayCommand<CommodityViewModel?> SelectCommodityCommand { get; }
        public RelayCommand BuyCommand { get; }
        public RelayCommand SellCommand { get; }

        public bool IsEmpty => Commodities.Count == 0;

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
                    OnPropertyChanged(nameof(AvailableBuyScu));
                    OnPropertyChanged(nameof(AvailableSellScu));
                    OnPropertyChanged(nameof(PlannedTradeScu));
                    OnPropertyChanged(nameof(ProjectedTradeProfit));
                    OnPropertyChanged(nameof(CanSellSelectedCargo));
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

        public int TradeQuantityScu
        {
            get => _tradeQuantityScu;
            set
            {
                var normalized = Math.Max(1, value);
                if (_tradeQuantityScu == normalized) return;
                _tradeQuantityScu = normalized;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlannedTradeScu));
                OnPropertyChanged(nameof(ProjectedTradeProfit));
            }
        }

        public int CargoCapacityScu
        {
            get => _cargoCapacityScu;
            set
            {
                var normalized = Math.Max(1, value);
                if (_cargoCapacityScu == normalized) return;
                _cargoCapacityScu = normalized;
                if (_currentCargoScu > normalized)
                    CurrentCargoScu = normalized;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlannedTradeScu));
                OnPropertyChanged(nameof(ProjectedTradeProfit));
            }
        }

        public int CurrentCargoScu
        {
            get => _currentCargoScu;
            private set
            {
                if (_currentCargoScu == value) return;
                _currentCargoScu = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CargoSpaceRemainingScu));
                OnPropertyChanged(nameof(PlannedTradeScu));
                OnPropertyChanged(nameof(ProjectedTradeProfit));
            }
        }

        public string CurrentCargoCommodity
        {
            get => _currentCargoCommodity;
            private set { _currentCargoCommodity = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanSellSelectedCargo)); }
        }

        public int AvailableBuyScu => Math.Max(0, (SelectedCommodity?.BuyStock ?? 0) - BoughtScuForSelectedCommodity);
        public int AvailableSellScu => Math.Max(0, (SelectedCommodity?.SellDemand ?? 0) - SoldScuForSelectedCommodity);
        public int CargoSpaceRemainingScu => Math.Max(0, CargoCapacityScu - CurrentCargoScu);
        public int PlannedTradeScu => Math.Max(0, Math.Min(TradeQuantityScu,
            Math.Min(AvailableBuyScu, Math.Min(AvailableSellScu, CargoSpaceRemainingScu))));
        public decimal ProjectedTradeProfit => SelectedCommodity == null
            ? 0m
            : Math.Max(0, PlannedTradeScu) * (SelectedCommodity.SellPrice - SelectedCommodity.BuyPrice);
        public bool CanSellSelectedCargo => SelectedCommodity != null &&
            CurrentCargoScu > 0 &&
            string.Equals(CurrentCargoCommodity, SelectedCommodity.Name, StringComparison.OrdinalIgnoreCase);

        private int BoughtScuForSelectedCommodity => SelectedCommodity == null ? 0 :
            TradeHistory.Where(trade => trade.CommodityName == SelectedCommodity.Name && trade.Action == "Bought").Sum(trade => trade.Scu);
        private int SoldScuForSelectedCommodity => SelectedCommodity == null ? 0 :
            TradeHistory.Where(trade => trade.CommodityName == SelectedCommodity.Name && trade.Action == "Sold").Sum(trade => trade.Scu);

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
                await LoadWatchlistAsync();
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
                    if (_marketDataService.CurrentStatus == DataSourceStatus.Live)
                        RecordMarketHistory(liveData);
                    MapMarketDataToViewModels(liveData);
                    HasCommodities = Commodities.Count > 0;
                    DataStatus = _marketDataService.CurrentStatus;
                    LastUpdated = _marketDataService.CurrentStatus == DataSourceStatus.Live
                        ? _marketDataService.LastRefresh
                        : DateTime.UtcNow;
                    UpdateCategoriesAndLocations(liveData);
                    UpdateQuickStats();
                    StatusMessage = _marketDataService.CurrentStatus == DataSourceStatus.Live
                        ? _marketHistoryError ?? $"Market data refreshed ({liveData.Commodities.Count} commodities); 24h trends build from saved snapshots."
                        : $"UEXCorp unavailable; showing local data. {_marketDataService.LastError}";
                    if (_marketDataService.CurrentStatus == DataSourceStatus.Unavailable)
                        DataStatus = DataSourceStatus.Sample;
                }
                catch
                {
                    StatusMessage = $"UEXCorp is unavailable; showing local market data. {_marketDataService.LastError}";
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

        private void BuyCargo()
        {
            if (SelectedCommodity == null)
            {
                StatusMessage = "Select a commodity before buying.";
                return;
            }

            if (CurrentCargoScu > 0 && !string.Equals(CurrentCargoCommodity, SelectedCommodity.Name, StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = $"Sell or clear your {CurrentCargoCommodity} cargo before loading another commodity.";
                return;
            }

            var quantity = Math.Min(TradeQuantityScu, Math.Min(AvailableBuyScu, CargoSpaceRemainingScu));
            if (quantity <= 0)
            {
                StatusMessage = "No buy stock or cargo space is available for this quantity.";
                return;
            }

            _tradeCostBasis = CurrentCargoScu == 0
                ? SelectedCommodity.BuyPrice
                : ((_tradeCostBasis * CurrentCargoScu) + (SelectedCommodity.BuyPrice * quantity)) / (CurrentCargoScu + quantity);
            CurrentCargoCommodity = SelectedCommodity.Name;
            CurrentCargoScu += quantity;
            var historySaved = AddTradeHistory(new MarketTradeEntry
            {
                CommodityName = SelectedCommodity.Name,
                Action = "Bought",
                Scu = quantity,
                Location = SelectedCommodity.BuyLocation,
                UnitPrice = SelectedCommodity.BuyPrice,
                Timestamp = DateTime.UtcNow
            });
            NotifyTradeCapacityChanged();
            StatusMessage = historySaved
                ? $"Loaded {quantity:N0} SCU of {SelectedCommodity.Name} at {SelectedCommodity.BuyLocation}."
                : "Cargo loaded for this session, but its local trade history could not be saved.";
        }

        private void SellCargo()
        {
            if (!CanSellSelectedCargo)
            {
                StatusMessage = "Select the commodity currently in your cargo before selling.";
                return;
            }

            var quantity = Math.Min(TradeQuantityScu, Math.Min(CurrentCargoScu, AvailableSellScu));
            if (quantity <= 0)
            {
                StatusMessage = "No sell demand is available for this commodity at the selected market.";
                return;
            }

            var realizedProfit = (SelectedCommodity!.SellPrice - _tradeCostBasis) * quantity;
            CurrentCargoScu -= quantity;
            if (CurrentCargoScu == 0)
            {
                CurrentCargoCommodity = string.Empty;
                _tradeCostBasis = 0m;
            }
            var historySaved = AddTradeHistory(new MarketTradeEntry
            {
                CommodityName = SelectedCommodity.Name,
                Action = "Sold",
                Scu = quantity,
                Location = SelectedCommodity.SellLocation,
                UnitPrice = SelectedCommodity.SellPrice,
                Profit = realizedProfit,
                Timestamp = DateTime.UtcNow
            });
            NotifyTradeCapacityChanged();
            StatusMessage = historySaved
                ? $"Sold {quantity:N0} SCU at {SelectedCommodity.SellLocation}; realized {realizedProfit:N0} aUEC."
                : "Sale recorded for this session, but its local trade history could not be saved.";
        }

        private bool AddTradeHistory(MarketTradeEntry trade)
        {
            TradeHistory.Insert(0, trade);
            while (TradeHistory.Count > 100)
                TradeHistory.RemoveAt(TradeHistory.Count - 1);
            try
            {
                LocalFeatureStore.Write("market-trades.json", new MarketTradeState
                {
                    History = TradeHistory,
                    CargoScu = CurrentCargoScu,
                    CargoCommodity = CurrentCargoCommodity,
                    CostBasisPerScu = _tradeCostBasis
                });
                return true;
            }
            catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
            {
                StatusMessage = $"Could not save local trade history: {ex.Message}";
                return false;
            }
        }

        private void NotifyTradeCapacityChanged()
        {
            OnPropertyChanged(nameof(AvailableBuyScu));
            OnPropertyChanged(nameof(AvailableSellScu));
            OnPropertyChanged(nameof(PlannedTradeScu));
            OnPropertyChanged(nameof(ProjectedTradeProfit));
            OnPropertyChanged(nameof(CanSellSelectedCargo));
        }

        private async Task ExecuteRefreshAsync()
        {
            IsRefreshing = true;
            StatusMessage = "Refreshing market data...";

            try
            {
                await LoadDataAsync();
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
                    .GroupBy(o => o.LocationId)
                    .Select(group => group.OrderByDescending(o => o.Timestamp).First())
                    .ToList();

                var bestBuy = observations.Where(o => o.BuyPrice > 0).OrderBy(o => o.BuyPrice).FirstOrDefault();
                var bestSell = observations.Where(o => o.SellPrice > 0).OrderByDescending(o => o.SellPrice).FirstOrDefault();

                var buyPrice = bestBuy?.BuyPrice ?? 0m;
                var sellPrice = bestSell?.SellPrice ?? 0m;
                var margin = CalculateMargin(buyPrice, sellPrice);
                var volume = observations.Sum(o => o.Volume);
                var demandVolume = observations.Sum(o => o.DemandVolume);
                var buyStock = bestBuy?.Volume ?? 0;
                var sellDemand = bestSell?.DemandVolume ?? 0;

                var buyLoc = bestBuy == null
                    ? "—"
                    : locations.TryGetValue(bestBuy.LocationId, out var buyLocName) ? buyLocName : bestBuy.LocationName;
                var sellLoc = bestSell == null
                    ? "—"
                    : locations.TryGetValue(bestSell.LocationId, out var sellLocName) ? sellLocName : bestSell.LocationName;

                var risk = observations.Count > 0 ? CalculateRisk(margin, volume, demandVolume) : 50;
                var priceChange = Get24HourChange(commodity.Id);
                var trend = priceChange.HasValue
                    ? priceChange.Value >= 0 ? "Up" : "Down"
                    : "Awaiting history";

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
                    BuyPrice = buyPrice,
                    ReferenceMarginPerScu = commodity.ReferenceMarginPerScu,
                    BuyLocation = buyLoc,
                    SellPrice = sellPrice,
                    SellLocation = sellLoc,
                    Margin = margin,
                    RiskAdjustedMargin = margin * (100 - risk) / 100m,
                    Volume = volume,
                    DemandVolume = demandVolume,
                    BuyStock = buyStock,
                    SellDemand = sellDemand,
                    BuyContainerSizes = FormatContainerSizes(bestBuy?.SupportedContainerSizes),
                    SellContainerSizes = FormatContainerSizes(bestSell?.SupportedContainerSizes),
                    BuyLoadingTime = FormatLoadingTime(bestBuy?.EstimatedLoadingTimeSeconds),
                    SellLoadingTime = FormatLoadingTime(bestSell?.EstimatedLoadingTimeSeconds),
                    UexAverageIndex = commodity.UexAverageIndex,
                    UexAverageBuyPrice = commodity.UexAverageBuyPrice,
                    UexAverageSellPrice = commodity.UexAverageSellPrice,
                    UexAverageSupplyScu = commodity.UexAverageSupplyScu,
                    UexAverageDemandScu = commodity.UexAverageDemandScu,
                    LastUpdated = observations.Count > 0
                        ? observations.Max(o => o.Timestamp)
                        : commodity.UexUpdatedAt ?? DateTime.MinValue,
                    Risk = risk,
                    Trend = trend,
                    Change24hPercent = priceChange,
                    Change24hText = priceChange.HasValue
                        ? $"{(priceChange.Value >= 0 ? "+" : string.Empty)}{priceChange.Value:0.0}%"
                        : "Building 24h history",
                    TrendPoints = Get24HourSparkline(commodity.Id),
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
                            SupportedContainerSizes = FormatContainerSizes(o.SupportedContainerSizes),
                            EstimatedLoadingTime = FormatLoadingTime(o.EstimatedLoadingTimeSeconds),
                            Timestamp = o.Timestamp,
                            SourceName = o.SourceName
                        }).ToList()
                });
            }

            SortCommodities();
        }

        private static string FormatContainerSizes(List<int>? sizes) =>
            sizes is { Count: > 0 } ? string.Join(", ", sizes) : "Not reported";

        private static string FormatLoadingTime(int? seconds) =>
            seconds is int value
                ? $"{(int)TimeSpan.FromSeconds(value).TotalHours:00}:{TimeSpan.FromSeconds(value).Minutes:00}:{TimeSpan.FromSeconds(value).Seconds:00}"
                : "Not reported";

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

        private decimal? Get24HourChange(string commodityId)
        {
            var now = DateTime.UtcNow;
            var current = _marketHistory
                .Where(entry => entry.CommodityId == commodityId && entry.CapturedAtUtc <= now)
                .OrderByDescending(entry => entry.CapturedAtUtc)
                .FirstOrDefault();
            var previous = _marketHistory
                .Where(entry => entry.CommodityId == commodityId && entry.CapturedAtUtc <= now.AddHours(-24))
                .OrderByDescending(entry => entry.CapturedAtUtc)
                .FirstOrDefault();
            return current == null || previous == null || previous.ReferencePrice <= 0
                ? null
                : Math.Round((current.ReferencePrice - previous.ReferencePrice) / previous.ReferencePrice * 100m, 2);
        }

        private PointCollection Get24HourSparkline(string commodityId)
        {
            var now = DateTime.UtcNow;
            var points = _marketHistory
                .Where(entry => entry.CommodityId == commodityId && entry.CapturedAtUtc >= now.AddHours(-24) && entry.CapturedAtUtc <= now)
                .OrderBy(entry => entry.CapturedAtUtc)
                .ToList();
            if (points.Count < 2) return new PointCollection();

            var minimum = points.Min(point => point.ReferencePrice);
            var range = points.Max(point => point.ReferencePrice) - minimum;
            if (range == 0) range = 1;
            var result = new PointCollection();
            for (var index = 0; index < points.Count; index++)
            {
                var x = 40d * index / (points.Count - 1);
                var y = 15d - (double)((points[index].ReferencePrice - minimum) / range * 14m);
                result.Add(new Point(x, y));
            }
            return result;
        }

        private void RecordMarketHistory(MarketData marketData)
        {
            var now = DateTime.UtcNow;
            var bucket = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute / 15 * 15, 0, DateTimeKind.Utc);
            var live = marketData.PriceObservations
                .Where(observation => observation.SourceStatus == DataSourceStatus.Live)
                .GroupBy(observation => observation.CommodityId);
            foreach (var group in live)
            {
                if (_marketHistory.Any(entry => entry.CommodityId == group.Key && entry.CapturedAtUtc >= bucket))
                    continue;

                var observations = group.ToList();
                var buy = observations.Where(observation => observation.BuyPrice > 0)
                    .OrderBy(observation => observation.BuyPrice).FirstOrDefault();
                var sell = observations.Where(observation => observation.SellPrice > 0)
                    .OrderByDescending(observation => observation.SellPrice).FirstOrDefault();
                var referencePrice = buy != null && sell != null
                    ? (buy.BuyPrice + sell.SellPrice) / 2m
                    : buy?.BuyPrice ?? sell?.SellPrice ?? 0m;
                if (referencePrice <= 0) continue;

                _marketHistory.Add(new MarketPriceHistoryEntry(group.Key, bucket, referencePrice));
            }

            _marketHistory.RemoveAll(entry => entry.CapturedAtUtc < now.AddDays(-7));
            try
            {
                LocalFeatureStore.Write("market-history.json", _marketHistory);
                _marketHistoryError = null;
            }
            catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
            {
                _marketHistoryError = $"Live data loaded, but market history could not be saved: {ex.Message}";
            }
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
            var withMargin = Commodities.Where(c => c.Margin > 0).OrderByDescending(c => c.RiskAdjustedMargin).ToList();
            var withLoss = Commodities.Where(c => c.Margin < 0).OrderBy(c => c.RiskAdjustedMargin).ToList();
            var mostTraded = Commodities.OrderByDescending(c => c.Volume).FirstOrDefault();

            if (withMargin.Count > 0)
            {
                var best = withMargin.First();
                BestGainName = best.Name;
                BestGainMargin = $"+{best.Margin:0.##}% · risk-adjusted {best.RiskAdjustedMargin:0.##}%";
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

        private void LoadStationDetail(string commodityId)
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
                case "24h Change":
                    view.SortDescriptions.Add(new SortDescription(nameof(CommodityViewModel.Change24hPercent), direction));
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
        public decimal ReferenceMarginPerScu { get; set; }
        public decimal SellPrice { get; set; } = 0m;
        public decimal Margin { get; set; } = 0m;
        public decimal MarginPercent => BuyPrice > 0 ? Math.Round(((SellPrice - BuyPrice) / BuyPrice) * 100, 2) : 0m;
        public string BuyLocation { get; set; } = string.Empty;
        public string SellLocation { get; set; } = string.Empty;
        public string BuyPriceText => BuyPrice > 0 ? BuyPrice.ToString("N0") : "—";
        public string SellPriceText => SellPrice > 0 ? SellPrice.ToString("N0") : "—";
        public string MarginText => StationObservations.Count > 0 ? $"{Margin:0.##}%" : "—";
        public decimal RiskAdjustedMargin { get; set; }
        public int Volume { get; set; } = 0;
        public int DemandVolume { get; set; } = 0;
        public int BuyStock { get; set; } = 0;
        public int SellDemand { get; set; } = 0;
        public string BuyContainerSizes { get; set; } = "Not reported";
        public string SellContainerSizes { get; set; } = "Not reported";
        public string BuyLoadingTime { get; set; } = "Not reported";
        public string SellLoadingTime { get; set; } = "Not reported";
        public decimal UexAverageIndex { get; set; }
        public decimal UexAverageBuyPrice { get; set; }
        public decimal UexAverageSellPrice { get; set; }
        public decimal UexAverageSupplyScu { get; set; }
        public decimal UexAverageDemandScu { get; set; }
        public string UexAverageIndexText => UexAverageBuyPrice > 0 || UexAverageSellPrice > 0
            ? $"UEX average index: {UexAverageIndex:+#,##0;-#,##0;0} aUEC/SCU"
            : "UEX average index: not reported";
        public string UexAveragePricesText => UexAverageBuyPrice > 0 || UexAverageSellPrice > 0
            ? $"UEX average buy: {UexAverageBuyPrice:N0}  ·  average sell: {UexAverageSellPrice:N0} aUEC/SCU"
            : "UEX price averages: not reported";
        public string UexAverageSupplyDemandText => UexAverageSupplyScu > 0 || UexAverageDemandScu > 0
            ? $"UEX average supply: {UexAverageSupplyScu:N0} SCU  ·  average demand: {UexAverageDemandScu:N0} SCU"
            : "UEX average supply/demand: not reported";
        public DateTime LastUpdated { get; set; } = DateTime.MinValue;
        public bool IsIllegal { get; set; } = false;
        public DataSourceStatus Status { get; set; } = DataSourceStatus.Sample;
        public int Risk { get; set; } = 50;
        public string Trend { get; set; } = "Marginal";
        public decimal? Change24hPercent { get; set; }
        public string Change24hText { get; set; } = "Building 24h history";
        public PointCollection TrendPoints { get; set; } = new();
        public List<StationObservation> StationObservations { get; set; } = new();

        public ICommand ExpandCommand { get; }

        public CommodityViewModel()
        {
            ExpandCommand = new RelayCommand(_ => IsExpanded = !IsExpanded);
        }

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
        public string SupportedContainerSizes { get; set; } = "Not reported";
        public string EstimatedLoadingTime { get; set; } = "Not reported";
        public DateTime Timestamp { get; set; }
        public string SourceName { get; set; } = string.Empty;
        public string MarginText => $"{Margin:0.##}%";
    }

    public sealed class MarketTradeEntry
    {
        public string CommodityName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public int Scu { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal Profit { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public sealed class MarketTradeState
    {
        public ObservableCollection<MarketTradeEntry> History { get; set; } = new();
        public int CargoScu { get; set; }
        public string CargoCommodity { get; set; } = string.Empty;
        public decimal CostBasisPerScu { get; set; }
    }

    public sealed record MarketPriceHistoryEntry(string CommodityId, DateTime CapturedAtUtc, decimal ReferencePrice);
}
