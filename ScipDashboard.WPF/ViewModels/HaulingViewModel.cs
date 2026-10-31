using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ScipDashboard.Core.Data;
using ScipDashboard.Core.Models;
using ScipDashboard.Core.Services;

namespace ScipDashboard.WPF.ViewModels
{
    public class HaulingViewModel : INotifyPropertyChanged
    {
        private readonly IDataService _dataService;
        private List<Location> _allLocations = new List<Location>();
        private List<Commodity> _allCommodities = new List<Commodity>();
        private List<PriceObservation> _allObservations = new List<PriceObservation>();
        private decimal _remainingCash;

        public HaulingViewModel(IDataService dataService)
        {
            _dataService = dataService;

            AddLegCommand = new RelayCommand(p => ExecuteAddLeg(p), _ => CanAddLeg);
            SaveLoopCommand = new RelayCommand(async () => await ExecuteSaveLoopAsync(), () => Legs.Count > 0 && !string.IsNullOrWhiteSpace(LoopName));
            ClearLoopCommand = new RelayCommand(p => ExecuteClearLoop(p));
            RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
            RecommendedBoxCommand = new RelayCommand(() => SelectedMaxBox = RecommendedMaxBox);

            _remainingCash = ParseDecimal(_startingCapital, 100000m);
            _ = LoadDataAsync();
        }

