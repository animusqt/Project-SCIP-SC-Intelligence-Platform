using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using ScipDashboard.Core.Data;
using ScipDashboard.Core.Models;
using ScipDashboard.Core.Services;
using ScipDashboard.WPF.Services;

namespace ScipDashboard.WPF.ViewModels
{
    public class HaulingViewModel : INotifyPropertyChanged
    {
        private readonly IDataService _dataService;
        private readonly IMarketDataService? _marketDataService;
        private List<Location> _allLocations = new List<Location>();
        private List<Commodity> _allCommodities = new List<Commodity>();
        private List<PriceObservation> _allObservations = new List<PriceObservation>();
        private decimal _remainingCash;
        private HashSet<string> _enabledRouteCategories = new(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _enabledRouteSystems = new(StringComparer.OrdinalIgnoreCase);
        private bool _routeFiltersConfigured;

        public HaulingViewModel(IDataService dataService)
            : this(dataService, null)
        {
        }

        public HaulingViewModel(IDataService dataService, IMarketDataService? marketDataService)
        {
            _dataService = dataService;
            _marketDataService = marketDataService;

            AddLegCommand = new RelayCommand(p => ExecuteAddLeg(p), _ => CanAddLeg);
            SaveLoopCommand = new RelayCommand(async () => await ExecuteSaveLoopAsync(), () => IsLoopClosed && !string.IsNullOrWhiteSpace(LoopName));
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
            set
            {
                if (_cargoCapacity == value) return;
                _cargoCapacity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatCargoCapacity));
                OnPropertyChanged(nameof(SelectedRoutePreview));
                RebuildBestRoutes();
                RebuildLoopOptions();
                RebuildCurrentLegOptions();
                RaiseCommandStates();
            }
        }

        private string _startingCapital = "100000";
        public string StartingCapital
        {
            get => _startingCapital;
            set
            {
                _startingCapital = value;
                _remainingCash = ParseDecimal(value, 100000m) + Legs.Sum(leg => leg.Profit);
                OnPropertyChanged(nameof(CashOnHandText));
                OnPropertyChanged(nameof(SelectedRoutePreview));
                OnPropertyChanged();
                RebuildBestRoutes();
                RebuildLoopOptions();
                RebuildCurrentLegOptions();
            }
        }

        public IReadOnlyList<int> MaxBoxOptions { get; } = new[] { 1, 2, 4, 8, 16, 24, 32 };

