# SC-Truckers Desktop Dashboard — Phased Build Plan

**Project:** SC-Truckers Desktop  
**Target release:** `1.0.0` stable  
**Platform:** Windows desktop application (`.exe`)  
**Product direction:** Turn the existing SC-Truckers web project into a desktop-style, Star Citizen-inspired logistics dashboard that runs locally from its own application folder.  
**Primary references:**
- Main site: https://sc-truckers.lovable.app/
- Faction Reputation reference: https://sc-truckers.lovable.app/reputation

---

## 1. Mission and non-negotiable rules

Build the application in small, verifiable phases. Complete and test each phase before starting the next. The goal is a polished dashboard, not a terminal-driven utility.

### Required experience

- A Windows `.exe` that opens a graphical dashboard, like a modern game launcher.
- Persistent side navigation with animated tab transitions.
- Shared visual design, reusable components, and consistent navigation across all sections.
- Local application files and local persistence for user-created data and settings.
- Market Overview with commodities, buy/sell opportunities, and graphs.
- Hauling with market-informed suggested routes, a single-route builder, a multi-route builder, and a single-run route view.
- Fleet directory covering every known Star Citizen ship, with ship data kept updateable.
- **StarQuester**, a new feature that lets a player record or scan a quest and get structured guidance for faction-reputation progress.
- A visible fallback path when a data source, parser, or feature does not work as expected.

### Hard constraints

- **No terminal-based user workflow.** Users should not need to open Command Prompt, PowerShell, or a terminal to use the application.
- Do not build every feature at once.
- Do not duplicate existing website functionality without first auditing it.
- Do not pretend market prices, ship data, quest details, or reputation rewards are live or verified when they are not.
- Do not make the app depend on a website being reachable for basic navigation or saved local data.
- No maps are required for the first release. Route information can be shown with clear tables, stop lists, route cards, and status indicators.
- Keep the interface compact and data-dense, with sortable tables, dropdowns, dynamic charts, and a persistent sidebar.

---

## 2. Product architecture

### Recommended implementation path

Use the existing web application's frontend and convert it into a desktop shell with **Electron** if the current project is web-based and can be reused cleanly. Electron packages the UI and application code into a Windows desktop application and supports a normal installer or portable distribution.

**Alternative path if Electron is unsuitable:** evaluate **Tauri** only after a short technical spike. Choose one desktop framework and commit to it for the first release. Do not build parallel Electron and Tauri implementations.

The desktop shell must not require users to run commands. Developers may use build tools during development, but the shipped app must open by double-clicking its `.exe` or installed shortcut.

### Suggested layers

1. **Desktop shell:** window lifecycle, app menu, packaged assets, update/error messaging.
2. **UI shell:** sidebar, header, page transitions, notifications, global search, settings.
3. **Feature modules:** Dashboard, Market, Hauling, Routes, Scrap, Fleet, Crew, Cargo Bay, StarQuester, Settings.
4. **Domain/data layer:** normalized models, validation, import/export, cache, data-source adapters.
5. **Local storage:** user preferences, saved routes, fleet selections, watchlists, quest records, and cached reference data.
6. **External data adapters:** optional sources for commodity prices, ship data, quest information, and reputation references.

Keep each feature module independent. A failure in one module should not prevent the dashboard or other tabs from opening.

### Local data and security

- Store writable user data in the operating system's application-data location, not inside the read-only installed application directory.
- Keep packaged application files separate from user-created files.
- Validate imported files and never execute imported content as code.
- Do not scrape or automate a game client unless the data source and game rules permit it.
- Use documented or permitted data sources where available. Record the source and last-updated time for externally sourced data.
- Provide a clear offline state and explain which features need an internet connection.
- Do not store secrets or API keys directly in frontend code.

---

## 3. Navigation and page map

Keep the sidebar visible while the content panel changes. Use a short fade/slide transition, keyboard focus management, and a clear selected-tab state. Avoid excessive animation that slows navigation.

