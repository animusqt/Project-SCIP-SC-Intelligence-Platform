# QUICK FIX GUIDE - SCIP Dashboard WPF

## THE ROOT CAUSE IDENTIFIED ✅
**Zero windows display because MainViewModel construction failure blocks UI before Show() is ever called.**

---

## CRITICAL SYNTAX ERROR BLOCKING EVERYTHING

**Location**: `Core/Models/DomainModels.cs`, lines 314-324  
**Problem**: Duplicate closing braces causing `Invalid token '}' in a member declaration`  
**Impact**: Core project won't compile, blocking ALL other fixes

### IMMEDIATE FIX:
Open `Core/Models/DomainModels.cs` and look for:
- Line ~314: Extra closing brace after DataSourceConfig class definition
- Line ~324: Missing class closure
- Line 559: End of file has syntax issues

**What to do**: Find and remove the duplicate `}` that closes DomainModels namespace incorrectly.

---

## HOW TO VERIFY FIX WORKS

```powershell
cd "d:\SCIP Projects\SCIP Dashboard\scip_overlay_v2"
dotnet build Core/Core.csproj  # Must show NO errors now
dotnet build ScipDashboard.WPF/ScipDashboard.WPF.csproj  # Should succeed
.\launch.bat
```

---

## WHAT WORKS ALREADY (CONFIRMED)

✅ **Remove DataContext from MainWindow.xaml** - Window displays  
✅ **App.xaml.cs synchronous initialization** - Pattern is correct  
✅ **TestModuleClasses.cs stubs** - Provides views for binding  
✅ **LocalStorageManager basic methods** - File storage works  

---

## NEXT STEPS AFTER Core Compiles

### Option A: Gradual Feature Rollout
1. Add DataContext binding back with TestModuleClasses.cs
2. Verify window shows
3. Slowly add real module implementations
4. Each fix tested before next change

### Option B: Simplified ViewModel (Recommended)
Create minimal MainViewModel that doesn't try to instantiate views:

```csharp
public class MainViewModel : INotifyPropertyChanged
{
    private DashboardView _currentView;
    
    public DashboardView CurrentView
    {
        get => _currentView;
        set
        {
            _currentView = value;
            OnPropertyChanged(nameof(CurrentView));
        }
    }
    
    // REMOVED: CurrentViewContent property that instantiates views
    
    protected event PropertyChangedEventHandler? PropertyChanged;
    
    public MainViewModel()
    {
        CurrentView = DashboardView.Main;  // No view instantiation needed
    }
}
```

---

## COMMON FIX PATTERNS USED

### DataContext Binding Fix (CONFIRMED WORKING)
**Before**: `<Window.DataContext><local:MainViewModel /></Window.DataContext>`  
**After**: `[Removal - Window displays]`  

### Synchronous Initialization (CORRECT PATTERN)
```csharp
protected override void OnStartup(StartupEventArgs e)
{
    _serviceLocator.Register(new LocalStorageManager(appDataPath));
    _serviceLocator.Register(new DataService(_storageManager));
    Initialize();  // Services initialized BEFORE UI shows
}
```

### Generic JSON Storage (WORKING PATTERN)
```csharp
public T Get<T>(string filename) where T : class
{
    var filePath = Path.Combine(_appDataPath, filename);
    if (!File.Exists(filePath)) return default;
    return System.Text.Json.JsonSerializer.Deserialize<T>(File.ReadAllText(filePath));
}
```
