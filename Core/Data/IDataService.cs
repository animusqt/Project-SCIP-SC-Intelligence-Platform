using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

// Do NOT define placeholder classes - use fully qualified types from Models.cs

namespace ScipDashboard.Core.Data
{
    // IDataService interface using complete types from Models namespace
    public interface IDataService
    {
        Task<ScipDashboard.Core.Models.MarketData> GetMarketDataAsync(bool forceRefresh = false);
        Task<List<ScipDashboard.Core.Models.Commodity>> GetCommoditiesAsync();
        Task<List<ScipDashboard.Core.Models.PriceObservation>> GetPriceObservationsAsync(string commodityId);
        Task AddPriceObservationAsync(ScipDashboard.Core.Models.PriceObservation observation);
        Task SaveMarketDataAsync(ScipDashboard.Core.Models.MarketData data);
        Task<List<ScipDashboard.Core.Models.Ship>> GetShipsAsync();
        Task CreateRouteAsync(ScipDashboard.Core.Models.Route route);
        Task<List<ScipDashboard.Core.Models.Route>> GetRoutesAsync();
        Task UpdateAutoRefreshInterval(int intervalMinutes);
        Task SetPreferredDataSource(string sourceName);
        Task<List<ScipDashboard.Core.Models.WatchlistItem>> GetWatchlistAsync();
        Task AddToWatchlistAsync(ScipDashboard.Core.Models.WatchlistItem item);
        Task RemoveFromWatchlistAsync(string id);
        Task UpdateCommodityPriceAsync(string commodityId, string commodityName, string locationId, string locationName, string systemName, decimal buyPrice, decimal sellPrice, int volume, int demandVolume, DateTime timestamp, string sourceName, List<int>? supportedContainerSizes = null, int? estimatedLoadingTimeSeconds = null);
    }

    // Complete implementation using Models types
    public class DataService : IDataService
    {
        private readonly List<ScipDashboard.Core.Models.MarketData> _marketData = [];
        private readonly List<ScipDashboard.Core.Models.Commodity> _commodities = [];
        private readonly List<ScipDashboard.Core.Models.PriceObservation> _observations = [];
        private readonly SemaphoreSlim _marketDataLock = new(1, 1);
        private readonly string _marketDataPath;
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public DataService()
            : this(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SCIP Dashboard",
                "market-data.json"))
        {
        }

        public DataService(string marketDataPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(marketDataPath);
            _marketDataPath = marketDataPath;
        }
        
        public async Task SaveMarketDataAsync(ScipDashboard.Core.Models.MarketData data)
        {
            ArgumentNullException.ThrowIfNull(data);
            await _marketDataLock.WaitAsync();
            try
            {
                await SaveMarketDataCoreAsync(data);
            }
            finally
            {
                _marketDataLock.Release();
            }
        }

        
        // Placeholder - no-op for now
        public async Task RefreshDataAsync()
        {
            // Simulate data refresh
        }
        
        public async Task<ScipDashboard.Core.Models.MarketData> GetMarketDataAsync(bool forceRefresh = false)
        {
            await _marketDataLock.WaitAsync();
            try
            {
                if (_marketData.Count > 0 && !forceRefresh)
                    return _marketData[0];

                var referenceData = await LoadDataHubDataAsync();
                ScipDashboard.Core.Models.MarketData marketData;
                if (File.Exists(_marketDataPath))
                {
                    var json = await File.ReadAllTextAsync(_marketDataPath);
                    marketData = JsonSerializer.Deserialize<ScipDashboard.Core.Models.MarketData>(json, _jsonOptions)
                        ?? throw new InvalidDataException($"Saved market data at '{_marketDataPath}' is empty or invalid.");
                    marketData.Commodities ??= [];
                    marketData.Locations ??= [];
                    marketData.PriceObservations ??= [];
                }
                else
                {
                    marketData = referenceData;
                }

                MergeReferenceCatalog(marketData, referenceData);
                await SaveMarketDataCoreAsync(marketData);
                return marketData;
            }
            finally
            {
                _marketDataLock.Release();
            }
        }

