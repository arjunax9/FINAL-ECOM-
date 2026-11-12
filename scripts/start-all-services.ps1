Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Starting E-Commerce Microservices Suite" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$services = @(
    @{ Name = "Auth Service"; Path = "Services\ECommerce.AuthService\ECommerce.AuthService.csproj"; Port = "5001" },
    @{ Name = "Product Service"; Path = "Services\ECommerce.ProductService\ECommerce.ProductService.csproj"; Port = "5002" },
    @{ Name = "Order Service"; Path = "Services\ECommerce.OrderService\ECommerce.OrderService.csproj"; Port = "5003" },
    @{ Name = "AI Service"; Path = "Services\ECommerce.AIService\ECommerce.AIService.csproj"; Port = "5004" },
    @{ Name = "API Gateway"; Path = "Services\ECommerce.ApiGateway\ECommerce.ApiGateway.csproj"; Port = "5000" }
)

foreach ($svc in $services) {
    Write-Host "Launching $($svc.Name) on port $($svc.Port)..." -ForegroundColor Green
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "Write-Host 'Starting $($svc.Name)...' -ForegroundColor Cyan; dotnet run --project $($svc.Path)"
}

Write-Host "`nAll microservices and Gateway launched!" -ForegroundColor Yellow
Write-Host "API Gateway: http://localhost:5000" -ForegroundColor White
Write-Host "Auth Service Swagger: http://localhost:5001/swagger" -ForegroundColor White
Write-Host "Product Service Swagger: http://localhost:5002/swagger" -ForegroundColor White
Write-Host "Order Service Swagger: http://localhost:5003/swagger" -ForegroundColor White
Write-Host "AI Service Swagger: http://localhost:5004/swagger" -ForegroundColor White
