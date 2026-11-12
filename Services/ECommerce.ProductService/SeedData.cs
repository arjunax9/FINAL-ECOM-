using ECommerce.Models.Entities;
using ECommerce.ProductService.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.ProductService;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

        await context.Database.EnsureCreatedAsync();

        if (!await context.Products.AnyAsync())
        {
            var p1 = new Product
            {
                Name = "Wireless Noise-Canceling Headphones",
                Description = "High-fidelity audio with active noise cancellation and 30-hour battery life.",
                Price = 199.99m,
                Category = "Electronics"
            };
            var p2 = new Product
            {
                Name = "Ergonomic Mechanical Keyboard",
                Description = "Custom mechanical switches with customizable RGB backlighting.",
                Price = 89.99m,
                Category = "Accessories"
            };
            var p3 = new Product
            {
                Name = "Ultra HD 4K Monitor 27-inch",
                Description = "IPS display with 144Hz refresh rate and HDR400 support.",
                Price = 349.50m,
                Category = "Electronics"
            };

            context.Products.AddRange(p1, p2, p3);
            await context.SaveChangesAsync();

            context.Inventories.AddRange(
                new Inventory { ProductId = p1.Id, Quantity = 25 },
                new Inventory { ProductId = p2.Id, Quantity = 40 },
                new Inventory { ProductId = p3.Id, Quantity = 15 }
            );
            await context.SaveChangesAsync();
        }
    }
}
