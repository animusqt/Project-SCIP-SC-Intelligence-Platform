# SCIP Dashboard — Work Status

This list reflects the current dashboard implementation. Items that need game telemetry, an external service, or a verified data feed are called out instead of being presented as complete.

## Added and done

- Rebuilt the Main Kiosk as an SC-Truckers-style logistics landing page with a market hero, live/reference status, tracked-commodity and live-terminal metrics, top market board, and navigation to hauling, salvage/mining, fleet, quests, and OCR.
- Replaced the fixed-size window behavior with a resizable WPF window, minimum dimensions, and native maximize/restore controls.
- Loaded all 140 commodity rows from `Build Doc\sc_datahub_commodities_filtered.txt`, including entries whose source has no prices; included the category, reference buy/sell, and listed margin values.
- Packaged the commodity reference file with the app and removed fabricated station prices/SCU from the offline reference data. Reference rows show zero known terminal stock/demand rather than invented inventory.
- Added a 250-ship bundled snapshot from the linked SC-Truckers fleet reference; the Fleet catalog displays and searches the ship specifications from that page.
- Added a searchable Hauling location catalog with 815 asteroid/asteroid-cluster records, five cities, 103 station entries, 59 refinery service terminals, and three additional landing zones from the Star Citizen Wiki and UEXCorp catalogs; filters cover name/parent, star system, and location type. Repeated asteroid names are distinguished by a short catalog ID.
- Updated light and dark themes with SC-Truckers-inspired morning/night linear-gradient backgrounds and cards, layered panels, cyan/amber accents, and shared shadows.
- Added selectable Light and Dark Mode controls in Settings and saved the user's selected theme locally; Dark Mode is currently selected.
- Added 2px shell/card accents and soft shadows.
- Replaced the Fleet placeholder with a searchable spacecraft catalog, editable owned-ship records, cargo/location/assignment fields, and local persistence.
- Replaced the StarQuester placeholder with a local quest journal, pasted-text field extraction, completion/reward tracking, and faction summaries derived from the journal.
- Reworked Salvage/Mining to show observed market locations and support user-entered run logs with estimated gross yield and hourly value.
- Rebuilt the Market layout and added commodity/location observations, supply and demand SCU, cargo capacity, remaining stock/demand, buy/sell planning, trade history, and local trade-state persistence.
- Added staged image OCR using Windows OCR, editable scan rows, explicit reviewed-row application to the in-memory market data, and CSV export.
- Made module content width-responsive: disabled horizontal scrolling at screen level and replaced fixed-width Fleet, Market, Hauling, and Salvage table columns with proportional sizing so tables stay within the dashboard panel.
- Corrected hauling route quantities to use buy-side stock and sell-side demand, and excluded routes that have no observed stock or demand.
- Switched live market refresh to UEXCorp's bulk commodity-price and terminal endpoints, corrected player buy/sell price direction, and joined terminal IDs to station and system names.
- Persisted the merged SC DataHub baseline, complete UEX commodity catalog, terminal observations, and reviewed OCR/direct-entry observations in `%LocalAppData%\SCIP Dashboard\market-data.json`; atomic replacement protects the previous cache if a write is interrupted. Settings-based manual quote definitions remain in the local settings store.
- Added UEX average buy/sell prices, supply, demand, and average index to commodity details; exposed the index in Hauling as a tie-breaker after observed route profit. Averages do not fabricate station inventory or route eligibility.
- Fixed Hauling route ranking to evaluate each eligible terminal buy/sell pair rather than selecting the cheapest buy terminal before checking profitability; quantities and net profit now account for cash, cargo SCU, buy stock, and sell demand. The builder shows total buy cost, sell revenue, and net profit.
- Restricted auto-loop legs to terminal/orbit locations, capped them at 2–4 legs, and require the last stop to return to the starting terminal/orbit. Manually built routes can only be saved as loops when connected and closed.
- Verified Hauling with isolated synthetic quotes: best-pair selection respected stock limits, the builder preview showed spend/revenue/net profit for executable SCU, and a profitable two-leg loop returned to its starting terminal.
- Connected Hauling refresh to live market refresh; station, commodity, destination, best-route, and loop options now require observed terminal stock/demand rather than location-free reference prices.
- Fixed hauling selector labels so commodity choices render their actual names and spread details instead of literal interpolation placeholders.
- Added the V1-style in-game scanning flow: F8 selects and saves a screen region, F9 captures it from the desktop, Windows OCR stages editable rows, and temporary screenshots are cleaned up with a visible warning if deletion fails.
- Kept live UEX data, SC DataHub's bundled reference catalog, and SC-Truckers' shared UEX feed distinct; no undocumented SC-Truckers API is scraped.
- Updated local data persistence errors in the feature workflows to be surfaced in their status messages.
- Kept UEXCorp refresh failure visible and fall back to local market data rather than presenting fallback data as live.
- Reworked the landing market panel as Verse Market News, using current observed terminal spreads for its brief and linking to official Spectrum instead of inventing live headlines.
- Added locally persisted 15-minute live-price snapshots, 24-hour movement, and sparklines; the commodity boards and cargo planner span the same full content width as the Market overview panels.
- Expanded Fleet cards with pledge/AUEC, crew, max-box, auto-load, role/loadout, and RSI spec-link fields; unavailable catalog values are labeled rather than fabricated.
- Added salvage/mining planning selectors for hull, hazard, and target, with cargo-trip estimates and explicitly approximate component/material references.
- Added failed quest status/actions and per-quest journal reputation points that contribute to faction totals, alongside active/completed/failed counts.
- Set dropdown menus to white backgrounds with black text across themes for readable selections.
- Added Discord OAuth sign-in through Supabase PKCE and a locally persisted PayPal donation-link setting, enabled only for HTTPS PayPal URLs.
- Added OCR station/system context and Buy-tab versus Local Value modes; scanned rows show their mode, remain editable, preserve the unscanned market side, and require a station before applying generic OCR imports.
- Reworked game OCR to associate terminal text with commodity rows, read `price/SCU` separately from labeled `SHOP QUANTITY`, capture accepted cargo sizes (1/2/4/8/16/24/32 SCU) and visible loading-time estimates, and avoid treating Local Value's player-cargo count as station demand.
- Added supported cargo sizes and optional estimated loading seconds to market observations, exposed them in expanded commodity/station details, and populated box-size data from UEX API 2.0's `container_sizes` field.
- Connected the Salvage/Mining page to its view model; the previous XAML-only screen had no code-behind/data context, which left all bound controls empty.
- Made the hauling builder spell out the BUY AT → CARRY → SELL AT sequence, include the buy terminal in commodity choices, and advance to the next stop when adding a route leg.
- Replaced the open-ended greedy auto-loop builder with a maximum of four profitable, closed loops, each containing two to four legs and returning to its start station; changed loop buttons to theme-aware colors.
- Changed OCR system selection to a Stanton/Nyx/Pyro dropdown and added the same choices to editable scan rows.
- Updated the hauling "What to carry" list to show all commodities with observed buy stock, even when no profitable sell quote is currently available, and added a clear no-stock message for empty stations.
- Added a Settings catalog override for exact hidden UEX commodity names; matched live prices/stock can be included on the next refresh without inventing route data.
- Added editable manual commodity quotes in Settings with commodity, buy/sell prices, both terminal locations, and observed buy-stock/sell-demand SCU; entries persist locally and remain available when live refresh fails.
- Added saved Hauling route filters for asteroid clusters, naval bases, outposts, settlements, space stations, and available star systems; commodity catalog entries are not filtered.
- Added separate Google and Discord Supabase OAuth connection choices, plus a Support SCIP heading and description above the donation button.
- Added Settings fields for the Supabase project URL and publishable/anon key; saving the values configures Google/Discord PKCE sign-in immediately and persists only public client configuration locally. Server-side secret/service-role keys are rejected.
- Expanded the cargo trade planner and three summary cards to the full page width; made the system status strip stretch to its parent panel width.
- Simplified the Support SCIP section to a single disabled-until-configured button; the PayPal URL can be supplied through `SCIP_PAYPAL_DONATION_URL`.
- Replaced the partial wiki ship snapshot with all 250 records from the linked SC-Truckers fleet reference, including its pledge/aUEC, crew, max-box, loading, best-for/loadout, and RSI link fields.
- Updated the root README with launch instructions, feature/data notes, PayPal configuration, Google/Discord Supabase setup, manual quotes, route filters, and the SC-Truckers account integration limitation; removed obsolete planning/error documentation and confirmed the bundled commodity text remains packaged by the active WPF project.

