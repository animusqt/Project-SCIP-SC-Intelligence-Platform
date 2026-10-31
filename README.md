# SC Intelligence Platform Dashboard

A native WPF (.NET 8) logistics dashboard for Star Citizen, themed after [SC-Truckers](https://sc-truckers.lovable.app/). Tracks market prices, hauling routes, fleet assets, and salvage operations with live UEXCorp data integration.

**Live repo:** https://github.com/animusqt/Project-SCIP-SC-Intelligence-Platform  
**Branch:** `fix-blank-views`

---

## Features

### Market Overview
- Commodity table with real SC-Truckers data (21 commodities across Metal/Ore, Bio/Contraband, Agricultural, Gas/Fuel, Salvage/Industrial categories)
- Live status badge + last updated timestamp
- Quick stat cards: Best Gain, Biggest Loss, Most Traded (name, margin %, volume, risk)
- Filter bar: category chips, sort dropdown, sort direction toggle, favorites toggle, refresh, clear
- Risk score (0-100, color-coded) and Trend indicator (Strong Buy / Weak Buy / Marginal / Avoid)
- Expandable rows showing per-station price observations
- Watchlist toggle per commodity (persisted to local settings)
- Empty state when no data matches filters
- Kiosk scanner section: upload screenshot (.png/.jpg/.webp) or import local scan (.json)

### Main Kiosk (Home Dashboard)
- Four quick-stat cards: Total Profit, Hauling Runs, Active Routes, Fleet Value
- Quick Access shortcut buttons: Market Overview, Hauling Routes, Fleet Manager, Settings
- Recent activity feed with icons, titles, subtitles, timestamps

### Hauling
- Route planner with buy/sell location pickers
- Profit-per-SCU and total-profit calculations
- SCU capacity selector: `(1, 2, 4, 8, 16, 24, 32)`
- All flyable routes display (no artificial limits)
- Route save/load via data service
- Risk indicators per route

### Fleet
- Ship list with capacity and profit tracking
- Fleet value summary

### StarQuester
- Quest/mission tracking

### RouteBuilder / MultiRouteBuilder / SingleRun
- Route planning tools

### Salvaging
- Salvage location list with material yield estimates
- Cargo fill percentage
- Profit per run calculations

### Settings
- Theme selector: Light / Dark (SC-Truckers HUD design system)
- Auto-refresh interval picker (minutes)
- Data source toggle (Sample / UEXCorp / Live / Cached)
- Login / Session management (username/password, session token persistence)
- Star Citizen server preferences (Persistent Universe / Arena Commander)
- Save Settings / Reset to Defaults

---

## Theme System

Built on the **SC-Truckers HUD design system** (converted from `styles (1).css`):

| Element | Light Mode | Dark Mode |
|---------|-----------|-----------|
| Background | `#f9fbff` | `#070b12` |
| Card | `#ffffff` | `#131c28` |
| Primary Text | `#070b12` | `#f0f2f5` |
| Secondary Text | `#4a5568` | `#94a3b8` |
| Primary Accent (Amber) | `#f5b32f` | `#f5b32f` |
| Secondary Accent (Cyan) | `#3ba8ff` | `#3ba8ff` |
| Success (Green) | `#22c55e` | `#22c55e` |

All colors use 3-digit or 6-digit hex only (no 8-digit alpha to avoid WPF `BrushConverter` runtime parse errors). All brush resources return `SolidColorBrush` objects, not hex strings.

Gradient resources: `GradientBackground`, `GradientCard`, `GradientHeader`, `GradientButton`, `GradientPanel`

Theme switching happens at runtime via `App.SetTheme()` (wrapped in try/catch). Global unhandled exception handlers prevent error popups from crashing the app.

---

## Data Sources

- **UXCorp API** (uexcorp.space): Real-time commodity prices, supply, demand, and location data (10-second timeout with fallback to cached/sample data)
- **Sample data:** SC-Truckers commodity names, categories, and stations seeded locally when API is unavailable
- **Kiosk scanner:** Upload screenshots of commodity terminals for local price capture
- **Local scan import:** Import `.json` files from in-game SC Price Scanner app (F9)

Seeded commodity data includes real SC-Truckers items: Quantanium, Laranite, Titanium, Tungsten, Iron, Gold, Diamond, Copper, Aluminum, Beryl, Borase, Altruciatoxin, Medsticks, Neon, Agricium, Golden Medmon, Hydrogen, Helium, Methane, Waste, Scrap, Ship Parts.

Seeded stations: Area18, Lorville, Everus Harbor, New Babbage, Baijini Point, Port Tressler, Orison (Stanton system).

---

## Project Structure

```
scip_overlay_v2/
├── Core/
│   ├── Core.csproj
│   ├── Data/
│   │   └── IDataService.cs        # Data access + SC-Truckers sample data seeding
│   ├── Models/
│   │   └── Models.cs              # Data models (Commodity, Location, Route, Ship, etc.)
│   └── Services/
│       └── MarketDataService.cs   # UEXCorp API integration + sample fallback
├── ScipDashboard.WPF/
│   ├── App.xaml.cs               # DI registration, theme switching, global exception handlers
│   ├── LightTheme.xaml           # SC-Truckers light mode colors + LinearGradientBrush resources
│   ├── DarkTheme.xaml            # SC-Truckers dark mode colors + LinearGradientBrush resources
│   ├── Styles.xaml               # Shared button/textbox/combobox styles
│   ├── MainViewModel.cs          # Tab switching (DashboardView enum + CurrentViewContent)
│   ├── MainWindow.xaml           # Shell + sidebar navigation
│   ├── MainWindow.xaml.cs        # Navigation handlers + loading overlay
│   ├── Converters/               # Value converters (SolidColorBrush, not hex strings)
│   ├── Modules/
│   │   ├── DashboardView.xaml    # Main Kiosk home dashboard
│   │   ├── MarketView.xaml       # Market overview (SC-Truckers design)
│   │   ├── HaulingView.xaml      # Hauling route planner
│   │   ├── SalvageView.xaml      # Salvaging tab
│   │   ├── SettingsView.xaml     # Settings with login
│   │   ├── FleetView.xaml        # Fleet manager
│   │   ├── StarQuesterView.xaml  # Quest tracking
│   │   ├── RouteBuilderView.xaml # Route builder
│   │   └── ...                   # MultiRouteBuilder, SingleRun placeholders
│   ├── ViewModels/
│   │   ├── MarketViewModel.cs    # Market data + quick stats + risk/trend calc
│   │   ├── HaulingViewModel.cs   # Hauling route calculations
│   │   ├── SettingsViewModel.cs  # Settings + login persistence
│   │   └── SalvageViewModel.cs   # Salvage data model
│   └── ScipDashboard.WPF.csproj
├── Publish/                      # Published output (dotnet publish --configuration Release)
├── launch.bat                    # Launches ScipDashboard.WPF.exe directly
├── TODO.md                       # Full remaining work list
├── CHANGELOG.md                  # Change history
└── .gitignore                    # Build artifacts excluded
```

---

## Build & Run

### Prerequisites
- .NET 8 SDK
- Windows (WPF requires Windows desktop)

### Build
```powershell
dotnet build "scip_overlay_v2\ScipDashboard.WPF\ScipDashboard.WPF.csproj"
dotnet build "scip_overlay_v2\Core\Core.csproj"
```

### Publish
```powershell
dotnet publish "scip_overlay_v2\ScipDashboard.WPF\ScipDashboard.WPF.csproj" --configuration Release --output "scip_overlay_v2\Publish"
```

### Run
```powershell
# From repo root
launch.bat

# Or directly
& "scip_overlay_v2\Publish\ScipDashboard.WPF.exe"
```

### Expected Build Output
- `Core`: 0 errors, 0 warnings
- `WPF`: 0 errors (pre-existing nullable warnings in SettingsViewModel only)
- `Publish`: Successful output to `Publish\` directory

---

## Screenshots

*Coming soon — launch the app to view the SC-Truckers themed dashboard.*

---

## Development Notes

- WPF requires an interactive desktop session (cannot run in headless/CI environments)
- Stale `ScipDashboard.WPF.exe` processes can lock DLLs — kill them before rebuilding: `Get-Process -Name "ScipDashboard*" | Stop-Process -Force`
- All `SolidColorBrush` resources use 3-digit/6-digit hex only — 8-digit alpha hex causes WPF `BrushConverter` runtime parse errors
- Global exception handlers in `App.xaml.cs` catch and log errors instead of showing error popups

---

## License

MIT License — see [LICENSE](LICENSE) for details.

---

## Acknowledgments

- [SC-Truckers](https://sc-truckers.lovable.app/) — HUD design system and commodity data reference
- [UEXCorp](https://uexcorp.space/) — Real-time Star Citizen commodity price data
- Star Citizen community — commodity names, station data, and trading information
