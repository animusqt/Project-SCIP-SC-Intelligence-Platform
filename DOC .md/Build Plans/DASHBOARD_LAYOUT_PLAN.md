# SCIP Dashboard - Layout Plan

## Vision: "Game Launcher" Aesthetic Dashboard

### Design Philosophy
- **High Data Density**: Maximum information visibility with minimal clutter
- **Visual Clarity**: High contrast, clear hierarchy, instant readability
- **Professional Game Launcher Feel**: Polished, responsive, modern
- **Local-First**: All features work without internet connection

---

## Color Palette

| Name | Hex | Usage |
|------|-----|-------|
| Deep Charcoal | `#2c3e50` | Sidebar background, primary text |
| Navy Sidebar | `#1a252f` | Sidebar accent |
| Safety Orange | `#FFC107` | Primary actions, highlights, buttons |
| Background Gray | `#fafafa` | Main background |
| Card Background | `#f5f5f5` | Card backgrounds |
| Orange Light | `#ffa000` | Hover states |
| Orange Dark | `#ff8f00` | Active states |
| Text Light | `#ecf0f1` | Sidebar text |
| Text Dark | `#2c3e50` | Main text |

---

## Window Design

### Window Style
- **Frameless**: Custom borderless window
- **Custom Controls**: Orange minimize/maximize/close buttons in top-right
- **Always on Top**: Window stays above other applications
- **Minimize to Tray**: Minimizes to system tray icon
- **Corner Radius**: 8px rounded corners
- **Shadow**: Subtle drop shadow

### Window Controls (Top-Right)
```
┌───────────────────────────────────────────────────────┐
│                                                           │
│  [Minimize] [Maximize] [Close]   (Orange, 24x24px)    │
│                                                           │
└───────────────────────────────────────────────────────┘
```

---

## Layout Structure

### Overall Layout (1400x800 default)
```
┌─────────────────────────────────────────────────────────────────┐
│  SCIP Dashboard Header (Optional)                                     │
├────────────────────┬────────────────────────────────────────────┤
│                    │                                                    │
│  SIDEBAR           │   MAIN CONTENT AREA                              │
│  (200px)           │                                                    │
│                    │  ┌─────────────────────────────────────────┐ │
│  ● Dashboard       │  │  STATS CARDS ROW (Full Width)             │ │
│  ● Market Overview  │  │  [Card 1] [Card 2] [Card 3] [Card 4]       │ │
│  ● Hauling         │  │                                                 │ │
│  ● Fleet           │  └─────────────────────────────────────────┘ │
│  ● StarQuester     │                                                    │
│  ● Settings        │  ┌─────────────────────────────────────────┐ │
│                    │  │  DYNAMIC CONTENT (Module Views)              │ │
│  [User Avatar]     │  │                                                 │ │
│  [Status]          │  │  Module content loads here based              │ │
│                    │  │  on sidebar selection                         │ │
│                    │  │                                                 │ │
│                    │  └─────────────────────────────────────────┘ │
│                    │                                                    │
├────────────────────┴────────────────────────────────────────────┤
│  Status Bar (Optional): Connection status, updates, etc.            │
└─────────────────────────────────────────────────────────────────┘
```

---

## Sidebar Navigation

### Structure
- **Width**: 200px
- **Background**: Deep Charcoal (`#2c3e50`)
- **Padding**: 10px
- **Corner Radius**: 8px
- **Margin**: 10px from top/bottom

### Navigation Items
1. **Dashboard** (icon: 📊)
2. **Market Overview** (icon: 💰)
3. **Hauling** (icon: 🚛)
4. **Fleet** (icon: ⛵)
5. **StarQuester** (icon: ⭐)
6. **Settings** (icon: ⚙️)

### Button Style
```
- Background: Transparent
- Foreground: White (#ecf0f1)
- Height: 30px
- Margin: 0,5px
- Padding: 10px,0px
- Font: Segoe UI, 12pt
- Hover: Background #34495e
- Active: Background #FFC107, Foreground Black
- Border: None
- Text Alignment: Left
- Icon: Left-aligned, 16px
```

### User Section (Bottom of Sidebar)
```
┌─────────────────────┐
│  [Avatar 40x40]     │
│  Username           │
│  Online Status      │
│  [Logout Button]    │
└─────────────────────┘
```

---

## Stats Cards Row

### Layout
- **Position**: Top of main content area
- **Width**: Full width of content area
- **Height**: 120px
- **Margin**: 0,0,20px,0 (bottom margin only)
- **Grid**: 4 columns, equal width

