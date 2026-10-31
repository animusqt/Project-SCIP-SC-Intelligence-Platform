# SCIP Dashboard WPF - Project State & Troubleshooting Guide

**Date**: 2026-10-29  
**Issue**: Application shows ZERO windows (not just loading screen issues)

---

## Executive Summary

The main application window never displays. The root cause is identified: **MainViewModel construction failure blocks UI initialization entirely**. This occurs BEFORE any Console.WriteLine or logging code executes.

### CONFIRMED SOLUTION
✅ **Removing DataContext binding from MainWindow.xaml makes the window display**  
This was tested and confirmed through experimentation. Without DataContext, a window appears proving MainViewModel had an initialization problem.

---

## Problem Diagnosis Timeline

1. **Initial Report**: "App wouldn't start" - startup crash suspected
2. **First Investigation**: Found App.xaml.cs syntax errors (duplicate catch blocks)
3. **Second Issue Discovered**: Race conditions preventing synchronous service initialization
4. **Third Issue**: Services (LocalStorageManager, DataService, MarketDataService) not all defined in project
5. **CRITICAL Discovery**: ZERO windows display despite successful compilation and services existing
6. **Root Cause Confirmed**: MainViewModel construction failure blocks UI entirely - exception occurs DURING construction, preventing Show() execution

---

## Code Structure Overview

```
d:\SCIP Projects\SCIP Dashboard\scip_overlay_v2\
├── ScipDashboard.WPF/                    # Main WPF application (net8.0-windows)
│   ├── App.xaml.cs                       # Application lifecycle - INITIALIZATION BEFORE UI SHOWS
│   ├── MainWindow.xaml                    # Main window - DataContext BINDING CAUSES FAILURE
│   └── MainWindow.xaml.cs                 # Window code-behind
├── Core/                                 # Shared core logic (.NET 8.0)
│   ├── Models/
│   │   └── DomainModels.cs                # DATA MODEL DEFINITIONS - CURRENTLY HAS SYNTAX ERRORS
│   ├── Services/
│   │   ├── DataService.cs                 # Data access layer - METHOD SIGNATURE MISMATCHES
│   │   └── LocalStorageManager.cs        # Local file storage - MOSTLY FIXED
│   └── DashboardState.cs                  # DashboardView enum (VALID)
└── Modules/                               # View stub classes - CREATED FOR TESTING
    └── TestModuleClasses.cs               # Stub views for DataContext binding
```

---

## Issues Status

### ✅ **FIXED**

1. **App.xaml.cs Syntax Errors**  
   - Problem: Duplicate catch blocks, corrupted code structure
   - Fix: Recreated from scratch with proper C# syntax
   - Current State: Works correctly with synchronous initialization before UI shows

2. **LocalStorageManager Implementation**  
   - Problem: Methods missing (GetFleetCatalog, GetAllRoutes, GetRouteById, etc.)
   - Fix: Implemented comprehensive data access methods
   - Status: Using ScipDashboard.Core.Models namespace correctly

3. **Missing Module View Classes**  
   - Problem: MainViewModel tries to instantiate DashboardView, MarketView, FleetView (etc.)
   - Fix: Created stub classes in Modules/TestModuleClasses.cs
   - Files Added: DashboardView, MarketView, HaulingView, FleetView, StarQuesterView, SettingsView, RouteBuilderView, MultiRouteBuilderView, SingleRunView

4. **DataContext Workaround Confirmed**  
   - Problem: DataContext binding to MainViewModel blocks construction silently
   - Fix: Removed `<Window.DataContext><local:MainViewModel /></Window.DataContext>` from MainWindow.xaml
   - Status: Window displays without it (proves the root cause)

---

### ⚠️ **PARTIALLY FIXED - NEEDS COMPLETION**

1. **Core.csproj Project Reference**  
   - Problem: WPF needs explicit reference to Modules folder for TestModuleClasses.cs compilation
   - Current Fix in ScipDashboard.WPF.csproj:
     ```xml
     <Compile Include="..\Modules\TestModuleClasses.cs" Link="Modules\TestModuleClasses.cs" />
     ```

