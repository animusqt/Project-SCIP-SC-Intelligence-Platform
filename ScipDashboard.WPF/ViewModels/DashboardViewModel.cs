using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;
using ScipDashboard.Core.Data;
using ScipDashboard.Core.Models;
using ScipDashboard.Core.Services;

namespace ScipDashboard.WPF.ViewModels
{
    public sealed class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly IDataService _dataService;
        private readonly IMarketDataService _marketDataService;
        private bool _isRefreshing;
        private string _marketStatus = "Loading reference prices...";
        private string _marketSource = "SC DataHub default price reference; not per-terminal inventory.";
        private int _trackedCommodities;
        private int _observedTerminals;
        private int _profitableSpreads;
        private string _bestMargin = "—";
        private string _updatedAt = "Not yet refreshed";
        private string _verseMarketBrief = "Waiting for a live market snapshot before preparing the verse market brief.";

        public DashboardViewModel(IDataService dataService, IMarketDataService marketDataService)
        {
            _dataService = dataService;
            _marketDataService = marketDataService;
            TopCommodities = new ObservableCollection<KioskCommodity>();
            RefreshCommand = new RelayCommand(() => _ = RefreshAsync());
            _ = RefreshAsync();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<KioskCommodity> TopCommodities { get; }
        public RelayCommand RefreshCommand { get; }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            private set { if (_isRefreshing != value) { _isRefreshing = value; OnPropertyChanged(); } }
        }

