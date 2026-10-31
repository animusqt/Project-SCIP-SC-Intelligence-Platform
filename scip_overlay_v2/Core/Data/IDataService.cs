using System;
using System.Collections.Generic;
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

            // Build market data from seeded commodities and observations if none saved yet
            await GetCommoditiesAsync(); // ensures seeding happens

            var marketData = new ScipDashboard.Core.Models.MarketData
            {
                Name = "SC-Truckers Sample Market Data",
                Timestamp = DateTime.UtcNow,
                Locations = new List<ScipDashboard.Core.Models.Location>(),
                Commodities = new List<ScipDashboard.Core.Models.Commodity>(_commodities),
                PriceObservations = new List<ScipDashboard.Core.Models.PriceObservation>(_observations),
            };

            // Build unique locations from observations
            var locationIds = new HashSet<string>();
            foreach (var obs in _observations)
            {
                if (!locationIds.Contains(obs.LocationId))
                {
                    locationIds.Add(obs.LocationId);
                    marketData.Locations.Add(new ScipDashboard.Core.Models.Location
                    {
                        Id = obs.LocationId,
                        Name = obs.LocationName,
                        SystemName = obs.SystemName,
                        Type = "Terminal",
                        IsActive = true,
                        HasCommoditiesMarket = true,
                    });
                }
            }

            if (!forceRefresh)
                await SaveMarketDataAsync(marketData);

            return marketData;
        }
        
        public async Task<List<ScipDashboard.Core.Models.Commodity>> GetCommoditiesAsync()
        {
            if (_commodities.Count == 0)
            {
                // Seed with SC-Truckers real commodity categories and names
                SeedScTruckersData();
            }
            return _commodities;
        }

        private void SeedScTruckersData()
        {
            // SC-Truckers station/terminal locations
            var stations = new[]
            {
                new { Name = "Area18", System = "Stanton", Shop = "Cubby Blast" },
                new { Name = "Lorville", System = "Stanton", Shop = "Dumper's Depot" },
                new { Name = "Everus Harbor", System = "Stanton", Shop = "Platinum Bay" },
                new { Name = "New Babbage", System = "Stanton", Shop = "New Deal" },
                new { Name = "Baijini Point", System = "Stanton", Shop = "Platinum Bay" },
                new { Name = "Port Tressler", System = "Stanton", Shop = "Platinum Bay" },
                new { Name = "Orison", System = "Stanton", Shop = "Omni Polo" },
            };

            var commodityData = new[]
            {
                // Metal / Ore
                ("Quantanium", "Metal / Ore", 24500, 26800, false),
                ("Laranite", "Metal / Ore", 8900, 11200, false),
                ("Titanium", "Metal / Ore", 1850, 2100, false),
                ("Tungsten", "Metal / Ore", 4200, 5100, false),
                ("Iron", "Metal / Ore", 350, 450, false),
                ("Gold", "Metal / Ore", 15500, 18900, false),
                ("Diamond", "Metal / Ore", 28000, 34000, false),
                ("Copper", "Metal / Ore", 1200, 1450, false),
                ("Aluminum", "Metal / Ore", 950, 1150, false),
                ("Beryl", "Metal / Ore", 6800, 8200, false),
                ("Borase", "Metal / Ore", 12500, 15200, false),
                // Bio / Contraband
                ("Altruciatoxin", "Bio / Contraband", 48000, 65000, true),
                ("Medsticks", "Bio / Contraband", 8500, 12500, false),
                ("Neon", "Bio / Contraband", 9500, 11500, false),
                // Agricultural
                ("Agricium", "Agricultural", 2200, 3100, false),
                ("Golden Medmon", "Agricultural", 15500, 19500, false),
                // Gas / Fuel
                ("Hydrogen", "Gas / Fuel", 350, 480, false),
                ("Helium", "Gas / Fuel", 4200, 5800, false),
                ("Methane", "Gas / Fuel", 850, 1100, false),
                // Salvage / Industrial
                ("Waste", "Salvage / Industrial", 150, 220, false),
                ("Scrap", "Salvage / Industrial", 450, 650, false),
                ("Ship Parts", "Salvage / Industrial", 5500, 7800, false),
            };

            int commodityIndex = 0;
            foreach (var (name, category, buyPrice, sellPrice, isIllegal) in commodityData)
            {
                var commodity = new ScipDashboard.Core.Models.Commodity
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    DisplayName = name,
                    Category = category,
                    CurrentBuyPrice = buyPrice,
                    CurrentSellPrice = sellPrice,
                    MarginPercent = Math.Round(((sellPrice - buyPrice) / buyPrice) * 100m, 2),
                    IsIllegal = isIllegal,
                    SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample,
                };
                _commodities.Add(commodity);

                // Create price observations for each station
                foreach (var station in stations)
                {
                    var buyVariation = buyPrice * (0.9m + (decimal)commodityIndex * 0.01m);
                    var sellVariation = sellPrice * (0.95m + (decimal)commodityIndex * 0.01m);

                    _observations.Add(new ScipDashboard.Core.Models.PriceObservation
                    {
                        Id = Guid.NewGuid().ToString(),
                        CommodityId = commodity.Id,
                        CommodityName = name,
                        LocationId = $"loc-{station.System}-{station.Name}".Replace(" ", "-").ToLower(),
                        LocationName = station.Name,
                        SystemName = station.System,
                        BuyPrice = Math.Round(buyVariation, 2),
                        SellPrice = Math.Round(sellVariation, 2),
                        Volume = 1000 + commodityIndex * 100,
                        DemandVolume = 800 + commodityIndex * 80,
                        Timestamp = DateTime.UtcNow.AddMinutes(-commodityIndex * 5),
                        SourceName = "UEXCorp Sample",
                        SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample,
                    });
                }

                commodityIndex++;
            }
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
    }
}
