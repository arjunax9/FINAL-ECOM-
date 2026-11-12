Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " Building & Starting Services with Podman" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Check if podman is installed
if (-not (Get-Command podman -ErrorAction SilentlyContinue)) {
    Write-Host "Error: Podman is not detected in your PATH." -ForegroundColor Red
    Write-Host "To install Podman on Windows, run:" -ForegroundColor Yellow
    Write-Host "  winget install RedHat.Podman-Desktop" -ForegroundColor White
    Write-Host "or download from: https://podman-desktop.io" -ForegroundColor White
    exit 1
}

# Ensure Podman machine is responsive
podman info >$null 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Podman machine is not running. Starting..." -ForegroundColor Yellow
    $startOut = podman machine start 2>&1
    if ($LASTEXITCODE -ne 0 -and $startOut -notmatch "already running") {
        Write-Host "Failed to start Podman machine: $startOut" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

# Detect WSL Podman VM IP
$vmIp = $null
try {
    $rawIp = podman machine ssh -- "ip addr show eth0 2>/dev/null | grep -oP '(?<=inet\s)\d+(\.\d+){3}'" 2>$null
    if ($rawIp) { $vmIp = ($rawIp.Trim() -split "`r?`n")[0].Trim() }
} catch {}

# Ensure Podman API TCP service is active inside VM
try {
    podman machine ssh -- "sudo systemctl is-active podman-tcp.service 2>/dev/null || nohup podman system service --time=0 tcp://0.0.0.0:2375 </dev/null >/dev/null 2>&1 &" 2>$null
} catch {}

# Configure DOCKER_HOST for compose provider (TCP avoids Windows named pipe forwarding issues)
if ($vmIp) {
    $env:DOCKER_HOST = "tcp://${vmIp}:2375"
} elseif (-not $env:DOCKER_HOST) {
    $env:DOCKER_HOST = "tcp://127.0.0.1:2375"
}

# Cleanly down existing containers to prevent "container name already in use" collisions in Podman
Write-Host "Ensuring clean container state..." -ForegroundColor Gray
podman compose down 2>$null

# Run Podman Compose to build and launch all containers
podman compose up --build -d

if ($LASTEXITCODE -ne 0) {
    Write-Host "`nFailed to start containers. Please review the error above." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "`nAll Microservices are now running in Podman containers!" -ForegroundColor Green

# Detect WSL Podman VM IP for direct browser access if required
$vmIp = $null
try {
    $rawIp = podman machine ssh -- "ip addr show eth0 2>/dev/null | grep -oP '(?<=inet\s)\d+(\.\d+){3}'" 2>$null
    if ($rawIp) { $vmIp = ($rawIp.Trim() -split "`r?`n")[0].Trim() }
} catch {}

Write-Host "`nAccess Endpoints:" -ForegroundColor Cyan
if ($vmIp) {
    Write-Host "  Frontend (UI)       : http://${vmIp}:4200 (or http://localhost:4200)" -ForegroundColor White
    Write-Host "  API Gateway         : http://${vmIp}:5000 (or http://localhost:5000)" -ForegroundColor White
    Write-Host "  Auth Service Swagger: http://${vmIp}:5001/swagger" -ForegroundColor White
    Write-Host "  Product Service     : http://${vmIp}:5002/swagger" -ForegroundColor White
    Write-Host "  Order Service       : http://${vmIp}:5003/swagger" -ForegroundColor White
    Write-Host "  AI Service Swagger  : http://${vmIp}:5004/swagger" -ForegroundColor White
} else {
    Write-Host "  Frontend (UI)       : http://localhost:4200" -ForegroundColor White
    Write-Host "  API Gateway         : http://localhost:5000" -ForegroundColor White
    Write-Host "  Auth Service Swagger: http://localhost:5001/swagger" -ForegroundColor White
    Write-Host "  Product Service     : http://localhost:5002/swagger" -ForegroundColor White
    Write-Host "  Order Service       : http://localhost:5003/swagger" -ForegroundColor White
    Write-Host "  AI Service Swagger  : http://localhost:5004/swagger" -ForegroundColor White
}


