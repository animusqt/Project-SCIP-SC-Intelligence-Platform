using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ScipDashboard.Core.Data;
using ScipDashboard.Core.Models;
using ScipDashboard.Core.Services;
using ScipDashboard;
using ScipDashboard.WPF;
using ScipDashboard.WPF.Services;

namespace ScipDashboard.WPF.ViewModels
{
    /// <summary>
    /// Settings view model for managing application configuration and preferences.
    /// Handles theme selection, data preferences, notification settings, login/session, etc.
    /// </summary>
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private string _theme = "Light";
        private bool _isDarkModeSelected;
        private bool _isLightModeSelected;

        // Login / Session Properties
        private string _loginUsername = string.Empty;
        private string _loginPassword = string.Empty;
        private string _sessionToken = string.Empty;
        private bool _isLoggedIn;
        private string _loginStatusMessage = string.Empty;
        private string _supabaseProjectUrl = string.Empty;
        private string _supabasePublishableKey = string.Empty;
        private string _paypalDonationLink = string.Empty;
        private string _supportStatusMessage = "Set a PayPal link to enable the donation button.";
        private string _newCommodityName = string.Empty;
        private string _commodityCatalogStatus = "Add exact UEX commodity names to include hidden entries in market refreshes.";
        private string _manualCommodityName = string.Empty;
        private string _manualBuyPrice = string.Empty;
        private string _manualSellPrice = string.Empty;
        private string _manualBuyLocation = string.Empty;
        private string _manualSellLocation = string.Empty;
        private string _manualBuyStockScu = "0";
        private string _manualSellDemandScu = "0";
        private string _routeFilterStatus = "Route filters apply to hauling options, not the full commodity catalog.";

        // Theme Properties
        public string Theme
        {
            get => _theme;
            set 
            { 
                var normalized = string.Equals(value, "Dark", StringComparison.OrdinalIgnoreCase) ? "Dark" : "Light";
                App.SetTheme(normalized);
                if (_theme != normalized)
                {
                    _theme = normalized;
                    OnPropertyChanged();
                }

                var darkSelected = normalized == "Dark";
                if (_isDarkModeSelected != darkSelected)
                {
                    _isDarkModeSelected = darkSelected;
                    OnPropertyChanged(nameof(IsDarkModeSelected));
                }

                if (_isLightModeSelected == darkSelected)
                {
                    _isLightModeSelected = !darkSelected;
                    OnPropertyChanged(nameof(IsLightModeSelected));
                }

            }
        }

