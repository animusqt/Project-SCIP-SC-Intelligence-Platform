using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ScipDashboard.Core.Data;
using ScipDashboard;
using ScipDashboard.WPF;

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

        // Theme Properties
        public string Theme
        {
            get => _theme;
            set 
            { 
                if (_theme != value) 
                {
                    App.SetTheme(value);
                    _theme = value;
                    OnPropertyChanged();
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
            set { if (_isLoggedIn != value) { _isLoggedIn = value; OnPropertyChanged(); OnPropertyChanged(nameof(LoginDisplayText)); } }
        }

        public string LoginDisplayText => IsLoggedIn ? $"Logged in as {LoginUsername}" : "Not logged in";

        public string LoginStatusMessage
        {
            get => _loginStatusMessage;
            set { if (_loginStatusMessage != value) { _loginStatusMessage = value; OnPropertyChanged(); } }
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
        public ICommand LoginCommand { get; set; }
        public ICommand LogoutCommand { get; set; }

        public SettingsViewModel()
        {
            InitializeFromSettings();
            CreateDataSources();
            CreatePreferences();
            InitializeCommands();
        }

        private void InitializeFromSettings()
        {
            // Load theme from settings
            var savedTheme = App.AppSettings?.Get("theme");
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
            _sessionToken = App.AppSettings?.Get("sessionToken") ?? string.Empty;
            LoginUsername = App.AppSettings?.Get("loginUsername") ?? string.Empty;
            IsLoggedIn = !string.IsNullOrEmpty(_sessionToken);

            _lastSettingsUpdate = DateTime.Now;
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

            LoginCommand = new RelayCommand(() =>
            {
                ExecuteLogin();
            });

            LogoutCommand = new RelayCommand(() =>
            {
                ExecuteLogout();
            });
        }

        private void ExecuteLogin()
        {
            if (string.IsNullOrWhiteSpace(LoginUsername) || string.IsNullOrWhiteSpace(LoginPassword))
            {
                LoginStatusMessage = "Please enter username and password.";
                return;
            }

            // Simulate login - in production this would call an auth API
            _sessionToken = $"sess_{LoginUsername}_{DateTime.UtcNow.Ticks}";
            IsLoggedIn = true;
            LoginStatusMessage = $"Logged in as {LoginUsername}";
            SaveAllSettings();
        }

        private void ExecuteLogout()
        {
            _sessionToken = string.Empty;
            LoginUsername = string.Empty;
            LoginPassword = string.Empty;
            IsLoggedIn = false;
            LoginStatusMessage = "Logged out.";
            SaveAllSettings();
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

            // Reset login session
            _sessionToken = string.Empty;
            LoginUsername = string.Empty;
            LoginPassword = string.Empty;
            IsLoggedIn = false;
            LoginStatusMessage = string.Empty;

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
}