### Card Design
```
┌─────────────────┐
│  Background:     │  Card Background (#f5f5f5)    │
│  Border:         │  1px solid #e0e0e0             │
│  Corner Radius:  │  8px                           │
│  Padding:        │  16px                          │
│  Shadow:         │  0 2px 4px rgba(0,0,0,0.1)     │
└─────────────────┘
```

### Card Structure
```
┌─────────────────────┐
│  Title (12pt)       │  ← Top-left, subtle color
│  ↓                  │
│  Value (36pt)       │  ← Center, bold
│  Label (10pt)       │  ← Bottom, description
└─────────────────────┘
```

### Card 1: Active Hauls
- **Title**: "Active Hauls"
- **Value**: Number (e.g., "5")
- **Background**: Light blue (`#e3f2fd`)
- **Accent Color**: `#1565c0`

### Card 2: Available Routes
- **Title**: "Available Routes"
- **Value**: Number (e.g., "12")
- **Background**: Light green (`#e8f5e9`)
- **Accent Color**: `#2e7d32`

### Card 3: Fleet Available
- **Title**: "Fleet Available"
- **Value**: Number (e.g., "8/10")
- **Background**: Light orange (`#fff3e0`)
- **Accent Color**: `#ef6c00`

### Card 4: Cargo Capacity
- **Title**: "Cargo Capacity"
- **Value**: "1,500/2,000 SCU"
- **Background**: Light purple (`#f3e5f5`)
- **Accent Color**: `#7b1fa2`

---

## Module Views Structure

### Module Container
- **Position**: Below stats cards
- **Padding**: 0px
- **Background**: Transparent
- **Animation**: Fade in/out (0.3s) when switching modules

### Dashboard Module (Default View)
```
┌─────────────────────────────────────────────┐
│  Recent Activity                                  │
├─────────────────────────────────────────────┤
│  ┌─────────────────┬──────────────────────┐ │
│  │  Haul Started    │  2 hours ago          │ │
│  │  Route: Sol...   │  Status: In Progress   │ │
│  ├─────────────────┼──────────────────────┤ │
│  │  Haul Completed  │  5 hours ago          │ │
│  │  Route: Earth... │  Status: Completed    │ │
│  ├─────────────────┼──────────────────────┤ │
│  │  Market Update   │  1 day ago            │ │
│  └─────────────────┴──────────────────────┘ │
│                                                 │
│  Quick Actions                                  │
│  [New Haul] [New Route] [Market Scan]          │
└─────────────────────────────────────────────┘
```

### Market Overview Module
```
┌─────────────────────────────────────────────┐
│  Search & Filters                             │
│  [Search Box] [Category ▼] [Source ▼]          │
│                                                 │
├─────────────────────────────────────────────┤
│  Commodity Table                               │
│  ┌──────────┬──────────┬────────┬────────┐ │
│  │ Name     │ Buy      │ Sell    │ Margin  │ │
│  │ Gold     │ 150      │ 165     │ +10%   │ │
│  │ Titanium │ 80       │ 95      │ +18.75%│ │
│  │ Fuel     │ 20       │ 25      │ +25%   │ │
│  └──────────┴──────────┴────────┴────────┘ │
│                                                 │
├─────────────────────────────────────────────┤
│  Price History Chart                            │
│  [Interactive Chart Area]                      │
│  Time Range: [1h] [24h] [7d] [30d]            │
└─────────────────────────────────────────────┘
```

### Hauling Module
```
┌─────────────────────────────────────────────┐
│  Route Builder                                  │
├──────────────────────┬──────────────────────┤
│  Stops                 │  Route Summary        │
│  ┌──────────────────┐│  Total Distance:     │
│  │ 1. Port Olisar    ││  Estimated Time:     │
│  │ 2. Grim Hex       ││  Gross Margin:       │
│  │ 3. Leviathan      ││  Net Profit:         │
│  │ 4. Microtech      ││                      │
│  │  [+ Add Stop]     ││  [Save Route]        │
│  └──────────────────┘│  [Load Route]        │
│                      │  [Optimize]           │
└──────────────────────┴──────────────────────┘
```

