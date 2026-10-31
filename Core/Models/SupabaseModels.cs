using System;
using System.Collections.Generic;

namespace ScipDashboard.Core.Models
{
    /// <summary>
    /// Supabase database table: user_settings
    /// Stores per-user theme, data source preferences, and notification settings.
    /// </summary>
    public class SupabaseUserSettings
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string PreferredTheme { get; set; } = "Light";
        public bool EnableAutoRefresh { get; set; } = false;
        public int RefreshIntervalMinutes { get; set; } = 15;
        public decimal MinProfitMarginPercent { get; set; } = 0m;
        public bool EnableDesktopNotifications { get; set; } = false;
        public string DefaultDataSource { get; set; } = "UEXCorp";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Supabase database table: user_watchlist
    /// Stores per-user commodity watchlist items.
    /// </summary>
    public class SupabaseWatchlistItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string CommodityId { get; set; } = string.Empty;
        public string CommodityName { get; set; } = string.Empty;
        public decimal TargetBuyPrice { get; set; }
        public decimal TargetSellPrice { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool NotifyOnPriceChange { get; set; } = true;
    }

    /// <summary>
    /// Supabase database table: user_routes
    /// Stores per-user saved hauling routes.
    /// </summary>
    public class SupabaseUserRoute
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RouteJson { get; set; } = string.Empty; // Serialized Route object
        public decimal EstimatedNetProfit { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Supabase database table: user_fleet
    /// Stores per-user fleet ship information.
    /// </summary>
    public class SupabaseUserFleetShip
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string ShipName { get; set; } = string.Empty;
        public int TotalCapacitySCU { get; set; }
        public int CurrentLoadSCU { get; set; }
        public decimal AverageSpeed { get; set; }
        public bool IsAvailable { get; set; } = true;
        public string CurrentLocationSystem { get; set; } = string.Empty;
        public int CompletedRuns { get; set; } = 0;
        public decimal TotalProfit { get; set; } = 0m;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Supabase database table: market_snapshots
    /// Cached market data snapshots from UEXCorp API.
    /// </summary>
    public class SupabaseMarketSnapshot
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CommodityId { get; set; } = string.Empty;
        public string CommodityName { get; set; } = string.Empty;
        public string LocationId { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string SystemName { get; set; } = string.Empty;
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
        public int Volume { get; set; }
        public int DemandVolume { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string SourceName { get; set; } = string.Empty;
    }
}