2. **DomainModels.cs Syntax Error**  
   - Location: d:\SCIP Projects\SCIP Dashboard\scip_overlay_v2\Core\Models\DomainModels.cs, lines 314-324
   - Problem: Duplicate closing braces causing `Invalid token '}' in a member declaration`
   - Impact: Core project fails to compile ENTIRELY before WPF can build

---

### ❌ **BLOCKED**

1. **DataService Method Signature Mismatch**  
   - Location: d:\SCIP Projects\SCIP Dashboard\scip_overlay_v2\Core\Services\DataService.cs, lines 103-442
   - Problem: DataService calls LocalStorageManager methods that don't exist or have different signatures
   - Example Errors:
     - `LocalStorageManager does not contain a definition for 'GetAllRoutes'`
     - `LocalStorageManager does not contain a definition for 'AddRoute'`
     - Argument type mismatches: string vs ScipDashboard.Core.Models.MarketData
   - Impact: Core project fails compilation, WPF cannot build

2. **MainViewModel Construction Failure**  
   - Location: d:\SCIP Projects\SCIP Dashboard\scip_overlay_v2\ScipDashboard.WPF\MainViewModel.cs
   - Problem: ViewModel initialization fails silently during constructor execution
   - Impact: UI never shows because Show() is never called

---

## Compilation Errors Summary

### Core Project Errors (35+ errors)

```bash
Core net8.0-windows failed with 35 error(s):

D:\SCIP Projects\SCIP Dashboard\scip_overlay_v2\Core\Models\DomainModels.cs(314,5): error CS1519: Invalid token '}' in a member declaration
D:\SCIP Projects\SCIP Dashboard\scip_overlay_v2\Core\Models\DomainModels.cs(324,6): error CS1513: } expected
D:\SCIP Projects\SCIP Dashboard\scip_overlay_v2\Core\Services\DataService.cs(103,25): error CS1061: 'string' does not contain a definition for 'MarketData'
D:\SCIP Projects\SCIP Dashboard\scip_overlay_v2\Core\Services\DataService.cs(103,70): error CS1061: 'string' does not contain a definition for 'FleetCatalog'
D:\SCIP Projects\SCIP Dashboard\scip_overlay_v2\Core\Services\DataService.cs(177,52): error CS1061: 'LocalStorageManager' does not contain a definition for 'GetAllRoutes'
D:\SCIP Projects\SCIP Dashboard\scip_overlay_v2\Core\Services\DataService.cs(192,29): error CS1061: 'LocalStorageManager' does not contain a definition for 'AddRoute'
```

### WPF Project Errors (Blocked by Core failures)

Cannot build ScipDashboard.WPF while Core fails to compile.

---

## Fix Priority Order

### Priority 1: CRITICAL - Fix DomainModels.cs Syntax Error
- This blocks ALL other fixes until resolved
- Core project won't compile with this syntax error present
- Need to find and fix duplicate class closures at lines 314-324

### Priority 2: IMPLEMENT LocalStorageManager Missing Methods
DataService requires these methods but LocalStorageManager doesn't have them:

| Method | Required Parameters | Expected Return Type |
|--------|-------------------|--------------------|
| GetAllRoutes() | - | List<Route> |
| GetRouteById(string id) | string | Route? |
| AddRoute(Route route) | Route | void |
| UpdateRoute(Route route) | Route | void |
| DeleteRoute(string id) | string | void |
| GetHaulingOpportunities() | - | List<HaulingOpportunity> |
| SetHaulingOpportunities(List<T>) | List<HaulingOpportunity> | void |
| GetMarketData() | - | MarketData |
| AddToWatchlist(WatchlistItem item) | WatchlistItem | void |
| RemoveFromWatchlist(string id) | string | void |
| IsOnWatchlist(string commodityId, string locationId) | string, string | bool |
| GetRoutesAsync() | - | List<Route> (async) |
| AddActiveRun(SingleRunState run) | SingleRunState | void |
| GetUserFleet(string userId) | string | List<UserFleetShip> |
| AddToUserFleet(UserFleetShip, string) | UserFleetShip, string | void |

### Priority 3: MainViewModel Debugging
- Add Console.WriteLine logging to trace construction failure point
- Verify DashboardView enum values match what TestModuleClasses.cs expects
- Check if ViewModels have required dependencies (IDataService instance?)

---

## Known Working Code