### Fleet Module
```
┌─────────────────────────────────────────────┐
│  Search: [____________]  Filter: [All ▼]        │
│                                                 │
│  ┌─────────────────────────────────────────┐ │
│  │  My Fleet                                    │ │
│  │  ┌──────────┬──────────┬────────┬──────┐│ │
│  │  │ Ship      │ Location │ Status  │ Cargo ││ │
│  │  │ C2 Her... │ Port Oli │ Ready   │ 0/100 ││ │
│  │  │ Hull B    │ Grim Hex│ In Use  │ 50/100││ │
│  │  └──────────┴──────────┴────────┴──────┘│ │
│  └─────────────────────────────────────────┘ │
│                                                 │
│  Ship Directory (Searchable)                         │
│  [Tab: All Ships] [Tab: My Fleet] [Tab: Watchlist] │
└─────────────────────────────────────────────┘
```

### StarQuester Module
```
┌─────────────────────────────────────────────┐
│  Quest Capture                                  │
│  [Paste Text] [OCR Scan] [Manual Entry]        │
│                                                 │
│  Active Quests                                   │
│  ┌─────────────────────────────────────────┐ │
│  │  Quest: Delivery Mission                    │ │
│  │  Faction: UEE Navy                           │ │
│  │  Reward: 5,000 UEC                           │ │
│  │  Status: [In Progress]                       │ │
│  │  Progress: [======          ] 50%            │ │
│  └─────────────────────────────────────────┘ │
│                                                 │
│  Reputation Tracker                              │
│  UEE Navy: [=======>          ] 35% (Loyal)    │
│  Crusader:  [=====>            ] 28% (Neutral)  │
└─────────────────────────────────────────────┘
```

### Settings Module
```
┌─────────────────────────────────────────────┐
│  Settings                                        │
├─────────────────────────────────────────────┤
│  Appearance                                       │
│  Theme: [Dark ▼]                                  │
│  Accent Color: [Orange ▼]                       │
│                                                 │
│  Data Sources                                    │
│  ✓ Live Market Data (uexcorp.space)              │
│  ✓ Local Cache                                  │
│  ☐ Experimental API                               │
│                                                 │
│  Storage                                        │
│  Cache Location: D:\...\AppData\SCIP           │
│  [Clear Cache] [Export Data] [Import Data]       │
│                                                 │
│  About                                          │
│  Version: 1.0.0                                  │
│  [Check for Updates]                             │
└─────────────────────────────────────────────┘
```

---

## Typography

| Element | Font | Size | Weight | Color |
|---------|------|------|--------|-------|
| Window Title | Segoe UI | 18pt | Bold | #2c3e50 |
| Card Title | Segoe UI | 12pt | SemiBold | #1565c0 / #2e7d32 / #ef6c00 / #7b1fa2 |
| Card Value | Segoe UI | 36pt | Bold | #2c3e50 |
| Sidebar Text | Segoe UI | 14pt | Regular | #ecf0f1 |
| Sidebar Active | Segoe UI | 14pt | SemiBold | #2c3e50 |
| Table Headers | Segoe UI | 12pt | SemiBold | #2c3e50 |
| Table Cells | Segoe UI | 11pt | Regular | #2c3e50 |
| Button Text | Segoe UI | 12pt | SemiBold | #2c3e50 / White |
| Labels | Segoe UI | 10pt | Regular | #666666 |

---

## Button Styles

### Primary Button (Orange)
```
- Background: #FFC107
- Foreground: #2c3e50
- Height: 32px
- Padding: 16px, 24px
- Corner Radius: 4px
- Font: Segoe UI, 12pt, SemiBold
- Hover: #ffa000
- Active: #ff8f00
- Border: None
```

### Secondary Button (Outline)
```
- Background: Transparent
- Border: 1px solid #FFC107
- Foreground: #FFC107
- Height: 32px
- Padding: 16px, 24px
- Corner Radius: 4px
- Font: Segoe UI, 12pt, SemiBold
- Hover: Background #FFC107, Foreground #2c3e50
```

### Icon Button
```
- Size: 24px x 24px
- Background: Transparent
- Icon Color: #FFC107
- Hover: Background #FFC107, Icon Color: #2c3e50
- Corner Radius: 4px
```

---

## Input Styles

### TextBox
```
- Background: White
- Border: 1px solid #cccccc
- Foreground: #2c3e50
- Height: 32px
- Padding: 8px, 12px
- Corner Radius: 4px
- Font: Segoe UI, 12pt
- Focus: Border #FFC107, Box Shadow 0 0 0 2px rgba(255,193,7,0.2)
```

### Search Box
```
- Same as TextBox
- Icon: Search (🔍) on left, color #999999
- Placeholder: "Search..."
```

