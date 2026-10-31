@echo off
setlocal enabledelayedexpansion

:: ==================================================
:: SCIP Dashboard WPF - Launcher for Borderless Window Mode
:: Builds the app (if needed) and starts it without a console window.
:: ==================================================

:: Start the compiled WinExe directly - no console window is shown.
start "" "%~dp0Publish\ScipDashboard.WPF.exe"

:: Exit immediately so this console window closes as the dashboard appears.
exit /b 0
