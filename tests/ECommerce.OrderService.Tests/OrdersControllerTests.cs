using ECommerce.Models.DTOs;
using ECommerce.Models.Entities;
using ECommerce.OrderService.Controllers;
using ECommerce.OrderService.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ECommerce.OrderService.Tests;

public class OrdersControllerTests
{
    private readonly Mock<IOrderService> _mockOrderService;
    private readonly OrdersController _controller;

    public OrdersControllerTests()
    {
        _mockOrderService = new Mock<IOrderService>();
        _controller = new OrdersController(_mockOrderService.Object);
    }

    [Fact]
    public async Task CreateOrder_ValidRequest_ReturnsCreatedAtAction()
    {
        // Arrange
        var request = new CreateOrderRequest { UserId = 1, Items = new List<OrderItemRequest>() };
        var createdOrder = new Order { Id = 101, UserId = 1, Status = "Pending", TotalAmount = 50m };
        _mockOrderService.Setup(s => s.CreateOrderAsync(request)).ReturnsAsync(createdOrder);

        // Act
        var result = await _controller.CreateOrder(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(101, createdResult.RouteValues?["id"]);
    }

    [Fact]
    public async Task CreateOrder_ServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateOrderRequest { UserId = 1, Items = new List<OrderItemRequest>() };
        _mockOrderService.Setup(s => s.CreateOrderAsync(request))
            .ThrowsAsync(new InvalidOperationException("Insufficient stock."));

        // Act
        var result = await _controller.CreateOrder(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task GetById_ExistingOrder_ReturnsOk()
    {
        // Arrange
        var order = new Order { Id = 1, Status = "Pending" };
        _mockOrderService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(order);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(order, okResult.Value);
    }

    [Fact]
    public async Task GetById_NonExistingOrder_ReturnsNotFound()
    {
        // Arrange
        _mockOrderService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((Order?)null);

        // Act
        var result = await _controller.GetById(99);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Checkout_ExistingOrder_ReturnsNoContent()
    {
        // Arrange
        _mockOrderService.Setup(s => s.CheckoutAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Checkout(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task ProcessPayment_SuccessfulPayment_ReturnsOk()
    {
        // Arrange
        _mockOrderService.Setup(s => s.ProcessPaymentAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.ProcessPayment(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ProcessPayment_ServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange
        _mockOrderService.Setup(s => s.ProcessPaymentAsync(1))
            .ThrowsAsync(new InvalidOperationException("Order not found"));

        // Act
        var result = await _controller.ProcessPayment(1);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }
}
