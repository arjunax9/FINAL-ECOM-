Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " Deploying Kubernetes Manifests in Podman" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# 1. Check if podman is installed
if (-not (Get-Command podman -ErrorAction SilentlyContinue)) {
    Write-Host "Error: Podman is not detected in your PATH." -ForegroundColor Red
    exit 1
}

# 2. Ensure Podman machine is running
podman info >$null 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Podman machine is not running. Starting..." -ForegroundColor Yellow
    $startOut = podman machine start 2>&1
    if ($LASTEXITCODE -ne 0 -and $startOut -notmatch "already running") {
        Write-Host "Failed to start Podman machine: $startOut" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

# 3. Always run from repository root where k8s folder is located
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Set-Location $repoRoot

# 4. Ensure local image tags match Kubernetes manifest expectations
Write-Host "`nEnsuring local image tags..." -ForegroundColor Yellow
$tagPairs = @(
    @("ecommerceapi-auth-service:latest", "ecommerce-auth-service:latest"),
    @("ecommerceapi-product-service:latest", "ecommerce-product-service:latest"),
    @("ecommerceapi-order-service:latest", "ecommerce-order-service:latest"),
    @("ecommerceapi-ai-service:latest", "ecommerce-ai-service:latest"),
    @("ecommerceapi-api-gateway:latest", "ecommerce-api-gateway:latest"),
    @("ecommerceapi-frontend:latest", "ecommerce-frontend:latest")
)
foreach ($pair in $tagPairs) {
    if (podman image exists $pair[0]) {
        podman tag $pair[0] $pair[1] 2>$null
        podman tag $pair[0] "localhost/$($pair[1])" 2>$null
    }
}

# 5. Load Secrets into Podman
Write-Host "`n1. Applying Kubernetes Secrets..." -ForegroundColor Green
podman kube play k8s/secrets.yaml

# 6. Deploy Microservices Pods using K8s Manifests with ports published
Write-Host "`n2. Deploying SQL Server Pod..." -ForegroundColor Green
podman kube play --replace --configmap k8s/configmap.yaml --publish-all k8s/sqlserver.yaml

Write-Host "`n3. Deploying Auth Service Pod..." -ForegroundColor Green
podman kube play --replace --configmap k8s/configmap.yaml --publish-all k8s/auth-service.yaml

Write-Host "`n4. Deploying Product Service Pod..." -ForegroundColor Green
podman kube play --replace --configmap k8s/configmap.yaml --publish-all k8s/product-service.yaml

Write-Host "`n5. Deploying Order Service Pod..." -ForegroundColor Green
podman kube play --replace --configmap k8s/configmap.yaml --publish-all k8s/order-service.yaml

Write-Host "`n6. Deploying AI Service Pod..." -ForegroundColor Green
podman kube play --replace --configmap k8s/configmap.yaml --publish-all k8s/ai-service.yaml

Write-Host "`n7. Deploying API Gateway Pod..." -ForegroundColor Green
podman kube play --replace --configmap k8s/configmap.yaml --publish-all k8s/api-gateway.yaml

Write-Host "`n8. Deploying Frontend Pod..." -ForegroundColor Green
podman image exists "ecommerce-frontend:latest" 2>$null
$hasFrontend = ($LASTEXITCODE -eq 0)
if (-not $hasFrontend) {
    podman image exists "localhost/ecommerce-frontend:latest" 2>$null
    $hasFrontend = ($LASTEXITCODE -eq 0)
}
if ($hasFrontend) {
    podman kube play --replace --configmap k8s/configmap.yaml --publish-all k8s/frontend.yaml
} else {
    Write-Host "Notice: ecommerce-frontend image is not found, skipping." -ForegroundColor Gray
}

Write-Host "`n==========================================" -ForegroundColor Cyan
Write-Host " All Pods Deployed via Kubernetes in Podman" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Cyan
podman pod ls

# Detect WSL Podman VM IP for direct browser access
$vmIp = $null
try {
    $rawIp = podman machine ssh -- "ip addr show eth0 2>/dev/null | grep -oP '(?<=inet\s)\d+(\.\d+){3}'" 2>$null
    if ($rawIp) { $vmIp = ($rawIp.Trim() -split "`r?`n")[0].Trim() }
} catch {}

Write-Host "`nAccess Endpoints:" -ForegroundColor Cyan
if ($vmIp) {
    Write-Host "  Frontend (UI)       : http://${vmIp}:80 (or http://localhost:80)" -ForegroundColor White
    Write-Host "  API Gateway         : http://${vmIp}:5000 (or http://localhost:5000)" -ForegroundColor White
    Write-Host "  Auth Service Swagger: http://${vmIp}:5001/swagger" -ForegroundColor White
    Write-Host "  Product Service     : http://${vmIp}:5002/swagger" -ForegroundColor White
    Write-Host "  Order Service       : http://${vmIp}:5003/swagger" -ForegroundColor White
    Write-Host "  AI Service Swagger  : http://${vmIp}:5004/swagger" -ForegroundColor White
} else {
    Write-Host "  Frontend (UI)       : http://localhost:80" -ForegroundColor White
    Write-Host "  API Gateway         : http://localhost:5000" -ForegroundColor White
    Write-Host "  Auth Service Swagger: http://localhost:5001/swagger" -ForegroundColor White
    Write-Host "  Product Service     : http://localhost:5002/swagger" -ForegroundColor White
    Write-Host "  Order Service       : http://localhost:5003/swagger" -ForegroundColor White
    Write-Host "  AI Service Swagger  : http://localhost:5004/swagger" -ForegroundColor White
}
