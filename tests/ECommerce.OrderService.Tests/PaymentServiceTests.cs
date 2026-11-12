using ECommerce.OrderService.Services;
using Xunit;

namespace ECommerce.OrderService.Tests;

public class PaymentServiceTests
{
    private readonly PaymentService _paymentService = new();

    [Theory]
    [InlineData(0.01)]
    [InlineData(100.50)]
    [InlineData(99999.99)]
    public async Task ProcessPaymentAsync_PositiveAmount_ReturnsTrue(decimal amount)
    {
        var result = await _paymentService.ProcessPaymentAsync(amount);
        Assert.True(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50.0)]
    public async Task ProcessPaymentAsync_ZeroOrNegativeAmount_ReturnsFalse(decimal amount)
    {
        var result = await _paymentService.ProcessPaymentAsync(amount);
        Assert.False(result);
    }
}
