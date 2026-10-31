# Building SC Intelligence Platform

## How to read this tracker

- `[x]` = step finished. `[ ]` = not finished.
- Every step has a **Done when** line: the check that proves it. A step is only ticked once its check passes.
- Items marked ✅ in the old tracker have their steps ticked below because the old tracker said they were done. **Those ticks have not been checked against the code.** Run each "Done when" and untick anything that fails.
- Status key on the headings: ✅ complete · 🟡 partly in v1, needs extending · ⬜ not started.

---

## Progress Tracker (completed work)

### ✅ 1. Project Structure & Modular Architecture
- [x] Create the folder layout: `modules/ocr`, `modules/market`, `modules/fleet`, `modules/routes`, `data/`, `scanner-engine/`, `electron-overlay/`.
  **Done when:** all folders exist and each module folder has an `__init__.py`.
- [x] Give every module the same entry point, e.g. `handle(action, payload) -> dict`, so `main.py` and the bridge can call any module the same way.
  **Done when:** `main.py` can call `market`, `fleet`, `routes` and `ocr` by name with no module-specific code.
- [x] Put all file paths in one place (data folder, scans folder, calibration file) so nothing is hard-coded inside modules.
  **Done when:** moving the project folder doesn't break any path.

### ✅ 2. OCR Module Implementation
- [x] Install the OCR dependencies: Tesseract (the program), plus `opencv-python`, `numpy`, `pytesseract`, `mss`, `pynput`.
  **Done when:** `tesseract --version` works and `python -c "import cv2, numpy, pytesseract, mss, pynput"` raises no error.
- [x] Bring the v1 engine into `scanner-engine/` (`sc_parser.py`, `scanner.py`, `scanner_overlay_bridge.py`).
  **Done when:** v1's parser tests pass (`python -m unittest tests/test_sc_parser.py`, 9 tests).
- [x] Write `modules/ocr/scanner.py` as a thin wrapper that calls the scanner-engine instead of copying its logic.
  **Done when:** there is one copy of the parsing code in the project.
- [x] Screen capture of the saved region, plus saving/loading that region (`scanner_region.json`).
  **Done when:** calling scan on a kiosk screen returns commodity rows, and the region survives a restart.
- [x] Return results as JSON: station, tab (buy/sell), rows (commodity, buy, sell, stock, demand), quality warnings.
  **Done when:** the same scan always has the same JSON shape, including when it finds nothing (empty `rows`, not an error).

### ✅ 3. Market Module Implementation
- [x] Define the `data/commodities.json` shape (per station → per commodity → buy, sell, stock, demand, scanned_at).
  **Done when:** the shape is written down in this file or a schema file.