## Still needs doing

- Test image OCR against representative screenshots on Windows with the required OCR language pack installed; verify extracted prices and SCU values before applying.
- Validate OCR against the supplied Buy-tab and Local Value screenshots on Windows; confirm every row's price/SCU, shop stock, supported box sizes, and loading-time estimate before applying.
- Exercise live UEX refresh in the app and confirm generated route choices against current terminal prices, stock, and demand; the build does not verify external API availability.
- Verify closed-loop generation with current UEX data; the planner requires enough observed buy stock and sell demand to build a 2–4-leg cycle.
- Test Google and Discord OAuth after configuring the Supabase providers, callbacks, localhost redirect allowlist, and local environment variables described in `README.md`.
- Enable the Google provider in the project's Supabase Auth settings and validate its browser/localhost callback using the Settings-saved Supabase configuration.
- Set `SCIP_PAYPAL_DONATION_URL` only after a real PayPal page is ready, then confirm the Support SCIP button opens that URL.
- Visually verify the full-width market and hauling layouts, system status strip, OCR system dropdown, Salvage/Mining content, and fleet records in the running app.
- Configure and verify both Google and Discord OAuth against the deployment's Supabase project; external provider credentials and redirect allowlists are deployment-specific and are not bundled.
- Exercise saved manual commodity quotes during both live and offline market refresh, and verify route filters against observed terminals for each supported system/category.
- Test market cache persistence using an isolated data path: save manual/OCR quotes, create a second `DataService`, reload, and confirm the catalog, locations, and observations remain intact.
- Configure a real PayPal.Me/PayPal donation URL in Settings before enabling the donation action.
- Verify Hauling single-route and closed-loop calculations against current UEX quotes and in-game terminal prices, including inventory/departure changes between refreshes.
- Market 24-hour change requires at least one saved live snapshot from 24 hours earlier; the panel shows a building-history state until then. Live Spectrum headlines are not integrated.
- Some fleet-page entries have no listed pledge/AUEC price, crew range, or maximum box size; those individual source values appear as unavailable/not listed rather than being inferred.
- OCR-applied market observations currently update active in-memory market data; persistence across restarts remains to be implemented.
- Route generation requires observed terminal prices plus available stock and demand; SC DataHub reference prices alone are not sufficient to build a location-specific route.
- Test F8/F9 calibration and capture with Star Citizen in borderless/windowed mode, including DPI/multi-monitor coordinates, hotkey conflicts, OCR accuracy, and reviewing/applying staged rows.
- Refresh the bundled ship snapshot periodically from the linked SC-Truckers Fleet Reference; it is static and has no API synchronization.
- Refresh the bundled Hauling location catalog from its upstream Star Citizen Wiki and UEXCorp sources periodically; its reference entries are not market-price observations.
- Verify the included commodity rows and ship catalog visually in an interactive Windows session, including behavior when the app is resized and maximized.
- Add automated tests for market trade limits, OCR parsing/application, and local feature persistence. No test project was found in the dashboard workspace.