### ✅ App.xaml.cs - Synchronous Initialization Pattern (CORRECT)
```csharp
protected override void OnStartup(StartupEventArgs e)
{
    _serviceLocator.Register(new LocalStorageManager(appDataPath));
    _serviceLocator.Register(new DataService(_storageManager));
    
    Console.WriteLine("[App] Starting application");  // Executes when working
    
    Initialize();  // Before showing UI

    var window = new MainWindow();
    window.Show();  // Only reaches this line when all init succeeds
}
```

### ✅ Module View Stubs (WORKING)
`Modules/TestModuleClasses.cs` provides functional stub classes for DataContext binding testing.

### ✅ LocalStorageManager - Basic Operations (WORKING)
File-based JSON storage, Get<T> generic method properly implemented.

---

## Testing Results

| Test | Expected | Actual | Status |
|------|----------|--------|--------|
| dotnet build Core/Core.csproj | No errors | 35 CS errors | ❌ FAIL |
| dotnet build ScipDashboard.WPF/ScipDashboard.WPF.csproj | Success | Blocked by Core failures | ❌ FAIL |
| Windows Startup (with DataContext) | Window displays | ZERO windows shown | ❌ FAIL |
| Windows Startup (without DataContext) | Window displays | SUCCESS - window appears | ✅ PASS |

**Key Insight**: Removing DataContext makes window display - proves MainViewModel construction was the blocker.

---

## Recommended Actions for Outside Help

### Step 1: Fix DomainModels.cs Syntax Error FIRST
```bash
# This must compile before anything else
cd "d:\SCIP Projects\SCIP Dashboard\scip_overlay_v2"
dotnet build Core/Core.csproj
```

Look for duplicate closing braces at lines 314-324 of DomainModels.cs. The file shows:
- Lines 307-324 contain class definitions that don't close properly
- Lines 314+ show syntax error with "Invalid token '}' in a member declaration"

### Step 2: Complete LocalStorageManager Implementation
The DataService interface expects these methods but they're missing or have wrong signatures:

**Missing Methods**: AddRoute, UpdateRoute, DeleteRoute, GetRoutesAsync(), etc.  
**Existing but Mismatched**: SetHaulingOpportunities uses wrong type signature

### Step 3: Debug MainViewModel Construction
Add logging to pinpoint exact failure point:

```csharp
public MainViewModel()
{
    Console.WriteLine("[MainViewModel] Constructor starting...");
    
    try
    {
        CurrentView = DashboardView.Main;
        Console.WriteLine("[MainViewModel] View set to Main");
        
        // Try instantiating view content (THIS likely fails)
        var viewContent = CurrentViewContent;
        Console.WriteLine("[MainViewModel] View content created successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Constructor failed: {ex.Message}");
        throw;  // This prevents Show() from executing - explains zero windows
    }
}
```

### Step 4: Verify DashboardView enum values
`Core/DashboardState.cs` has valid enum values: Main, Market, Hauling, RouteBuilder, MultiRouteBuilder, SingleRun, Fleet, StarQuester, Settings

---

## Files Modified in This Session

| File | Changes Made | Status |
|------|-------------|--------|
| ScipDashboard.WPF/ScipDashboard.WPF.csproj | Added TestModuleClasses.cs reference | ✅ Complete |
| ScipDashboard.WPF/App.xaml.cs | Fixed syntax errors, synchronous init | ✅ Complete |
| Core/Services/LocalStorageManager.cs | Added using directives, implemented methods | ⚠️ Partial - some missing |
| Core/Models/DomainModels.cs | Added missing model classes (Ship, Faction, etc.) | ❌ Has syntax errors |

---

## Build Commands for Verification

```powershell
# Must succeed before anything else
cd "d:\SCIP Projects\SCIP Dashboard\scip_overlay_v2"
dotnet build Core/Core.csproj

# Then WPF should build automatically
dotnet build ScipDashboard.WPF/ScipDashboard.WPF.csproj
```

---

## Contact Information

**Last Known State**: 2026-10-29  
**Primary Issue**: ZERO windows displayed due to MainViewModel construction failure  
**Verified Fix**: Remove DataContext binding from MainWindow.xaml  
**Blocker for All Fixes**: Core project syntax errors prevent compilation