| Sidebar item | Main purpose | Phase |
|---|---|---:|
| Dashboard | Summary cards, watchlist changes, saved routes, fleet status, recent activity | 1 |
| Market Overview | Commodity prices, buy/sell comparisons, graphs, watchlists | 3 |
| Hauling | Market-informed route suggestions and hauling overview | 4 |
| Route Builder | Build and save one route | 4 |
| Multi-Route Builder | Plan and compare several routes | 4 |
| Single Run | Follow one selected route/haul at a time | 4 |
| Scrap | Scrap-related information and workflow from the existing site | 6 |
| Fleet | Searchable directory of Star Citizen ships and user fleet selections | 5 |
| Crew | Crew information and assignments if supported by the current site | 6 |
| Cargo Bay | Cargo inventory/planning if supported by the current site | 6 |
| StarQuester | Quest capture, quest review, faction-reputation guidance | 7 |
| Settings | Theme, data sources, local data, import/export, diagnostics | 2 |

**Naming rule:** audit the existing site before renaming or removing anything. Preserve familiar terminology where it still fits. “Garage” becomes **Fleet**.

---

## 4. Phases and completion gates

Each phase has a scope, implementation steps, a test checklist, and a gate. A phase is not complete merely because the screen looks correct; its key interactions must work.

## Phase 0 — Audit and freeze the baseline

**Goal:** understand what already exists before writing replacement code.

### Steps

1. Review the existing SC-Truckers site and inventory every page, feature, component, and interaction.
2. Review the current project files and identify framework, package manager, routing, chart library, data sources, and existing persistence.
3. Record which features work, partly work, or are placeholders.
4. Capture the existing page names, terms, and expected behavior.
5. Create a feature matrix with columns: `Feature`, `Existing implementation`, `Reusable?`, `Missing work`, `Acceptance test`.
6. Identify any existing APIs or data feeds and confirm their terms, reliability, and access requirements.
7. Record the current working baseline in version control before modifying it.

### Exit gate

- [ ] All current pages and important interactions are inventoried.
- [ ] Reusable code is identified.
- [ ] Data-source limitations are documented.
- [ ] No implementation begins based only on assumptions about the site.

**Fallback:** if the source project is unavailable, create a small, explicit interface specification from the visible site and rebuild only the minimum shell first. Do not claim that the original code was reused.

---

## Phase 1 — Desktop shell and navigation

**Goal:** get a stable `.exe` with a working shell before implementing complex features.

### Steps

1. Choose Electron or, after a documented spike, Tauri.
2. Create the desktop application entry point and package the existing UI.
3. Set application name, version, window size, minimum window size, and app icon.
4. Create the persistent sidebar and header.
5. Add page routing for every planned section, using placeholder screens where features are not built yet.
6. Add consistent active-tab styling and modest page transition animations.
7. Create shared UI components: cards, tables, search fields, dropdowns, modal/dialog, toast/notification, loading state, empty state, error state.
8. Add global error boundaries so a broken feature does not crash the whole app.
9. Add a visible application version and a simple About panel.
10. Build a test package and launch it by double-clicking it on a clean Windows user profile.

### Acceptance tests

- [ ] App launches without a terminal.
- [ ] Every sidebar item opens the correct placeholder page.
- [ ] Sidebar selection and page transitions are consistent.
- [ ] Resizing the window does not break navigation.
- [ ] A simulated page error does not crash the whole app.
- [ ] The app opens when the network is unavailable.
- [ ] No required runtime files are missing from the packaged build.

### Exit gate

Do not proceed until the packaged shell launches repeatedly and navigation is stable.

**Fallback:** if packaging the whole current frontend fails, first package a minimal standalone shell and migrate the existing pages into it one by one. Do not rewrite all features to solve a packaging problem.

---

## Phase 2 — Local data foundation and Settings

**Goal:** establish reliable storage and data-source status before features depend on them.

### Steps

1. Define shared data models for commodities, price observations, locations, routes, route stops, ships, quest records, faction records, and user settings.
2. Define versioned local storage and a migration strategy.
3. Store user data in the OS application-data directory.
4. Add Settings screens for theme, data-source status, cache controls, and import/export.
5. Add a source adapter interface so live, cached, and sample data can be switched without rewriting UI components.
6. Add source labels such as `Live`, `Cached`, `Sample`, and `Unavailable`.
7. Add timestamps and source attribution wherever external data is displayed.
8. Add a small sample dataset for development and demos, clearly labeled as sample data.
9. Add backup/export and import validation.
10. Test corrupt, missing, outdated, and incompatible local data.

