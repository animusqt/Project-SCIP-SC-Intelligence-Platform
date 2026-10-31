using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ScipDashboard.Core.Models;

namespace ScipDashboard.Core.Data
{
    /// <summary>
    /// Interface for Supabase data operations - user settings, watchlist, routes, fleet, market snapshots.
    /// </summary>
    public interface ISupabaseDataService
    {
        // User Settings
        Task<SupabaseUserSettings?> GetUserSettingsAsync(string userId);
        Task SaveUserSettingsAsync(SupabaseUserSettings settings);

        // Watchlist
        Task<List<SupabaseWatchlistItem>> GetWatchlistAsync(string userId);
        Task AddToWatchlistAsync(SupabaseWatchlistItem item);
        Task RemoveFromWatchlistAsync(string itemId);
        Task UpdateWatchlistItemAsync(SupabaseWatchlistItem item);

        // Routes
        Task<List<SupabaseUserRoute>> GetUserRoutesAsync(string userId);
        Task SaveUserRouteAsync(SupabaseUserRoute route);
        Task DeleteUserRouteAsync(string routeId);

        // Fleet
        Task<List<SupabaseUserFleetShip>> GetUserFleetAsync(string userId);
        Task SaveUserFleetShipAsync(SupabaseUserFleetShip ship);
        Task DeleteUserFleetShipAsync(string shipId);

        // Market Snapshots (shared/cached data)
        Task<List<SupabaseMarketSnapshot>> GetMarketSnapshotsAsync(string? commodityId = null, string? locationId = null);
        Task SaveMarketSnapshotAsync(SupabaseMarketSnapshot snapshot);
        Task<bool> IsMarketSnapshotFreshAsync(string commodityId, string locationId, int maxAgeMinutes = 30);
    }
}