## Could not complete here — requires additional capabilities or data

- Live Star Citizen contract capture or RSI faction standings/ranks: the dashboard has no supported in-game/API integration; quest status and reputation points are user-entered journal estimates.
- Live mining deposits, salvage targets, or in-game operation telemetry: run details and yields are manually entered, and station market observations are not field-deposit data.
- Injected in-game overlay, game-memory/API integration, or capture from exclusive-fullscreen mode: the scanner captures a saved desktop region with global F8/F9 hotkeys in borderless/windowed mode, but does not hook into the game.
- Production-grade OCR with calibrated confidence, image cleanup, table/region detection, and broad layout support: the current implementation uses the installed Windows OCR engine and heuristic parsing; representative labeled screenshots and additional OCR engineering are required.
- Verified live Spectrum news, live ship pricing/pledge data, and complete ship-specific loadout recommendations are not available through the integrated data sources; the UI links to official Spectrum/RSI and marks missing catalog data.
- The Google/Discord flows and donation action are implemented but cannot be fully exercised until the user supplies deployment-specific Supabase configuration and a PayPal link.
- UEX webhooks are not connected; their documented receiver flow could not be verified without account access and requires a public HTTPS backend. The WPF app uses client-initiated API refresh instead.
- A private shared data/API secret cannot be embedded in the Lovable frontend or desktop client. Put true secrets in a server-side API/Edge Function; Settings accepts only the shared Supabase publishable/anon key, which is intentionally public.
- UEX `items_prices_all` is a verified endpoint, but its item-price data is not yet incorporated into salvage/component recommendations; no component recommendations should be treated as live prices.
- SC-Truckers account/profile synchronization cannot be enabled without a supported shared API/account integration from SC-Truckers; Supabase login only connects to the configured dashboard project.
- A private key shared by a public Lovable frontend and a desktop client cannot remain secret in either client. A backend/Edge Function with server-side secrets and appropriate user authorization is required for private shared data access.
- Fleet details are a dated public-page snapshot (2026-11-01), not a live synchronization; refresh the bundled catalog when the source changes.
- The 250-ship Fleet Reference data is a bundled snapshot from the linked SC-Truckers page, retrieved on 2026-11-01; it is not automatically synchronized and may become stale.