- [x] Write `modules/market/lookup.py`: load the file, look up a commodity by name (use the engine's name matching so "Quantanium"/"Quantainium" both work), find best buy and best sell across stations.
  **Done when:** `lookup("Quartz")` returns the cheapest buy and the highest sell with station names.
- [x] Update the file from a scan: newest values win, older known values are kept, every entry carries a timestamp.
  **Done when:** scanning the same station twice updates it without duplicate rows.
- [x] Write the file safely (write to a temp file, then replace) so a crash can't corrupt it.
  **Done when:** killing the program mid-write leaves a valid JSON file.

### ✅ 4. Fleet Module Implementation
- [x] Define `data/fleet.json` (id, name, ship model, cargo SCU).
  **Done when:** the shape is documented.
- [x] Write `modules/fleet/manager.py`: add, list, update, remove, with validation (name required, cargo is a non-negative number).
  **Done when:** bad input returns a clear error and leaves the file unchanged.
- [x] Persist across restarts.
  **Done when:** add a ship, restart, the ship is still listed.

### ✅ 5. Route Module Implementation
- [x] Write `modules/routes/planner.py`: for each commodity, pair every buy station with every different sell station, keep profitable pairs.
  **Done when:** with two stations of sample data, the expected route and profit per SCU come out.
- [x] Use the chosen ship's cargo size for total profit.
  **Done when:** changing ship changes total profit but not profit per SCU.
- [x] Ignore stale prices (e.g. older than 7 days) and rank by profit.
  **Done when:** a price older than the limit never appears in a route.
- [x] Save results to `data/routes.json` (optional cache).
  **Done when:** the file is rewritten on each plan and stays valid JSON.

### ✅ 6. Electron Integration
- [x] Create the Electron app: `package.json`, `main.js`, `preload.js`, `renderer.js`, `overlay.html`, `overlay.css`.
  **Done when:** `npm install` then `npm start` opens the overlay window.
- [x] Window behaviour: frameless, transparent, always on top; closing hides to the tray; tray menu has Show and Quit.
  **Done when:** close hides the window, Quit ends every process (check Task Manager: no leftover Electron or Python).
- [x] Start and stop the Python backend from Electron (child process), including when Electron crashes.
  **Done when:** no orphaned Python process remains after the overlay exits.
- [x] Launchers: `Launch Overlay.bat` and `launch-hidden.vbs`.
  **Done when:** double-clicking opens the overlay with no console window left behind.

### ✅ 7. Communication Bridge (Heartbeat)
- [x] Choose the transport: one JSON message per line over the Python process's stdin/stdout (the v1 bridge pattern).
  **Done when:** a test message goes both ways.
- [x] Python replies to a `ping` with `pong`; Electron pings on a timer.
  **Done when:** the overlay shows "connected" within a few seconds of start.
- [x] If pongs stop, mark disconnected and restart the backend (with a retry limit).
  **Done when:** killing the Python process shows "disconnected", then "connected" again by itself.

### ✅ 8. Communication Bridge (Market Module)
- [x] Define actions: `market.lookup`, `market.best`, `market.update`.
  **Done when:** each action has a request and reply shape written down.
- [x] Expose them through `preload.js` using an allow-list of channel names (no raw `ipcRenderer` in the page).
  **Done when:** calling an unlisted channel from the page does nothing.
- [x] Add a request id so replies match requests.
  **Done when:** two lookups sent at once each get the right answer.

### ✅ 9. Communication Bridge (Remaining Modules)
- [x] Add the same pattern for `fleet.*`, `routes.*` and `ocr.scan / calibrate / live / set_station`.
  **Done when:** every module action can be called from the overlay.
- [x] One error format for all modules, e.g. `{ok: false, error: "..."}`.
  **Done when:** an unknown action or bad input returns an error message and the backend keeps running.
- [x] Push events from Python to the overlay (scan finished, calibration preview, status text).
  **Done when:** pressing F9 shows the result in the overlay without a manual refresh.

### ✅ 10. Frontend Integration
- [x] Overlay panels: Scan, Market, Fleet, Routes, plus a status bar (connection, scanning, last scan).
  **Done when:** each panel loads its data on open.
- [x] Hotkeys: F8 calibrate, F9 scan, Esc cancel, F10 exit.
  **Done when:** the hotkeys work while Star Citizen has focus.
- [x] Show quality warnings and let the user fix the station name before saving.
  **Done when:** a scan of "Unknown Station" can be renamed and saved to the right station.
- [x] End-to-end flow.
  **Done when:** F9 scan → rows shown → saved → visible in Market → a route appears in Routes.

### ✅ 11. Testing (the Notes say "tested", but the tracker has no testing step)
- [x] Unit tests for each module (`market`, `fleet`, `routes`, `ocr` wrapper) using sample JSON.
  **Done when:** one command runs them all and they pass.
- [x] A bridge test: send each action with good and bad input, expect no crash.
  **Done when:** all actions return `ok` or a clean error.
- [x] A saved-screenshot test for OCR (so it can run without the game).
  **Done when:** a known screenshot always gives the same rows.

---

## Pending Ideas (For Approval)

### ⬜ D. Supabase Integration (replace local JSON)
- [ ] Design tables that match the JSON: commodities/prices, fleet, routes. Add `updated_at` columns.
  **Done when:** the schema file is written and reviewed.
- [ ] Add a storage layer with two backends (JSON and Supabase) behind the same functions, so modules don't know which is in use.
  **Done when:** all modules pass the same tests on both backends.
- [ ] Set access rules (row-level security) before any real data goes in; the app uses only the public (anon) key, never the service key.
  **Done when:** a request without sign-in cannot read or change data it shouldn't.
- [ ] Keep keys in a local settings file or environment variables, never in the repo.
  **Done when:** searching the repo finds no keys.
- [ ] Migration script: JSON → Supabase, safe to run twice.
  **Done when:** running it twice gives the same row counts.
- [ ] Offline fallback: keep working from a local copy and sync when back online.
  **Done when:** unplug the network, scan, reconnect: the scan appears online.

### ⬜ E. Discord Notifications (price alerts)
- [ ] Define an alert rule (commodity, buy/sell, above/below, price) stored in `data/alerts.json`.
  **Done when:** rules can be added, listed and removed.
- [ ] Check the rules after every scan or market update.
  **Done when:** a scan that crosses a price triggers exactly one alert.
- [ ] Send through a Discord webhook; treat the webhook URL as a secret (settings file, not the repo).
  **Done when:** a test button posts a message to the channel.
- [ ] Stop repeat alerts (don't resend the same alert within a set time) and handle failures quietly.
  **Done when:** three scans in a row at the same price send one message, and a failed send doesn't stop scanning.

### ⬜ B. Real-time Fleet Sync (WebSocket)
- [ ] **Decide what syncs with what** (two overlays? the overlay and a website?). This changes everything below.
  **Done when:** one sentence says who sends updates and who receives them.
- [ ] Define the ship status fields (location, cargo loaded, available/busy).
  **Done when:** the fields are written down and `fleet.json` has them.
- [ ] Add a WebSocket server in the Python backend and a client in the overlay.
  **Done when:** changing a ship in one place updates the other within about a second.
- [ ] Reconnect automatically and re-sync everything after a drop (reuse the heartbeat).
  **Done when:** disconnect the network, change a ship, reconnect: both sides match.
- [ ] Decide who wins when both sides changed the same ship (newest timestamp is simplest).
  **Done when:** a test with simultaneous edits gives one predictable result.

### ⬜ C. Route Visualizer (mini-map)
- [ ] Build `data/locations.json` with a position for every station (system, planet/moon, coordinates).
  **Done when:** every station used in a route has a position.
- [ ] Draw the map (SVG or canvas) in a new overlay panel: stations as dots, the chosen route as a line.
  **Done when:** selecting a route in Routes highlights it on the map.
- [ ] Handle stations with no position (show them in a list, don't crash).
  **Done when:** a route with an unknown station still shows the rest.
- [ ] Zoom and pan; keep it light so the game isn't slowed.
  **Done when:** the overlay's CPU use stays low while the map is open.

### ⬜ D. Supabase Integration (replace local JSON)
- [ ] Design tables that match the JSON: commodities/prices, fleet, routes. Add `updated_at` columns.
  **Done when:** the schema file is written and reviewed.
- [ ] Add a storage layer with two backends (JSON and Supabase) behind the same functions, so modules don't know which is in use.
  **Done when:** all modules pass the same tests on both backends.
- [ ] Set access rules (row-level security) before any real data goes in; the app uses only the public (anon) key, never the service key.
  **Done when:** a request without sign-in cannot read or change data it shouldn't.
- [ ] Keep keys in a local settings file or environment variables, never in the repo.
  **Done when:** searching the repo finds no keys.
- [ ] Migration script: JSON → Supabase, safe to run twice.
  **Done when:** running it twice gives the same row counts.
- [ ] Offline fallback: keep working from a local copy and sync when back online.
  **Done when:** unplug the network, scan, reconnect: the scan appears online.

### ⬜ E. Discord Notifications (price alerts)
- [ ] Define an alert rule (commodity, buy/sell, above/below, price) stored in `data/alerts.json`.
  **Done when:** rules can be added, listed and removed.
- [ ] Check the rules after every scan or market update.
  **Done when:** a scan that crosses a price triggers exactly one alert.
- [ ] Send through a Discord webhook; treat the webhook URL as a secret (settings file, not the repo).
  **Done when:** a test button posts a message to the channel.
- [ ] Stop repeat alerts (don't resend the same alert within a set time) and handle failures quietly.
  **Done when:** three scans in a row at the same price send one message, and a failed send doesn't stop scanning.

---

## Gaps found in the Steps and Prerequisites below

These come from comparing this file with your v1 repo, not from your D: folder, which I couldn't open.

- **Tesseract is missing from Prerequisites.** The Python packages are not enough: the Tesseract program itself must be installed (the v1 bridge looks in `C:\Program Files\Tesseract-OCR\`).
- **Python 3.8+ may be too low.** Check what the code actually uses; 3.10+ is a safer minimum if any newer syntax is used.
- **Folder name mismatch.** Step 3 says `electron_overlay` (underscore); the v1 repo's folder is `electron-overlay` (hyphen). Use whichever your D: folder really has, in this file and in the launcher scripts.
- **No root `requirements.txt` in v1.** Step 2 (`pip install -r requirements.txt`) fails unless one is created. It needs: `opencv-python`, `numpy`, `pytesseract`, `mss`, `pynput`.
- **Placeholder clone URL.** Step 1 still says `yourusername`.

## Notes

- Ensure that the OCR logic is correctly integrated from `scanner-engine`.
- Ensure that all modules are correctly implemented and tested (see item 11).
- Ensure that Electron integration is correctly set up.

## Prerequisites
- Python 3.10+ (Python 3.8+ may be too low)
- pip
- Node.js (for Electron)
- Tesseract OCR (program and dependencies)

## Steps

1. **Clone the Repository**
    ```bash
    git clone https://github.com/YOUR_USERNAME/scip_overlay_v2.git
    cd scip_overlay_v2
    ```

2. **Install Python Dependencies**
    - Create a `requirements.txt` file in the root directory with the following content:
      ```
      opencv-python
      numpy
      pytesseract
      mss
      pynput
      ```
    - Install the dependencies:
      ```bash
      pip install -r requirements.txt
      ```

3. **Install Electron Dependencies**
    ```bash
    cd electron-overlay
    npm install
    ```

4. **Run the Application**
    - **With Python Backend**
        ```bash
        python main.py
        ```
    - **With Electron Frontend**
        ```bash
        cd electron-overlay
        npm start
        ```

## Modules

- **OCR Module**
    - `modules/ocr/scanner.py` (Integrated with scanner-engine)

- **Market Module**
    - `modules/market/lookup.py`

- **Fleet Module**
    - `modules/fleet/manager.py`

- **Route Module**
    - `modules/routes/planner.py`

## JSON Data

- `data/commodities.json`
- `data/fleet.json`
- `data/routes.json`
