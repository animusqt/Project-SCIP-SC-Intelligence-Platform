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
            return _marketData.Count > 0 ? _marketData[0] : new ScipDashboard.Core.Models.MarketData();
        }
        
        public async Task<List<ScipDashboard.Core.Models.Commodity>> GetCommoditiesAsync()
        {
            if (_commodities.Count == 0)
            {
                // Populate with sample data
                for (int i = 0; i < 10; i++)
                {
                    _commodities.Add(new ScipDashboard.Core.Models.Commodity
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = $"Commodity-{i+1}",
                        CurrentBuyPrice = 10 + (i * 2),
                        MarginPercent = 5,
                        CurrentSellPrice = 15 + (i * 2)
                    });
                }
            }
            return _commodities;
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
