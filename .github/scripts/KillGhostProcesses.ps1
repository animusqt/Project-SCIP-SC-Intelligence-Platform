# Kill all background ghost processes for SCIP Dashboard

# Stop Python processes (scanner-engine, modules, server)
Stop-Process -Name python -Force -ErrorAction SilentlyContinue

# Stop Node.js/Electron processes  
Stop-Process -Name node -Force -ErrorAction SilentlyContinue
Stop-Process -Name electron -Force -ErrorAction SilentlyContinue

# Stop .NET background processes (WPF, Windows Forms builds)
Stop-Process -Name dotnet -Force -ErrorAction SilentlyContinue

# Stop any remaining suspicious processes
Get-Process | Where-Object { $_.Path -like "*scip*" -or $_.Path -like "*node*" -or $_.Path -like "*electron*" } | Stop-Process -Force

Write-Host "All SCIP Dashboard background processes terminated."