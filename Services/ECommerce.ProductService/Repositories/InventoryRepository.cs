using ECommerce.Models.Entities;
using ECommerce.ProductService.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.ProductService.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly ProductDbContext _context;

    public InventoryRepository(ProductDbContext context)
    {
        _context = context;
    }

    public async Task<Inventory?> GetByProductIdAsync(int productId)
    {
        return await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId);
    }

    public async Task AddAsync(Inventory inventory)
    {
        _context.Inventories.Add(inventory);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Inventory inventory)
    {
        _context.Inventories.Update(inventory);
        await _context.SaveChangesAsync();
    }
}
