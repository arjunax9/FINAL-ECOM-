using ECommerce.Models.DTOs;
using ECommerce.Models.Entities;
using ECommerce.OrderService.Clients;
using ECommerce.OrderService.Hubs;
using ECommerce.OrderService.Repositories;
using ECommerce.OrderService.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ECommerce.OrderService.Tests;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepo;
    private readonly Mock<IProductCatalogClient> _mockProductClient;
    private readonly Mock<IPaymentService> _mockPaymentService;
    private readonly Mock<IHubContext<OrderHub>> _mockHubContext;
    private readonly Mock<ILogger<Services.OrderService>> _mockLogger;
    private readonly Services.OrderService _orderService;

    public OrderServiceTests()
    {
        _mockOrderRepo = new Mock<IOrderRepository>();
        _mockProductClient = new Mock<IProductCatalogClient>();
        _mockPaymentService = new Mock<IPaymentService>();
        _mockHubContext = new Mock<IHubContext<OrderHub>>();
        _mockLogger = new Mock<ILogger<Services.OrderService>>();

        // Setup mock Hub clients for SignalR broadcast
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        _orderService = new Services.OrderService(
            _mockOrderRepo.Object,
            _mockProductClient.Object,
            _mockPaymentService.Object,
            _mockHubContext.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CreateOrderAsync_ValidItemsAndStock_CreatesPendingOrderWithTotal()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            UserId = 1,
            Items = new List<OrderItemRequest>
            {
                new OrderItemRequest { ProductId = 10, Quantity = 2 }
            }
        };

        var catalogItem = new ProductCatalogItem
        {
            Id = 10,
            Name = "Mechanical Keyboard",
            Price = 100.0m,
            Quantity = 20
        };

        _mockProductClient.Setup(c => c.GetProductByIdAsync(10)).ReturnsAsync(catalogItem);
        _mockProductClient.Setup(c => c.DeductStockAsync(10, 2)).ReturnsAsync(true);
        _mockOrderRepo.Setup(r => r.AddAsync(It.IsAny<Order>())).ReturnsAsync((Order o) => { o.Id = 123; return o; });

        // Act
        var result = await _orderService.CreateOrderAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(123, result.Id);
        Assert.Equal("Pending", result.Status);
        Assert.Equal(200.0m, result.TotalAmount); // 100 * 2
        Assert.Single(result.OrderItems);
        _mockProductClient.Verify(c => c.DeductStockAsync(10, 2), Times.Once);
        _mockOrderRepo.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_ProductNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            UserId = 1,
            Items = new List<OrderItemRequest> { new OrderItemRequest { ProductId = 999, Quantity = 1 } }
        };

        _mockProductClient.Setup(c => c.GetProductByIdAsync(999)).ReturnsAsync((ProductCatalogItem?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _orderService.CreateOrderAsync(request));
        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
        _mockOrderRepo.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrderAsync_InsufficientStock_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            UserId = 1,
            Items = new List<OrderItemRequest> { new OrderItemRequest { ProductId = 5, Quantity = 10 } }
        };

        var item = new ProductCatalogItem { Id = 5, Name = "Mouse", Price = 25m, Quantity = 2 };
        _mockProductClient.Setup(c => c.GetProductByIdAsync(5)).ReturnsAsync(item);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _orderService.CreateOrderAsync(request));
        Assert.Contains("Insufficient stock", ex.Message);
        _mockProductClient.Verify(c => c.DeductStockAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CheckoutAsync_ExistingOrder_UpdatesStatusToCheckedOut()
    {
        // Arrange
        var order = new Order { Id = 1, Status = "Pending" };
        _mockOrderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _mockOrderRepo.Setup(r => r.UpdateAsync(order)).Returns(Task.CompletedTask);

        // Act
        await _orderService.CheckoutAsync(1);

        // Assert
        Assert.Equal("CheckedOut", order.Status);
        _mockOrderRepo.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_PaymentSucceeds_MarksOrderAsPaid()
    {
        // Arrange
        var order = new Order { Id = 1, Status = "CheckedOut", TotalAmount = 150m };
        _mockOrderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _mockPaymentService.Setup(p => p.ProcessPaymentAsync(150m)).ReturnsAsync(true);
        _mockOrderRepo.Setup(r => r.UpdateAsync(order)).Returns(Task.CompletedTask);

        // Act
        var result = await _orderService.ProcessPaymentAsync(1);

        // Assert
        Assert.True(result);
        Assert.Equal("Paid", order.Status);
        _mockOrderRepo.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_PaymentFails_KeepsStatusUnchanged()
    {
        // Arrange
        var order = new Order { Id = 1, Status = "CheckedOut", TotalAmount = 50m };
        _mockOrderRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(order);
        _mockPaymentService.Setup(p => p.ProcessPaymentAsync(50m)).ReturnsAsync(false);

        // Act
        var result = await _orderService.ProcessPaymentAsync(1);

        // Assert
        Assert.False(result);
        Assert.Equal("CheckedOut", order.Status);
        _mockOrderRepo.Verify(r => r.UpdateAsync(order), Times.Never);
    }

    [Fact]
    public async Task ApproveOrderAsync_ValidOrder_MarksAsPaid()
    {
        // Arrange
        var order = new Order { Id = 10, Status = "CheckedOut" };
        _mockOrderRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(order);
        _mockOrderRepo.Setup(r => r.UpdateAsync(order)).Returns(Task.CompletedTask);

        // Act
        var result = await _orderService.ApproveOrderAsync(10);

        // Assert
        Assert.Equal("Paid", result.Status);
    }
}
