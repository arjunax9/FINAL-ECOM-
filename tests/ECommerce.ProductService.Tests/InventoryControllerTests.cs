using ECommerce.ProductService.Controllers;
using ECommerce.ProductService.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ECommerce.ProductService.Tests;

public class InventoryControllerTests
{
    private readonly Mock<IInventoryService> _mockInventoryService;
    private readonly InventoryController _controller;

    public InventoryControllerTests()
    {
        _mockInventoryService = new Mock<IInventoryService>();
        _controller = new InventoryController(_mockInventoryService.Object);
    }

    [Fact]
    public async Task Get_ReturnsOkWithStockQuantity()
    {
        // Arrange
        _mockInventoryService.Setup(s => s.GetQuantityAsync(1)).ReturnsAsync(40);

        // Act
        var result = await _controller.Get(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Update_ValidStock_ReturnsOk()
    {
        // Arrange
        _mockInventoryService.Setup(s => s.UpdateQuantityAsync(1, 100))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Update(1, 100);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockInventoryService.Verify(s => s.UpdateQuantityAsync(1, 100), Times.Once);
    }

    [Fact]
    public async Task DeductStock_SufficientStock_ReturnsOk()
    {
        // Arrange
        _mockInventoryService.Setup(s => s.DeductStockAsync(2, 5)).ReturnsAsync(true);

        // Act
        var result = await _controller.DeductStock(2, 5);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task DeductStock_InsufficientStock_ReturnsBadRequest()
    {
        // Arrange
        _mockInventoryService.Setup(s => s.DeductStockAsync(2, 999)).ReturnsAsync(false);

        // Act
        var result = await _controller.DeductStock(2, 999);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }
}
