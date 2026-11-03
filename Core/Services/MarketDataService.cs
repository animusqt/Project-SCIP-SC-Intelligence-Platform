using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ScipDashboard.Core.Models;
using ScipDashboard.Core.Data;

namespace ScipDashboard.Core.Services
{
    public interface IMarketDataService
    {
        Task<MarketData> GetMarketDataAsync(bool forceRefresh = false);
        Task<MarketData> GetMarketDataWithCacheAsync();
        Task RefreshFromUEXCorpAsync();
        Task RefreshFromAllSourcesAsync();
        Task<List<Commodity>> SearchCommoditiesAsync(string query);
        Task<List<PriceObservation>> GetPriceHistoryAsync(string commodityId, int hours = 24);
        Task AddManualPriceObservationAsync(PriceObservation observation);
        void SetAdditionalCommodityNames(IEnumerable<string> names);
        void SetManualCommodityEntries(IEnumerable<ManualCommodityEntry> entries);
        DataSourceStatus CurrentStatus { get; }
        DateTime LastRefresh { get; }
        string? LastError { get; }
    }

    public class MarketDataService : IMarketDataService
    {
        private readonly IDataService _dataService;
        private readonly HttpClient _httpClient;
        private readonly HashSet<string> _additionalCommodityNames = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<ManualCommodityEntry> _manualCommodityEntries = new();
        private DateTime _lastRefresh = DateTime.MinValue;
        private string? _lastError;

        public MarketDataService(IDataService dataService)
        {
            _dataService = dataService;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public DataSourceStatus CurrentStatus { get; private set; } = DataSourceStatus.Default;
        public DateTime LastRefresh => _lastRefresh;
        public string? LastError => _lastError;

        public void SetAdditionalCommodityNames(IEnumerable<string> names)
        {
            ArgumentNullException.ThrowIfNull(names);
            _additionalCommodityNames.Clear();
            foreach (var name in names)
            {
                if (!string.IsNullOrWhiteSpace(name))
                    _additionalCommodityNames.Add(name.Trim());
            }
        }

        public void SetManualCommodityEntries(IEnumerable<ManualCommodityEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);
            _manualCommodityEntries.Clear();
            _manualCommodityEntries.AddRange(entries
                .Where(entry => entry != null &&
                                !string.IsNullOrWhiteSpace(entry.Name) &&
                                (!string.IsNullOrWhiteSpace(entry.BuyLocation) || !string.IsNullOrWhiteSpace(entry.SellLocation)))
                .Select(entry => new ManualCommodityEntry
                {
                    Id = entry.Id,
                    Name = entry.Name.Trim(),
                    BuyPrice = Math.Max(0, entry.BuyPrice),
                    SellPrice = Math.Max(0, entry.SellPrice),
                    BuyLocation = entry.BuyLocation.Trim(),
                    SellLocation = entry.SellLocation.Trim(),
                    BuyStockScu = Math.Max(0, entry.BuyStockScu),
                    SellDemandScu = Math.Max(0, entry.SellDemandScu)
                }));
            foreach (var entry in _manualCommodityEntries)
                _additionalCommodityNames.Add(entry.Name);
        }

        // ==================== MAIN DATA ACCESS ====================

        public async Task<MarketData> GetMarketDataAsync(bool forceRefresh = false)
        {
            if (forceRefresh)
            {
                await RefreshFromUEXCorpAsync();
            }

            var marketData = await _dataService.GetMarketDataAsync();
            ApplyManualCommodityEntries(marketData);
            return marketData;
        }

        public async Task<MarketData> GetMarketDataWithCacheAsync()
        {
            try
            {
                await RefreshFromUEXCorpAsync();
            }
            catch (Exception ex)
            {
                _lastError = $"UEXCorp refresh failed: {ex.Message}";
                CurrentStatus = DataSourceStatus.Unavailable;
            }

            var marketData = await _dataService.GetMarketDataAsync();
            ApplyManualCommodityEntries(marketData);
            return marketData;
        }

