using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using ScipDashboard.Core.Models;

namespace ScipDashboard.Core.Data
{
    /// <summary>
    /// Supabase data service implementation using SupabaseClient for REST API calls.
    /// Handles user settings, watchlist, routes, fleet, and market snapshots.
    /// </summary>
    public class SupabaseDataService : ISupabaseDataService
    {
        private readonly SupabaseClient _client;
        private readonly string _userId;

        public SupabaseDataService(SupabaseClient client, string userId)
        {
            _client = client;
            _userId = userId;
        }

        #region User Settings

        public async Task<SupabaseUserSettings?> GetUserSettingsAsync(string userId)
        {
            var json = await _client.GetAsync("user_settings", $"user_id=eq.{userId}", "updated_at.desc", 1);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
            {
                return JsonSerializer.Deserialize<SupabaseUserSettings>(doc.RootElement[0].GetRawText());
            }
            return null;
        }

        public async Task SaveUserSettingsAsync(SupabaseUserSettings settings)
        {
            settings.UserId = _userId;
            settings.UpdatedAt = DateTime.UtcNow;

            // Try update first, then insert if no rows affected
            var existing = await GetUserSettingsAsync(_userId);
            if (existing != null)
            {
                await _client.PatchAsync("user_settings", $"id=eq.{existing.Id}", settings);
            }
            else
            {
                await _client.PostAsync("user_settings", settings);
            }
        }

        #endregion

        #region Watchlist

        public async Task<List<SupabaseWatchlistItem>> GetWatchlistAsync(string userId)
        {
            var json = await _client.GetAsync("user_watchlist", $"user_id=eq.{userId}", "created_at.desc");
            using var doc = JsonDocument.Parse(json);
            var items = new List<SupabaseWatchlistItem>();
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    items.Add(JsonSerializer.Deserialize<SupabaseWatchlistItem>(el.GetRawText())!);
                }
            }
            return items;
        }

        public async Task AddToWatchlistAsync(SupabaseWatchlistItem item)
        {
            item.UserId = _userId;
            await _client.PostAsync("user_watchlist", item);
        }

        public async Task RemoveFromWatchlistAsync(string itemId)
        {
            await _client.DeleteAsync("user_watchlist", $"id=eq.{itemId}");
        }

        public async Task UpdateWatchlistItemAsync(SupabaseWatchlistItem item)
        {
            await _client.PatchAsync("user_watchlist", $"id=eq.{item.Id}", item);
        }

        #endregion

        #region Routes

        public async Task<List<SupabaseUserRoute>> GetUserRoutesAsync(string userId)
        {
            var json = await _client.GetAsync("user_routes", $"user_id=eq.{userId}", "updated_at.desc");
            using var doc = JsonDocument.Parse(json);
            var routes = new List<SupabaseUserRoute>();
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    routes.Add(JsonSerializer.Deserialize<SupabaseUserRoute>(el.GetRawText())!);
                }
            }
            return routes;
        }

        public async Task SaveUserRouteAsync(SupabaseUserRoute route)
        {
            route.UserId = _userId;
            route.UpdatedAt = DateTime.UtcNow;

            var existing = await GetUserRoutesAsync(_userId);
            var existingRoute = existing.Find(r => r.Id == route.Id);
            if (existingRoute != null)
            {
                await _client.PatchAsync("user_routes", $"id=eq.{route.Id}", route);
            }
            else
            {
                await _client.PostAsync("user_routes", route);
            }
        }

        public async Task DeleteUserRouteAsync(string routeId)
        {
            await _client.DeleteAsync("user_routes", $"id=eq.{routeId}");
        }

        #endregion

        #region Fleet

        public async Task<List<SupabaseUserFleetShip>> GetUserFleetAsync(string userId)
        {
            var json = await _client.GetAsync("user_fleet", $"user_id=eq.{userId}", "created_at.desc");
            using var doc = JsonDocument.Parse(json);
            var fleet = new List<SupabaseUserFleetShip>();
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    fleet.Add(JsonSerializer.Deserialize<SupabaseUserFleetShip>(el.GetRawText())!);
                }
            }
            return fleet;
        }

        public async Task SaveUserFleetShipAsync(SupabaseUserFleetShip ship)
        {
            ship.UserId = _userId;

            var existing = await GetUserFleetAsync(_userId);
            var existingShip = existing.Find(s => s.Id == ship.Id);
            if (existingShip != null)
            {
                await _client.PatchAsync("user_fleet", $"id=eq.{ship.Id}", ship);
            }
            else
            {
                await _client.PostAsync("user_fleet", ship);
            }
        }

        public async Task DeleteUserFleetShipAsync(string shipId)
        {
            await _client.DeleteAsync("user_fleet", $"id=eq.{shipId}");
        }

        #endregion

        #region Market Snapshots

        public async Task<List<SupabaseMarketSnapshot>> GetMarketSnapshotsAsync(string? commodityId = null, string? locationId = null)
        {
            var filters = new List<string>();
            if (!string.IsNullOrEmpty(commodityId)) filters.Add($"commodity_id=eq.{commodityId}");
            if (!string.IsNullOrEmpty(locationId)) filters.Add($"location_id=eq.{locationId}");
            var filter = filters.Count > 0 ? string.Join("&", filters) : null;

            var json = await _client.GetAsync("market_snapshots", filter, "timestamp.desc", 100);
            using var doc = JsonDocument.Parse(json);
            var snapshots = new List<SupabaseMarketSnapshot>();
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    snapshots.Add(JsonSerializer.Deserialize<SupabaseMarketSnapshot>(el.GetRawText())!);
                }
            }
            return snapshots;
        }

        public async Task SaveMarketSnapshotAsync(SupabaseMarketSnapshot snapshot)
        {
            await _client.PostAsync("market_snapshots", snapshot);
        }

        public async Task<bool> IsMarketSnapshotFreshAsync(string commodityId, string locationId, int maxAgeMinutes = 30)
        {
            var snapshots = await GetMarketSnapshotsAsync(commodityId, locationId);
            if (snapshots.Count == 0) return false;

            var latest = snapshots[0].Timestamp;
            var age = DateTime.UtcNow - latest;
            return age.TotalMinutes <= maxAgeMinutes;
        }

        #endregion
    }
}