        private string _cargoCapacity = "96";
        public string CargoCapacity
        {
            get => _cargoCapacity;
            set { _cargoCapacity = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatCargoCapacity)); RaiseCommandStates(); }
        }

        private string _startingCapital = "100000";
        public string StartingCapital
        {
            get => _startingCapital;
            set
            {
                _startingCapital = value;
                if (Legs.Count == 0)
                {
                    _remainingCash = ParseDecimal(value, 100000m);
                    OnPropertyChanged(nameof(CashOnHandText));
                }
                OnPropertyChanged();
            }
        }

        public IReadOnlyList<int> MaxBoxOptions { get; } = new[] { 1, 2, 4, 8, 16, 24, 32 };

        private int _selectedMaxBox = 24;
        public int SelectedMaxBox
        {
            get => _selectedMaxBox;
            set { _selectedMaxBox = value; OnPropertyChanged(); }
        }

        private int _recommendedMaxBox = 24;
        public int RecommendedMaxBox
        {
            get => _recommendedMaxBox;
            set { _recommendedMaxBox = value; OnPropertyChanged(); }
        }

        private string _shipsCountText = "SHIPS (0)";
        public string ShipsCountText
        {
            get => _shipsCountText;
            set { _shipsCountText = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Location> Stations { get; } = new ObservableCollection<Location>();

        private Location? _selectedStation;
        public Location? SelectedStation
        {
            get => _selectedStation;
            set
            {
                _selectedStation = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasStationSelection));
                RebuildCommodityOptions();
                RaiseCommandStates();
            }
        }

        public bool HasStationSelection => _selectedStation != null;

        public ObservableCollection<LegCommodityOption> CommodityOptions { get; } = new ObservableCollection<LegCommodityOption>();

        private LegCommodityOption? _selectedCommodityOption;
        public LegCommodityOption? SelectedCommodityOption
        {
            get => _selectedCommodityOption;
            set
            {
                _selectedCommodityOption = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasCommoditySelection));
                RebuildDestinationOptions();
                RaiseCommandStates();
            }
        }

        public bool HasCommoditySelection => _selectedCommodityOption != null;

        public ObservableCollection<LegDestinationOption> DestinationOptions { get; } = new ObservableCollection<LegDestinationOption>();

        private LegDestinationOption? _selectedDestinationOption;
        public LegDestinationOption? SelectedDestinationOption
        {
            get => _selectedDestinationOption;
            set
            {
                _selectedDestinationOption = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasDestinationSelection));
                RaiseCommandStates();
            }
        }

        public bool HasDestinationSelection => _selectedDestinationOption != null;

        public bool CanAddLeg => HasStationSelection && HasCommoditySelection && HasDestinationSelection;

        public ObservableCollection<RouteLeg> Legs { get; } = new ObservableCollection<RouteLeg>();

        public decimal LoopTotalProfit => Legs.Sum(l => l.Profit);

        public string LoopTotalText => $"{LoopTotalProfit:N0} aUEC";

        public string CashOnHandText => $"Cash on hand: {_remainingCash:N0} aUEC";

        public bool HasLegs => Legs.Count > 0;
        public bool NoLegs => Legs.Count == 0;

        private string _loopName = string.Empty;
        public string LoopName
        {
            get => _loopName;
            set { _loopName = value; OnPropertyChanged(); RaiseCommandStates(); }
        }

        public ObservableCollection<SavedLoopSummary> SavedLoops { get; } = new ObservableCollection<SavedLoopSummary>();

        public string SavedLoopsEmptyText => SavedLoops.Count == 0
            ? "Nothing saved yet. Build a loop above then click Save."
            : string.Empty;

        public bool NoSavedLoops => SavedLoops.Count == 0;

        public ObservableCollection<LoopOption> LoopOptions { get; } = new ObservableCollection<LoopOption>();

        private LoopOption? _selectedLoopOption;
        public LoopOption? SelectedLoopOption
        {
            get => _selectedLoopOption;
            set { _selectedLoopOption = value; OnPropertyChanged(); }
        }

        public string LoopOptionsHeader => LoopOptions.Count > 0
            ? $"Pick from {LoopOptions.Count} alternative loops."
            : "No profitable loops found in current market data.";

        public string StatCargoCapacity => $"{ParseInt(CargoCapacity, 96):N0} SCU";

        private int _statProfitableRoutes;
        public int StatProfitableRoutes
        {
            get => _statProfitableRoutes;
            set { _statProfitableRoutes = value; OnPropertyChanged(); }
        }

        private string _statTopRouteProfit = "0 aUEC";
        public string StatTopRouteProfit
        {
            get => _statTopRouteProfit;
            set { _statTopRouteProfit = value; OnPropertyChanged(); }
        }

        public ObservableCollection<BestRouteRow> BestRoutes { get; } = new ObservableCollection<BestRouteRow>();

        public string BestRoutesHeader => $"Best Single Routes ({StatProfitableRoutes:N0})";

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public RelayCommand AddLegCommand { get; }
        public RelayCommand SaveLoopCommand { get; }
        public RelayCommand ClearLoopCommand { get; }
        public RelayCommand RefreshCommand { get; }
        public RelayCommand RecommendedBoxCommand { get; }

        private async Task LoadDataAsync()
        {
            try
            {
                StatusMessage = "Loading market data...";

                var marketData = await _dataService.GetMarketDataAsync();
                _allLocations = marketData.Locations ?? new List<Location>();
                _allCommodities = marketData.Commodities ?? new List<Commodity>();
                _allObservations = marketData.PriceObservations ?? new List<PriceObservation>();

                Stations.Clear();
                foreach (var loc in _allLocations.Where(l => l.IsActive && l.HasCommoditiesMarket).OrderBy(l => l.Name))
                    Stations.Add(loc);

                var ships = await _dataService.GetShipsAsync();
                ShipsCountText = $"SHIPS ({ships.Count})";

                RebuildCommodityOptions();
                RebuildDestinationOptions();
                await ReloadSavedLoopsAsync();
                RebuildBestRoutes();
                RebuildLoopOptions();

                StatusMessage = $"Loaded {_allCommodities.Count} commodities across {Stations.Count} stations";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load market data: {ex.Message}";
            }
        }

        private PriceObservation? BestSellFor(string commodityId, string excludeLocationId)
        {
            return _allObservations
                .Where(o => o.CommodityId == commodityId && o.LocationId != excludeLocationId)
                .OrderByDescending(o => o.SellPrice)
                .FirstOrDefault();
        }

        private void RebuildCommodityOptions()
        {
            CommodityOptions.Clear();
            _selectedCommodityOption = null;
            OnPropertyChanged(nameof(SelectedCommodityOption));
            OnPropertyChanged(nameof(HasCommoditySelection));

            if (_selectedStation == null)
            {
                DestinationOptions.Clear();
                return;
            }

            int cargo = ParseInt(CargoCapacity, 96);

            var options = new List<LegCommodityOption>();
            foreach (var commodity in _allCommodities)
            {
                var buyObs = _allObservations.FirstOrDefault(o => o.CommodityId == commodity.Id && o.LocationId == _selectedStation.Id);
                if (buyObs == null || buyObs.BuyPrice <= 0) continue;

                var bestSell = BestSellFor(commodity.Id, _selectedStation.Id);
                if (bestSell == null || bestSell.SellPrice <= buyObs.BuyPrice) continue;

                decimal marginPct = (bestSell.SellPrice - buyObs.BuyPrice) / buyObs.BuyPrice * 100m;
                int qty = Math.Min(cargo, Math.Min(buyObs.Volume, bestSell.Volume));

                options.Add(new LegCommodityOption
                {
                    Id = commodity.Id,
                    Name = commodity.Name,
                    BuyPrice = buyObs.BuyPrice,
                    MarginPercent = Math.Round(marginPct, 1),
                    EstimatedProfitPerScu = Math.Round(bestSell.SellPrice - buyObs.BuyPrice, 2),
                    StockVolume = buyObs.Volume
                });
            }

            foreach (var option in options.OrderByDescending(o => o.EstimatedProfitPerScu))
                CommodityOptions.Add(option);
        }

        private void RebuildDestinationOptions()
        {
            DestinationOptions.Clear();
            _selectedDestinationOption = null;
            OnPropertyChanged(nameof(SelectedDestinationOption));
            OnPropertyChanged(nameof(HasDestinationSelection));

            if (_selectedStation == null || _selectedCommodityOption == null) return;

            var options = new List<LegDestinationOption>();
            foreach (var obs in _allObservations.Where(o => o.CommodityId == _selectedCommodityOption.Id && o.LocationId != _selectedStation.Id))
            {
                if (obs.SellPrice <= _selectedCommodityOption.BuyPrice) continue;

                var location = _allLocations.FirstOrDefault(l => l.Id == obs.LocationId);
                if (location == null) continue;

                decimal marginPct = (obs.SellPrice - _selectedCommodityOption.BuyPrice) / _selectedCommodityOption.BuyPrice * 100m;

                options.Add(new LegDestinationOption
                {
                    Id = location.Id,
                    Name = location.Name,
                    System = location.SystemName,
                    Type = location.Type,
                    SellPrice = obs.SellPrice,
                    MarginPercent = Math.Round(marginPct, 1),
                    DemandVolume = obs.Volume
                });
            }

            foreach (var option in options.OrderByDescending(o => o.SellPrice))
                DestinationOptions.Add(option);
        }

        private void ExecuteAddLeg(object? parameter)
        {
            if (_selectedStation == null || _selectedCommodityOption == null || _selectedDestinationOption == null)
                return;

            if (Legs.Count == 0)
                _remainingCash = ParseDecimal(StartingCapital, 100000m);

            var leg = BuildLeg(_selectedStation, _selectedCommodityOption, _selectedDestinationOption);
            if (leg == null) return;

            Legs.Add(leg);
            RenumberLegs();
            _remainingCash += leg.Profit;

            var nextStation = _allLocations.FirstOrDefault(l => l.Id == _selectedDestinationOption.Id);
            _selectedStation = null;
            SelectedStation = nextStation;

            OnPropertyChanged(nameof(LoopTotalProfit));
            OnPropertyChanged(nameof(LoopTotalText));
            OnPropertyChanged(nameof(CashOnHandText));
            OnPropertyChanged(nameof(HasLegs));
            OnPropertyChanged(nameof(NoLegs));
            RaiseCommandStates();
        }

        private RouteLeg? BuildLeg(Location from, LegCommodityOption commodityOption, LegDestinationOption destinationOption)
        {
            var commodity = _allCommodities.FirstOrDefault(c => c.Id == commodityOption.Id);
            var buyObs = _allObservations.FirstOrDefault(o => o.CommodityId == commodityOption.Id && o.LocationId == from.Id);
            var sellObs = _allObservations.FirstOrDefault(o => o.CommodityId == commodityOption.Id && o.LocationId == destinationOption.Id);
            var to = _allLocations.FirstOrDefault(l => l.Id == destinationOption.Id);
            if (commodity == null || buyObs == null || sellObs == null || to == null) return null;
            if (buyObs.BuyPrice <= 0 || sellObs.SellPrice <= buyObs.BuyPrice) return null;

            int cargo = ParseInt(CargoCapacity, 96);
            int affordable = buyObs.BuyPrice > 0 ? (int)(_remainingCash / buyObs.BuyPrice) : 0;
            int qty = Math.Min(cargo, Math.Min(Math.Min(buyObs.Volume, sellObs.Volume), affordable));
            if (qty <= 0) return null;

            decimal spread = sellObs.SellPrice - buyObs.BuyPrice;

            return new RouteLeg
            {
                From = from,
                To = to,
                Commodity = commodity,
                BuyPrice = buyObs.BuyPrice,
                SellPrice = sellObs.SellPrice,
                MarginPercent = Math.Round(spread / buyObs.BuyPrice * 100m, 1),
                Quantity = qty,
                CargoCapacity = cargo,
                BuyStock = buyObs.Volume,
                SellDemand = sellObs.Volume,
                Profit = Math.Round(spread * qty, 0)
            };
        }

        private void RenumberLegs()
        {
            for (int i = 0; i < Legs.Count; i++)
                Legs[i].StepNumber = i + 1;
        }

        private void ExecuteClearLoop(object? parameter)
        {
            Legs.Clear();
            _remainingCash = ParseDecimal(StartingCapital, 100000m);
            _selectedStation = null;
            OnPropertyChanged(nameof(SelectedStation));
            OnPropertyChanged(nameof(HasStationSelection));
            RebuildCommodityOptions();
            RebuildDestinationOptions();
            OnPropertyChanged(nameof(LoopTotalProfit));
            OnPropertyChanged(nameof(LoopTotalText));
            OnPropertyChanged(nameof(CashOnHandText));
            OnPropertyChanged(nameof(HasLegs));
            OnPropertyChanged(nameof(NoLegs));
            RaiseCommandStates();
        }

        public void UseLoopOption(LoopOption option)
        {
            Legs.Clear();
            foreach (var leg in option.Legs)
            {
                Legs.Add(new RouteLeg
                {
                    StepNumber = leg.StepNumber,
                    From = leg.From,
                    To = leg.To,
                    Commodity = leg.Commodity,
                    BuyPrice = leg.BuyPrice,
                    SellPrice = leg.SellPrice,
                    MarginPercent = leg.MarginPercent,
                    Quantity = leg.Quantity,
                    CargoCapacity = leg.CargoCapacity,
                    BuyStock = leg.BuyStock,
                    SellDemand = leg.SellDemand,
                    Profit = leg.Profit
                });
            }

            _remainingCash = ParseDecimal(StartingCapital, 100000m) + Legs.Sum(l => l.Profit);
            SelectedLoopOption = option;
            _selectedStation = Legs.LastOrDefault()?.To;
            OnPropertyChanged(nameof(SelectedStation));
            OnPropertyChanged(nameof(HasStationSelection));
            RebuildCommodityOptions();

            OnPropertyChanged(nameof(LoopTotalProfit));
            OnPropertyChanged(nameof(LoopTotalText));
            OnPropertyChanged(nameof(CashOnHandText));
            OnPropertyChanged(nameof(HasLegs));
            OnPropertyChanged(nameof(NoLegs));
            RaiseCommandStates();
        }

        private async Task ExecuteSaveLoopAsync()
        {
            if (Legs.Count == 0 || string.IsNullOrWhiteSpace(LoopName))
                return;

            try
            {
                StatusMessage = "Saving loop...";

                var stops = new List<RouteStop>();
                foreach (var leg in Legs)
                {
                    string routeId = Guid.NewGuid().ToString();

                    stops.Add(new RouteStop
                    {
                        Id = Guid.NewGuid().ToString(),
                        RouteId = routeId,
                        LocationId = leg.From.Id,
                        LocationName = leg.From.Name,
                        CommodityId = leg.Commodity.Id,
                        CommodityName = leg.Commodity.Name,
                        Quantity = leg.Quantity,
                        BuyPrice = leg.BuyPrice,
                        SellPrice = leg.SellPrice,
                        IsBuy = true,
                        OrderIndex = stops.Count
                    });

                    stops.Add(new RouteStop
                    {
                        Id = Guid.NewGuid().ToString(),
                        RouteId = routeId,
                        LocationId = leg.To.Id,
                        LocationName = leg.To.Name,
                        CommodityId = leg.Commodity.Id,
                        CommodityName = leg.Commodity.Name,
                        Quantity = leg.Quantity,
                        BuyPrice = leg.BuyPrice,
                        SellPrice = leg.SellPrice,
                        IsBuy = false,
                        OrderIndex = stops.Count
                    });
                }

                decimal total = LoopTotalProfit;
                var route = new Route
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = LoopName.Trim(),
                    Description = $"Hauling loop · {Legs.Count} legs · built in Hauling module",
                    Stops = stops,
                    EstimatedNetProfit = total,
                    EstimatedGrossMargin = total,
                    EstimatedFees = 0,
                    Status = RouteStatus.Draft,
                    IsLoop = true,
                    CreatedAt = DateTime.UtcNow,
                    CargoUsed = Legs.Sum(l => l.Quantity),
                    DataConfidence = DataSourceStatus.Sample,
                    Tags = "haul-loop"
                };

                await _dataService.CreateRouteAsync(route);
                await ReloadSavedLoopsAsync();

                StatusMessage = $"Saved \"{route.Name}\" ({Legs.Count} legs · +{total:N0} aUEC). It appears in Route Builder.";
                LoopName = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Save failed: {ex.Message}";
            }
        }

        private async Task ReloadSavedLoopsAsync()
        {
            var routes = await _dataService.GetRoutesAsync();
            SavedLoops.Clear();
            foreach (var route in routes.Where(r => r.Tags != null && r.Tags.Contains("haul-loop")).OrderByDescending(r => r.CreatedAt))
            {
                SavedLoops.Add(new SavedLoopSummary
                {
                    Name = route.Name,
                    StopCount = route.Stops.Count,
                    TotalProfit = route.EstimatedNetProfit
                });
            }
            OnPropertyChanged(nameof(SavedLoopsEmptyText));
            OnPropertyChanged(nameof(NoSavedLoops));
        }

        private void RebuildLoopOptions()
        {
            LoopOptions.Clear();

            int cargo = ParseInt(CargoCapacity, 96);
            decimal cash = ParseDecimal(StartingCapital, 100000m);

            var stationScores = Stations
                .Select(s => new { Station = s, Best = FindBestLegFrom(s, cargo, cash, new HashSet<string>()) })
                .Where(x => x.Best != null)
                .OrderByDescending(x => x.Best!.Profit);

            var built = new List<LoopOption>();
            foreach (var entry in stationScores)
            {
                var option = BuildGreedyLoop(entry.Station, cargo, cash, built.Count + 1);
                if (option.Legs.Count > 0)
                {
                    option.TotalProfit = option.Legs.Sum(l => l.Profit);
                    built.Add(option);
                }
            }

            int index = 1;
            foreach (var option in built.OrderByDescending(o => o.TotalProfit))
            {
                option.Index = index++;
                LoopOptions.Add(option);
            }

            OnPropertyChanged(nameof(LoopOptionsHeader));
        }

        private LoopOption BuildGreedyLoop(Location start, int cargo, decimal cash, int index)
        {
            var option = new LoopOption { Index = index };
            var visited = new HashSet<string> { start.Id };
            Location current = start;
            decimal available = cash;

            for (int step = 0; step < 8; step++)
            {
                var leg = FindBestLegFrom(current, cargo, available, visited);
                if (leg == null) break;

                leg.StepNumber = option.Legs.Count + 1;
                option.Legs.Add(leg);
                available += leg.Profit;
                current = leg.To;
                visited.Add(leg.To.Id);
            }

            return option;
        }

        private RouteLeg? FindBestLegFrom(Location from, int cargo, decimal cash, HashSet<string> visited)
        {
            RouteLeg? best = null;

            foreach (var commodity in _allCommodities)
            {
                var buyObs = _allObservations.FirstOrDefault(o => o.CommodityId == commodity.Id && o.LocationId == from.Id);
                if (buyObs == null || buyObs.BuyPrice <= 0) continue;

                foreach (var sellObs in _allObservations.Where(o => o.CommodityId == commodity.Id && o.LocationId != from.Id))
                {
                    if (visited.Contains(sellObs.LocationId)) continue;
                    if (sellObs.SellPrice <= buyObs.BuyPrice) continue;

                    var to = _allLocations.FirstOrDefault(l => l.Id == sellObs.LocationId);
                    if (to == null) continue;

                    int affordable = (int)(cash / buyObs.BuyPrice);
                    int qty = Math.Min(cargo, Math.Min(Math.Min(buyObs.Volume, sellObs.Volume), affordable));
                    if (qty <= 0) continue;

                    decimal profit = Math.Round((sellObs.SellPrice - buyObs.BuyPrice) * qty, 0);
                    if (best == null || profit > best.Profit)
                    {
                        best = new RouteLeg
                        {
                            From = from,
                            To = to,
                            Commodity = commodity,
                            BuyPrice = buyObs.BuyPrice,
                            SellPrice = sellObs.SellPrice,
                            MarginPercent = Math.Round((sellObs.SellPrice - buyObs.BuyPrice) / buyObs.BuyPrice * 100m, 1),
                            Quantity = qty,
                            CargoCapacity = cargo,
                            BuyStock = buyObs.Volume,
                            SellDemand = sellObs.Volume,
                            Profit = profit
                        };
                    }
                }
            }

            return best;
        }

        private void RebuildBestRoutes()
        {
            BestRoutes.Clear();

            int cargo = ParseInt(CargoCapacity, 96);
            var rows = new List<(BestRouteRow Row, decimal Profit)>();

            foreach (var commodity in _allCommodities)
            {
                var observations = _allObservations.Where(o => o.CommodityId == commodity.Id).ToList();
                if (observations.Count < 2) continue;

                var bestBuy = observations.OrderBy(o => o.BuyPrice).FirstOrDefault();
                if (bestBuy == null || bestBuy.BuyPrice <= 0) continue;

                var bestSell = observations
                    .Where(o => o.LocationId != bestBuy.LocationId && o.SellPrice > bestBuy.BuyPrice)
                    .OrderByDescending(o => o.SellPrice)
                    .FirstOrDefault();
                if (bestSell == null) continue;

                int qty = Math.Min(cargo, Math.Min(bestBuy.Volume, bestSell.Volume));
                if (qty <= 0) continue;

                decimal profit = Math.Round((bestSell.SellPrice - bestBuy.BuyPrice) * qty, 0);

                rows.Add((new BestRouteRow
                {
                    Start = bestBuy.LocationName,
                    Commodity = commodity.Name,
                    SellAt = bestSell.LocationName,
                    BuyText = $"{bestBuy.BuyPrice:N0}",
                    SellText = $"{bestSell.SellPrice:N0}",
                    ProfitText = $"+{profit:N0}",
                    TimeText = $"{Math.Max(0, (int)(DateTime.UtcNow - bestBuy.Timestamp).TotalHours)}h ago"
                }, profit));
            }

            foreach (var row in rows.OrderByDescending(r => r.Profit))
                BestRoutes.Add(row.Row);

            StatProfitableRoutes = rows.Count;
            StatTopRouteProfit = rows.Count > 0 ? $"{rows.Max(r => r.Profit):N0} aUEC" : "0 aUEC";
            OnPropertyChanged(nameof(BestRoutesHeader));
        }

        private void RaiseCommandStates()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        private static int ParseInt(string text, int fallback)
            => int.TryParse(text?.Trim(), out int v) && v > 0 ? v : fallback;

        private static decimal ParseDecimal(string text, decimal fallback)
            => decimal.TryParse(text?.Trim(), out decimal v) && v >= 0 ? v : fallback;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class MarketEntryOption
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal BuyPrice { get; set; }
        public decimal MarginPercent { get; set; }
        public decimal EstimatedProfitPerScu { get; set; }
        public int StockVolume { get; set; }
        public string DisplayText => "{Name}   ·   +{MarginPercent:0.#}% margin   ·   {EstimatedProfitPerScu:N0} aUEC/SCU";
    }

    public class LegCommodityOption
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal BuyPrice { get; set; }
        public decimal MarginPercent { get; set; }
        public decimal EstimatedProfitPerScu { get; set; }
        public int StockVolume { get; set; }
        public string DisplayText => "{Name}   ·   +{MarginPercent:0.#}% margin   ·   {EstimatedProfitPerScu:N0} aUEC/SCU";
    }

    public class LegDestinationOption
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string System { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal SellPrice { get; set; }
        public decimal MarginPercent { get; set; }
        public int DemandVolume { get; set; }
        public string DisplayText => $"{Name}   ·   {SellPrice:N0} aUEC   ·   +{MarginPercent:0.#}% margin";
    }

    public class RouteLeg : INotifyPropertyChanged
    {
        public Location From { get; set; } = new Location();
        public Location To { get; set; } = new Location();
        public Commodity Commodity { get; set; } = new Commodity();
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
        public decimal MarginPercent { get; set; }
        public int Quantity { get; set; }
        public int CargoCapacity { get; set; }
        public int BuyStock { get; set; }
        public int SellDemand { get; set; }
        public decimal Profit { get; set; }

        private int _stepNumber;
        public int StepNumber
        {
            get => _stepNumber;
            set { _stepNumber = value; OnPropertyChanged(); }
        }

        public string FromName => From.Name;
        public string ToName => To.Name;
        public string CommodityName => Commodity.Name;
        public string FromSystem => From.SystemName.ToUpperInvariant();
        public string FromType => From.Type.ToUpperInvariant();
        public string ToSystem => To.SystemName.ToUpperInvariant();
        public string ToType => To.Type.ToUpperInvariant();

        public string BuyText => $"Buy {BuyPrice:N0}";
        public string SellText => $"Sell {SellPrice:N0}";
        public string MarginText => $"{MarginPercent:0.0}% margin";
        public string ScuText => $"{Quantity:N0}/{CargoCapacity:N0} SCU";
        public string ProfitText => $"+{Profit:N0}";
        public string BuyStockText => $"BUY STOCK {BuyStock:N0} SCU";
        public string SellDemandText => $"SELL DEMAND {SellDemand:N0} SCU";

        public bool BuyStockGood => BuyStock >= Quantity;
        public bool SellDemandOk => SellDemand >= Quantity;

        public string BuyStockStateText => BuyStockGood ? "STOCK GOOD" : "STOCK LOW";
        public string SellDemandStateText => SellDemandOk ? "DEMAND OK" : "DEMAND LOW";

        public double BuyStockFill => BuyStock <= 0 ? 0 : Math.Min(100, Quantity == 0 ? 100 : BuyStock * 100.0 / Quantity);
        public double SellDemandFill => SellDemand <= 0 ? 0 : Math.Min(100, Quantity == 0 ? 100 : SellDemand * 100.0 / Quantity);

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class LoopOption
    {
        public int Index { get; set; }
        public ObservableCollection<RouteLeg> Legs { get; set; } = new ObservableCollection<RouteLeg>();
        public decimal TotalProfit { get; set; }
        public int LegCount => Legs.Count;
        public string DisplayText => $"Option {Index}   ·   {LegCount} leg{(LegCount == 1 ? "" : "s")}   ·   +{TotalProfit:N0}";
    }

    public class SavedLoopSummary
    {
        public string Name { get; set; } = string.Empty;
        public int StopCount { get; set; }
        public decimal TotalProfit { get; set; }
        public string DetailText => $"{StopCount} stops   ·   est. +{TotalProfit:N0} aUEC";
    }

    public class BestRouteRow
    {
        public string Start { get; set; } = string.Empty;
        public string Commodity { get; set; } = string.Empty;
        public string SellAt { get; set; } = string.Empty;
        public string BuyText { get; set; } = string.Empty;
        public string SellText { get; set; } = string.Empty;
        public string ProfitText { get; set; } = string.Empty;
        public string TimeText { get; set; } = string.Empty;
    }
}