## Verification

- Latest Release build after OCR row parsing, cargo-size/loading-time fields, UEX `container_sizes`, manual quotes, route filters, and Google/Discord OAuth updates — succeeded with 0 errors and 14 nullable-initialization warnings across Core and `SettingsViewModel.cs`. `launch.bat` rebuilt and started the dashboard successfully; representative OCR label checks and `git diff --check` passed.
- Market-cache/catalog changes — full WPF build using isolated `SCIPVerify` intermediates succeeded with 0 errors and 14 existing nullable warnings. A temporary persistence check loaded all 140 baseline commodities, saved an OCR quote, and confirmed the commodity, station, prices, SCU, box sizes, and loading time after reloading through a second `DataService`. `git diff --check` passed. The normal shared Release build encountered WPF BG1002 missing-BAML errors while other build processes were present; no launcher smoke test was started because multiple dashboard instances were already running.
- Latest `git diff --check` — passed; Git reported only LF-to-CRLF conversion notices for existing edited files.
- Ran the updated root `launch.bat` after the latest OCR changes; Release build succeeded and the newly built dashboard opened its main window (`SC Intelligence Platform - Dashboard`). Stopped only the smoke-test instance afterward.
- Parsed all 250 ship rows from the linked SC-Truckers fleet page, verified unique ship IDs and 250 official RSI HTTPS links, and generated the packaged JSON catalog from those fields.
- Removed old documentation and verified the SC DataHub commodity reference remains included by the active WPF project file.
- `dotnet build ScipDashboard.WPF\ScipDashboard.WPF.csproj --configuration Release --output <temp>` — succeeded, 0 errors (12 existing nullable warnings in Core and SettingsViewModel).
- Ran the updated root `launch.bat`; it built to a fresh temporary output and the dashboard window opened successfully.
- Confirmed the packaged output contains the commodity text source and ship-catalog JSON; commodity parser input has 139 well-formed rows, and the bundled catalog has 250 distinct spacecraft names.
- Bundled Hauling location catalog parses as 985 unique entries: 815 asteroids, five cities, 103 station entries, 59 refinery terminals, and three landing zones across Stanton, Pyro, and Nyx. Verified city-name, system, and type filters return matching entries, and the app build packages the catalog.
- Rebuilt the updated Release app successfully (0 errors, 12 existing nullable warnings), confirmed `Data\location-catalog.json` is present in build output, and smoke-tested dashboard startup.
- Applied coordinated linear-gradient surfaces, buttons, and theme-specific card shadows to both Light and Dark modes; Release build and dashboard startup verified with Dark mode selected.
- Rebuilt after making module tables responsive and disabling root horizontal scrolling; Release build succeeded with 0 errors and 12 existing nullable warnings. `git diff --check` passed (Git reported only the existing ScanViewModel line-ending conversion warning).
- Latest Release WPF build after live hauling and F8/F9 scanner changes succeeded with 0 errors and 12 nullable warnings; dashboard startup smoke test displayed a responsive main window, then the test process was stopped.
- `git diff --check` passed after the latest changes. Live UEX routing, actual in-game capture, and OCR accuracy still require hands-on verification.
