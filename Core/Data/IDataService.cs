using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        Task UpdateCommodityPriceAsync(string commodityId, string locationId, string locationName, string systemName, decimal buyPrice, decimal sellPrice, int volume, int demandVolume, DateTime timestamp, string sourceName);
    }

    // Complete implementation using Models types
    public class DataService : IDataService
    {
        // Private fields use Models namespace types directly (compiler will resolve them)
        private readonly List<ScipDashboard.Core.Models.MarketData> _marketData = [];
        private readonly List<ScipDashboard.Core.Models.Commodity> _commodities = [];
        private readonly List<ScipDashboard.Core.Models.PriceObservation> _observations = [];
        
        public async Task SaveMarketDataAsync(ScipDashboard.Core.Models.MarketData data)
        {
            _marketData.Clear();
            _marketData.Add(data);
        }

        
        // Placeholder - no-op for now
        public async Task RefreshDataAsync()
        {
            // Simulate data refresh
        }
        
        public async Task<ScipDashboard.Core.Models.MarketData> GetMarketDataAsync(bool forceRefresh = false)
        {
            if (_marketData.Count > 0 && !forceRefresh)
                return _marketData[0];

            // Load SC DataHub commodity data
            var marketData = await LoadDataHubDataAsync();
            
            if (_marketData.Count == 0)
                await SaveMarketDataAsync(marketData);

            return marketData;
        }

        public async Task<List<ScipDashboard.Core.Models.Commodity>> GetCommoditiesAsync()
        {
            if (_commodities.Count == 0)
            {
                await LoadDataHubDataAsync();
            }
            return _commodities;
        }
        
        private async Task<ScipDashboard.Core.Models.MarketData> LoadDataHubDataAsync()
        {
            var dataFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Build Doc", "sc_datahub_commodities_filtered.txt");
            
            if (!File.Exists(dataFilePath))
            {
                // Fallback: try relative path
                dataFilePath = Path.Combine(AppContext.BaseDirectory, "Build Doc", "sc_datahub_commodities_filtered.txt");
            }

            var marketData = new ScipDashboard.Core.Models.MarketData
            {
                Name = "SC DataHub Market Data",
                Timestamp = DateTime.UtcNow,
                Locations = new List<ScipDashboard.Core.Models.Location>(),
                Commodities = new List<ScipDashboard.Core.Models.Commodity>(),
                PriceObservations = new List<ScipDashboard.Core.Models.PriceObservation>()
            };

            // Create some sample locations
            var locations = new List<ScipDashboard.Core.Models.Location>
            {
                new() { Id = "loc-nb", Name = "New Babbage", SystemName = "Stanton", Type = "City", IsActive = true, HasCommoditiesMarket = true },
                new() { Id = "loc-lor", Name = "Lorville", SystemName = "Hurston", Type = "City", IsActive = true, HasCommoditiesMarket = true },
                new() { Id = "loc-olisar", Name = "Port Olisar", SystemName = "Stanton", Type = "Spaceport", IsActive = true, HasCommoditiesMarket = true },
                new() { Id = "loc-mic", Name = "MicroTech", SystemName = "Stanton", Type = "Spaceport", IsActive = true, HasCommoditiesMarket = true },
                new() { Id = "loc-everus", Name = "Everus Harbor", SystemName = "Stanton", Type = "Spaceport", IsActive = true, HasCommoditiesMarket = true },
                new() { Id = "loc-pyro1", Name = "Prospect", SystemName = "Pyro", Type = "Spaceport", IsActive = true, HasCommoditiesMarket = true },
                new() { Id = "loc-nyx1", Name = "Riot", SystemName = "Nyx", Type = "Spaceport", IsActive = true, HasCommoditiesMarket = true },
            };
            marketData.Locations = locations;

            if (!File.Exists(dataFilePath))
            {
                // Fallback to sample data if file not found
                for (int i = 0; i < 10; i++)
                {
                    var commodity = new ScipDashboard.Core.Models.Commodity
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = $"Commodity-{i+1}",
                        DisplayName = $"Commodity-{i+1}",
                        CurrentBuyPrice = 10 + (i * 2),
                        CurrentSellPrice = 15 + (i * 2),
                        MarginPercent = 5,
                        IsIllegal = false,
                        SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample
                    };
                    _commodities.Add(commodity);
                    marketData.Commodities.Add(commodity);

                    foreach (var loc in locations)
                    {
                        var obs = new ScipDashboard.Core.Models.PriceObservation
                        {
                            Id = Guid.NewGuid().ToString(),
                            CommodityId = commodity.Id,
                            LocationId = loc.Id,
                            LocationName = loc.Name,
                            SystemName = loc.SystemName,
                            BuyPrice = commodity.CurrentBuyPrice,
                            SellPrice = commodity.CurrentSellPrice,
                            Volume = 1000,
                            DemandVolume = 800,
                            Timestamp = DateTime.UtcNow.AddHours(-new Random().Next(1, 24)),
                            SourceName = "SC DataHub",
                            MarginPercent = commodity.MarginPercent
                        };
                        _observations.Add(obs);
                        marketData.PriceObservations.Add(obs);
                    }
                }
                return marketData;
            }

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
                if (trimmed.StartsWith("==") || trimmed.StartsWith("---")) continue;

                var cols = trimmed.Split('|');
                if (cols.Length < 3) continue;

                var commodityName = cols[0].Trim('"', ' ');
                if (string.IsNullOrWhiteSpace(commodityName)) continue;
                if (commodityName.Equals("Commodity", StringComparison.OrdinalIgnoreCase)) continue;

                var buyPriceStr = cols.Length > 2 ? cols[2].Trim('"', ' ') : string.Empty;
                var sellPriceStr = cols.Length > 3 ? cols[3].Trim('"', ' ') : string.Empty;

                decimal buyPrice = ParseAuecPrice(buyPriceStr);
                decimal sellPrice = ParseAuecPrice(sellPriceStr);

                if (buyPrice <= 0 && sellPrice <= 0) continue;

                var commodity = new ScipDashboard.Core.Models.Commodity
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = commodityName,
                    DisplayName = commodityName,
                    CurrentBuyPrice = buyPrice,
                    CurrentSellPrice = sellPrice,
                    MarginPercent = buyPrice > 0 ? Math.Round(((sellPrice - buyPrice) / buyPrice) * 100, 2) : 0m,
                    IsIllegal = false,
                    SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample
                };
                _commodities.Add(commodity);
                marketData.Commodities.Add(commodity);

                // Add price observations for each location
                foreach (var loc in locations)
                {
                    // Add some price variation per location
                    var locationVariation = new Random((int)(commodity.Id.GetHashCode() + loc.Id.GetHashCode()));
                    var buyVariation = buyPrice * (0.9m + (decimal)locationVariation.NextDouble() * 0.2m);
                    var sellVariation = sellPrice * (0.9m + (decimal)locationVariation.NextDouble() * 0.2m);

                    var obs = new ScipDashboard.Core.Models.PriceObservation
                    {
                        Id = Guid.NewGuid().ToString(),
                        CommodityId = commodity.Id,
                        LocationId = loc.Id,
                        LocationName = loc.Name,
                        SystemName = loc.SystemName,
                        BuyPrice = Math.Round(buyVariation, 2),
                        SellPrice = Math.Round(sellVariation, 2),
                        Volume = 1000,
                        DemandVolume = 800,
                        Timestamp = DateTime.UtcNow.AddHours(-new Random().Next(1, 24)),
                        SourceName = "SC DataHub",
                        MarginPercent = buyVariation > 0 ? Math.Round(((sellVariation - buyVariation) / buyVariation) * 100, 2) : 0m
                    };
                    _observations.Add(obs);
                    marketData.PriceObservations.Add(obs);
                }
            }

            return marketData;
        }

        private decimal ParseAuecPrice(string priceStr)
        {
            if (string.IsNullOrWhiteSpace(priceStr)) return 0m;
            if (priceStr.Trim() == "—") return 0m;

            var cleaned = priceStr.Replace("aUEC", "").Replace(",", "").Trim();
            if (decimal.TryParse(cleaned, out decimal result))
                return result;

            return 0m;
        }

        public async Task<List<ScipDashboard.Core.Models.PriceObservation>> GetPriceObservationsAsync(string commodityId)
        {
            var observations = _observations.Where(o => o.CommodityId == commodityId).ToList();
            if (observations.Count == 0)
            {
                // Populate with sample data
                for (int i = 0; i < 5; i++)
                {
                    var obs = new ScipDashboard.Core.Models.PriceObservation
                    {
                        Id = Guid.NewGuid().ToString(),
                        CommodityId = commodityId,
                        Timestamp = DateTime.UtcNow.AddHours(-i * 12),
                        SourceName = "Sample"
                    };
                    
                    var comp = _commodities.FirstOrDefault(c => c.Id == commodityId);
                    if (comp != null)
                    {
                        obs.BuyPrice = comp.CurrentBuyPrice;
                        obs.SellPrice = comp.CurrentSellPrice + 5;
                        obs.MarginPercent = 7.5m;
                    }
                    
                    _observations.Add(obs);
                }
            }
            return observations;
        }
        
        public async Task AddPriceObservationAsync(ScipDashboard.Core.Models.PriceObservation observation)
        {
            observation.Id = Guid.NewGuid().ToString();
            observation.Timestamp = DateTime.UtcNow;
            observation.SourceName = "Manual Entry";
            _observations.Add(observation);
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

        public async Task UpdateCommodityPriceAsync(string commodityId, string locationId, string locationName, string systemName, decimal buyPrice, decimal sellPrice, int volume, int demandVolume, DateTime timestamp, string sourceName)
        {
            // Try to find existing commodity
            var commodity = _commodities.FirstOrDefault(c => c.Id == commodityId);
            if (commodity == null)
            {
                // Create new commodity entry
                commodity = new ScipDashboard.Core.Models.Commodity
                {
                    Id = commodityId,
                    Name = locationName,
                    DisplayName = locationName,
                    CurrentBuyPrice = buyPrice,
                    CurrentSellPrice = sellPrice,
                    MarginPercent = buyPrice > 0 ? Math.Round(((sellPrice - buyPrice) / buyPrice) * 100, 2) : 0m,
                    SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample
                };
                _commodities.Add(commodity);
            }
            else
            {
                // Update existing commodity prices
                commodity.CurrentBuyPrice = buyPrice;
                commodity.CurrentSellPrice = sellPrice;
                commodity.MarginPercent = buyPrice > 0 ? Math.Round(((sellPrice - buyPrice) / buyPrice) * 100, 2) : 0m;
            }

            // Add price observation
            var observation = new ScipDashboard.Core.Models.PriceObservation
            {
                Id = Guid.NewGuid().ToString(),
                CommodityId = commodityId,
                LocationId = locationId,
                LocationName = locationName,
                SystemName = systemName,
                BuyPrice = buyPrice,
                SellPrice = sellPrice,
                Volume = volume,
                DemandVolume = demandVolume,
                Timestamp = timestamp,
                SourceName = sourceName,
                MarginPercent = buyPrice > 0 ? Math.Round(((sellPrice - buyPrice) / buyPrice) * 100, 2) : 0m
            };
            _observations.Add(observation);

            await Task.CompletedTask;
        }
    }
}
