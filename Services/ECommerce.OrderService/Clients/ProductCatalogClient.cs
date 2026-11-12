using System.Net.Http.Json;

namespace ECommerce.OrderService.Clients;

public class ProductCatalogClient : IProductCatalogClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductCatalogClient> _logger;

    public ProductCatalogClient(HttpClient httpClient, ILogger<ProductCatalogClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ProductCatalogItem?> GetProductByIdAsync(int productId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/products/{productId}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Product microservice returned {StatusCode} for product {ProductId}", response.StatusCode, productId);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ProductCatalogItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call Product microservice for product {ProductId}", productId);
            return null;
        }
    }

    public async Task<List<ProductCatalogItem>> GetAllProductsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/products");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Product microservice returned {StatusCode} for GetAllProducts", response.StatusCode);
                return new List<ProductCatalogItem>();
            }

            var items = await response.Content.ReadFromJsonAsync<List<ProductCatalogItem>>();
            return items ?? new List<ProductCatalogItem>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all products from Product microservice");
            return new List<ProductCatalogItem>();
        }
    }

    public async Task<bool> DeductStockAsync(int productId, int quantity)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/inventory/{productId}/deduct", quantity);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deduct stock from Product microservice for product {ProductId}", productId);
            return false;
        }
    }
}
