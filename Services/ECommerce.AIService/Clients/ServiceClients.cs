using System.Text.Json;
using ECommerce.AIService.Configuration;
using ECommerce.AIService.Models;
using ECommerce.Models.DTOs;
using ECommerce.Models.Entities;
using Microsoft.Extensions.Options;

namespace ECommerce.AIService.Clients;

public interface IProductServiceClient
{
    Task<List<ProductCatalogItem>> GetAllProductsAsync(CancellationToken cancellationToken = default);
    Task<ProductCatalogItem?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default);
}

public class ProductServiceClient : IProductServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ServiceUrlsOptions _urls;
    private readonly ILogger<ProductServiceClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ProductServiceClient(
        HttpClient httpClient,
        IOptions<ServiceUrlsOptions> urls,
        ILogger<ProductServiceClient> logger)
    {
        _httpClient = httpClient;
        _urls = urls.Value;
        _logger = logger;
    }

    public async Task<List<ProductCatalogItem>> GetAllProductsAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{_urls.ProductServiceUrl.TrimEnd('/')}/api/products";
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var response = await _httpClient.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var items = JsonSerializer.Deserialize<List<ProductCatalogItem>>(content, JsonOptions);
                    if (items != null && items.Count > 0)
                    {
                        return items;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Attempt {Attempt}/3: Failed to fetch products from ProductService ({Url})", attempt, url);
                if (attempt < 3)
                {
                    try { await Task.Delay(2000, cancellationToken); } catch { }
                }
            }
        }

        _logger.LogWarning("All attempts to query ProductService failed, using fallback catalogue");
        return FallbackProducts();
    }

    public async Task<ProductCatalogItem?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_urls.ProductServiceUrl.TrimEnd('/')}/api/products/{id}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<ProductCatalogItem>(content, JsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch product #{Id} from ProductService", id);
        }

        return FallbackProducts().FirstOrDefault(p => p.Id == id);
    }

    public static List<ProductCatalogItem> FallbackProducts() => new()
    {
        new ProductCatalogItem { Id = 1, Name = "Wireless Noise-Canceling Headphones", Description = "High-fidelity audio with active noise cancellation and 30-hour battery life.", Price = 199.99m, Category = "Electronics", Quantity = 25 },
        new ProductCatalogItem { Id = 2, Name = "Mechanical Gaming Keyboard", Description = "RGB backlit mechanical keyboard with tactile blue switches and detachable wrist rest.", Price = 89.99m, Category = "Electronics", Quantity = 40 },
        new ProductCatalogItem { Id = 3, Name = "Ultra-Wide 4K Gaming Monitor", Description = "34-inch curved 144Hz HDR monitor with 1ms response time and G-Sync compatibility.", Price = 499.99m, Category = "Electronics", Quantity = 12 },
        new ProductCatalogItem { Id = 4, Name = "Ergonomic Office Chair", Description = "Breathable mesh chair with adjustable lumbar support, 3D armrests, and 135-degree recline.", Price = 249.99m, Category = "Furniture", Quantity = 15 },
        new ProductCatalogItem { Id = 5, Name = "Stainless Steel Water Bottle", Description = "Double-wall vacuum insulated water bottle keeping liquids ice cold for 24 hours.", Price = 29.99m, Category = "Home & Kitchen", Quantity = 60 },
        new ProductCatalogItem { Id = 6, Name = "Smart Fitness Watch", Description = "Waterproof smartwatch with continuous heart rate monitoring, GPS tracking, and sleep analysis.", Price = 149.99m, Category = "Electronics", Quantity = 30 }
    };
}

public interface IOrderServiceClient
{
    Task<Order?> GetOrderByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Order>> GetOrdersByUserIdAsync(int userId, CancellationToken cancellationToken = default);
}

public class OrderServiceClient : IOrderServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ServiceUrlsOptions _urls;
    private readonly ILogger<OrderServiceClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public OrderServiceClient(
        HttpClient httpClient,
        IOptions<ServiceUrlsOptions> urls,
        ILogger<OrderServiceClient> logger)
    {
        _httpClient = httpClient;
        _urls = urls.Value;
        _logger = logger;
    }

    public async Task<Order?> GetOrderByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_urls.OrderServiceUrl.TrimEnd('/')}/api/orders/{id}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<Order>(content, JsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query OrderService for order #{Id}", id);
        }

        return null;
    }

    public async Task<List<Order>> GetOrdersByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_urls.OrderServiceUrl.TrimEnd('/')}/api/orders/user/{userId}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<List<Order>>(content, JsonOptions) ?? new();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query OrderService for user #{UserId} orders", userId);
        }

        return new List<Order>();
    }
}