        // ==================== LIVE DATA FETCHING ====================

        public async Task RefreshFromUEXCorpAsync()
        {
            try
            {
                CurrentStatus = DataSourceStatus.Unavailable;
                _lastError = null;

                // Try to fetch real data from UEXCorp API
                var commoditiesResponse = await _httpClient.GetStringAsync("https://uexcorp.space/api/2.0/commodities/");
                var pricesResponse = await _httpClient.GetStringAsync("https://uexcorp.space/api/2.0/commodities_prices_all/");
                var terminalsResponse = await _httpClient.GetStringAsync("https://uexcorp.space/api/2.0/terminals/");

                using var commoditiesDoc = System.Text.Json.JsonDocument.Parse(commoditiesResponse);
                using var pricesDoc = System.Text.Json.JsonDocument.Parse(pricesResponse);
                using var terminalsDoc = System.Text.Json.JsonDocument.Parse(terminalsResponse);

                var commoditiesArray = commoditiesDoc.RootElement.GetProperty("data");
                var pricesArray = pricesDoc.RootElement.GetProperty("data");
                var priceRows = pricesArray.EnumerateArray().ToList();
                var terminalsById = terminalsDoc.RootElement.GetProperty("data")
                    .EnumerateArray()
                    .Where(terminal => ReadInt(terminal, "id") > 0)
                    .GroupBy(terminal => ReadInt(terminal, "id"))
                    .ToDictionary(group => group.Key, group => group.First());
                var referenceData = await _dataService.GetMarketDataAsync();
                var referenceCommodities = referenceData.Commodities.ToDictionary(
                    commodity => commodity.Name,
                    StringComparer.OrdinalIgnoreCase);
                var priceRowsByCommodity = priceRows
                    .Where(price => ReadInt(price, "id_commodity") > 0)
                    .GroupBy(price => ReadInt(price, "id_commodity"))
                    .ToDictionary(group => group.Key, group => group.ToList());

                var marketData = new ScipDashboard.Core.Models.MarketData
                {
                    Name = "UEXCorp Live Market Data",
                    Timestamp = DateTime.UtcNow,
                    Locations = new List<ScipDashboard.Core.Models.Location>(),
                    Commodities = new List<ScipDashboard.Core.Models.Commodity>(),
                    PriceObservations = new List<ScipDashboard.Core.Models.PriceObservation>()
                };

                // Parse commodities
                var commodityMap = new Dictionary<int, ScipDashboard.Core.Models.Commodity>();
                foreach (var c in commoditiesArray.EnumerateArray())
                {
                    var id = c.GetProperty("id").GetInt32();
                    var name = c.GetProperty("name").GetString() ?? "";
                    var kind = ReadString(c, "kind", "General");
                    var priceBuy = ReadDecimal(c, "price_buy");
                    var priceSell = ReadDecimal(c, "price_sell");
                    var isIllegal = ReadInt(c, "is_illegal") == 1;
                    priceRowsByCommodity.TryGetValue(id, out var commodityPriceRows);
                    commodityPriceRows ??= new List<System.Text.Json.JsonElement>();
                    referenceCommodities.TryGetValue(name, out var referenceCommodity);
                    var averageBuyPrice = AveragePositive(commodityPriceRows, "price_sell_avg");
                    var averageSellPrice = AveragePositive(commodityPriceRows, "price_buy_avg");
                    var averageSupply = AveragePositive(commodityPriceRows, "scu_sell_stock_avg");
                    var averageDemand = AveragePositive(commodityPriceRows, "scu_buy_avg");
                    var modified = Math.Max(
                        ReadLong(c, "date_modified"),
                        commodityPriceRows.Select(row => ReadLong(row, "date_modified")).DefaultIfEmpty().Max());

                    var commodity = new ScipDashboard.Core.Models.Commodity
                    {
                        Id = id.ToString(),
                        Name = name,
                        DisplayName = name,
                        Category = kind,
                        PricePerScu = priceBuy > 0 ? priceBuy : priceSell,
                        CurrentBuyPrice = priceBuy,
                        CurrentSellPrice = priceSell,
                        ReferenceBuyPrice = referenceCommodity?.ReferenceBuyPrice ?? 0m,
                        ReferenceSellPrice = referenceCommodity?.ReferenceSellPrice ?? 0m,
                        ReferenceMarginPerScu = referenceCommodity?.ReferenceMarginPerScu ?? 0m,
                        UexAverageBuyPrice = averageBuyPrice,
                        UexAverageSellPrice = averageSellPrice,
                        UexAverageSupplyScu = averageSupply,
                        UexAverageDemandScu = averageDemand,
                        UexUpdatedAt = modified > 0
                            ? DateTimeOffset.FromUnixTimeSeconds(modified).UtcDateTime
                            : null,
                        SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Live,
                        IsIllegal = isIllegal
                    };

                    commodityMap[id] = commodity;
                    marketData.Commodities.Add(commodity);
                }

                foreach (var price in priceRows)
                {
                    var commId = ReadInt(price, "id_commodity");
                    var terminalId = ReadInt(price, "id_terminal");
                    if (!commodityMap.TryGetValue(commId, out var commodity)) continue;

                    var terminalFound = terminalsById.TryGetValue(terminalId, out var terminal);
                    var terminalName = ReadString(price, "terminal_name",
                        terminalFound ? ReadString(terminal, "displayname", ReadString(terminal, "name", "Unknown terminal")) : "Unknown terminal");
                    var starSystem = terminalFound
                        ? ReadString(terminal, "star_system_name")
                        : ReadString(price, "star_system_name");
                    var priceToBuy = ReadDecimal(price, "price_sell");
                    var priceToSell = ReadDecimal(price, "price_buy");
                    var stockScu = ReadInt(price, "scu_sell_stock");
                    var demandScu = ReadInt(price, "scu_buy");
                    if ((priceToBuy <= 0 || stockScu <= 0) && (priceToSell <= 0 || demandScu <= 0))
                        continue;

                    var locationId = terminalId > 0
                        ? $"uex-terminal-{terminalId}"
                        : $"uex-terminal-{starSystem}-{terminalName}".Replace(" ", "-").ToLowerInvariant();

                    if (!marketData.Locations.Any(location => location.Id == locationId))
                    {
                        marketData.Locations.Add(new ScipDashboard.Core.Models.Location
                        {
                            Id = locationId,
                            Name = terminalName,
                            SystemName = starSystem,
                            Type = "Terminal",
                            IsActive = true,
                            HasCommoditiesMarket = true
                        });
                    }

                    var modified = ReadLong(price, "date_modified");
                    var containerSizes = ReadString(price, "container_sizes")
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(size => int.TryParse(size, out var parsedSize) ? parsedSize : 0)
                        .Where(size => size > 0)
                        .Distinct()
                        .OrderBy(size => size)
                        .ToList();
                    marketData.PriceObservations.Add(new ScipDashboard.Core.Models.PriceObservation
                    {
                        Id = Guid.NewGuid().ToString(),
                        CommodityId = commId.ToString(),
                        CommodityName = commodity.Name,
                        LocationId = locationId,
                        LocationName = terminalName,
                        SystemName = starSystem,
                        BuyPrice = priceToBuy,
                        SellPrice = priceToSell,
                        Volume = stockScu,
                        DemandVolume = demandScu,
                        SupportedContainerSizes = containerSizes,
                        Timestamp = modified > 0
                            ? DateTimeOffset.FromUnixTimeSeconds(modified).UtcDateTime
                            : DateTime.UtcNow,
                        SourceName = "UEXCorp",
                        SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Live
                    });
                }

                foreach (var previousObservation in referenceData.PriceObservations.Where(observation =>
                    !string.Equals(observation.SourceName, "UEXCorp", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(observation.SourceName, "SC DataHub", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(observation.SourceName, "SC DataHub reference", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(observation.SourceName, "Sample", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(observation.SourceName, "Sample Data", StringComparison.OrdinalIgnoreCase)))
                {
                    var targetCommodity = marketData.Commodities.FirstOrDefault(commodity =>
                        string.Equals(commodity.Name, previousObservation.CommodityName, StringComparison.OrdinalIgnoreCase));
                    if (targetCommodity == null)
                    {
                        targetCommodity = referenceData.Commodities.FirstOrDefault(commodity =>
                            string.Equals(commodity.Name, previousObservation.CommodityName, StringComparison.OrdinalIgnoreCase));
                        if (targetCommodity == null) continue;
                        marketData.Commodities.Add(targetCommodity);
                    }

                    var targetLocation = marketData.Locations.FirstOrDefault(location =>
                        location.Id == previousObservation.LocationId ||
                        (string.Equals(location.Name, previousObservation.LocationName, StringComparison.OrdinalIgnoreCase) &&
                         string.Equals(location.SystemName, previousObservation.SystemName, StringComparison.OrdinalIgnoreCase)));
                    if (targetLocation == null)
                    {
                        var previousLocation = referenceData.Locations.FirstOrDefault(location =>
                            location.Id == previousObservation.LocationId);
                        if (previousLocation == null) continue;

                        targetLocation = new ScipDashboard.Core.Models.Location
                        {
                            Id = previousLocation.Id,
                            Name = previousLocation.Name,
                            SystemName = previousLocation.SystemName,
                            Type = previousLocation.Type,
                            IsActive = previousLocation.IsActive,
                            HasCommoditiesMarket = previousLocation.HasCommoditiesMarket
                        };
                        marketData.Locations.Add(targetLocation);
                    }

                    var latestObservationAtLocation = marketData.PriceObservations
                        .Where(observation =>
                            observation.CommodityId == targetCommodity.Id &&
                            observation.LocationId == targetLocation.Id)
                        .OrderByDescending(observation => observation.Timestamp)
                        .FirstOrDefault();
                    if (latestObservationAtLocation != null &&
                        latestObservationAtLocation.Timestamp >= previousObservation.Timestamp)
                        continue;

                    marketData.PriceObservations.Add(new PriceObservation
                    {
                        Id = previousObservation.Id,
                        CommodityId = targetCommodity.Id,
                        CommodityName = targetCommodity.Name,
                        LocationId = targetLocation.Id,
                        LocationName = targetLocation.Name,
                        SystemName = targetLocation.SystemName,
                        BuyPrice = previousObservation.BuyPrice,
                        SellPrice = previousObservation.SellPrice,
                        MarginPercent = previousObservation.MarginPercent,
                        Volume = previousObservation.Volume,
                        DemandVolume = previousObservation.DemandVolume,
                        SupportedContainerSizes = previousObservation.SupportedContainerSizes.ToList(),
                        EstimatedLoadingTimeSeconds = previousObservation.EstimatedLoadingTimeSeconds,
                        Timestamp = previousObservation.Timestamp,
                        SourceName = previousObservation.SourceName,
                        SourceStatus = previousObservation.SourceStatus
                    });
                }

                foreach (var referenceCommodity in referenceData.Commodities)
                {
                    var targetCommodity = marketData.Commodities.FirstOrDefault(commodity =>
                        string.Equals(commodity.Name, referenceCommodity.Name, StringComparison.OrdinalIgnoreCase));
                    if (targetCommodity == null)
                    {
                        marketData.Commodities.Add(referenceCommodity);
                        targetCommodity = referenceCommodity;
                    }

                    targetCommodity.ReferenceBuyPrice = referenceCommodity.ReferenceBuyPrice;
                    targetCommodity.ReferenceSellPrice = referenceCommodity.ReferenceSellPrice;
                    targetCommodity.ReferenceMarginPerScu = referenceCommodity.ReferenceMarginPerScu;
                }

                ApplyManualCommodityEntries(marketData);
                await _dataService.SaveMarketDataAsync(marketData);
                _lastRefresh = DateTime.UtcNow;
                CurrentStatus = DataSourceStatus.Live;
            }
            catch (Exception ex)
            {
                _lastError = $"UEXCorp fetch failed: {ex.Message}";
                CurrentStatus = DataSourceStatus.Unavailable;

                // Fall back to sample data
                var marketData = await _dataService.GetMarketDataAsync();
                ApplyManualCommodityEntries(marketData);
                await _dataService.SaveMarketDataAsync(marketData);
                if (marketData.Commodities.Count == 0)
                {
                    await SeedSampleDataFallbackAsync(_dataService);
                }
            }
        }

        private void ApplyManualCommodityEntries(MarketData marketData)
        {
            foreach (var entry in _manualCommodityEntries)
            {
                var commodity = marketData.Commodities.FirstOrDefault(candidate =>
                    string.Equals(candidate.Name, entry.Name, StringComparison.OrdinalIgnoreCase));
                if (commodity == null)
                {
                    commodity = new Commodity
                    {
                        Id = $"manual-{entry.Id}",
                        Name = entry.Name,
                        DisplayName = entry.Name,
                        Category = "Manual",
                        SourceStatus = DataSourceStatus.Sample
                    };
                    marketData.Commodities.Add(commodity);
                }

                Location? buyLocation = null;
                Location? sellLocation = null;
                if (!string.IsNullOrWhiteSpace(entry.BuyLocation))
                    buyLocation = GetOrCreateManualLocation(marketData, entry.BuyLocation);
                if (!string.IsNullOrWhiteSpace(entry.SellLocation))
                    sellLocation = GetOrCreateManualLocation(marketData, entry.SellLocation);

                if (buyLocation != null)
                {
                    var buyObservation = GetOrCreateManualObservation(marketData, entry, commodity, buyLocation);
                    buyObservation.BuyPrice = entry.BuyPrice;
                    buyObservation.Volume = entry.BuyStockScu;
                    if (sellLocation?.Id == buyLocation.Id)
                    {
                        buyObservation.SellPrice = entry.SellPrice;
                        buyObservation.DemandVolume = entry.SellDemandScu;
                    }
                }

                if (sellLocation != null && sellLocation.Id != buyLocation?.Id)
                {
                    var sellObservation = GetOrCreateManualObservation(marketData, entry, commodity, sellLocation);
                    sellObservation.SellPrice = entry.SellPrice;
                    sellObservation.DemandVolume = entry.SellDemandScu;
                }
            }
        }

        private static Location GetOrCreateManualLocation(MarketData marketData, string name)
        {
            var existing = marketData.Locations.FirstOrDefault(location =>
                string.Equals(location.Name, name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
                return existing;

            var id = $"manual-location-{Slug(name)}";
            var manualLocation = new Location
            {
                Id = id,
                Name = name,
                SystemName = "Unknown",
                Type = "Manual location",
                IsActive = true,
                HasCommoditiesMarket = true
            };
            marketData.Locations.Add(manualLocation);
            return manualLocation;
        }

        private static PriceObservation GetOrCreateManualObservation(
            MarketData marketData,
            ManualCommodityEntry entry,
            Commodity commodity,
            Location location)
        {
            var sourceName = $"Manual Entry: {entry.Id}";
            var observation = marketData.PriceObservations.FirstOrDefault(candidate =>
                candidate.SourceName == sourceName &&
                candidate.CommodityId == commodity.Id &&
                candidate.LocationId == location.Id);
            if (observation != null)
                return observation;

            observation = new PriceObservation
            {
                CommodityId = commodity.Id,
                CommodityName = commodity.Name,
                LocationId = location.Id,
                LocationName = location.Name,
                SystemName = location.SystemName,
                Timestamp = DateTime.UtcNow,
                SourceName = sourceName,
                SourceStatus = DataSourceStatus.Sample
            };
            marketData.PriceObservations.Add(observation);
            return observation;
        }

        private static string Slug(string value)
        {
            var slug = new string(value.ToLowerInvariant()
                .Select(character => char.IsLetterOrDigit(character) ? character : '-')
                .ToArray())
                .Trim('-');
            return string.IsNullOrEmpty(slug) ? Guid.NewGuid().ToString("N") : slug;
        }

        private static async Task SeedSampleDataFallbackAsync(IDataService dataService)
        {
            // Keep the existing sample data seeding
            await Task.CompletedTask;
        }

        private static string ReadString(System.Text.Json.JsonElement element, string propertyName, string fallback = "")
        {
            return element.TryGetProperty(propertyName, out var property) &&
                   property.ValueKind == System.Text.Json.JsonValueKind.String
                ? property.GetString() ?? fallback
                : fallback;
        }

        private static decimal AveragePositive(
            IEnumerable<System.Text.Json.JsonElement> rows,
            string propertyName)
        {
            var values = rows
                .Select(row => ReadDecimal(row, propertyName))
                .Where(value => value > 0)
                .ToList();
            return values.Count == 0 ? 0m : values.Average();
        }

        private static int ReadInt(System.Text.Json.JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property)) return 0;
            if (property.ValueKind == System.Text.Json.JsonValueKind.Number && property.TryGetInt32(out var number))
                return number;
            return property.ValueKind == System.Text.Json.JsonValueKind.String &&
                   int.TryParse(property.GetString(), out number)
                ? number
                : 0;
        }

        private static long ReadLong(System.Text.Json.JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property)) return 0;
            if (property.ValueKind == System.Text.Json.JsonValueKind.Number && property.TryGetInt64(out var number))
                return number;
            return property.ValueKind == System.Text.Json.JsonValueKind.String &&
                   long.TryParse(property.GetString(), out number)
                ? number
                : 0;
        }

        private static decimal ReadDecimal(System.Text.Json.JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property)) return 0m;
            if (property.ValueKind == System.Text.Json.JsonValueKind.Number && property.TryGetDecimal(out var number))
                return number;
            return property.ValueKind == System.Text.Json.JsonValueKind.String &&
                   decimal.TryParse(property.GetString(), System.Globalization.NumberStyles.Number,
                       System.Globalization.CultureInfo.InvariantCulture, out number)
                ? number
                : 0m;
        }

