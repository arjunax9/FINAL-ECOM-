# Detect WSL Podman VM IP
$vmIp = $null
try {
    $rawIp = podman machine ssh -- "ip addr show eth0 2>/dev/null | grep -oP '(?<=inet\s)\d+(\.\d+){3}'" 2>$null
    if ($rawIp) { $vmIp = ($rawIp.Trim() -split "`r?`n")[0].Trim() }
} catch {}

if ($vmIp) {
    $env:DOCKER_HOST = "tcp://${vmIp}:2375"
} elseif (-not $env:DOCKER_HOST) {
    $env:DOCKER_HOST = "tcp://127.0.0.1:2375"
}

Write-Host "Stopping and removing all Podman containers..." -ForegroundColor Yellow
podman compose down
Write-Host "All containers stopped successfully." -ForegroundColor Green