### Acceptance tests

- [ ] Settings persist after closing and reopening the app.
- [ ] Saved user data survives app updates.
- [ ] Import rejects invalid files with a useful message.
- [ ] A failed external source falls back to cached or sample data without crashing.
- [ ] Sample values are never labeled as live.
- [ ] Data migration failure preserves a recoverable backup where possible.

### Exit gate

The data layer must be testable independently of any individual page.

**Fallback:** start with a small local database or structured local file storage appropriate to the chosen framework. Avoid introducing a remote backend unless a demonstrated requirement demands it.

---

## Phase 3 — Market Overview

**Goal:** deliver the first complete data-driven feature.

### Required UI

- Commodity search and category filters.
- Buy and sell price columns.
- Location/market comparison.
- Estimated spread and potential margin, with assumptions shown.
- Price-history graph when historical observations exist.
- Watchlist and favorites.
- Data-source status and last-updated time.
- Sortable table, empty state, loading state, and unavailable-data state.

### Steps

1. Finalize the commodity and price-observation models.
2. Build the commodity table using local sample data.
3. Implement search, sorting, filtering, and watchlist selection.
4. Implement the price-history graph with a selectable commodity and time range.
5. Calculate spread only when comparable buy and sell prices exist.
6. Clearly separate gross spread from estimated profit; include known fees, cargo quantity, and other costs only when reliable inputs exist.
7. Add a source adapter for the first verified market data source.
8. Add caching and a manual refresh action.
9. Add stale-data warnings and graceful handling of missing history.
10. Test with empty, incomplete, duplicate, stale, and unusually large datasets.

### Acceptance tests

- [ ] Users can find, sort, and filter commodities.
- [ ] Buy and sell values show their source and update time.
- [ ] Graphs use actual stored observations, not fabricated history.
- [ ] Missing history displays a helpful empty state.
- [ ] Margin calculations show their assumptions and do not imply guaranteed profit.
- [ ] Market data failure does not break other pages.
- [ ] Watchlists persist after restart.

### Exit gate

Market Overview is usable with both sample data and the first supported real data source.

**Fallback:** if no dependable live market feed is available, ship manual/imported price entry and clearly labeled cached data. Do not block the entire application waiting for an unreliable source.

---

## Phase 4 — Hauling and route tools

**Goal:** use available market data to suggest routes, while allowing users to build and run their own plans.

### Feature A: Market-informed Hauling

1. Read available commodity prices and location data through the shared data layer.
2. Generate candidate buy-at/sell-at opportunities from comparable, sufficiently fresh data.
3. Estimate cargo value and gross spread.
4. Apply configurable cargo capacity and user-selected constraints where supported.
5. Rank suggestions by a clearly named metric, such as estimated gross margin or margin per unit of cargo.
6. Explain why each route was suggested and show data freshness.
7. Exclude or flag routes with missing, stale, or incomparable prices.

### Feature B: Single Route Builder

1. Let the user create a route and give it a name.
2. Add, remove, and reorder stops.
3. Select locations and optional commodities.
4. Display each stop's buy/sell assumptions and route totals.
5. Validate missing locations, missing prices, and impossible or incomplete stop sequences.
6. Save, duplicate, edit, and delete routes.
7. Allow export/import of saved route plans.

### Feature C: Multi-Route Builder

1. Let the user select multiple candidate routes or saved routes.
2. Compare routes in a sortable table.
3. Show total stops, estimated cargo value, gross spread, known costs, and data confidence where available.
4. Warn about shared constraints or incompatible assumptions.
5. Allow the user to save a multi-route plan.
6. Do not automatically optimize for unknown travel times, fuel use, or risk. Add these metrics only when dependable inputs exist.

### Feature D: Single Run

1. Let the user select one saved route.
2. Show the active stop, next stop, cargo/commodity checklist, and route progress.
3. Provide manual actions such as `Mark stop complete`, `Skip stop`, and `Reset run`.
4. Save progress locally.
5. Show a confirmation before resetting a run.
6. Keep Single Run useful without live game telemetry; never imply the app detects in-game movement unless that integration is implemented and verified.

### Acceptance tests

