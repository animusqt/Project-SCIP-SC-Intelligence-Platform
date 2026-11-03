# SCIP Dashboard

SCIP Dashboard is a Windows desktop application built with C# and WPF on .NET 8. `launch.bat` builds the current source into a fresh temporary output folder and starts that build, so it does not launch a stale copy from `Publish`.

## Requirements and launch

- Windows 10/11
- .NET 8 SDK with the Windows Desktop workload

From the project directory, double-click `launch.bat`, or run:

```powershell
dotnet build .\ScipDashboard.WPF\ScipDashboard.WPF.csproj --configuration Release
.\launch.bat
```

The app runs locally by default. Commodity references, the ship reference snapshot, the hauling location catalog, OCR, and saved fleet/quest/operation data are available without signing in. Live UEX market data requires an internet connection.

## Features

- Main kiosk with market status and observed price spreads
- Commodity catalog and cargo trade planner
- Hauling route builder with terminal buy/sell quote pairing, SCU/cash/cargo limits, explicit buy cost/sell revenue/net-profit previews, and 2–4-leg closed terminal/orbit loops
- 250-ship catalog from the SC-Truckers fleet reference, with pledge/aUEC prices, cargo capacity, crew, box size, loading support, loadout guidance, and RSI links
- Salvage/mining planning and a local run log
- StarQuester journal
- Windows OCR for images or a calibrated game-screen region (F8 to select, F9 to capture); review and edit recognized rows before applying
- OCR reads Buy-tab price/SCU and explicitly labeled shop stock separately from Local Value sell price, accepted cargo-box sizes, and any visible loading-time estimate
- Light/dark themes and local settings

The ship catalog is a bundled snapshot and can become outdated. Live market values, stock, and demand are external observations; bundled commodity reference prices are not terminal inventory. The scanner treats Local Value's displayed `0 SCU` as the player's current cargo, not terminal sell demand. Accepted cargo sizes and a loading-time estimate are separate fields. OCR results are estimates until reviewed.

## Connect a dashboard account with Supabase

Discord and Google account connection use Supabase Auth and PKCE. The app does not need or store your provider client secret. Configure either or both providers:

1. In your [Supabase project](https://supabase.com/dashboard), open **Authentication → Sign In / Providers** and enable Discord and/or Google.
2. Create OAuth credentials in the [Discord Developer Portal](https://discord.com/developers/applications) and/or [Google Cloud Console](https://console.cloud.google.com/apis/credentials); enter each provider's client ID and secret in the matching Supabase provider settings.
3. Add the provider callback URL shown by Supabase to each OAuth application's authorized redirect URIs. It has this form:

   ```text
   https://<project-ref>.supabase.co/auth/v1/callback
   ```

4. In Supabase, open **Authentication → URL Configuration** and add this development redirect URL:

   ```text
   http://localhost:*/oauth2/callback/
   ```

   SCIP selects an available localhost port for each sign-in attempt. Keep this wildcard limited to local development; production web apps should use exact redirect URLs.
5. In **Settings → Connect Account**, enter your Supabase project URL and **publishable/anon** key, then choose **Save Supabase Settings**. This configuration is stored on this PC and can be the same public client configuration used by the Lovable app. Alternatively, set them in the same PowerShell window used to launch SCIP:

   ```powershell
   $env:SUPABASE_URL = "https://<project-ref>.supabase.co"
   $env:SUPABASE_API_KEY = "<publishable-or-anon-key>"
   .\launch.bat
   ```

6. Open **Settings → Connect Account** and choose Discord or Google.

The project URL and publishable/anon key are public client configuration, not secrets. Never put a Supabase `service_role` or secret key in the desktop app, Lovable frontend, source code, or a checked-in file. Any genuinely shared server credential must live in a backend/Edge Function secret and be accessed through that backend. Google must be enabled in Supabase, and its redirect allowlist must match before Google sign-in can complete.

This connects the desktop dashboard to a Supabase user. It does not authenticate to or synchronize account/profile data with SC-Truckers; that requires an integration and shared account/backend support from that service. For setup details, see [Supabase: Sign in with Discord](https://supabase.com/docs/guides/auth/social-login/auth-discord), [Supabase: Sign in with Google](https://supabase.com/docs/guides/auth/social-login/auth-google), and [Supabase: Redirect URLs](https://supabase.com/docs/guides/auth/redirect-urls).

## Manual commodity quotes and route filters

In **Settings → Commodity Catalog**, add commodity name, buy/sell prices, buy/sell locations, and observed buy-stock/sell-demand SCU. Quotes are saved locally and merged into the market view. Unknown stock or demand remains zero; routes require positive observed SCU rather than invented inventory.

The Hauling builder ranks commodities and destinations by estimated total net profit for the currently affordable SCU quantity, using terminal buy price × purchased SCU and terminal sell price × sold SCU. Auto loops are restricted to 2–4 connected terminal/orbit legs and return to their starting terminal/orbit; manually built loops must also close before they can be saved.

**Settings → Route Location Filters** constrains route suggestions by location type and star system. These filters do not remove commodities from the catalog.

## Support button

The **Support SCIP** button is disabled until a valid HTTPS PayPal or PayPal.Me URL is configured. When ready, set this environment variable before launching:

```powershell
$env:SCIP_PAYPAL_DONATION_URL = "https://paypal.me/<your-page>"
.\launch.bat
```

## Data and privacy notes

- Local fleet, journal, operation, market-history, and support settings are stored under the current Windows user's local application data.
- Sign-in is optional. Market refresh uses UEXCorp; the bundled SC DataHub commodity list is reference data.
- Live commodity prices and terminal stock/demand use UEX API 2.0's `commodities_prices_all` feed; its `container_sizes` field supplies supported box sizes when available. See the [UEX API 2.0 documentation](https://uexcorp.space/api/documentation/). The API is community-sourced, so verify prices and station availability in game.
- The market cache is saved as JSON at `%LocalAppData%\SCIP Dashboard\market-data.json`. It keeps the SC DataHub catalog, UEX commodity catalog and averages, terminal quotes, and reviewed OCR/direct-entry observations across restarts. Settings-based manual quote definitions remain in the local settings store. UEX average prices/supply/demand are context for commodity comparison; hauling still requires terminal-specific quotes and stock/demand.
- Live refresh is client-initiated polling. UEX webhooks are not connected: the documented webhook workflow requires a reachable public HTTPS receiver, which a local WPF desktop app does not provide.
- UEX API 2.0 also exposes terminal item prices through `items_prices_all`; the dashboard does not yet import that feed into salvage/component recommendations. Mining/salvage yield and component selections remain planning estimates, not live extraction data.
- The scanner captures a selected desktop region and uses the installed Windows OCR language pack. It does not read game memory or inject into Star Citizen.
- External feeds, OAuth, and in-game OCR behavior depend on the user's network, provider configuration, game display mode, and OCR language pack.

See [TODO.md](./TODO.md) for completed work, remaining checks, and known limitations.