        public bool IsDarkModeSelected
        {
            get => _isDarkModeSelected;
            set
            {
                if (_isDarkModeSelected != value)
                {
                    _isDarkModeSelected = value;
                    Theme = value ? "Dark" : "Light";
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLightModeSelected
        {
            get => _isLightModeSelected;
            set
            {
                if (_isLightModeSelected != value)
                {
                    _isLightModeSelected = value;
                    Theme = value ? "Light" : "Dark";
                    OnPropertyChanged();
                }
            }
        }

        // Login Properties
        public string LoginUsername
        {
            get => _loginUsername;
            set { if (_loginUsername != value) { _loginUsername = value; OnPropertyChanged(); } }
        }

        public string LoginPassword
        {
            get => _loginPassword;
            set { if (_loginPassword != value) { _loginPassword = value; OnPropertyChanged(); } }
        }

        public string SessionToken
        {
            get => _sessionToken;
            set { if (_sessionToken != value) { _sessionToken = value; OnPropertyChanged(); } }
        }

        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set
            {
                if (_isLoggedIn == value) return;
                _isLoggedIn = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LoginDisplayText));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string LoginDisplayText => IsLoggedIn ? $"Logged in as {LoginUsername}" : "Not logged in";
        public bool IsDiscordAuthAvailable => App.AuthService != null;
        public bool IsGoogleAuthAvailable => App.AuthService != null;

        public string SupabaseProjectUrl
        {
            get => _supabaseProjectUrl;
            set { if (_supabaseProjectUrl != value) { _supabaseProjectUrl = value; OnPropertyChanged(); } }
        }

        public string SupabasePublishableKey
        {
            get => _supabasePublishableKey;
            set { if (_supabasePublishableKey != value) { _supabasePublishableKey = value; OnPropertyChanged(); } }
        }

        public string LoginStatusMessage
        {
            get => _loginStatusMessage;
            set { if (_loginStatusMessage != value) { _loginStatusMessage = value; OnPropertyChanged(); } }
        }

        public string PayPalDonationLink
        {
            get => _paypalDonationLink;
            set
            {
                if (_paypalDonationLink == value) return;
                _paypalDonationLink = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanOpenDonationLink));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool CanOpenDonationLink =>
            Uri.TryCreate(PayPalDonationLink, UriKind.Absolute, out var uri) &&
            uri.Scheme == Uri.UriSchemeHttps &&
            (uri.Host.Equals("paypal.com", StringComparison.OrdinalIgnoreCase) ||
             uri.Host.EndsWith(".paypal.com", StringComparison.OrdinalIgnoreCase) ||
             uri.Host.Equals("paypal.me", StringComparison.OrdinalIgnoreCase) ||
             uri.Host.EndsWith(".paypal.me", StringComparison.OrdinalIgnoreCase));

        public string SupportStatusMessage
        {
            get => _supportStatusMessage;
            private set { if (_supportStatusMessage != value) { _supportStatusMessage = value; OnPropertyChanged(); } }
        }

        public ObservableCollection<string> AdditionalCommodityNames { get; } = new();
        public ObservableCollection<ManualCommodityEntry> ManualCommodityEntries { get; } = new();
        public ObservableCollection<RouteFilterOption> RouteCategories { get; } = new();
        public ObservableCollection<RouteFilterOption> RouteSystems { get; } = new();

        public string ManualCommodityName { get => _manualCommodityName; set { _manualCommodityName = value; OnPropertyChanged(); } }
        public string ManualBuyPrice { get => _manualBuyPrice; set { _manualBuyPrice = value; OnPropertyChanged(); } }
        public string ManualSellPrice { get => _manualSellPrice; set { _manualSellPrice = value; OnPropertyChanged(); } }
        public string ManualBuyLocation { get => _manualBuyLocation; set { _manualBuyLocation = value; OnPropertyChanged(); } }
        public string ManualSellLocation { get => _manualSellLocation; set { _manualSellLocation = value; OnPropertyChanged(); } }
        public string ManualBuyStockScu { get => _manualBuyStockScu; set { _manualBuyStockScu = value; OnPropertyChanged(); } }
        public string ManualSellDemandScu { get => _manualSellDemandScu; set { _manualSellDemandScu = value; OnPropertyChanged(); } }
        public string RouteFilterStatus
        {
            get => _routeFilterStatus;
            private set { if (_routeFilterStatus != value) { _routeFilterStatus = value; OnPropertyChanged(); } }
        }

        public string NewCommodityName
        {
            get => _newCommodityName;
            set
            {
                if (_newCommodityName == value) return;
                _newCommodityName = value;
                OnPropertyChanged();
                AddCommodityNameCommand?.RaiseCanExecuteChanged();
            }
        }

        public string CommodityCatalogStatus
        {
            get => _commodityCatalogStatus;
            private set { if (_commodityCatalogStatus != value) { _commodityCatalogStatus = value; OnPropertyChanged(); } }
        }

        // Data Update Settings
        private int _refreshIntervalMinutes = 5;
        public ObservableCollection<int> AvailableIntervals { get; } = new ObservableCollection<int> { 1, 5, 10, 15, 30, 60 };
        
        public int RefreshInterval
        {
            get => _refreshIntervalMinutes;
            set
            {
                if (_refreshIntervalMinutes != value)
                {
                    _refreshIntervalMinutes = Math.Max(1, Math.Min(120, value));
                    OnPropertyChanged();
                    // Save to DataService for auto-refresh
                    App.Services.Get<IDataService>()?.UpdateAutoRefreshInterval(value);
                }
            }
        }

        // Route Data Preference
        public ObservableCollection<RoutePreferenceOption> Preferences { get; } = new();
        public RoutePreferenceOption DefaultPreference { get; set; }

        // Notification Settings
        private bool _enableEmailNotifications = true;
        private bool _enableDesktopNotifications = false;
        
        public bool EnableEmailNotifications
        {
            get => _enableEmailNotifications;
            set
            {
                if (_enableEmailNotifications != value)
                {
                    _enableEmailNotifications = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool EnableDesktopNotifications
        {
            get => _enableDesktopNotifications;
            set
            {
                if (_enableDesktopNotifications != value)
                {
                    _enableDesktopNotifications = value;
                    OnPropertyChanged();
                }
            }
        }

        // Star Citizen Server Preferences
        private string _persistentUniverseServer = "NA";
        public string PersistentUniverseServer
        {
            get => _persistentUniverseServer;
            set
            {
                if (_persistentUniverseServer != value)
                {
                    _persistentUniverseServer = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _arenaCommanderServer = "NA";
        public string ArenaCommanderServer
        {
            get => _arenaCommanderServer;
            set
            {
                if (_arenaCommanderServer != value)
                {
                    _arenaCommanderServer = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> AvailableServers { get; } = new ObservableCollection<string> { "NA", "EU", "AU", "OCE" };

        // Data Source Selection
        private string _defaultDataSource;
        public ObservableCollection<DataSourceOption> DataSources { get; } = new();

        public string DefaultDataSource
        {
            get => _defaultDataSource;
            set
            {
                if (_defaultDataSource != value)
                {
                    _defaultDataSource = value;
                    OnPropertyChanged();
                    App.Services.Get<IDataService>()?.SetPreferredDataSource(value);
                }
            }
        }

        // System Information
        private DateTime _lastSettingsUpdate = DateTime.Now;
        public string LastSettingsUpdate => _lastSettingsUpdate.ToString("MMM dd, yyyy HH:mm");

        // Commands
        public ICommand SaveSettingsCommand { get; set; }
        public ICommand ResetToDefaultsCommand { get; set; }
        public RelayCommand LoginWithDiscordCommand { get; private set; } = null!;
        public RelayCommand LoginWithGoogleCommand { get; private set; } = null!;
        public RelayCommand ConfigureSupabaseCommand { get; private set; } = null!;
        public RelayCommand LogoutCommand { get; private set; } = null!;
        public RelayCommand OpenDonationLinkCommand { get; private set; } = null!;
        public RelayCommand AddCommodityNameCommand { get; private set; } = null!;
        public RelayCommand<string> RemoveCommodityNameCommand { get; private set; } = null!;
        public RelayCommand AddManualCommodityEntryCommand { get; private set; } = null!;
        public RelayCommand<string> RemoveManualCommodityEntryCommand { get; private set; } = null!;

        public SettingsViewModel()
        {
            InitializeFromSettings();
            LoadSupportSettings();
            CreateDataSources();
            CreatePreferences();
            InitializeCommands();
            LoadCommodityCatalogSettings();
            LoadRouteFilters();
        }

        private void InitializeFromSettings()
        {
            // Load theme from settings
            var savedTheme = App.AppSettings?.Get("theme") ?? App.CurrentTheme;
            if (string.IsNullOrEmpty(savedTheme))
                savedTheme = "Light";
            
            Theme = savedTheme;

            // Load refresh interval
            var savedInterval = App.AppSettings?.Get("refreshIntervalMinutes");
            if (int.TryParse(savedInterval, out var result) && result > 0)
                RefreshInterval = Math.Max(1, Math.Min(120, result));

            // Load data source preference
            var savedDataSource = App.AppSettings?.Get("dataSource");
            DefaultDataSource = savedDataSource ?? "Real-Time Market Data";

            // Load notification preferences
            EnableEmailNotifications = bool.Parse(App.AppSettings?.Get("emailNotifications")?.ToString() ?? "true");
            EnableDesktopNotifications = bool.Parse(App.AppSettings?.Get("desktopNotifications")?.ToString() ?? "false");

            // Load server preferences
            PersistentUniverseServer = App.AppSettings?.Get("persistentUniverseServer") ?? "NA";
            ArenaCommanderServer = App.AppSettings?.Get("arenaCommanderServer") ?? "NA";

            // Load login session
            _sessionToken = string.Empty;
            LoginUsername = App.AppSettings?.Get("loginUsername") ?? string.Empty;
            IsLoggedIn = App.AuthService?.IsAuthenticated == true;
            if (IsLoggedIn)
                LoginUsername = App.AuthService?.CurrentUserId ?? LoginUsername;
            else if (!IsDiscordAuthAvailable)
                LoginStatusMessage = "Enter your Supabase project URL and publishable/anon key below to enable Google or Discord sign-in.";

            _lastSettingsUpdate = DateTime.Now;
            SupabaseProjectUrl = App.SupabaseConfig?.SupabaseUrl ?? string.Empty;
            SupabasePublishableKey = App.SupabaseConfig?.ApiKey ?? string.Empty;
        }

        private void CreateDataSources()
        {
            DataSources.Clear();
            
            foreach (var ds in new[] 
            { 
                new DataSourceOption { Name = "Real-Time Market Data", Description = "Live market data from official channels" },
                new DataSourceOption { Name = "Historical Archives", Description = "Access historical trade records" },
                new DataSourceOption { Name = "Third-Party APIs", Description = "External trading platform data" } 
            })
            {
                DataSources.Add(ds);
            }

            // Add more options dynamically as needed
            var existingNames = DataSources.Where(d => d.Name != null).Select(d => d.Name).ToList();
        }

        private void CreatePreferences()
        {
            Preferences.Clear();
            
            foreach (var pref in new[] 
            { 
                new RoutePreferenceOption { Name = "High Margin", Description = "Prioritize routes with highest profit margins" },
                new RoutePreferenceOption { Name = "Fast Turnaround", Description = "Focus on shortest route distances" },
                new RoutePreferenceOption { Name = "Low Competition", Description = "Avoid crowded trading systems" } 
            })
            {
                Preferences.Add(pref);
            }
        }

        private void InitializeCommands()
        {
            // RefreshDataCommand already in HaulingViewModel
            SaveSettingsCommand = new RelayCommand(() =>
            {
                SaveAllSettings();
            });

            ResetToDefaultsCommand = new RelayCommand(() =>
            {
                ResetToDefaults();
            });

            LoginWithDiscordCommand = new RelayCommand(async () => await ExecuteOAuthLoginAsync("discord"),
                () => !IsLoggedIn && IsDiscordAuthAvailable);
            LoginWithGoogleCommand = new RelayCommand(async () => await ExecuteOAuthLoginAsync("google"),
                () => !IsLoggedIn && IsGoogleAuthAvailable);
            ConfigureSupabaseCommand = new RelayCommand(ConfigureSupabase);

            LogoutCommand = new RelayCommand(async () => await ExecuteLogoutAsync(), () => IsLoggedIn);
            OpenDonationLinkCommand = new RelayCommand(OpenDonationLink, () => CanOpenDonationLink);
            AddCommodityNameCommand = new RelayCommand(AddCommodityName,
                () => !string.IsNullOrWhiteSpace(NewCommodityName));
            RemoveCommodityNameCommand = new RelayCommand<string>(RemoveCommodityName);
            AddManualCommodityEntryCommand = new RelayCommand(AddManualCommodityEntry);
            RemoveManualCommodityEntryCommand = new RelayCommand<string>(RemoveManualCommodityEntry);
        }

        private void LoadCommodityCatalogSettings()
        {
            try
            {
                var settings = LocalFeatureStore.Read<CommodityCatalogSettings>("commodity-catalog.json");
                foreach (var name in (settings.AdditionalCommodityNames ?? new List<string>())
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    AdditionalCommodityNames.Add(name);
                }

                foreach (var entry in (settings.ManualEntries ?? new List<ManualCommodityEntry>())
                    .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.Name)))
                    ManualCommodityEntries.Add(entry);

                if (App.Services.Has<IMarketDataService>())
                {
                    App.Services.Get<IMarketDataService>().SetAdditionalCommodityNames(AdditionalCommodityNames);
                    App.Services.Get<IMarketDataService>().SetManualCommodityEntries(ManualCommodityEntries);
                }
                CommodityCatalogStatus = AdditionalCommodityNames.Count == 0
                    ? $"{ManualCommodityEntries.Count} manual quote(s) loaded."
                    : $"{AdditionalCommodityNames.Count} hidden name(s) and {ManualCommodityEntries.Count} manual quote(s) loaded.";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                CommodityCatalogStatus = $"Could not load manual commodity names: {ex.Message}";
            }
        }

        private void AddCommodityName()
        {
            var name = NewCommodityName.Trim();
            if (string.IsNullOrWhiteSpace(name)) return;
            if (AdditionalCommodityNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                CommodityCatalogStatus = $"{name} is already in your manual commodity list.";
                return;
            }

            AdditionalCommodityNames.Add(name);
            NewCommodityName = string.Empty;
            SaveCommodityCatalogSettings();
        }

        private void RemoveCommodityName(string name)
        {
            var existing = AdditionalCommodityNames.FirstOrDefault(item =>
                string.Equals(item, name, StringComparison.OrdinalIgnoreCase));
            if (existing == null) return;
            AdditionalCommodityNames.Remove(existing);
            SaveCommodityCatalogSettings();
        }

        private void SaveCommodityCatalogSettings()
        {
            var settings = new CommodityCatalogSettings
            {
                AdditionalCommodityNames = AdditionalCommodityNames.ToList(),
                ManualEntries = ManualCommodityEntries.ToList()
            };
            try
            {
                LocalFeatureStore.Write("commodity-catalog.json", settings);
                if (App.Services.Has<IMarketDataService>())
                {
                    App.Services.Get<IMarketDataService>().SetAdditionalCommodityNames(settings.AdditionalCommodityNames);
                    App.Services.Get<IMarketDataService>().SetManualCommodityEntries(settings.ManualEntries);
                }
                CommodityCatalogStatus = $"{settings.AdditionalCommodityNames.Count} hidden name(s) and {settings.ManualEntries.Count} manual quote(s) saved. Refresh market data to apply them.";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                CommodityCatalogStatus = $"Could not save manual commodity names: {ex.Message}";
            }
        }

        private void AddManualCommodityEntry()
        {
            var name = ManualCommodityName.Trim();
            var buyLocation = ManualBuyLocation.Trim();
            var sellLocation = ManualSellLocation.Trim();
            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(buyLocation) ||
                string.IsNullOrWhiteSpace(sellLocation) ||
                !TryParseNonNegativeDecimal(ManualBuyPrice, out var buyPrice) ||
                !TryParseNonNegativeDecimal(ManualSellPrice, out var sellPrice) ||
                buyPrice <= 0 ||
                sellPrice <= 0 ||
                !int.TryParse(ManualBuyStockScu, out var stock) ||
                stock < 0 ||
                !int.TryParse(ManualSellDemandScu, out var demand) ||
                demand < 0)
            {
                CommodityCatalogStatus = "Enter a commodity, positive buy/sell prices, both locations, and non-negative stock/demand SCU.";
                return;
            }

            var entry = new ManualCommodityEntry
            {
                Name = name,
                BuyPrice = buyPrice,
                SellPrice = sellPrice,
                BuyLocation = buyLocation,
                SellLocation = sellLocation,
                BuyStockScu = stock,
                SellDemandScu = demand
            };
            ManualCommodityEntries.Add(entry);
            if (!AdditionalCommodityNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                AdditionalCommodityNames.Add(name);
            ManualCommodityName = string.Empty;
            ManualBuyPrice = string.Empty;
            ManualSellPrice = string.Empty;
            ManualBuyLocation = string.Empty;
            ManualSellLocation = string.Empty;
            ManualBuyStockScu = "0";
            ManualSellDemandScu = "0";
            SaveCommodityCatalogSettings();
        }

        private void RemoveManualCommodityEntry(string id)
        {
            var entry = ManualCommodityEntries.FirstOrDefault(candidate => candidate.Id == id);
            if (entry == null) return;
            ManualCommodityEntries.Remove(entry);
            SaveCommodityCatalogSettings();
        }

        private static bool TryParseNonNegativeDecimal(string value, out decimal result) =>
            decimal.TryParse(value, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.CurrentCulture, out result) && result >= 0;

        private void LoadRouteFilters()
        {
            var saved = new RoutingFilterSettings();
            try
            {
                saved = LocalFeatureStore.Read<RoutingFilterSettings>("routing-filters.json");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                RouteFilterStatus = $"Saved route filters could not be loaded: {ex.Message}";
            }

            foreach (var category in new[]
            {
                (Value: "Asteroid clusters", Name: "Asteroid clusters (e.g., Humboldt Mines)"),
                (Value: "Naval bases", Name: "Naval bases (e.g., HDMS ships)"),
                (Value: "Outposts", Name: "Outposts (e.g., research outposts)"),
                (Value: "Settlements", Name: "Settlements (e.g., Everus Harbor)"),
                (Value: "Space stations", Name: "Space stations (e.g., ARC-L1 Wide Forest Station)")
            })
            {
                var enabled = !saved.IsConfigured ||
                    (saved.EnabledCategories ?? new List<string>())
                        .Contains(category.Value, StringComparer.OrdinalIgnoreCase);
                RouteCategories.Add(new RouteFilterOption(category.Name, category.Value, enabled, SaveRouteFilters));
            }

            var systems = new List<string>();
            try
            {
                var locationPath = Path.Combine(AppContext.BaseDirectory, "Data", "location-catalog.json");
                if (File.Exists(locationPath))
                {
                    var entries = JsonSerializer.Deserialize<List<LocationCatalogEntry>>(
                        File.ReadAllText(locationPath),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    systems = entries?.Select(entry => entry.SystemName)
                        .Where(system => !string.IsNullOrWhiteSpace(system))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(system => system, StringComparer.OrdinalIgnoreCase)
                        .ToList() ?? new List<string>();
                }

                foreach (var system in systems)
                {
                    var enabled = !saved.IsConfigured ||
                        (saved.EnabledSystems ?? new List<string>())
                            .Contains(system, StringComparer.OrdinalIgnoreCase);
                    RouteSystems.Add(new RouteFilterOption(system, system, enabled, SaveRouteFilters));
                }

                RouteFilterStatus = "These filters constrain route locations and systems only; commodity names remain available.";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                RouteFilterStatus = $"Route filters could not be loaded: {ex.Message}";
            }
        }

        private void SaveRouteFilters()
        {
            try
            {
                var settings = new RoutingFilterSettings
                {
                    IsConfigured = true,
                    EnabledCategories = RouteCategories.Where(option => option.IsEnabled).Select(option => option.Value).ToList(),
                    EnabledSystems = RouteSystems.Where(option => option.IsEnabled).Select(option => option.Value).ToList()
                };
                LocalFeatureStore.Write("routing-filters.json", settings);
                RouteFilterStatus = "Route filters saved. Commodity catalog is not filtered.";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                RouteFilterStatus = $"Could not save route filters: {ex.Message}";
            }
        }

        private async Task ExecuteOAuthLoginAsync(string provider)
        {
            var authService = App.AuthService;
            if (authService == null)
            {
                LoginStatusMessage = "Account connection requires Supabase URL/API key and the provider to be configured.";
                return;
            }

            var providerName = provider == "google" ? "Google" : "Discord";
            LoginStatusMessage = $"Connecting your dashboard account with {providerName}...";
            try
            {
                var result = await authService.SignInWithOAuthAsync(provider);
                if (!result.Success || string.IsNullOrWhiteSpace(result.UserId))
                {
                    LoginStatusMessage = $"{providerName} connection failed: {result.ErrorMessage ?? "No user session was returned."}";
                    return;
                }

                LoginUsername = result.UserId;
                IsLoggedIn = true;
                LoginStatusMessage = "Dashboard account connected. SC-Truckers data sync requires a shared account integration from that service.";
            }
            catch (Exception ex)
            {
                LoginStatusMessage = $"{providerName} connection failed: {ex.Message}";
            }
        }

        private void ConfigureSupabase()
        {
            if (!App.TryConfigureSupabase(SupabaseProjectUrl, SupabasePublishableKey, out var error))
            {
                LoginStatusMessage = error;
                return;
            }

            try
            {
                LocalFeatureStore.Write("supabase-settings.json", new LocalSupabaseSettings
                {
                    ProjectUrl = App.SupabaseConfig!.SupabaseUrl,
                    PublishableKey = App.SupabaseConfig.ApiKey
                });
                OnPropertyChanged(nameof(IsDiscordAuthAvailable));
                OnPropertyChanged(nameof(IsGoogleAuthAvailable));
                CommandManager.InvalidateRequerySuggested();
                LoginStatusMessage = "Supabase configured on this PC. Enable Google under Supabase Authentication → Providers, then connect with Google.";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                LoginStatusMessage = $"Supabase is configured for this session, but settings could not be saved: {ex.Message}";
            }
        }

        private async Task ExecuteLogoutAsync()
        {
            try
            {
                if (App.AuthService != null)
                    await App.AuthService.SignOutAsync();
                _sessionToken = string.Empty;
                LoginUsername = string.Empty;
                LoginPassword = string.Empty;
                IsLoggedIn = false;
                LoginStatusMessage = "Logged out.";
                SaveAllSettings();
            }
            catch (Exception ex)
            {
                LoginStatusMessage = $"Sign out failed: {ex.Message}";
            }
        }

        private void LoadSupportSettings()
        {
            try
            {
                var configuredLink = Environment.GetEnvironmentVariable("SCIP_PAYPAL_DONATION_URL");
                PayPalDonationLink = string.IsNullOrWhiteSpace(configuredLink)
                    ? LocalFeatureStore.Read<SupportSettings>("support.json").PayPalDonationLink
                    : configuredLink.Trim();
                SupportStatusMessage = CanOpenDonationLink
                    ? "Donation link is configured. Save changes to keep it on this PC."
                    : "Set a PayPal link to enable the donation button.";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                SupportStatusMessage = $"Could not load the saved donation link: {ex.Message}";
            }
        }

        private void OpenDonationLink()
        {
            if (!CanOpenDonationLink ||
                !Uri.TryCreate(PayPalDonationLink, UriKind.Absolute, out var uri))
            {
                SupportStatusMessage = "Enter a valid HTTPS PayPal or PayPal.Me link first.";
                return;
            }

            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }

        public void SaveAllSettings()
        {
            var settings = new Dictionary<string, string>
            {
                {"theme", Theme},
                {"refreshIntervalMinutes", RefreshInterval.ToString()},
                {"dataSource", DefaultDataSource ?? "Real-Time Market Data"},
                {"emailNotifications", EnableEmailNotifications.ToString().ToLower()},
                {"desktopNotifications", EnableDesktopNotifications.ToString().ToLower()},
                {"persistentUniverseServer", PersistentUniverseServer ?? "NA"},
                {"arenaCommanderServer", ArenaCommanderServer ?? "NA"},
                {"sessionToken", _sessionToken},
                {"loginUsername", LoginUsername}
            };

            // Save to AppSettings if available, otherwise use LocalApplicationData
            try
            {
                var settingsManager = App.Services.Get<IAppSettings>();
                if (settingsManager != null)
                {
                    foreach (var kvp in settings)
                        settingsManager.Set(kvp.Key, kvp.Value);
                    
                    // Update last modified timestamp
                    _lastSettingsUpdate = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save settings: {ex.Message}", 
                    "SC Intelligence", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            OnPropertyChanged(nameof(LastSettingsUpdate));

            try
            {
                LocalFeatureStore.Write("support.json", new SupportSettings { PayPalDonationLink = PayPalDonationLink.Trim() });
                SupportStatusMessage = CanOpenDonationLink
                    ? "PayPal donation link saved on this PC."
                    : "No active donation link. Add your PayPal URL when it is ready.";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                SupportStatusMessage = $"Could not save the PayPal link: {ex.Message}";
            }
        }

        public void ResetToDefaults()
        {
            // Restore all values to factory defaults
            _isDarkModeSelected = false;
            Theme = "Light";
            
            RefreshInterval = 5;
            
            DefaultDataSource = "Real-Time Market Data";
            
            EnableEmailNotifications = true;
            EnableDesktopNotifications = false;

            PersistentUniverseServer = "NA";
            ArenaCommanderServer = "NA";

            IsLoggedIn = App.AuthService?.IsAuthenticated == true;
            LoginUsername = IsLoggedIn ? App.AuthService?.CurrentUserId ?? string.Empty : string.Empty;
            LoginStatusMessage = IsLoggedIn ? "Account session unchanged." : string.Empty;

            _lastSettingsUpdate = DateTime.Now;

            OnPropertyChanged(nameof(Theme));
            OnPropertyChanged(nameof(RefreshInterval));
            OnPropertyChanged(nameof(DefaultDataSource));
            OnPropertyChanged(nameof(PersistentUniverseServer));
            OnPropertyChanged(nameof(ArenaCommanderServer));
            OnPropertyChanged(nameof(LoginUsername));
            OnPropertyChanged(nameof(LoginPassword));
            OnPropertyChanged(nameof(IsLoggedIn));
            OnPropertyChanged(nameof(LoginDisplayText));
            OnPropertyChanged(nameof(LoginStatusMessage));
            OnPropertyChanged(nameof(LastSettingsUpdate));
        }

        public override string ToString()
        {
            return $"SettingsViewModel {{ Theme={Theme}, DarkMode={IsDarkModeSelected}, EmailNotifications={EnableEmailNotifications} }}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Option class for refresh interval selection.
    /// </summary>
    public class RefreshIntervalOption
    {
        public int Minutes { get; set; }
        public string Text => $"{Minutes} minutes auto-refresh";
    }

    /// <summary>
    /// Data source selection option.
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

    public sealed class SupportSettings
    {
        public string PayPalDonationLink { get; set; } = string.Empty;
    }

    public sealed class CommodityCatalogSettings
    {
        public List<string> AdditionalCommodityNames { get; set; } = new();
        public List<ManualCommodityEntry> ManualEntries { get; set; } = new();
    }

    public sealed class RoutingFilterSettings
    {
        public bool IsConfigured { get; set; }
        public List<string> EnabledCategories { get; set; } = new();
        public List<string> EnabledSystems { get; set; } = new();
    }

    public sealed class RouteFilterOption : INotifyPropertyChanged
    {
        private readonly Action _changed;
        private bool _isEnabled;

        public RouteFilterOption(string name, string value, bool isEnabled, Action changed)
        {
            Name = name;
            Value = value;
            _isEnabled = isEnabled;
            _changed = changed;
        }

        public string Name { get; }
        public string Value { get; }
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value) return;
                _isEnabled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
                _changed();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