        private async Task SaveMarketDataCoreAsync(ScipDashboard.Core.Models.MarketData data)
        {
            data.Timestamp = data.Timestamp == default ? DateTime.UtcNow : data.Timestamp;
            var directory = Path.GetDirectoryName(_marketDataPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            var temporaryPath = $"{_marketDataPath}.{Guid.NewGuid():N}.tmp";
            try
            {
                await using (var output = new FileStream(
                    temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                {
                    await JsonSerializer.SerializeAsync(output, data, _jsonOptions);
                    await output.FlushAsync();
                }

                File.Move(temporaryPath, _marketDataPath, overwrite: true);
                _marketData.Clear();
                _marketData.Add(data);
                _commodities.Clear();
                _commodities.AddRange(data.Commodities);
                _observations.Clear();
                _observations.AddRange(data.PriceObservations);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        private static void MergeReferenceCatalog(
            ScipDashboard.Core.Models.MarketData marketData,
            ScipDashboard.Core.Models.MarketData referenceData)
        {
            foreach (var reference in referenceData.Commodities)
            {
                var existing = marketData.Commodities.FirstOrDefault(commodity =>
                    string.Equals(commodity.Name, reference.Name, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                {
                    marketData.Commodities.Add(reference);
                    continue;
                }

                existing.ReferenceBuyPrice = reference.ReferenceBuyPrice;
                existing.ReferenceSellPrice = reference.ReferenceSellPrice;
                existing.ReferenceMarginPerScu = reference.ReferenceMarginPerScu;
                if (string.IsNullOrWhiteSpace(existing.Category) || existing.Category == "General")
                    existing.Category = reference.Category;
            }

            var obsoleteReferenceIds = marketData.PriceObservations
                .Where(observation =>
                    string.Equals(observation.SourceName, "SC DataHub reference", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(observation.SourceName, "Sample", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(observation.SourceName, "Sample Data", StringComparison.OrdinalIgnoreCase))
                .Select(observation => observation.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            marketData.PriceObservations.RemoveAll(observation => obsoleteReferenceIds.Contains(observation.Id));

            var usedLocationIds = marketData.PriceObservations
                .Select(observation => observation.LocationId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            marketData.Locations.RemoveAll(location =>
                string.Equals(location.Id, "sc-datahub-reference", StringComparison.OrdinalIgnoreCase) &&
                !usedLocationIds.Contains(location.Id));
            marketData.Name = "SC DataHub baseline + UEX live market";
        }

        public async Task<List<ScipDashboard.Core.Models.Commodity>> GetCommoditiesAsync()
        {
            if (_commodities.Count == 0)
            {
                await GetMarketDataAsync();
            }
            return _commodities;
        }
        
        private async Task<ScipDashboard.Core.Models.MarketData> LoadDataHubDataAsync()
        {
            var dataFilePath = Path.Combine(AppContext.BaseDirectory, "Build Doc", "sc_datahub_commodities_filtered.txt");
            if (!File.Exists(dataFilePath))
                throw new FileNotFoundException("The SC DataHub commodity reference file was not packaged with the dashboard.", dataFilePath);

            var marketData = new ScipDashboard.Core.Models.MarketData
            {
                Name = "SC DataHub Reference Prices",
                Timestamp = DateTime.UtcNow,
                Locations = [],
                Commodities = new List<ScipDashboard.Core.Models.Commodity>(),
                PriceObservations = new List<ScipDashboard.Core.Models.PriceObservation>()
            };

            var lines = File.ReadAllLines(dataFilePath);
            bool inCommoditiesSection = false;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.Equals("COMMODITIES", StringComparison.OrdinalIgnoreCase))
                {
                    inCommoditiesSection = true;
                    continue;
                }

                if (!inCommoditiesSection) continue;
                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                if (trimmed.StartsWith("==") || trimmed.StartsWith("---") || trimmed.StartsWith("- ")) continue;

                var cols = trimmed.Split('|');
                if (cols.Length < 2) continue;

                var commodityName = cols[0].Trim('"', ' ');
                if (string.IsNullOrWhiteSpace(commodityName)) continue;
                if (commodityName.Equals("Commodity", StringComparison.OrdinalIgnoreCase)) continue;

                var category = cols[1].Trim('"', ' ');
                var buyPrice = ParseAuecPrice(cols.Length > 2 ? cols[2] : string.Empty);
                var sellPrice = ParseAuecPrice(cols.Length > 3 ? cols[3] : string.Empty);
                var marginPerScu = ParseAuecPrice(cols.Length > 4 ? cols[4] : string.Empty);

                var commodity = new ScipDashboard.Core.Models.Commodity
                {
                    Id = Regex.Replace(commodityName.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-'),
                    Name = commodityName,
                    DisplayName = commodityName,
                    Category = category,
                    PricePerScu = buyPrice > 0 ? buyPrice : sellPrice,
                    ReferenceMarginPerScu = marginPerScu,
                    CurrentBuyPrice = buyPrice,
                    CurrentSellPrice = sellPrice,
                    ReferenceBuyPrice = buyPrice,
                    ReferenceSellPrice = sellPrice,
                    MarginPercent = buyPrice > 0 ? Math.Round(((sellPrice - buyPrice) / buyPrice) * 100, 2) : 0m,
                    IsIllegal = false,
                    SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample
                };
                if (marketData.Commodities.Any(existing => existing.Id == commodity.Id))
                    continue;

                marketData.Commodities.Add(commodity);
            }

            return marketData;
        }

        private decimal ParseAuecPrice(string priceStr)
        {
            if (string.IsNullOrWhiteSpace(priceStr)) return 0m;
            if (priceStr.Trim() is "—" or "-") return 0m;

            var cleaned = priceStr.Replace("aUEC", "", StringComparison.OrdinalIgnoreCase).Replace(",", "").Trim();
            if (decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
                return result;

            return 0m;
        }

        public async Task<List<ScipDashboard.Core.Models.PriceObservation>> GetPriceObservationsAsync(string commodityId)
        {
            var marketData = await GetMarketDataAsync();
            return marketData.PriceObservations
                .Where(observation => observation.CommodityId == commodityId)
                .OrderByDescending(observation => observation.Timestamp)
                .ToList();
        }
        
        public async Task AddPriceObservationAsync(ScipDashboard.Core.Models.PriceObservation observation)
        {
            var marketData = await GetMarketDataAsync();
            observation.Id = Guid.NewGuid().ToString();
            observation.Timestamp = DateTime.UtcNow;
            observation.SourceName = "Manual Entry";
            marketData.PriceObservations.Add(observation);
            await SaveMarketDataAsync(marketData);
        }

        public async Task<List<ScipDashboard.Core.Models.Ship>> GetShipsAsync()
        {
            var ships = new List<ScipDashboard.Core.Models.Ship>
            {
                new ScipDashboard.Core.Models.Ship { Name = "Cutter", TotalCapacitySCU = 24, CurrentLoadSCU = 0, AverageSpeed = 100m, IsAvailable = true },
                new ScipDashboard.Core.Models.Ship { Name = "Freelancer", TotalCapacitySCU = 96, CurrentLoadSCU = 0, AverageSpeed = 90m, IsAvailable = true },
                new ScipDashboard.Core.Models.Ship { Name = "Caterpillar", TotalCapacitySCU = 576, CurrentLoadSCU = 0, AverageSpeed = 80m, IsAvailable = true }
            };
            return await Task.FromResult(ships);
        }

        private readonly List<ScipDashboard.Core.Models.Route> _routes = [];

        public async Task CreateRouteAsync(ScipDashboard.Core.Models.Route route)
        {
            _routes.Add(route);
            await Task.CompletedTask;
        }

        public async Task<List<ScipDashboard.Core.Models.Route>> GetRoutesAsync()
        {
            return await Task.FromResult(_routes);
        }

        public async Task UpdateAutoRefreshInterval(int intervalMinutes)
        {
            await Task.CompletedTask;
        }

        public async Task SetPreferredDataSource(string sourceName)
        {
            await Task.CompletedTask;
        }

        private readonly List<ScipDashboard.Core.Models.WatchlistItem> _watchlist = [];

        public async Task<List<ScipDashboard.Core.Models.WatchlistItem>> GetWatchlistAsync()
        {
            return await Task.FromResult(_watchlist);
        }

        public async Task AddToWatchlistAsync(ScipDashboard.Core.Models.WatchlistItem item)
        {
            _watchlist.Add(item);
            await Task.CompletedTask;
        }

        public async Task RemoveFromWatchlistAsync(string id)
        {
            _watchlist.RemoveAll(w => w.Id.ToString() == id);
            await Task.CompletedTask;
        }

        public async Task UpdateCommodityPriceAsync(string commodityId, string commodityName, string locationId, string locationName, string systemName, decimal buyPrice, decimal sellPrice, int volume, int demandVolume, DateTime timestamp, string sourceName, List<int>? supportedContainerSizes = null, int? estimatedLoadingTimeSeconds = null)
        {
            var marketData = await GetMarketDataAsync();
            var commodity = marketData.Commodities.FirstOrDefault(c => c.Id == commodityId) ??
                marketData.Commodities.FirstOrDefault(c =>
                    string.Equals(c.Name, commodityName, StringComparison.OrdinalIgnoreCase));
            if (commodity == null)
            {
                commodity = new ScipDashboard.Core.Models.Commodity
                {
                    Id = commodityId,
                    Name = commodityName,
                    DisplayName = commodityName,
                    CurrentBuyPrice = buyPrice,
                    CurrentSellPrice = sellPrice,
                    MarginPercent = buyPrice > 0 ? Math.Round(((sellPrice - buyPrice) / buyPrice) * 100, 2) : 0m,
                    SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample
                };
                marketData.Commodities.Add(commodity);
                _commodities.Add(commodity);
            }
            else
            {
                commodityId = commodity.Id;
                if (buyPrice > 0)
                    commodity.CurrentBuyPrice = buyPrice;
                if (sellPrice > 0)
                    commodity.CurrentSellPrice = sellPrice;
                commodity.MarginPercent = commodity.CurrentBuyPrice > 0
                    ? Math.Round(((commodity.CurrentSellPrice - commodity.CurrentBuyPrice) / commodity.CurrentBuyPrice) * 100, 2)
                    : 0m;
            }

            var matchingLocation = marketData.Locations.FirstOrDefault(location =>
                location.Id == locationId ||
                (string.Equals(location.Name, locationName, StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(location.SystemName, systemName, StringComparison.OrdinalIgnoreCase)));
            if (matchingLocation != null)
            {
                locationId = matchingLocation.Id;
                locationName = matchingLocation.Name;
                systemName = matchingLocation.SystemName;
            }
            else
            {
                marketData.Locations.Add(new ScipDashboard.Core.Models.Location
                {
                    Id = locationId,
                    Name = locationName,
                    SystemName = systemName,
                    Type = "Terminal",
                    IsActive = true,
                    HasCommoditiesMarket = true
                });
            }

            var observation = new ScipDashboard.Core.Models.PriceObservation
            {
                Id = Guid.NewGuid().ToString(),
                CommodityId = commodityId,
                CommodityName = commodityName,
                LocationId = locationId,
                LocationName = locationName,
                SystemName = systemName,
                BuyPrice = buyPrice,
                SellPrice = sellPrice,
                Volume = volume,
                DemandVolume = demandVolume,
                SupportedContainerSizes = supportedContainerSizes?.Distinct().OrderBy(size => size).ToList() ?? [],
                EstimatedLoadingTimeSeconds = estimatedLoadingTimeSeconds,
                Timestamp = timestamp,
                SourceName = sourceName,
                MarginPercent = buyPrice > 0 ? Math.Round(((sellPrice - buyPrice) / buyPrice) * 100, 2) : 0m
            };

            var existingObservation = marketData.PriceObservations
                .Where(existing => existing.CommodityId == commodityId && existing.LocationId == locationId)
                .OrderByDescending(existing => existing.Timestamp)
                .FirstOrDefault();
            if (existingObservation == null)
            {
                marketData.PriceObservations.Add(observation);
            }
            else
            {
                existingObservation.CommodityName = observation.CommodityName;
                existingObservation.BuyPrice = observation.BuyPrice;
                existingObservation.SellPrice = observation.SellPrice;
                existingObservation.Volume = observation.Volume;
                existingObservation.DemandVolume = observation.DemandVolume;
                if (supportedContainerSizes is { Count: > 0 })
                    existingObservation.SupportedContainerSizes = observation.SupportedContainerSizes;
                if (estimatedLoadingTimeSeconds.HasValue)
                    existingObservation.EstimatedLoadingTimeSeconds = estimatedLoadingTimeSeconds;
                existingObservation.Timestamp = observation.Timestamp;
                existingObservation.SourceName = observation.SourceName;
                existingObservation.SourceStatus = observation.SourceStatus;
                existingObservation.MarginPercent = observation.MarginPercent;
                observation = existingObservation;
            }

            _observations.Add(observation);
            await SaveMarketDataAsync(marketData);
        }
    }
}
