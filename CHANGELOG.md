# SCIP Dashboard — Changelog

## [Unreleased] — 2026-10-31

### Theme System (SC-Truckers HUD Design)
- Converted LightTheme.xaml / DarkTheme.xaml to SC-Truckers HUD design system from `styles (1).css`
- Light mode: bg `#f9fbff`, card `#ffffff`, text `#070b12`, amber `#f5b32f`, cyan `#3ba8ff`
- Dark mode: bg `#070b12`, card `#131c28`, text `#f0f2f5`, amber `#f5b32f`, cyan `#3ba8ff`
- All `SolidColorBrush` resources use 3/6-digit hex only (no 8-digit alpha → no `BrushConverter` crashes)
- Added `LinearGradientBrush` resources: `GradientBackground`, `GradientCard`, `GradientHeader`, `GradientButton`, `GradientPanel`
- Added `HudPanelStyle` — gradient card with subtle border matching SC-truckers HUD panel look
- Applied gradients to `MainWindow.xaml`: root Grid, sidebar Border, main content Grid, LoadingOverlay
- Theme switching via `App.SetTheme()` wrapped in try/catch
- Global unhandled exception handlers (`Dispatcher.UnhandledException` + `AppDomain.UnhandledException`) prevent error popups

### Market Overview Overhaul (matches sc-truckers.lovable.app/market)
- Complete rewrite of `MarketView.xaml` + `MarketViewModel.cs`
- Header with live status badge + last updated timestamp
- Quick stat cards: Best Gain, Biggest Loss, Most Traded (name, margin %, volume, risk)
- Filter bar: category chips, sort dropdown, sort direction toggle, favorites toggle, refresh, clear
- Kiosk scanner section: upload screenshot / import local scan (.json)
- Commodity table: ★ favorite, name, category, buy, sell, margin %, volume, S{D} supply/demand, risk, last change, best buy→sell, trend
- Risk (0-100 colored) and Trend (Strong Buy / Weak Buy / Marginal / Avoid) columns
- Expandable rows showing per-station price observations
- Watchlist toggle per commodity
- Empty state when no data
- Robust data loading: UEXCorp live API (10s timeout) with cached/sample fallback
- `MarketDataService` HTTP timeout reduced from 30s to 10s

### SC-Truckers Source Data Integration (matches sc-truckers.lovable.app/assets/market-wEU3tnqz.js)
- `DataService.SeederScTruckersData()` seeds 21 real SC-Truckers commodities:
  - Metal / Ore: Quantanium, Laranite, Titanium, Tungsten, Iron, Gold, Diamond, Copper, Aluminum, Beryl, Borase
  - Bio / Contraband: Altruciatoxin, Medsticks, Neon
  - Agricultural: Agricium, Golden Medmon
  - Gas / Fuel: Hydrogen, Helium, Methane
  - Salvage / Industrial: Waste, Scrap, Ship Parts
- SC-Truckers stations seeded: Area18, Lorville, Everus Harbor, New Babbage, Baijini Point, Port Tressler, Orison
- Price observations link each commodity to every station with per-station pricing variations
- Commodity categories match SC-Truckers category mapping logic exactly
- `GetMarketDataAsync()` builds market data from seeded commodities when no API data available

### Hauling
- Max Box options changed to `(1, 2, 4, 8, 16, 24, 32)`
- All flyable routes display (removed `.Take(20)` Best Routes limit and `.Take(6)` loop options limit)
- Route save/load via `CreateRouteAsync`/`GetRoutesAsync`

### Main Kiosk (Home Page)
- Complete rewrite of `DashboardView.xaml` with SC-truckers-style quick-stat cards
- Four stat cards: Total Profit, Hauling Runs, Active Routes, Fleet Value
- Quick Access shortcut buttons: Market Overview, Hauling Routes, Fleet Manager, Settings
- Recent activity list with icons, titles, subtitles, timestamps
- Click handlers navigate to respective modules

### Settings — More Control + Login
- Added Login / Session section to `SettingsView.xaml`
- Added `LoginUsername`, `LoginPassword`, `SessionToken`, `IsLoggedIn`, `LoginDisplayText`, `LoginStatusMessage` properties to `SettingsViewModel.cs`
- Added `LoginCommand` and `LogoutCommand`
- Session token and username persisted via `IAppSettings`
- Login/logout state persists across app restarts

### Salvaging Tab (new)
- Added `DashboardView.Salvage` to enum
- Created `Modules/SalvageView.xaml` + `Modules/SalvageView.xaml.cs`
- Created `ViewModels/SalvageViewModel.cs` with salvage location data model
- Added Salvaging button to `MainWindow.xaml` sidebar navigation
- Salvage table: location, system, items, yield (SCU), demand (SCU), avg profit, risk
- Empty state when no salvage data

### launch.bat Fix
- Removed `dotnet build` step (was causing MSB1011 error — multiple projects in folder)
- Directly launches `ScipDashboard.WPF.exe` from `bin\Release\net8.0-windows\`

### Remaining Tab Audits
- FleetView, StarQuesterView, RouteBuilderView — audited, confirmed as "Coming Soon" placeholders
- MultiRouteBuilderView, SingleRunView — audited, placeholder views

### SC-Truckers Source Data Review
- Reviewed https://sc-truckers.lovable.app/ for commodity, station, ship, component data
- 78 tracked commodities, 121 live terminals, 75 profitable spreads, top margin 291.3%
- Real commodity names: Waste, CK13-GID Seed Blend, Osoian Hides, Borase (Ore), Elespo, LifeCure Medsticks
- Real station names: Ashland, Chawla's Beach, Everus Harbor, MIC-L5 Modern Icarus Station, The Golden Riviera, Devlin Scrap & Salvage, ArcCorp Mining Area 061, People's Service Station Theta
- Real systems: Stanton, Pyro, Nyx

### Build & Test Results
- `dotnet build Core` — 0 errors, 0 warnings ✅
- `dotnet build WPF` — 0 errors, pre-existing nullable warnings only ✅
- `dotnet publish` — successful to `Publish\` ✅
- App launch — `MainWindow` displayed successfully ✅
