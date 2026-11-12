[CmdletBinding()]
param(
    [string]$Namespace = "ecommerce",
    [switch]$Delete
)

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  Kubernetes Cluster Deployment Manager  " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# 1. Check for kubectl
if (-not (Get-Command kubectl -ErrorAction SilentlyContinue)) {
    Write-Host "Error: kubectl CLI is not found in your PATH." -ForegroundColor Red
    exit 1
}

# 2. Check cluster connection
Write-Host "`nChecking Kubernetes cluster connectivity..." -ForegroundColor Yellow
$currentContext = kubectl config current-context 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: No active Kubernetes context found. Please ensure your cluster (Docker Desktop, Minikube, Kind, AKS, EKS, etc.) is running." -ForegroundColor Red
    Write-Host "Details: $currentContext" -ForegroundColor Gray
    exit 1
}
Write-Host "Active Context: $currentContext" -ForegroundColor Green

$k8sDir = Join-Path $PSScriptRoot "..\k8s"

if ($Delete) {
    Write-Host "`nTearing down resources in namespace '$Namespace'..." -ForegroundColor Yellow
    kubectl delete -k $k8sDir --ignore-not-found=true
    Write-Host "Resources deleted." -ForegroundColor Green
    exit 0
}

# 3. Apply manifests via Kustomize
Write-Host "`nApplying Kubernetes manifests via Kustomize..." -ForegroundColor Green
kubectl apply -k $k8sDir
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to apply manifests. Check the errors above." -ForegroundColor Red
    exit $LASTEXITCODE
}

# 4. Wait for database deployment rollout
Write-Host "`nWaiting for SQL Server to be ready..." -ForegroundColor Yellow
kubectl rollout status deployment/ecommerce-sqlserver -n $Namespace --timeout=120s

# 5. Wait for microservices rollouts
$deployments = @(
    "ecommerce-auth-service",
    "ecommerce-product-service",
    "ecommerce-order-service",
    "ecommerce-ai-service",
    "ecommerce-api-gateway",
    "ecommerce-frontend"
)

foreach ($dep in $deployments) {
    Write-Host "`nChecking rollout status for $dep..." -ForegroundColor Yellow
    kubectl rollout status deployment/$dep -n $Namespace --timeout=120s
}

# 6. Display cluster status
Write-Host "`n==========================================" -ForegroundColor Cyan
Write-Host "     Deployment Summary ($Namespace)      " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

Write-Host "`n--- Pods ---" -ForegroundColor Green
kubectl get pods -n $Namespace -o wide

Write-Host "`n--- Services ---" -ForegroundColor Green
kubectl get services -n $Namespace

Write-Host "`n--- Ingress ---" -ForegroundColor Green
kubectl get ingress -n $Namespace

Write-Host "`nDeployment completed successfully!" -ForegroundColor Green
Write-Host "To access the application locally without an Ingress controller, you can port-forward:" -ForegroundColor Cyan
Write-Host "  Frontend:    kubectl port-forward svc/ecommerce-frontend 4200:80 -n $Namespace" -ForegroundColor White
Write-Host "  API Gateway: kubectl port-forward svc/ecommerce-api-gateway 5000:5000 -n $Namespace" -ForegroundColor White

