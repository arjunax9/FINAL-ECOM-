# stop-demo.ps1
# Cleanly stops all port forwarders, Kubernetes cluster, and the Podman VM

Write-Host "==========================================================" -ForegroundColor Yellow
Write-Host "   Stopping Apex E-Commerce Kubernetes Environment        " -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Yellow

# 1. Kill any active port-forwarding processes
Write-Host "`n[1/3] Terminating port-forward processes..." -ForegroundColor Gray
Stop-Process -Name kubectl -Force -ErrorAction SilentlyContinue
Get-Process powershell -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowTitle -match "Port-Forward" } | Stop-Process -Force -ErrorAction SilentlyContinue

# 2. Stop the Kubernetes cluster container
Write-Host "[2/3] Pausing Kubernetes cluster container..." -ForegroundColor Gray
podman stop ecommerce-cluster-control-plane 2>$null

# 3. Stop the Podman machine to free host CPU & RAM
Write-Host "[3/3] Stopping Podman VM..." -ForegroundColor Gray
podman machine stop

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "   All services stopped cleanly! RAM and CPU freed.       " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
