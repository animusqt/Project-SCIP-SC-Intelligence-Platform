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
        DataSourceStatus CurrentStatus { get; }
        DateTime LastRefresh { get; }
        string? LastError { get; }
    }

    public class MarketDataService : IMarketDataService
    {
        private readonly IDataService _dataService;
        private readonly HttpClient _httpClient;
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

        // ==================== MAIN DATA ACCESS ====================

        public async Task<MarketData> GetMarketDataAsync(bool forceRefresh = false)
        {
            if (forceRefresh)
            {
                await RefreshFromUEXCorpAsync();
            }

            return await _dataService.GetMarketDataAsync();
        }

        public async Task<MarketData> GetMarketDataWithCacheAsync()
        {
            // Try to get fresh data
            try
            {
                await RefreshFromUEXCorpAsync();
                CurrentStatus = DataSourceStatus.Live;
            }
            catch
            {
                // Fall back to cached/sample data
                CurrentStatus = DataSourceStatus.Cached;
            }

            return await _dataService.GetMarketDataAsync();
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
                var pricesResponse = await _httpClient.GetStringAsync("https://uexcorp.space/api/2.0/commodities_prices/");

                using var commoditiesDoc = System.Text.Json.JsonDocument.Parse(commoditiesResponse);
                using var pricesDoc = System.Text.Json.JsonDocument.Parse(pricesResponse);

                var commoditiesArray = commoditiesDoc.RootElement.GetProperty("data");
                var pricesArray = pricesDoc.RootElement.GetProperty("data");

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
                    var code = c.TryGetProperty("code", out var codeProp) ? codeProp.GetString() ?? "" : "";
                    var kind = c.TryGetProperty("kind", out var kindProp) ? kindProp.GetString() ?? "" : "";
                    var priceBuy = c.TryGetProperty("price_buy", out var pb) ? pb.GetDecimal() : 0m;
                    var priceSell = c.TryGetProperty("price_sell", out var ps) ? ps.GetDecimal() : 0m;
                    var isIllegal = c.TryGetProperty("is_illegal", out var ii) && ii.GetInt32() == 1;
                    var isVisible = c.TryGetProperty("is_visible", out var iv) && iv.GetInt32() == 1;

                    if (!isVisible) continue;

                    var commodity = new ScipDashboard.Core.Models.Commodity
                    {
                        Id = id.ToString(),
                        Name = name,
                        DisplayName = name,
                        Category = kind,
                        PricePerScu = priceBuy > 0 ? priceBuy : priceSell,
                        CurrentBuyPrice = priceBuy,
                        CurrentSellPrice = priceSell,
                        SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Live,
                        IsIllegal = isIllegal
                    };

                    commodityMap[id] = commodity;
                    marketData.Commodities.Add(commodity);
                }

                // Parse prices - group by commodity to find best buy/sell
                var commodityPrices = new Dictionary<int, List<System.Text.Json.JsonElement>>();
                foreach (var p in pricesArray.EnumerateArray())
                {
                    var commId = p.GetProperty("id_commodity").GetInt32();
                    if (!commodityPrices.ContainsKey(commId))
                        commodityPrices[commId] = new List<System.Text.Json.JsonElement>();
                    commodityPrices[commId].Add(p);
                }

                // Build price observations
                foreach (var kvp in commodityPrices)
                {
                    var commId = kvp.Key;
                    if (!commodityMap.ContainsKey(commId)) continue;

                    var commodity = commodityMap[commId];
                    foreach (var price in kvp.Value)
                    {
                        var terminalName = price.TryGetProperty("terminal_name", out var tn) ? tn.GetString() ?? "Unknown" : "Unknown";
                        var locationName = price.TryGetProperty("space_station_name", out var ssn) && ssn.ValueKind != System.Text.Json.JsonValueKind.Null
                            ? ssn.GetString() ?? terminalName
                            : (price.TryGetProperty("city_name", out var cn) && cn.ValueKind != System.Text.Json.JsonValueKind.Null ? cn.GetString() ?? terminalName : terminalName);
                        var starSystem = price.TryGetProperty("star_system_name", out var ss) ? ss.GetString() ?? "" : "";

                        var priceBuy = price.TryGetProperty("price_buy", out var pb) ? pb.GetDecimal() : 0m;
                        var priceSell = price.TryGetProperty("price_sell", out var ps) ? ps.GetDecimal() : 0m;
                        var scuBuy = price.TryGetProperty("scu_buy", out var sb) ? sb.GetInt32() : 0;
                        var scuSellStock = price.TryGetProperty("scu_sell_stock", out var ss2) ? ss2.GetInt32() : 0;
                        var dateModified = price.TryGetProperty("date_modified", out var dm) ? dm.GetInt64() : 0L;

                        var locationId = $"loc-{starSystem}-{terminalName}".Replace(" ", "-").ToLower();

                        // Add location if not exists
                        if (!marketData.Locations.Any(l => l.Id == locationId))
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

                        marketData.PriceObservations.Add(new ScipDashboard.Core.Models.PriceObservation
                        {
                            Id = Guid.NewGuid().ToString(),
                            CommodityId = commId.ToString(),
                            CommodityName = commodity.Name,
                            LocationId = locationId,
                            LocationName = terminalName,
                            BuyPrice = priceBuy,
                            SellPrice = priceSell,
                            Volume = scuBuy,
                            DemandVolume = scuSellStock,
                            Timestamp = dateModified > 0 ? DateTimeOffset.FromUnixTimeSeconds(dateModified).UtcDateTime : DateTime.UtcNow,
                            SourceName = "UEXCorp",
                            SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Live
                        });
                    }
                }

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
                if (marketData.Commodities.Count == 0)
                {
                    await SeedSampleDataFallbackAsync(_dataService);
                }
            }
        }

        private static async Task SeedSampleDataFallbackAsync(IDataService dataService)
        {
            // Ensure SC-Truckers sample data is seeded
            await dataService.GetCommoditiesAsync();
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