### Dropdown/ComboBox
```
- Background: White
- Border: 1px solid #cccccc
- Foreground: #2c3e50
- Height: 32px
- Padding: 8px, 12px
- Corner Radius: 4px
- Arrow: #666666
- Hover: Border #999999
- Focus: Border #FFC107
```

---

## Table Styles

### Header
```
- Background: #f5f5f5
- Foreground: #2c3e50
- Font: Segoe UI, 11pt, SemiBold
- Height: 40px
- Border Bottom: 1px solid #e0e0e0
- Sortable: Show arrow indicator on hover
```

### Row
```
- Background: White
- Foreground: #2c3e50
- Font: Segoe UI, 11pt
- Height: 36px
- Border Bottom: 1px solid #f0f0f0
- Hover: Background #fafafa
- Selected: Background #e3f2fd
```

### Alternating Row
```
- Background: #fafafa
- All other properties same as Row
```

---

## Animations

### Page Transition
```
- Type: Fade
- Duration: 0.3 seconds
- Easing: EaseInOut
- Opacity: 0 → 1
```

### Button Hover
```
- Type: Background color change
- Duration: 0.2 seconds
- Easing: EaseOut
```

### Card Hover
```
- Type: Shadow + Transform
- Duration: 0.2 seconds
- Shadow: 0 2px 4px → 0 4px 8px rgba(0,0,0,0.15)
- Transform: TranslateY(0) → TranslateY(-2px)
```

---

## Responsive Behavior

### Minimum Window Size
- Width: 1024px
- Height: 768px

### Sidebar Collapse
- When window < 1200px: Sidebar collapses to 60px width, showing only icons
- Hover on collapsed sidebar: Expands to full width temporarily

### Stats Cards
- On small windows: Wraps to 2 rows (2 cards per row)
- Card size adjusts proportionally

### Tables
- Horizontal scroll appears when content overflows
- Column widths adjust proportionally
- Minimum column width: 80px

---

## File Structure

```
scip_overlay_v2/
├── Core/
│   ├── Common/
│   │   └── RelayCommand.cs
│   ├── Data/
│   │   ├── LocalStorageManager.cs
│   │   └── MarketDataService.cs
│   ├── Models/
│   │   ├── DomainModels.cs (Commodity, Route, Ship, Quest, etc.)
│   │   └── UserSettings.cs
│   └── DashboardState.cs
├── ScipDashboard.WPF/
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
│   ├── Styles.xaml (Shared styles)
│   ├── MainViewModel.cs
│   └── Modules/ (Module views)
└── launch.bat
```

---

## Implementation Priority

### Phase 1: Core Shell (COMPLETE)
- ✅ Project structure
- ✅ Window shell with custom controls
- ✅ Sidebar navigation
- ✅ Stats cards

### Phase 2: Dashboard Module (NEXT)
- Dashboard view with recent activity
- Quick actions
- Stats cards with real data

### Phase 3: Market Module
- Commodity table with search/filter
- Price history charts
- Watchlist functionality

### Phase 4: Hauling & Routes
- Route builder
- Route management
- Haul tracking

### Phase 5: Fleet Module
- Ship directory
- My fleet management
- Ship details

### Phase 6: StarQuester
- Quest capture
- Quest tracking
- Reputation tracking

### Phase 7: Settings
- Theme settings
- Data sources
- Storage management

---

## Color Usage Guide

### Primary Colors
- **Safety Orange (#FFC107)**: Primary actions, highlights, active states
- **Deep Charcoal (#2c3e50)**: Sidebar, primary text, headers
- **Navy (#1a252f)**: Sidebar accent
- **White (#ffffff)**: Backgrounds, cards
- **Light Gray (#fafafa)**: Page background

### Semantic Colors
- **Success**: Green (#4caf50) - Completed hauls, positive margins
- **Warning**: Orange (#FFC107) - In progress, attention needed
- **Error**: Red (#f44336) - Failed, errors, negative values
- **Info**: Blue (#2196f3) - Information, neutral states

### Gradients
- Sidebar: Linear gradient from #1a252f to #2c3e50
- Cards: Subtle white to #f5f5f5 gradient

---

## Next Steps

1. **Implement the redesigned MainWindow.xaml** based on this layout plan
2. **Create the Dashboard module** with stats cards and recent activity
3. **Add navigation functionality** between modules
4. **Connect data sources** to populate UI elements
5. **Test and refine** each module

This layout plan captures the "game launcher" aesthetic with high data density and visual clarity that you described.
