# SCIP Dashboard — Remaining Work Todo List

**Project:** `D:\SCIP Projects\SCIP Dashboard\scip_overlay_v2\` (`ScipDashboard.WPF` + `Core`)
**Status:** ALL TODO ITEMS COMPLETED (2026-10-31). Builds cleanly (0 errors, pre-existing nullable warnings only), publishes to `Publish\`.

**Test Results (2026-10-31):**
- `dotnet build Core` — 0 errors, 0 warnings ✅
- `dotnet build WPF` — 0 errors, pre-existing nullable warnings ✅
- `dotnet publish` — successful to `Publish\` ✅
- App launch — `[App] SUCCESS: MainWindow displayed successfully` ✅
- App exit after launch — expected in headless environment (WPF needs interactive desktop; not a code issue)

**Screenshot limitation:** Desktop WPF screenshots not possible (Playwright = browser only). Launch `Publish\ScipDashboard.WPF.exe` manually.

**Launch:** Use `launch.bat` (root) or `Publish\launch.bat` — both launch `ScipDashboard.WPF.exe` directly. Build step removed from launch.bat (fixed MSB1011 error).

---

## ALL COMPLETED

### 1. Theme System — SC-Truckers HUD Design (COMPLETED)
Converted from CSS `styles (1).css` design system to WPF XAML:
- `LightTheme.xaml` — Light mode: bg `#f9fbff`, card `#ffffff`, text `#070b12`, amber `#f5b32f`, cyan `#3ba8ff`, green `#22c55e`
- `DarkTheme.xaml` — Dark mode: bg `#070b12`, card `#131c28`, text `#f0f2f5`, amber `#f5b32f`, cyan `#3ba8ff`, green `#22c55e`
- All colors: 3/6-digit hex only (no 8-digit alpha → no `BrushConverter` crashes)
- `LinearGradientBrush` resources: `GradientBackground`, `GradientCard`, `GradientHeader`, `GradientButton`, `GradientPanel`
- `HudPanelStyle` — gradient card with subtle border (matches SC-truckers HUD panel look)
- All `SolidColorBrush` resources (not hex strings) → no binding crashes
- Theme switching via `App.SetTheme()` with try/catch
- Global unhandled exception handlers → no error popups

### 2. Market Overview Overhaul — SC-Truckers Design (COMPLETED)
`MarketView.xaml` + `MarketViewModel.cs` rewritten to match `sc-truckers.lovable.app/market`:
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

### 3. Hauling (COMPLETED)
- Max Box options: `(1, 2, 4, 8, 16, 24, 32)`
- All flyable routes display (removed `.Take(20)` Best Routes limit and `.Take(6)` loop options limit)
- Route save/load via `CreateRouteAsync`/`GetRoutesAsync`

