# SCIP Dashboard - Unified Build Plan (scip_overlay_v2)

**Project:** SCIP Dashboard (scip_overlay_v2)  
**Target release:** `1.0.0` stable  
**Platform:** Windows desktop application (`.exe`)  
**Product direction:** A native C# WPF dashboard for Star Citizen logistics, inspired by the SC-Truckers web project but designed as a high-performance, local-first desktop application.

---

## Preview: The "Game Launcher" Aesthetic

The dashboard is designed to feel like a high-end game launcher, emphasizing "Data Density" and "Visual Clarity."

### Visual Style
- **Color Palette**: Deep charcoal and navy sidebar (#2c3e50) with vibrant "Safety Orange" (#FFC107) for primary actions and highlights.
- **Layout**: A persistent left-hand sidebar for navigation, with a large main content area for data-heavy modules.
- **Stats Grid**: The top of the dashboard features four "Quick Look" cards (Active Hauls, Routes, Fleet, Cargo) using high-contrast colors to make vital information instantly readable.
- **Transitions**: Smooth, brief fade-in animations when switching between modules to provide a polished "desktop app" feel.

---

## 1. Mission and Non-Negotiable Rules
...
Build the application in small, verifiable phases. Complete and test each phase before starting the next. The goal is a polished, professional dashboard, not a terminal-driven utility.

### Required Experience
- A Windows `.exe` that opens a graphical dashboard, like a modern game launcher.
- Persistent side navigation with animated tab transitions.
- Shared visual design, reusable components, and consistent navigation.
- Local application files and local persistence for user-created data and settings.
- **Market Overview**: Commodities, buy/sell opportunities, and graphs.
- **Hauling**: Market-informed suggested routes, single-route builder, and multi-route builder.
- **Fleet**: Searchable directory of every known Star Citizen ship, with ship data kept updateable.
- **StarQuester**: A new feature to record/scan quests and get structured guidance for faction-reputation progress.
- **Fallback Path**: A visible fallback for when data sources, parsers, or features fail.

### Hard Constraints
- **No terminal-based user workflow.** Users should not need a terminal to use the application.
- **Incremental builds.** Do not build every feature at once.
- **Audit first.** Do not duplicate existing website functionality without auditing it.
- **No fake "live" data.** Do not pretend prices or rewards are live/verified when they are not.
- **Local-first.** The app should not depend on a website being reachable for basic navigation or saved local data.
- **No maps required.** Route information is shown via clear tables, stop lists, and status indicators.
- **Data Density.** Keep the interface compact with sortable tables and dynamic charts.

---

## 2. Product Architecture

### Suggested Layers
1. **Desktop Shell**: Window lifecycle, app menu, packaged assets, update/error messaging.
2. **UI Shell**: Sidebar, header, page transitions, notifications, global search, settings.
3. **Feature Modules**: Dashboard, Market, Hauling, Routes, Scrap, Fleet, Crew, Cargo Bay, StarQuester, Settings.
4. **Domain/Data Layer**: Normalized models, validation, import/export, cache, data-source adapters.
5. **Local Storage**: User preferences, saved routes, fleet selections, watchlists, quest records.
6. **External Data Adapters**: Optional sources for commodity prices, ship data, quest information, and reputation references.

### Local Data & Security
- Store writable user data in the OS application-data directory (not the read-only app folder).
- Keep packaged application files separate from user-created files.
- Validate imported files; never execute imported content as code.
- Do not store secrets (API keys, etc.) directly in the frontend code.
- Provide a clear offline state and explain which features need an internet connection.

---

## 3. Navigation and Page Map

Keep the sidebar visible while the content panel changes. Use short fade/slide transitions.

| Sidebar item | Main purpose | Phase |
|---|---|---:|
| Dashboard | Summary cards, watchlist changes, saved routes, fleet status | 1 |
| Market Overview | Commodity prices, buy/sell comparisons, graphs, watchlists | 3 |
| Hauling | Market-informed route suggestions and hauling overview | 4 |
| Route Builder | Build and save one route | 4 |
| Multi-Route Builder | Plan and compare several routes | 4 |
| Single Run | Follow one selected route/haul at a time | 4 |
| Scrap | Scrap-related information and workflow | 6 |
| Fleet | Searchable directory of ships and user fleet selections | 5 |
| Crew | Crew information and assignments | 6 |
| Cargo Bay | Cargo inventory/planning | 6 |
| StarQuester | Quest capture, review, and faction-reputation guidance | 7 |
| Settings | Theme, data sources, local data, import/export, diagnostics | 2 |

---

## 4. Phased Build Plan

### Phase 0 — Audit and Freeze (Baseline)
- Review existing SC-Truckers site and inventory features.
- Review current project files and identify framework/data sources.
- Record current working baseline.
- **Exit Gate**: Feature matrix and data-source inventory complete.

### Phase 1 — Core Shell & Navigation (CURRENT)
- [x] Initialize project structure (Modules, Core folders).
- [x] Establish base directory structure.
- [x] Implement `DashboardState` for global state management.
- [x] Implement `MainViewModel` for navigation logic.
- [x] Create primary `MainWindow` shell.
- [x] Create MarketModuleView placeholder.
- [x] Implement Navigation UI and ViewSelector.
- [x] Wire sidebar buttons to navigation commands.
- [x] Implement dynamic module switching via `ContentControl`.
- **Exit Gate**: Packaged shell launches repeatedly and navigation is stable.


### Phase 2 — Local Data Foundation & Settings
- [x] Define shared data models (Commodities, Routes, Ships, Quests).
- [x] Define versioned local storage and migration strategy.
- [x] Implement Settings screens (Theme, Source Status, Cache).
- [x] Add source adapter interface for live, cached, and sample data.
- [x] **Exit Gate**: Data layer is testable independently of UI.

### Phase 3 — Market Overview
- [x] Build commodity table with search, sorting, and filtering.
- [x] Implement price-history graph with selectable time ranges.
- [x] Calculate spread and margins (with clear assumptions).
- [x] Add watchlist and favorites.
- [x] **Exit Gate**: Usable with both sample data and first real data source.

### Phase 4 — Hauling and Route Tools
- [x] **Market-informed Hauling**: Generate buy-at/sell-at opportunities.
- [x] **Single Route Builder**: CRUD for routes with stop reordering.
- [x] **Multi-Route Builder**: Compare multiple plans in a sortable table.
- [x] **Single Run**: View active stops, progress, and checklist.
- [x] **Exit Gate**: User can go from market comparison to tracking a single haul.

### Phase 5 — Fleet Directory
- [x] Build searchable/filterable directory of the Star Citizen ship catalog.
- [x] Add Ship Detail panels with provenance/source info.
- [x] Add "My Fleet" selection layer (distinct from game catalog).
- [x] **Exit Gate**: Supports full catalog supplied by verified source.

### Phase 6 — Migration of Existing Sections
- [x] Migrate Scrap, Crew, and Cargo Bay features into the new shell.
- [x] Wire to shared data layer.
- [x] Regression test core modules.
- [x] **Exit Gate**: Migrated sections work within the unified shell.

### Phase 7 — StarQuester
- [x] Implement Quest Capture (Paste/OCR/Manual entry).
- [x] Parse quest details into normalized records.
- [x] Match quests to faction-reputation goals.
- [x] Show evidence-based guidance and progress tracking.
- [x] **Exit Gate**: Players can capture, confirm, and track quest progress.

### Phase 8 — Dashboard Summary Integration
- [x] Link cards to their respective modules.
- [x] Pull real-time data into summary widgets (Watchlists, Active Routes).
- [x] Add "New User" empty states and guidance.
- [x] **Exit Gate**: Dashboard provides a cohesive high-level overview.

### Phase 9 — Visual Polish & Accessibility
- [x] Apply sci-fi inspired styling consistently.
- [x] Implement loading skeletons and progress feedback.
- [x] Ensure keyboard navigation and high contrast.
- [x] Add diagnostics and error boundaries.
- [x] **Exit Gate**: Application meets "Game Launcher" quality standards.

### Phase 10 — Release Candidate & Stable 1.0.0
- [x] Finalize feature list; move new ideas to backlog.
- [x] Unit/Integration/E2E testing.
- [x] Test clean install, update, and uninstall.
- [x] Build release package from tagged source.
- [x] **Exit Gate**: Stable `.exe` published with release notes.

---

## 5. Data Strategy & Fallbacks

Use the following ladder for any data point:
1. Verified, permitted source (Live).
2. Documented second source (Backup).
3. User-imported structured data.
4. Manual entry and local saved data.
5. Labeled sample data (Development/Demo only).

---

## 6. Development Workflow

For every feature/bug:
1. **Specify**: Define behavior and acceptance criteria.
2. **Locate**: Identify files and data models.
3. **Reuse**: Check for existing components.
4. **Smallest Slice**: Build one interaction/data flow at a time.
5. **Test Locally**: Test normal, empty, invalid, and failure cases.
6. **Regression Check**: Verify adjacent features.
7. **Review**: Check errors, accessibility, and wording.
8. **Record**: Update project checklist.
9. **Commit**: Small version-control commit.
10. **Gate**: Only move to next task once current is DONE.
