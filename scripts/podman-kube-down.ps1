Write-Host "==========================================" -ForegroundColor Yellow
Write-Host " Tearing Down Kubernetes Pods in Podman  " -ForegroundColor Yellow
Write-Host "==========================================" -ForegroundColor Yellow

# 1. Ensure Podman is responsive
if (Get-Command podman -ErrorAction SilentlyContinue) {
    podman info >$null 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Podman machine is not running. Starting..." -ForegroundColor Yellow
        podman machine start 2>$null
    }
}

$pods = @(
    "ecommerce-frontend-pod",
    "ecommerce-api-gateway-pod",
    "ecommerce-ai-service-pod",
    "ecommerce-order-service-pod",
    "ecommerce-product-service-pod",
    "ecommerce-auth-service-pod",
    "ecommerce-sqlserver-pod",
    # Legacy fallback names
    "frontend-pod",
    "api-gateway-pod",
    "ai-service-pod",
    "order-service-pod",
    "product-service-pod",
    "auth-service-pod",
    "sqlserver-pod"
)

foreach ($p in $pods) {
    podman pod exists $p 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Removing pod $p..." -ForegroundColor Gray
        podman pod rm -f $p 2>$null
    }
}

Write-Host "All Kubernetes pods removed from Podman." -ForegroundColor Green
podman pod ls
