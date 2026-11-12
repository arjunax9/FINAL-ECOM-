using ECommerce.Models.Entities;

namespace ECommerce.AuthService.Services;

public interface IAuthService
{
    Task<User> RegisterAsync(User user);
    Task<string?> LoginAsync(string email, string password);
}
