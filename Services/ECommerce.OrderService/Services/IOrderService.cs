using ECommerce.Models.DTOs;
using ECommerce.Models.Entities;

namespace ECommerce.OrderService.Services;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(CreateOrderRequest request);
    Task<Order?> GetByIdAsync(int id);
    Task<List<Order>> GetByUserIdAsync(int userId);
    Task<List<Order>> GetAllAsync();
    Task CheckoutAsync(int id);
    Task<bool> ProcessPaymentAsync(int id);
    Task<Order> ApproveOrderAsync(int id);
}
