# start-demo.ps1
# All-In-One Startup Script for Apex E-Commerce Demo

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   Starting Apex E-Commerce Kubernetes Demo Environment   " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Ensure Podman Machine is Running
Write-Host "`n[1/5] Checking Podman VM..." -ForegroundColor Yellow
$vmRunning = podman machine list | Select-String "running"
if (-not $vmRunning) {
    Write-Host "Starting Podman Machine (WSL)..." -ForegroundColor Gray
    podman machine start
} else {
    Write-Host "Podman Machine is active." -ForegroundColor Green
}

# 2. Ensure Kind Cluster Container is Started
Write-Host "`n[2/5] Checking Kubernetes Cluster..." -ForegroundColor Yellow
$nodeRunning = podman ps --filter "name=ecommerce-cluster-control-plane" -q
if (-not $nodeRunning) {
    $nodeExited = podman ps -a --filter "name=ecommerce-cluster-control-plane" -q
    if ($nodeExited) {
        Write-Host "Starting existing Kind container..." -ForegroundColor Gray
        podman start ecommerce-cluster-control-plane | Out-Null
        Start-Sleep -Seconds 4
    } else {
        Write-Host "Creating Kind Cluster..." -ForegroundColor Gray
        kind create cluster --config ".\ECommerceAPI\scripts\kind-cluster-config.yaml"
    }
} else {
    Write-Host "Kind cluster node is active." -ForegroundColor Green
}

# 3. Synchronize Kubeconfig Server IP with WSL VM
Write-Host "`n[3/5] Syncing Kubernetes API connection..." -ForegroundColor Yellow
$wslIp = (wsl -d podman-machine-default ip -4 addr show eth0 | Select-String "inet (\d+\.\d+\.\d+\.\d+)").Matches.Groups[1].Value
if ($wslIp) {
    kubectl config set-cluster kind-ecommerce-cluster --server="https://${wslIp}:6443" --insecure-skip-tls-verify=true | Out-Null
    Write-Host "Kubernetes API connected via https://${wslIp}:6443" -ForegroundColor Green
}

# 4. Check & Apply Kubernetes Pods
Write-Host "`n[4/5] Checking Pods in 'ecommerce' namespace..." -ForegroundColor Yellow
$runningPods = (kubectl get pods -n ecommerce --no-headers 2>$null | Where-Object { $_ -match "1/1\s+Running" } | Measure-Object).Count
if ($runningPods -lt 12) {
    Write-Host "Applying Kubernetes manifests..." -ForegroundColor Gray
    kubectl apply -k ".\ECommerceAPI\k8s"
    Write-Host "Waiting for pods to be ready..." -ForegroundColor Gray
    Start-Sleep -Seconds 5
}
$readyPods = (kubectl get pods -n ecommerce --no-headers 2>$null | Where-Object { $_ -match "1/1\s+Running" } | Measure-Object).Count
Write-Host "Running Pods: $readyPods / 12 ready." -ForegroundColor Green

# 5. Launch Persistent Port Forwarders & Open Browser
Write-Host "`n[5/5] Launching Port Forwarders..." -ForegroundColor Yellow
Stop-Process -Name kubectl -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1
& ".\ECommerceAPI\scripts\start-port-forwards.ps1"

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host "   Environment Ready! Opening http://localhost:4200       " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Cyan
Start-Sleep -Seconds 2
Start-Process "http://localhost:4200"