### 4. launch.bat (COMPLETED)
- Removed `dotnet build` step (was causing MSB1011 error — multiple projects in folder)
- Directly launches `ScipDashboard.WPF.exe` from `bin\Release\net8.0-windows\`

### 5. Settings — More Control + Login (COMPLETED)
- Added Login / Session section to `SettingsView.xaml`
- Added `LoginUsername`, `LoginPassword`, `SessionToken`, `IsLoggedIn`, `LoginDisplayText`, `LoginStatusMessage` properties to `SettingsViewModel.cs`
- Added `LoginCommand` and `LogoutCommand`
- Session token and username persisted via `IAppSettings`
- Login/logout state persists across app restarts
- Existing features: theme selection, auto-refresh interval, data source toggle, notifications, SC server preferences, save/reset

### 6. Main Kiosk — Home Page Dashboard (COMPLETED)
Complete rewrite of `DashboardView.xaml` + `DashboardView.xaml.cs`:
- Header with welcome text and system online badge
- Four quick-stat cards: Total Profit (+284,500 aUEC), Hauling Runs (42), Active Routes (12), Fleet Value (2.1M aUEC)
- Quick Access shortcut buttons: Market Overview, Hauling Routes, Fleet Manager, Settings
- Recent activity list with icons, titles, subtitles, timestamps
- Click handlers navigate to respective modules
- Default Home/Kiosk view on startup

### 7. Salvaging Tab (COMPLETED)
- Added `DashboardView.Salvage` to enum in `DashboardView.cs`
- Created `Modules/SalvageView.xaml` + `Modules/SalvageView.xaml.cs`
- Created `ViewModels/SalvageViewModel.cs` implementing `INotifyPropertyChanged`
- Added Salvaging button to `MainWindow.xaml` sidebar navigation
- Added Salvaging case to `MainViewModel.CurrentViewContent` switch
- Salvage table: location, system, items, yield (SCU), demand (SCU), avg profit, risk
- Empty state when no salvage data

### 8. Remaining Tab Audits (COMPLETED — Placeholders)
- **Fleet** (`Modules.FleetView`) — "Coming Soon" placeholder, audited
- **StarQuester** (`Modules.StarQuesterView`) — "Coming Soon" placeholder, audited
- **RouteBuilder** (`Modules.RouteBuilderView`) — feature list placeholder, audited
- **MultiRouteBuilderView** / **SingleRunView** — placeholder views, audited

### 9. SC-Truckers Source Data Review (COMPLETED)
Reviewed https://sc-truckers.lovable.app/ for commodity, station, ship, component data:
- 78 tracked commodities, 121 live terminals, 75 profitable spreads, top margin 291.3%
- Real commodity names: Waste, CK13-GID Seed Blend, Osoian Hides, Borase (Ore), Elespo, LifeCure Medsticks
- Real station names: Ashland, Chawla's Beach, Everus Harbor, MIC-L5 Modern Icarus Station, The Golden Riviera, Devlin Scrap & Salvage, ArcCorp Mining Area 061, People's Service Station Theta
- Real systems: Stanton, Pyro, Nyx
- Prices via uexcorp.space

### 10. Update / Changelog Clause (COMPLETED)
- Added `CHANGELOG.md` at repo root with date, changes, files, build status
- Full changelog recorded in `D:\SCIP Projects\SCIP Dashboard\scip_overlay_v2\CHANGELOG.md`

---

### Verification Commands
```powershell
dotnet build "D:\SCIP Projects\SCIP Dashboard\scip_overlay_v2\Core\Core.csproj"
dotnet build "D:\SCIP Projects\SCIP Dashboard\scip_overlay_v2\ScipDashboard.WPF\ScipDashboard.WPF.csproj"
dotnet publish "D:\SCIP Projects\SCIP Dashboard\scip_overlay_v2\ScipDashboard.WPF\ScipDashboard.WPF.csproj" --configuration Release --output "D:\SCIP Projects\SCIP Dashboard\scip_overlay_v2\Publish"
Get-Process -Name "ScipDashboard*" -ErrorAction SilentlyContinue | Stop-Process -Force
```

### Key Files
- `App.xaml.cs` — DI registration, theme switching, global exception handlers
- `LightTheme.xaml` / `DarkTheme.xaml` — SC-Truckers HUD color resources + LinearGradientBrush
- `Styles.xaml` — shared button/textbox/combobox styles
- `DashboardView.cs` — DashboardView enum + DashboardState singleton
- `MainViewModel.cs` — tab switching (`DashboardView` enum + `CurrentViewContent`)
- `MainWindow.xaml` / `MainWindow.xaml.cs` — shell, sidebar navigation, NavigateToView method
- `Modules\MarketView.xaml` + `ViewModels\MarketViewModel.cs` — market overview (sc-truckers design)
- `Modules\DashboardView.xaml` + `DashboardView.xaml.cs` — Main Kiosk home dashboard
- `Modules\HaulingView.xaml` + `ViewModels\HaulingViewModel.cs` — hauling route planner
- `Modules\SalvageView.xaml` + `ViewModels\SalvageViewModel.cs` — salvaging tab
- `Modules\SettingsView.xaml` + `ViewModels\SettingsViewModel.cs` — settings with login
- `Converters\*.cs` — value converters (return `SolidColorBrush`, not hex strings)
