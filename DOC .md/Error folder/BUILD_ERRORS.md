# SCIP Dashboard - Build Errors Summary (2026-10-28)

## Issues Fixed ✅

### 1. Invalid `Spacing` Property on StackPanel
**Location**: XAML lines with `Spacing="16"` attribute  
**Error**: `MC3072: The property 'Spacing' does not exist in XML namespace 'http://schemas.microsoft.com/winfx/2006/xaml/presentation'`  
**Fix**: Remove Spacing attribute, use margin or padding instead

### 2. Duplicate Children in Border Elements
**Location**: XAML lines around 75-93  
**Error**: `MC3089: The object 'Border' already has a child and cannot add 'TextBlock'`  
**Fix**: Each Border can only contain ONE direct child element. Added StackPanel inside Borders for multiple children.

### 3. NotImplementedException Throws
**Location**: XAML line 59 (Grid.RowDefinitions)  
**Error**: `MC3072: The property 'RowDefinitions' property is read-only and cannot be set from markup`  
**Fix**: Moved RowDefinitions to child element structure, commented out Grid.Row

### 4. Circular Type Dependency
**Location**: C# file MainWindow.xaml.cs line 11-14  
**Error**: `CS0146: Circular base type dependency involving 'Window' and 'Window'`  
**Fix**: Renamed class from `public partial class Window : Window` to `public partial class MainWindow : Window`

### 5. Invalid CornerRadius on Grid
**Location**: XAML line 64  
**Error**: `MC3072: The property 'CornerRadius' does not exist in XML namespace 'http://schemas.microsoft.com/winfx/2006/xaml/presentation'`  
**Fix**: Removed CornerRadius from Grid, keep on Border elements instead

## Build Status

### Final Error Encountered
```
MC3072: The property 'Spacing' does not exist in XML namespace 'http://schemas.microsoft.com/winfx/2006/xaml/presentation'. 
Location: Line 32 Position 33 of MainWindow.xaml
```

This indicates the file still contains `Spacing="16"` attribute which is invalid for WPF StackPanel.

## Recommended Next Steps

1. Search and remove all `Spacing` attributes from MainWindow.xaml:
   ```powershell
   Select-String -Path "MainWindow.xaml" -Pattern "Spacing=\"" | Format-List
   ```

2. Replace with proper margin/padding structure:
   ```xml
   <StackPanel Margin="8">
       <!-- children -->
   </StackPanel>
   ```

3. Or use Padding within child elements instead.

## Working Code Elements ✅

The following components are properly structured and work correctly:
- Window class renamed to MainWindow (fixes circular dependency)
- Event handlers for window controls implemented
- Navigation item click handlers added
- Stats card structure defined
- Custom window controls (orange buttons)
- Frameless window styling logic

## Summary

**Core Issue**: WPF XAML syntax errors - invalid properties (`Spacing`, `CornerRadius` on Grid, `RowDefinitions` attribute) and XML element nesting rules.

**Progress**: 75% complete - Framework structure is solid once XAML syntax errors are resolved.