- [ ] Suggestions use available price and location data and explain their ranking.
- [ ] Users can create, edit, reorder, save, duplicate, and delete a single route.
- [ ] Multi-Route Builder compares at least two plans without mixing units or assumptions.
- [ ] Single Run progress survives app restart.
- [ ] Missing or stale data is visibly flagged.
- [ ] Invalid routes cannot silently produce misleading totals.
- [ ] No map is required; route stops and status are legible in tables/cards.

### Exit gate

A user can go from market comparison to a saved route and track a single run without leaving the app.

**Fallback:** if automatic route generation lacks reliable data, keep the builder fully functional and provide transparent, rule-based suggestions from available prices. Do not label a manually created route as automatically optimized.

---

## Phase 5 — Fleet directory

**Goal:** provide a searchable reference for every known Star Citizen ship, plus the user's own fleet selections.

### Data strategy

The ship directory should be designed to support the full known ship catalog, including future additions and variants where the source distinguishes them. Avoid hardcoding the catalog into UI components. Use a normalized dataset with source attribution and an update process.

### Steps

1. Define the ship model: stable ID, display name, manufacturer, ship role/type, variant, capacity fields where verified, availability/status where sourced, source URL, and last-updated timestamp.
2. Identify a reliable, permitted source for the ship catalog and verify its coverage and reuse terms.
3. Import a baseline dataset and validate duplicates, missing IDs, and variant handling.
4. Build a searchable, filterable directory with manufacturer, role/type, and other verified attributes.
5. Add a ship detail panel with provenance for each sourced field.
6. Add a separate `My Fleet` selection layer so owned/desired ships are not confused with the complete game catalog.
7. Add user-defined status and notes, if desired.
8. Add a repeatable catalog update/import process.
9. Show `Unknown` rather than inventing values for unverified capacity or status.
10. Test the directory with a large dataset and future ship additions.

### Acceptance tests

- [ ] The directory is data-driven and does not require UI code changes for every new ship.
- [ ] Search and filters work across the full imported catalog.
- [ ] Variants are represented consistently with the chosen source.
- [ ] Each record identifies its source and update time.
- [ ] User fleet selections remain distinct from the full ship catalog.
- [ ] Missing fields are marked unknown, not guessed.
- [ ] Updating the catalog does not erase user notes or fleet selections.

### Exit gate

The app supports the full catalog supplied by the verified source and can update it without a code rewrite.

**Fallback:** if no single source provides complete coverage, create a source adapter and an import format for a maintained catalog. Clearly disclose coverage gaps; do not promise “every ship” until the dataset has been checked against the selected source.

---

## Phase 6 — Migrate remaining existing sections

**Goal:** bring across the site's existing logistics features without disrupting completed modules.

### Steps

1. Audit the current Scrap feature and map its existing behavior.
2. Migrate Scrap into the desktop shell, reusing compatible components and models.
3. Audit and migrate Crew only to the extent that the current site supports it.
4. Audit and migrate Cargo Bay using shared cargo/commodity models where appropriate.
5. Confirm whether any Dashboard widgets need data from these sections.
6. Wire each migrated section to the shared data layer.
7. Add page-specific loading, empty, error, and offline states.
8. Regression-test Market, Hauling, Route Builder, Multi-Route Builder, Single Run, and Fleet after each migration.

### Acceptance tests

- [ ] Existing supported behavior is preserved or its change is documented.
- [ ] Sections share consistent styling and navigation.
- [ ] No module maintains a conflicting duplicate copy of shared data.
- [ ] A failure in Scrap, Crew, or Cargo Bay does not break the app shell.

### Exit gate

All sections selected for the first release work within the desktop shell. Defer unsupported or low-value features rather than creating fake functionality.

---

## Phase 7 — StarQuester: quest capture and faction reputation

**Goal:** introduce a new workflow that turns a player's captured quest details into organized, evidence-based faction-reputation guidance.

### Important scope boundary

“Scan a quest” should initially mean **user-provided capture**: paste quest text, enter details, or import a screenshot/image for review if image/OCR support is implemented. Do not assume the app can read the live game client or automatically inspect game memory. Add deeper integrations only if they are permitted, technically supported, and separately tested.

### Main workflow

