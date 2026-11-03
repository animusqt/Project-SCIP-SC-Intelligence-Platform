using System;
using System.Collections.Generic;
using System.Linq;

namespace ScipDashboard.Core.Models
{
    /// <summary>
    /// Market data entity containing price observations for commodities across systems.
    /// </summary>
    public class MarketData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int LastUpdated { get; set; }
        
        // Commodity observations
        public List<PriceObservation> PriceObservations { get; set; } = [];
        
        // Locations
        public List<Location> Locations { get; set; } = [];
        
        // Commodities
        public List<Commodity> Commodities { get; set; } = [];
        
        // Watchlist items
        public List<WatchlistItem> WatchlistItems { get; set; } = [];
        
        // Hauling opportunities
        public List<HaulingOpportunity> Opportunities { get; set; } = [];
        
        // Source configuration
        public DataSourceConfig DataSourceConfiguration { get; set; }
    }

    /// <summary>
    /// Price observation for a specific commodity-system pair.
    /// </summary>
    public class PriceObservation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; }
        public string SystemName { get; set; } = string.Empty;
        public string SourceName { get; set; } = string.Empty;
        public DataSourceStatus SourceStatus { get; set; } = DataSourceStatus.Sample;
        
        // Commodity information
        public string CommodityId { get; set; } = string.Empty;
        public string CommodityName { get; set; } = string.Empty;
        public string LocationId { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public int Volume { get; set; }
        public List<int> SupportedContainerSizes { get; set; } = [];
        public int? EstimatedLoadingTimeSeconds { get; set; }
        
        // Buy price and margin info
        public decimal BuyPrice { get; set; }
        public decimal MarginPercent { get; set; }
        public decimal EstimatedProfitPerScu => (BuyPrice * MarginPercent / 100m);
        
        // Sell price and demand info
        public decimal SellPrice { get; set; }
        public decimal MarginPercentSell { get; set; }
        public int DemandVolume { get; set; }
        
        // Display text combining buy/sell info
        public string DisplayText => 
            $"{CommodityName}: Buy @{BuyPrice:N0}, Sell @{SellPrice:N0} (+{MarginPercent:0.0}%)";
    }

    /// <summary>
    /// Location in a trading system with coordinates and infrastructure details.
    /// </summary>
    public class Location
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string SystemName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Spaceport, station, etc.
        
        public double DistanceFromStartNode { get; set; }
        
        public bool IsActive { get; set; } = true;
        public bool HasCommoditiesMarket { get; set; } = false;
    }

    /// <summary>
    /// Route connecting two locations with commodities available for transport.
    /// </summary>
    public class Route
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<RouteStop> Stops { get; set; } = [];
        public decimal EstimatedNetProfit { get; set; }
        public decimal EstimatedGrossMargin { get; set; }
        public decimal EstimatedFees { get; set; }
        public RouteStatus Status { get; set; } = RouteStatus.Draft;
        public bool IsLoop { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CargoUsed { get; set; }
        public DataSourceStatus DataConfidence { get; set; }
        public string Tags { get; set; } = string.Empty;
        
        // Legacy properties for multi-route planning
        public decimal Quantity { get; set; }
        public decimal Profit { get; set; }
        public int Distance { get; set; }
    }

    /// <summary>
    /// Commodity available for buying and selling in the market.
    /// </summary>
    public class Commodity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        
        public decimal PricePerScu { get; set; }
        public decimal ReferenceMarginPerScu { get; set; }
        public decimal ReferenceBuyPrice { get; set; }
        public decimal ReferenceSellPrice { get; set; }
        public int BaseDemandVolume { get; set; }
        public decimal UexAverageBuyPrice { get; set; }
        public decimal UexAverageSellPrice { get; set; }
        public decimal UexAverageSupplyScu { get; set; }
        public decimal UexAverageDemandScu { get; set; }
        public decimal UexAverageIndex => UexAverageSellPrice - UexAverageBuyPrice;
        public DateTime? UexUpdatedAt { get; set; }
        
        public bool IsIllegal { get; set; }
        public string SystemName { get; set; } = string.Empty;
        
        // Buy price data
        public decimal CurrentBuyPrice { get; set; }
        public decimal MarginPercent { get; set; }
        public decimal CurrentPricePerScu => CurrentBuyPrice;
        public decimal EstimatedProfitPerScu => (CurrentBuyPrice * MarginPercent / 100m);
        
        // Sell price data  
        public decimal CurrentSellPrice { get; set; }
        public int DemandVolume { get; set; }
        public DataSourceStatus SourceStatus { get; set; } = DataSourceStatus.Sample;
    }

    public sealed class ManualCommodityEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
        public string BuyLocation { get; set; } = string.Empty;
        public string SellLocation { get; set; } = string.Empty;
        public int BuyStockScu { get; set; }
        public int SellDemandScu { get; set; }
    }

    /// <summary>
    /// Fleet of ships available for hauling operations.
    /// </summary>
    public class Ship
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public decimal TotalProfit { get; set; }
        
        // Fleet info
        public int TotalCapacitySCU { get; set; }
        public int CurrentLoadSCU { get; set; }
        public decimal AverageSpeed { get; set; }
        public decimal FuelEfficiency => TotalCapacitySCU * 0.8m; // 20% overhead
        
        // Status
        public bool IsAvailable { get; set; }
        public string CurrentLocationSystem { get; set; } = string.Empty;
    }

    /// <summary>
    /// User's fleet with multiple ships for operations.
    /// </summary>
    public class UserFleetShip
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public Ship Ship { get; set; } = new();
        
        // Personal stats
        public int CompletedRuns { get; set; }
        public decimal TotalProfit { get; set; }
        public string LastLocation { get; set; } = string.Empty;
    }

    /// <summary>
    /// Trading system faction with member ships and activities.
    /// </summary>
    public class Faction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        
        // Fleet info
        public int ShipCount { get; set; }
        public decimal TotalCapacitySCU { get; set; }
        public ReputationTier ReputationLevel { get; set; }
        
        // Trade volume tracking
        public decimal TradedVolumeSCU { get; set; }
        public DateTime LastActivityDate { get; set; }
    }

    /// <summary>
    /// Ship's current reputation tier affecting trade privileges.
    /// </summary>
    public enum ReputationTier
    {
        None = 0,
        Bronze = 1,
        Silver = 2,
        Gold = 3,
        Diamond = 4
    }

    /// <summary>
    /// Quest/mission record tracking player activities.
    /// </summary>
    public class QuestRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        // Status tracking
        public bool IsCompleted { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime CompletionDate { get; set; }
        
        // Rewards earned
        public int ShipsAcquired { get; set; }
        public decimal ProfitReceived { get; set; }
    }

    /// <summary>
    /// Player progress tracking achievements and milestones.
    /// </summary>
    public class PlayerProgress
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int TotalRunsCompleted { get; set; }
        public decimal TotalProfitEarned { get; set; }
        public ReputationTier CurrentTier { get; set; }
        
        // Achievement tracking
        public List<QuestRecord> CompletedQuests { get; set; } = [];
        
        // Best records
        public int MostRunsInSingleSystem => 0;
    }

    /// <summary>
    /// Data source configuration for fetching market information.
    /// </summary>
    public class DataSourceConfig
    {
        public string Name { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public int AutoRefreshIntervalMinutes { get; set; }
        public decimal BuyPriceThreshold { get; set; } // For filtering low-price items
        public decimal SellPriceThreshold { get; set; } // For filtering high-price opportunities
    }

    /// <summary>
    /// DataSourceStatus enumeration for data source reporting.
    /// </summary>
    public enum DataSourceStatus
    {
        Default = 0,
        Sample = 1,
        UEXCorp = 2,
        Live = 3,
        Cached = 4,
        Unavailable = 5,
        AllSources = 6
    }

    /// <summary>
    /// Multi-route plan with multiple legs for complex trades.
    /// </summary>
    public class MultiRoutePlan
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public int TotalLegs { get; set; }
        public List<Route> RouteLegs { get; set; } = [];
        
        // Aggregated stats
        public decimal TotalCapacitySCU => RouteLegs.Sum(l => l.Quantity);
        public int TotalDistance => RouteLegs.Sum(l => l.Distance);
        public decimal TotalProfit => RouteLegs.Sum(l => l.Profit);
    }

    /// <summary>
    /// Single run state tracking current operation details.
    /// </summary>
    public class SingleRunState
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        // Current leg info
        public Route ActiveLeg { get; set; } = new();
        public int CargoLoadedSCU { get; set; }
        
        // Progress tracking
        public DateTime RunStartTime { get; set; }
        public bool IsComplete { get; set; }
        public decimal RunProfit => 0m;
    }

    /// <summary>
    /// Watchlist item for commodities or routes to track.
    /// </summary>
    public class WatchlistItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string CommodityId { get; set; } = string.Empty;
        public decimal TargetBuyPrice { get; set; }
        public decimal TargetSellPrice { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool NotifyOnPriceChange { get; set; } = true;
        
        // Type classification
        public enum ItemType { Commodity, Route, System }
        public ItemType Type { get; set; }
        
        // Additional data based on type
        public decimal LastPrice => 0m;
        public int AdditionsNeeded => 0;
    }

    /// <summary>
    /// Hauling opportunity combining buy/sell points for profit.
    /// </summary>
    public class HaulingOpportunity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        // Route connection
        public Route Route { get; set; } = new();
        
        // Buy side
        public Commodity CommodityToBuy { get; set; } = new();
        public decimal CurrentPricePerScu => 0m;
        public int AvailableStockSCU => 0;
        
        // Sell side  
        public Location DestinationSystem { get; set; } = new();
        public decimal SellingPricePerScu => 0m;
        public int DemandAvailableSCU => 0;
        
        // Profit calculation
        public decimal TotalCost => CommodityToBuy.CurrentPricePerScu * AvailableStockSCU;
        public decimal TotalRevenue => SellingPricePerScu * DemandAvailableSCU;
        public decimal NetProfit => TotalRevenue - TotalCost;
        
        // Display text showing opportunity summary
        public string DisplayText => 
            $"Op: Buy @{CommodityToBuy.CurrentPricePerScu:N0}, " +
            $"Sell @{SellingPricePerScu:N0} (+{NetProfit:N0} aUEC)";
    }

    /// <summary>
    /// App-level user settings and configuration.
    /// </summary>
    public class UserSettings
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        // Theme preference
        public string PreferredTheme { get; set; } = "Light";
        
        // Data preferences
        public bool EnableAutoRefresh { get; set; }
        public int RefreshIntervalMinutes { get; set; }
        public decimal MinProfitMarginPercent { get; set; }
        
        // Notification settings
        public bool EnableEmailNotifications { get; set; }
        public bool EnableDesktopNotifications { get; set; }
    }

    /// <summary>
    /// Application data wrapper containing all game state.
    /// </summary>
    public class AppData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "SC Intelligence Dashboard";
        
        // Market data
        public MarketData CurrentMarketData { get; set; }
        
        // User progress
        public PlayerProgress UserProgress { get; set; }
        
        // Settings
        public UserSettings AppSettings { get; set; }
    }

    /// <summary>
    /// All data sources available for market information.
    /// </summary>
    public class DataSourceOption
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Route preference option for data prioritization.
    /// </summary>
    public class RoutePreferenceOption
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Fleet catalog with all available ships for selection.
    /// </summary>
    public class FleetCatalog
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public int TotalShips { get; set; }
        public decimal TotalCapacitySCU { get; set; }
        public List<Ship> AvailableShips { get; set; } = [];
    }

    /// <summary>
    /// Complete fleet information with ship details.
    /// </summary>
    public class SingleRunStateComplete
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        // Fleet composition
        public List<UserFleetShip> FleetComposition { get; set; } = [];
        
        // Current status
        public bool IsActive { get; set; }
        public string LastCompletedLocation { get; set; } = string.Empty;
    }

    /// <summary>
    /// Route status enumeration.
    /// </summary>
    public enum RouteStatus
    {
        Draft = 0,
        Active = 1,
        Completed = 2,
        Cancelled = 3
    }

    /// <summary>
    /// Route stop representing a single buy/sell stop in a route.
    /// </summary>
    public class RouteStop
    {
        public string Id { get; set; } = string.Empty;
        public string RouteId { get; set; } = string.Empty;
        public string LocationId { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string CommodityId { get; set; } = string.Empty;
        public string CommodityName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
        public bool IsBuy { get; set; }
        public int OrderIndex { get; set; }
    }
}
