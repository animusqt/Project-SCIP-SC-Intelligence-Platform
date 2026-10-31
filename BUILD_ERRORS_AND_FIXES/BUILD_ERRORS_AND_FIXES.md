# Build Errors and Fixes Tracking

**Last Updated**: 2026-10-30  
**Project**: SCIP Dashboard - Native C# WPF Application

---

## NETSDK1022 - Duplicate Compile Items Error (RESOLVED)

### Error Details
```
NETSDK1022: Duplicate 'Compile' items were included. The .NET SDK includes 'Compile' items from your project directory by default. You can either remove these items from your project file, or set the 'EnableDefaultCompileItems' property to 'false' if you want to explicitly include them in your project file.
```

### Root Cause
The `Core.csproj` file was missing or had incorrect configuration. The SDK's implicit compile mechanism was conflicting with explicit Compile Include items.

### Fix Applied
1. Created fresh `Core.csproj` from scratch
2. Added `<EnableDefaultCompileItems>false</EnableDefaultCompileItems>` property  
3. Explicitly listed all source files in Compile Include order:
   - Models\Models.cs (first to ensure types are compiled before Services reference them)
   - Common/Common/*.cs
   - Data/*.cs
   - Services/*.cs

### Files Modified
- `Core/Core.csproj` (recreated from scratch)

### Status: ✅ RESOLVED

---

## Remaining Compilation Errors (Updated 2026-10-30)

### ✅ Fixed During This Session:

| Issue | Fix Applied | File Modified |
|-------|-------------|---------------|
| decimal/double conversion | Changed FuelEfficiency return type to `decimal` | Models.cs line 137 |
| Route.Quantity missing | Added `public decimal Quantity { get; set; }` | Models.cs line ~258 |
| Route.Profit missing | Added `public decimal Profit { get; set; }` | Models.cs line ~261 |

### ⚠️ Still Blocking Compilation:

**Core Issue:** IDataService interface has **placeholder types** (empty/minimal classes) but MarketDataService expects the **complete types** defined in Models.cs. This creates a type mismatch that blocks 8+ compilation errors.

#### Primary Blocker: Type Mismatch Between Interface & Implementation

**Issue:**
```
cs0117: 'Commodity' does not contain a definition for 'Id'
cs0117: 'PriceObservation' does not contain a definition for 'Timestamp'
```
These errors occur because:
- `IDataService.cs` has **empty placeholder classes**: `public class Commodity { /* nothing */ }`
- `MarketDataService.cs` tries to use properties that don't exist in placeholders
- The actual complete definitions live in `Models.cs`

**Why This Blocks Fix Progress:**
- Cannot verify if fixes are working until Core project compiles
- IDE shows errors referencing placeholder types instead of Models.cs types
- Type checking happens at compile time, not runtime
- Fixes to Model classes (already applied) don't help because interface is using wrong types

#### Potential Blockers from Missing Context:

1. **Namespace Resolution Confusion**
   - When IDataService declares `Task<List<Commodity>>`, it uses the placeholder class in that file
   - MarketDataService must compile against Models.cs type to get complete properties
   - The two types are **different classes** with same name, causing type confusion

2. **Compile Order Dependency**
   - Core.csproj lists: `Models` → `Data` → `Services`
   - Data/*.cs includes both IDataService interface AND SampleDataGenerator class
   - If Models.cs doesn't fully compile before Services reference it, types won't be available
   - Already mitigated by EnableDefaultCompileItems=false and Models-first ordering

3. **Missing Model Properties**
   - Even if we use fully qualified namespaces, MarketDataService expects:
     - `MarketData.Commodities` (List<Commodity>)
     - Complete PriceObservation fields (Timestamp, SourceName, etc.)
     - Complete Commodity fields (Id, Name, CurrentBuyPrice, etc.)
   - These must match what Models.cs defines

4. **SampleDataGenerator Class Visibility**
   - MarketDataService references: `SampleDataGenerator.GenerateFullSampleData()`
   - SampleDataGenerator is in Data/SampleDataGenerator.cs
   - Must compile successfully before Services use it
   - Currently included in Compile Include="Data\*.cs" ✅

5. **IDataService Implementation Stub**
   - IDataService has placeholder implementation classes that don't match Models.cs types
   - MarketDataService injects interface but calls methods using complete Model types
   - Needs either:
     a) Complete IDataService implementations matching Models, OR
     b) Remove stubs and let Services compile directly against Models

---

### Summary of Required Changes

| Issue | Severity | Status | Fix Applied |
|-------|----------|--------|-------------|
| NETSDK1022 | Critical | ✅ RESOLVED | Core.csproj recreated |
| decimal/double conversion | High | ⚠️ NEEDS FIX | Change to decimal return type |
| Route.Quantity missing | Medium | ⚠️ NEEDS FIX | Add Quantity property |
| Route.Profit missing | Medium | ⚠️ NEEDS FIX | Add Profit property |
| MarketData.Commodities missing | High | ⚠️ NEEDS FIX | Add Commodities List |
| SampleDataGenerator undefined | Medium | ⚠️ NEEDS FIX | Verify Compile include |
| Missing IDataService methods | High | ⚠️ NEEDS FIX | Update interface with all methods |

---

## Next Steps to Complete Build

1. **Fix decimal/double conversion** in Ship.FuelEfficiency property
2. **Add missing Route properties**: Quantity, Profit
3. **Add MarketData.Commodities List** property  
4. **Ensure SampleDataGenerator is in Compile Include** list in Core.csproj
5. **Update IDataService interface** with all required methods:
   - GetCommoditiesAsync()
   - GetPriceObservationsAsync(commodityId)
   - AddPriceObservationAsync(observation)
   - SaveMarketDataAsync(data)
6. **Verify namespace references** in IDataService for Commodity and PriceObservation

---

## Build Plan

After fixes are applied:
1. Clean build output: `dotnet clean Core/Core.csproj`
2. Restore packages: `dotnet restore Core/Core.csproj`
3. Build Core project: `dotnet build --configuration Debug`
4. Test build WPF: `dotnet build ScipDashboard.WPF/ScipDashboard.WPF.csproj`
5. Run application and verify UI displays
