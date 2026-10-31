# SC Intelligence Platform - Real Data Integration Guide

## Overview
This document outlines how to integrate your real SC-Truckers website data into the dashboard.

## Website Structure
- **Main Site**: https://sc-truckers.lovable.app/
- **Market**: https://sc-truckers.lovable.app/market
- **Hauling**: https://sc-truckers.lovable.app/hauling
- **Data Source**: uexcorp.space (live commodity prices)

## Current Status

### ✅ Completed
1. **Dashboard UI** - Updated branding to "SC Intelligence Platform"
2. **Loading Screen** - Simple progress bar with terminal output capture
3. **Module Structure** - OCR, Market, Fleet, Routes tabs implemented
4. **Market Lookup** - Basic structure with sample data

### 🔄 In Progress
1. **Real API Integration** - Connect to uexcorp.space
2. **OCR Processing** - Parse screenshots from SC-Truckers terminals
3. **Route Optimization** - Multi-stop loops based on live prices
4. **Live Data Sync** - WebSocket connection for real-time updates

### 📋 Next Steps

#### 1. API Endpoint Discovery
Need to find the actual API endpoints on sc-truckers.lovable.app:
- Market data endpoint
- Hauling routes endpoint  
- OCR processing endpoint

#### 2. WebSocket Integration
Set up WebSocket connection to uexcorp.space for live commodity prices:
```python
import websocket
import json

def on_message(ws, message):
    # Parse real-time market data
    commodities = json.loads(message)
    market_lookup.update_from_api(commodities)
```

#### 3. OCR Module Enhancement
Update scanner.py to parse SC-Truckers terminal screenshots:
- Extract commodity names and prices from images
- Format data for MarketLookup integration
- Update dashboard with real scan results

#### 4. Route Planner Integration
Connect route optimization to live market data:
- Fetch current commodity prices
- Calculate profit margins for multi-stop loops
- Recommend optimal routes based on demand

## Files Modified
1. `modules/market/lookup.py` - Added API fetch method
2. `electron_overlay/index.html` - Updated branding
3. `electron_overlay/renderer.js` - Loading screen with terminal output

## Testing
Run the launcher to test integration:
```batch
SCIP Launcher.bat
```

Expected behavior:
1. Clean application window (no terminal)
2. Loading bar shows module initialization
3. Dashboard displays real market data from uexcorp.space
4. OCR scanner processes screenshots and updates prices
5. Route planner optimizes based on live commodity spreads

## Contact & Resources
- SC-Truckers Website: https://sc-truckers.lovable.app/
- UEX Corp API: https://uexcorp.space/
- Dashboard Location: `d:\SCIP Projects\SCIP Dashboard\scip_overlay_v2`