        public async Task RefreshFromAllSourcesAsync()
        {
            try
            {
                await RefreshFromUEXCorpAsync();
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                CurrentStatus = DataSourceStatus.Unavailable;
            }
        }

        // ==================== SEARCH & QUERY ====================

        public async Task<List<Commodity>> SearchCommoditiesAsync(string query)
        {
            var allCommodities = await _dataService.GetCommoditiesAsync();
            
            if (string.IsNullOrWhiteSpace(query))
                return allCommodities;

            query = query.ToLower();
            
            return allCommodities.Where(c => 
                c.Name.ToLower().Contains(query) ||
                c.DisplayName.ToLower().Contains(query) ||
                c.Category.ToString().ToLower().Contains(query))
                .ToList();
        }

        public async Task<List<PriceObservation>> GetPriceHistoryAsync(string commodityId, int hours = 24)
        {
            var allObservations = await _dataService.GetPriceObservationsAsync(commodityId);
            var cutoff = DateTime.UtcNow.AddHours(-hours);
            
            return allObservations.Where(o => o.Timestamp >= cutoff)
                .OrderBy(o => o.Timestamp)
                .ToList();
        }

        public async Task AddManualPriceObservationAsync(ScipDashboard.Core.Models.PriceObservation observation)
        {
            observation.Id = Guid.NewGuid().ToString();
            observation.Timestamp = DateTime.UtcNow;
            observation.SourceStatus = DataSourceStatus.Sample;
            observation.SourceName = "Manual Entry";

            await _dataService.AddPriceObservationAsync(observation);
        }
    }
}
