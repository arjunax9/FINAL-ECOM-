using ECommerce.AuthService.Data;
using ECommerce.AuthService.Services;
using ECommerce.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.AuthService;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        await context.Database.EnsureCreatedAsync();

        // Seed demo customer user if not present
        if (!await context.Users.AnyAsync(u => u.Email == "demo@local"))
        {
            context.Users.Add(new User
            {
                Name = "Demo Customer",
                Email = "demo@local",
                Password = PasswordHasher.Hash("Password123"),
                Role = "Customer"
            });
        }

        // Seed admin user if not present
        if (!await context.Users.AnyAsync(u => u.Email == "admin@local"))
        {
            context.Users.Add(new User
            {
                Name = "Administrator",
                Email = "admin@local",
                Password = PasswordHasher.Hash("Admin123!"),
                Role = "Admin"
            });
        }

        await context.SaveChangesAsync();
    }
}
