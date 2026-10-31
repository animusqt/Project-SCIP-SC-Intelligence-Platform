# SCIP Dashboard - XAML Fix Summary

**Date**: 2026-10-28  
**Status**: READY TO FIX - All syntax errors identified, here's what to do

---

## Quick Fix: Remove Invalid Properties

### Error 1: Line 64 - Spacing Property
```xml
<!-- REMOVE THIS ENTIRE LINE -->
<StackPanel Spacing="16" Margin="16,0,16,8">
```

Replace with:
```xml
<StackPanel Margin="16,0,16,8">
    <Border Padding="8">
        <TextBlock Text="Item 1"/>
    </Border>
</StackPanel>
```

### Error 2: Line 75 - Border Already Has Child
The problem is nested borders with duplicate children. Use this structure:

```xml
<!-- CORRECT STRUCTURE FOR STATS GRID -->
<StackPanel Orientation="Horizontal" Spacing="16"> <!-- Note: Spacing is on StackPanel itself, not as attribute -->
    <Border CornerRadius="8" Padding="16" Width="160" Height="120" Background="#e3f2fd">
        <TextBlock Text="Active Hauls" HorizontalAlignment="Center" VerticalAlignment="Top"/>
        <TextBlock Text="0" HorizontalAlignment="Center" VerticalAlignment="Bottom"/>
    </Border>
    
    <!-- Other cards... -->
</StackPanel>
```

---

## Alternative: Use Margin Instead of Spacing for StackPanel
In WPF, use `Margin` instead of non-existent `Spacing`:

```xml
<StackPanel>
    <Border Padding="4">
        ...
    </Border>
    <Margin Height="16"/> <!-- Add explicit spacing between items -->
    <Border Padding="4">
        ...
    </Border>
</StackPanel>
```

---

## Final Command to Test
After applying fixes:
```powershell
cd "d:\SCIP Projects\SCIP Dashboard\scip_overlay_v2"
dotnet run --project ScipDashboard.WPF --configuration Release
```

Expected: Native WPF window opens with dashboard UI.
