using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ScipDashboard.Core.Data;
using ScipDashboard.Core.Services;
using ScipDashboard.WPF.Services;

namespace ScipDashboard
{
    public interface IAppSettings
    {
        string? Get(string key);
        void Set(string key, string value);
    }

    public sealed class ServiceLocator
    {
        private readonly Dictionary<Type, object> _services = new();

        public void Register<T>(T service) where T : class
        {
            ArgumentNullException.ThrowIfNull(service);
            _services[typeof(T)] = service;
        }

        public T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }

            throw new InvalidOperationException(
                $"Service of type {typeof(T).Name} is not registered.");
        }

        public bool Has<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }
    }
    public partial class App : Application
    {
        public static ServiceLocator Services { get; } = new();
        public static IAppSettings? AppSettings { get; set; }
        public static SupabaseAuthService? AuthService { get; set; }
        public static SupabaseClient? SupabaseClient { get; set; }
        public static SupabaseConfig? SupabaseConfig { get; set; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Global unhandled exception handlers - prevent error popups from crashing the app
            Dispatcher.UnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;

            System.Console.WriteLine("[App] Starting application");

            // Load Supabase configuration
            SupabaseConfig = LoadSupabaseConfig();

            // Register core services
            var dataService = new DataService();
            Services.Register<IDataService>(dataService);
            Services.Register<IMarketDataService>(new MarketDataService(dataService));

            // Initialize Supabase if configured
            if (SupabaseConfig != null && SupabaseConfig.IsEnabled && SupabaseConfig.IsConfigured)
            {
                try
                {
                    var supabaseClient = new SupabaseClient(SupabaseConfig.SupabaseUrl, SupabaseConfig.ApiKey);
                    SupabaseClient = supabaseClient;
                    Services.Register<SupabaseClient>(supabaseClient);

                    var authService = new SupabaseAuthService(supabaseClient);
                    AuthService = authService;
                    Services.Register<SupabaseAuthService>(authService);

                    System.Console.WriteLine("[App] Supabase initialized successfully");
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"[App] Supabase initialization failed: {ex.Message}");
                }
            }
            else
            {
                System.Console.WriteLine("[App] Supabase not configured - running in local-only mode");
            }

            // Register settings
            Services.Register<IAppSettings>(new InMemoryAppSettings());
            AppSettings = Services.Get<IAppSettings>();

            // Seed sample market data so views have something to display
            SeedSampleDataAsync(dataService).GetAwaiter().GetResult();

            // Apply saved theme on startup
            var savedTheme = AppSettings?.Get("theme") ?? "Light";
            SetTheme(savedTheme);

            var mainWindow = new Main.MainWindow();
            MainWindow = mainWindow;

            try
            {
                mainWindow.Show();

                if (MainWindow != null)
                {
                    System.Console.WriteLine("[App] SUCCESS: MainWindow displayed successfully");
                }
                else
                {
                    System.Console.WriteLine("[ERROR] MainWindow is null!");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[ERROR] Exception: {ex.Message}");

                MessageBox.Show(
                    $"The application failed to initialize.\n\n{ex.Message}",
                    "SC Intelligence - Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Environment.Exit(1);
            }
        }

        private static SupabaseConfig LoadSupabaseConfig()
        {
            // Try to read from environment variables first
            var url = Environment.GetEnvironmentVariable("SUPABASE_URL");
            var key = Environment.GetEnvironmentVariable("SUPABASE_API_KEY");
            var secretKey = Environment.GetEnvironmentVariable("SUPABASE_SECRET_KEY");
            var jwksUrl = Environment.GetEnvironmentVariable("SUPABASE_JWKS_URL");

            if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(key))
            {
                return new SupabaseConfig
                {
                    SupabaseUrl = url,
                    ApiKey = key,
                    SecretKey = secretKey ?? string.Empty,
                    JwksUrl = jwksUrl ?? string.Empty,
                    IsEnabled = true
                };
            }

            return new SupabaseConfig
            {
                SupabaseUrl = string.Empty,
                ApiKey = string.Empty,
                SecretKey = string.Empty,
                JwksUrl = string.Empty,
                IsEnabled = false
            };
        }

        // Global exception handlers - prevent error popups from crashing the app
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            System.Console.WriteLine($"[App] Dispatcher unhandled exception: {e.Exception.Message}");
            System.Console.WriteLine($"[App] Stack trace: {e.Exception.StackTrace}");
            e.Handled = true;
        }

        private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
                System.Console.WriteLine($"[App] AppDomain unhandled exception: {ex.Message}");
                System.Console.WriteLine($"[App] Stack trace: {ex.StackTrace}");
            }
        }

        public static void SetTheme(string themeName)
        {
            try
            {
                var dict = Application.Current.Resources.MergedDictionaries;

                // Remove all theme and style dictionaries
                var toRemove = dict.Where(rd =>
                    rd.Source != null &&
                    (rd.Source.ToString().Contains("DarkTheme", StringComparison.OrdinalIgnoreCase) ||
                     rd.Source.ToString().Contains("LightTheme", StringComparison.OrdinalIgnoreCase) ||
                     rd.Source.ToString().Contains("Styles.xaml", StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                foreach (var rd in toRemove)
                    dict.Remove(rd);

                // Always add LightTheme only (Dark theme disabled)
                var themeDict = new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/ScipDashboard.WPF;component/LightTheme.xaml", UriKind.Absolute)
                };
                dict.Add(themeDict);

                var stylesDict = new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/ScipDashboard.WPF;component/Styles.xaml", UriKind.Absolute)
                };
                dict.Add(stylesDict);
            }
            catch
            {
                // Theme switching is best-effort
            }
        }

        private static async Task SeedSampleDataAsync(DataService dataService)
        {
            var locations = new List<ScipDashboard.Core.Models.Location>
            {
                // Stanton System
                new() { Id = "loc-nb", Name = "New Babbage", SystemName = "Stanton", Type = "City", IsActive = true, HasCommoditiesMarket = true },
                new() { Id = "loc-a18", Name = "Area18", SystemName = "Stanton", Type = "City", IsActive = true, HasCommoditiesMarket = true },
                new() { Id = "loc-lor", Name = "Lorville", SystemName = "Hurston", Type = "City", IsActive = true, HasCommoditiesMarket = true },
                new() { Id = "loc-olisar", Name = "Port Olisar", SystemName = "Stanton", Type = "Spaceport", IsActive = true, HasCommoditiesMarket = true },
                new() { Id = "loc-arc", Name = "ArcCorp", SystemName = "Stanton", Type = "Spaceport", IsActive = true, HasCommoditiesMarket = true },
                new() { Id = "loc-mic", Name = "MicroTech", SystemName = "Stanton", Type = "Spaceport", IsActive = true, HasCommoditiesMarket = true },
                new() { Id = "loc-everus", Name = "Everus Harbor", SystemName = "Stanton", Type = "Spaceport", IsActive = true, HasCommoditiesMarket = true },
                new() { Id = "loc-chawla", Name = "Chawla's Beach", SystemName = "Stanton", Type = "Outpost", IsActive = true, HasCommoditiesMarket = true },
                new() { Id = "loc-golden", Name = "The Golden Riviera", SystemName = "Stanton", Type = "Spaceport", IsActive = true, HasCommoditiesMarket = true },
                new() { Id = "loc-devlin", Name = "Devlin Scrap & Salvage", SystemName = "Stanton", Type = "Salvage Yard", IsActive = true, HasCommoditiesMarket = true },
                new() { Id = "loc-arcmine", Name = "ArcCorp Mining Area 061", SystemName = "Stanton", Type = "Mine", IsActive = true, HasCommoditiesMarket = true },
                new() { Id = "loc-peoples", Name = "People's Service Station Theta", SystemName = "Stanton", Type = "Station", IsActive = true, HasCommoditiesMarket = true },
                // Pyro System
                new() { Id = "loc-pyro1", Name = "Prospect", SystemName = "Pyro", Type = "Spaceport", IsActive = true, HasCommoditiesMarket = true },
                new() { Id = "loc-pyro2", Name = "Eclipse", SystemName = "Pyro", Type = "Station", IsActive = true, HasCommoditiesMarket = true },
                new() { Id = "loc-pyro3", Name = "Vigilance", SystemName = "Pyro", Type = "Station", IsActive = true, HasCommoditiesMarket = true },
                // Nyx System
                new() { Id = "loc-nyx1", Name = "Riot", SystemName = "Nyx", Type = "Spaceport", IsActive = true, HasCommoditiesMarket = true },
                new() { Id = "loc-nyx2", Name = "Orison Mining Platform", SystemName = "Nyx", Type = "Platform", IsActive = true, HasCommoditiesMarket = true },
                new() { Id = "loc-nyx3", Name = "Pyro Salvage Base", SystemName = "Nyx", Type = "Salvage Yard", IsActive = true, HasCommoditiesMarket = true },
            };

            var commodities = new List<ScipDashboard.Core.Models.Commodity>
            {
                new() { Id = "titanium", Name = "Titanium", DisplayName = "Titanium", Category = "Minerals", PricePerScu = 1200, CurrentBuyPrice = 1100, CurrentSellPrice = 1400, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "gold", Name = "Gold", DisplayName = "Gold", Category = "Minerals", PricePerScu = 2500, CurrentBuyPrice = 2300, CurrentSellPrice = 2800, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "agricium", Name = "Agricium", DisplayName = "Agricium", Category = "Agriculture", PricePerScu = 80, CurrentBuyPrice = 70, CurrentSellPrice = 110, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "scrap", Name = "Scrap", DisplayName = "Scrap", Category = "Salvage", PricePerScu = 25, CurrentBuyPrice = 20, CurrentSellPrice = 45, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "quantanium", Name = "Quantanium", DisplayName = "Quantanium", Category = "Minerals", PricePerScu = 5000, CurrentBuyPrice = 4500, CurrentSellPrice = 5500, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "laranite", Name = "Laranite", DisplayName = "Laranite", Category = "Minerals", PricePerScu = 3200, CurrentBuyPrice = 2900, CurrentSellPrice = 3500, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "beryl", Name = "Beryl", DisplayName = "Beryl", Category = "Minerals", PricePerScu = 1800, CurrentBuyPrice = 1600, CurrentSellPrice = 2000, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "tungsten", Name = "Tungsten", DisplayName = "Tungsten", Category = "Minerals", PricePerScu = 1500, CurrentBuyPrice = 1300, CurrentSellPrice = 1700, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "diamond", Name = "Diamond", DisplayName = "Diamond", Category = "Minerals", PricePerScu = 8000, CurrentBuyPrice = 7200, CurrentSellPrice = 8800, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "quartz", Name = "Quartz", DisplayName = "Quartz", Category = "Minerals", PricePerScu = 600, CurrentBuyPrice = 500, CurrentSellPrice = 700, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "hydrogen", Name = "Hydrogen", DisplayName = "Hydrogen", Category = "Fuel", PricePerScu = 15, CurrentBuyPrice = 12, CurrentSellPrice = 18, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "helium", Name = "Helium", DisplayName = "Helium", Category = "Fuel", PricePerScu = 20, CurrentBuyPrice = 16, CurrentSellPrice = 24, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "medical", Name = "Medical Supplies", DisplayName = "Medical", Category = "Medical", PricePerScu = 500, CurrentBuyPrice = 420, CurrentSellPrice = 580, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "food", Name = "Processed Food", DisplayName = "Food", Category = "Food", PricePerScu = 120, CurrentBuyPrice = 90, CurrentSellPrice = 150, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "water", Name = "Water", DisplayName = "Water", Category = "Food", PricePerScu = 30, CurrentBuyPrice = 20, CurrentSellPrice = 40, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "scrap-metal", Name = "Scrap Metal", DisplayName = "Scrap Metal", Category = "Salvage", PricePerScu = 40, CurrentBuyPrice = 30, CurrentSellPrice = 50, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample, IsIllegal = false },
                new() { Id = "stolen-goods", Name = "Stolen Goods", DisplayName = "Stolen Goods", Category = "Illegal", PricePerScu = 200, CurrentBuyPrice = 150, CurrentSellPrice = 250, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample, IsIllegal = true },
                new() { Id = "drugs", Name = "Drugs", DisplayName = "Drugs", Category = "Illegal", PricePerScu = 500, CurrentBuyPrice = 400, CurrentSellPrice = 600, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample, IsIllegal = true },
                new() { Id = "weapons", Name = "Weapons", DisplayName = "Weapons", Category = "Combat", PricePerScu = 1200, CurrentBuyPrice = 1000, CurrentSellPrice = 1400, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "ammo", Name = "Ammunition", DisplayName = "Ammo", Category = "Combat", PricePerScu = 80, CurrentBuyPrice = 60, CurrentSellPrice = 100, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "ship-parts", Name = "Ship Parts", DisplayName = "Ship Parts", Category = "Components", PricePerScu = 350, CurrentBuyPrice = 280, CurrentSellPrice = 420, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "electronics", Name = "Electronics", DisplayName = "Electronics", Category = "Components", PricePerScu = 280, CurrentBuyPrice = 220, CurrentSellPrice = 340, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "textiles", Name = "Textiles", DisplayName = "Textiles", Category = "Manufactured", PricePerScu = 90, CurrentBuyPrice = 70, CurrentSellPrice = 110, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "plastics", Name = "Plastics", DisplayName = "Plastics", Category = "Manufactured", PricePerScu = 60, CurrentBuyPrice = 45, CurrentSellPrice = 75, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "metals", Name = "Metals", DisplayName = "Metals", Category = "Manufactured", PricePerScu = 200, CurrentBuyPrice = 160, CurrentSellPrice = 240, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "chemicals", Name = "Chemicals", DisplayName = "Chemicals", Category = "Manufactured", PricePerScu = 150, CurrentBuyPrice = 120, CurrentSellPrice = 180, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "fuel", Name = "Fuel", DisplayName = "Fuel", Category = "Fuel", PricePerScu = 50, CurrentBuyPrice = 40, CurrentSellPrice = 60, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "ore", Name = "Raw Ore", DisplayName = "Ore", Category = "Minerals", PricePerScu = 100, CurrentBuyPrice = 80, CurrentSellPrice = 120, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "gems", Name = "Gems", DisplayName = "Gems", Category = "Minerals", PricePerScu = 4000, CurrentBuyPrice = 3500, CurrentSellPrice = 4500, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "art", Name = "Art", DisplayName = "Art", Category = "Luxury", PricePerScu = 2000, CurrentBuyPrice = 1600, CurrentSellPrice = 2400, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "jewelry", Name = "Jewelry", DisplayName = "Jewelry", Category = "Luxury", PricePerScu = 3000, CurrentBuyPrice = 2500, CurrentSellPrice = 3500, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
                new() { Id = "luxury-goods", Name = "Luxury Goods", DisplayName = "Luxury Goods", Category = "Luxury", PricePerScu = 1500, CurrentBuyPrice = 1200, CurrentSellPrice = 1800, SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample },
            };

            var random = new Random(42);
            var observations = new List<ScipDashboard.Core.Models.PriceObservation>();

            foreach (var commodity in commodities)
            {
                foreach (var location in locations)
                {
                    var baseBuy = commodity.CurrentBuyPrice;
                    var baseSell = commodity.CurrentSellPrice;
                    var buyVariation = (decimal)(random.NextDouble() * 0.3 - 0.15);
                    var sellVariation = (decimal)(random.NextDouble() * 0.3 - 0.15);
                    var buyPrice = Math.Round(baseBuy * (1 + buyVariation), 0);
                    var sellPrice = Math.Round(baseSell * (1 + sellVariation), 0);
                    var volume = random.Next(100, 5000);
                    var demand = random.Next(50, 3000);

                    observations.Add(new ScipDashboard.Core.Models.PriceObservation
                    {
                        Id = Guid.NewGuid().ToString(),
                        CommodityId = commodity.Id,
                        CommodityName = commodity.Name,
                        LocationId = location.Id,
                        LocationName = location.Name,
                        BuyPrice = buyPrice,
                        SellPrice = sellPrice,
                        Volume = volume,
                        DemandVolume = demand,
                        Timestamp = DateTime.UtcNow.AddMinutes(-random.Next(0, 120)),
                        SourceName = "Sample Data",
                        SourceStatus = ScipDashboard.Core.Models.DataSourceStatus.Sample
                    });
                }
            }

            var marketData = new ScipDashboard.Core.Models.MarketData
            {
                Name = "Star Citizen Market Data",
                Timestamp = DateTime.UtcNow,
                Locations = locations,
                Commodities = commodities,
                PriceObservations = observations
            };

            await dataService.SaveMarketDataAsync(marketData);
        }

        public class InMemoryAppSettings : IAppSettings
        {
            private readonly Dictionary<string, string> _values = new();
            public string? Get(string key) => _values.TryGetValue(key, out var v) ? v : null;
            public void Set(string key, string value) => _values[key] = value;
        }
    }
}