        public string MarketStatus
        {
            get => _marketStatus;
            private set
            {
                if (_marketStatus == value) return;
                _marketStatus = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MarketStatusBrush));
            }
        }

        public Brush MarketStatusBrush => MarketStatus.StartsWith("Live", StringComparison.OrdinalIgnoreCase)
            ? Brushes.ForestGreen
            : MarketStatus.Contains("unavailable", StringComparison.OrdinalIgnoreCase)
                ? Brushes.IndianRed
                : Brushes.DarkGoldenrod;

        public string MarketSource
        {
            get => _marketSource;
            private set { if (_marketSource != value) { _marketSource = value; OnPropertyChanged(); } }
        }

        public int TrackedCommodities
        {
            get => _trackedCommodities;
            private set { if (_trackedCommodities != value) { _trackedCommodities = value; OnPropertyChanged(); } }
        }

        public int ObservedTerminals
        {
            get => _observedTerminals;
            private set { if (_observedTerminals != value) { _observedTerminals = value; OnPropertyChanged(); } }
        }

        public int ProfitableSpreads
        {
            get => _profitableSpreads;
            private set { if (_profitableSpreads != value) { _profitableSpreads = value; OnPropertyChanged(); } }
        }

        public string BestMargin
        {
            get => _bestMargin;
            private set { if (_bestMargin != value) { _bestMargin = value; OnPropertyChanged(); } }
        }

        public string UpdatedAt
        {
            get => _updatedAt;
            private set { if (_updatedAt != value) { _updatedAt = value; OnPropertyChanged(); } }
        }

        public string VerseMarketBrief
        {
            get => _verseMarketBrief;
            private set { if (_verseMarketBrief != value) { _verseMarketBrief = value; OnPropertyChanged(); } }
        }

        private async Task RefreshAsync()
        {
            if (IsRefreshing) return;

            IsRefreshing = true;
            MarketStatus = "Loading local commodity reference...";
            try
            {
                var marketData = await _dataService.GetMarketDataAsync();
                UpdateMarketBoard(marketData);

                MarketStatus = "Checking live market feed...";
                await _marketDataService.RefreshFromUEXCorpAsync();
                marketData = await _dataService.GetMarketDataAsync();
                UpdateMarketBoard(marketData);

                if (_marketDataService.CurrentStatus == DataSourceStatus.Live)
                {
                    MarketStatus = "Live UEXCorp market data";
                    MarketSource = "Live prices and terminal stock where available; SC DataHub reference values fill gaps.";
                }
                else
                {
                    MarketStatus = "Reference snapshot";
                    MarketSource = string.IsNullOrWhiteSpace(_marketDataService.LastError)
                        ? "SC DataHub default prices; station inventory and live spreads require a live feed."
                        : $"SC DataHub default prices; live feed unavailable. {_marketDataService.LastError}";
                }

                UpdatedAt = _marketDataService.CurrentStatus == DataSourceStatus.Live
                    ? _marketDataService.LastRefresh.ToLocalTime().ToString("g")
                    : DateTime.Now.ToString("g");
            }
            catch (Exception ex)
            {
                MarketStatus = "Market data unavailable";
                MarketSource = $"Could not load the packaged commodity catalog: {ex.Message}";
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private void UpdateMarketBoard(MarketData marketData)
        {
            var liveObservations = marketData.PriceObservations
                .Where(observation =>
                    observation.SourceStatus == DataSourceStatus.Live &&
                    !string.Equals(observation.SourceName, "SC DataHub reference", StringComparison.OrdinalIgnoreCase))
                .ToList();

            TrackedCommodities = marketData.Commodities.Count;
            ObservedTerminals = liveObservations
                .Select(observation => observation.LocationId)
                .Where(locationId => !string.IsNullOrWhiteSpace(locationId))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            var spreads = marketData.Commodities
                .Select(commodity =>
                {
                    var observations = liveObservations
                        .Where(observation => observation.CommodityId == commodity.Id)
                        .GroupBy(observation => observation.LocationId)
                        .Select(group => group.OrderByDescending(observation => observation.Timestamp).First())
                        .ToList();
                    var bestBuy = observations
                        .Where(observation => observation.BuyPrice > 0)
                        .OrderBy(observation => observation.BuyPrice)
                        .FirstOrDefault();
                    var bestSell = observations
                        .Where(observation => observation.SellPrice > 0)
                        .OrderByDescending(observation => observation.SellPrice)
                        .FirstOrDefault();

                    if (bestBuy != null && bestSell != null &&
                        bestBuy.LocationId != bestSell.LocationId)
                    {
                        var margin = bestSell.SellPrice - bestBuy.BuyPrice;
                        return new KioskCommodity(
                            commodity.Name,
                            commodity.Category,
                            bestBuy.BuyPrice,
                            bestBuy.LocationName,
                            bestSell.SellPrice,
                            bestSell.LocationName,
                            margin,
                            "UEXCorp",
                            true);
                    }

                    return new KioskCommodity(
                        commodity.Name,
                        commodity.Category,
                        commodity.CurrentBuyPrice,
                        "SC DataHub reference",
                        commodity.CurrentSellPrice,
                        "SC DataHub reference",
                        commodity.ReferenceMarginPerScu,
                        "Reference",
                        false);
                })
                .Where(commodity => commodity.BuyPrice > 0 || commodity.SellPrice > 0 || commodity.MarginPerScu > 0)
                .OrderByDescending(commodity => commodity.MarginPerScu)
                .ThenBy(commodity => commodity.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            ProfitableSpreads = spreads.Count(spread => spread.IsLiveSpread && spread.MarginPerScu > 0);
            var best = spreads.FirstOrDefault();
            BestMargin = best == null ? "—" : $"{best.MarginPerScu:N0} aUEC/SCU";
            VerseMarketBrief = best?.IsLiveSpread == true
                ? $"{best.Name} currently leads the observed cross-terminal spread at {best.MarginPerScu:N0} aUEC/SCU ({best.BuyLocation} → {best.SellLocation}). This is a market snapshot, not historical news."
                : "No live cross-terminal spread is available right now. Reference prices are not terminal inventory and are excluded from this brief.";

            TopCommodities.Clear();
            foreach (var commodity in spreads.Take(8))
                TopCommodities.Add(commodity);
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public sealed record KioskCommodity(
        string Name,
        string Category,
        decimal BuyPrice,
        string BuyLocation,
        decimal SellPrice,
        string SellLocation,
        decimal MarginPerScu,
        string Source,
        bool IsLiveSpread);
}
