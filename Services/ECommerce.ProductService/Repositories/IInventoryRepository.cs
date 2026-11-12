using ECommerce.Models.Entities;

namespace ECommerce.ProductService.Repositories;

public interface IInventoryRepository
{
    Task<Inventory?> GetByProductIdAsync(int productId);
    Task AddAsync(Inventory inventory);
    Task UpdateAsync(Inventory inventory);
}
