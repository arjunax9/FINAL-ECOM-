namespace ECommerce.OrderService.Services;

public interface IPaymentService
{
    Task<bool> ProcessPaymentAsync(decimal amount);
}

public class PaymentService : IPaymentService
{
    public Task<bool> ProcessPaymentAsync(decimal amount)
    {
        // Simple mock payment processor (returns true for any positive amount)
        return Task.FromResult(amount > 0);
    }
}