        private int _selectedMaxBox = 24;
        public int SelectedMaxBox
        {
            get => _selectedMaxBox;
            set
            {
                if (_selectedMaxBox == value) return;
                _selectedMaxBox = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedRoutePreview));
            }
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

        public ObservableCollection<LocationCatalogEntry> FilteredCatalogLocations { get; } = new ObservableCollection<LocationCatalogEntry>();
        public ObservableCollection<string> LocationSystems { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> LocationTypes { get; } = new ObservableCollection<string>();

        private readonly List<LocationCatalogEntry> _locationCatalog = new List<LocationCatalogEntry>();
        private string _locationSearchText = string.Empty;
        public string LocationSearchText
        {
            get => _locationSearchText;
            set
            {
                if (_locationSearchText == value) return;
                _locationSearchText = value;
                OnPropertyChanged();
                ApplyLocationFilters();
            }
        }

        private string _selectedLocationSystem = "All Systems";
        public string SelectedLocationSystem
        {
            get => _selectedLocationSystem;
            set
            {
                if (_selectedLocationSystem == value) return;
                _selectedLocationSystem = value;
                OnPropertyChanged();
                ApplyLocationFilters();
            }
        }

        private string _selectedLocationType = "All Types";
        public string SelectedLocationType
        {
            get => _selectedLocationType;
            set
            {
                if (_selectedLocationType == value) return;
                _selectedLocationType = value;
                OnPropertyChanged();
                ApplyLocationFilters();
            }
        }

        public string LocationCatalogSummary =>
            $"{FilteredCatalogLocations.Count:N0} of {_locationCatalog.Count:N0} locations · filter by name, system, or type";

        private Location? _selectedStation;
        public Location? SelectedStation
        {
            get => _selectedStation;
            set
            {
                _selectedStation = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasStationSelection));
                OnPropertyChanged(nameof(CommodityOptionsMessage));
                RebuildCommodityOptions();
                OnPropertyChanged(nameof(SelectedRoutePreview));
                RaiseCommandStates();
                OnPropertyChanged(nameof(BuilderStepSummary));
            }
        }

        public bool HasStationSelection => _selectedStation != null;

        public ObservableCollection<LegCommodityOption> CommodityOptions { get; } = new ObservableCollection<LegCommodityOption>();

        public string CommodityOptionsMessage => _selectedStation == null
            ? "Choose a station to see available commodities."
            : CommodityOptions.Count == 0
                ? $"No current buy stock is reported at {_selectedStation.Name}."
                : "Choose a commodity stocked at the selected station.";

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
                OnPropertyChanged(nameof(SelectedRoutePreview));
                RaiseCommandStates();
                OnPropertyChanged(nameof(BuilderStepSummary));
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
                OnPropertyChanged(nameof(SelectedRoutePreview));
                RaiseCommandStates();
                OnPropertyChanged(nameof(BuilderStepSummary));
            }
        }

        public bool HasDestinationSelection => _selectedDestinationOption != null;

        public bool CanAddLeg => HasStationSelection && HasCommoditySelection && HasDestinationSelection &&
                                 Legs.Count < 4 && !IsLoopClosed &&
                                 (Legs.Count == 0 || Legs[^1].To.Id == SelectedStation?.Id);

        public ObservableCollection<RouteLeg> Legs { get; } = new ObservableCollection<RouteLeg>();

        public decimal LoopTotalProfit => Legs.Sum(l => l.Profit);

        public string LoopTotalText => $"{LoopTotalProfit:N0} aUEC";

        public string CashOnHandText => $"Cash on hand: {_remainingCash:N0} aUEC";

        public bool HasLegs => Legs.Count > 0;
        public bool NoLegs => Legs.Count == 0;
        public bool IsLoopClosed => Legs.Count is >= 2 and <= 4 &&
                                    Legs[0].From.Id == Legs[^1].To.Id &&
                                    Legs.Zip(Legs.Skip(1), (first, next) => first.To.Id == next.From.Id).All(isConnected => isConnected);
        public string LoopClosureText => IsLoopClosed
            ? $"Closed loop: {Legs[0].From.Name} → {Legs[^1].To.Name} · +{LoopTotalProfit:N0} aUEC"
            : Legs.Count == 0
                ? "Add 2–4 connected, profitable legs and return to your starting terminal/orbit to close the loop."
                : Legs.Count >= 4
                    ? "This route is at the 4-leg limit. The final destination must be the starting terminal/orbit."
                    : "Loop not closed yet. Continue from the last destination and finish at the starting terminal/orbit.";

        public string SelectedRoutePreview
        {
            get
            {
                if (_selectedStation == null || _selectedCommodityOption == null || _selectedDestinationOption == null)
                    return "Select a buy terminal, commodity, and sell terminal to preview total spend and profit.";

                var leg = BuildLeg(_selectedStation, _selectedCommodityOption, _selectedDestinationOption);
                return leg == null
                    ? "No executable profit at the selected prices, stock, demand, cargo capacity, and cash."
                    : $"{leg.From.Name} → {leg.Commodity.Name} → {leg.To.Name} · Buy {leg.Quantity:N0} SCU × {leg.BuyPrice:N0} = {leg.BuyCost:N0} aUEC · Sell {leg.Quantity:N0} SCU × {leg.SellPrice:N0} = {leg.SellRevenue:N0} aUEC · Net +{leg.Profit:N0} aUEC";
            }
        }

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
            ? $"Showing up to 4 profitable loops. Each starts and ends at the same terminal/orbit (2–4 legs), with spend, revenue, and net profit based on SCU."
            : "No profitable closed loops (2–4 legs) found in current market data.";

        public string BuilderStepSummary
        {
            get
            {
                if (SelectedStation == null)
                    return "1. Buy at a station  →  2. Carry a commodity stocked there  →  3. Sell at a station with demand";

                var buy = $"BUY AT {SelectedStation.Name}";
                if (SelectedCommodityOption == null)
                    return $"{buy}  →  choose a commodity available here  →  choose a sell station";

                var carry = $"CARRY {SelectedCommodityOption.Name}";
                if (SelectedDestinationOption == null)
                    return $"{buy}  →  {carry}  →  choose a station with demand";

                return $"{buy}  →  {carry}  →  SELL AT {SelectedDestinationOption.Name}";
            }
        }

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
                LoadLocationCatalog();

                if (_marketDataService != null)
                    await _marketDataService.RefreshFromUEXCorpAsync();

                var marketData = await _dataService.GetMarketDataAsync();
                _allLocations = marketData.Locations ?? new List<Location>();
                _allCommodities = marketData.Commodities ?? new List<Commodity>();
                _allObservations = marketData.PriceObservations ?? new List<PriceObservation>();
                LoadRoutingFilters();

                Stations.Clear();
                var observedMarketLocationIds = _allObservations
                    .Where(observation => observation.BuyPrice > 0 && observation.Volume > 0)
                    .Select(observation => observation.LocationId)
                    .ToHashSet(StringComparer.Ordinal);
                foreach (var loc in _allLocations
                    .Where(location => location.IsActive && location.HasCommoditiesMarket &&
                                       observedMarketLocationIds.Contains(location.Id) &&
                                       IsRouteLocationAllowed(location))
                    .OrderBy(location => location.Name))
                    Stations.Add(loc);

                var ships = await _dataService.GetShipsAsync();
                ShipsCountText = $"SHIPS ({ships.Count})";

                RebuildCommodityOptions();
                RebuildDestinationOptions();
                await ReloadSavedLoopsAsync();
                RebuildBestRoutes();
                RebuildLoopOptions();

                if (_marketDataService?.CurrentStatus == DataSourceStatus.Live)
                {
                    StatusMessage = BestRoutes.Count > 0
                        ? $"UEXCorp live: {Stations.Count} terminals, {BestRoutes.Count} profitable single routes, {LoopOptions.Count} auto loops. SC DataHub catalog loaded; SC-Truckers uses the same UEX feed."
                        : $"UEXCorp live: {Stations.Count} market terminals loaded, but no profitable routes match current stock and demand. SC DataHub reference prices do not include terminal inventory.";
                }
                else
                {
                    var error = _marketDataService?.LastError;
                    StatusMessage = Stations.Count == 0
                        ? $"No observed market terminals are available. SC DataHub reference prices are location-free and cannot build routes. {error}"
                        : $"Loaded {_allCommodities.Count} commodities across {Stations.Count} terminals. Live UEX refresh unavailable. {error}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load market data: {ex.Message}";
            }
        }

        private void LoadLocationCatalog()
        {
            if (_locationCatalog.Count > 0) return;

            var catalogPath = Path.Combine(AppContext.BaseDirectory, "Data", "location-catalog.json");
            if (!File.Exists(catalogPath))
                throw new FileNotFoundException("The bundled hauling location catalog is missing.", catalogPath);

            var json = File.ReadAllText(catalogPath);
            var locations = JsonSerializer.Deserialize<List<LocationCatalogEntry>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (locations == null)
                throw new InvalidDataException("The bundled hauling location catalog could not be read.");

            _locationCatalog.AddRange(locations);

            LocationSystems.Clear();
            LocationSystems.Add("All Systems");
            foreach (var system in _locationCatalog
                .Select(location => location.SystemName)
                .Where(system => !string.IsNullOrWhiteSpace(system))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(system => system))
            {
                LocationSystems.Add(system);
            }

            LocationTypes.Clear();
            LocationTypes.Add("All Types");
            foreach (var type in _locationCatalog
                .Select(location => location.Type)
                .Where(type => !string.IsNullOrWhiteSpace(type))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(type => type))
            {
                LocationTypes.Add(type);
            }

            ApplyLocationFilters();
        }

        private void ApplyLocationFilters()
        {
            var search = LocationSearchText.Trim();
            var filtered = _locationCatalog.Where(location =>
                (string.IsNullOrEmpty(search) ||
                    location.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    location.ParentName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    location.SystemName.Contains(search, StringComparison.OrdinalIgnoreCase)) &&
                (SelectedLocationSystem == "All Systems" ||
                    string.Equals(location.SystemName, SelectedLocationSystem, StringComparison.OrdinalIgnoreCase)) &&
                (SelectedLocationType == "All Types" ||
                    string.Equals(location.Type, SelectedLocationType, StringComparison.OrdinalIgnoreCase)));

            FilteredCatalogLocations.Clear();
            foreach (var location in filtered)
                FilteredCatalogLocations.Add(location);

            OnPropertyChanged(nameof(LocationCatalogSummary));
        }

        private void LoadRoutingFilters()
        {
            try
            {
                var settings = LocalFeatureStore.Read<RoutingFilterSettings>("routing-filters.json");
                _routeFiltersConfigured = settings.IsConfigured;
                _enabledRouteCategories = settings.IsConfigured
                    ? new HashSet<string>(settings.EnabledCategories ?? new List<string>(), StringComparer.OrdinalIgnoreCase)
                    : new HashSet<string>(
                        new[] { "Asteroid clusters", "Naval bases", "Outposts", "Settlements", "Space stations" },
                        StringComparer.OrdinalIgnoreCase);
                _enabledRouteSystems = settings.IsConfigured
                    ? new HashSet<string>(settings.EnabledSystems ?? new List<string>(), StringComparer.OrdinalIgnoreCase)
                    : new HashSet<string>(
                        _locationCatalog.Select(location => location.SystemName),
                        StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                _routeFiltersConfigured = false;
                _enabledRouteCategories = new HashSet<string>(
                    new[] { "Asteroid clusters", "Naval bases", "Outposts", "Settlements", "Space stations" },
                    StringComparer.OrdinalIgnoreCase);
                _enabledRouteSystems = new HashSet<string>(
                    _locationCatalog.Select(location => location.SystemName),
                    StringComparer.OrdinalIgnoreCase);
                StatusMessage = $"Could not load saved route filters; using all locations. {ex.Message}";
            }
        }

        private bool IsRouteLocationAllowed(Location location)
        {
            if (_routeFiltersConfigured &&
                (!_enabledRouteSystems.Contains(location.SystemName) ||
                 !_enabledRouteCategories.Contains(GetRouteLocationCategory(location))))
                return false;

            return true;
        }

        private string GetRouteLocationCategory(Location location)
        {
            if (location.Name.StartsWith("HDMS", StringComparison.OrdinalIgnoreCase) ||
                location.Type.Contains("naval", StringComparison.OrdinalIgnoreCase) ||
                location.Type.Contains("military", StringComparison.OrdinalIgnoreCase))
                return "Naval bases";

            if (location.Name.Contains("asteroid", StringComparison.OrdinalIgnoreCase) ||
                location.Name.Contains("cluster", StringComparison.OrdinalIgnoreCase) ||
                location.Name.Contains("mine", StringComparison.OrdinalIgnoreCase) ||
                _locationCatalog.Any(entry =>
                    string.Equals(entry.Name, location.Name, StringComparison.OrdinalIgnoreCase) &&
                    entry.Type.Contains("asteroid", StringComparison.OrdinalIgnoreCase)))
                return "Asteroid clusters";

            if (location.Name.Contains("outpost", StringComparison.OrdinalIgnoreCase) ||
                location.Name.Contains("research", StringComparison.OrdinalIgnoreCase) ||
                _locationCatalog.Any(entry =>
                    string.Equals(entry.Name, location.Name, StringComparison.OrdinalIgnoreCase) &&
                    entry.Type.Contains("outpost", StringComparison.OrdinalIgnoreCase)))
                return "Outposts";

            if (location.Name.Equals("Everus Harbor", StringComparison.OrdinalIgnoreCase) ||
                location.Name.Equals("Lorville", StringComparison.OrdinalIgnoreCase) ||
                location.Name.Equals("Area18", StringComparison.OrdinalIgnoreCase) ||
                location.Name.Equals("Orison", StringComparison.OrdinalIgnoreCase) ||
                location.Name.Equals("New Babbage", StringComparison.OrdinalIgnoreCase) ||
                _locationCatalog.Any(entry =>
                    string.Equals(entry.Name, location.Name, StringComparison.OrdinalIgnoreCase) &&
                    (entry.Type.Equals("City", StringComparison.OrdinalIgnoreCase) ||
                     entry.Type.Equals("Landing Zone", StringComparison.OrdinalIgnoreCase))))
                return "Settlements";

            return "Space stations";
        }

        private PriceObservation? BestSellFor(
            string commodityId,
            string excludeLocationId,
            decimal buyPrice,
            int buyStock,
            int cargo,
            decimal availableCash)
        {
            if (buyPrice <= 0 || buyStock <= 0)
                return null;

            var affordable = (int)Math.Min(int.MaxValue, availableCash / buyPrice);
            return _allObservations
                .Where(observation => observation.CommodityId == commodityId &&
                                      observation.LocationId != excludeLocationId &&
                                      _allLocations.FirstOrDefault(location => location.Id == observation.LocationId) is { } location &&
                                      IsTerminalOrOrbit(location) &&
                                      IsRouteLocationAllowed(location))
                .GroupBy(observation => observation.LocationId)
                .Select(group => group.OrderByDescending(observation => observation.Timestamp).First())
                .Where(observation => observation.SellPrice > buyPrice && observation.DemandVolume > 0)
                .Select(observation => new
                {
                    Observation = observation,
                    Quantity = Math.Min(cargo, Math.Min(Math.Min(buyStock, observation.DemandVolume), affordable))
                })
                .Where(candidate => candidate.Quantity > 0)
                .OrderByDescending(candidate =>
                    (candidate.Observation.SellPrice - buyPrice) * candidate.Quantity)
                .ThenByDescending(candidate => candidate.Observation.SellPrice)
                .Select(candidate => candidate.Observation)
                .FirstOrDefault();
        }

        private PriceObservation? LatestObservationFor(string commodityId, string locationId)
        {
            return _allObservations
                .Where(observation => observation.CommodityId == commodityId &&
                                      observation.LocationId == locationId)
                .OrderByDescending(observation => observation.Timestamp)
                .FirstOrDefault();
        }

        private void RebuildCommodityOptions()
        {
            CommodityOptions.Clear();
            _selectedCommodityOption = null;
            OnPropertyChanged(nameof(SelectedCommodityOption));
            OnPropertyChanged(nameof(HasCommoditySelection));
            OnPropertyChanged(nameof(CommodityOptionsMessage));
            _selectedDestinationOption = null;
            DestinationOptions.Clear();
            OnPropertyChanged(nameof(SelectedDestinationOption));
            OnPropertyChanged(nameof(HasDestinationSelection));

            if (_selectedStation == null)
                return;

            int cargo = ParseInt(CargoCapacity, 96);
            decimal availableCash = _remainingCash;

            var options = new List<LegCommodityOption>();
            foreach (var commodity in _allCommodities)
            {
                var buyObs = LatestObservationFor(commodity.Id, _selectedStation.Id);
                if (buyObs == null || buyObs.BuyPrice <= 0 || buyObs.Volume <= 0) continue;

                var bestSell = BestSellFor(
                    commodity.Id,
                    _selectedStation.Id,
                    buyObs.BuyPrice,
                    buyObs.Volume,
                    cargo,
                    availableCash);
                bool hasProfitableSell = bestSell != null && bestSell.SellPrice > buyObs.BuyPrice;
                decimal marginPct = hasProfitableSell
                    ? (bestSell!.SellPrice - buyObs.BuyPrice) / buyObs.BuyPrice * 100m
                    : 0m;
                int affordable = (int)Math.Min(int.MaxValue, availableCash / buyObs.BuyPrice);
                int qty = hasProfitableSell
                    ? Math.Min(cargo, Math.Min(Math.Min(buyObs.Volume, bestSell!.DemandVolume), affordable))
                    : Math.Min(cargo, Math.Min(buyObs.Volume, affordable));
                if (qty <= 0) continue;

                var profitPerScu = hasProfitableSell ? bestSell!.SellPrice - buyObs.BuyPrice : 0m;
                options.Add(new LegCommodityOption
                {
                    Id = commodity.Id,
                    Name = commodity.Name,
                    BuyPrice = buyObs.BuyPrice,
                    MarginPercent = Math.Round(marginPct, 1),
                    EstimatedProfitPerScu = Math.Round(profitPerScu, 2),
                    EstimatedQuantity = qty,
                    EstimatedTotalProfit = Math.Round(profitPerScu * qty, 0),
                    UexAverageIndex = commodity.UexAverageIndex,
                    HasUexAverageIndex = commodity.UexAverageBuyPrice > 0 || commodity.UexAverageSellPrice > 0,
                    StockVolume = buyObs.Volume,
                    BuyAtStation = _selectedStation.Name,
                    HasProfitableSell = hasProfitableSell
                });
            }

            foreach (var option in options
                         .OrderByDescending(o => o.EstimatedTotalProfit)
                         .ThenByDescending(o => o.EstimatedProfitPerScu)
                         .ThenByDescending(o => o.UexAverageIndex))
                CommodityOptions.Add(option);

            OnPropertyChanged(nameof(CommodityOptionsMessage));
        }

        private void RebuildDestinationOptions()
        {
            DestinationOptions.Clear();
            _selectedDestinationOption = null;
            OnPropertyChanged(nameof(SelectedDestinationOption));
            OnPropertyChanged(nameof(HasDestinationSelection));

            if (_selectedStation == null || _selectedCommodityOption == null) return;

            decimal availableCash = _remainingCash;
            int cargo = ParseInt(CargoCapacity, 96);
            var options = new List<LegDestinationOption>();
            foreach (var obs in _allObservations
                .Where(observation => observation.CommodityId == _selectedCommodityOption.Id &&
                                      observation.LocationId != _selectedStation.Id &&
                                      _allLocations.FirstOrDefault(location => location.Id == observation.LocationId) is { } location &&
                                      IsTerminalOrOrbit(location) &&
                                      IsRouteLocationAllowed(location))
                .GroupBy(observation => observation.LocationId)
                .Select(group => group.OrderByDescending(observation => observation.Timestamp).First()))
            {
                if (obs.SellPrice <= _selectedCommodityOption.BuyPrice || obs.DemandVolume <= 0) continue;

                var location = _allLocations.FirstOrDefault(l => l.Id == obs.LocationId);
                if (location == null) continue;

                decimal marginPct = (obs.SellPrice - _selectedCommodityOption.BuyPrice) / _selectedCommodityOption.BuyPrice * 100m;
                var buyObservation = LatestObservationFor(_selectedCommodityOption.Id, _selectedStation.Id);
                if (buyObservation == null || buyObservation.BuyPrice <= 0) continue;
                int affordable = (int)Math.Min(int.MaxValue, availableCash / buyObservation.BuyPrice);
                int quantity = Math.Min(cargo, Math.Min(Math.Min(buyObservation.Volume, obs.DemandVolume), affordable));
                if (quantity <= 0) continue;
                decimal profitPerScu = obs.SellPrice - buyObservation.BuyPrice;

                options.Add(new LegDestinationOption
                {
                    Id = location.Id,
                    Name = location.Name,
                    System = location.SystemName,
                    Type = location.Type,
                    SellPrice = obs.SellPrice,
                    MarginPercent = Math.Round(marginPct, 1),
                    DemandVolume = obs.DemandVolume,
                    EstimatedQuantity = quantity,
                    EstimatedTotalProfit = Math.Round(profitPerScu * quantity, 0)
                });
            }

            foreach (var option in options.OrderByDescending(o => o.EstimatedTotalProfit))
                DestinationOptions.Add(option);
        }

        private void RebuildCurrentLegOptions()
        {
            var selectedCommodityId = _selectedCommodityOption?.Id;
            var selectedDestinationId = _selectedDestinationOption?.Id;
            RebuildCommodityOptions();

            if (selectedCommodityId != null)
                SelectedCommodityOption = CommodityOptions.FirstOrDefault(option => option.Id == selectedCommodityId);
            if (selectedDestinationId != null)
                SelectedDestinationOption = DestinationOptions.FirstOrDefault(option => option.Id == selectedDestinationId);
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
            OnPropertyChanged(nameof(IsLoopClosed));
            OnPropertyChanged(nameof(LoopClosureText));
            OnPropertyChanged(nameof(SelectedRoutePreview));
            OnPropertyChanged(nameof(HasLegs));
            OnPropertyChanged(nameof(NoLegs));
            RaiseCommandStates();
        }

        private RouteLeg? BuildLeg(Location from, LegCommodityOption commodityOption, LegDestinationOption destinationOption)
        {
            var commodity = _allCommodities.FirstOrDefault(c => c.Id == commodityOption.Id);
            var buyObs = LatestObservationFor(commodityOption.Id, from.Id);
            var sellObs = LatestObservationFor(commodityOption.Id, destinationOption.Id);
            var to = _allLocations.FirstOrDefault(l => l.Id == destinationOption.Id);
            if (commodity == null || buyObs == null || sellObs == null || to == null) return null;
            if (!IsTerminalOrOrbit(from) || !IsTerminalOrOrbit(to) ||
                buyObs.BuyPrice <= 0 || sellObs.SellPrice <= buyObs.BuyPrice ||
                buyObs.Volume <= 0 || sellObs.DemandVolume <= 0) return null;

            int cargo = ParseInt(CargoCapacity, 96);
            int affordable = buyObs.BuyPrice > 0
                ? (int)Math.Min(int.MaxValue, _remainingCash / buyObs.BuyPrice)
                : 0;
            int qty = Math.Min(cargo, Math.Min(Math.Min(buyObs.Volume, sellObs.DemandVolume), affordable));
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
                SellDemand = sellObs.DemandVolume,
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
            OnPropertyChanged(nameof(IsLoopClosed));
            OnPropertyChanged(nameof(LoopClosureText));
            OnPropertyChanged(nameof(SelectedRoutePreview));
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
            OnPropertyChanged(nameof(IsLoopClosed));
            OnPropertyChanged(nameof(LoopClosureText));
            OnPropertyChanged(nameof(SelectedRoutePreview));
            OnPropertyChanged(nameof(HasLegs));
            OnPropertyChanged(nameof(NoLegs));
            RaiseCommandStates();
        }

        private async Task ExecuteSaveLoopAsync()
        {
            if (!IsLoopClosed || string.IsNullOrWhiteSpace(LoopName))
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
            var observationsByCommodity = _allObservations
                .GroupBy(observation => observation.CommodityId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .GroupBy(observation => observation.LocationId)
                        .Select(locationGroup => locationGroup.OrderByDescending(observation => observation.Timestamp).First())
                        .ToList());
            var locationsById = _allLocations.ToDictionary(location => location.Id);
            var completed = new Dictionary<string, LoopOption>(StringComparer.Ordinal);

            void Explore(Location start, Location current, HashSet<string> visited, List<RouteLeg> path, decimal availableCash)
            {
                if (completed.Count >= 64)
                    return;

                var candidates = FindLoopLegCandidates(current, cargo, availableCash, observationsByCommodity, locationsById);
                var options = candidates
                    .Where(leg => !string.Equals(leg.To.Id, start.Id, StringComparison.Ordinal) &&
                                  !visited.Contains(leg.To.Id) &&
                                  StringComparer.Ordinal.Compare(leg.To.Id, start.Id) > 0)
                    .OrderByDescending(leg => leg.Profit)
                    .Take(6)
                    .Concat(candidates.Where(leg => string.Equals(leg.To.Id, start.Id, StringComparison.Ordinal)))
                    .ToList();

                foreach (var leg in options)
                {
                    if (string.Equals(leg.To.Id, start.Id, StringComparison.Ordinal))
                    {
                        if (path.Count == 0)
                            continue;

                        var closedLegs = path.Append(leg).ToList();
                        var signature = string.Join("|", closedLegs.Select(routeLeg =>
                            $"{routeLeg.From.Id}:{routeLeg.Commodity.Id}>{routeLeg.To.Id}"));
                        if (!completed.ContainsKey(signature))
                        {
                            completed.Add(signature, new LoopOption
                            {
                                Legs = new ObservableCollection<RouteLeg>(closedLegs),
                                TotalProfit = closedLegs.Sum(routeLeg => routeLeg.Profit),
                                StartingCapital = cash
                            });
                        }
                        continue;
                    }

                    if (path.Count >= 3)
                        continue;

                    path.Add(leg);
                    visited.Add(leg.To.Id);
                    Explore(start, leg.To, visited, path, availableCash + leg.Profit);
                    visited.Remove(leg.To.Id);
                    path.RemoveAt(path.Count - 1);
                    if (completed.Count >= 64)
                        return;
                }
            }

            foreach (var start in Stations.OrderBy(station => station.Name, StringComparer.OrdinalIgnoreCase))
            {
                Explore(start, start, new HashSet<string>(StringComparer.Ordinal) { start.Id }, new List<RouteLeg>(), cash);
                if (completed.Count >= 64)
                    break;
            }

            int index = 1;
            foreach (var option in completed.Values
                .OrderByDescending(loop => loop.TotalProfit)
                .ThenBy(loop => loop.Legs.Count)
                .Take(4))
            {
                option.Index = index++;
                for (int step = 0; step < option.Legs.Count; step++)
                    option.Legs[step].StepNumber = step + 1;
                LoopOptions.Add(option);
            }

            OnPropertyChanged(nameof(LoopOptionsHeader));
        }

        private List<RouteLeg> FindLoopLegCandidates(
            Location from,
            int cargo,
            decimal cash,
            IReadOnlyDictionary<string, List<PriceObservation>> observationsByCommodity,
            IReadOnlyDictionary<string, Location> locationsById)
        {
            var candidates = new List<RouteLeg>();
            foreach (var commodity in _allCommodities)
            {
                if (!observationsByCommodity.TryGetValue(commodity.Id, out var observations))
                    continue;

                var buyObservation = observations.FirstOrDefault(observation =>
                    string.Equals(observation.LocationId, from.Id, StringComparison.Ordinal));
                if (buyObservation == null || buyObservation.BuyPrice <= 0 || buyObservation.Volume <= 0)
                    continue;

                int affordable = (int)Math.Min(int.MaxValue, cash / buyObservation.BuyPrice);

                foreach (var sellObservation in observations)
                {
                    if (string.Equals(sellObservation.LocationId, from.Id, StringComparison.Ordinal) ||
                        sellObservation.SellPrice <= buyObservation.BuyPrice ||
                        sellObservation.DemandVolume <= 0 ||
                        !locationsById.TryGetValue(sellObservation.LocationId, out var to) ||
                        !IsTerminalOrOrbit(from) ||
                        !IsTerminalOrOrbit(to) ||
                        !IsRouteLocationAllowed(to))
                        continue;

                    int quantity = Math.Min(cargo, Math.Min(Math.Min(buyObservation.Volume, sellObservation.DemandVolume), affordable));
                    if (quantity <= 0)
                        continue;

                    var spread = sellObservation.SellPrice - buyObservation.BuyPrice;
                    candidates.Add(new RouteLeg
                    {
                        From = from,
                        To = to,
                        Commodity = commodity,
                        BuyPrice = buyObservation.BuyPrice,
                        SellPrice = sellObservation.SellPrice,
                        MarginPercent = Math.Round(spread / buyObservation.BuyPrice * 100m, 1),
                        Quantity = quantity,
                        CargoCapacity = cargo,
                        BuyStock = buyObservation.Volume,
                        SellDemand = sellObservation.DemandVolume,
                        Profit = Math.Round(spread * quantity, 0)
                    });
                }
            }

            return candidates;
        }

        private void RebuildBestRoutes()
        {
            BestRoutes.Clear();

            int cargo = ParseInt(CargoCapacity, 96);
            var rows = new List<(BestRouteRow Row, decimal Profit)>();

            foreach (var commodity in _allCommodities)
            {
                var observations = _allObservations
                    .Where(observation => observation.CommodityId == commodity.Id)
                    .GroupBy(observation => observation.LocationId)
                    .Select(group => group.OrderByDescending(observation => observation.Timestamp).First())
                    .ToList();
                if (observations.Count < 2) continue;

                decimal capital = ParseDecimal(StartingCapital, 100000m);
                (PriceObservation Buy, PriceObservation Sell, int Quantity, decimal Profit)? best = null;
                foreach (var buy in observations.Where(o => o.BuyPrice > 0 && o.Volume > 0 &&
                             _allLocations.FirstOrDefault(location => location.Id == o.LocationId) is { } location &&
                             IsTerminalOrOrbit(location) && IsRouteLocationAllowed(location)))
                {
                    var affordable = (int)Math.Min(int.MaxValue, capital / buy.BuyPrice);
                    foreach (var sell in observations.Where(o => o.LocationId != buy.LocationId &&
                                 o.SellPrice > buy.BuyPrice && o.DemandVolume > 0 &&
                                 _allLocations.FirstOrDefault(location => location.Id == o.LocationId) is { } location &&
                                 IsTerminalOrOrbit(location) && IsRouteLocationAllowed(location)))
                    {
                        var quantity = Math.Min(cargo, Math.Min(Math.Min(buy.Volume, sell.DemandVolume), affordable));
                        if (quantity <= 0) continue;
                        var profit = Math.Round((sell.SellPrice - buy.BuyPrice) * quantity, 0);
                        if (best == null || profit > best.Value.Profit)
                            best = (buy, sell, quantity, profit);
                    }
                }

                if (best is not { } route) continue;
                rows.Add((new BestRouteRow
                {
                    Start = route.Buy.LocationName,
                    Commodity = commodity.Name,
                    SellAt = route.Sell.LocationName,
                    BuyText = $"{route.Buy.BuyPrice:N0}",
                    SellText = $"{route.Sell.SellPrice:N0}",
                    ProfitText = $"+{route.Profit:N0} ({route.Quantity:N0} SCU)",
                    TimeText = $"{Math.Max(0, (int)(DateTime.UtcNow - route.Buy.Timestamp).TotalHours)}h ago"
                }, route.Profit));
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

        private static bool IsTerminalOrOrbit(Location location)
        {
            var type = location.Type ?? string.Empty;
            return type.Contains("terminal", StringComparison.OrdinalIgnoreCase) ||
                   type.Contains("orbit", StringComparison.OrdinalIgnoreCase) ||
                   type.Contains("station", StringComparison.OrdinalIgnoreCase);
        }

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
        public string DisplayText => $"{Name}   ·   +{MarginPercent:0.#}% margin   ·   {EstimatedProfitPerScu:N0} aUEC/SCU";
    }

    public class LegCommodityOption
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal BuyPrice { get; set; }
        public decimal MarginPercent { get; set; }
        public decimal EstimatedProfitPerScu { get; set; }
        public int EstimatedQuantity { get; set; }
        public decimal EstimatedTotalProfit { get; set; }
        public decimal UexAverageIndex { get; set; }
        public bool HasUexAverageIndex { get; set; }
        public int StockVolume { get; set; }
        public string BuyAtStation { get; set; } = string.Empty;
        public bool HasProfitableSell { get; set; }
        public string UexAverageIndexText => HasUexAverageIndex
            ? $"UEX avg {UexAverageIndex:+#,##0;-#,##0;0}"
            : "UEX avg unavailable";
        public string DisplayText => HasProfitableSell
            ? $"{Name}   ·   buy at {BuyAtStation}   ·   {EstimatedQuantity:N0} SCU   ·   +{EstimatedProfitPerScu:N0}/SCU   ·   +{EstimatedTotalProfit:N0} total   ·   {UexAverageIndexText}"
            : $"{Name}   ·   {StockVolume:N0} SCU stock   ·   no profitable sell quote";
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
        public int EstimatedQuantity { get; set; }
        public decimal EstimatedTotalProfit { get; set; }
        public string DisplayText => $"{Name}   ·   {EstimatedQuantity:N0} SCU   ·   sell {SellPrice:N0} aUEC/SCU   ·   +{EstimatedTotalProfit:N0} total";
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
        public decimal BuyCost => BuyPrice * Quantity;
        public decimal SellRevenue => SellPrice * Quantity;

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
        public string PurchaseBreakdown => $"Buy {Quantity:N0} SCU × {BuyPrice:N0} = {BuyCost:N0} aUEC";
        public string SaleBreakdown => $"Sell {Quantity:N0} SCU × {SellPrice:N0} = {SellRevenue:N0} aUEC";
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
        public decimal StartingCapital { get; set; }
        public decimal EndingCapital => StartingCapital + TotalProfit;
        public decimal ReturnOnInvestment => StartingCapital > 0 ? TotalProfit / StartingCapital * 100m : 0m;
        public int LegCount => Legs.Count;
        public string StartStation => Legs.FirstOrDefault()?.From.Name ?? string.Empty;
        public decimal TotalBuyCost => Legs.Sum(leg => leg.BuyCost);
        public decimal TotalSellRevenue => Legs.Sum(leg => leg.SellRevenue);
        public string DisplayText => $"{StartStation}  →  …  →  {StartStation}   ·   {LegCount} legs   ·   Buy {TotalBuyCost:N0}   ·   Sell {TotalSellRevenue:N0}   ·   +{TotalProfit:N0} aUEC   ·   End {EndingCapital:N0}   ·   {ReturnOnInvestment:0.#}% ROI";
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

    public class LocationCatalogEntry
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SystemName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string ParentName { get; set; } = string.Empty;
        public bool HasResources { get; set; }
        public string Source { get; set; } = string.Empty;
        public string DisplayName => Type == "Asteroid" && Id.Length > 8
            ? $"{Name} · {Id.Substring(Id.Length - 8)}"
            : Name;
    }
}
