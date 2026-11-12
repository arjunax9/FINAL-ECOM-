using ECommerce.Models.Entities;

namespace ECommerce.AuthService.Repositories;

public interface IUserRepository
{
    Task<User> AddAsync(User user);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(int id);
    Task<List<User>> GetAllAsync();
    Task UpdateAsync(User user);
    Task DeleteAsync(int id);
}
