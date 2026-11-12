# scripts/start-port-forwards.ps1
# Keeps frontend (:4200), api-gateway (:5000), and sqlserver (:1433) continuously running with auto-reconnect

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   Starting Apex E-Commerce Kubernetes Port Forwarders     " -ForegroundColor Cyan
Write-Host "   Frontend:    http://localhost:4200                      " -ForegroundColor Green
Write-Host "   API Gateway: http://localhost:5000                      " -ForegroundColor Green
Write-Host "   SQL Server:  localhost,1433                             " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Close any existing duplicate port-forward windows and processes
Write-Host "Closing any existing port-forward windows..." -ForegroundColor Gray
Get-Process powershell -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowTitle -match "Port-Forward" } | Stop-Process -Force -ErrorAction SilentlyContinue
Stop-Process -Name kubectl -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

# 2. Launch single clean set of persistent port forwarders
Start-Process powershell -ArgumentList "-NoExit", "-Command", "`$host.UI.RawUI.WindowTitle = 'Frontend Port-Forward (4200)'; while (`$true) { Write-Host 'Connecting Frontend :4200 -> :80...'; kubectl port-forward svc/frontend 4200:80 -n ecommerce --address 0.0.0.0,127.0.0.1; Start-Sleep -Seconds 1 }"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "`$host.UI.RawUI.WindowTitle = 'API Gateway Port-Forward (5000)'; while (`$true) { Write-Host 'Connecting API Gateway :5000 -> :5000...'; kubectl port-forward svc/api-gateway 5000:5000 -n ecommerce --address 0.0.0.0,127.0.0.1; Start-Sleep -Seconds 1 }"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "`$host.UI.RawUI.WindowTitle = 'SQL Server Port-Forward (1433)'; while (`$true) { Write-Host 'Connecting SQL Server :1433 -> :1433...'; kubectl port-forward svc/sqlserver 1433:1433 -n ecommerce --address 0.0.0.0,127.0.0.1; Start-Sleep -Seconds 1 }"

Write-Host "`nPort forwarders launched in dedicated persistent background windows." -ForegroundColor Yellow
Write-Host "You can now open http://localhost:4200 in your browser and connect to localhost,1433 in SSMS." -ForegroundColor Cyan
