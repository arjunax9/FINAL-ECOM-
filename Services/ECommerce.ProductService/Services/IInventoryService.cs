namespace ECommerce.ProductService.Services;

public interface IInventoryService
{
    Task<int> GetQuantityAsync(int productId);
    Task UpdateQuantityAsync(int productId, int quantity);
    Task<bool> DeductStockAsync(int productId, int quantity);
}
