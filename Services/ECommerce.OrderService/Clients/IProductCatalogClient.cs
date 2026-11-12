namespace ECommerce.OrderService.Clients;

public class ProductCatalogItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public interface IProductCatalogClient
{
    Task<ProductCatalogItem?> GetProductByIdAsync(int productId);
    Task<List<ProductCatalogItem>> GetAllProductsAsync();
    Task<bool> DeductStockAsync(int productId, int quantity);
}