1. User opens StarQuester and chooses `Add Quest`.
2. User pastes quest text, enters it manually, or selects a supported screenshot/image input.
3. If OCR is available, extract candidate text and show it for correction before analysis.
4. Parse the quest into editable fields: title, description/objective, quest giver, location, faction references, stated rewards, and source/evidence.
5. Show the parsed result and ask the user to confirm or correct uncertain fields.
6. Match the quest to a local, versioned reference catalog of faction and reputation information.
7. Show relevant reputation goals, known quest requirements, possible next steps, and prerequisite information only when supported by the catalog.
8. Explain why each recommendation appears and distinguish verified information from inferred matches.
9. Let the player save the quest, mark steps complete, and record reputation progress manually.
10. Update the player's local StarQuester journal and show the next suggested task.
11. Provide links to the relevant source material, including the existing SC-Truckers faction reputation page: https://sc-truckers.lovable.app/reputation
12. Allow the user to correct a bad match and report unsupported quest text.

### StarQuester data model

- **Quest record:** ID, title, raw input, normalized text, objectives, location, faction references, captured date, user corrections.
- **Faction record:** ID, name, aliases, reputation goals, source references, last verified date.
- **Quest-reputation link:** quest ID, faction ID, requirement/reward evidence, confidence/status, source reference.
- **Player progress:** selected faction goal, completed tasks, manually entered progress, notes, last updated.
- **Source record:** URL/title, retrieval date, version or patch context, verification status.

### Acceptance tests

- [ ] Users can add quest information without OCR.
- [ ] OCR output, if supported, is editable before it is used.
- [ ] Uncertain quest matches are presented as suggestions, not facts.
- [ ] Reputation advice includes source/evidence and distinguishes verified from inferred data.
- [ ] Unknown quests receive a useful “no confident match” state.
- [ ] Users can correct a match and save progress.
- [ ] Saved quests and progress persist after restart.
- [ ] StarQuester remains usable if external reference pages are offline.
- [ ] No automatic in-game reading is claimed unless implemented, permitted, and verified.

### Exit gate

A player can capture a quest, confirm its details, see appropriately qualified faction-reputation guidance, and track progress locally.

**Fallback:** if OCR is unreliable or expensive, ship paste/manual entry first. If quest-to-reputation matching is incomplete, provide search across the reference catalog and clearly marked related entries rather than fabricating a match.

---

## Phase 8 — Dashboard summary and cross-feature integration

**Goal:** make the home screen a useful overview rather than a decorative landing page.

### Steps

1. Define the dashboard cards using real data from completed modules.
2. Add market watchlist changes and selected commodity graph.
3. Add saved routes and the last active Single Run.
4. Add My Fleet summary and user-defined status.
5. Add StarQuester's active reputation goal and next saved task.
6. Add recent activity only for events actually recorded by the app.
7. Allow cards to open their source section.
8. Add empty states for first-time users and a way to hide/reorder optional cards if feasible.
9. Ensure summary values are derived from the same domain layer as their detail pages.

### Acceptance tests

- [ ] Dashboard cards reflect the current saved data.
- [ ] Clicking a card opens the relevant section.
- [ ] No fabricated activity or price movement appears.
- [ ] A new user sees clear next steps instead of blank widgets.

---

## Phase 9 — Visual polish, accessibility, and reliability

**Goal:** make the app feel like a cohesive desktop product.

### Steps

1. Apply the chosen Star Citizen-inspired visual system consistently.
2. Use restrained sci-fi styling: clear typography, high contrast, compact panels, subtle borders, and purposeful accent colors.
3. Keep charts and tables readable at the minimum supported window size.
4. Add loading skeletons or progress feedback for longer operations.
5. Respect reduced-motion preferences where practical.
6. Ensure keyboard navigation, visible focus, and accessible labels.
7. Add clear confirmations for destructive actions.
8. Add diagnostics that users can copy/export without using a terminal.
9. Test offline, slow-network, and failed-source behavior.
10. Profile large tables, chart updates, and navigation animations.

### Acceptance tests

- [ ] Core workflows are usable by keyboard.
- [ ] Animation does not block or delay input.
- [ ] Text, table columns, and charts remain readable at supported sizes.
- [ ] Errors explain the issue and the next safe action.
- [ ] Logs avoid exposing secrets or unnecessary personal data.

