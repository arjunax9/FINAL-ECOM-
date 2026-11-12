using ECommerce.Models.Entities;
using ECommerce.ProductService.Repositories;

namespace ECommerce.ProductService.Services;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _repository;

    public InventoryService(IInventoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<int> GetQuantityAsync(int productId)
    {
        var inv = await _repository.GetByProductIdAsync(productId);
        return inv?.Quantity ?? 0;
    }

    public async Task UpdateQuantityAsync(int productId, int quantity)
    {
        var inv = await _repository.GetByProductIdAsync(productId);
        if (inv == null)
        {
            inv = new Inventory { ProductId = productId, Quantity = quantity };
            await _repository.AddAsync(inv);
        }
        else
        {
            inv.Quantity = quantity;
            await _repository.UpdateAsync(inv);
        }
    }

    public async Task<bool> DeductStockAsync(int productId, int quantity)
    {
        var inv = await _repository.GetByProductIdAsync(productId);
        if (inv == null || inv.Quantity < quantity)
        {
            return false;
        }

        inv.Quantity -= quantity;
        await _repository.UpdateAsync(inv);
        return true;
    }
}
