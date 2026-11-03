@echo off
setlocal

:: ==================================================
:: SCIP Dashboard WPF - Rebuild and launch the resizable dashboard
:: Builds the current source, then starts that build's executable.
:: ==================================================

set "BUILD_DIR=%TEMP%\SCIP-Dashboard-%RANDOM%-%RANDOM%"

dotnet build "%~dp0ScipDashboard.WPF\ScipDashboard.WPF.csproj" --configuration Release --output "%BUILD_DIR%"
if errorlevel 1 goto build_failed

start "" "%BUILD_DIR%\ScipDashboard.WPF.exe"
exit /b 0

:build_failed
echo.
echo SCIP Dashboard build failed. The app was not started.
pause
exit /b 1