---

## Phase 10 — Release candidate and stable `1.0.0`

**Goal:** produce a reproducible, tested Windows release.

### Steps

1. Freeze the feature list; defer new ideas to a post-1.0 backlog.
2. Run unit tests for calculations, parsers, validation, and data migrations.
3. Run integration tests for storage and data-source adapters.
4. Run end-to-end tests for the most important user journeys.
5. Test a clean install, first launch, update/upgrade, uninstall, and reinstall.
6. Test a non-admin Windows user account and a path containing spaces.
7. Test without internet access and with unavailable data sources.
8. Verify app icon, product name, version, file metadata, and installer behavior.
9. Confirm license/attribution requirements for imported ship, quest, and market data.
10. Build the release package from a clean, tagged source revision.
11. Produce release notes, known limitations, and a short graphical user guide.
12. Publish the stable build only after all release blockers are closed.

### Release blockers

- App fails to launch or close cleanly.
- User data is lost during ordinary restart or upgrade.
- Navigation can crash the whole application.
- Market calculations use incomparable or fabricated values.
- Fleet data is advertised as complete without a verified coverage check.
- StarQuester presents uncertain matches as confirmed facts.
- A core workflow requires a terminal.
- Packaged builds depend on development-only files or network access for basic startup.

### Definition of done for `1.0.0`

- [ ] Windows `.exe` or installer launches the dashboard without a terminal.
- [ ] Sidebar navigation and transitions work.
- [ ] Settings and user data persist locally.
- [ ] Market Overview has a functioning table and graph.
- [ ] Hauling suggestions, single-route building, multi-route comparison, and Single Run are functional within available data limits.
- [ ] Fleet uses a maintainable catalog and shows source/coverage status.
- [ ] StarQuester supports quest capture, correction, reputation guidance, and local progress.
- [ ] Existing supported Scrap/Crew/Cargo Bay features are migrated or explicitly deferred.
- [ ] Error, empty, offline, and stale-data states are handled.
- [ ] Release tests pass and known limitations are documented.

---

## 5. End-to-end test journeys

Run these journeys after relevant phases and again before release.

### Journey A — Market to haul

1. Open Market Overview.
2. Search for a commodity.
3. Compare buy and sell values at two locations.
4. Inspect the price graph and its source timestamp.
5. Add the commodity to the watchlist.
6. Open Hauling and review market-informed suggestions.
7. Create a route from a suggestion.
8. Save the route and open it in Single Run.
9. Mark a stop complete, close the app, reopen it, and confirm progress persisted.

### Journey B — Plan multiple routes

1. Create two separate routes.
2. Open Multi-Route Builder.
3. Compare their supported metrics and assumptions.
4. Save the comparison/plan.
5. Reopen it and confirm the stops and calculations are intact.

### Journey C — Fleet

1. Open Fleet.
2. Search by ship name and filter by manufacturer or role.
3. Open a ship detail panel and inspect its source.
4. Add it to My Fleet and add a note.
5. Update/re-import the catalog.
6. Confirm the user's selection and note remain intact.

### Journey D — StarQuester

1. Open StarQuester and add quest text manually.
2. Review and correct extracted fields.
3. Confirm a suggested faction match or reject it.
4. Review evidence and next-step guidance.
5. Save the quest and update progress.
6. Restart the app and confirm the journal and progress persist.
7. Repeat with an unknown quest and confirm the app does not invent an answer.

### Journey E — Offline resilience

1. Start the app without network access.
2. Open each section.
3. Confirm local data and saved plans remain available.
4. Confirm network-dependent values are marked cached, stale, or unavailable.
5. Restore connectivity and refresh the relevant source.

---

## 6. Development workflow: prevent skipped steps

Use this checklist for **every** feature and bug fix.

1. **Specify:** write the expected behavior and acceptance criteria before coding.
2. **Locate:** identify the files/components/data models involved.
3. **Reuse:** check for an existing component or service before adding a new one.
4. **Implement the smallest slice:** build one interaction or data flow at a time.
5. **Test locally:** test normal, empty, invalid, and failure cases.
6. **Run regression checks:** verify the current feature and adjacent features still work.
7. **Review:** inspect errors, accessibility, stale data, and user-facing wording.
8. **Record:** update the phase checklist and document changed behavior.
9. **Commit:** make a small version-control commit with a specific message.
10. **Gate:** do not start the next dependent task until the acceptance criteria pass.

### Task status vocabulary

Use only these statuses in the project checklist:

- `NOT STARTED`
- `IN PROGRESS`
- `BLOCKED`
- `TESTING`
- `DONE`
- `DEFERRED`

A task marked `DONE` must have test evidence or a recorded manual verification. A task marked `BLOCKED` must name the blocker and the fallback path.

### Suggested project structure

```text
SC-Truckers-Desktop/
├─ app/
│  ├─ main/                  # Desktop lifecycle and secure IPC
│  ├─ preload/               # Explicitly exposed desktop APIs
│  └─ renderer/
│     ├─ app-shell/          # Sidebar, header, navigation, transitions
│     ├─ components/         # Shared tables, charts, dialogs, cards
│     ├─ pages/
│     │  ├─ dashboard/
│     │  ├─ market/
│     │  ├─ hauling/
│     │  ├─ routes/
│     │  ├─ fleet/
│     │  ├─ scrap/
│     │  ├─ crew/
│     │  ├─ cargo-bay/
│     │  ├─ starquester/
│     │  └─ settings/
│     └─ styles/
├─ domain/
│  ├─ models/
│  ├─ calculations/
│  ├─ validation/
│  └─ services/
├─ data/
│  ├─ adapters/
│  ├─ import-export/
│  ├─ migrations/
│  └─ sample/                # Clearly labeled demo data only
├─ tests/
│  ├─ unit/
│  ├─ integration/
│  └─ e2e/
├─ assets/
├─ docs/
│  ├─ BUILD_PLAN.md
│  ├─ DATA_SOURCES.md
│  └─ RELEASE_CHECKLIST.md
└─ README.md
```

This is a proposed structure, not a requirement to move every existing file immediately. Adapt it to the actual framework after Phase 0.

---

## 7. Data-source plan and failure alternatives

Maintain `DATA_SOURCES.md` with one row per source:

| Field | What to record |
|---|---|
| Data category | Commodity prices, ship catalog, quest text, faction reputation |
| Source | Exact URL/API/file/provider |
| Access method | Documented API, permitted import, manual entry, etc. |
| Terms | License, attribution, rate limits, reuse restrictions |
| Coverage | Which locations, ships, factions, or quests are included |
| Freshness | Update interval or last verified date |
| Failure behavior | Cache, manual input, alternative source, or unavailable state |
| Owner/status | Task owner and verification status |

### Fallback ladder

Use the first viable option in this order:

1. Verified, permitted source with a stable interface.
2. A second verified source with a documented adapter.
3. User-imported structured data.
4. Manual entry and local saved data.
5. Clearly labeled sample data for demonstration/testing only.

Never silently substitute sample data for real data. If one provider fails, isolate the failure to that adapter and keep the rest of the app functional.

---

## 8. Scope control: what not to build first

Do **not** start with these items:

- A complete automatic in-game quest scanner.
- Unverified live price feeds or guaranteed-profit calculations.
- Advanced route optimization requiring unknown travel time, fuel, or risk.
- A custom backend before local storage and data needs are understood.
- Multiple desktop frameworks in parallel.
- Rewriting the existing site before auditing what can be reused.
- Decorative animations that delay core workflows.
- A large settings system unrelated to the first release.

Put new ideas into a `Post-1.0 Backlog` and revisit them only after the stable release criteria pass.

---

## 9. Immediate next actions

Begin with Phase 0 only:

1. Obtain or open the current SC-Truckers project source.
2. Inventory every page and working interaction.
3. Inspect the current Market, Hauling, Fleet/Garage, Scrap, Crew, Cargo Bay, and faction reputation implementations.
4. Record the current framework and build process.
5. Produce the feature matrix and data-source inventory.
6. Decide whether the current frontend can be packaged directly with Electron.
7. Create a baseline commit and Phase 0 completion checklist.
8. Stop and review the audit before beginning Phase 1.

**Core rule:** finish the smallest working vertical slice, test it, and only then continue. When a path fails, document the failure, choose the next fallback in the ladder, and preserve working code rather than expanding the scope.
